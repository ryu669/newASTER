using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool scaleUiActive,scaleUiComplete;
        private int scalePeople,scaleForms;
        private string scaleLastId,scaleLastQuery;
        private static string ScaleReplace(string value,string from,string to)=>value==null?null:value==from?to:value.EndsWith("."+from,StringComparison.Ordinal)?value.Substring(0,value.Length-from.Length)+to:value.Replace(from+".",to+".");
        private static T ScaleCopy<T>(T value){var copy=Activator.CreateInstance<T>();foreach(var field in typeof(T).GetFields(BindingFlags.Public|BindingFlags.Instance))field.SetValue(copy,field.GetValue(value));return copy;}
        private static object ScaleClone(object value,string from,string to)
        {
            if(value==null)return null;var type=value.GetType();if(type==typeof(string))return ScaleReplace((string)value,from,to);if(type.IsPrimitive || type.IsEnum || type==typeof(decimal))return value;
            if(type.IsArray){var old=(Array)value;var copy=Array.CreateInstance(type.GetElementType(),old.Length);for(int i=0;i<old.Length;i++)copy.SetValue(ScaleClone(old.GetValue(i),from,to),i);return copy;}
            var result=Activator.CreateInstance(type);foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))field.SetValue(result,ScaleClone(field.GetValue(value),from,to));return result;
        }
        // These are isolated synthetic catalog rows; referenced artwork remains shared test art.
        private static void ExpandScaleArrays(object target,object source,string from,string to)
        {
            foreach(var field in source.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)){
                if(!field.FieldType.IsArray || new[]{"formation","oopartDefs","gardens","furniture","actorSlots"}.Contains(field.Name))continue;
                var values=field.GetValue(source) as Array;if(values==null)continue;
                var type=field.FieldType.GetElementType();var additions=new List<object>();
                foreach(var row in values){
                    if(row==null)continue;
                    if(type==typeof(string)){string old=(string)row,next=ScaleReplace(old,from,to);if(next!=old)additions.Add(next);continue;}
                    var key=type.GetField("id")??type.GetField("heroineId")??type.GetField("subjectId");
                    if(key==null || key.FieldType!=typeof(string))continue;
                    string identity=(string)key.GetValue(row);if(ScaleReplace(identity,from,to)==identity)continue;
                    additions.Add(ScaleClone(row,from,to));
                }
                if(additions.Count==0)continue;
                var current=(Array)field.GetValue(target);var combined=Array.CreateInstance(type,current.Length+additions.Count);Array.Copy(current,combined,current.Length);for(int i=0;i<additions.Count;i++)combined.SetValue(additions[i],current.Length+i);field.SetValue(target,combined);
            }
        }
        private void PrepareScaleUiCapture(string[] args)
        {
            if(!args.Contains("-validateScaleUi"))return;
            AcceptanceCheck(formalDiagnostic && capturePath!=null && plan10UiCapture,"Scale UI fixture is diagnostic only");scaleUiActive=true;
            var sourceCombat=ScaleCopy(combatDefinitions);var sourceHome=ScaleCopy(HomeData());var sourceCollection=ScaleCopy(CollectionData());var sourceStory=ScaleCopy(ProductionStoryData());
            HeroFraming("heroine.slayer");var sourcePortrait=ScaleCopy(portraitFraming);
            var save=formalCampaign.Snapshot;int existingPeople=combatDefinitions.HeroineIds.Select(combatDefinitions.PersonId).Distinct().Count();
            int target=200;int targetAt=Array.IndexOf(args,"-scaleTargetPeople");
            if(targetAt>=0 && (targetAt+1>=args.Length || !int.TryParse(args[targetAt+1],out target) || target<1 || target>1000))throw new ArgumentException("Invalid scale target (1..1000).");
            scalePeople=Math.Max(target,existingPeople+1);int additions=scalePeople-existingPeople;scaleForms=combatDefinitions.HeroineIds.Length+additions;
            scaleLastId="heroine.scale."+additions.ToString("D3");scaleLastQuery="検証"+new string(additions.ToString("D3").Select(c=>(char)(c+0xFEE0)).ToArray());
            foreach(string id in combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)))save.growth.heroines=save.growth.heroines.Concat(new[]{new FormalHeroineGrowth{heroineId=id}}).ToArray();
            for(int n=1;n<=additions;n++){
                string id="heroine.scale."+n.ToString("D3");const string from="heroine.slayer";
                ExpandScaleArrays(combatDefinitions,sourceCombat,from,id);ExpandScaleArrays(homeData,sourceHome,from,id);ExpandScaleArrays(collectionCatalog,sourceCollection,from,id);ExpandScaleArrays(productionStory,sourceStory,from,id);ExpandScaleArrays(portraitFraming,sourcePortrait,from,id);
                var hero=combatDefinitions.Hero(id);hero.name="検証"+n.ToString("D3")+(n%10==0?"・長い名前のヒロイン":"");hero.personId=id;
                foreach(var subject in homeData.subjects.Where(s=>s.subjectId==id))subject.pageOrder=1000+n;
                save.growth.heroines=save.growth.heroines.Concat(new[]{new FormalHeroineGrowth{heroineId=id,level=1+n%50}}).ToArray();
            }
            combatDefinitions.Validate();portraitFraming.Validate();heroineRoster=null;save.revision++;
            save.growth.stones=3000;save.growth.kinderPoints=200;save.growth.tickets=new[]{new HeroineTicket{heroineId=scaleLastId,count=1}};
            BindFormalCampaign(save);AcceptanceCheck(acceptanceStore.Save(formalCampaign.Snapshot),"Persist isolated 200-person fixture before testing transactions");InitializeKinder();AcceptanceCheck(HeroineIndex().PersonCount==scalePeople && GrowthSnapshot().heroines.Length==scaleForms,"Dynamic person and form counts match the expanded catalog");
            var all=HeroineIndex().Search("",ownedOnly:true);AcceptanceCheck(all.Length==scaleForms && all.Distinct().Count()==scaleForms,"Every form selectable without truncation");
            AcceptanceCheck(HeroineIndex().Search(scaleLastQuery,ownedOnly:true).Single()==scaleLastId,"Full-width query finds last synthetic person");
            kinderQuery="";AcceptanceCheck(KinderCatalogIds(KinderScreen.Tickets,GrowthSnapshot()).SequenceEqual(new[]{scaleLastId}),"Ticket selection searches actual owned tickets across 203 forms");
            var timer=System.Diagnostics.Stopwatch.StartNew();combatDefinitions.Validate();Debug.Log("SCALE_PROFILE combat="+timer.ElapsedMilliseconds);timer.Restart();HomeData().Validate();Debug.Log("SCALE_PROFILE home="+timer.ElapsedMilliseconds);timer.Restart();CollectionData().Validate();Debug.Log("SCALE_PROFILE collection="+timer.ElapsedMilliseconds);timer.Restart();OopartSnapshot();Debug.Log("SCALE_PROFILE oopart="+timer.ElapsedMilliseconds);
            StartCoroutine(CaptureScaleUiScreens());
        }
        private void VerifyScaleUiOperations()
        {
            string id=scaleLastId;
            encounter=null;storyPickerOpen=false;formationJobPicker=false;affectionPanel=false;kinderGarden=false;
            book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();formationOpen=true;
            ProposeHome(new HomeOperation("formation",id,"0"));ConfirmHome();
            AcceptanceCheck(homeRequest==null && CurrentFormation()[0]==id,"Last person can be committed to formation: "+homeError);
            AcceptanceCheck(acceptanceStore.Load(out var saved)==FormalLoadResult.Loaded && saved.home.formationIds[0]==id,"Last person formation reloads from isolated save");
            kinderScreen=KinderScreen.Exchange;ConfirmKinder(KinderOperation.Exchange,GrowthSnapshot(),id);
            AcceptanceCheck(kinderRequest.HeroineId==id,"Exchange confirmation retains selected identity");
            AcceptanceCheck(formalProgression.CommitKinder(kinderRequest,kinderBanner,KinderRandom.NextBelow,SaveFormalGrowth)!=GrowthCommitResult.SaveFailed,"Last person exchange saves");
            kinderRequest=null;kinderScreen=KinderScreen.Tickets;
            AcceptanceCheck(GrowthSnapshot().tickets.Single(t=>t.heroineId==id).count==2,"Exchange updates cached ticket count");
            int fragments=HeroineIndex().Growth(id).fragments;
            ConfirmKinder(KinderOperation.TicketDraw,GrowthSnapshot(),id);
            AcceptanceCheck(formalProgression.CommitKinder(kinderRequest,kinderBanner,KinderRandom.NextBelow,SaveFormalGrowth)!=GrowthCommitResult.SaveFailed,"Last person ticket saves");
            kinderRequest=null;
            AcceptanceCheck(HeroineIndex().Growth(id).fragments==fragments+100 && GrowthSnapshot().tickets.Single(t=>t.heroineId==id).count==1,"Ticket updates correct person and refreshes UI index");
            AcceptanceCheck(acceptanceStore.Load(out saved)==FormalLoadResult.Loaded && saved.growth.heroines.Single(h=>h.heroineId==id).fragments==fragments+100,"Ticket result reloads from isolated save");
            Debug.Log("SCALE_UI_OPERATIONS_PASS formation / exchange / ticket / save reload / cache refresh physicalInput=0");
        }
        private IEnumerator CaptureScaleUiScreens()
        {
            string folder=Path.GetDirectoryName(capturePath);var measurements=new List<string>();
            foreach(string view in new[]{"roster-last","roster-search","roster-empty","formation-last","formation-search","formation-jobs","formation-empty","rates-last","rates-search","exchange","tickets","tickets-empty","garden-last","garden-search","detail","tree","level","awakening","duplicate","skill","affection","stories","stories-select","stories-last","stories-empty","formation-overview","formation-member","targets","items","materials","newworld","worlds","colossi","hunt","battle"}){
                var clock=System.Diagnostics.Stopwatch.StartNew();title=false;encounter=null;formationOpen=false;kinderGarden=false;collectionOpen=false;affectionPanel=false;affectionShop=false;growthScreen=GrowthScreen.Overview;gardenLifePanel=null;heroineRosterOpen=true;
                heroineQuery="";heroineJobFilter=0;affectionRosterFilter=0;heroinePage=0;kinderQuery="";kinderPage=0;
                book.SetResults(BookBookmark.Heroines,GrowthSnapshot().heroines.Select(h=>h.heroineId));book.ChangeBookmark(BookBookmark.Heroines);book.CompleteTransition();
                if(view.StartsWith("roster")){heroinePage=view=="roster-last"?(scaleForms-1)/HeroinePageSize:0;heroineQuery=view=="roster-search"?scaleLastQuery:view=="roster-empty"?"見つからない名前":"";}
                else if(view.StartsWith("formation")){book.ChangeBookmark(BookBookmark.Formation);book.CompleteTransition();formationOpen=true;OpenFormationHeroReplacement(0);formationSelectionPagePending=false;formationPage=(scaleForms-1)/8;formationCandidateHero=scaleLastId;formationJobPicker=view=="formation-jobs";formationQuery=view=="formation-search"?scaleLastQuery:view=="formation-empty"?"見つからない名前":"";if(view=="formation-overview")formationLayer=0;else if(view=="formation-member")formationLayer=1;}
                else if(view.StartsWith("rates") || view=="targets" || view=="exchange" || view.StartsWith("tickets")){
                    book.ChangeBookmark(BookBookmark.Summoning);book.CompleteTransition();kinderGarden=true;kinderScreen=view=="targets"?KinderScreen.Targets:view.StartsWith("rates")?KinderScreen.Rates:view=="exchange"?KinderScreen.Exchange:KinderScreen.Tickets;
                    kinderQuery=view=="rates-search" || view=="exchange"?scaleLastQuery:view=="tickets-empty"?"見つからない名前":"";kinderPage=view=="rates-last"?(scaleForms-1)/5:0;
                }else if(view.StartsWith("garden")){
                    gardenLifeCapture=true;book.ChangeBookmark(BookBookmark.Gardens);book.CompleteTransition();gardenLifePanel="人物";gardenLifeQuery=view=="garden-search"?scaleLastQuery:"";gardenLifeResidentFilter="全員";gardenLifeScroll=new Vector2(0,view=="garden-last"?scaleForms*55:0);StartLifeScene();
                }else if(view.StartsWith("stories")){book.ChangeBookmark(BookBookmark.Stories);book.CompleteTransition();storyPickerOpen=view!="stories";storyPickerQuery=view=="stories-select"?scaleLastQuery:view=="stories-empty"?"見つからない名前":"";storyPickerPage=view=="stories-last"?int.MaxValue:0;}
                else if(new[]{"items","materials","newworld","worlds","colossi","hunt","battle"}.Contains(view)){
                    book.ChangeBookmark(view=="items" || view=="materials"?BookBookmark.Items:view=="newworld"?BookBookmark.NewWorld:view=="worlds"?BookBookmark.PossibleWorlds:view=="hunt"?BookBookmark.RelicHunt:BookBookmark.Colossi);book.CompleteTransition();collectionTab=view=="materials"?2:1;
                    if(view=="battle")StartBattle(WorldCatalog.Colossi[0].Id);
                }
                else{heroineRosterOpen=false;book.RequestSubject(BookBookmark.Heroines,"heroine.slayer");book.CompleteTransition();if(view=="tree")growthScreen=GrowthScreen.Weapons;else if(view=="level")GrowthSelect(GrowthScreen.Level,HeroineIndex().Growth("heroine.slayer"));else if(view=="awakening")GrowthSelect(GrowthScreen.Awakening,HeroineIndex().Growth("heroine.slayer"));else if(view=="duplicate")GrowthSelect(GrowthScreen.Duplicate,HeroineIndex().Growth("heroine.slayer"));else if(view=="skill")GrowthSelect(GrowthScreen.Skill,HeroineIndex().Growth("heroine.slayer"));if(view=="affection"){affectionForm="heroine.slayer";affectionPanel=true;}}
                yield return null;yield return new WaitForEndOfFrame();
                while(heroinePortraitLoads>0 || heroinePortraitRequests.Count>0)yield return null;
                yield return new WaitForEndOfFrame();clock.Stop();
                AcceptanceCheck(heroinePortraits.Count<=24,"Scale screen portrait cache bounded");
                var frames=new List<double>();for(int i=0;i<30;i++){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
                double p95=frames.OrderBy(v=>v).ElementAt(28);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,view+".png"));measurements.Add(view+" readyMs="+clock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" p95FrameMs="+p95.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" maxFrameMs="+frames.Max().ToString("F3",System.Globalization.CultureInfo.InvariantCulture));

                yield return new WaitForSecondsRealtime(.25f);
            }
            File.WriteAllLines(Path.Combine(folder,"scale-ui-times.txt"),measurements);VerifyScaleUiOperations();Debug.Log("SCALE_UI_PLAYER_PASS persons="+scalePeople+" forms="+scaleForms+" screens=35 sharedFixtureArt=true");scaleUiComplete=true;
        }
    }
}
