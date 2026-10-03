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
        private bool plan8StoryTrial;
        private ColossusCombatDef ActiveColossusDefinition(string id)=>plan8StoryTrial?ColossusCombatCatalog.GetPlan8Trial(id):ColossusCombatCatalog.Get(id);
        private TrialStoryContent plan8Story;
        private string trialPoemChapter;
        private Vector2 trialPoemScroll;
        private void DrawTrialPoemConditions()
        {
            var chapter=StoryData().chapters.Single(c=>TrialStoryCatalog.Id(c.id)==trialPoemChapter);
            var world=formalCampaign.Snapshot.world;
            Label(150,210,1280,55,chapter.title+" ／ 章を開く条件",growthTitleStyle);
            trialPoemScroll=GUI.BeginScrollView(new Rect(150,285,1280,400),trialPoemScroll,new Rect(0,0,1230,chapter.poems.Length*110));
            for(int i=0;i<chapter.poems.Length;i++){
                var poem=chapter.poems[i];string id=chapter.ownerId==StoryData().colossusId?poem.id:TrialStoryCatalog.Id(poem.id);
                string detail=(world.poemIds.Contains(id)?"取得済み：":"未取得：")+poem.text;
                if(poem.sourcePoemId!=null){var source=StoryData().chapters.Take(3).SelectMany(c=>c.poems).Single(p=>p.id==poem.sourcePoemId);detail+="\n緑還竜の歌「"+source.text+"」を、開始編成で聞く。\n"+poem.reason;}
                else detail+="\n緑還竜の完了した行動で、この歌を聞く。";
                Label(10,i*110,1200,100,detail,growthSmallStyle);
            }
            GUI.EndScrollView();
            if(GrowthButton(150,715,1280,65,"章の進捗へ戻る"))trialPoemChapter=null;
        }
        private TrialStoryContent StoryData()
        {
            if(plan8Story==null){
                var source=Resources.Load<TextAsset>("Trial/plan8-story-content");
                if(source==null)throw new ArgumentException("Trial story resource missing.");
                plan8Story=JsonUtility.FromJson<TrialStoryContent>(source.text);plan8Story.Validate(combatDefinitions,WorldCatalog.ColossusIds[0]);
            }
            return plan8Story;
        }
        private bool HasTrialText(string id)=>id!=null && id.StartsWith("trial.plan8.",StringComparison.Ordinal);
        private string OriginalStoryTitle(string id)=>StoryData().chapters.Where(c=>TrialStoryCatalog.Id(c.id)==id).Select(c=>c.title)
            .Concat(StoryData().events.Where(e=>TrialStoryCatalog.Id(e.id)==id).Select(e=>e.title)).FirstOrDefault()??"物語";
        private void PreparePlan8StoryCapture(string[] args)
        {
            if(!formalDiagnostic || capturePath==null)throw new InvalidOperationException("Story acceptance requires capture isolation.");
            Func<string,string> option=key=>{int at=Array.IndexOf(args,key);if(at<0 || at+1>=args.Length || args.Count(v=>v==key)!=1)throw new ArgumentException("Missing story option: "+key);return args[at+1];};
            bool resume=args.Contains("-plan8StoryResume");
            var boundary=resume?TrialDiagnosticBoundary.OpenExisting(option("-plan8RepositoryRoot"),Application.persistentDataPath,option("-plan8StoryRunId")):
                TrialDiagnosticBoundary.Create(option("-plan8RepositoryRoot"),Application.persistentDataPath,option("-plan8StoryRunId"));
            if(!resume)Directory.CreateDirectory(boundary.DirectoryPath);
            EnterPlan8StoryTrial(boundary.SavePath);
            if(!plan8StoryTrial || acceptanceStore==null)throw new InvalidOperationException("Authored trial entry failed.");
            acceptanceChecks=0;var story=StoryData();string chapter=TrialStoryCatalog.Id(story.chapters[0].id);
            if(resume){
                AcceptanceCheck(formalCampaign.Snapshot.home.readEventIds.Contains(TrialStoryCatalog.Id(story.events[0].id)),"original affection event survives separate process restart");
                AcceptanceCheck(formalCampaign.Snapshot.world.readStoryIds.Contains(chapter),"original chapter read survives separate process restart");
                AcceptanceCheck(formalCampaign.Snapshot.world.readStoryIds.Count(HasTrialText)==8,"all eight original read chapters survive restart");
                AcceptanceCheck(!formalCampaign.Snapshot.home.loverHeroineIds.Any(),"original affection event does not establish lover");
                book.ChangeBookmark(BookBookmark.Stories);encounter=null;result=null;
                string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);BeginAdv(chapter,true);
                AcceptanceCheck(adv!=null && adv.Replay,"original chapter replay opens read only");
                AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"replay entry writes no progress");
            }else{
                int battles=0;
                while(battles<128 && formalCampaign.Snapshot.world.unlockedStoryIds.Count(HasTrialText)<8){PlayedAcceptanceEnding(BattleEndReason.Victory,19000+battles++,2);}
                AcceptanceCheck(battles<128 && formalCampaign.Snapshot.world.unlockedStoryIds.Count(HasTrialText)==8,"actual completed enemy singing unlocks eight authored chapters");
                AcceptanceCheck(formalCampaign.Snapshot.world.poemIds.Count(p=>CollectionData().poems.Any(c=>c.id==p))==54,"unmade heroine chapters never acquire fixture correspondences");
                encounter=null;result=null;book.ChangeBookmark(BookBookmark.Stories);BeginAdv(chapter,false);
                AcceptanceCheck(adv!=null && !adv.Completed,"original chapter opens from real acquired poems");
                AdvanceAdv();AdvanceAdv();string next=adv.LineId;CloseAdv();ReloadAcceptance();BeginAdv(chapter,false);
                AcceptanceCheck(adv.LineId==next && adv.NewlyRead.Count==0,"restart resumes first unread line without duplicate read receipts");
                int steps=0;while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}
                AcceptanceCheck(adv.EndReached,"original text reaches explicit end");
                string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);formalVictoryDiagnosticFailure=true;CompleteAdv();
                AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot) && formalCampaign.HasPending,"failed original read completion is atomic");
                formalVictoryDiagnosticFailure=false;CompleteAdv();AcceptanceCheck(adv.Completed,"same original completion retries successfully");CloseAdv();ReloadAcceptance();
                foreach(var source in story.chapters.Skip(1)){
                    string id=TrialStoryCatalog.Id(source.id);BeginAdv(id,false);steps=0;
                    AcceptanceCheck(adv!=null && adv.SourceId==id,"each original chapter opens its own script");
                    while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}
                    CompleteAdv();AcceptanceCheck(adv.Completed,"each original chapter completes with durable read lines");CloseAdv();ReloadAcceptance();
                }
                string ev=TrialStoryCatalog.Id(story.events[0].id);BeginAdv(ev,false);
                AcceptanceCheck(adv==null && !formalCampaign.Snapshot.home.unlockedEventIds.Contains(ev),"original event cannot be read before costed interaction");
                var talk=HomeData().interactions.Single(t=>t.heroineId=="heroine.slayer");
                ProposeHome(new HomeOperation("talk",talk.heroineId));ConfirmHome();
                AcceptanceCheck(formalCampaign.Snapshot.home.unlockedEventIds.Contains(ev),"normal costed interaction unlocks original event");
                BeginAdv(ev,false);AdvanceAdv();AdvanceAdv();next=adv.LineId;CloseAdv();ReloadAcceptance();BeginAdv(ev,false);
                AcceptanceCheck(adv.LineId==next && adv.NewlyRead.Count==0,"original event resumes first unread after restart");
                steps=0;while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}CompleteAdv();CloseAdv();ReloadAcceptance();
                AcceptanceCheck(formalCampaign.Snapshot.home.readEventIds.Contains(ev) && !formalCampaign.Snapshot.home.loverHeroineIds.Any(),"original event completes without lover state");
                before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);BeginAdv(ev,true);steps=0;
                while(!adv.EndReached && steps++<100){AdvanceAdv();AdvanceAdv();}CompleteAdv();CloseAdv();
                AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"original event replay changes no affection, rewards or read state");
                BeginAdv(chapter,true);
                Debug.Log("PLAN8_STORY_ACTUAL_COLLECTION battles="+battles+" scope=automated-contract-not-human-timing");
            }
            Debug.Log("PLAN8_STORY_PLAYER_PASS assertions="+acceptanceChecks+" resume="+resume+" originalChapters=8 poems=54 lover=False");
        }
        private void EnterPlan8StoryTrial(string diagnosticPath=null)
        {
            if(!BookInputAllowed || homeTrial || plan8StoryTrial)return;
            var original=formalCampaign.Snapshot;
            // A sibling directory keeps the authored trial outside the ordinary save root.
            string path=diagnosticPath??Path.Combine(Application.persistentDataPath+"-plan8","original-story-v1.json");
            var store=new FormalCampaignStore(path,UnityFormalCampaignJson.Encode,UnityFormalCampaignJson.Decode,UnityFormalCampaignJson.DecodeHeader);
            try{
                var load=store.Load(out var save);
                if(load!=FormalLoadResult.Loaded && load!=FormalLoadResult.Missing){status="試遊保存を読めません。元ファイルを保持しています。";return;}
                var home=TrialStoryCatalog.Home(combatDefinitions,StoryData());var collection=TrialStoryCatalog.Collection(combatDefinitions,StoryData());
                if(load==FormalLoadResult.Missing){
                    save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion}};
                    if(!store.Save(save)){status="試遊の初期保存に失敗しました。";return;}
                }
                save.collection.ValidateContent(collection);save.home.ValidateContent(home,save);
                homeOriginal=original;homeData=home;collectionCatalog=collection;acceptanceStore=store;
                plan8StoryTrial=true;homeTrial=true;formalDiagnostic=true;BindFormalCampaign(save);
                title=false;encounter=null;ResetGardenMenu();book.ChangeBookmark(BookBookmark.Colossi);
                status="オリジナル試遊：戦闘で詩を聞き、物語の章を開きましょう。進行は専用保存です。";
            }catch(Exception e){status="オリジナル試遊を開始できません。保存を保持しています。";Debug.LogException(e);}
        }
    }
}
