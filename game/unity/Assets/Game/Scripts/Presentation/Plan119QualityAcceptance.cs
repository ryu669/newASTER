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
        private bool quality119ProfileActive;
        [Serializable] private sealed class Quality119FrameCost {public int frame,gcCollections;public bool focused;public double frameMs,updateMs,guiMs,saveMs;}
        private readonly Dictionary<int,Quality119FrameCost> quality119FrameCosts=new Dictionary<int,Quality119FrameCost>();
        private struct Quality119CostScope:IDisposable
        {
            private readonly PrototypeBootstrap owner;private readonly int kind,frame;private readonly long started;
            public Quality119CostScope(PrototypeBootstrap owner,int kind){this.owner=owner.quality119ProfileActive?owner:null;this.kind=kind;frame=Time.frameCount;started=this.owner==null?0:System.Diagnostics.Stopwatch.GetTimestamp();}
            public void Dispose(){if(owner==null)return;double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;if(!owner.quality119FrameCosts.TryGetValue(frame,out var cost)){cost=new Quality119FrameCost{frame=frame,focused=Application.isFocused};owner.quality119FrameCosts[frame]=cost;}if(kind==0)cost.updateMs+=ms;else if(kind==1)cost.guiMs+=ms;else cost.saveMs+=ms;}
        }
        [Serializable] private sealed class Quality119FrameReport {public Quality119FrameCost[] frames;public string scope="Frame interval with previous-frame callback CPU costs; save is included within Update, not additive. GPU, engine wait and other behaviours are outside these CPU scopes.";}

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
            yield return ValidateQuality119Images();
            var stableSnapshot=LifeSnapshot();long stableRevision=formalCampaign.Revision;yield return null;
            if(stableRevision==formalCampaign.Revision)AcceptanceCheck(ReferenceEquals(stableSnapshot,LifeSnapshot()),"11-9E unchanged progress is reused across frames");
            string favoriteBefore=LifeSnapshot().gardenLife.favoriteGardenId;
            stableSnapshot=LifeSnapshot();CommitLife("settings","quality-favorite",s=>s.gardenLife.favoriteGardenId=book.SubjectId);
            AcceptanceCheck(!ReferenceEquals(stableSnapshot,LifeSnapshot()) && LifeSnapshot().gardenLife.favoriteGardenId==book.SubjectId,"11-9E committed settings invalidate the presentation snapshot");
            CommitLife("settings","quality-favorite-restore",s=>s.gardenLife.favoriteGardenId=favoriteBefore);
            yield return Resources.UnloadUnusedAssets();yield return new WaitForSecondsRealtime(2);
            int copiesBefore=lifeSnapshotCopies;quality119FrameCosts.Clear();quality119ProfileActive=true;
            var sampledCosts=new List<Quality119FrameCost>();var frames=new List<float>();int gcBefore=GC.CollectionCount(0);
            for(int i=0;i<180;i++){yield return null;frames.Add(Time.unscaledDeltaTime);int frame=Time.frameCount-1;if(!quality119FrameCosts.TryGetValue(frame,out var cost))cost=new Quality119FrameCost{frame=frame,focused=Application.isFocused};cost.frameMs=Time.unscaledDeltaTime*1000;cost.gcCollections=GC.CollectionCount(0)-gcBefore;gcBefore=GC.CollectionCount(0);sampledCosts.Add(cost);}
            quality119ProfileActive=false;
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capturePath),"normal-frame-costs.json"),JsonUtility.ToJson(new Quality119FrameReport{frames=sampledCosts.ToArray()},true));
            foreach(var cost in sampledCosts.Where(c=>c.frameMs>40))Debug.Log("PLAN11_9_SLOW_FRAME "+JsonUtility.ToJson(cost));
            int normalSnapshotCopies=lifeSnapshotCopies-copiesBefore;
            double normalFps=frames.Count/frames.Sum(t=>(double)t);
            double normalP95Ms=frames.OrderBy(t=>t).ElementAt((int)Math.Ceiling(frames.Count*.95)-1)*1000,normalMaxMs=frames.Max()*1000;
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capturePath),"normal-frame-times-ms.txt"),string.Join(",",frames.Select(t=>(t*1000).ToString("F3",System.Globalization.CultureInfo.InvariantCulture))));
            long textureBytes=Resources.FindObjectsOfTypeAll<Texture2D>().Sum(t=>(long)t.width*t.height*4);
            // Watch uses the same diagnostic garden and real Windows window lifecycle.
            string hero=gardenLifeRuntime.Agents.First().heroineId;float previousWatchVolume=watchVolume;watchVolume=0;StartWatchMode(hero);
            AcceptanceCheck(watchModeActive,"11-9C watch mode enters");AcceptanceCheck(AudioListener.volume==0,"11-9D watch mute");yield return new WaitForSecondsRealtime(2);
            OnApplicationFocus(false);SyncLifeAudio();AcceptanceCheck(gardenAmbient!=null && gardenAmbient.Any(a=>a.isPlaying),"11-9D visible watch mode keeps ambience through focus loss");
            frames.Clear();for(int i=0;i<300;i++){yield return null;frames.Add(Time.unscaledDeltaTime);}
            double watchFps=frames.Count/frames.Sum(t=>(double)t);AcceptanceCheck(Application.targetFrameRate==30,"11-9E watch targets 30FPS");
            ExitWatchMode();watchVolume=previousWatchVolume;
            AcceptanceCheck((artTextures?.Count??0)<=48 && (advImages?.Count??0)<=24 && plan9FacePatches.Count<=12 && gardenLayers.Count<=24,"11-9E SD, ADV, and face patch caches are bounded");
            AcceptanceCheck(!watchModeActive && Math.Abs(AudioListener.volume-watchOriginalVolume)<.001f,"11-9C/D watch returns and restores volume");
            Debug.Log("PLAN11_9_PLAYER_MEASUREMENT bookCreateMs="+bookMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" pageProcessMs="+pageMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" normalFps="+normalFps.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" watchFps="+watchFps.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" residentTextureRgbaEstimateBytes="+textureBytes+" productionForms="+combatDefinitions.HeroineIds.Length+" syntheticTextures=0 physicalInput=0 listening=0");
            Debug.Log("PLAN11_9_NORMAL_FRAME_MEASUREMENT p95Ms="+normalP95Ms.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" maxMs="+normalMaxMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" progressSnapshotCopies="+normalSnapshotCopies+" scope=isolatedGardenWithImmediateDiagnosticPersistence");
            Debug.Log("PLAN11_9_PLAYER_QUALITY_PASS");quality119Complete=true;
        }
    }
}
