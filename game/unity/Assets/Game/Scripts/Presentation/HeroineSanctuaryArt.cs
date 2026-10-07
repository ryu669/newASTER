using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly Dictionary<string,Texture2D> sanctuaryIcons=new Dictionary<string,Texture2D>(),sanctuaryTrees=new Dictionary<string,Texture2D>();
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
        private NewAster.Data.HeroineWeaponTreeCatalog weaponTreeCatalog;
        private Texture2D weaponEffectAtlas;
        private NewAster.Data.HeroineWeaponTreeDef WeaponTreeLayout(string hero)
        {
            if(weaponTreeCatalog==null){var json=Resources.Load<TextAsset>("UI/heroine-weapon-trees");if(json==null)throw new InvalidOperationException("Missing weapon tree image layout catalog.");weaponTreeCatalog=JsonUtility.FromJson<NewAster.Data.HeroineWeaponTreeCatalog>(json.text);weaponTreeCatalog.Validate();}
            return weaponTreeCatalog.Entry(hero);
        }
        private Texture2D SanctuaryTree(string hero)
        {
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
            var kinds=WeaponGrowthRules.EffectKinds(node);int cols=Math.Min(3,kinds.Length);float size=kinds.Length<=2?32:24;int rows=(kinds.Length+cols-1)/cols;
            for(int i=0;i<kinds.Length;i++)DrawWeaponEffect(new Rect(x-cols*size/2+(i%cols)*size,y-rows*size/2+(i/cols)*size,size,size),kinds[i],acquired);
        }

        private void DrawSanctuaryWeaponTree(string hero)
        {
            var catalog=HomeData();var nodes=catalog.weaponNodes.Where(n=>n.heroineId==hero).ToArray();var state=HomeState;var snapshot=formalCampaign.Snapshot;
            if(selectedNode==null || !nodes.Any(n=>n.id==selectedNode))selectedNode=state.weaponEquipment.SingleOrDefault(e=>e.heroineId==hero)?.nodeId??nodes[0].id;
            var selected=nodes.Single(n=>n.id==selectedNode);bool owned=state.weaponNodeIds.Contains(selected.id);int level=state.WeaponLevel(selected.id);bool parents=selected.parentIds.All(p=>state.weaponNodeIds.Contains(p));
            SanctuaryHeader("神器  ／  誓いを育む木",combatDefinitions.Hero(hero).name+"の固有樹");
            if(GrowthButton(55,104,235,48,returnToFormationFromWeapon?"‹ 編成の設定へ":"‹ 能力へ戻る",BookInputAllowed && homeRequest==null && !formalCampaign.HasPending)){ReturnFromFormationWeapon();return;}
            GrowthFill(62,176,473,632,parchment);Label(90,197,414,55,selected.terminal,sanctuaryHeading);
            DrawSanctuaryIcon(new Rect(92,265,78,78),"sword",new Color(.50f,.39f,.23f));
            Label(188,255,315,96,(owned?"取得済み":"未解放")+"  ／  Lv."+level+" / 7\n"+((hero.StartsWith("heroine.annihilator",StringComparison.Ordinal) || hero=="heroine.shell" || hero=="heroine.oriflamme" || hero=="heroine.nighthawk" || hero=="heroine.slayer-swim" || hero=="heroine.arcane" || hero=="heroine.arcane-academy" || hero=="heroine.shangrila")?"攻撃スキル強化 ×":"通常攻撃 ")+ (WeaponGrowthRules.Power(selected,level)*100).ToString("0.#")+"%",sanctuaryBody);
            Label(90,340,418,65,WeaponGrowthRules.Trait(selected),sanctuarySmall);
            for(int lv=1;lv<=7;lv++){
                float y=405+(lv-1)*34;GrowthFill(84,y,429,32,owned && level>=lv?new Color(.84f,.80f,.66f):new Color(.90f,.87f,.78f));
                string effects=WeaponGrowthRules.Summary(selected,lv);if(selected.speedBonus>0)effects=effects.Replace("攻撃","攻").Replace("速度","速").Replace(" ／ ","  ");
                Label(94,y,413,33,"Lv."+lv+"  "+effects,new GUIStyle(sanctuarySmall){fontSize=12,wordWrap=false});
            }
            var costs=owned && level<7?WeaponGrowthRules.Costs(selected,level,catalog):selected.costs;
            string cost=owned && level==7?"最大Lvです。":costs.Length==0?"初期神器は素材なしで取得できます。":string.Join("\n",costs.Select(c=>{var m=catalog.materials.Single(x=>x.id==c.resourceId);return (m.rarity>=4?"SSR":m.rarity==3?"SR":m.rarity==2?"R":"N")+" "+(m.name??NewAster.Data.WorldCatalog.Colossi.Single(x=>x.Id==m.colossusId).DisplayName+"素材")+"  "+c.amount+" / 所持 "+HomeRules.Balance(snapshot,c.resourceId);}));
            Label(90,646,418,78,cost,new GUIStyle(sanctuarySmall){fontSize=14});
            bool sufficient=costs.All(c=>HomeRules.Balance(snapshot,c.resourceId)>=c.amount);
            if(GrowthButton(83,728,430,55,owned?level==7?"Lv.7  MAX":"神器をLv."+(level+1)+"へ強化":!parents?"親の神器を解放してください":sufficient?"素材で神器を解放":"素材が不足しています",homeRequest==null && !formalCampaign.HasPending && (owned?level<7 && sufficient:parents && sufficient),true))ProposeHome(new HomeOperation(owned?"weapon-level":"weapon",selected.id,owned?(level+1).ToString():null));
            var treeRect=new Rect(571,173,965,640);GrowthFrame(treeRect.x,treeRect.y,treeRect.width,treeRect.height);
            var layout=WeaponTreeLayout(hero);var tree=SanctuaryTree(hero);var imageRect=ContainImage(new Rect(600,186,906,608),tree.width,tree.height);GUI.DrawTexture(imageRect,tree,ScaleMode.ScaleToFit,true);
            // Use the same native image rectangle for artwork and its authored branch anchors.
            var branchRect=imageRect;
            foreach(var n in nodes){var anchor=layout.Position(n.id);float x=branchRect.x+anchor.x*branchRect.width,y=branchRect.y+anchor.y*branchRect.height;bool acquired=state.weaponNodeIds.Contains(n.id),selectedNow=n.id==selected.id;var rect=new Rect(x-31,y-31,62,62);
                GrowthFill(x-34,y-30,68,60,new Color(.04f,.11f,.13f,.85f));DrawWeaponNodeEffects(x,y,n,acquired);
                if(selectedNow)GrowthDiamond(x,y,38);GrowthFill(x-58,y+32,116,23,new Color(.025f,.05f,.075f,.94f));Label(x-58,y+32,116,23,acquired?state.WeaponLevel(n.id)==7?"MAX":"Lv."+state.WeaponLevel(n.id):"未解放",new GUIStyle(growthSmallStyle){fontSize=14,alignment=TextAnchor.MiddleCenter},acquired?gold:ivory);
                if(ImageUiSkin.Button(new Rect(x-38,y-30,76,85),"",GUIStyle.none) && homeRequest==null && BookInputAllowed){selectedNode=n.id;PlayProductionUiSound("決定");}
            }
            bool equipped=state.weaponEquipment.Any(e=>e.heroineId==hero && e.nodeId==selected.id);
            if(GrowthButton(1014,829,245,48,equipped?"装備中":"選択した神器を装備",owned && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip",selected.id,hero));
            if(GrowthButton(1280,829,254,48,"装備を外す",state.weaponEquipment.Any(e=>e.heroineId==hero) && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip","unequip",hero));
            if(homeRequest!=null)DrawSanctuaryHomeConfirmation(cost);
        }
        private void DrawSanctuaryHomeConfirmation(string cost)
        {
            GrowthFill(0,0,1600,900,new Color(0,0,0,.65f));GrowthFrame(374,262,852,390);Label(412,289,770,48,homeOperation.Kind=="formation" || homeOperation.Kind=="commander" || homeOperation.Kind=="sniper-support"?"編成・役割の変更を確認":"神器の変更を確認",growthTitleStyle,gold);
            Label(412,351,772,180,HomeOperationSummary()+"\n"+(homeOperation.Kind=="equip"?"装備変更は無消費です。":cost)+"\n"+(homeError??"保存成功後に確定。取消では素材を消費しません。"),new GUIStyle(growthTextStyle){fontSize=18});
            if(GrowthButton(412,548,500, sixty,formalCampaign.HasPending?"同じ内容で保存を再試行":"この内容で確定する",true,true))ConfirmHome();
            if(GrowthButton(930,548,256, sixty,"取消",!formalCampaign.HasPending)){homeRequest=null;homeOperation=null;homeError=null;}
        }
    }
}
