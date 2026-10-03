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
        private Texture2D background,body,middle,foreground,enemyMajor;
        private readonly string[] args=Environment.GetCommandLineArgs();
        private string Inspection { get {int i=Array.IndexOf(args,"-inspectPlan7Battle");return i>=0 && i+1<args.Length?args[i+1]:null;} }
        private System.Collections.Generic.Dictionary<string,Texture2D> layers=new System.Collections.Generic.Dictionary<string,Texture2D>();
        private Texture2D LoadLayer(string path)
        {
            if(string.IsNullOrWhiteSpace(path)) return null;
            if(layers.TryGetValue(path,out var found)) return found;
            var image=Resources.Load<Texture2D>(path);
            if(image==null) throw new ArgumentException("Image missing: "+path);
            layers.Add(path,image);return image;
        }
        public BattleIllustrationView(string resource="Illustrations/battle-preview")
        {
            try {
                var source=Resources.Load<TextAsset>(resource);
                if(source==null) throw new ArgumentException("battle-preview manifest missing");
                manifest=JsonUtility.FromJson<BattleIllustrationManifest>(source.text);manifest.Validate();
                portraits=manifest.heroes.Select(h=>string.IsNullOrEmpty(h.resourcePath)?null:Resources.Load<Texture2D>(h.resourcePath)).ToArray();
                background=LoadLayer(manifest.backgroundResourcePath);body=LoadLayer(manifest.bodyResourcePath);
                middle=LoadLayer(manifest.middleResourcePath);foreground=LoadLayer(manifest.foregroundResourcePath);
                enemyMajor=LoadLayer(manifest.enemyMajorResourcePath);
                foreach(var hero in manifest.heroes)foreach(var path in new[]{hero.attackResourcePath,hero.hitResourcePath,hero.cutinResourcePath})LoadLayer(path);
                foreach(var part in manifest.parts) {
                    foreach(var path in new[]{part.resourcePath,part.destroyedResourcePath}) {
                        var layer=LoadLayer(path);
                        if(layer!=null && (body==null || layer.width!=body.width || layer.height!=body.height))
                            throw new ArgumentException("Enemy layer canvas mismatch: "+path);
                    }
                }
                for(int i=0;i<5;i++) if(!string.IsNullOrEmpty(manifest.heroes[i].resourcePath) && portraits[i]==null) throw new ArgumentException("Image missing: "+manifest.heroes[i].resourcePath);
            } catch(Exception e) { manifest=null;portraits=null;background=null;body=null;middle=null;foreground=null;enemyMajor=null;layers.Clear();warning=e.Message;Debug.LogWarning("ILLUSTRATION_MANIFEST_WARNING "+warning); }
        }
        private void DrawEnemyLayer(PartIllustrationBinding part,Rect canvas,PlayableBattle battle,BattlePresentationEvent e)
        {
            int index=battle.State.Parts.ToList().FindIndex(p=>p.Id==part.partId);
            if(index<0) return;
            int hp=e?.PartHp[index]??battle.State.Parts[index].HitPoints;
            if(Inspection==part.partId)hp=0; // diagnostic drawing only, state remains unchanged
            string path=BattleIllustrationManifest.PartResource(part,hp);
            if(path!=null && layers.TryGetValue(path,out var art)) GUI.DrawTexture(canvas,art,ScaleMode.ScaleToFit,true);
        }
        public static int DisplayActor(int available,BattlePresentationEvent e) => e!=null?e.Actor:available;
        public void DrawEnemyPreview(Rect canvas,int brokenMask)
        {
            if(manifest==null || body==null)return;
            foreach(var part in manifest.parts.Where(p=>p.drawOrder<0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal))DrawPreviewPart(part,canvas,brokenMask);
            GUI.DrawTexture(canvas,body,ScaleMode.ScaleToFit,true);
            foreach(var part in manifest.parts.Where(p=>p.drawOrder>=0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal))DrawPreviewPart(part,canvas,brokenMask);
        }
        public void DrawEnemyMajorPreview(Rect canvas){if(enemyMajor!=null)GUI.DrawTexture(canvas,enemyMajor,ScaleMode.ScaleToFit,true);}
        private void DrawPreviewPart(PartIllustrationBinding part,Rect canvas,int mask)
        {int index=Array.IndexOf(manifest.parts,part);string path=BattleIllustrationManifest.PartResource(part,(mask&(1<<index))!=0?0:1);if(path!=null && layers.TryGetValue(path,out var art))GUI.DrawTexture(canvas,art,ScaleMode.ScaleToFit,true);}
        private static void Fill(Rect rect,Color color) { var saved=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=saved; }
        public string Draw(PlayableBattle battle,BattlePresentationEvent e,float elapsed,string target,bool canSelect,GUIStyle style,GUIStyle small,string[] names,string[] partNames)
        {
            style=new GUIStyle(style);small=new GUIStyle(small);style.normal.textColor=Color.white;small.normal.textColor=new Color(.97f,.94f,.83f);
            Fill(new Rect(0,330,1152,372),new Color(.08f,.14f,.19f));
            if(background!=null) GUI.DrawTexture(new Rect(0,330,1152,372),background,ScaleMode.ScaleAndCrop);
            float parallax=ArtSampleSettings.ReducedMotion?0:Mathf.Sin(elapsed*.7f)*3;
            if(middle!=null)GUI.DrawTexture(new Rect(-4+parallax,330,1160,372),middle,ScaleMode.StretchToFill,true);
            int actor=DisplayActor(battle.AvailableHero,e);
            // Asset inspection only: never changes the engine's available actor or command target.
            bool inspectStanding=args.Contains("-inspectPlan7Standing") || Inspection!=null;
            if(inspectStanding) actor=battle.State.Heroes.ToList().FindIndex(h=>h.Id=="heroine.slayer");
            bool victim=e!=null && e.Kind==BattlePresentationKind.Enemy && e.TargetIds.Contains("heroine.slayer");
            if(victim && !inspectStanding)actor=battle.State.Heroes.ToList().FindIndex(h=>h.Id=="heroine.slayer");
            float progress=e==null?0:BattleVisualCue.Progress(elapsed,e.Kind,e.Major);
            float movement=!ArtSampleSettings.ReducedMotion && e!=null && e.Kind==BattlePresentationKind.Attack?24*Mathf.Sin(progress*Mathf.PI):0;
            var actorRect=new Rect(16+movement,339,470,335);
            if(actor>=0 && actor<5) {
                int binding=manifest!=null?manifest.HeroIndex(battle.State.Heroes[actor].Id):-1;
                Texture2D art=binding>=0?portraits[binding]:null;
                if(binding>=0){var hero=manifest.heroes[binding];string path=Inspection=="hit" || victim?hero.hitResourcePath:Inspection=="cutin" || e!=null && e.Actor==actor && (e.Major || e.FullChain)?hero.cutinResourcePath:Inspection=="attack" || e!=null && e.Actor==actor && (e.Kind==BattlePresentationKind.Attack || e.Kind==BattlePresentationKind.CastRelease)?hero.attackResourcePath:null;
                    if(path!=null && layers.TryGetValue(path,out var pose))art=pose;}
                if(art!=null) {
                    if(manifest.heroes[binding].fullCanvas) GUI.DrawTexture(new Rect(actorRect.x,actorRect.y,actorRect.width,300),art,ScaleMode.ScaleToFit,true);
                    else GUI.DrawTextureWithTexCoords(actorRect,art,new Rect(380f/1672,1-750f/941,912f/1672,650f/941));
                }
                else {
                    Fill(actorRect,new Color(.15f,.23f,.29f));
                    GUI.Label(new Rect(44,454,416,90),names[actor]+"\n人物イラスト未制作",style);
                }
            } else GUI.Label(new Rect(40,458,435,75),"巨神獣の行動\n対象の状態は下部カードへ",style);
            var enemy=body==null?new Rect(538,339,590,340):new Rect(650,339,340,340);
            if(body==null) {
                Fill(enemy,new Color(.12f,.21f,.22f));
                GUI.Label(new Rect(670,447,320,55),"巨神獣：部位配置の仮表示",small);
            } else if(e!=null && e.Kind==BattlePresentationKind.Enemy && e.Major && e.PartHp.All(hp=>hp>0) && enemyMajor!=null){GUI.DrawTexture(enemy,enemyMajor,ScaleMode.ScaleToFit,true);} else {
                foreach(var part in manifest.parts.Where(p=>p.drawOrder<0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal)) DrawEnemyLayer(part,enemy,battle,e);
                GUI.DrawTexture(enemy,body,ScaleMode.ScaleToFit,true);
                foreach(var part in manifest.parts.Where(p=>p.drawOrder>=0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal)) DrawEnemyLayer(part,enemy,battle,e);
            }
            if(body!=null && manifest!=null && canSelect && Event.current.type==EventType.MouseDown && Event.current.button==0 && enemy.Contains(Event.current.mousePosition)) {
                var mouse=Event.current.mousePosition;
                string hit=manifest.HitPart((mouse.x-enemy.x)/enemy.width,(mouse.y-enemy.y)/enemy.height,id=>battle.State.Parts.Any(p=>p.Id==id && p.HitPoints>0));
                if(hit!=null){target=hit;Event.current.Use();}
                else if(new Rect(785,470,85,170).Contains(mouse)){target="body";Event.current.Use();}
            }
            if(body==null && canSelect && GUI.Button(new Rect(735,487,195,64),(target=="body"?"◆ ":"")+"本体")) target="body";
            else if(!canSelect && body==null) GUI.Label(new Rect(745,500,190,40),"本体",style);
            if(warning!=null) GUI.Label(new Rect(548,610,570,65),"素材警告："+warning,small);
            if(manifest!=null) foreach(var p in manifest.parts) {
                int index=battle.State.Parts.ToList().FindIndex(part=>part.Id==p.partId);
                if(index<0) { GUI.Label(new Rect(548,610,550,55),"部位IDが戦闘定義と不一致",small);continue; }
                int hp=Inspection==p.partId?0:e?.PartHp[index]??battle.State.Parts[index].HitPoints;
                var rect=new Rect(enemy.x+p.x*enemy.width,enemy.y+p.y*enemy.height,p.width*enemy.width,p.height*enemy.height);
                var old=GUI.enabled;GUI.enabled=canSelect && hp>0;
                if(body==null && GUI.Button(rect,(target==p.partId?"◆ ":"")+partNames[index]+"\n"+(hp==0?"破壊済み":"HP "+hp))) target=p.partId;
                if(body!=null && (target==p.partId || rect.Contains(Event.current.mousePosition))) {Fill(new Rect(rect.x,rect.y,rect.width,2),new Color(1,.85f,.35f));Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),new Color(1,.85f,.35f));}
                GUI.enabled=old;
            }
            var unplaced=battle.State.Parts.Select((part,index)=>new{part,index})
                .Where(p=>manifest==null || !manifest.parts.Any(binding=>binding.partId==p.part.Id)).ToArray();
            for(int slot=0;slot<unplaced.Length;slot++) {
                var p=unplaced[slot];int hp=e?.PartHp[p.index]??p.part.HitPoints;
                // Missing art bindings remain selectable; the header always lists every part.
                bool sideSlots=manifest!=null && unplaced.Length<=2;
                float width=sideSlots?140:560f/System.Math.Min(3,unplaced.Length);
                var rect=new Rect(sideSlots?548+slot*430:548+(slot%3)*width,sideSlots?612:568+(slot/3)*56,width-8,50);
                var old=GUI.enabled;GUI.enabled=canSelect && hp>0;
                if(GUI.Button(rect,(target==p.part.Id?"◆ ":"")+partNames[p.index]+"\n"+(hp==0?"破壊済み":"HP "+hp))) target=p.part.Id;
                GUI.enabled=old;
            }
            if(foreground!=null)GUI.DrawTexture(new Rect(0,330,1152,372),foreground,ScaleMode.StretchToFill,true);
            if(e!=null && !ArtSampleSettings.ReducedFlash && elapsed<.25f){Color tint=e.PartBroken?new Color(1,.65f,.2f,.12f):e.Kind==BattlePresentationKind.Healing?new Color(.25f,1,.55f,.10f):e.Kind==BattlePresentationKind.Support?new Color(.3f,.65f,1,.10f):new Color(1,1,1,.06f);Fill(new Rect(0,330,1152,372),tint);}
            if(actor>=0 && actor<5){int binding=manifest!=null?manifest.HeroIndex(battle.State.Heroes[actor].Id):-1;
                Fill(new Rect(16,639,470,40),new Color(.035f,.065f,.08f,.92f));
                GUI.Label(new Rect(30,643,445,32),(inspectStanding?"素材確認 / ":victim?"被弾対象 / ":"行動者 "+(actor+1)+" / ")+names[actor]+(binding>=0 && portraits[binding]!=null && manifest.heroes[binding].placeholder?"（候補絵）":""),small);}
            if(e!=null && (e.Damage>0 || e.Kind==BattlePresentationKind.CastStart)) {
                Fill(new Rect(510,639,626,40),new Color(.035f,.065f,.08f,.92f));
                GUI.Label(new Rect(524,643,598,32),e.Kind==BattlePresentationKind.CastStart?"詠唱開始（まだダメージなし）":"合計 −"+e.Damage+" / "+e.TargetIds.Count+"対象",small);
            }
            Fill(new Rect(0,679,1152,23),new Color(.035f,.065f,.08f,.92f));
            var footerStyle=new GUIStyle(small){padding=new RectOffset(0,0,0,0)};
            GUI.Label(new Rect(16,680,1110,22),"美術候補 / 残り4人は未制作。正式採用前。"+(Inspection!=null?" 素材確認："+Inspection:""),footerStyle);
            return target;
        }
    }
}
