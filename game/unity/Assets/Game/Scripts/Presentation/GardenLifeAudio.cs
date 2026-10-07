using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private AudioSource[] gardenAmbient;
        private AudioSource gardenLifeSe;
        private float gardenLastLifeSound;
        private void StopLifeAudio(){if(gardenAmbient!=null)foreach(var a in gardenAmbient)a.Stop();gardenLifeSe?.Stop();}
        private void SyncLifeAudio()
        {
            if(gardenLifeRuntime==null)return;
            if(gardenAmbient==null){gardenAmbient=Enumerable.Range(0,3).Select(i=>gameObject.AddComponent<AudioSource>()).ToArray();foreach(var a in gardenAmbient){a.playOnAwake=false;a.loop=true;}gardenLifeSe=gameObject.AddComponent<AudioSource>();gardenLifeSe.playOnAwake=false;}
            var snapshot=LifeSnapshot();string type=GardenLifeCatalog.Types[Array.IndexOf(GardenLifeCatalog.GardenIds,gardenLifeRuntime.GardenId)];
            string baseSound=type=="forest" || type=="flower"?"forest":type=="lakeside" || type=="hotspring"?"water":"wind";
            string terraformSound=snapshot.world.terraform.domains.Single(d=>d.domainId=="water").currentLevel>=3?"water":"wind";
            string effect=gardenLifeRuntime.Weather=="rain"?"rain":gardenLifeRuntime.Weather=="snow"?"snow":gardenLifeRuntime.Context().phenomenonId!=null?"phenomenon":null;
            string[] names={baseSound,terraformSound,effect};
            for(int i=0;i<3;i++){
                var source=gardenAmbient[i];source.volume=ArtSampleSettings.Se*.22f;
                var clip=names[i]==null?null:Resources.Load<AudioClip>("Audio/garden-life-"+names[i]+"-v1");
                if(source.clip!=clip){source.Stop();source.clip=clip;}
                if(clip!=null && !source.isPlaying)source.Play();if(clip==null)source.Stop();
            }
        }
        private void PlayLifeSound(string trigger,GardenLifeContext context)
        {
            if(gardenLifeSe==null || Time.realtimeSinceStartup-gardenLastLifeSound<2)return;
            string sound=trigger=="movement"?"step":context.interactionTag=="read"?"book":context.interactionTag=="bathe"?"water-use":context.interactionTag=="sit"?"seat":context.interactionTag=="eat" || context.interactionTag=="drink"?"table":trigger=="furniture"?"furniture":null;
            if(sound==null)return;var clip=Resources.Load<AudioClip>("Audio/garden-life-"+sound+"-v1");if(clip==null)throw new InvalidOperationException("Missing life sound: "+sound);gardenLifeSe.volume=ArtSampleSettings.Se;gardenLifeSe.PlayOneShot(clip);gardenLastLifeSound=Time.realtimeSinceStartup;
        }
    }
}
