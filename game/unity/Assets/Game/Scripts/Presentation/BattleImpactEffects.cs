using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    // Cosmetic hits only. Uses elapsed playback time; never calls combat or RNG.
    public sealed class BattleImpactEffects
    {
        [Serializable] public sealed class Profile
        {
            public string skillId,family;
            public float[] times,sizes;
            public float red,green,blue;
        }
        [Serializable] private sealed class Catalog { public Profile[] profiles; }
        private readonly Dictionary<string,Profile> profiles;
        private readonly Texture2D glow;
        private readonly Profile fallback=new Profile{family="impact",times=new[]{.48f},sizes=new[]{1.2f},red=1,green=.7f,blue=.25f};
        public BattleImpactEffects()
        {
            var asset=Resources.Load<TextAsset>("UI/battle-impact-profiles");
            if(asset==null)throw new InvalidOperationException("Missing battle impact profiles");
            var catalog=JsonUtility.FromJson<Catalog>(asset.text);
            if(catalog?.profiles==null || catalog.profiles.Any(p=>string.IsNullOrEmpty(p.skillId) || p.times==null || p.sizes==null || p.times.Length<1 || p.times.Length>6 || p.times.Length!=p.sizes.Length || p.times.Any(t=>t<.1f || t>.7f) || !p.times.SequenceEqual(p.times.OrderBy(t=>t)) || p.sizes.Any(s=>s<=0 || s>2)))throw new InvalidOperationException("Invalid battle impact profile");
            profiles=catalog.profiles.ToDictionary(p=>p.skillId,StringComparer.Ordinal);
            glow=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Battle impact glow",hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Max(0,1-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16);pixels[y*32+x]=new Color(1,1,1,a*a);}
            glow.SetPixels(pixels);glow.Apply(false,true);
        }
        public Profile For(string id)=>id!=null && profiles.TryGetValue(id,out var value)?value:fallback;
        public Vector2 Offset(BattlePresentationEvent e,float elapsed)
        {
            if(e==null || ArtSampleSettings.ReducedMotion || e.Damage<=0)return Vector2.zero;
            var profile=For(e.PresentationId);float amount=0;
            foreach(float time in profile.times){float age=elapsed-time*BattleVisualCue.Duration(e.Kind,e.Major);if(age>=0 && age<.12f)amount+=Mathf.Sin(age*140)*(1-age/.12f);}
            return new Vector2(amount*(e.Major?9:5),amount*2);
        }
        public void Dispose(){UnityEngine.Object.Destroy(glow);}
        private static void Line(Vector2 from,Vector2 to,float width,Color color)
        {
            var matrix=GUI.matrix;var previous=GUI.color;var delta=to-from;
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(from.x,from.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
            GUI.color=color;GUI.DrawTexture(new Rect(0,-width*.5f,delta.magnitude,width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=previous;
        }
        private void Glow(Vector2 point,float size,Color color)
        {var previous=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(point.x-size,point.y-size,size*2,size*2),glow);GUI.color=previous;}
        public void Draw(BattlePresentationEvent e,float elapsed,Vector2[] targets,GUIStyle text)
        {
            if(e==null || targets.Length==0 || Event.current.type!=EventType.Repaint)return;
            bool attack=e.Kind==BattlePresentationKind.Attack || e.Kind==BattlePresentationKind.CastRelease || e.Kind==BattlePresentationKind.Enemy;
            bool healing=e.Kind==BattlePresentationKind.Healing,support=e.Kind==BattlePresentationKind.Support;
            if(!attack && !healing && !support && e.Kind!=BattlePresentationKind.CastStart)return;
            var profile=For(e.PresentationId);float duration=BattleVisualCue.Duration(e.Kind,e.Major);
            var tint=healing?new Color(.25f,1,.55f):support || e.Kind==BattlePresentationKind.CastStart?new Color(.35f,.7f,1):new Color(profile.red,profile.green,profile.blue);
            int count=attack?profile.times.Length:1;
            for(int hit=0;hit<count;hit++){
                float age=elapsed-(attack?profile.times[hit]*duration:duration*.22f);
                if(age<0 || age>.30f)continue;
                float progress=age/.30f,fade=(1-progress)*(ArtSampleSettings.ReducedFlash?.35f:1),size=(attack?profile.sizes[hit]:1.1f)*(e.Major || e.FullChain?1.4f:1);
                if(attack)size*=1+Mathf.Min(5,Mathf.Max(0,e.Chain-1))*.08f;
                if(e.PartBroken && hit==count-1)size*=1.25f;
                if(ArtSampleSettings.ReducedMotion)progress=.5f;
                foreach(var target in targets.Take(4)){
                    Vector2 point=target+(ArtSampleSettings.ReducedMotion?Vector2.zero:new Vector2((hit%3-1)*28,(hit%2==0?-1:1)*18));
                    if(!ArtSampleSettings.ReducedFlash)Glow(point,(45+progress*100)*size,new Color(tint.r,tint.g,tint.b,fade*.65f));
                    int rays=ArtSampleSettings.ReducedMotion?6:12;
                    for(int ray=0;ray<rays;ray++){
                        float angle=(ray*360f/rays+hit*23)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                        Line(point+direction*(15+progress*65)*size,point+direction*(30+progress*110)*size,2+fade*3,new Color(tint.r,tint.g,tint.b,fade));
                    }
                    if(attack && profile.family=="slash"){
                        var direction=new Vector2(.8f,hit%2==0?-.6f:.6f);
                        Line(point-direction*105*size,point+direction*105*size,4+fade*13,new Color(tint.r,tint.g,tint.b,fade*.7f));
                        Line(point-direction*88*size,point+direction*88*size,2+fade*3,new Color(1,1,1,fade));
                    }else if(attack && profile.family=="shot")Line(point+new Vector2(240,-75),point,2+fade*4,new Color(1,.85f,.45f,fade));
                    else for(int r=0;r<24;r++){
                        float a=r*Mathf.PI*2/24,b=(r+1)*Mathf.PI*2/24,radius=(25+progress*100)*size;
                        Line(point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,point+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,2+fade*3,new Color(tint.r,tint.g,tint.b,fade));
                    }
                }
                if(attack && hit==count-1 && e.Damage>0){
                    var style=new GUIStyle(text){fontSize=e.FullChain?48:38,alignment=TextAnchor.MiddleCenter};style.normal.textColor=e.PartBroken?new Color(1,.8f,.3f):Color.white;
                    var at=targets[0];GUI.Label(new Rect(at.x-160,at.y-120-(ArtSampleSettings.ReducedMotion?0:progress*35),320,110),(e.PartBroken?"BREAK\n":"")+(targets.Length>1?"TOTAL ":"")+e.Damage.ToString("N0"),style);
                }
            }
        }
    }
}
