using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string productionMusicResource;
        private float productionMusicBlend;
        private bool productionAudioStarted,productionAudioComplete;
        private bool ProductionAudioCapture=>formalDiagnostic && capturePath!=null && Environment.GetCommandLineArgs().Contains("-validatePlan9Audio");
        // Isolated diagnostics inject focus callbacks; normal play also observes the OS focus.
        private bool ProductionAudioHasFocus=>artHasFocus && (ProductionAudioCapture && formalDiagnostic && capturePath!=null || Application.isFocused);
        private void TryStartProductionAudioCapture()
        {if(ProductionAudioCapture && !productionAudioStarted){productionAudioStarted=true;OnApplicationFocus(true);StartCoroutine(ValidateProductionAudio());}}
        private IEnumerator ValidateProductionAudio()
        {
            string replaySource=HomeData().chapters.First(ch=>formalCampaign.Snapshot.world.unlockedStoryIds.Contains(ch.id)).id;
            if(!formalCampaign.Snapshot.world.readStoryIds.Contains(replaySource))ReadProductionDiagnosticScene(replaySource,false);
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);title=true;yield return new WaitForSecondsRealtime(1);
            AcceptanceCheck(artBgm.clip==Resources.Load<AudioClip>("Audio/plan9-bgm-title-candidate-v1") && artBgm.isPlaying,"production title owns title music");
            OnApplicationFocus(false);AcceptanceCheck(!artBgm.isPlaying,"production title loses focus immediately");OnApplicationFocus(true);yield return new WaitForSecondsRealtime(.1f);AcceptanceCheck(artBgm.isPlaying,"production title resumes on focus return");
            title=false;yield return new WaitForSecondsRealtime(1);AcceptanceCheck(artBgm.clip==Resources.Load<AudioClip>("Audio/plan9-bgm-garden-candidate-v1") && artBgm.isPlaying,"production garden switches to its own music");
            foreach(string caption in new[]{"決定","取消","ページ"}){PlayProductionUiSound(caption);AcceptanceCheck(artSe.isPlaying,"production own UI sound plays: "+caption);yield return new WaitForSecondsRealtime(.9f);}
            PlayProductionUnlock();AcceptanceCheck(artSe.isPlaying,"production unlock sound plays");OnApplicationFocus(false);AcceptanceCheck(!artBgm.isPlaying && !artSe.isPlaying,"production focus loss stops UI sound and garden music");OnApplicationFocus(true);
            StartBattle(NewAster.Data.WorldCatalog.ColossusIds[0]);yield return new WaitForSecondsRealtime(1);AcceptanceCheck(artBgm.clip==Resources.Load<AudioClip>("Audio/plan9-bgm-battle-candidate-v1") && artBgm.isPlaying,"production battle switches to battle music");
            OnApplicationFocus(false);AcceptanceCheck(paused && !artBgm.isPlaying,"production battle focus loss pauses");OnApplicationFocus(true);yield return new WaitForSecondsRealtime(.1f);AcceptanceCheck(paused && !artBgm.isPlaying,"production battle waits for explicit resume");paused=false;yield return new WaitForSecondsRealtime(.1f);AcceptanceCheck(artBgm.isPlaying,"production battle manually resumes audio");
            encounter=null;playback.Reset();BeginAdv(replaySource,true);AcceptanceCheck(adv!=null && adv.Replay,"production audio ADV is an existing replay");yield return new WaitForSecondsRealtime(1);AcceptanceCheck(artBgm.clip==Resources.Load<AudioClip>("Audio/plan9-bgm-adv-candidate-v1") && artBgm.isPlaying,"production ADV owns its music");AcceptanceCheck(advBgm!=null && !advBgm.isPlaying,"production ADV does not overlap legacy ambient music");
            OnApplicationFocus(false);AcceptanceCheck(adv.Paused && !artBgm.isPlaying,"production ADV focus loss pauses music and text");OnApplicationFocus(true);yield return new WaitForSecondsRealtime(.1f);AcceptanceCheck(adv.Paused && !artBgm.isPlaying,"production ADV waits for manual resume");adv.Resume();yield return new WaitForSecondsRealtime(.1f);AcceptanceCheck(artBgm.isPlaying,"production ADV manually resumes music");CloseAdv();yield return new WaitForSecondsRealtime(1);
            AcceptanceCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),"production audio and focus callbacks change no saved progress");productionAudioComplete=true;
            Debug.Log("PLAN9_PRODUCTION_AUDIO_PASS sceneBgm=4 uiSounds=4 focus=title,garden,battle,adv saveUnchanged=true physicalInput=0 listening=0");
        }
        private string ProductionMusicScene=>adv!=null?"adv":encounter!=null?"battle":!title && book!=null && book.Bookmark==NewAster.Core.BookBookmark.Gardens?"garden":"title";
        private void UpdateProductionMusic(bool active,bool stopped)
        {
            if(!active){artBgm.Stop();artSe.Stop();productionMusicBlend=0;return;}
            if(stopped){artBgm.Pause();artSe.Pause();return;}
            string desired="Audio/plan9-bgm-"+ProductionMusicScene+"-candidate-v1";
            bool changing=productionMusicResource!=desired;
            productionMusicBlend=Mathf.MoveTowards(productionMusicBlend,changing?0:1,Time.unscaledDeltaTime*4);
            if(changing && productionMusicBlend<=0){artBgm.Stop();artBgm.clip=Resources.Load<AudioClip>(desired);productionMusicResource=desired;}
            artBgm.volume=ArtSampleSettings.Bgm*productionMusicBlend;
            artBgm.UnPause();artSe.UnPause();if(!artBgm.isPlaying && artBgm.clip!=null)artBgm.Play();
        }
        private void PlayProductionUiSound(string caption)
        {
            if(!ProductionStoryActive || !ProductionAudioHasFocus)return;EnsureArtAudio();
            string kind=caption.Contains("取消") || caption.Contains("戻") || caption.Contains("閉") || caption.Contains("中断")?"cancel":caption.Contains("ページ") || caption.Contains("しおり") || caption.Contains("庭を切替")?"page":"confirm";
            var clip=Resources.Load<AudioClip>("Audio/plan9-se-"+kind+"-candidate-v1");if(clip!=null)artSe.PlayOneShot(clip,ArtSampleSettings.Se);
        }
        private void PlayProductionUnlock()
        {
            if(!ProductionStoryActive || !ProductionAudioHasFocus)return;EnsureArtAudio();var clip=Resources.Load<AudioClip>("Audio/plan9-se-unlock-candidate-v1");if(clip!=null)artSe.PlayOneShot(clip,ArtSampleSettings.Se);
        }
    }
}
