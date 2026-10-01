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
        public string OutfitId => outfit.EquippedOutfitId;
        public static SlayerModelView Create(Transform parent)
        {
            var asset=Resources.Load<GameObject>("Characters/Slayer/slayer-production-v1");
            if(asset==null) return null;
            var obj=Instantiate(asset,parent,false); obj.name="Slayer Production Base";
            // Match the existing floating party formation; model feet at root origin.
            obj.transform.localPosition=new Vector3(0,-.63f,0); obj.transform.localScale=Vector3.one*.65f;
            obj.transform.localRotation=Quaternion.Euler(0,90,0);
            return obj.AddComponent<SlayerModelView>();
        }
        private void Awake()
        {
            outfit=new HeroineOutfitState(new[]{new HeroineOutfitDefinition("rose","slayer.rose",new[]{"torso","hips","shins"}),new HeroineOutfitDefinition("training","slayer.training",new[]{"torso","hips","legs"})},"rose");
            motion=GetComponent<Animation>(); if(motion==null) motion=GetComponentInChildren<Animation>();
            var shader=Resources.Load<Shader>("StageSurface");
            foreach(var renderer in GetComponentsInChildren<Renderer>()) {
                var original=renderer.sharedMaterials; var materials=new Material[original.Length];
                for(int i=0;i<materials.Length;i++) materials[i]=new Material(shader) { color=original[i]!=null?original[i].color:Color.white };
                renderer.sharedMaterials=materials;
                if(renderer.name.StartsWith("Outfit_Rose")) rose.Add(renderer);
                if(renderer.name.StartsWith("Outfit_Training")) training.Add(renderer);
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
        public void PlayEvent(BattlePresentationEvent e)
        {
            if(e.Kind==BattlePresentationKind.Enemy) { if(e.HeroHp[0]>0) Play("Hit"); return; }
            if(e.Actor!=0) return;
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
            if(motion==null) return;
            foreach(AnimationState state in motion) state.speed=paused?0:1;
            if(!paused && transientRemaining>0) {
                transientRemaining-=Time.deltaTime;
                if(transientRemaining<=0) Play(casting?"Cast":"Idle");
            }
            if(!paused && !motion.isPlaying) Play(casting?"Cast":"Idle");
        }
        private void OnDestroy() { foreach(var r in GetComponentsInChildren<Renderer>()) foreach(var m in r.sharedMaterials) if(m!=null) Destroy(m); }
    }
}
