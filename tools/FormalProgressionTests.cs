using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using NewAster.Core;

public static class FormalProgressionTests
{
    public static void Battle(Action<bool,string> check,CombatDefinitionCatalog catalog)
    {
        catalog.Validate();
        var save=new FormalGrowthSave {saveId="growth.battle",heroines=catalog.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id,level=50,duplicateRank=5}).ToArray()};
        var legacy=new PlayableProgress();
        var battle=new PlayableBattle(1,legacy,combatDefinitions:catalog,formalGrowth:save);
        for(int i=0;i<5;i++) {
            var h=catalog.Hero(catalog.FormationIds[i]);var j=catalog.Job(h.jobId);var actor=battle.State.Heroes[i];
            int trait=h.traitAttackPercent==0?0:FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,5);
            check(actor.Attack==(int)((long)FormalGrowthMath.Stat(j.attack,h.attackBp,50,5)*(10000+trait)/10000),"Growth and explicit trait rank affect actual battle attack");
            check(actor.PhysicalDefense==FormalGrowthMath.Stat(j.defense,h.defenseBp,50,5),"Growth and rank affect battle defense");
            check(actor.Speed==FormalGrowthMath.Speed(j.speed,h.speedBp),"Growth does not increase speed");
        }
        int attack=battle.State.Heroes[0].Attack;save.heroines[0].level=1;save.heroines[0].duplicateRank=0;
        check(battle.State.Heroes[0].Attack==attack,"Departure snapshots growth; later changes cannot affect battle");
        check(legacy.Levels.All(level=>level==1),"Formal growth never changes legacy progression");
        bool rejected=false;save.heroines=save.heroines.Skip(1).ToArray();
        try{new PlayableBattle(1,legacy,combatDefinitions:catalog,formalGrowth:save);}catch(ArgumentException){rejected=true;}
        check(rejected,"Unowned formation rejected");
    }
    public static void Run(Action<bool,string> check)
    {
        var ids=new[]{"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
        Func<FormalGrowthSave> fresh=()=>new FormalGrowthSave {saveId="test.formal-growth",nectar=20000,awakeningCrystals=80,heroines=ids.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
        Action<Action,string> rejects=(action,label)=>{bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(OverflowException){rejected=true;}check(rejected,label);};
        check(FormalProgression.LevelCost(1,50)==2940,"Nectar to Lv50");
        check(FormalProgression.LevelCost(50,80)==4170,"Nectar to Lv80");
        check(FormalProgression.LevelCost(80,120)==8360,"Nectar to Lv120");
        check(FormalProgression.LevelCost(1,120)==15470,"Nectar total to Lv120");
        rejects(()=>FormalProgression.LevelCost(1,1),"No-op leveling rejected");
        for(int level=1;level<120;level++) check(FormalProgression.LevelCost(level,level+1)==10+2*level,"Per-level nectar matches formula");
        var input=fresh();var state=new FormalProgression(input,ids);input.nectar=0;input.heroines[0].level=50;
        check(state.Snapshot.nectar==20000 && state.Snapshot.heroines[0].level==1,"Input detached");
        var altered=state.Snapshot;altered.heroines[0].level=49;
        check(state.Snapshot.heroines[0].level==1,"Snapshot detached");
        var request=new GrowthRequest("growth.level50",ids[0],0,GrowthOperation.Level,50);
        var preview=state.Preview(request);preview.HeroineAfter.level=3;
        check(preview.NectarCost==2940 && state.Snapshot.revision==0 && state.Snapshot.nectar==20000,"Preview does not consume");
        rejects(()=>state.Preview(new GrowthRequest("growth.too-high",ids[0],0,GrowthOperation.Level,51)),"Cap boundary rejected");
        rejects(()=>state.Preview(new GrowthRequest("growth.early-awaken",ids[0],0,GrowthOperation.Awaken)),"Early awakening rejected");
        FormalGrowthSave saved=null;
        Func<FormalGrowthSave,bool> write=s=>{saved=s.Copy();return true;};
        check(state.Commit(request,s=>false)==GrowthCommitResult.SaveFailed,"Failed save remains pending");
        check(state.HasPending && state.Snapshot.nectar==20000 && state.Snapshot.heroines[0].level==1 && state.Snapshot.revision==0,"Save failure leaves memory unchanged");
        rejects(()=>state.Commit(new GrowthRequest("growth.other",ids[1],0,GrowthOperation.Level,2),write),"Other operation blocked while pending");
        rejects(()=>state.Preview(request),"Preview blocked while pending");
        check(state.Commit(request,s=>{s.nectar=0;return false;})==GrowthCommitResult.SaveFailed,"Writer cannot mutate retry payload");
        check(state.Commit(request,write)==GrowthCommitResult.Committed && saved.nectar==17060 && saved.heroines[0].level==50,"Retry retains original result");
        int writes=0;
        check(state.Commit(request,s=>{writes++;return true;})==GrowthCommitResult.AlreadyCommitted && writes==0,"Committed duplicate is idempotent");
        rejects(()=>state.Commit(new GrowthRequest(request.TransactionId,ids[1],0,GrowthOperation.Level,50),write),"ID collision rejected");
        rejects(()=>state.Commit(new GrowthRequest("growth.stale",ids[0],0,GrowthOperation.Awaken),write),"Stale revision rejected");
        check(state.Commit(new GrowthRequest("growth.awaken1",ids[0],1,GrowthOperation.Awaken),write)==GrowthCommitResult.Committed,"Awakening1 committed");
        check(state.Snapshot.heroines[0].level==50 && state.Snapshot.heroines[0].LevelCap==80 && state.Snapshot.awakeningCrystals==60,"Awakening changes cap only");
        state.Commit(new GrowthRequest("growth.level80",ids[0],2,GrowthOperation.Level,80),write);
        state.Commit(new GrowthRequest("growth.awaken2",ids[0],3,GrowthOperation.Awaken),write);
        state.Commit(new GrowthRequest("growth.level120",ids[0],4,GrowthOperation.Level,120),write);
        check(state.Snapshot.nectar==4530 && state.Snapshot.awakeningCrystals==0 && state.Snapshot.heroines[0].level==120,"Full progression consumes exact currencies");
        rejects(()=>state.Preview(new GrowthRequest("growth.awaken3",ids[0],5,GrowthOperation.Awaken)),"Third awakening rejected");
        var duplicate=fresh();duplicate.heroines[0].fragments=540;duplicate.overflow=60;
        var ds=new FormalProgression(duplicate,ids);
        for(int rank=0;rank<5;rank++) ds.Commit(new GrowthRequest("growth.rank"+rank,ids[0],rank,GrowthOperation.Strengthen),s=>true);
        check(ds.Snapshot.heroines[0].duplicateRank==5 && ds.Snapshot.heroines[0].fragments==0 && ds.Snapshot.overflow==100,"Leftover fragments convert at cap");
        ds.Commit(new GrowthRequest("growth.receive-max",ids[0],5,GrowthOperation.ReceiveHeroine),s=>true);
        check(ds.Snapshot.overflow==200 && ds.Snapshot.heroines[0].level==1,"Maximum duplicate grants overflow without losing hero");
        rejects(()=>ds.Preview(new GrowthRequest("growth.rank6",ids[0],6,GrowthOperation.Strengthen)),"Sixth rank rejected");
        var mixed=fresh();mixed.heroines[0].fragments=40;mixed.overflow=60;
        var ms=new FormalProgression(mixed,ids);var mix=ms.Preview(new GrowthRequest("growth.mixed",ids[0],0,GrowthOperation.Strengthen));
        check(mix.FragmentCost==40 && mix.OverflowCost==60,"Dedicated fragments consumed first");
        ms.Commit(new GrowthRequest("growth.mixed",ids[0],0,GrowthOperation.Strengthen),s=>true);
        check(ms.Snapshot.overflow==0 && ms.Snapshot.heroines[0].fragments==0 && ms.Snapshot.heroines[0].duplicateRank==1,"Mixed strengthening applied once");
        var unowned=fresh();unowned.heroines=Array.Empty<FormalHeroineGrowth>();var us=new FormalProgression(unowned,ids);
        us.Commit(new GrowthRequest("growth.first",ids[0],0,GrowthOperation.ReceiveHeroine),s=>true);
        check(us.Snapshot.heroines[0].fragments==0,"First acquisition grants ownership not fragments");
        us.Commit(new GrowthRequest("growth.second",ids[0],1,GrowthOperation.ReceiveHeroine),s=>true);
        check(us.Snapshot.heroines[0].fragments==100 && us.Snapshot.heroines[0].duplicateRank==0,"Duplicate grants fragments not automatic rank");
        var unknown=fresh();unknown.heroines=unknown.heroines.Concat(new[]{new FormalHeroineGrowth {heroineId="heroine.unknown",level=20}}).ToArray();var unknownState=new FormalProgression(unknown,ids);
        rejects(()=>unknownState.Preview(new GrowthRequest("growth.unknown", "heroine.unknown",0,GrowthOperation.Level,21)),"Unknown owned ID cannot act");
        unknownState.Commit(new GrowthRequest("growth.known",ids[0],0,GrowthOperation.Level,2),s=>true);
        check(unknownState.Snapshot.heroines.Single(h=>h.heroineId=="heroine.unknown").level==20,"Unknown owned ID preserved across write");
        var future=fresh();future.version=2;rejects(()=>new FormalProgression(future,ids),"Future save version rejected");
        future=fresh();future.contentVersion="future";rejects(()=>new FormalProgression(future,ids),"Future content version rejected");
        var empty=fresh();empty.nectar=11;var poor=new FormalProgression(empty,ids);
        rejects(()=>poor.Preview(new GrowthRequest("growth.poor",ids[0],0,GrowthOperation.Level,2)),"Insufficient nectar rejected");
        check(poor.Snapshot.nectar==11 && !poor.HasPending,"Insufficient balance leaves state unchanged");
        var full=fresh();full.heroines[0].duplicateRank=5;full.overflow=int.MaxValue;var fullState=new FormalProgression(full,ids);
        rejects(()=>fullState.Commit(new GrowthRequest("growth.overflow",ids[0],0,GrowthOperation.ReceiveHeroine),s=>true),"Overflow arithmetic rejected");
        check(fullState.Snapshot.overflow==int.MaxValue && !fullState.HasPending,"Overflow rejection atomic");
        var throwing=new FormalProgression(fresh(),ids);var retry=new GrowthRequest("growth.exception",ids[0],0,GrowthOperation.Level,2);
        bool threw=false;try{throwing.Commit(retry,s=>throw new System.IO.IOException("test"));}catch(System.IO.IOException){threw=true;}
        check(threw && throwing.HasPending && throwing.Snapshot.revision==0,"IO exception retains exact pending operation");
        rejects(()=>throwing.Commit(retry,s=>{throwing.Commit(retry,s2=>true);return true;}),"Reentrant writes rejected");
        throwing.Commit(retry,s=>true);check(throwing.Snapshot.nectar==19988,"Exception retry succeeds once");
        var options=new JsonSerializerOptions {IncludeFields=true};
        var roundtrip=JsonSerializer.Deserialize<FormalGrowthSave>(JsonSerializer.Serialize(state.Snapshot,options),options);
        roundtrip.Validate();var reloaded=new FormalProgression(roundtrip,ids);
        check(reloaded.Commit(request,s=>false)==GrowthCommitResult.AlreadyCommitted,"Receipt survives serialization and restart");
        check(FormalGrowthMath.Stat(1100,10000,50,0)==2717 && FormalGrowthMath.Stat(1100,10000,120,5)==5529,"Job growth and rank formula");
        check(FormalGrowthMath.Speed(105,10000)==105 && FormalGrowthMath.TraitAmount(500,5)==550,"Speed fixed and trait units preserve precision");
        for(int rank=0;rank<=5;rank++) for(int level=1;level<=120;level++) check(FormalGrowthMath.Stat(110,10000,level,rank)==(110*(100+3*(level-1))/100)*(100+2*rank)/100,"Growth floors at defined boundaries");
        rejects(()=>FormalGrowthMath.Stat(110,11001,1,0),"Invalid character modifier rejected");
        rejects(()=>FormalGrowthMath.Speed(100,10501),"Invalid speed modifier rejected");
        string directory=Path.Combine(Path.GetTempPath(),"newaster-growth-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);string path=Path.Combine(directory,"formal.json");
        Func<FormalGrowthSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalGrowthSave> decode=text=>{try{return JsonSerializer.Deserialize<FormalGrowthSave>(text,options);}catch(JsonException e){throw new ArgumentException("Invalid JSON",e);}};
        try {
            var store=new FormalGrowthStore(path,"test.formal-growth",encode,decode);
            check(store.Load(out _) == FormalLoadResult.Missing,"New formal slot is empty");
            store.Save(fresh());
            var diskState=new FormalProgression(fresh(),ids);
            var diskRequest=new GrowthRequest("growth.disk",ids[0],0,GrowthOperation.Level,2);
            check(diskState.Commit(diskRequest,store.Save)==GrowthCommitResult.Committed,"Real atomic file commit succeeds");
            check(store.Load(out var diskSave)==FormalLoadResult.Loaded && diskSave.revision==1 && diskSave.nectar==19988,"Real file load returns committed payload");
            check(File.Exists(path+".bak") && decode(File.ReadAllText(path+".bak")).revision==0,"Previous normal file backed up");
            check(!File.Exists(path+".tmp"),"Temporary is consumed by replace");
            rejects(()=>store.Save(diskSave),"Repeated durable revision rejected");
            string normal=File.ReadAllText(path);
            var futureFile=diskSave.Copy();futureFile.version=2;File.WriteAllText(path,encode(futureFile));
            check(store.Load(out _)==FormalLoadResult.Blocked,"Future primary not silently rolled back to backup");
            rejects(()=>store.Save(diskSave),"Future primary cannot be overwritten");
            check(decode(File.ReadAllText(path)).version==2,"Future file preserved");
            File.WriteAllText(path,"broken");
            check(store.Load(out var recovered)==FormalLoadResult.RecoveredBackup && recovered.revision==0,"Corrupt primary recovers matching backup read-only");
            rejects(()=>store.Save(diskSave),"Corrupt primary requires explicit recovery before write");
            check(File.ReadAllText(path)=="broken","Corrupt original not overwritten");
            var alien=fresh();alien.saveId="another.save";File.WriteAllText(path+".bak",encode(alien));
            check(store.Load(out _)==FormalLoadResult.Blocked,"Wrong-identity backup rejected");
            File.WriteAllText(path,normal);File.WriteAllText(path+".tmp",encode(futureFile));
            check(store.Load(out var primary)==FormalLoadResult.Loaded && primary.revision==1,"Uncommitted temporary ignored");
            File.Delete(path);
            check(store.Load(out _)==FormalLoadResult.Blocked,"Orphan backup prevents silent new game");
        } finally {
            foreach(string file in new[]{path,path+".bak",path+".tmp"}) if(File.Exists(file)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}
