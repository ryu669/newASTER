using System.Collections.Generic;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed class SlayerModelView : MonoBehaviour
    {
        private Animation motion;
        private readonly List<Renderer> rose=new List<Renderer>(), training=new List<Renderer>();
        private HeroineOutfitState outfit;
        private float transientRemaining;
        private readonly List<SkinnedMeshRenderer> expressions=new List<SkinnedMeshRenderer>();
        private float expressionClock;
        private bool forceBlink;
        private string expression="Smile";
        public string Expression => expression;
        public bool SetExpression(string value)
        {
            if(value!="Neutral" && value!="Smile" && value!="Joy" && value!="Sad" && value!="Angry" && value!="Surprise" && value!="Talk") return false;
            expression=value; return true;
        }
        public string OutfitId => outfit.EquippedOutfitId;
        public static SlayerModelView Create(Transform parent)
        {
            var asset=Resources.Load<GameObject>("Characters/Slayer/slayer-beauty-v2");
            if(asset==null) return null;
            var obj=Instantiate(asset,parent,false); obj.name="Slayer";
            // Match the existing floating party formation; model feet at root origin.
            obj.transform.localPosition=new Vector3(0,-.63f,0); obj.transform.localScale=Vector3.one*.65f;
            obj.transform.localRotation=Quaternion.Euler(0,90,0);
            return obj.AddComponent<SlayerModelView>();
        }
        private void Awake()
        {
            outfit=new HeroineOutfitState(new[]{new HeroineOutfitDefinition("rose","slayer.rose",new[]{"torso","hips","shins"}),new HeroineOutfitDefinition("training","slayer.training",new[]{"torso","hips","legs"})},"rose");
            motion=GetComponent<Animation>(); if(motion==null) motion=GetComponentInChildren<Animation>();
            var shader=Resources.Load<Shader>("HeroineBeauty");
            forceBlink=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureSlayerBlink")>=0;
            var args=System.Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++) if(args[i]=="-captureExpression") SetExpression(args[i+1]);
            foreach(var renderer in GetComponentsInChildren<Renderer>()) {
                var original=renderer.sharedMaterials; var materials=new Material[original.Length];
                for(int i=0;i<materials.Length;i++) {
                    var source=original[i];
                    Color color=source!=null?source.color:Color.white;
                    if(QualitySettings.activeColorSpace==ColorSpace.Gamma) color=color.gamma;
                    materials[i]=new Material(shader) { color=color };
                    string name=source!=null?source.name:"";
                    bool face=name.Contains("Skin") || name.Contains("Eye") || name.Contains("Iris") || name.Contains("Lip") || name.Contains("Lash") || name.Contains("Mouth");
                    materials[i].SetFloat("_Face",face?1:0);
                    materials[i].SetFloat("_Gloss",name.Contains("Hair")?.07f:name.Contains("Gold") || name.Contains("Silver")?.32f:0);
                    materials[i].SetFloat("_Outline",face?0:.00025f);
                    materials[i].SetFloat("_Fabric",name.Contains("Fabric")?1:0);
                }
                renderer.sharedMaterials=materials;
                if(renderer.name.StartsWith("Outfit_Rose")) rose.Add(renderer);
                if(renderer.name.StartsWith("Outfit_Training")) training.Add(renderer);
                var skinned=renderer as SkinnedMeshRenderer;
                if(skinned!=null && skinned.sharedMesh.blendShapeCount>0) expressions.Add(skinned);
            }
            if(motion!=null) motion.playAutomatically=false;
            TryEquip(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-captureTrainingOutfit")>=0?"training":"rose"); Play("Idle");
        }
        public bool TryEquip(string id)
        {
            if(!outfit.TryEquip(id)) return false;
            foreach(var r in rose) r.enabled=id=="rose";
            foreach(var r in training) r.enabled=id=="training";
            return true;
        }
        public void PlayEvent(BattlePresentationEvent e,int actor=0)
        {
            if(e.Kind==BattlePresentationKind.Enemy) { if(e.HeroHp[actor]>0) Play("Hit"); return; }
            if(e.Actor!=actor) return;
            Play(e.Kind==BattlePresentationKind.Attack || e.Kind==BattlePresentationKind.CastRelease?"Attack":e.Kind==BattlePresentationKind.CastStart || e.Kind==BattlePresentationKind.Support?"Cast":"Idle");
            if(e.Kind==BattlePresentationKind.Support) transientRemaining=.6f;
        }
        private void Play(string clip)
        {
            if(motion==null || motion.GetClip(clip)==null) return;
            motion.Stop(); motion.Play(clip);
            transientRemaining=clip=="Idle" || clip=="Cast"?0:motion.GetClip(clip).length;
        }
        public void Synchronize(bool paused,bool casting)
        {
            if(!paused) expressionClock+=Time.deltaTime;
            if(motion==null) return;
            foreach(AnimationState state in motion) state.speed=paused?0:1;
            if(!paused && transientRemaining>0) {
                transientRemaining-=Time.deltaTime;
                if(transientRemaining<=0) Play(casting?"Cast":"Idle");
            }
            if(!paused && !motion.isPlaying) Play(casting?"Cast":"Idle");
        }
        private void LateUpdate()
        {
            // Imported legacy clips also key blend shapes. Apply facial animation
            // after the body animation pass so Idle cannot reset every blink.
            float phase=expressionClock%3.7f;
            float blink=forceBlink?100:phase<.16f?Mathf.Sin(phase/.16f*Mathf.PI)*100:0;
            if(expression=="Joy") blink=100;
            foreach(var r in expressions) for(int i=0;i<r.sharedMesh.blendShapeCount;i++) {
                string shape=r.sharedMesh.GetBlendShapeName(i);
                r.SetBlendShapeWeight(i,0);
                if(shape.EndsWith("Blink")) r.SetBlendShapeWeight(i,blink);
                if(shape.EndsWith("Smile")) r.SetBlendShapeWeight(i,expression=="Joy"?100:expression=="Smile"?65:0);
                if(shape.EndsWith("Sad")) r.SetBlendShapeWeight(i,expression=="Sad"?100:0);
                if(shape.EndsWith("Angry")) r.SetBlendShapeWeight(i,expression=="Angry"?100:0);
                if(shape.EndsWith("Surprise")) r.SetBlendShapeWeight(i,expression=="Surprise"?100:0);
                if(shape.EndsWith("Talk")) r.SetBlendShapeWeight(i,expression=="Surprise"?90:expression=="Talk"?(35+65*Mathf.Abs(Mathf.Sin(expressionClock*7))):0);
            }
        }
        private void OnDestroy() { foreach(var r in GetComponentsInChildren<Renderer>()) foreach(var m in r.sharedMaterials) if(m!=null) Destroy(m); }
    }
}
