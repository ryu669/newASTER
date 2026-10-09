using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool heroineRosterOpen=true;
        private bool rosterFilterOpen,rosterFilterJob;
        private string heroineQuery="";
        private int heroinePage,heroineJobFilter,selectedTrait=-1,selectedSkillSlot;
        private const int HeroinePageSize=12;
        private HeroineRoster heroineRoster;
        private HeroineRosterEntry[] bookHeroineResults;
        private string bookHeroineResultKey;
        private GUIStyle sanctuaryHeading,sanctuaryBody,sanctuarySmall;
        private BoundedCache<string,Texture2D> heroinePortraits;
        private bool heroineAssetCleanupPending;
        private void EnsureHeroineImageCaches()
        {
            if(heroinePortraits==null)heroinePortraits=new BoundedCache<string,Texture2D>(24,ScheduleHeroineAssetCleanup);
            if(sanctuaryTrees==null)sanctuaryTrees=new BoundedCache<string,Texture2D>(3,ScheduleHeroineAssetCleanup);
        }
        private void ScheduleHeroineAssetCleanup()
        {
            if(heroineAssetCleanupPending)return;
            heroineAssetCleanupPending=true;StartCoroutine(ReleaseUnusedHeroineImages());
        }
        private System.Collections.IEnumerator ReleaseUnusedHeroineImages()
        {
            yield return new WaitForEndOfFrame();
            yield return Resources.UnloadUnusedAssets();
            heroineAssetCleanupPending=false;
        }
        private readonly Color parchment=new Color(.94f,.89f,.77f),paperInk=new Color(.13f,.18f,.22f);
        private HeroinePortraitCatalog portraitFraming;
        private HeroinePortraitDef HeroFraming(string id)
        {
            if(quality119FixtureFraming!=null && quality119FixtureFraming.TryGetValue(id,out var fixture))return fixture;
            if(portraitFraming==null){
                var source=Resources.Load<TextAsset>("UI/heroine-portrait-framing");
                if(source==null)throw new InvalidOperationException("Missing heroine portrait framing catalog.");
                portraitFraming=JsonUtility.FromJson<HeroinePortraitCatalog>(source.text);
                portraitFraming.Validate();
            }
            return portraitFraming.Entry(id);
        }
        private Texture2D HeroPortrait(string id)
        {
            EnsureHeroineImageCaches();
            string key=id;
            if(!heroinePortraits.TryGetValue(key,out var texture)){
                double started=quality119Started?MeasurementClock:0;
                var framing=HeroFraming(id);
                string path=framing.resourcePath;
                texture=quality119FixtureFraming!=null && quality119FixtureFraming.ContainsKey(id)?quality119FixtureBundle.LoadAsset<Texture2D>(path):path==null?null:Resources.Load<Texture2D>(path);
                {if(texture==null)throw new InvalidOperationException("Missing selection portrait: "+id);portraitFraming.ValidateSource(framing,texture.width,texture.height);}
                heroinePortraits[key]=texture;
                if(quality119Started)quality119MaxPortraitLoadCpuMs=Math.Max(quality119MaxPortraitLoadCpuMs,(MeasurementClock-started)*1000);
            }
            return texture;
        }
        // Spread new visible portraits across frames; cap the queue and drop stale pages.
        private readonly Dictionary<string,int> heroinePortraitRequests=new Dictionary<string,int>();
        private bool heroinePortraitWorker;
        private Texture2D RequestHeroPortrait(string id)
        {
            EnsureHeroineImageCaches();
            if(heroinePortraits.TryGetValue(id,out var texture))return texture;
            if(heroinePortraitRequests.ContainsKey(id))heroinePortraitRequests[id]=Time.frameCount;
            else if(heroinePortraitRequests.Count<HeroinePageSize)heroinePortraitRequests.Add(id,Time.frameCount);
            if(!heroinePortraitWorker && heroinePortraitRequests.Count>0){heroinePortraitWorker=true;StartCoroutine(LoadQueuedHeroPortraits());}
            return null;
        }
        private System.Collections.IEnumerator LoadQueuedHeroPortraits()
        {
            try{
                while(heroinePortraitRequests.Count>0){
                    yield return null;
                    double batchStarted=MeasurementClock;
                    for(int i=0;i<2 && heroinePortraitRequests.Count>0;i++){
                        var next=heroinePortraitRequests.First();heroinePortraitRequests.Remove(next.Key);
                        if(Time.frameCount-next.Value<=2)HeroPortrait(next.Key);
                        // A single resource load is indivisible; stop scheduling more work once over budget.
                        if((MeasurementClock-batchStarted)*1000>=4)break;
                    }
                }
            }finally{heroinePortraitWorker=false;}
        }
        // Face anchors are authored against the unmodified source PNG, using top-left coordinates.
        // Clip the bust to its panel; use one scale for both axes to preserve the original proportions.
        private void DrawHeroPortrait(Rect panel,string id)
        {
            var texture=RequestHeroPortrait(id);var framing=HeroFraming(id);
            if(texture==null || framing==null){DrawSanctuaryIcon(new Rect(panel.center.x-24,panel.center.y-24,48,48),"star",gold);return;}
            float scale=Mathf.Min(HeroinePortraitCatalog.MaximumDisplayScale,panel.height*portraitFraming.faceHeightRatio/(texture.height*framing.faceHeight));
            float w=texture.width*scale,h=texture.height*scale;
            GUI.BeginGroup(panel);
            GUI.DrawTexture(new Rect(panel.width*.5f-framing.faceCenterX*w,panel.height*portraitFraming.faceCenterYRatio-framing.faceCenterY*h,w,h),texture,ScaleMode.ScaleToFit,true);
            GUI.EndGroup();
        }
        private void SanctuaryStyles()
        {
            GrowthStyles();if(sanctuaryHeading!=null)return;
            sanctuaryHeading=new GUIStyle(growthTitleStyle){fontSize=27};sanctuaryHeading.normal.textColor=paperInk;
            sanctuaryBody=new GUIStyle(growthTextStyle){fontSize=ArtSampleSettings.LargeText?22:19,wordWrap=true};sanctuaryBody.normal.textColor=paperInk;
            sanctuarySmall=new GUIStyle(growthSmallStyle){fontSize=ArtSampleSettings.LargeText?19:17,wordWrap=true};sanctuarySmall.normal.textColor=new Color(.32f,.37f,.40f);
        }
        private void SanctuaryHeader(string titleText,string subtitle)
        {
            GrowthFill(0,0,1600,900,ink);GrowthLine(55,79,1545,79,gold);GrowthDiamond(800,79,8);

        }
        private void DrawGrowthExperience()
        {
            SanctuaryStyles();
            if(formationOpen){OpenFormationPage();return;}
            if(heroineRosterOpen){DrawHeroineRoster();return;}
            if(!book.HasSubject){heroineRosterOpen=true;DrawHeroineRoster();return;}
            if(growthScreen==GrowthScreen.Overview || growthScreen==GrowthScreen.Information){DrawHeroineDetail();return;}
            if(growthScreen==GrowthScreen.Weapons && ProductionStoryActive){DrawSanctuaryWeaponTree(book.SubjectId);return;}
            if(growthScreen==GrowthScreen.Skill){DrawHeroineSkillUpgrade();return;}
            DrawLegacyGrowthExperience();
        }
        private void DrawHeroineRoster()
        {
            bool previousEnabled=GUI.enabled;if(expansionRecruitmentOpen || rosterFilterOpen)GUI.enabled=false;
            SanctuaryHeader("誓女の星図","名前と顔から、会いたい誓女を選ぶ");
            heroineRoster=heroineRoster??HeroineRosterCatalog.InitialFive(combatDefinitions);
            Label(278,111,110,38,"名前検索",growthSmallStyle);
            string query=ImageUiSkin.TextField(new Rect(383,108,455,48),heroineQuery,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=23,padding=new RectOffset(14,14,10,8)});
            if(query!=heroineQuery){heroineQuery=query;heroinePage=0;}
            if(GrowthButton(853,108,95,48,"クリア",heroineQuery.Length>0)){heroineQuery="";heroinePage=0;}
            var jobs=combatDefinitions.jobs.Select(j=>j.id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            heroineJobFilter=Mathf.Clamp(heroineJobFilter,0,jobs.Length);affectionRosterFilter=Mathf.Clamp(affectionRosterFilter,0,4);
            if(GrowthButton(975,108,570,48,"ジョブ  ／  "+(heroineJobFilter==0?"すべて":HeroineIdentityCatalog.JobName(jobs[heroineJobFilter-1]))+"  ›",BookInputAllowed)){rosterFilterOpen=true;rosterFilterJob=true;}
            heroineJobFilter=Mathf.Clamp(heroineJobFilter,0,jobs.Length);affectionRosterFilter=Mathf.Clamp(affectionRosterFilter,0,4);
            if(GrowthButton(1060,167,485,40,"交流で絞り込む："+new[]{"全員","恋人","未読イベント","Lv10以上","Lv20以上"}[affectionRosterFilter],BookInputAllowed)){rosterFilterOpen=true;rosterFilterJob=false;}
            heroineJobFilter=Mathf.Clamp(heroineJobFilter,0,jobs.Length);affectionRosterFilter=Mathf.Clamp(affectionRosterFilter,0,4);var snapshot=LifeSnapshot().growth;string resultKey=heroineQuery+"|"+heroineJobFilter+"|"+affectionRosterFilter+"|"+formalCampaign.Revision;if(bookHeroineResults==null || bookHeroineResultKey!=resultKey){bookHeroineResults=heroineRoster.Search(heroineQuery,heroineJobFilter==0?null:jobs[heroineJobFilter-1],snapshot.heroines.Select(h=>h.heroineId)).Where(e=>AffectionRosterMatch(e.id)).ToArray();bookHeroineResultKey=resultKey;}var entries=bookHeroineResults;
            var saved=book.PageState;var activeFilters=new[]{heroineJobFilter.ToString(),affectionRosterFilter.ToString()};if(saved.searchQuery!=heroineQuery || !(saved.filterIds??Array.Empty<string>()).SequenceEqual(activeFilters)){saved.searchQuery=heroineQuery;saved.filterIds=activeFilters;bookNavigationDirty=true;}book.SetResults(BookBookmark.Heroines,entries.Select(e=>e.id));
            int pages=Math.Max(1,(entries.Length+HeroinePageSize-1)/HeroinePageSize);heroinePage=Mathf.Clamp(heroinePage,0,pages-1);
            int people=snapshot.heroines.Select(h=>combatDefinitions.PersonId(h.heroineId)).Distinct().Count();
            Label(60,177,900,35,people==snapshot.heroines.Length?$"所持 {people}人  ／  表示 {entries.Length}人":$"所持 {people}人・{snapshot.heroines.Length}形態  ／  表示 {entries.Length}形態",growthSmallStyle);
            var page=entries.Skip(heroinePage*HeroinePageSize).Take(HeroinePageSize).ToArray();
            for(int i=0;i<page.Length;i++){
                var entry=page[i];var h=snapshot.heroines.Single(g=>g.heroineId==entry.id);float x=62+(i%4)*374,y=232+(i/4)*183;
                var rect=new Rect(x,y,352,166);bool hover=rect.Contains(Event.current.mousePosition);
                GrowthFrame(x,y,352,166);GrowthFill(x+3,y+3,117,160,hover?new Color(.18f,.28f,.31f):new Color(.10f,.18f,.23f));
                DrawHeroPortrait(new Rect(x+5,y+6,111,154),entry.id);
                var nameStyle=new GUIStyle(growthTextStyle){fontSize=24};while(nameStyle.fontSize>14 && nameStyle.CalcSize(new GUIContent(entry.name)).x>212)nameStyle.fontSize--;
                Label(x+134,y+23,212,41,entry.name,nameStyle);
                Label(x+134,y+75,207,31,HeroineIdentityCatalog.JobName(entry.jobId),growthSmallStyle,gold);
                Label(x+134,y+119,204,33,"Lv."+h.level+"   ／   ★6",growthSmallStyle);
                if(hover)GrowthLine(x+124,y+156,x+338,y+156,gold,2);
                if(ImageUiSkin.Button(rect,"",GUIStyle.none) && BookInputAllowed){if(book.SubjectId!=entry.id)book.RequestSubject(BookBookmark.Heroines,entry.id);heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;selectedTrait=-1;selectedNode=null;bookTransitionElapsed=0;PlayProductionUiSound("決定");}
            }
            if(page.Length==0){GrowthDiamond(800,454,50);Label(440,530,800,50,"該当なし",growthTextStyle);}
            if(GrowthButton(62,808,210,52,"‹ 前の12人",heroinePage>0 && BookInputAllowed))heroinePage--;

            Label(665,818,280,40,$"{heroinePage+1} / {pages} ページ",growthTextStyle);
            if(GrowthButton(1320,808,225,52,"次の12人 ›",heroinePage+1<pages && BookInputAllowed))heroinePage++;
            GUI.enabled=previousEnabled;if(rosterFilterOpen)DrawRosterFilter(jobs);DrawAnnihilatorRecruitmentDialog();
        }
        private void SelectRosterFilter(int index)
        {
            if(rosterFilterJob)heroineJobFilter=index;else affectionRosterFilter=index;
            heroinePage=0;rosterFilterOpen=false;
        }
        private void DrawRosterFilter(string[] jobs)
        {
            drawingModal=true;GrowthFill(0,0,1600,900,new Color(0,0,0,.78f));GrowthFill(340,150,920,610,ink);GrowthFrame(340,150,920,610);
            Label(385,185,720,50,rosterFilterJob?"ジョブで絞り込む":"交流で絞り込む",growthTitleStyle,gold);
            if(GrowthButton(1150,185,65,48,"×")){rosterFilterOpen=false;return;}
            var labels=rosterFilterJob?new[]{"すべて"}.Concat(jobs.Select(HeroineIdentityCatalog.JobName)).ToArray():new[]{"全員","恋人","未読イベント","Lv10以上","Lv20以上"};
            for(int i=0;i<labels.Length;i++){
                bool selected=i==(rosterFilterJob?heroineJobFilter:affectionRosterFilter);
                if(GrowthButton(385+i%3*280,270+i/3*78,260,62,(selected?"✓ ":"")+labels[i]))SelectRosterFilter(i);
            }
            if(GrowthButton(905,690,310,48,"キャンセル"))rosterFilterOpen=false;
        }
        private PlayableBattle HeroinePreview(FormalGrowthSave growth=null,string heroId=null)=>new PlayableBattle(1,campaign.Playable,combatDefinitions:combatDefinitions.WithFormation(PreviewFormation(heroId)),formalGrowth:growth??formalProgression.Snapshot,homeProgress:formalCampaign.Snapshot.home,homeCatalog:HomeData(),collectionGrowth:formalCampaign.Snapshot.collection,relicCatalog:CollectionData());
        private SkillCombatDef DisplayHeroineSkill(string id,int slot)
        {
            var skill=HeroineSkillRules.AtLevel(combatDefinitions.Skill(id,slot),1);if(slot!=combatDefinitions.WeaponSkillSlot(id) || skill.effectRuleId!="effect.damage")return skill;
            var equipped=HomeState.weaponEquipment.SingleOrDefault(e=>e.heroineId==id);if(equipped==null)return skill;
            var node=HomeData().weaponNodes.Single(n=>n.id==equipped.nodeId);if(node.initial)return skill;float power=WeaponGrowthRules.Power(node,HomeState.WeaponLevel(node.id));skill.powerScale=(id.StartsWith("heroine.annihilator",StringComparison.Ordinal) || id=="heroine.shell" || id=="heroine.oriflamme" || id=="heroine.nighthawk")?skill.powerScale*power:power;return skill;
        }
        private void DrawHeroineDetail()
        {
            string id=book.SubjectId;var definition=combatDefinitions.Hero(id);var growth=formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id);
            int actor=Array.IndexOf(PreviewFormation(id),id);var state=HeroinePreview(heroId:id).State.Heroes[actor];var job=combatDefinitions.Job(definition.jobId);
            SanctuaryHeader("誓女の記憶",HeroineIdentityCatalog.JobName(definition.jobId)+"  ／  ★ ★ ★ ★ ★ ★");
            if(GrowthButton(52,104,206,48,"‹ 誓女一覧",BookInputAllowed)){heroineRosterOpen=true;selectedTrait=-1;return;}
            GrowthFrame(52,176,646,643);GrowthDiamond(373,414,204);GrowthDiamond(373,414,222);
            var heroineNameStyle=new GUIStyle(growthTitleStyle);while(heroineNameStyle.fontSize>21 && heroineNameStyle.CalcSize(new GUIContent(definition.name)).x>415)heroineNameStyle.fontSize--;
            Label(286,106,415,53,definition.name,heroineNameStyle,gold);
            DrawHeroPortrait(new Rect(69,187,609,344),id);
            GrowthFill(69,606,612,194,new Color(.045f,.095f,.135f,.97f));
            string[] stats={"HP  "+state.MaxHitPoints,"攻撃  "+state.Attack,"物理防御  "+state.PhysicalDefense,"魔法防御  "+state.MagicDefense,"速度  "+state.Speed,"会心  "+(state.CriticalChanceBp/100f).ToString("0.#")+"%"};
            for(int i=0;i<stats.Length;i++)Label(90+(i%2)*303,615+(i/2)*43,292,39,stats[i],growthTextStyle);
            if(GrowthButton(90,753,530,40,"交流へ  ›  好感度 Lv"+AffectionService.State(LifeSnapshot(),HomeData(),id).level,BookInputAllowed))OpenAffection(id);
            GrowthFill(737,104,811,715,parchment);GrowthLine(754,118,1531,118,gold,2);GrowthLine(754,801,1531,801,gold,2);
            if(GrowthButton(771,140,240,71,"Lv強化  "+growth.level+" / "+growth.LevelCap,BookInputAllowed,true))GrowthSelect(GrowthScreen.Level,growth);
            if(GrowthButton(1027,140,225,71,"覚醒  "+growth.awakeningStage+" / 2  ＋",BookInputAllowed))GrowthSelect(GrowthScreen.Awakening,growth);
            if(GrowthButton(1268,140,245,71,"重複強化  "+growth.duplicateRank+" / 5",BookInputAllowed))GrowthSelect(GrowthScreen.Duplicate,growth);
            if(bookNotices.For(id)!=BookNotice.None)Label(771,216,743,28,"通知："+BookNoticeText(bookNotices.For(id)),growthSmallStyle,gold);
            if(book.Face==BookFace.Details && formalCampaign.HasAffection)DrawBookHeroineInformation(id);
            else for(int slot=0;slot<3;slot++){
                var skill=DisplayHeroineSkill(id,slot);int level=growth.SkillLevel(slot);float y=253+slot*150;
                GrowthFill(762,y,765,140,new Color(.985f,.955f,.86f));GrowthLine(771,y+138,1511,y+138,new Color(.67f,.55f,.33f));
                DrawHeroineSkillIcon(new Rect(782,y+27,69,69),skill,slot);
                Label(869,y+9,490,36,skill.name,new GUIStyle(sanctuaryHeading){fontSize=24,wordWrap=false});
                string description=HeroineSkillRules.Description(skill,level,job).Split('\n')[0];
                if(description.Length>55)description=description.Substring(0,55)+"…";

                Label(869,y+51,480,86,description,sanctuarySmall);
                Label(1361,y+12,145,32,"Lv."+level+" / 7",sanctuaryBody);
                if(GrowthButton(1361,y+80,145,44,level==7?"詳細":"強化",BookInputAllowed)){selectedSkillSlot=slot;growthScreen=GrowthScreen.Skill;growthOutcome=null;PlayProductionUiSound("決定");}
            }
            var traits=HeroineIdentityCatalog.Traits(definition,growth);
            GrowthFill(84,531,596,74,new Color(.035f,.065f,.10f,.94f));
            for(int i=0;i<traits.Length;i++){
                var rect=new Rect(92+i*70,538,60,60);DrawSanctuaryIcon(rect,traits[i].icon,traits[i].active?gold:muted);
                if(selectedTrait==i)GrowthDiamond(rect.center.x,rect.center.y,34);
                if(ImageUiSkin.Button(new Rect(rect.x,rect.y,64,64),"",GUIStyle.none) && BookInputAllowed)selectedTrait=selectedTrait==i?-1:i;
            }
            if(GrowthButton(771,726,743, sixty,"神器  ／  装備の木をひらく",BookInputAllowed,true)){growthScreen=GrowthScreen.Weapons;selectedNode=null;}
            if(selectedTrait>=0){var t=traits[selectedTrait];GrowthFrame(80,617,586,183);Label(103,633,520,35,t.name,growthTextStyle,gold);Label(103,677,526,106,t.description,new GUIStyle(growthSmallStyle){fontSize=19,wordWrap=true});}
            DrawBookTransition(true);DrawBookFooter(true);
        }
        private void DrawBookHeroineInformation(string id)
        {
            var save=LifeSnapshot();var a=AffectionService.State(save,HomeData(),id);
            Label(771,253,615,42,"好感度 Lv"+a.level+" ／ EXP "+a.exp+" / 100"+(AffectionService.IsLover(a)?" ／ 恋人":""),sanctuaryHeading);
            GrowthFill(771,309,590,12,new Color(.16f,.19f,.22f));GrowthFill(771,309,590*a.exp/100f,12,gold);
            if(a.levelCap==99){DrawEternalRing(new Rect(1400,255,65,65));Label(1400,324,65,32,"99",new GUIStyle(sanctuaryBody){alignment=TextAnchor.MiddleCenter});}
            else Label(1380,271,130,40,"上限20",sanctuaryBody);
            var events=AffectionEventResolver.ForPerson(save,HomeData(),id);
            int unread=events.Count(e=>a.level>=e.requiredAffectionLevel && !a.readEventIds.Contains(e.id));
            Label(771,343,690,34,"交流・回想 "+events.Length+"件 ／ 未読 "+unread+"件",sanctuaryBody);
            affectionScroll=GUI.BeginScrollView(new Rect(771,385,743,267),affectionScroll,new Rect(0,0,715,Math.Max(267,events.Length*58)));
            int first=Math.Max(0,(int)(affectionScroll.y/58)),last=Math.Min(events.Length,first+6);
            for(int i=first;i<last;i++){
                var ev=events[i];bool open=a.level>=ev.requiredAffectionLevel,read=a.readEventIds.Contains(ev.id);
                string title=!open && ev.visibility=="hint"?"これから紡ぐ物語":ProductionStoryActive?ProductionStoryTitle(ev.id):ev.id;
                if(GrowthButton(0,i*58,710,52,(read?"回想":open?"未読":"Lv"+ev.requiredAffectionLevel+"で解放")+" ／ "+title,open && BookInputAllowed))BeginAdv(ev.id,read);
            }
            GUI.EndScrollView();
            if(GrowthButton(771,666,743,44,"交流・指輪へ",BookInputAllowed))OpenAffection(id);
        }
        private const float sixty=60;
        private void DrawHeroineSkillUpgrade()
        {
            string id=book.SubjectId;var g=formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id);var skill=DisplayHeroineSkill(id,selectedSkillSlot);var job=combatDefinitions.Job(combatDefinitions.Hero(id).jobId);int level=g.SkillLevel(selectedSkillSlot);
            SanctuaryHeader("スキルの研鑽",combatDefinitions.Hero(id).name+"  ／  "+skill.name);
            GrowthFrame(145,146,1310,650);
            if(GrowthButton(180,172,195,48,"‹ 能力へ戻る",!formalProgression.HasPending)){growthScreen=GrowthScreen.Overview;return;}
            DrawHeroineSkillIcon(new Rect(430,177,65,65),skill,selectedSkillSlot);Label(518,184,800, fifty,skill.name,growthTitleStyle);
            GrowthFill(184,267,584,294,parchment);GrowthFill(788,267,626,294,parchment);
            Label(208,285,528,46,"現在  Lv."+level,sanctuaryHeading);Label(208,350,528,194,HeroineSkillRules.Description(skill,level,job),sanctuaryBody);
            Label(814,285,570,46,level==7?"最大Lvに到達しました":"次の強化  Lv."+(level+1),sanctuaryHeading);
            Label(814,350,570,194,HeroineSkillRules.Description(skill,Math.Min(7,level+1),job),sanctuaryBody);
            int cost=level==7?0:HeroineSkillRules.UpgradeCost(level);Label(187,582,1225,45,level==7?"このスキルは最大Lv7です。":$"ネクタル 必要 {cost} ／ 所持 {formalProgression.Snapshot.nectar}",growthTextStyle,gold);
            Label(187,639,1225,49,growthOutcome??"",growthSmallStyle);
            if(GrowthButton(184,710,1228, sixty,formalProgression.HasPending?"同じ内容で保存を再試行":level==7?"MAX ／ すべてのスキルLv7で熟達特性が解放":"Lv."+(level+1)+"へ強化する",formalProgression.HasPending || level<7 && formalProgression.Snapshot.nectar>=cost,true)){
                if(growthRequest==null)growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,formalProgression.Snapshot.revision,GrowthOperation.Skill,level+1,skillSlot:selectedSkillSlot);
                var result=formalProgression.Commit(growthRequest,SaveFormalGrowth);growthOutcome=result==GrowthCommitResult.SaveFailed?"保存できませんでした。費用とスキルLvは変更せず、同じ内容を再試行します。":"スキル強化を保存しました。";if(result!=GrowthCommitResult.SaveFailed){growthRequest=null;PlayProductionUnlock();}
            }
        }
        private const float fifty=50;
    }
}
