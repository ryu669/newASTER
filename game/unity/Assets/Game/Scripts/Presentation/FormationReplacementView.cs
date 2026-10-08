using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string formationCandidateHero,formationCandidateOopart,formationPreviewKey;
        private BattleHero formationPreviewHero;
        private Vector2 formationDetailScroll;
        private int formationHeroSort,formationItemSort; private bool formationSelectionPagePending;
        private void PickerSearch(ref string query,ref int page,ref int sort,bool interactive)
        {
            string next=ImageUiSkin.TextField(new Rect(540,108,635,48),query,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=22});if(next!=query){query=next;page=0;}
            if(GrowthButton(1190,108,340,48,sort==0?"名前 ↑":"Lv ↓",interactive)){sort=1-sort;page=0;}
        }
        private void PickerPage(ref int page,int pages,bool interactive)
        {
            if(GrowthButton(540,806,90,48,"‹",interactive && page>0))page--;
            Label(655,812,180,40,(page+1)+" / "+pages,growthSmallStyle);
            if(GrowthButton(850,806,90,48,"›",interactive && page+1<pages))page++;
        }
        private void PickerCard(Rect rect,string name,int level,bool selected,string badge)
        {
            GrowthFrame(rect.x,rect.y,rect.width,rect.height);if(selected){GrowthFill(rect.x+3,rect.y+3,rect.width-6,rect.height-6,new Color(.17f,.28f,.32f));GrowthLine(rect.x+8,rect.y+rect.height-5,rect.x+rect.width-8,rect.y+rect.height-5,gold,3);}
            var style=new GUIStyle(growthTextStyle){fontSize=20,alignment=TextAnchor.MiddleCenter};while(style.fontSize>13 && style.CalcSize(new GUIContent(name)).x>rect.width-16)style.fontSize--;
            Label(rect.x+8,rect.y+rect.height-75,rect.width-16,35,name,style);Label(rect.x+10,rect.y+rect.height-40,rect.width-20,28,"Lv."+level+(badge==null?"":" ／ "+badge),new GUIStyle(growthSmallStyle){fontSize=15,alignment=TextAnchor.MiddleCenter});
        }
        private BattleHero FormationCandidateStats(string form)
        {
            string key=formalCampaign.Revision+"|"+formationSlot+"|"+form;if(key==formationPreviewKey)return formationPreviewHero;
            var s=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(OopartSnapshot()));HomeRules.Apply(s,HomeData(),new HomeOperation("formation",form,formationSlot.ToString()));var ids=s.home.formationIds.ToArray();
            for(int i=0;i<5;i++)if(ids[i]==null)ids[i]=s.growth.heroines.Select(h=>h.heroineId).First(id=>!ids.Where(v=>v!=null).Any(v=>combatDefinitions.PersonId(v)==combatDefinitions.PersonId(id)));
            var b=new PlayableBattle(1,campaign.Playable,1,combatDefinitions:combatDefinitions.WithFormation(ids),formalGrowth:s.growth,collectionGrowth:s.collection,homeProgress:s.home,homeCatalog:HomeData(),relicCatalog:CollectionData(),useJobRulesV2:true);
            formationPreviewKey=key;return formationPreviewHero=b.State.Heroes[formationSlot];
        }
        private void DrawFormationHeroPicker(bool interactive)
        {
            PickerSearch(ref formationQuery,ref formationPage,ref formationHeroSort,interactive);
            var candidates=formalProgression.Snapshot.heroines.Where(h=>combatDefinitions.HeroineIds.Contains(h.heroineId) && combatDefinitions.Hero(h.heroineId).name.Contains(formationQuery));
            var all=(formationHeroSort==0?candidates.OrderBy(h=>combatDefinitions.Hero(h.heroineId).name):candidates.OrderByDescending(h=>h.level).ThenBy(h=>combatDefinitions.Hero(h.heroineId).name)).ToArray();int pages=Math.Max(1,(all.Length+7)/8);formationPage=Mathf.Clamp(formationPage,0,pages-1);
            if(formationCandidateHero==null && all.Length>0)formationCandidateHero=all[0].heroineId;
            if(formationSelectionPagePending){formationPage=Math.Max(0,Array.FindIndex(all,h=>h.heroineId==formationCandidateHero))/8;formationSelectionPagePending=false;}var visible=all.Skip(formationPage*8).Take(8).ToArray();
            for(int i=0;i<visible.Length;i++){
                var h=visible[i];float x=540+i%4*251,y=190+i/4*294;var rect=new Rect(x,y,232,276);PickerCard(rect,combatDefinitions.Hero(h.heroineId).name,h.level,formationCandidateHero==h.heroineId,null);DrawHeroPortrait(new Rect(x+6,y+7,220,188),h.heroineId);
                if(ImageUiSkin.Button(rect,"",GUIStyle.none) && interactive){formationCandidateHero=h.heroineId;formationDetailScroll=Vector2.zero;}
            }
            GrowthFrame(65,190,450,576);
            if(formationCandidateHero!=null){
                var h=formalProgression.Snapshot.heroines.Single(v=>v.heroineId==formationCandidateHero);var def=combatDefinitions.Hero(h.heroineId);DrawHeroPortrait(new Rect(80,205,135,183),h.heroineId);
                Label(235,214,260,85,def.name,new GUIStyle(growthTitleStyle){fontSize=25});Label(235,305,260,70,"Lv."+h.level+"\n"+HeroineIdentityCatalog.JobName(def.jobId),growthSmallStyle);
                var actor=FormationCandidateStats(h.heroineId);Label(90,400,390,105,"HP "+actor.MaxHitPoints+"　攻撃 "+actor.Attack+"\n物理防御 "+actor.PhysicalDefense+"　魔法防御 "+actor.MagicDefense+"\n速度 "+actor.Speed,growthSmallStyle);
                string skills=string.Join("\n\n",Enumerable.Range(0,3).Select(i=>{var d=HeroineSkillRules.AtLevel(combatDefinitions.Skill(h.heroineId,i),h.SkillLevel(i));return d.name+" ／ Lv."+h.SkillLevel(i)+"\n"+(d.effectRuleId=="effect.damage"?"威力 "+(d.powerScale*100).ToString("0.#")+"%":d.effectRuleId=="effect.heal"?"回復 "+d.baseHealing:TimedSelfEffectDef.Label(d.selfEffects?.FirstOrDefault()?.kind??"attack"))+" ／ "+d.resourceCost+" "+combatDefinitions.Job(def.jobId).resourceName;}));
                formationDetailScroll=GUI.BeginScrollView(new Rect(90,523,395,220),formationDetailScroll,new Rect(0,0,365,Math.Max(220,growthSmallStyle.CalcHeight(new GUIContent(skills),365))));Label(0,0,365,Math.Max(220,growthSmallStyle.CalcHeight(new GUIContent(skills),365)),skills,growthSmallStyle);GUI.EndScrollView();
            }
            PickerPage(ref formationPage,pages,interactive);
            if(GrowthButton(65,806,200,48,"外す",interactive && CurrentFormation()[formationSlot]!=null)){oopartSlot=formationSlot;ProposeOopart("clear-slot");CommitOopart();}
            if(GrowthButton(1220,806,312,48,"決定",interactive && formationCandidateHero!=null && formationCandidateHero!=CurrentFormation()[formationSlot],true)){ProposeHome(new HomeOperation("formation",formationCandidateHero,formationSlot.ToString()));ConfirmHome();}
        }
        private void DrawFormationItemPicker(bool interactive)
        {
            oopartSlot=formationSlot;var s=OopartSnapshot();var o=s.collection.ooparts;PickerSearch(ref oopartPickerQuery,ref oopartPickerPage,ref formationItemSort,interactive);
            var candidates=o.progress.Where(p=>CollectionData().Oopart(p.oopartId).name.Contains(oopartPickerQuery));var all=(formationItemSort==0?candidates.OrderBy(p=>CollectionData().Oopart(p.oopartId).name):candidates.OrderByDescending(p=>p.level).ThenBy(p=>CollectionData().Oopart(p.oopartId).name)).ToArray();int pages=Math.Max(1,(all.Length+7)/8);oopartPickerPage=Mathf.Clamp(oopartPickerPage,0,pages-1);
            if(formationCandidateOopart==null && all.Length>0)formationCandidateOopart=all[0].oopartId;
            if(formationSelectionPagePending){oopartPickerPage=Math.Max(0,Array.FindIndex(all,p=>p.oopartId==formationCandidateOopart))/8;formationSelectionPagePending=false;}var visible=all.Skip(oopartPickerPage*8).Take(8).ToArray();
            for(int i=0;i<visible.Length;i++){
                var p=visible[i];float x=540+i%4*251,y=190+i/4*294;var rect=new Rect(x,y,232,276);int slot=Array.FindIndex(o.slots,v=>v.equippedOopartId==p.oopartId);
                PickerCard(rect,CollectionData().Oopart(p.oopartId).name,p.level,p.oopartId==formationCandidateOopart,slot<0?null:"第"+(slot+1)+"枠");DrawOopartImage(new Rect(x+23,y+30,186,155),p.oopartId);
                if(ImageUiSkin.Button(rect,"",GUIStyle.none) && interactive){formationCandidateOopart=p.oopartId;formationDetailScroll=Vector2.zero;}
            }
            GrowthFrame(65,190,450,576);
            var selected=o.progress.SingleOrDefault(p=>p.oopartId==formationCandidateOopart);
            if(selected!=null){
                var d=CollectionData().Oopart(selected.oopartId);DrawOopartImage(new Rect(85,217,126,137),d.id);Label(230,210,260,95,d.name,new GUIStyle(growthTitleStyle){fontSize=24});Label(230,310,260, forty,"Lv."+selected.level+" / 120",growthSmallStyle);
                if(GrowthButton(230,363,250,42,"強化",interactive))OpenOoparts(formationSlot,d.id);
                var total=OopartService.Fixed(selected,d);Label(90,428,395,105,"HP +"+total.hp+"　攻撃 +"+total.attack+"\n物理防御 +"+total.physicalDefense+"　魔法防御 +"+total.magicDefense+"\n速度 +"+total.speed,growthSmallStyle);
                string form=o.slots[formationSlot].heroineFormId;var context=new OopartEffectContext{inBattle=false,jobId=form==null?null:combatDefinitions.Hero(form).jobId,formationJobs=o.slots.Where(v=>v.heroineFormId!=null).Select(v=>combatDefinitions.Hero(v.heroineFormId).jobId).ToArray()};
                string effects=string.Join("\n\n",d.effects.Select(e=>EffectText(e)+"\n"+(form==null?"空枠":OopartEffectEngine.Active(e,context)==false?"無効":e.scalingType!="constant" || e.durationClock>0 || OopartEffectEngine.Active(e,context)==null?"戦闘中に判定":"有効")+(e.conditions.Length==0?"":" ／ "+string.Join(" かつ ",e.conditions.Select(OopartConditionText)))));
                float height=Math.Max(205,growthSmallStyle.CalcHeight(new GUIContent(effects),365));formationDetailScroll=GUI.BeginScrollView(new Rect(90,542,395,205),formationDetailScroll,new Rect(0,0,365,height));Label(0,0,365,height,effects,growthSmallStyle);GUI.EndScrollView();
            }
            PickerPage(ref oopartPickerPage,pages,interactive);
            if(GrowthButton(65,806,200,48,"外す",interactive && o.slots[formationSlot].equippedOopartId!=null)){ProposeOopart("equip");CommitOopart();}
            bool available=selected!=null && !o.slots.Any(v=>v.equippedOopartId==selected.oopartId);
            if(GrowthButton(1220,806,312,48,"決定",interactive && available,true)){selectedOopart=selected.oopartId;ProposeOopart("equip",selected.oopartId);CommitOopart();}
        }
    }
}
