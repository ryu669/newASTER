using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly Dictionary<string,Texture2D> sanctuaryIcons=new Dictionary<string,Texture2D>();
        private BoundedCache<string,Texture2D> sanctuaryTrees;
        private Texture2D SanctuaryIcon(string kind)
        {
            if(sanctuaryIcons.TryGetValue(kind,out var texture))return texture;
            if(kind.StartsWith("resource.",StringComparison.Ordinal)){texture=Resources.Load<Texture2D>("UI/Jobs/"+kind.Substring(9));if(texture==null)throw new InvalidOperationException("Missing distinct job resource image: "+kind);sanctuaryIcons[kind]=texture;return texture;}
            string image=kind=="sword"?"fighter":kind=="leaf"?"healer":kind=="moon"?"alchemist":kind=="crown"?"general":kind=="flame"?"berserker":kind=="resource"?"artist":"gambler";
            texture=Resources.Load<Texture2D>("UI/Jobs/"+image);if(texture==null)throw new InvalidOperationException("Missing image emblem: "+kind);sanctuaryIcons[kind]=texture;return texture;
        }
        private void DrawSanctuaryIcon(Rect rect,string kind,Color color)
        {
            var prior=GUI.color;GUI.color=new Color(1,1,1,color==muted?.45f:1f);
            if(kind.StartsWith("resource.",StringComparison.Ordinal))GUI.DrawTexture(rect,SanctuaryIcon(kind),ScaleMode.ScaleToFit,true);
            else DrawBookEmblem(rect,kind=="sword"?9:kind=="leaf"?10:kind=="moon"?11:kind=="crown"?12:kind=="flame"?13:kind=="resource"?14:15);
            GUI.color=prior;
        }
        private void DrawHeroineSkillIcon(Rect rect,SkillCombatDef skill,int slot)
        {
            string effect=skill.effectRuleId=="effect.heal"?"heal":skill.effectRuleId=="effect.allies-buff" || skill.effectRuleId=="effect.self-buff"?"trait":skill.selfHealingBaseAttackPercent>0?"heal":slot==2?"critical-damage":skill.damageType=="magic"?"magic":"attack";
            DrawWeaponEffect(rect,effect);
            var badge=new Rect(rect.xMax-25,rect.yMax-25,25,25);
            GrowthFill(badge.x,badge.y,badge.width,badge.height,ink);
            Label(badge.x,badge.y,badge.width,badge.height,(slot+1).ToString(),new GUIStyle(growthSmallStyle){fontSize=17,alignment=TextAnchor.MiddleCenter},ivory);
        }
        private NewAster.Data.HeroineWeaponTreeCatalog weaponTreeCatalog;
        private Texture2D weaponEffectAtlas;
        private Vector2 weaponComparisonScroll,weaponNextLevelScroll;
        private GUIStyle weaponCompactButtonStyle;
        private bool weaponShowLevels;
        private string weaponMaterialReturnHero;
        private bool WeaponCompactButton(float x,float y,float width,float height,string caption,bool enabled=true)
        {var previous=growthButtonStyle;try{growthButtonStyle=weaponCompactButtonStyle??(weaponCompactButtonStyle=new GUIStyle(previous){fontSize=15,padding=new RectOffset(0,0,0,0),wordWrap=false});return GrowthButton(x,y,width,height,caption,enabled);}finally{growthButtonStyle=previous;}}
        private void OpenWeaponMaterialSource(string hero,string colossus)
        {
            if(!BookInputAllowed || !FlushSaveChanges())return;
            if(book.RequestSubject(BookBookmark.Colossi,colossus)){weaponMaterialReturnHero=hero;bookTransitionElapsed=0;}
        }
        private NewAster.Data.HeroineWeaponTreeDef WeaponTreeLayout(string hero)
        {
            if(weaponTreeCatalog==null){var json=Resources.Load<TextAsset>("UI/heroine-weapon-trees");if(json==null)throw new InvalidOperationException("Missing weapon tree image layout catalog.");weaponTreeCatalog=JsonUtility.FromJson<NewAster.Data.HeroineWeaponTreeCatalog>(json.text);weaponTreeCatalog.Validate();}
            return weaponTreeCatalog.Entry(hero);
        }
        private Texture2D SanctuaryTree(string hero)
        {
            EnsureHeroineImageCaches();
            if(sanctuaryTrees.TryGetValue(hero,out var texture))return texture;
            texture=Resources.Load<Texture2D>(WeaponTreeLayout(hero).resourcePath);if(texture==null)throw new InvalidOperationException("Missing heroine weapon tree image: "+hero);
            sanctuaryTrees[hero]=texture;return texture;
        }
        private void DrawWeaponEffect(Rect rect,string effect,bool acquired=true)
        {
            if(weaponEffectAtlas==null)weaponEffectAtlas=Resources.Load<Texture2D>("UI/weapon-effects-v1");if(weaponEffectAtlas==null)throw new InvalidOperationException("Missing weapon effect icon atlas.");
            int index=Array.IndexOf(new[]{"attack","power","physical","magic","speed","critical","critical-damage","trait","hp","heal","potion","time","fire","ice","root","wing"},effect);if(index<0)throw new ArgumentException("Unknown weapon effect icon: "+effect);
            var prior=GUI.color;GUI.color=new Color(1,1,1,acquired?1:.50f);GUI.DrawTextureWithTexCoords(rect,weaponEffectAtlas,new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f),true);GUI.color=prior;
        }
        private void DrawWeaponNodeEffects(float x,float y,HomeWeaponNode node,bool acquired)
        {
            DrawWeaponEffect(new Rect(x-25,y-25,50,50),WeaponGrowthRules.Icon(node),acquired);
        }

        private void DrawSanctuaryWeaponTree(string hero)
        {
            var catalog=HomeData();var nodes=catalog.weaponNodes.Where(n=>n.heroineId==hero).ToArray();var state=HomeState;var snapshot=formalCampaign.Snapshot;
            if(selectedNode==null || !nodes.Any(n=>n.id==selectedNode))selectedNode=state.weaponEquipment.SingleOrDefault(e=>e.heroineId==hero)?.nodeId??nodes[0].id;
            var selected=nodes.Single(n=>n.id==selectedNode);bool owned=state.weaponNodeIds.Contains(selected.id);int level=state.WeaponLevel(selected.id);bool parents=selected.parentIds.All(p=>state.weaponNodeIds.Contains(p));
            var equipment=state.weaponEquipment.SingleOrDefault(e=>e.heroineId==hero);
            var equippedNode=nodes.Single(n=>n.id==(equipment?.nodeId??nodes.Single(x=>x.initial).id));
            SanctuaryHeader("神器  ／  誓いを育む木",combatDefinitions.Hero(hero).name+"の固有樹");
            if(GrowthButton(55,104,235,48,returnToFormationFromWeapon?"‹ 編成の設定へ":"‹ 能力へ戻る",BookInputAllowed && homeRequest==null && !formalCampaign.HasPending)){ReturnFromFormationWeapon();return;}
            GrowthFill(62,176,473,632,parchment);Label(90,197,414,55,selected.terminal,sanctuaryHeading);
            DrawWeaponEffect(new Rect(92,265,78,78),WeaponGrowthRules.Icon(selected),owned);
            Label(188,255,315,96,(selected.initial?"誓いの根":(owned?"取得済み":"未解放")+"  ／  Lv."+level+" / 7")+"\n"+((hero.StartsWith("heroine.annihilator",StringComparison.Ordinal) || hero=="heroine.shell" || hero=="heroine.oriflamme" || hero=="heroine.nighthawk" || hero=="heroine.slayer-swim" || hero=="heroine.arcane" || hero=="heroine.arcane-academy" || hero=="heroine.shangrila")?"攻撃スキル強化 ×":"通常攻撃 ")+ (WeaponGrowthRules.Power(selected,level)*100).ToString("0.#")+"%",sanctuaryBody);
            Label(90,235,418,25,WeaponGrowthRules.Branch(selected)+(selected.initial?"":" ／ "+WeaponGrowthRules.Tier(selected)+" / 4"),sanctuarySmall);
            Label(90,340,418,65,"装備："+(equippedNode.initial?"根":WeaponGrowthRules.UniqueAbility(equippedNode))+"\n選択："+(selected.initial?"根":WeaponGrowthRules.UniqueAbility(selected)),new GUIStyle(sanctuarySmall){fontSize=16});
            Label(90,405,335,25,weaponShowLevels?"Lv一覧":"装備比較",sanctuarySmall);
            if(WeaponCompactButton(432,405,81,24,weaponShowLevels?"比較":"全Lv",!selected.initial)){weaponShowLevels=!weaponShowLevels;weaponComparisonScroll=Vector2.zero;}
            var comparison=weaponShowLevels?string.Join("\n\n",Enumerable.Range(1,7).Select(lv=>"Lv."+lv+" ／ "+WeaponGrowthRules.Summary(selected,lv)+" ／ 技倍率 "+(WeaponGrowthRules.Power(selected,lv)*100).ToString("0.#")+"%")):WeaponGrowthRules.Compare(equippedNode,state.WeaponLevel(equippedNode.id),selected,level);
            var comparisonStyle=new GUIStyle(sanctuaryBody){fontSize=17};float comparisonHeight=Math.Max(130,comparisonStyle.CalcHeight(new GUIContent(comparison),385)+8);
            weaponComparisonScroll=GUI.BeginScrollView(new Rect(90,433,418,130),weaponComparisonScroll,new Rect(0,0,390,comparisonHeight));
            GUI.Label(new Rect(0,0,385,comparisonHeight),comparison,comparisonStyle);GUI.EndScrollView();
            if(!selected.initial && level<7){
                string next="Lv."+level+" → "+(level+1)+"\n"+WeaponGrowthRules.Compare(selected,level,selected,level+1);
                var nextStyle=new GUIStyle(sanctuarySmall){fontSize=15};float nextHeight=Math.Max(64,nextStyle.CalcHeight(new GUIContent(next),385)+4);
                weaponNextLevelScroll=GUI.BeginScrollView(new Rect(90,578,418,64),weaponNextLevelScroll,new Rect(0,0,390,nextHeight));
                GUI.Label(new Rect(0,0,385,nextHeight),next,nextStyle);GUI.EndScrollView();
            }else Label(90,578,418,64,selected.initial?"":"Lv.7 MAX",sanctuarySmall);
            var costs=selected.initial?Array.Empty<HomeCost>():owned && level<7?WeaponGrowthRules.Costs(selected,level,catalog):selected.costs;
            string cost=selected.initial?"":owned && level==7?"最大Lvです。":costs.Length==0?"初期神器は素材なしで取得できます。":string.Join("\n",costs.Select(c=>{var m=catalog.materials.Single(x=>x.id==c.resourceId);return (m.rarity>=4?"SSR":m.rarity==3?"SR":m.rarity==2?"R":"N")+" "+(m.name??NewAster.Data.WorldCatalog.Colossi.Single(x=>x.Id==m.colossusId).DisplayName+"素材")+"  "+c.amount+" / 所持 "+HomeRules.Balance(snapshot,c.resourceId);}));
            for(int i=0;i<costs.Length && !(owned && level==7);i++){
                var c=costs[i];var material=catalog.materials.Single(m=>m.id==c.resourceId);int balance=HomeRules.Balance(snapshot,c.resourceId);
                string name=material.name??NewAster.Data.WorldCatalog.Colossi.Single(x=>x.Id==material.colossusId).DisplayName;
                Label(90,650+i*24,335,24,name+" "+balance+" / "+c.amount+(balance<c.amount?" (-"+(c.amount-balance)+")":""),new GUIStyle(sanctuarySmall){fontSize=14});
                if(WeaponCompactButton(432,650+i*24,81,24,"入手先",BookInputAllowed && homeRequest==null))OpenWeaponMaterialSource(hero,material.colossusId);
            }
            bool sufficient=costs.All(c=>HomeRules.Balance(snapshot,c.resourceId)>=c.amount);
            if(!selected.initial && GrowthButton(83,728,430,55,owned?level==7?"Lv.7  MAX":"神器をLv."+(level+1)+"へ強化":!parents?"親の神器を解放してください":sufficient?"素材で神器を解放":"素材が不足しています",homeRequest==null && !formalCampaign.HasPending && (owned?level<7 && sufficient:parents && sufficient),true))ProposeHome(new HomeOperation(owned?"weapon-level":"weapon",selected.id,owned?(level+1).ToString():null));
            var treeRect=new Rect(571,173,965,640);GrowthFrame(treeRect.x,treeRect.y,treeRect.width,treeRect.height);
            var layout=WeaponTreeLayout(hero);var tree=SanctuaryTree(hero);var imageRect=ContainImage(new Rect(600,186,906,608),tree.width,tree.height);GUI.DrawTexture(imageRect,tree,ScaleMode.ScaleToFit,true);
            // Use the same native image rectangle for artwork and its authored branch anchors.
            var branchRect=imageRect;
            var path=new HashSet<string>();var pending=new Stack<string>();pending.Push(selected.id);
            while(pending.Count>0){string id=pending.Pop();if(!path.Add(id))continue;foreach(string parent in nodes.Single(n=>n.id==id).parentIds)pending.Push(parent);}
            foreach(var n in nodes)foreach(string parent in n.parentIds){var a=layout.Position(n.id);var b=layout.Position(parent);GrowthLine(branchRect.x+a.x*branchRect.width,branchRect.y+a.y*branchRect.height,branchRect.x+b.x*branchRect.width,branchRect.y+b.y*branchRect.height,path.Contains(n.id)?gold:new Color(.3f,.4f,.4f,.5f),path.Contains(n.id)?3:1);}
            for(int route=0;route<3;route++)Label(620+route*290,183,250,27,new[]{"攻撃","守護","技巧"}[route],new GUIStyle(growthSmallStyle){alignment=TextAnchor.MiddleCenter},gold);
            foreach(var n in nodes){var anchor=layout.Position(n.id);float x=branchRect.x+anchor.x*branchRect.width,y=branchRect.y+anchor.y*branchRect.height;bool acquired=state.weaponNodeIds.Contains(n.id),selectedNow=n.id==selected.id;var rect=new Rect(x-31,y-31,62,62);
                GrowthFill(x-34,y-30,68,60,new Color(.04f,.11f,.13f,.85f));DrawWeaponNodeEffects(x,y,n,acquired);
                bool wearing=equipment?.nodeId==n.id;
                if(wearing){var blue=new Color(.35f,.85f,1);GrowthLine(x-34,y-31,x+34,y-31,blue,3);GrowthLine(x-34,y+31,x+34,y+31,blue,3);GrowthLine(x-34,y-31,x-34,y+31,blue,3);GrowthLine(x+34,y-31,x+34,y+31,blue,3);Label(x-40,y-52,80,20,"装備",new GUIStyle(growthSmallStyle){fontSize=13,alignment=TextAnchor.MiddleCenter},blue);}
                bool ready=n.parentIds.All(p=>state.weaponNodeIds.Contains(p));bool affordable=n.costs.All(c=>HomeRules.Balance(snapshot,c.resourceId)>=c.amount);
                if(selectedNow)GrowthDiamond(x,y,38);GrowthFill(x-40,y+32,80,23,new Color(.025f,.05f,.075f,.94f));Label(x-40,y+32,80,23,n.initial?"根":acquired?"Lv."+state.WeaponLevel(n.id):!ready?"前提未達":affordable?"解放可":"素材不足",new GUIStyle(growthSmallStyle){fontSize=13,alignment=TextAnchor.MiddleCenter},acquired?gold:ready && affordable?new Color(.5f,1,.6f):ivory);
                if(ImageUiSkin.Button(new Rect(x-38,y-30,76,85),"",GUIStyle.none) && homeRequest==null && BookInputAllowed){selectedNode=n.id;weaponComparisonScroll=Vector2.zero;weaponNextLevelScroll=Vector2.zero;weaponShowLevels=false;PlayProductionUiSound("決定");}
            }
            bool equipped=state.weaponEquipment.Any(e=>e.heroineId==hero && e.nodeId==selected.id);
            if(GrowthButton(571,829,420,48,equipped?"装備中":"選択した神器を装備",owned && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip",selected.id,hero));
            if(GrowthButton(1280,829,254,48,"根を装備",state.weaponEquipment.Any(e=>e.heroineId==hero) && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip","unequip",hero));
            if(homeRequest!=null)DrawSanctuaryHomeConfirmation(cost);
        }
        private void DrawSanctuaryHomeConfirmation(string cost)
        {
            GrowthFill(0,0,1600,900,new Color(0,0,0,.65f));GrowthFrame(374,262,852,390);Label(412,289,770,48,homeOperation.Kind=="formation" || homeOperation.Kind=="commander" || homeOperation.Kind=="sniper-support"?"編成・役割の変更を確認":"神器の変更を確認",growthTitleStyle,gold);
            Label(412,351,772,180,HomeOperationSummary()+"\n"+(homeOperation.Kind=="equip"?"":cost)+"\n"+(homeError??""),new GUIStyle(growthTextStyle){fontSize=18});
            if(GrowthButton(412,548,500, sixty,formalCampaign.HasPending?"同じ内容で保存を再試行":"この内容で確定する",true,true))ConfirmHome();
            if(GrowthButton(930,548,256, sixty,"取消",!formalCampaign.HasPending)){homeRequest=null;homeOperation=null;homeError=null;}
        }
    }
}
