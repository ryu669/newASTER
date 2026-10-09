using System;
using System.Collections;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly bool quality119EnvironmentAudioRequested=Environment.GetCommandLineArgs().Contains("-validatePlan119EnvironmentAudio");
        private bool quality119EnvironmentAudioStarted,quality119EnvironmentAudioComplete;
        private bool Quality119EnvironmentAudioCapture=>formalDiagnostic && capturePath!=null && quality119EnvironmentAudioRequested;
        private void TryStartEnvironmentAudio119()
        {
            if(!Quality119EnvironmentAudioCapture || quality119EnvironmentAudioStarted || gardenLifeRuntime==null || Time.realtimeSinceStartup<2)return;
            quality119EnvironmentAudioStarted=true;StartCoroutine(ValidateEnvironmentAudio119());
        }
        private GardenLifeRuntime EnvironmentAudioFixture(string weather)
        {
            var save=formalCampaign.Snapshot;var setting=save.gardenLife.Setting(book.SubjectId);setting.autoWeather=false;setting.weather=weather;
            save.world.terraform.activeWorldPhenomenonId=null;
            return new GardenLifeRuntime(save,HomeData(),book.SubjectId,119){Paused=true};
        }
        private IEnumerator ValidateEnvironmentAudio119()
        {
            var original=gardenLifeRuntime;bool shopBefore=affectionShop;bool pausedBefore=original.Paused;original.Paused=true;gardenLifeTriggers.Clear();gardenLifeFinds.Clear();
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
            try{
                OnApplicationFocus(true);StopLifeAudio();gardenLifeRuntime=EnvironmentAudioFixture("clear");
                yield return new WaitForSecondsRealtime(.8f);
                AcceptanceCheck(gardenAmbient!=null && gardenAmbient.Take(2).All(a=>a.isPlaying) && gardenAmbientBlend.Take(2).All(gain=>gain>.99f) && !gardenAmbient[2].isPlaying,"11-9D clear garden owns two ambient layers");
                gardenLifeRuntime=EnvironmentAudioFixture("rain");SyncLifeAudio();
                AcceptanceCheck(gardenAmbient[2].clip!=null && gardenAmbientBlend[2]<1,"11-9D rain fades in instead of jumping to full volume");
                yield return new WaitForSecondsRealtime(.8f);var rain=gardenAmbient[2].clip;float rainBlend=gardenAmbientBlend[2];
                AcceptanceCheck(rain.name=="garden-life-rain-v1" && gardenAmbient[2].isPlaying && rainBlend>.99f,"11-9D rain ambience reaches audible gain");
                gardenLifeRuntime=EnvironmentAudioFixture("snow");SyncLifeAudio();
                AcceptanceCheck(gardenAmbient[2].clip==rain && gardenAmbientBlend[2]<rainBlend,"11-9D outgoing weather fades before clip replacement");
                yield return new WaitForSecondsRealtime(.8f);
                AcceptanceCheck(gardenAmbient[2].clip.name=="garden-life-snow-v1" && gardenAmbient[2].isPlaying,"11-9D snow replaces rain after fade");
                gardenLastLifeSound=Time.realtimeSinceStartup-3;PlayLifeSound("movement",gardenLifeRuntime.Context());
                AcceptanceCheck(gardenLifeSe.isPlaying,"11-9D life movement SE plays while focused");
                affectionShop=true;OnApplicationFocus(false);
                AcceptanceCheck(gardenAmbient.All(a=>!a.isPlaying) && !gardenLifeSe.isPlaying,"11-9D focus loss immediately pauses ambience and stops obsolete life SE");
                SyncLifeAudio();gardenLastLifeSound=Time.realtimeSinceStartup-3;PlayLifeSound("movement",gardenLifeRuntime.Context());
                AcceptanceCheck(gardenAmbient.All(a=>!a.isPlaying) && !gardenLifeSe.isPlaying,"11-9D unfocused updates and triggers cannot restart garden audio");
                OnApplicationFocus(true);yield return new WaitForSecondsRealtime(.2f);
                AcceptanceCheck(gardenAmbient.All(a=>a.clip==null || a.isPlaying) && !gardenLifeSe.isPlaying,"11-9D focus return resumes ambience behind a modal without replaying stale footsteps");
                affectionShop=shopBefore;gardenLifeRuntime=EnvironmentAudioFixture("clear");yield return new WaitForSecondsRealtime(.8f);
                AcceptanceCheck(gardenAmbient[2].clip==null && !gardenAmbient[2].isPlaying,"11-9D weather effect fades out when no longer needed");
                var duplicateSave=formalCampaign.Snapshot;
                bool waterAmbience=duplicateSave.world.terraform.domains.Single(d=>d.domainId=="water").currentLevel>=3;
                string duplicateGarden=GardenLifeCatalog.GardenIds[waterAmbience?3:1];
                duplicateSave.world.unlockedGardenIds=duplicateSave.world.unlockedGardenIds.Concat(new[]{duplicateGarden}).Distinct().ToArray();
                duplicateSave.world.terraform.activeWorldPhenomenonId=null;
                var duplicateSetting=duplicateSave.gardenLife.Setting(duplicateGarden);duplicateSetting.autoWeather=false;duplicateSetting.weather="clear";
                book.RequestSubject(BookBookmark.Gardens,duplicateGarden);book.CompleteTransition();
                gardenLifeRuntime=new GardenLifeRuntime(duplicateSave,HomeData(),duplicateGarden,119){Paused=true};
                yield return new WaitForSecondsRealtime(.8f);
                AcceptanceCheck(gardenAmbient[0].clip.name=="garden-life-"+(waterAmbience?"water":"wind")+"-v1" && gardenAmbient[0].isPlaying && gardenAmbient[1].clip==null && !gardenAmbient[1].isPlaying,"11-9D identical base and Terraform ambience plays on one source only");
                StopLifeAudio();AcceptanceCheck(gardenAmbient.All(a=>!a.isPlaying) && !gardenLifeSe.isPlaying,"11-9D scene/load cleanup stops every garden audio source");
                AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"11-9D weather audio fixtures mutate no player progress");
            }finally{affectionShop=shopBefore;StopLifeAudio();book.RequestSubject(BookBookmark.Gardens,original.GardenId);book.CompleteTransition();gardenLifeRuntime=original;original.Paused=pausedBefore;OnApplicationFocus(true);}
            quality119EnvironmentAudioComplete=true;
            Debug.Log("PLAN11_9_ENVIRONMENT_AUDIO_PASS weather=clear,rain,snow fade=true duplicateLayers=false focusPause=true modalResume=true staleSeStopped=true saveUnchanged=true physicalInput=0 listening=0");
        }
    }
}
