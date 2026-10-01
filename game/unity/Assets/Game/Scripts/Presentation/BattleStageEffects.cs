using System.Collections.Generic;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    // Reusable geometry effects. No colliders, battle callbacks, or damage calculation.
    public sealed class BattleStageEffects : MonoBehaviour
    {
        private readonly List<LineRenderer> castingRings=new List<LineRenderer>();
        private LineRenderer slash, impact;
        private GameObject projectile;
        private BattlePresentationEvent current;
        private Vector3 source,destination;
        private float elapsed;
        private bool paused;
        public void Initialize(int count)
        {
            for(int i=0;i<count;i++) castingRings.Add(Line("Casting circle "+i,new Color(.65f,.4f,1f),.025f,49));
            slash=Line("Sword arc",new Color(1f,.85f,.48f),.045f,25);
            impact=Line("Impact ring",new Color(1f,.75f,.32f),.04f,49);
            projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere); projectile.name="Cast projectile";
            var collider=projectile.GetComponent<Collider>(); if(collider!=null) Destroy(collider);
            projectile.transform.SetParent(transform,false);
            projectile.GetComponent<Renderer>().material=new Material(Resources.Load<Shader>("StageSurface")) { color=new Color(.6f,.3f,1f) };
            projectile.GetComponent<Renderer>().material.SetColor("_EmissionColor",new Color(.6f,.3f,1f));
            Clear();
        }
        private LineRenderer Line(string name,Color color,float width,int points)
        {
            var obj=new GameObject(name); obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>(); line.material=new Material(Resources.Load<Shader>("StageSurface")) { color=color };
            line.material.SetColor("_EmissionColor",color); line.positionCount=points;
            line.startWidth=line.endWidth=width; line.enabled=false; return line;
        }
        public void Begin(BattlePresentationEvent e,Vector3 from,Vector3 to)
        {
            ClearTransient(); current=e; elapsed=0; source=from; destination=to;
            UpdateTransient();
        }
        public void Synchronize(IReadOnlyList<Transform> members,PlayableBattle battle,BattlePresentationEvent visual,bool stop)
        {
            paused=stop;
            for(int i=0;i<castingRings.Count;i++) {
                bool active=battle!=null && (visual?.Casting[i]??battle.IsCasting(i));
                var ring=castingRings[i]; ring.enabled=active;
                if(active) Circle(ring,members[i].position+Vector3.up*.25f,.34f+.03f*Mathf.Sin(TimePhase()*2),TimePhase(),true);
            }
        }
        private float pulseClock;
        private float TimePhase() => pulseClock*1.5f;
        private void Update()
        {
            if(paused) return;
            pulseClock+=Time.deltaTime;
            if(current==null) return;
            elapsed+=Time.deltaTime; UpdateTransient();
        }
        private void UpdateTransient()
        {
            if(current==null) return;
            float p=BattleVisualCue.Progress(elapsed,current.Kind,current.Major);
            bool release=current.Kind==BattlePresentationKind.CastRelease;
            bool attack=current.Kind==BattlePresentationKind.Attack;
            float travel=BattleVisualCue.Travel(p), hit=BattleVisualCue.Impact(p);
            projectile.SetActive(release && p>=.16f && p<.56f);
            if(projectile.activeSelf) {
                projectile.transform.position=Vector3.Lerp(source,destination,travel)+Vector3.up*Mathf.Sin(travel*Mathf.PI)*.6f;
                projectile.transform.localScale=Vector3.one*(.16f+.04f*Mathf.Sin(p*24));
            }
            slash.enabled=attack && p>=.16f && p<.56f;
            if(slash.enabled) for(int i=0;i<slash.positionCount;i++) {
                float a=(i/(float)(slash.positionCount-1)-.5f)*2.5f+p*4f;
                slash.SetPosition(i,Vector3.Lerp(source,destination,travel)+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.35f);
            }
            impact.enabled=(release || attack) && hit>0;
            if(impact.enabled) {
                Circle(impact,destination,.15f+(1-hit)*.65f,0,false);
                impact.startWidth=impact.endWidth=.06f*hit;
            }
        }
        private static void Circle(LineRenderer line,Vector3 center,float radius,float phase,bool horizontal)
        {
            for(int i=0;i<line.positionCount;i++) {
                float a=i/(float)(line.positionCount-1)*Mathf.PI*2+phase;
                line.SetPosition(i,center+(horizontal?new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)):new Vector3(Mathf.Cos(a),Mathf.Sin(a),0))*radius);
            }
        }
        private void ClearTransient() { if(slash!=null) slash.enabled=false; if(impact!=null) impact.enabled=false; if(projectile!=null) projectile.SetActive(false); current=null; }
        public void Clear() { ClearTransient(); foreach(var ring in castingRings) ring.enabled=false; }
        private void OnDestroy()
        {
            foreach(var line in GetComponentsInChildren<LineRenderer>()) if(line.sharedMaterial!=null) Destroy(line.sharedMaterial);
            if(projectile!=null) Destroy(projectile.GetComponent<Renderer>().sharedMaterial);
        }
    }
}
