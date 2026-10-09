using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool watchModeActive;
        private WatchModeSettings watchSettings;
        private WatchWindowAdapter watchWindow;
        private GardenLifeRuntime watchLife;
        private int watchOriginalFps,watchOriginalVsync;
        private bool watchOriginalAudioPause;
        private float watchOriginalVolume;
        private bool watchOriginalBackground;
        private int watchTargetIndex;
        private bool watchTop;
        private float watchVolume=.5f;
        private bool watchSettingsLoaded;
        private double watchLastClock,watchMeasuredSeconds;
        private int watchMeasuredFrames;
        private void EnsureWatchSettings(){if(watchSettingsLoaded)return;watchSettings=new WatchModeSettings();try{JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString((plan15Manual?"watch.settings.plan15-test":"watch.settings"),"{}"),watchSettings);}catch{}watchTop=watchSettings.alwaysOnTop;watchVolume=float.IsNaN(watchSettings.volumeMultiplier) || float.IsInfinity(watchSettings.volumeMultiplier)?.5f:Mathf.Clamp01(watchSettings.volumeMultiplier);var ids=LifeSnapshot().growth.heroines.Select(h=>h.heroineId).ToArray();watchTargetIndex=Math.Max(0,Array.IndexOf(ids,watchSettings.heroineId));watchSettingsLoaded=true;}
        private System.Collections.IEnumerator ApplyWatchWindow(bool entering,WatchWindowAdapter adapter){yield return null;yield return null;if(entering && watchModeActive)adapter.Apply(watchSettings);else if(!entering)adapter.RestorePosition();}
        private void DrawWatchSettings(){EnsureWatchSettings();var ids=LifeSnapshot().growth.heroines.Select(h=>h.heroineId).ToArray();if(ids.Length==0)return;watchTargetIndex=Mathf.Clamp(watchTargetIndex,0,ids.Length-1);if(Btn(40,738,360,36,"対象："+combatDefinitions.Hero(ids[watchTargetIndex]).name))watchTargetIndex=(watchTargetIndex+1)%ids.Length;watchTop=GUI.Toggle(new Rect(420,741,245,36),watchTop,"最前面に固定",new GUIStyle(GUI.skin.toggle){font=font,fontSize=19});Label(670,741,85,36,"音量",small,Color.white);watchVolume=GUI.HorizontalSlider(new Rect(725,752,200,25),watchVolume,0,1);if(Btn(1040,738,300,36,"見守りを開始",BookInputAllowed && Application.platform==RuntimePlatform.WindowsPlayer))StartWatchMode(ids[watchTargetIndex]);}
        private void StartWatchMode(string heroine)
        {
            if(Application.platform!=RuntimePlatform.WindowsPlayer || !BookInputAllowed || gardenLifeRuntime==null)return;
            if(!FlushSaveChanges())return;
            EnsureWatchSettings();
            watchSettings.heroineId=heroine;
            watchSettings.alwaysOnTop=watchTop;watchSettings.volumeMultiplier=watchVolume;
            watchLife=gardenLifeRuntime.Agents.Any(a=>a.heroineId==heroine)?gardenLifeRuntime:new GardenLifeRuntime(LifeSnapshot(),HomeData(),gardenLifeRuntime.GardenId,displayHeroineId:heroine);watchLife.SetWatchTarget(heroine);
            watchOriginalAudioPause=AudioListener.pause;watchOriginalVsync=QualitySettings.vSyncCount;watchOriginalFps=Application.targetFrameRate;watchOriginalVolume=AudioListener.volume;watchOriginalBackground=Application.runInBackground;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=WatchModePresentation.TargetFrameRate;AudioListener.volume=WatchModePresentation.Volume(watchOriginalVolume,watchSettings.volumeMultiplier);
            watchWindow=new WatchWindowAdapter();watchWindow.Enter(watchSettings);watchModeActive=true;Application.runInBackground=true;watchLastClock=Time.realtimeSinceStartupAsDouble;watchMeasuredSeconds=0;watchMeasuredFrames=0;StartCoroutine(ApplyWatchWindow(true,watchWindow));
        }
        private void SaveWatchSettings(){if(watchModeActive)watchWindow.Capture(watchSettings);PlayerPrefs.SetString((plan15Manual?"watch.settings.plan15-test":"watch.settings"),JsonUtility.ToJson(watchSettings));RequestSettingsSave();}
        private void ExitWatchMode()
        {
            if(!watchModeActive)return;watchWindow.Capture(watchSettings);watchWindow.Exit();StartCoroutine(ApplyWatchWindow(false,watchWindow));watchLife.SetWatchTarget(null);
            SaveWatchSettings();
            AudioListener.pause=watchOriginalAudioPause;QualitySettings.vSyncCount=watchOriginalVsync;Application.targetFrameRate=watchOriginalFps;AudioListener.volume=watchOriginalVolume;watchModeActive=false;Application.runInBackground=watchOriginalBackground;watchLife=null;gardenLifeTriggers.Clear();gardenLifeFinds.Clear();
        }
        private void UpdateWatchMode()
        {
            if(!watchModeActive)return;
            if(Input.GetKeyDown(KeyCode.Escape)){ExitWatchMode();return;}
            AudioListener.pause=watchOriginalAudioPause || !WatchWindowAdapter.IsVisible;double now=Time.realtimeSinceStartupAsDouble;double elapsed=now-watchLastClock;watchLastClock=now;
            if(WatchWindowAdapter.IsVisible && elapsed>0 && elapsed<=2){watchLife.Tick((float)elapsed);SyncLifeAudio();watchMeasuredSeconds+=elapsed;watchMeasuredFrames++;if(watchMeasuredSeconds>=30){Debug.Log("WATCH_MODE_FPS seconds="+watchMeasuredSeconds.ToString("F2")+" fps="+(watchMeasuredFrames/watchMeasuredSeconds).ToString("F2")+" agents="+watchLife.WatchAgents.Count);watchMeasuredSeconds=0;watchMeasuredFrames=0;}}else StopLifeAudio();gardenLifeTriggers.Clear();gardenLifeFinds.Clear();
            if(WatchWindowAdapter.IsVisible && Math.Abs(Screen.height-Screen.width*9/16)>2 && elapsed<.25)Screen.SetResolution(Screen.width,Screen.width*9/16,FullScreenMode.Windowed);
        }
        private void DrawWatchMode()
        {
            if(!WatchWindowAdapter.IsVisible)return;Styles();BeginAspectCanvas();var state=LifeSnapshot().home;string garden=watchLife.GardenId;
            state.occupants=watchLife.WatchAgents.Select(a=>LifeOccupant(a,garden,state)).ToArray();
            // Follow the target within the same garden using the existing scene renderer.
            var target=watchLife.WatchAgents.First(a=>a.heroineId==watchSettings.heroineId);
            var view=WatchCameraController.Follow(target.x,target.y);
            DrawGardenScene(view,state,garden,HomeData().gardens.Single(g=>g.id==garden),false);
            var old=GUI.matrix;GUI.matrix=Matrix4x4.identity;var style=new GUIStyle(GUI.skin.button){font=font,fontSize=14};if(GUI.Button(new Rect(Screen.width-150,Screen.height-32,145,27),"通常画面へ戻る",style))ExitWatchMode();GUI.matrix=old;
        }
    }
}
