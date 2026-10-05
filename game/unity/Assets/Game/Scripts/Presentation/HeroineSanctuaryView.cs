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
        private string heroineQuery="";
        private int heroinePage,heroineJobFilter,selectedTrait=-1,selectedSkillSlot;
        private const int HeroinePageSize=12;
        private HeroineRoster heroineRoster;
        private GUIStyle sanctuaryHeading,sanctuaryBody,sanctuarySmall;
        private readonly Dictionary<string,Texture2D> heroinePortraits=new Dictionary<string,Texture2D>();
        private readonly Color parchment=new Color(.94f,.89f,.77f),paperInk=new Color(.13f,.18f,.22f);
        private Texture2D HeroPortrait(string id)
        {
            if(!heroinePortraits.TryGetValue(id,out var texture)){texture=Resources.Load<Texture2D>("Illustrations/"+id.Substring("heroine.".Length)+"-portrait-candidate-v1");heroinePortraits[id]=texture;}return texture;
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
            Label(58,20,1000,43,titleText,growthTitleStyle);Label(1060,32,480,34,subtitle,growthSmallStyle,gold);
        }
        private void DrawGrowthExperience()
        {
            SanctuaryStyles();
            if(heroineRosterOpen){DrawHeroineRoster();return;}
            if(!book.HasSubject){heroineRosterOpen=true;DrawHeroineRoster();return;}
            if(growthScreen==GrowthScreen.Overview || growthScreen==GrowthScreen.Information){DrawHeroineDetail();return;}
            if(growthScreen==GrowthScreen.Weapons && ProductionStoryActive){DrawSanctuaryWeaponTree(book.SubjectId);return;}
            if(growthScreen==GrowthScreen.Skill){DrawHeroineSkillUpgrade();return;}
            DrawLegacyGrowthExperience();
        }
        private void DrawHeroineRoster()
        {
            SanctuaryHeader("誓女の星図","名前と顔から、会いたい誓女を選ぶ");
            heroineRoster=heroineRoster??HeroineRosterCatalog.InitialFive(combatDefinitions);
            if(GrowthButton(58,108,190,48,"万物の書へ",BookInputAllowed)){book.Close();book.Reenter();return;}
            Label(278,111,110,38,"名前検索",growthSmallStyle);
            string query=GUI.TextField(new Rect(383,108,455,48),heroineQuery,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=23,padding=new RectOffset(14,14,10,8)});
            if(query!=heroineQuery){heroineQuery=query;heroinePage=0;}
            if(GrowthButton(853,108,95,48,"クリア",heroineQuery.Length>0)){heroineQuery="";heroinePage=0;}
            var jobs=combatDefinitions.jobs.Select(j=>j.id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            if(GrowthButton(975,108,570,48,"ジョブ  ／  "+(heroineJobFilter==0?"すべて":HeroineIdentityCatalog.JobName(jobs[heroineJobFilter-1]))+"  ›",BookInputAllowed)){heroineJobFilter=(heroineJobFilter+1)%(jobs.Length+1);heroinePage=0;}
            var snapshot=formalProgression.Snapshot;var entries=heroineRoster.Search(heroineQuery,heroineJobFilter==0?null:jobs[heroineJobFilter-1],snapshot.heroines.Select(h=>h.heroineId));
            int pages=Math.Max(1,(entries.Length+HeroinePageSize-1)/HeroinePageSize);heroinePage=Mathf.Clamp(heroinePage,0,pages-1);
            Label(60,177,1440,35,$"所持 {snapshot.heroines.Length}人  ／  表示 {entries.Length}人    ·    カードを選択すると能力・スキル・神器を開きます",growthSmallStyle);
            var page=entries.Skip(heroinePage*HeroinePageSize).Take(HeroinePageSize).ToArray();
            for(int i=0;i<page.Length;i++){
                var entry=page[i];var h=snapshot.heroines.Single(g=>g.heroineId==entry.id);float x=62+(i%4)*374,y=232+(i/4)*183;
                var rect=new Rect(x,y,352,166);bool hover=rect.Contains(Event.current.mousePosition);
                GrowthFrame(x,y,352,166);GrowthFill(x+3,y+3,117,160,hover?new Color(.18f,.28f,.31f):new Color(.10f,.18f,.23f));
                var portrait=HeroPortrait(entry.id);if(portrait!=null)GUI.DrawTexture(new Rect(x+5,y+6,111,154),portrait,ScaleMode.ScaleAndCrop,true);else DrawSanctuaryIcon(new Rect(x+32,y+45,65,65),"star",gold);
                Label(x+134,y+23,212,41,entry.name,new GUIStyle(growthTextStyle){fontSize=24});
                Label(x+134,y+75,207,31,HeroineIdentityCatalog.JobName(entry.jobId),growthSmallStyle,gold);
                Label(x+134,y+119,204,33,"Lv."+h.level+"   ／   ★6",growthSmallStyle);
                if(hover)GrowthLine(x+124,y+156,x+338,y+156,gold,2);
                if(GUI.Button(rect,"",GUIStyle.none) && BookInputAllowed){if(book.SubjectId!=entry.id)book.RequestSubject(BookBookmark.Heroines,entry.id);heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;selectedTrait=-1;selectedNode=null;bookTransitionElapsed=0;PlayProductionUiSound("決定");}
            }
            if(page.Length==0){GrowthDiamond(800,454,50);Label(440,530,800,50,"条件に合う誓女がいません。検索やジョブを変更してください。",growthTextStyle);}
            if(GrowthButton(62,808,210,52,"‹ 前の12人",heroinePage>0 && BookInputAllowed))heroinePage--;
            Label(665,818,280,40,$"{heroinePage+1} / {pages} ページ",growthTextStyle);
            if(GrowthButton(1320,808,225,52,"次の12人 ›",heroinePage+1<pages && BookInputAllowed))heroinePage++;
        }
        private PlayableBattle HeroinePreview(FormalGrowthSave growth=null)=>new PlayableBattle(1,campaign.Playable,combatDefinitions:combatDefinitions,formalGrowth:growth??formalProgression.Snapshot,homeProgress:formalCampaign.Snapshot.home,homeCatalog:HomeData(),collectionGrowth:formalCampaign.Snapshot.collection,relicCatalog:CollectionData());
        private SkillCombatDef DisplayHeroineSkill(string id,int slot)
        {
            var skill=HeroineSkillRules.AtLevel(combatDefinitions.Skill(id,slot),1);if(slot!=0)return skill;
            var equipped=HomeState.weaponEquipment.SingleOrDefault(e=>e.heroineId==id);if(equipped==null)return skill;
            var node=HomeData().weaponNodes.Single(n=>n.id==equipped.nodeId);skill.powerScale=WeaponGrowthRules.Power(node,HomeState.WeaponLevel(node.id));return skill;
        }
        private void DrawHeroineDetail()
        {
            string id=book.SubjectId;var definition=combatDefinitions.Hero(id);var growth=formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id);
            int actor=Array.IndexOf(combatDefinitions.FormationIds,id);var state=HeroinePreview().State.Heroes[actor];var job=combatDefinitions.Job(definition.jobId);
            SanctuaryHeader("誓女の記憶",HeroineIdentityCatalog.JobName(definition.jobId)+"  ／  ★ ★ ★ ★ ★ ★");
            if(GrowthButton(52,104,206,48,"‹ 誓女一覧",BookInputAllowed)){heroineRosterOpen=true;selectedTrait=-1;return;}
            GrowthFrame(52,176,646,643);GrowthDiamond(373,414,204);GrowthDiamond(373,414,222);
            Label(286,106,415,53,definition.name,growthTitleStyle,gold);
            var portrait=HeroPortrait(id);if(portrait!=null)GUI.DrawTexture(new Rect(69,187,609,410),portrait,ScaleMode.ScaleToFit,true);
            GrowthFill(69,606,612,194,new Color(.045f,.095f,.135f,.97f));
            string[] stats={"HP  "+state.MaxHitPoints,"攻撃  "+state.Attack,"物理防御  "+state.PhysicalDefense,"魔法防御  "+state.MagicDefense,"速度  "+state.Speed,"会心  "+(state.CriticalChanceBp/100f).ToString("0.#")+"%"};
            for(int i=0;i<stats.Length;i++)Label(90+(i%2)*303,615+(i/2)*43,292,39,stats[i],growthTextStyle);
            Label(90,753,530,32,"会心威力 "+state.CriticalMultiplierPercent+"% ／ 神器・遺物・特性込み",growthSmallStyle);
            GrowthFill(737,104,811,715,parchment);GrowthLine(754,118,1531,118,gold,2);GrowthLine(754,801,1531,801,gold,2);
            if(GrowthButton(771,140,240,71,"Lv. "+growth.level+" / "+growth.LevelCap+"  ＋",BookInputAllowed,true))GrowthSelect(GrowthScreen.Level,growth);
            if(GrowthButton(1027,140,225,71,"覚醒  "+growth.awakeningStage+" / 2  ＋",BookInputAllowed))GrowthSelect(GrowthScreen.Awakening,growth);
            if(GrowthButton(1268,140,245,71,"誓い  "+growth.duplicateRank+" / 5  ＋",BookInputAllowed))GrowthSelect(GrowthScreen.Duplicate,growth);
            Label(771,215,740,29,"能力の数値とスキルをクリックして強化",sanctuarySmall);
            for(int slot=0;slot<3;slot++){
                var skill=DisplayHeroineSkill(id,slot);int level=growth.SkillLevel(slot);float y=253+slot*150;
                GrowthFill(762,y,765,140,new Color(.985f,.955f,.86f));GrowthLine(771,y+138,1511,y+138,new Color(.67f,.55f,.33f));
                DrawSanctuaryIcon(new Rect(782,y+27,69,69),skill.effectRuleId=="effect.self-buff"?"star":skill.damageType=="magic"?"moon":"sword",slot==1?new Color(.35f,.32f,.60f):new Color(.65f,.26f,.30f));
                Label(869,y+9,490,36,skill.name,new GUIStyle(sanctuaryHeading){fontSize=24,wordWrap=false});
                string description=HeroineSkillRules.Description(skill,level,job).Split('\n')[0];
                if(description.Length>55)description=description.Substring(0,55)+"…";
                description+="\n全文・消費・強化はクリック ›";
                Label(869,y+51,633,86,description,sanctuarySmall);
                Label(1361,y+12,145,32,"Lv."+level+" / 7  "+(level==7?"MAX":"＋"),sanctuaryBody);
                if(GUI.Button(new Rect(762,y,765,140),"",GUIStyle.none) && BookInputAllowed){selectedSkillSlot=slot;growthScreen=GrowthScreen.Skill;growthOutcome=null;PlayProductionUiSound("決定");}
            }
            var traits=HeroineIdentityCatalog.Traits(definition,growth);
            GrowthFill(84,531,596,74,new Color(.035f,.065f,.10f,.94f));
            for(int i=0;i<traits.Length;i++){
                var rect=new Rect(102+i*184,538,74,63);DrawSanctuaryIcon(rect,traits[i].icon,traits[i].active?gold:muted);
                Label(rect.x+82,rect.y+2,95,59,traits[i].active?"特性\n詳細 ›":"熟達\n未解放",growthSmallStyle);
                if(GUI.Button(new Rect(rect.x,rect.y,174,63),"",GUIStyle.none) && BookInputAllowed)selectedTrait=selectedTrait==i?-1:i;
            }
            if(GrowthButton(771,726,743, sixty,"神器  ／  装備の木をひらく",BookInputAllowed,true)){growthScreen=GrowthScreen.Weapons;selectedNode=null;}
            if(selectedTrait>=0){var t=traits[selectedTrait];GrowthFrame(80,617,586,183);Label(103,633,520,35,t.name,growthTextStyle,gold);Label(103,677,526,106,t.description,new GUIStyle(growthSmallStyle){fontSize=19,wordWrap=true});}
            Label(59,850,1470,33,"レベル → 育成   ·   スキル → Lv1〜7強化   ·   特性アイコン → 効果説明   ·   Escで一覧へ",growthSmallStyle);
            DrawBookTransition(true);
        }
        private const float sixty=60;
        private void DrawHeroineSkillUpgrade()
        {
            string id=book.SubjectId;var g=formalProgression.Snapshot.heroines.Single(h=>h.heroineId==id);var skill=DisplayHeroineSkill(id,selectedSkillSlot);var job=combatDefinitions.Job(combatDefinitions.Hero(id).jobId);int level=g.SkillLevel(selectedSkillSlot);
            SanctuaryHeader("スキルの研鑽",combatDefinitions.Hero(id).name+"  ／  "+skill.name);
            GrowthFrame(145,146,1310,650);
            if(GrowthButton(180,172,195,48,"‹ 能力へ戻る",!formalProgression.HasPending)){growthScreen=GrowthScreen.Overview;return;}
            DrawSanctuaryIcon(new Rect(430,177,65,65),skill.damageType=="magic"?"moon":"sword",gold);Label(518,184,800, fifty,skill.name,growthTitleStyle);
            GrowthFill(184,267,584,294,parchment);GrowthFill(788,267,626,294,parchment);
            Label(208,285,528,46,"現在  Lv."+level,sanctuaryHeading);Label(208,350,528,194,HeroineSkillRules.Description(skill,level,job),sanctuaryBody);
            Label(814,285,570,46,level==7?"最大Lvに到達しました":"次の強化  Lv."+(level+1),sanctuaryHeading);
            Label(814,350,570,194,HeroineSkillRules.Description(skill,Math.Min(7,level+1),job),sanctuaryBody);
            int cost=level==7?0:HeroineSkillRules.UpgradeCost(level);Label(187,582,1225,45,level==7?"このスキルは最大Lv7です。":$"ネクタル 必要 {cost} ／ 所持 {formalProgression.Snapshot.nectar}    ·    確定まで消費しません",growthTextStyle,gold);
            Label(187,639,1225,49,growthOutcome??"Lv強化では待機・詠唱・資源消費・チェイン率は変わりません。",growthSmallStyle);
            if(GrowthButton(184,710,1228, sixty,formalProgression.HasPending?"同じ内容で保存を再試行":level==7?"MAX ／ すべてのスキルLv7で熟達特性が解放":"Lv."+(level+1)+"へ強化する",formalProgression.HasPending || level<7 && formalProgression.Snapshot.nectar>=cost,true)){
                if(growthRequest==null)growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,formalProgression.Snapshot.revision,GrowthOperation.Skill,level+1,skillSlot:selectedSkillSlot);
                var result=formalProgression.Commit(growthRequest,SaveFormalGrowth);growthOutcome=result==GrowthCommitResult.SaveFailed?"保存できませんでした。費用とスキルLvは変更せず、同じ内容を再試行します。":"スキル強化を保存しました。";if(result!=GrowthCommitResult.SaveFailed){growthRequest=null;PlayProductionUnlock();}
            }
        }
        private const float fifty=50;
    }
}
