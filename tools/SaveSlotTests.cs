using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NewAster.Core;
using NewAster.Data;
public static class SaveSlotTests
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,Json);
    static T Decode<T>(string text){try{return JsonSerializer.Deserialize<T>(text,Json);}catch(JsonException e){throw new ArgumentException("Malformed JSON",e);}}
    static SaveValidationService Validation(int generation=0,SaveMigrationService migrations=null)=>new SaveValidationService(generation,"0.9.5-rc.2",Encode,Decode<FormalCampaignSave>,s=>JsonSerializer.Serialize(s,Json),Decode<GameSave>,migrations:migrations);
    static SaveSlotManager Manager(string path)=>new SaveSlotManager(new SaveTransactionService(path,Validation()));
    static FormalCampaignSave Fresh(int value=0)=>new FormalCampaignSave{revision=value,world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=100+value,stones=300}};
    static string Latest(SaveSlotManager m,int slot,int backup=0)=>m.Transactions.FilePath(slot,m.Transactions.Pointer(slot),backup);
    static string Fingerprint(SaveSlotManager m,int slot)=>string.Join("|",Enumerable.Range(0,3).Select(b=>File.Exists(Latest(m,slot,b))?File.ReadAllText(Latest(m,slot,b)):"missing"));
    public static void CrashWorker(string root,string stage)
    {
        var m=Manager(root);m.Transactions.Fault=s=>{if(s!=stage)return;File.WriteAllText(Path.Combine(root,"crash-ready"),s);Thread.Sleep(Timeout.Infinite);};m.AutoSave(Fresh(99),DateTime.UtcNow);
    }
    public static void Run(Action<bool,string> check,string resources)
    {
        string root=Path.Combine(Path.GetTempPath(),"newaster-plan11-8-"+Guid.NewGuid().ToString("N"));
        Action<Action,string> reject=(action,label)=>{bool bad=false;try{action();}catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){bad=true;}check(bad,label);};
        var now=DateTime.UtcNow;var m=Manager(Path.Combine(root,"main"));
        check(m.Inspect(1).Status==SaveSlotStatus.Empty && m.Inspect(2).Status==SaveSlotStatus.Empty,"SV-01 two empty slots");
        m.Save(1,Fresh(1),now);m.Save(2,Fresh(20),now);m.AutoSave(Fresh(21),now);
        check(m.Load(1).revision==1 && m.Load(2).revision==21,"SV-01/02 independent manual and active auto save");
        check(Manager(m.Transactions.Root).ActiveSlot==2,"SV-02 active slot persists across restart");
        m.Load(1);var delayed=new DelayedSaveCoordinator(m);delayed.Queue(Fresh(2),0);delayed.Flush(4.99,now);
        check(m.Load(1).revision==1 && delayed.Dirty,"SV-03 no write before five seconds");
        delayed.Queue(Fresh(3),4);delayed.Flush(8.99,now);check(m.Load(1).revision==1,"SV-03 debounce resets on last change");
        delayed.Flush(9,now);check(m.Load(1).revision==3 && !delayed.Dirty,"SV-03 five second flush");
        delayed.Queue(Fresh(4),10);delayed.Flush(10,now,true);check(m.Load(1).revision==4,"SV-03 transition forces pending flush");
        delayed.Queue(Fresh(5),11);delayed.SaveImmediate(Fresh(6),now);check(m.Load(1).revision==6 && !delayed.Dirty,"SV-02 critical save supersedes deferred snapshot");
        m.Save(1,Fresh(7),now);check(m.Transactions.Validation.Validate(m.Transactions.Validation.Read(Latest(m,1,1))).revision==6 && m.Transactions.Validation.Validate(m.Transactions.Validation.Read(Latest(m,1,2))).revision==4,"SV-04 exactly two rotated backups");
        foreach(string stage in new[]{"latest-verified","backups-verified","before-publish"}){
            string before=Fingerprint(m,1),pointer=m.Transactions.Pointer(1);m.Transactions.Fault=s=>{if(s==stage)throw new IOException("disk full / interrupted");};
            reject(()=>m.Save(1,Fresh(8),now),"SV-06/09 injected failure "+stage);
            check(pointer==m.Transactions.Pointer(1) && Fingerprint(m,1)==before,"SV-06 all current and backup bytes preserved "+stage);
        }
        delayed.Queue(Fresh(8),20);reject(()=>delayed.Flush(25,now),"SV-03 failed delayed write");check(delayed.Dirty,"SV-03 failed deferred change retained");m.Transactions.Fault=null;delayed.Flush(30,now);
        var offer=m.Inspect(1,2);long restored=m.Transactions.Validation.Validate(m.Transactions.Validation.Read(Latest(m,1,2))).revision;
        m.RestoreConfirmed(offer,now);check(m.Load(1).revision==restored && m.Transactions.Validation.Validate(m.Transactions.Validation.Read(Latest(m,1,1))).revision==8,"SV-05 restore keeps pre-restore latest as backup1");
        reject(()=>m.RestoreConfirmed(offer,now),"SV-06 stale restore candidate rejected");
        string restoreBefore=Fingerprint(m,1);var validOffer=m.Inspect(1,1);m.Transactions.Fault=s=>{if(s=="before-publish")throw new IOException("restore interrupted");};reject(()=>m.RestoreConfirmed(validOffer,now),"SV-06 interrupted restore rejected");check(Fingerprint(m,1)==restoreBefore,"SV-06 restore failure preserves all three files");m.Transactions.Fault=null;
        m.Transactions.Fault=s=>{if(s=="after-publish")throw new IOException("uncertain reply");};m.Save(1,Fresh(10),now);m.Transactions.Fault=null;
        check(m.Load(1).revision==10,"SV-09 committed pointer recognized after reply failure");
        string old=File.ReadAllText(Latest(m,1));File.WriteAllText(Latest(m,1),old.Replace("\"checksum\":\"","\"checksum\":\"broken"));
        check(m.Inspect(1).Status==SaveSlotStatus.Corrupt && m.Inspect(1,1).Status==SaveSlotStatus.Ready,"SV-10 corruption detected without damaging backups");
        m.RestoreConfirmed(m.Inspect(1,1),now);check(m.Inspect(1).Status==SaveSlotStatus.Ready && File.Exists(Path.Combine(Path.GetDirectoryName(Latest(m,1)),"preserved-corrupt.json")),"SV-05 corrupt main restored with original bytes retained");
        File.WriteAllText(Latest(m,1),"{");check(m.Inspect(1).Status==SaveSlotStatus.Corrupt,"SV-10 malformed JSON safely rejected");m.RestoreConfirmed(m.Inspect(1,1),now);
        string other=Fingerprint(m,2);var ticket=m.RequestDelete(1);reject(()=>m.DeleteConfirmed(ticket),"SV-07 one confirmation cannot delete");
        m.ConfirmDelete(ticket);m.DeleteConfirmed(ticket);check(m.Inspect(1).Status==SaveSlotStatus.Empty && Fingerprint(m,2)==other,"SV-07 selected tree deleted independently");
        m.Save(1,Fresh(),now);check(m.Inspect(1,1).Status==SaveSlotStatus.Empty,"SV-07 deleted slot reusable without old backups");
        File.WriteAllText(Path.Combine(m.Transactions.SlotPath(1),"current"),"../../outside");check(m.Inspect(1).Status==SaveSlotStatus.Corrupt,"SV-10 unsafe pointer rejected");
        ticket=m.RequestDelete(1);m.ConfirmDelete(ticket);m.DeleteConfirmed(ticket);check(m.Inspect(1).Status==SaveSlotStatus.Empty,"SV-07 corrupt pointer slot can be deleted safely");
        check(!typeof(SaveSlotManager).GetMethods().Any(x=>x.Name.IndexOf("DeleteAll",StringComparison.OrdinalIgnoreCase)>=0),"SV-08 no delete all operation");
        m.Save(1,Fresh(),now);using(m.Transactions.Lock()){
            reject(()=>m.Save(2,Fresh(),now),"SV-02 exclusive save lock");reject(()=>m.Load(1),"SV-06 exclusive load lock");reject(()=>m.RestoreConfirmed(m.Inspect(1,1),now),"SV-06 exclusive restore lock");reject(()=>m.RequestDelete(2),"SV-07 exclusive delete lock");
        }
        var stale=Manager(m.Transactions.Root);m.Save(1,Fresh(2),now);reject(()=>stale.AutoSave(Fresh(3),now),"SV-09 stale writer cannot overwrite external commit");
        reject(()=>m.Save(3,Fresh(),now),"SV-10 reject slot3");reject(()=>new SaveTransactionService("relative",Validation()),"SV-10 reject relative path");reject(()=>new SaveTransactionService(Path.Combine(root,"..","outside"),Validation()),"SV-10 reject traversal");
        var doc=Validation().Create(Fresh(),Guid.NewGuid().ToString("N"),now);doc.header.schemaVersion=2;doc.checksum=SaveValidationService.Checksum(doc);reject(()=>Validation().Validate(doc),"SV-16 newer schema refused");
        var migration=new SaveMigrationService(2);migration.Register(1,s=>{s.growth.nectar+=7;return s;});doc.header.schemaVersion=1;doc.checksum=SaveValidationService.Checksum(doc);
        check(Validation(0,migration).Validate(doc).growth.nectar==107,"SV-16 ordered schema migration");reject(()=>Validation(0,new SaveMigrationService(2)).Validate(doc),"SV-16 missing migration refused");reject(()=>Validation(1).Validate(doc),"SV-15 development data refused by release");
        check(SaveSlotManager.GenerationDirectory(root,0)!=SaveSlotManager.GenerationDirectory(root,1),"SV-15 directories separate");
        var many=Fresh();many.growth.heroines=Enumerable.Range(0,256).Select(i=>new FormalHeroineGrowth{heroineId="heroine.test-"+i}).ToArray();m.Save(1,many,now);check(m.Load(1).growth.heroines.Length==256,"SV-17 256 persons round trip");
        FullSystems(check,resources,Manager(Path.Combine(root,"systems")));
        foreach(string stage in new[]{"latest-verified","backups-verified","before-publish","after-publish"}){
            var crash=Manager(Path.Combine(root,"crash-"+stage));for(int i=1;i<=3;i++)crash.Save(1,Fresh(i),now);string before=Fingerprint(crash,1);
            var start=new ProcessStartInfo(Environment.ProcessPath){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);start.ArgumentList.Add("--save-crash-helper");start.ArgumentList.Add(crash.Transactions.Root);start.ArgumentList.Add(stage);
            using(var process=Process.Start(start)){
                var timer=Stopwatch.StartNew();while(!File.Exists(Path.Combine(crash.Transactions.Root,"crash-ready")) && !process.HasExited && timer.ElapsedMilliseconds<15000)Thread.Sleep(20);
                bool reached=File.Exists(Path.Combine(crash.Transactions.Root,"crash-ready"));if(!process.HasExited)process.Kill(true);process.WaitForExit();check(reached,"SV-09 real child reached "+stage);
            }
            var reopened=Manager(crash.Transactions.Root);check(reopened.Load(1).revision==(stage=="after-publish"?99:3),"SV-09 forced process kill recovery "+stage);
            check(stage=="after-publish"?reopened.Inspect(1,2).Status==SaveSlotStatus.Ready:Fingerprint(reopened,1)==before,"SV-09 complete backup set after forced kill "+stage);
        }
        Console.WriteLine("PLAN11_8_SAVE_PASS forced-kill=4 root="+root);
    }
    static void FullSystems(Action<bool,string> check,string resources,SaveSlotManager m)
    {
        var combat=Decode<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-shangrila.json")));
        var story=Decode<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-shangrila-story-content.json")));var home=ProductionStoryCatalog.Home(combat,story);var catalog=ProductionStoryCatalog.Collection(combat,story);
        var s=Fresh();s.growth.stones=30000;s.growth.nectar=300000;s.growth.heroines=combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=30}).ToArray();s.home=FormalHomeProgress.Empty(home.contentVersion);s.home.formationIds=combat.FormationIds;
        s.collection=new FormalCollectionLedger{contentVersion=catalog.contentVersion,relics=catalog.relics.Select(r=>new CollectionRelic{id=r.id,contentVersion=catalog.contentVersion}).ToArray(),materials=catalog.resources.Where(r=>r.kind=="material").Select(r=>new CollectionMaterial{id=r.id,sourceColossusId=r.ownerId,amount=10000}).ToArray()};
        OopartSaveAdapter.Migrate(s.collection,catalog,s.home.formationIds);AffectionSaveAdapter.Migrate(s,home);AffectionService.State(s,home,"heroine.slayer").level=20;AffectionRingService.Purchase(s);AffectionRingService.Use(s,home,"heroine.slayer");
        var def=catalog.oopartDefs.First();var rolls=new StatValues();foreach(var stat in def.randomStats)rolls.Set(stat.stat,stat.maximum/2);OopartService.Merge(s.collection.ooparts,def,rolls);OopartService.Equip(s.collection.ooparts,0,def.id);
        s.world.unlockedGardenIds=home.gardens.Select(g=>g.id).ToArray();foreach(var d in s.world.terraform.domains){d.currentLevel=5;d.maxReachedLevel=5;}s.world.terraform.totalTp=500;HomeConditions.Refresh(s,home);
        s.home.furnitureInstances=new[]{new HomeFurnitureInstance{instanceId="furniture.saved.bench",defId="furniture.fixture.0"}};s.home.furniturePlacements=new[]{new HomePlacement{instanceId="furniture.saved.bench",defId="furniture.fixture.0",gardenId=home.gardens[0].id,zoneId="zone.ground",orientationId="orientation.default",x=.25f,y=.72f}};
        s.home.occupants=new[]{new HomeOccupant{heroineId="heroine.slayer",gardenId=home.gardens[0].id,slotId="slot.idle",x=.22f,y=.7f}};GardenLifeCatalog.Migrate(s);
        GardenLifeDiscoveries.Grant(s,"garden.discovery.morning","save.discovery",new GardenLifeContext{gardenId=home.gardens[0].id,timePhase="morning",weather="clear",heroineId="heroine.slayer"});
        s.bookNavigation=new BookNavigationSave{lastBookmarkId="heroines",pages=new[]{new BookPageState{bookmarkId="heroines",selectedSubjectId="heroine.slayer",selectedFaceId="back",searchQuery="saved",filterIds=new[]{"owned"},selectedColossusLevel=50}}};s.playRewards=new PlayRewardState{totalPlaySeconds=12345,rewardRemainderSeconds=12.5,lastDailyRewardDate="2026-10-09"};
        m.Save(1,s,DateTime.UtcNow);check(Encode(m.Load(1))==Encode(s),"SV-12 unified affection ring oopart growth materials round trip");
        var loaded=m.Load(1);check(loaded.world.terraform.totalTp==500 && loaded.world.terraform.domains.All(d=>d.currentLevel==5) && loaded.home.furniturePlacements.Single().x==.25f && loaded.gardenLife.records.Length==1 && loaded.gardenLife.assignments.Length==1,"SV-13 terraform furniture assignments and discovery survive restart");
        check(loaded.bookNavigation.pages.Single().selectedSubjectId=="heroine.slayer" && loaded.bookNavigation.pages.Single().searchQuery=="saved" && loaded.bookNavigation.pages.Single().selectedColossusLevel==50 && m.Inspect(1).PlaySeconds==12345,"SV-14 book location search summon level and playtime round trip");
        var journal=new FormalCampaignJournal(m.Load(1),Encode,Decode<FormalCampaignSave>);var buy=new AffectionRequest("save.ring",journal.Revision,"ring-purchase",Array.Empty<string>(),"ring");
        check(journal.CommitAffection(buy,home,p=>m.AutoSave(p,DateTime.UtcNow))==GrowthCommitResult.Committed,"SV-11 ring transaction durably commits");
        var reopened=new FormalCampaignJournal(Manager(m.Transactions.Root).Load(1),Encode,Decode<FormalCampaignSave>);check(reopened.CommitAffection(buy,home,p=>throw new Exception("duplicate write"))==GrowthCommitResult.AlreadyCommitted && reopened.Snapshot.growth.stones==10000,"SV-11 receipt prevents duplicate spend after restart");
        check(!Encode(s).Contains("reactionStyle") && !Encode(s).Contains("SocialSession") && !Encode(s).Contains("secondTraitId"),"SV-12 static definitions and temporary sessions absent");
    }
}
