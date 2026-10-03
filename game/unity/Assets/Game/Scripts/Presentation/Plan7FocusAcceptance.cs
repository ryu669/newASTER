using System;
using System.Collections;
using UnityEngine;
using NewAster.Core;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool plan7FocusStarted;
        private static void FocusCheck(bool condition,string message)
        {if(!condition)throw new InvalidOperationException("PLAN7_FOCUS_FAIL "+message);}
        private IEnumerator ValidatePlan7Focus()
        {
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
            artSample=true;artSamplePaused=false;UpdateArtAudio();yield return new WaitForSecondsRealtime(.1f);
            FocusCheck(artBgm.isPlaying && PlayArtSound("hit"),"sample audio starts");
            OnApplicationFocus(false);
            FocusCheck(artSamplePaused && !artBgm.isPlaying && !artSe.isPlaying && !PlayArtSound("hit"),"focus loss immediately stops both sources and rejects preview");
            int sample=artBgm.timeSamples;yield return new WaitForSecondsRealtime(.1f);
            FocusCheck(artBgm.timeSamples==sample,"paused BGM sample position is frozen");
            OnApplicationFocus(true);UpdateArtAudio();FocusCheck(artSamplePaused && !artBgm.isPlaying,"regaining focus does not resume sample");
            SetArtSamplePaused(false);UpdateArtAudio();yield return new WaitForSecondsRealtime(.1f);FocusCheck(artBgm.isPlaying,"manual sample resume");
            SetArtSamplePaused(true);FocusCheck(!PlayArtSound("heal") && !artBgm.isPlaying,"manual pause rejects new sounds immediately");
            artSample=false;artSamplePaused=false;StartBattle(NewAster.Data.WorldCatalog.ColossusIds[0]);UpdateArtAudio();yield return new WaitForSecondsRealtime(.1f);
            FocusCheck(artBgm.isPlaying,"battle audio starts after leaving paused sample");
            int hp=encounter.State.BossHitPoints;OnApplicationFocus(false);FocusCheck(paused && !artBgm.isPlaying,"battle focus loss pauses immediately");
            yield return new WaitForSecondsRealtime(.1f);OnApplicationFocus(true);UpdateArtAudio();FocusCheck(paused && !artBgm.isPlaying && encounter.State.BossHitPoints==hp,"battle waits for manual resume without damage");
            paused=false;UpdateArtAudio();yield return new WaitForSecondsRealtime(.1f);FocusCheck(artBgm.isPlaying,"manual battle resume");
            encounter=null;UpdateArtAudio();FocusCheck(!artBgm.isPlaying && !artSe.isPlaying,"leaving battle stops both sources");
            adv=new AdvSession(HomeData(),"scene.art-candidate.slayer",HomeData().events[0].id,true,Array.Empty<HomeReadLine>());adv.Resume();SyncAdvAudio();
            advBgm.clip=Resources.Load<AudioClip>("Audio/candidate-bgm");advBgm.loop=true;advBgm.Play();yield return new WaitForSecondsRealtime(.1f);
            OnApplicationFocus(false);FocusCheck(adv.Paused && !advBgm.isPlaying && !advSe.isPlaying,"ADV focus loss stops both sources");
            string visible=adv.VisibleText;yield return new WaitForSecondsRealtime(.1f);OnApplicationFocus(true);SyncAdvAudio();
            FocusCheck(adv.Paused && !advBgm.isPlaying && adv.VisibleText==visible,"ADV focus regain keeps text and audio paused");
            adv.Resume();SyncAdvAudio();yield return new WaitForSecondsRealtime(.1f);FocusCheck(advBgm.isPlaying,"manual ADV resume");
            CloseAdv();artSample=true;artTab="settings";artSamplePaused=false;
            FocusCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"diagnostic does not change durable progress");
            Debug.Log("PLAN7_FOCUS_AUDIO_PASS sample battle ADV / simulated Unity focus callbacks / isolated");
        }
    }
}
