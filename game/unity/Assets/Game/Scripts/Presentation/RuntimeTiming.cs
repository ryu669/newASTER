using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private static System.Diagnostics.Stopwatch runtimeTimingClock;
        private static string runtimeTimingRoot;
        private static string runtimeTimingConfigurationError;
        private bool runtimeTimingTitleRecorded,runtimeTimingBookOpened,runtimeTimingBookRecorded;
        private float runtimeTimingQuitAt=-1;
        private static bool RuntimeTimingEnabled=>runtimeTimingClock!=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRuntimeTiming()
        {
            runtimeTimingClock=null;runtimeTimingRoot=null;runtimeTimingConfigurationError=null;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-runtimeTimingRoot");if(at<0)return;
            try{ConfigureRuntimeTiming(args,at);}catch(Exception e){runtimeTimingConfigurationError=e.Message;Debug.LogError("RUNTIME_TIMING_CONFIGURATION_ERROR "+e.Message);}
        }
        private static void ConfigureRuntimeTiming(string[] args,int at)
        {
            if(at+1>=args.Length || !Path.IsPathRooted(args[at+1]))throw new ArgumentException("Timing requires an absolute isolated save root.");
            string root=Path.GetFullPath(args[at+1]).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root==Path.GetPathRoot(Path.GetFullPath(args[at+1])).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar))throw new ArgumentException("Timing requires a dedicated subdirectory.");
            string normal=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(normal,StringComparison.OrdinalIgnoreCase) || root.StartsWith(normal+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || normal.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Timing must not use the ordinary save directory.");
            runtimeTimingRoot=root;runtimeTimingClock=System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("RUNTIME_TIMING phase=before-scene elapsedMs=0 isolatedSaveRoot="+root);
        }
        private static void RuntimeTimingMark(string phase)
        {
            if(RuntimeTimingEnabled)Debug.Log("RUNTIME_TIMING phase="+phase+" elapsedMs="+runtimeTimingClock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
        }
        private void UpdateRuntimeTiming()
        {
            if(!RuntimeTimingEnabled)return;
            if(runtimeTimingQuitAt>=0 && Time.unscaledTime>=runtimeTimingQuitAt){Application.Quit();return;}
            if(runtimeTimingTitleRecorded && !runtimeTimingBookOpened && Environment.GetCommandLineArgs().Contains("-runtimeTimingBook")){
                runtimeTimingBookOpened=true;RuntimeTimingMark("book-request");OpenFormalBook();RequestBookBookmark(NewAster.Core.BookBookmark.Heroines);
            }
        }
        private void RecordRuntimeTimingFrame()
        {
            if(!RuntimeTimingEnabled || Event.current.type!=EventType.Repaint || combatDefinitionError!=null || saveSlotBlocked || saveSlotDeleted)return;
            if(!runtimeTimingTitleRecorded && title && !FormalEntranceVisible){runtimeTimingTitleRecorded=true;StartCoroutine(FinishRuntimeTimingFrame("title-interactive"));}
            if(runtimeTimingBookOpened && !runtimeTimingBookRecorded && IsBookScreen && !book.IsTransitioning && heroinePortraitRequests.Count==0 && heroinePortraitLoads==0){runtimeTimingBookRecorded=true;StartCoroutine(FinishRuntimeTimingFrame("book-ready"));}
        }
        private IEnumerator FinishRuntimeTimingFrame(string phase)
        {
            yield return new WaitForEndOfFrame();RuntimeTimingMark(phase);
            if(phase=="book-ready" || !Environment.GetCommandLineArgs().Contains("-runtimeTimingBook")){
                ScreenCapture.CaptureScreenshot(Path.Combine(runtimeTimingRoot,"timing-"+phase+".png"));runtimeTimingQuitAt=Time.unscaledTime+1;
            }
        }
    }
}
