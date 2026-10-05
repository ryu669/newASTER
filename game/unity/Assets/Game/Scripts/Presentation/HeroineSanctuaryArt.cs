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
        // Original vector-like emblems and botanical artwork; source game pixels are never used.
        private sealed class BotanicalCanvas
        {
            private readonly int w,h;private readonly Color[] pixels;
            public BotanicalCanvas(int width,int height){w=width;h=height;pixels=new Color[w*h];}
            public void Disc(float x,float y,float rx,float ry,Color color)
            {
                for(int b=Math.Max(0,(int)(y-ry));b<Math.Min(h,y+ry+1);b++)for(int a=Math.Max(0,(int)(x-rx));a<Math.Min(w,x+rx+1);a++){
                    float distance=(a-x)*(a-x)/(rx*rx)+(b-y)*(b-y)/(ry*ry);if(distance>1)continue;float alpha=color.a*Mathf.Clamp01((1-distance)*5);int i=(h-1-b)*w+a;float total=alpha+pixels[i].a*(1-alpha);if(total<=0)continue;pixels[i]=new Color((color.r*alpha+pixels[i].r*pixels[i].a*(1-alpha))/total,(color.g*alpha+pixels[i].g*pixels[i].a*(1-alpha))/total,(color.b*alpha+pixels[i].b*pixels[i].a*(1-alpha))/total,total);
                }
            }
            public void Stroke(Vector2 a,Vector2 b,float width,Color color){int count=Math.Max(1,(int)Vector2.Distance(a,b)*2);for(int i=0;i<=count;i++){var p=Vector2.Lerp(a,b,i/(float)count);Disc(p.x,p.y,width,width,color);}}
            public void Curve(Vector2 a,Vector2 bend,Vector2 b,float start,float end,Color color){var prior=a;for(int i=1;i<=50;i++){float t=i/50f;var p=(1-t)*(1-t)*a+2*(1-t)*t*bend+t*t*b;Stroke(prior,p,Mathf.Lerp(start,end,t),color);prior=p;}}
            public Texture2D Texture(){var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};t.SetPixels(pixels);t.Apply(false,true);return t;}
        }
        private Texture2D SanctuaryIcon(string kind)
        {
            if(sanctuaryIcons.TryGetValue(kind,out var texture))return texture;var c=new BotanicalCanvas(96,96);var white=Color.white;
            c.Disc(48,48,46,46,new Color(1,1,1,.1f));
            for(int i=0;i<100;i++){float angle=i*Mathf.PI*2/100;c.Disc(48+42*Mathf.Cos(angle),48+42*Mathf.Sin(angle),1.5f,1.5f,white);}
            if(kind=="sword"){c.Stroke(new Vector2(29,69),new Vector2(67,28),3,white);c.Stroke(new Vector2(26,53),new Vector2(44,71),3,white);c.Stroke(new Vector2(26,72),new Vector2(21,78),3,white);c.Stroke(new Vector2(67,28),new Vector2(73,22),1.5f,white);}
            else if(kind=="leaf"){c.Curve(new Vector2(28,71),new Vector2(21,28),new Vector2(72,23),3,2,white);c.Curve(new Vector2(28,71),new Vector2(70,75),new Vector2(72,23),3,2,white);c.Stroke(new Vector2(24,77),new Vector2(66,32),2,white);c.Stroke(new Vector2(39,57),new Vector2(37,41),1.5f,white);c.Stroke(new Vector2(48,49),new Vector2(64,49),1.5f,white);}
            else if(kind=="moon"){c.Curve(new Vector2(63,22),new Vector2(10,39),new Vector2(60,75),5,4,white);c.Curve(new Vector2(63,22),new Vector2(30,47),new Vector2(60,75),3,3,white);c.Disc(68,44,4,4,white);c.Disc(58,55,2,2,white);}
            else if(kind=="crown"){var points=new[]{new Vector2(24,65),new Vector2(19,33),new Vector2(36,46),new Vector2(48,24),new Vector2(61,46),new Vector2(78,33),new Vector2(72,65),new Vector2(24,65)};for(int i=1;i<points.Length;i++)c.Stroke(points[i-1],points[i],2.5f,white);c.Stroke(new Vector2(27,72),new Vector2(70,72),2.5f,white);}
            else if(kind=="flame"){c.Curve(new Vector2(48,19),new Vector2(17,44),new Vector2(29,67),3,3,white);c.Curve(new Vector2(48,19),new Vector2(44,46),new Vector2(65,33),3,3,white);c.Curve(new Vector2(65,33),new Vector2(87,82),new Vector2(29,67),3,3,white);c.Curve(new Vector2(48,43),new Vector2(30,71),new Vector2(54,75),2,2,white);}
            else if(kind=="resource"){for(int i=0;i<3;i++){float x=26+i*22;c.Stroke(new Vector2(x,27),new Vector2(x-8,46),2,white);c.Stroke(new Vector2(x-8,46),new Vector2(x,68),2,white);c.Stroke(new Vector2(x,68),new Vector2(x+8,46),2,white);c.Stroke(new Vector2(x+8,46),new Vector2(x,27),2,white);}}
            else{for(int i=0;i<5;i++){float a=-Mathf.PI/2+i*Mathf.PI*2/5,b=a+Mathf.PI*4/5;c.Stroke(new Vector2(48+27*Mathf.Cos(a),48+27*Mathf.Sin(a)),new Vector2(48+27*Mathf.Cos(b),48+27*Mathf.Sin(b)),2,white);}c.Disc(48,48,5,5,white);}
            texture=c.Texture();sanctuaryIcons[kind]=texture;return texture;
        }
        private void DrawSanctuaryIcon(Rect rect,string kind,Color color){var prior=GUI.color;GUI.color=color;GUI.DrawTexture(rect,SanctuaryIcon(kind),ScaleMode.ScaleToFit,true);GUI.color=prior;}
        private Color HeroineLeafColor(string id)=>id=="heroine.slayer"?new Color(.87f,.54f,.66f):id=="heroine.iconoclast"?new Color(.49f,.40f,.77f):id=="heroine.undermine"?new Color(.39f,.69f,.46f):id=="heroine.echidna"?new Color(.86f,.40f,.28f):new Color(.43f,.65f,.79f);
        private Texture2D SanctuaryTree(string hero,HomeWeaponNode[] nodes)
        {
            if(sanctuaryTrees.TryGetValue(hero,out var texture))return texture;
            texture=Resources.Load<Texture2D>("Illustrations/"+hero.Replace("heroine.","")+"-equipment-tree-v1");
            if(texture==null)throw new InvalidOperationException("神器の木の美術素材がありません："+hero);
            sanctuaryTrees[hero]=texture;return texture;
        }

        private void DrawSanctuaryWeaponTree(string hero)
        {
            var catalog=HomeData();var nodes=catalog.weaponNodes.Where(n=>n.heroineId==hero).ToArray();var state=HomeState;var snapshot=formalCampaign.Snapshot;
            if(selectedNode==null || !nodes.Any(n=>n.id==selectedNode))selectedNode=state.weaponEquipment.SingleOrDefault(e=>e.heroineId==hero)?.nodeId??nodes[0].id;
            var selected=nodes.Single(n=>n.id==selectedNode);bool owned=state.weaponNodeIds.Contains(selected.id);int level=state.WeaponLevel(selected.id);bool parents=selected.parentIds.All(p=>state.weaponNodeIds.Contains(p));
            SanctuaryHeader("神器  ／  誓いを育む木",combatDefinitions.Hero(hero).name+"の固有樹");
            if(GrowthButton(55,104,235,48,"‹ 能力へ戻る",BookInputAllowed)){growthScreen=GrowthScreen.Overview;return;}
            Label(324,113,1140,38,"枝を選び、神器を解放・強化・装備する。全13ノード、各神器Lv1〜7。",growthSmallStyle);
            GrowthFill(62,176,473,632,parchment);Label(90,197,414,55,selected.terminal,sanctuaryHeading);
            DrawSanctuaryIcon(new Rect(92,265,78,78),"sword",new Color(.50f,.39f,.23f));
            Label(188,255,315,96,(owned?"取得済み":"未解放")+"  ／  Lv."+level+" / 7\n通常攻撃 "+(WeaponGrowthRules.Power(selected,level)*100).ToString("0.#")+"%",sanctuaryBody);
            Label(90,340,418,65,WeaponGrowthRules.Trait(selected),sanctuarySmall);
            for(int lv=1;lv<=7;lv++){
                float y=405+(lv-1)*34;GrowthFill(84,y,429,32,owned && level>=lv?new Color(.84f,.80f,.66f):new Color(.90f,.87f,.78f));
                string effects=WeaponGrowthRules.Summary(selected,lv);if(selected.speedBonus>0)effects=effects.Replace("攻撃","攻").Replace("速度","速").Replace(" ／ ","  ");
                Label(94,y,413,33,"Lv."+lv+"  "+effects,new GUIStyle(sanctuarySmall){fontSize=14,wordWrap=false});
            }
            var costs=owned && level<7?WeaponGrowthRules.Costs(selected,level,catalog):selected.costs;
            string cost=owned && level==7?"最大Lvです。":costs.Length==0?"初期神器は素材なしで取得できます。":string.Join("\n",costs.Select(c=>{var m=catalog.materials.Single(x=>x.id==c.resourceId);return (m.rarity>=4?"SSR":m.rarity==3?"SR":m.rarity==2?"R":"N")+" "+(m.name??NewAster.Data.WorldCatalog.Colossi.Single(x=>x.Id==m.colossusId).DisplayName+"素材")+"  "+c.amount+" / 所持 "+HomeRules.Balance(snapshot,c.resourceId);}));
            Label(90,646,418,78,cost,new GUIStyle(sanctuarySmall){fontSize=14});
            bool sufficient=costs.All(c=>HomeRules.Balance(snapshot,c.resourceId)>=c.amount);
            if(GrowthButton(83,728,430,55,owned?level==7?"Lv.7  MAX":"神器をLv."+(level+1)+"へ強化":!parents?"親の神器を解放してください":sufficient?"素材で神器を解放":"素材が不足しています",homeRequest==null && !formalCampaign.HasPending && (owned?level<7 && sufficient:parents && sufficient),true))ProposeHome(new HomeOperation(owned?"weapon-level":"weapon",selected.id,owned?(level+1).ToString():null));
            var treeRect=new Rect(571,173,965,640);GrowthFrame(treeRect.x,treeRect.y,treeRect.width,treeRect.height);GUI.DrawTexture(new Rect(610,166,875,637),SanctuaryTree(hero,nodes),ScaleMode.StretchToFill,true);
            foreach(var n in nodes)foreach(string p in n.parentIds){var parent=nodes.Single(x=>x.id==p);GrowthLine(644+parent.treePosition.x*794,199+parent.treePosition.y*536,644+n.treePosition.x*794,199+n.treePosition.y*536,state.weaponNodeIds.Contains(n.id)?new Color(.94f,.82f,.49f,.7f):new Color(.64f,.61f,.47f,.4f),2);}
            foreach(var n in nodes){float x=644+n.treePosition.x*794,y=199+n.treePosition.y*536;bool acquired=state.weaponNodeIds.Contains(n.id),selectedNow=n.id==selected.id;var rect=new Rect(x-31,y-31,62,62);
                GrowthFill(x-36,y-36,72,72,new Color(.04f,.11f,.13f,.80f));DrawSanctuaryIcon(rect,n.initial?"leaf":n.id.EndsWith("tier4")?"crown":"sword",selectedNow?ivory:acquired?gold:muted);
                if(selectedNow)GrowthDiamond(x,y,43);GrowthFill(x-58,y+34,116,30,new Color(.025f,.05f,.075f,.94f));Label(x-58,y+36,116,27,acquired?state.WeaponLevel(n.id)==7?"MAX":"Lv."+state.WeaponLevel(n.id):"未解放",new GUIStyle(growthSmallStyle){fontSize=16,alignment=TextAnchor.MiddleCenter},acquired?gold:ivory);
                if(GUI.Button(new Rect(x-38,y-38,76,100),"",GUIStyle.none) && homeRequest==null && BookInputAllowed){selectedNode=n.id;PlayProductionUiSound("決定");}
            }
            bool equipped=state.weaponEquipment.Any(e=>e.heroineId==hero && e.nodeId==selected.id);
            if(GrowthButton(1014,829,245,48,equipped?"装備中":"選択した神器を装備",owned && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip",selected.id,hero));
            if(GrowthButton(1280,829,254,48,"装備を外す",state.weaponEquipment.Any(e=>e.heroineId==hero) && homeRequest==null && BookInputAllowed))ProposeHome(new HomeOperation("equip","unequip",hero));
            Label(62,843,900,33,"取得済みの枝は失われません。装備は1人1神器。",growthSmallStyle);
            if(homeRequest!=null)DrawSanctuaryHomeConfirmation(cost);
        }
        private void DrawSanctuaryHomeConfirmation(string cost)
        {
            GrowthFill(0,0,1600,900,new Color(0,0,0,.65f));GrowthFrame(374,262,852,390);Label(412,289,770,48,homeOperation.Kind=="formation"?"編成の変更を確認":"神器の変更を確認",growthTitleStyle,gold);
            Label(412,351,772,180,HomeOperationSummary()+"\n"+(homeOperation.Kind=="equip"?"装備変更は無消費です。":cost)+"\n"+(homeError??"保存成功後に確定。取消では素材を消費しません。"),new GUIStyle(growthTextStyle){fontSize=18});
            if(GrowthButton(412,548,500, sixty,formalCampaign.HasPending?"同じ内容で保存を再試行":"この内容で確定する",true,true))ConfirmHome();
            if(GrowthButton(930,548,256, sixty,"取消",!formalCampaign.HasPending)){homeRequest=null;homeOperation=null;homeError=null;}
        }
    }
}
