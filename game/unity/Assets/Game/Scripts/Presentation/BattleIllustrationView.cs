using System;
using System.Linq;
using UnityEngine;
using NewAster.Core;
using NewAster.Data;
namespace NewAster.Presentation
{
    // Rendering only: never draws RNG, changes HP, or grants rewards.
    public sealed class BattleIllustrationView
    {
        private BattleIllustrationManifest manifest;
        private Texture2D[] portraits;
        private string warning;
        public BattleIllustrationView()
        {
            try {
                var source=Resources.Load<TextAsset>("Illustrations/battle-preview");
                if(source==null) throw new ArgumentException("battle-preview manifest missing");
                manifest=JsonUtility.FromJson<BattleIllustrationManifest>(source.text);manifest.Validate();
                portraits=manifest.heroes.Select(h=>string.IsNullOrEmpty(h.resourcePath)?null:Resources.Load<Texture2D>(h.resourcePath)).ToArray();
                for(int i=0;i<5;i++) if(!string.IsNullOrEmpty(manifest.heroes[i].resourcePath) && portraits[i]==null) throw new ArgumentException("Image missing: "+manifest.heroes[i].resourcePath);
            } catch(Exception e) { manifest=null;portraits=null;warning=e.Message;Debug.LogWarning("ILLUSTRATION_MANIFEST_WARNING "+warning); }
        }
        public static int DisplayActor(int available,BattlePresentationEvent e) => e!=null?e.Actor:available;
        private static void Fill(Rect rect,Color color) { var saved=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=saved; }
        public string Draw(PlayableBattle battle,BattlePresentationEvent e,float elapsed,string target,bool canSelect,GUIStyle style,GUIStyle small,string[] names,string[] partNames)
        {
            Fill(new Rect(0,330,1152,372),new Color(.08f,.14f,.19f));
            int actor=DisplayActor(battle.AvailableHero,e);
            float progress=e==null?0:BattleVisualCue.Progress(elapsed,e.Kind,e.Major);
            float movement=e!=null && e.Kind==BattlePresentationKind.Attack?24*Mathf.Sin(progress*Mathf.PI):0;
            var actorRect=new Rect(16+movement,339,470,335);
            if(actor>=0 && actor<5) {
                int binding=manifest!=null?manifest.HeroIndex(battle.State.Heroes[actor].Id):-1;
                Texture2D art=binding>=0?portraits[binding]:null;
                if(art!=null) GUI.DrawTextureWithTexCoords(actorRect,art,new Rect(380f/1672,1-750f/941,912f/1672,650f/941));
                else {
                    Fill(actorRect,new Color(.15f,.23f,.29f));
                    GUI.Label(new Rect(44,454,416,90),names[actor]+"\n人物イラスト未制作",style);
                }
                Fill(new Rect(16,639,470,48),new Color(.035f,.065f,.08f,.92f));
                GUI.Label(new Rect(30,646,445,35),"行動者 "+(actor+1)+" / "+names[actor]+(art!=null && manifest.heroes[binding].placeholder?"（候補絵）":""),small);
            } else GUI.Label(new Rect(40,458,435,75),"巨神獣の行動\n対象の状態は下部カードへ",style);
            var enemy=new Rect(538,339,590,340);
            Fill(enemy,new Color(.12f,.21f,.22f));
            GUI.Label(new Rect(670,447,320,55),"巨神獣：部位配置の仮表示",small);
            if(canSelect && GUI.Button(new Rect(735,487,195,64),(target=="body"?"◆ ":"")+"本体")) target="body";
            else if(!canSelect) GUI.Label(new Rect(745,500,190,40),"本体",style);
            if(warning!=null) GUI.Label(new Rect(548,610,570,65),"素材警告："+warning,small);
            if(manifest!=null) foreach(var p in manifest.parts) {
                int index=battle.State.Parts.ToList().FindIndex(part=>part.Id==p.partId);
                if(index<0) { GUI.Label(new Rect(548,610,550,55),"部位IDが戦闘定義と不一致",small);continue; }
                int hp=e?.PartHp[index]??battle.State.Parts[index].HitPoints;
                var rect=new Rect(enemy.x+p.x*enemy.width,enemy.y+p.y*enemy.height,p.width*enemy.width,p.height*enemy.height);
                var old=GUI.enabled;GUI.enabled=canSelect && hp>0;
                if(GUI.Button(rect,(target==p.partId?"◆ ":"")+partNames[index]+"\n"+(hp==0?"破壊済み":"HP "+hp))) target=p.partId;
                GUI.enabled=old;
            }
            if(e!=null && e.Damage>0) GUI.Label(new Rect(855,579,260,45),"−"+e.Damage+" / "+e.Target,small);
            if(e!=null && e.Kind==BattlePresentationKind.CastStart) GUI.Label(new Rect(50,592,400,40),"詠唱開始（まだダメージなし）",small);
            GUI.Label(new Rect(16,683,1110,22),"2D表示基盤 / 背景・敵・残り4人は未制作。候補絵は正式採用前。",small);
            return target;
        }
    }
}
