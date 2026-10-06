using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private ProductionStoryContent productionStory;
        private bool ProductionStoryActive=>!homeTrial && !plan8StoryTrial && (!formalDiagnostic || (Environment.GetCommandLineArgs().Contains("-capturePlan10Ui") || Environment.GetCommandLineArgs().Contains("-captureShangrila") || Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy") || Environment.GetCommandLineArgs().Contains("-captureArcane") || Environment.GetCommandLineArgs().Contains("-captureSlayerSwim") || Environment.GetCommandLineArgs().Contains("-captureNighthawk") || Environment.GetCommandLineArgs().Contains("-captureOriflamme") || Environment.GetCommandLineArgs().Contains("-captureShell") || Environment.GetCommandLineArgs().Contains("-captureAnnihilator") || Environment.GetCommandLineArgs().Contains("-capturePlan10") || Environment.GetCommandLineArgs().Contains("-capturePlan9Story") || Environment.GetCommandLineArgs().Contains("-capturePlan9ProductionEntry")));
        private bool HomeOperationsAllowed=>formalDiagnostic || ProductionStoryActive;
        private ProductionStoryContent ProductionStoryData()
        {
            if(productionStory==null){
                var asset=Resources.Load<TextAsset>(combatDefinitions.HeroineIds.Contains("heroine.shangrila")?"Story/plan10-shangrila-story-content":combatDefinitions.HeroineIds.Contains("heroine.arcane-academy")?"Story/plan10-arcane-academy-story-content":combatDefinitions.HeroineIds.Contains("heroine.arcane")?"Story/plan10-arcane-story-content":combatDefinitions.HeroineIds.Contains("heroine.slayer-swim")?"Story/plan10-slayer-swim-story-content":combatDefinitions.HeroineIds.Contains("heroine.nighthawk")?"Story/plan10-nighthawk-story-content":combatDefinitions.HeroineIds.Contains("heroine.oriflamme")?"Story/plan10-oriflamme-story-content":combatDefinitions.HeroineIds.Contains("heroine.shell")?"Story/plan10-shell-story-content":combatDefinitions.HeroineIds.Contains("heroine.annihilator")?"Story/plan10-annihilator-story-content":combatDefinitions.HeroineIds.Contains("heroine.r")?"Story/plan10-story-content":"Story/plan9-story-content");if(asset==null)throw new ArgumentException("Production story resource missing.");
                productionStory=JsonUtility.FromJson<ProductionStoryContent>(asset.text);productionStory.Validate(combatDefinitions.HeroineIds,WorldCatalog.ColossusIds.ToArray());
            }return productionStory;
        }
        private CollectionCatalog SelectCollectionCatalog()=>plan8StoryTrial?TrialStoryCatalog.Collection(combatDefinitions,StoryData()):ProductionStoryActive?ProductionStoryCatalog.Collection(combatDefinitions,ProductionStoryData()):CollectionContractFixture.Create(combatDefinitions);
        private HomeExperienceCatalog SelectHomeCatalog()=>ProductionStoryActive?ProductionStoryCatalog.Home(combatDefinitions,ProductionStoryData()):HomeExperienceFixture.Create(combatDefinitions);
        private string ProductionStoryTitle(string id)=>ProductionStoryData().chapters.Where(c=>c.id==id).Select(c=>c.title).Concat(ProductionStoryData().events.Where(e=>e.id==id).Select(e=>e.title)).FirstOrDefault()??"物語";
        private FormalCampaignSave PrepareProductionCampaign(FormalCampaignSave original)
        {
            if(!ProductionStoryMigration.Required(original))return original;
            var migrated=ProductionStoryMigration.Prepare(original,combatDefinitions,ProductionStoryData(),s=>UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(s)));
            if(!formalDiagnostic){
                string path=formalCampaignStore.SavePath;
                if(File.Exists(path)){string archive=path+".before-production-story-r"+original.revision+".json";if(!File.Exists(archive))File.Copy(path,archive,false);}
                if(!formalCampaignStore.Save(migrated))throw new IOException("Production story migration could not be saved.");
            }
            Debug.Log("PLAN9_STORY_MIGRATION_PASS revision="+migrated.revision+" original narrative history archived");return migrated;
        }
        private void DrawProductionPoemConditions()
        {
            var chapter=ProductionStoryData().chapters.Single(c=>c.id==trialPoemChapter);var world=formalCampaign.Snapshot.world;
            Label(150,210,1280,55,chapter.title+" ／ 章を開く条件",growthTitleStyle);
            trialPoemScroll=GUI.BeginScrollView(new Rect(150,285,1280,400),trialPoemScroll,new Rect(0,0,1230,chapter.poems.Length*140));
            for(int i=0;i<chapter.poems.Length;i++){
                var poem=chapter.poems[i];string detail=(world.poemIds.Contains(poem.id)?"取得済み：":"未取得：")+poem.text;
                if(!string.IsNullOrEmpty(poem.sourcePoemId)){
                    var sourceChapter=ProductionStoryData().chapters.Single(c=>c.poems.Any(p=>p.id==poem.sourcePoemId));
                    if(campaign.ColossusUnlocks.IsUnlocked(sourceChapter.ownerId)){var source=sourceChapter.poems.Single(p=>p.id==poem.sourcePoemId);detail+="\n"+WorldCatalog.Colossi.Single(c=>c.Id==sourceChapter.ownerId).DisplayName+"の歌「"+source.text+"」を開始編成で聞く。\n"+poem.reason;}
                    else detail+="\n対応元の巨神獣を解放すると、歌と対応理由を確認できます。";
                }else detail+="\nこの巨神獣の完了した行動で歌を聞く。敗北・撤退でも持ち帰れます。";
                Label(10,i*140,1200,130,detail,growthSmallStyle);
            }GUI.EndScrollView();if(GrowthButton(150,715,1280,65,"章の進捗へ戻る"))trialPoemChapter=null;
        }
        private void PreparePlan9StoryCapture(string[] args)
        {
            if(!formalDiagnostic || capturePath==null || !ProductionStoryActive)throw new InvalidOperationException("Production story capture requires isolated diagnostic mode.");
            Func<string,string> option=key=>{int at=Array.IndexOf(args,key);if(at<0 || at+1>=args.Length || args.Count(v=>v==key)!=1)throw new ArgumentException("Missing story capture option: "+key);return args[at+1];};
            var boundary=TrialDiagnosticBoundary.Create(option("-plan9RepositoryRoot"),Application.persistentDataPath,option("-plan9StoryRunId"));Directory.CreateDirectory(boundary.DirectoryPath);
            acceptanceStore=new FormalCampaignStore(boundary.SavePath,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);acceptanceChecks=0;
            var story=ProductionStoryData();var snapshot=formalCampaign.Snapshot;
            snapshot.world.poemIds=story.chapters.SelectMany(c=>c.poems).Select(p=>p.id).ToArray();
            snapshot.world.firstClearIds=WorldCatalog.ColossusIds.Take(14).ToArray();
            snapshot.world.unlockedGardenIds=HomeData().gardens.Take(2).Select(g=>g.id).ToArray();
            snapshot.home.affections=combatDefinitions.FormationIds.Select(id=>new HomeAffection{heroineId=id,value=20}).ToArray();HomeConditions.Refresh(snapshot,HomeData());
            AcceptanceCheck(acceptanceStore.Save(snapshot),"production diagnostic fixture is saved only in separate profile");BindFormalCampaign(snapshot);title=false;encounter=null;
            Debug.Log("PLAN9_STORY_DIAGNOSTIC_FIXTURE poems=450 affection=20 worldUnlocks=14; state prepared for UI and contracts, not earned progression");
            string chapter=story.chapters[0].id;ReadProductionDiagnosticScene(chapter,true);
            foreach(string hero in combatDefinitions.FormationIds)for(int i=0;i<5;i++){
                string id=hero+".event."+i;ReadProductionDiagnosticScene(id,false);
                AcceptanceCheck(formalCampaign.Snapshot.home.loverHeroineIds.Contains(hero)==(i>=2),"production lover state begins only at completed mutual confession");
            }
            ReloadAcceptance();
            AcceptanceCheck(formalCampaign.Snapshot.home.readEventIds.Length==25 && formalCampaign.Snapshot.home.loverHeroineIds.Length==5,"all production events survive physical reload with correct relationships");
            var saved=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);BeginAdv(story.events[0].id,true);
            int steps=0;while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}CompleteAdv();CloseAdv();
            AcceptanceCheck(saved==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"production replay never changes wallet affection receipts or read state");
            collectionOpen=false;trialPoemChapter=null;ResetGardenMenu();string view=option("-plan9StoryView");
            if(view=="chapters" || view=="conditions"){
                collectionOpen=true;collectionOwner=view=="conditions"?15:0;collectionTab=0;
                if(view=="conditions"){trialPoemChapter="heroine.slayer.poem-chapter.2";trialPoemScroll=Vector2.zero;}
            }else if(view.StartsWith("economy.",StringComparison.Ordinal)){
                PrepareProductionEconomyCapture(view.Substring(8));
            }else if(view.StartsWith("garden.",StringComparison.Ordinal)){
                PrepareProductionGardenCapture(view);
            }else if(view=="events"){
                book.ChangeBookmark(BookBookmark.Gardens);selectedResident="heroine.slayer";OpenGardenPanel(GardenPanel.Events);
            }else if(view=="book"){
                book.RequestSubject(BookBookmark.Stories,WorldCatalog.ColossusIds[0]);book.CompleteTransition();
            }else if(view=="chapter"){
                book.ChangeBookmark(BookBookmark.Stories);book.RequestSubject(BookBookmark.Stories,story.chapters[0].ownerId);book.CompleteTransition();BeginAdv(chapter,true);AcceptanceCheck(adv!=null && adv.SourceId==chapter,"capture selects authored chapter");AdvanceAdv();
            }else if(view.StartsWith("event.",StringComparison.Ordinal)){
                var parts=view.Split('.');if(parts.Length!=3 || !int.TryParse(parts[2],out int eventIndex) || eventIndex<0 || eventIndex>4)throw new ArgumentException("Unknown production event capture.");
                string id="heroine."+parts[1]+".event."+eventIndex;BeginAdv(id,true);AcceptanceCheck(adv!=null && adv.SourceId==id,"capture selects requested authored event");
                for(int line=0;line<3;line++){AdvanceAdv();if(line<2)AdvanceAdv();}
                AcceptanceCheck(adv.CgId!=null && HomeData().assets.Single(a=>a.id==adv.CgId).resourcePath==story.events.Single(e=>e.id==id).cgResourcePath,"capture owns requested event CG");
            }else throw new ArgumentException("Unknown production story view.");
            Debug.Log("PLAN9_STORY_PLAYER_PASS assertions="+acceptanceChecks+" events=25 lover=5 view="+view+" scope=isolated-contract-not-human-journey");
        }
        private void ReadProductionDiagnosticScene(string source,bool failOnce)
        {
            BeginAdv(source,false);AcceptanceCheck(adv!=null && adv.SourceId==source,"production scene opens through normal conditions");
            int steps=0;while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}
            AcceptanceCheck(adv.EndReached && steps<100,"production scene reaches end through normal presentation commands");
            if(failOnce){string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;CompleteAdv();AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot) && formalCampaign.HasPending,"failed production completion leaves original snapshot intact");formalVictoryDiagnosticFailure=false;}
            CompleteAdv();AcceptanceCheck(adv.Completed,"production end is durably committed");CloseAdv();
        }
        private void PrepareProductionEconomyCapture(string view)
        {
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            save.growth.stones=3000;save.growth.kinderPoints=200;save.growth.tickets=new[]{new HeroineTicket{heroineId=combatDefinitions.FormationIds[0],count=1}};
            save.collection.materials=CollectionData().resources.Where(r=>r.kind=="material").Select(r=>new CollectionMaterial{id=r.id,sourceColossusId=r.ownerId,amount=10000}).ToArray();
            save.collection.relics=CollectionData().relics.Select(r=>new CollectionRelic{id=r.id,contentVersion=CollectionData().contentVersion,attackRoll=80,hpRoll=800}).ToArray();save.revision++;
            AcceptanceCheck(acceptanceStore.Save(save),"production economy UI fixture is isolated");BindFormalCampaign(save);InitializeKinder();InitializeEngagement();
            book.RequestSubject(BookBookmark.Heroines,combatDefinitions.FormationIds[0]);book.CompleteTransition();growthScreen=GrowthScreen.Overview;heroineRosterOpen=false;
            if(view=="tree")growthScreen=GrowthScreen.Weapons;
            else if(view=="growth"){}
            else if(view=="relics"){collectionOpen=true;collectionTab=1;}
            else if(view=="engagement")OpenEngagement();
            else if(view.StartsWith("kinder-")){
                kinderGarden=true;string page=view.Substring(7);kinderScreen=page=="draw"?KinderScreen.Draw:page=="rates"?KinderScreen.Rates:page=="exchange"?KinderScreen.Exchange:page=="tickets"?KinderScreen.Tickets:KinderScreen.Entrance;
                if(page=="confirm" || page=="result"){kinderCount=10;kinderScreen=KinderScreen.Draw;ConfirmKinder(KinderOperation.StoneDraw,formalProgression.Snapshot);
                    if(page=="result"){int count=0;var committed=formalProgression.CommitKinder(kinderRequest,kinderBanner,max=>max==10000?(count++==0?0:9999):0,SaveFormalGrowth);AcceptanceCheck(committed==GrowthCommitResult.Committed,"production result uses atomic draw save");kinderReceipt=formalProgression.KinderReceipt(kinderRequest.Id);kinderScreen=KinderScreen.Result;}
                }
            }else throw new ArgumentException("Unknown production economy UI view");
            Debug.Log("PLAN9_ECONOMY_UI_PASS view="+view+" rules="+ProductionEconomyCatalog.Version+" fixture-not-earned-progression");
        }
    }
}
