using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool quality119Started,quality119Complete;
        private bool Quality119Capture=>capturePath!=null && formalDiagnostic && Environment.GetCommandLineArgs().Contains("-validatePlan119");
        private void TryStartQuality119()
        {
            if(!Quality119Capture || quality119Started || Time.realtimeSinceStartup<2)return;
            quality119Started=true;StartCoroutine(ValidateQuality119());
        }
        private IEnumerator ValidateQuality119()
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();var navigation=CreateFormalBook();timer.Stop();double bookMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();for(int i=0;i<100;i++)navigation.FlipPage();timer.Stop();double pageMs=timer.Elapsed.TotalMilliseconds/100;
            AcceptanceCheck(illustrationView.CachedLayerCount<=36,"11-9E battle images are bounded and loaded on demand");
            foreach(string id in combatDefinitions.HeroineIds){var portrait=HeroPortrait(id);AcceptanceCheck(portrait!=null,"11-9A own portrait "+id);}
            AcceptanceCheck(heroinePortraits.Count<=24,"11-9E portrait cache bound");
            yield return Resources.UnloadUnusedAssets();yield return new WaitForSecondsRealtime(2);
            var frames=new List<float>();for(int i=0;i<180;i++){yield return null;frames.Add(Time.unscaledDeltaTime);}
            double normalFps=frames.Count/frames.Sum(t=>(double)t);
            long textureBytes=Resources.FindObjectsOfTypeAll<Texture2D>().Sum(t=>(long)t.width*t.height*4);
            // Watch uses the same diagnostic garden and real Windows window lifecycle.
            string hero=gardenLifeRuntime.Agents.First().heroineId;float previousWatchVolume=watchVolume;watchVolume=0;StartWatchMode(hero);
            AcceptanceCheck(watchModeActive,"11-9C watch mode enters");AcceptanceCheck(AudioListener.volume==0,"11-9D watch mute");yield return new WaitForSecondsRealtime(2);
            frames.Clear();for(int i=0;i<300;i++){yield return null;frames.Add(Time.unscaledDeltaTime);}
            double watchFps=frames.Count/frames.Sum(t=>(double)t);AcceptanceCheck(Application.targetFrameRate==30,"11-9E watch targets 30FPS");
            ExitWatchMode();watchVolume=previousWatchVolume;
            AcceptanceCheck(!watchModeActive && Math.Abs(AudioListener.volume-watchOriginalVolume)<.001f,"11-9C/D watch returns and restores volume");
            Debug.Log("PLAN11_9_PLAYER_MEASUREMENT bookCreateMs="+bookMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" pageProcessMs="+pageMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" normalFps="+normalFps.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" watchFps="+watchFps.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" residentTextureRgbaEstimateBytes="+textureBytes+" productionForms="+combatDefinitions.HeroineIds.Length+" syntheticTextures=0 physicalInput=0 listening=0");
            Debug.Log("PLAN11_9_PLAYER_QUALITY_PASS");quality119Complete=true;
        }
    }
}
