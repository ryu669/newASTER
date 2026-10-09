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
        private readonly float[] gardenAmbientBlend=new float[3];
        private bool LifeAudioAllowed=>gardenLifeRuntime!=null && (watchModeActive?WatchWindowAdapter.IsVisible && !AudioListener.pause:artHasFocus && (Quality119EnvironmentAudioCapture || Application.isFocused) && !title && encounter==null && adv==null);
        private void PauseLifeAudio(){if(gardenAmbient!=null)foreach(var a in gardenAmbient)a.Pause();gardenLifeSe?.Stop();}
        private void StopLifeAudio(){if(gardenAmbient!=null)foreach(var a in gardenAmbient)a.Stop();Array.Clear(gardenAmbientBlend,0,gardenAmbientBlend.Length);gardenLifeSe?.Stop();}
        private void SyncLifeAudio()
        {
            if(!LifeAudioAllowed){PauseLifeAudio();return;}
            if(gardenAmbient==null){gardenAmbient=Enumerable.Range(0,3).Select(i=>gameObject.AddComponent<AudioSource>()).ToArray();foreach(var a in gardenAmbient){a.playOnAwake=false;a.loop=true;}gardenLifeSe=gameObject.AddComponent<AudioSource>();gardenLifeSe.playOnAwake=false;}
            var snapshot=LifeSnapshot();string type=GardenLifeCatalog.Types[Array.IndexOf(GardenLifeCatalog.GardenIds,gardenLifeRuntime.GardenId)];
            string baseSound=type=="forest" || type=="flower"?"forest":type=="lakeside" || type=="hotspring"?"water":"wind";
            string terraformSound=snapshot.world.terraform.domains.Single(d=>d.domainId=="water").currentLevel>=3?"water":"wind";
            string effect=gardenLifeRuntime.Weather=="rain"?"rain":gardenLifeRuntime.Weather=="snow"?"snow":gardenLifeRuntime.Context().phenomenonId!=null?"phenomenon":null;
            // A shared base/Terraform clip needs only one source; doubling it changes its gain.
            string[] names={baseSound,terraformSound==baseSound?null:terraformSound,effect};
            for(int i=0;i<3;i++){
                var source=gardenAmbient[i];
                var clip=names[i]==null?null:Resources.Load<AudioClip>("Audio/garden-life-"+names[i]+"-v1");
                bool changing=source.clip!=clip;
                gardenAmbientBlend[i]=Mathf.MoveTowards(gardenAmbientBlend[i],changing?0:clip==null?0:1,Time.unscaledDeltaTime*4);
                if(changing && gardenAmbientBlend[i]<=0){source.Stop();source.clip=clip;}
                source.volume=ArtSampleSettings.Se*.22f*gardenAmbientBlend[i];
                if(source.clip!=null){source.UnPause();if(!source.isPlaying)source.Play();}else source.Stop();
            }
        }
        private void PlayLifeSound(string trigger,GardenLifeContext context)
        {
            if(!LifeAudioAllowed || gardenLifeSe==null || Time.realtimeSinceStartup-gardenLastLifeSound<2)return;
            string sound=trigger=="movement"?"step":context.interactionTag=="read"?"book":context.interactionTag=="bathe"?"water-use":context.interactionTag=="sit"?"seat":context.interactionTag=="eat" || context.interactionTag=="drink"?"table":trigger=="furniture"?"furniture":null;
            if(sound==null)return;var clip=Resources.Load<AudioClip>("Audio/garden-life-"+sound+"-v1");if(clip==null)throw new InvalidOperationException("Missing life sound: "+sound);gardenLifeSe.volume=ArtSampleSettings.Se;gardenLifeSe.PlayOneShot(clip);gardenLastLifeSound=Time.realtimeSinceStartup;
        }
    }
}
