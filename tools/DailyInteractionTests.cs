using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class DailyInteractionTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var defs=DailyInteractionContent.Definitions;var art=DailyInteractionContent.Presentations;
        check(InteractionTraitCatalog.Ids.Length==27 && InteractionTraitCatalog.Ids.Distinct().Count()==27,"DL-01 exact 27 shared interaction definitions");
        foreach(string category in new[]{"common","trait","pair"})check(defs.Count(d=>d.category==category)==20,"DL-07 twenty authored "+category+" parts");
        foreach(var d in defs){d.Validate();var p=art.Single(a=>a.id==d.presentationId);check(p.seconds>=5 && p.seconds<=20 && !string.IsNullOrEmpty(p.description) && d.scriptId==null,"DL-07 each shared part works without invented dialogue");}
        var person=new ReactionStyleProfile{main="active",sub="cool"};
        check(Enumerable.Range(0,100).Count(n=>ReactionStyleRules.Choose(person,null,max=>n)=="active")==70,"DL-04 exact 70:30 weighting");
        check(ReactionStyleRules.Choose(person,new ReactionStyleProfile{main="reserved"},max=>99)=="reserved","DL-04 form override uses 100 percent when no sub style");
        foreach(var id in ReactionStyleRules.Ids)new ReactionStyleProfile{main=id}.Validate();
        bool bad=false;try{new ReactionStyleProfile{main="missing"}.Validate();}catch(ArgumentException){bad=true;}check(bad,"DL-04 missing main style rejected");
        Func<string,string[],DailyActorContext> actor=(id,tags)=>new DailyActorContext{form=new HeroineCombatDef{id=id,traitId=id+".trait",interactionTraitIds=tags},affectionLevel=10,lover=true};
        var context=new DailyInteractionContext{subject=actor("heroine.a",new[]{"taste.books"}),partner=actor("heroine.b",new[]{"personality.cool"}),garden="garden.test",time="night",weather="rain",interaction="read",participantCount=2,participantIds=new[]{"heroine.a","heroine.b"},extremes=new[]{"extreme.test"},terraformLevels=new Dictionary<string,int>{{"life",4}}};
        var pair=new DailyInteractionDef{id="daily.test.pair",category="pair",presentationId="daily.test.pair",topicTags=new[]{"books"},conditions=new[]{new InteractionCondition{kind="trait",value="taste.books"},new InteractionCondition{kind="trait",actor="partner",value="personality.cool"}}};pair.Validate();
        check(pair.Matches(context) && !pair.Matches(context.Swap()),"DL-05 ordered subject/partner pair");pair.symmetric=true;check(pair.Matches(context.Swap()),"DL-05 symmetric pair matches both orientations");
        var compound=new InteractionCondition{kind="and",items=new[]{new InteractionCondition{kind="affection",minimum=10},new InteractionCondition{kind="or",items=new[]{new InteractionCondition{kind="time",value="night"},new InteractionCondition{kind="weather",value="snow"}}}}};compound.Validate();check(compound.Matches(context),"DL-05 AND/OR condition tree");
        foreach(var c in new[]{new InteractionCondition{kind="lover"},new InteractionCondition{kind="form",value="heroine.a"},new InteractionCondition{kind="garden",value="garden.test"},new InteractionCondition{kind="terraform",value="life",minimum=3},new InteractionCondition{kind="extreme",value="extreme.test"},new InteractionCondition{kind="interaction",value="read"},new InteractionCondition{kind="participant",value="heroine.b"},new InteractionCondition{kind="count",minimum=2,maximum=2}}){c.Validate();check(c.Matches(context),"DL-05 condition "+c.kind);}
        context.subject.affectionLevel=99;check(compound.Matches(context),"DL-05 low-level interactions remain available at high affection");context.subject.affectionLevel=10;
        var history=new DailyInteractionHistory();var common=defs.First(d=>d.category=="common");check(history.Multiplier(common)==1m,"DL-06 unseen content weight");history.Record(common);check(history.Multiplier(common)==.1m,"DL-06 recent same topic has 0.2 x 0.5 weight");
        check(DailyInteractionSelector.Choose(new[]{common},context,history,n=>n-1)==common,"DL-06 sole candidate reusable");
        for(int i=0;i<6;i++)history.Record(new DailyInteractionDef{id="other."+i,topicTags=new[]{"other"}});check(history.Multiplier(common)==1m,"DL-06 recent window expires after five parts");
        check(new[]{"personal","pair","trait","common"}.Select(DailyInteractionSelector.BaseWeight).SequenceEqual(new[]{100,60,35,20}),"DL-06 default category weights");
        check(DailyInteractionSelector.Choose(new[]{common},context,history,n=>0,new[]{common.id})==null,"DL-08 date part exclusion");
        var fallback=new DailyPresentationDef{id="fallback",description="共通表示"};DailyPresentationDef specific=new DailyPresentationDef{id="heroine.a/part"},style=new DailyPresentationDef{id="cool/part"};
        check(DailyPresentationResolver.Resolve("part","heroine.a","cool",new[]{specific,style},fallback ).id==specific.id && DailyPresentationResolver.Resolve("part","heroine.b","cool",new[]{specific,style},fallback ).id==style.id && DailyPresentationResolver.Resolve("part","heroine.b","reserved",Array.Empty<DailyPresentationDef>(),fallback ).id==fallback.id,"DL-07 authored/style/common fallback order");
        int released=0,completed=0;DailyInteractionCompletion notice=null;
        var state=new DailyPresentationState{x=.3f,y=.7f,facing=-1,expression="smile",furniture="bench",distance=.1f};
        var parts=new[]{new DailyPresentationDef{id="part.a",seconds=5,gesture="look",expression=null},new DailyPresentationDef{id="part.b",seconds=5,gesture="sit",requiredFurniture="table",expression=null}};
        var session=new DailyInteractionSession("session.test","garden.test","date","date.walk",new[]{"heroine.a"},parts,state,()=>released++,n=>{completed++;notice=n;});
        for(int i=0;i<5;i++)session.Tick(1,true);check(session.Transitioning && state.x==.3f && state.y==.7f && state.facing==-1 && state.expression=="smile" && state.distance==.1f,"DL-08 position/expression/distance persist across parts");session.Tick(1,true);check(!session.Transitioning && state.furniture=="table","DL-08 one second transition reconciles furniture");
        for(int i=0;i<20;i++)session.Tick(1,true);check(session.Completed && released==1 && completed==1 && notice.GivesAffection,"DL-10 session sends exactly one completion and releases once");
        foreach(string kind in new[]{"social","replay"})check(!new DailyInteractionCompletion{kind=kind}.GivesAffection,"DL-11 zero reward notification for "+kind);
        released=0;completed=0;session=new DailyInteractionSession("session.cancel","garden.test","date","date.walk",new[]{"heroine.a"},parts,new DailyPresentationState(),()=>released++,n=>completed++);session.Cancel();session.Cancel();session.Tick(2,true);check(released==1 && completed==0 && session.Cancelled,"DL-12 cancel releases once and never notifies completion");
        check(DailyDateCatalog.Definitions.Length==5 && DailyDateCatalog.Definitions.All(d=>d.minimumAffection==10),"DL-09 five level-ten dates");
        var timer=Stopwatch.StartNew();for(int i=0;i<256;i++){
            var h=actor("heroine.scale."+i,new[]{InteractionTraitCatalog.Ids[i%27]});var ctx=new DailyInteractionContext{subject=h,time="night",weather="clear"};
            check(DailyInteractionSelector.Choose(defs,ctx,new DailyInteractionHistory(),n=>i%n)!=null,"DL-13 selection for person "+i);
        }timer.Stop();Console.WriteLine("DAILY_256_SELECTION_MS "+timer.Elapsed.TotalMilliseconds.ToString("F1"));
        var options=new JsonSerializerOptions{IncludeFields=true};var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-shangrila.json")),options);
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-shangrila-story-content.json")),options);var home=ProductionStoryCatalog.Home(combat,story);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=1}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion)};
        GardenLifeCatalog.Migrate(save);var life=new GardenLifeRuntime(save,home,GardenLifeCatalog.GardenIds[0]);var agent=life.Agents.First();
        check(life.TryReserveDailySession("session.one",new[]{agent.heroineId}),"DL-12 reserve participant");check(!life.TryReserveDailySession("session.two",new[]{agent.heroineId}),"DL-12 participant exclusivity");float x=agent.x,y=agent.y;for(int i=0;i<100;i++)life.Tick(.25f);check(agent.x==x && agent.y==y && agent.kind=="DailyInteraction","DL-12 selected life AI remains stopped");life.ReleaseDailySession("session.one");check(!life.OwnsDailySession("session.one"),"DL-12 participant resumes");life.TryReserveDailySession("session.stop",new[]{agent.heroineId});life.Stop();check(!life.OwnsDailySession("session.stop"),"DL-12 scene switch releases session ownership");
        foreach(string id in combat.HeroineIds){var p=DailyReactionProfiles.For(combat.PersonId(id));p.Validate();check(p.main!=null,"DL-04 authored main reaction for "+id);}
        AffectionSaveAdapter.Migrate(save,home);
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var journal=new FormalCampaignJournal(save,encode,decode);string form=combat.FormationIds[0];var start=AffectionService.State(journal.Snapshot,home,form);int before=start.level*100+start.exp;
        for(int i=0;i<3;i++){
            var request=new AffectionRequest("daily.reward."+i,journal.Revision,"interaction",new[]{form},"date.walk");
            check(journal.CommitAffection(request,home,s=>false)==GrowthCommitResult.SaveFailed,"DL-10 failed completion save is retryable");
            check(journal.CommitAffection(request,home,s=>true)==GrowthCommitResult.Committed,"DL-10 completion notification uses existing affection journal");
            check(journal.CommitAffection(request,home,s=>throw new Exception())==GrowthCommitResult.AlreadyCommitted,"DL-10 duplicate notification awards nothing");
        }
        var finish=AffectionService.State(journal.Snapshot,home,form);check(finish.level*100+finish.exp-before==9,"DL-10 existing repeated-content rewards remain 5/3/1");
        var warnings=new HeroineCombatDef{id="heroine.warning",interactionTraitIds=new[]{"hair.black","hair.blonde","outfit.school","outfit.swimsuit"}};
        check(InteractionTraitCatalog.Warnings(warnings).Length==2,"DL-01 intentional multiple hair/outfit tags warn without rejection");
        check(!encode(journal.Snapshot).Contains("DailyPresentationState") && !encode(journal.Snapshot).Contains("dailyOwners"),"DL-15 transient session state is not persisted");
        Console.WriteLine("DAILY_INTERACTION_PASS DL-01..13 / 15 (DL-14 requires Player verification)");
    }
}
