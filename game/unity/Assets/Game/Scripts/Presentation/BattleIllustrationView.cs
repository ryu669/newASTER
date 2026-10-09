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
        private string warning;
        public bool Ready=>manifest!=null && body!=null;
        public int PartCount=>manifest?.parts.Length??0;
        private Texture2D background,body,middle,foreground,enemyMajor;
        private readonly string[] args=Environment.GetCommandLineArgs();
        private string Inspection { get {int i=Array.IndexOf(args,"-inspectPlan7Battle");return i>=0 && i+1<args.Length?args[i+1]:null;} }
        private readonly BoundedCache<string,Texture2D> layers;
        public int CachedLayerCount=>layers.Count;
        private Texture2D LoadLayer(string path,bool optional=false)
        {
            if(string.IsNullOrWhiteSpace(path)) return null;
            if(layers.TryGetValue(path,out var found)) return found;
            var image=Resources.Load<Texture2D>(path);
            if(image==null && !optional) throw new ArgumentException("Image missing: "+path);
            layers[path]=image;return image;
        }
        public BattleIllustrationView(string resource="Illustrations/battle-preview",Action cleanup=null)
        {
            layers=new BoundedCache<string,Texture2D>(36,cleanup);
            try {
                var source=Resources.Load<TextAsset>(resource);
                if(source==null) throw new ArgumentException("battle-preview manifest missing");
                manifest=JsonUtility.FromJson<BattleIllustrationManifest>(source.text);manifest.Validate();
                foreach(var binding in Resources.LoadAll<TextAsset>("Illustrations").Where(a=>a.name.EndsWith("-battle-binding",StringComparison.Ordinal))){var hero=JsonUtility.FromJson<HeroIllustrationBinding>(binding.text);if(!manifest.heroes.Any(h=>h.heroineId==hero.heroineId))manifest.heroes=manifest.heroes.Concat(new[]{hero}).ToArray();}
                background=LoadLayer(manifest.backgroundResourcePath);body=LoadLayer(manifest.bodyResourcePath);
                middle=LoadLayer(manifest.middleResourcePath);foreground=LoadLayer(manifest.foregroundResourcePath);
                enemyMajor=LoadLayer(manifest.enemyMajorResourcePath);
                foreach(var part in manifest.parts) {
                    foreach(var path in new[]{part.resourcePath,part.destroyedResourcePath}) {
                        var layer=LoadLayer(path);
                        if(layer!=null && (body==null || layer.width!=body.width || layer.height!=body.height))
                            throw new ArgumentException("Enemy layer canvas mismatch: "+path);
                    }
                }
            } catch(Exception e) { manifest=null;background=null;body=null;middle=null;foreground=null;enemyMajor=null;layers.Clear();warning=e.Message;Debug.LogWarning("ILLUSTRATION_MANIFEST_WARNING "+warning); }
        }
        private void DrawEnemyLayer(PartIllustrationBinding part,Rect canvas,PlayableBattle battle,BattlePresentationEvent e)
        {
            int index=battle.State.Parts.ToList().FindIndex(p=>p.Id==part.partId);
            if(index<0) return;
            int hp=e?.PartHp[index]??battle.State.Parts[index].HitPoints;
            if(Inspection==part.partId)hp=0; // diagnostic drawing only, state remains unchanged
            string path=BattleIllustrationManifest.PartResource(part,hp);
            if(path!=null){var art=LoadLayer(path);GUI.DrawTexture(PartCanvas(canvas,part),art,ScaleMode.ScaleToFit,true);}
        }
        public static Rect PartCanvas(Rect canvas,PartIllustrationBinding part)
        =>LayerCanvas(canvas,part.placement);
        public static Rect LayerCanvas(Rect canvas,IllustrationLayerPlacement p)
        =>p==null || !p.enabled?canvas:new Rect(canvas.x+p.x*canvas.width,canvas.y+p.y*canvas.height,canvas.width*p.scale,canvas.height*p.scale);
        public static int DisplayActor(int available,BattlePresentationEvent e) => e!=null?e.Actor:available;
        public static int DisplayVictim(string[] heroineIds,BattlePresentationEvent e)
        {return e!=null && e.Kind==BattlePresentationKind.Enemy?Array.FindIndex(heroineIds,id=>e.TargetIds.Contains(id)):-1;}
        public void DrawEnemyPreview(Rect canvas,int brokenMask)
        {
            if(manifest==null || body==null)return;
            foreach(var part in manifest.parts.Where(p=>p.drawOrder<0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal))DrawPreviewPart(part,canvas,brokenMask);
            GUI.DrawTexture(LayerCanvas(canvas,manifest.bodyPlacement),body,ScaleMode.ScaleToFit,true);
            foreach(var part in manifest.parts.Where(p=>p.drawOrder>=0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal))DrawPreviewPart(part,canvas,brokenMask);
        }
        public void DrawEnemyMajorPreview(Rect canvas){if(enemyMajor!=null)GUI.DrawTexture(canvas,enemyMajor,ScaleMode.ScaleToFit,true);}
        private void DrawPreviewPart(PartIllustrationBinding part,Rect canvas,int mask)
        {int index=Array.IndexOf(manifest.parts,part);string path=BattleIllustrationManifest.PartResource(part,(mask&(1<<index))!=0?0:1);if(path!=null)GUI.DrawTexture(PartCanvas(canvas,part),LoadLayer(path),ScaleMode.ScaleToFit,true);}
        private static void Fill(Rect rect,Color color) { ImageUiSkin.Surface(rect,color); }
        public string Draw(PlayableBattle battle,BattlePresentationEvent e,float elapsed,string target,bool canSelect,GUIStyle style,GUIStyle small,string[] names,string[] partNames,bool showActorLabel=true)
        {
            style=new GUIStyle(style);small=new GUIStyle(small);style.normal.textColor=Color.white;small.normal.textColor=new Color(.97f,.94f,.83f);
            var sceneRect=new Rect(0,0,1600,900);
            Fill(sceneRect,new Color(.08f,.14f,.19f));
            if(background!=null) GUI.DrawTexture(sceneRect,background,ScaleMode.ScaleAndCrop);
            float parallax=ArtSampleSettings.ReducedMotion?0:Mathf.Sin(elapsed*.7f)*3;
            if(middle!=null)GUI.DrawTexture(new Rect(-4+parallax,0,1608,900),middle,ScaleMode.ScaleAndCrop,true);
            int actor=DisplayActor(battle.AvailableHero,e);
            // Asset inspection only: never changes the engine's available actor or command target.
            bool inspectStanding=args.Contains("-inspectPlan7Standing") || Inspection!=null;
            if(inspectStanding){
                int inspectIndex=Array.IndexOf(args,"-inspectPlan9Hero");
                string inspectId=inspectIndex>=0 && inspectIndex+1<args.Length?args[inspectIndex+1]:"heroine.slayer";
                actor=battle.State.Heroes.ToList().FindIndex(h=>h.Id==inspectId);
                if(actor<0)throw new ArgumentException("Unknown illustration inspection heroine: "+inspectId);
            }
            int victimIndex=DisplayVictim(battle.State.Heroes.Select(h=>h.Id).ToArray(),e);
            if(victimIndex>=0 && !inspectStanding)actor=victimIndex;
            bool victim=victimIndex>=0 && !inspectStanding;
            float progress=e==null?0:BattleVisualCue.Progress(elapsed,e.Kind,e.Major);
            float movement=!ArtSampleSettings.ReducedMotion && e!=null && e.Kind==BattlePresentationKind.Attack?24*Mathf.Sin(progress*Mathf.PI):0;
            var actorRect=new Rect(890+movement,104,650,550);
            if(actor>=0 && actor<5) {
                int binding=manifest!=null?manifest.HeroIndex(battle.State.Heroes[actor].Id):-1;
                Texture2D art=binding>=0?LoadLayer(manifest.heroes[binding].resourcePath):null;
                if(binding>=0){var hero=manifest.heroes[binding];string path=Inspection=="hit" || victim?hero.hitResourcePath:Inspection=="cutin" || e!=null && e.Actor==actor && (e.Major || e.FullChain)?hero.cutinResourcePath:Inspection=="attack" || e!=null && e.Actor==actor && (e.Kind==BattlePresentationKind.Attack || e.Kind==BattlePresentationKind.CastRelease)?hero.attackResourcePath:null;
                    if(path!=null){var pose=LoadLayer(path,true);if(pose!=null)art=pose;}}
                if(battle.State.Heroes[actor].Id=="heroine.shell" && battle.RequiresPanzerDefense(actor))art=LoadLayer("Illustrations/shell-bare-candidate-v1");
                if(art!=null) {
                    if(manifest.heroes[binding].fullCanvas) GUI.DrawTexture(actorRect,art,ScaleMode.ScaleToFit,true);
                    else {
                        var uv=new Rect(380f/1672,1-750f/941,912f/1672,650f/941);
                        var fit=AspectLayout.Contain(actorRect.x,actorRect.y,actorRect.width,actorRect.height,art.width*uv.width,art.height*uv.height);
                        GUI.DrawTextureWithTexCoords(new Rect(fit.X,fit.Y,fit.Width,fit.Height),art,uv);
                    }
                }
                else {
                    Fill(new Rect(960,580,490,75),new Color(.035f,.065f,.08f,.8f));
                    GUI.Label(new Rect(978,592,455,55),names[actor]+"\n人物の絵は準備中です",style);
                }
            }
            var enemy=new Rect(90,86,620,620);
            if(body==null) {
                Fill(enemy,new Color(.12f,.21f,.22f));
                GUI.Label(new Rect(210,305,450,55),"巨神獣：部位配置の仮表示",small);
            } else if(e!=null && e.Kind==BattlePresentationKind.Enemy && e.Major && e.PartHp.All(hp=>hp>0) && enemyMajor!=null){GUI.DrawTexture(enemy,enemyMajor,ScaleMode.ScaleToFit,true);} else {
                foreach(var part in manifest.parts.Where(p=>p.drawOrder<0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal)) DrawEnemyLayer(part,enemy,battle,e);
                GUI.DrawTexture(LayerCanvas(enemy,manifest.bodyPlacement),body,ScaleMode.ScaleToFit,true);
                foreach(var part in manifest.parts.Where(p=>p.drawOrder>=0).OrderBy(p=>p.drawOrder).ThenBy(p=>p.partId,StringComparer.Ordinal)) DrawEnemyLayer(part,enemy,battle,e);
            }
            if(foreground!=null)GUI.DrawTexture(sceneRect,foreground,ScaleMode.ScaleAndCrop,true);
            if(e!=null && !ArtSampleSettings.ReducedFlash && elapsed<.25f){Color tint=e.PartBroken?new Color(1,.65f,.2f,.12f):e.Kind==BattlePresentationKind.Healing?new Color(.25f,1,.55f,.10f):e.Kind==BattlePresentationKind.Support?new Color(.3f,.65f,1,.10f):new Color(1,1,1,.06f);Fill(sceneRect,tint);}
            if(body!=null && manifest!=null && canSelect && Event.current.type==EventType.MouseDown && Event.current.button==0 && enemy.Contains(Event.current.mousePosition) && Event.current.mousePosition.y<708) {
                var mouse=Event.current.mousePosition;
                string hit=manifest.HitPart((mouse.x-enemy.x)/enemy.width,(mouse.y-enemy.y)/enemy.height,id=>battle.State.Parts.Any(p=>p.Id==id && p.HitPoints>0));
                if(hit!=null){target=hit;Event.current.Use();}
                else if(new Rect(enemy.x+enemy.width*.397f,enemy.y+enemy.height*.385f,enemy.width*.25f,enemy.height*.5f).Contains(mouse)){target="body";Event.current.Use();}
            }
            if(body==null && canSelect && ImageUiSkin.Button(new Rect(330,440,195,64),(target=="body"?"◆ ":"")+"本体")) target="body";
            else if(!canSelect && body==null) GUI.Label(new Rect(340,450,190,40),"本体",style);
            if(warning!=null) GUI.Label(new Rect(100,615,570,65),"素材警告："+warning,small);
            if(manifest!=null) foreach(var p in manifest.parts) {
                int index=battle.State.Parts.ToList().FindIndex(part=>part.Id==p.partId);
                if(index<0) { GUI.Label(new Rect(100,615,550,55),"部位IDが戦闘定義と不一致",small);continue; }
                int hp=Inspection==p.partId?0:e?.PartHp[index]??battle.State.Parts[index].HitPoints;
                var rect=new Rect(enemy.x+p.x*enemy.width,enemy.y+p.y*enemy.height,p.width*enemy.width,p.height*enemy.height);
                var old=GUI.enabled;GUI.enabled=canSelect && hp>0;
                if(body==null && ImageUiSkin.Button(rect,(target==p.partId?"◆ ":"")+partNames[index]+"\n"+(hp==0?"破壊済み":"HP "+hp))) target=p.partId;
                if(body!=null && (target==p.partId || rect.Contains(Event.current.mousePosition))) {Fill(new Rect(rect.x,rect.y,rect.width,2),new Color(1,.85f,.35f));Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),new Color(1,.85f,.35f));}
                GUI.enabled=old;
            }
            // Parts without image bindings remain selectable in the on-demand target drawer.
            if(showActorLabel && actor>=0 && actor<5){int binding=manifest!=null?manifest.HeroIndex(battle.State.Heroes[actor].Id):-1;
                Fill(new Rect(900,666,650,32),new Color(.035f,.065f,.08f,.78f));
                GUI.Label(new Rect(914,669,620,28),(inspectStanding?"素材確認 / ":victim?"被弾対象 / ":"行動者 "+(actor+1)+" / ")+names[actor]+(binding>=0 && manifest.heroes[binding].placeholder?"（候補絵）":""),small);}
            if(e!=null && (e.Damage>0 || e.Kind==BattlePresentationKind.CastStart)) {
                Fill(new Rect(100,666,650,32),new Color(.035f,.065f,.08f,.78f));
                GUI.Label(new Rect(114,669,620,28),e.Kind==BattlePresentationKind.CastStart?"詠唱開始（まだダメージなし）":"合計 −"+e.Damage+" / "+e.TargetIds.Count+"対象",small);
            }
            return target;
        }
    }
}
