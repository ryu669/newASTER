using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private AssetBundle quality119FixtureBundle;
        private Dictionary<string,HeroinePortraitDef> quality119FixtureFraming;
        private string[] quality119FixturePage;
        private int quality119FixtureRepaints;
        private double quality119MaxPortraitLoadCpuMs;
        private System.Diagnostics.Stopwatch quality119ColdBookClock;
        private bool quality119ColdBookRecorded;
        private bool quality119ColdBookRequested;

        private void PrepareQuality119ColdBook(string[] args)
        {
            if(!args.Contains("-validatePlan119ColdBook"))return;
            AcceptanceCheck(formalDiagnostic && capturePath!=null,"11-9E cold book capture is isolated");title=true;
        }
        private void TryOpenQuality119ColdBook()
        {
            if(quality119ColdBookRequested || !Environment.GetCommandLineArgs().Contains("-validatePlan119ColdBook") || Time.realtimeSinceStartup<2)return;
            quality119ColdBookRequested=true;
            AcceptanceCheck((heroinePortraits?.Count??0)==0,"11-9E first book has no preloaded portraits");OpenFormalBook();
        }

        private void BeginQuality119ColdBook()
        {
            if(quality119ColdBookClock==null && Environment.GetCommandLineArgs().Contains("-validatePlan119ColdBook"))quality119ColdBookClock=System.Diagnostics.Stopwatch.StartNew();
        }
        private void RecordQuality119ColdBook()
        {
            if(quality119ColdBookClock==null || quality119ColdBookRecorded || heroinePortraitRequests.Count>0 || Event.current.type!=EventType.Repaint || !IsBookScreen)return;
            quality119ColdBookRecorded=true;StartCoroutine(FinishQuality119ColdBook());
        }
        private IEnumerator FinishQuality119ColdBook()
        {
            yield return new WaitForEndOfFrame();quality119ColdBookClock.Stop();
            Debug.Log("PLAN11_9_COLD_BOOK_MEASUREMENT firstRenderedFrameMs="+quality119ColdBookClock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" scope=firstTitleOpenHandlerToPortraitsReadyFrameEnd portraitsPreloaded=0 osDiskCache=uncontrolled processStartup=excluded gpuFence=excluded");
        }
        private void DrawQuality119LoadPage()
        {
            if(quality119FixturePage==null)return;
            GUI.Box(new Rect(0,0,1600,900),"Plan11-9 / テスト専用画像の読み込み検証");
            for(int i=0;i<quality119FixturePage.Length;i++){
                float x=40+(i%6)*258,y=60+(i/6)*390;
                DrawHeroPortrait(new Rect(x,y,230,330),quality119FixturePage[i]);
                var source=RequestHeroPortrait(quality119FixturePage[i]);
                if(source!=null)DrawMappedExpression(new Rect(x+170,y+260,50,50),source,source,new Rect(.4f,.3f,.2f,.2f),new Rect(.4f,.3f,.2f,.2f),new Rect(0,0,1,1));
                GUI.Label(new Rect(x,y+335,230,32),quality119FixturePage[i]);
            }
            if(Event.current.type==EventType.Repaint)quality119FixtureRepaints++;
        }
        private static int Quality119ResidentImages()=>Resources.FindObjectsOfTypeAll<Texture2D>().Count(t=>t.name.StartsWith("q119-load-",StringComparison.Ordinal));
        private static int Quality119ResidentFacePatches()=>Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name.StartsWith("heroine-face-patch/",StringComparison.Ordinal));
        private static long Quality119ImageBytes()=>Resources.FindObjectsOfTypeAll<Texture2D>().Where(t=>t.name.StartsWith("q119-load-",StringComparison.Ordinal)).Sum(t=>t.format==TextureFormat.DXT5?(long)((t.width+3)/4)*((t.height+3)/4)*16:(long)t.width*t.height*4);
        [Serializable] private sealed class Quality119ImageReport
        {
            public int images,persons,width,height,cacheLimit,maxResidentImages,maxResidentBeforeCleanup,finalResidentImages;
            public int faceCacheLimit=12,maxResidentFacePatches,finalResidentFacePatches;
            public long bundleBytes,peakTextureStorageEstimateBytes;
            public double maxPageFrameMs,p95PageFrameMs,meanPageFrameMs,maxLoadingFrameMs,bundleOpenMs,maxPortraitLoadCpuMs;
            public int maxPendingRequests;
            public int portraitLoadsPerFrameLimit=2;public double portraitBatchCpuBudgetMs=4;
            public double[] pageFrameMs;
            public string textureFormat,scope;
        }
        private IEnumerator ValidateQuality119Images()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qualityFixtureBundle");
            if(at<0)yield break;
            AcceptanceCheck(Quality119Capture && at+1<args.Length,"11-9E fixture use requires isolated quality capture");
            string path=Path.GetFullPath(args[at+1]);
            var bundleClock=System.Diagnostics.Stopwatch.StartNew();
            var bundleRequest=AssetBundle.LoadFromFileAsync(path);yield return bundleRequest;
            quality119FixtureBundle=bundleRequest.assetBundle;bundleRequest=null;
            yield return new WaitForEndOfFrame();bundleClock.Stop();quality119MaxPortraitLoadCpuMs=0;
            AcceptanceCheck(quality119FixtureBundle!=null,"11-9E fixture bundle loads");
            string[] names=quality119FixtureBundle.GetAllAssetNames().OrderBy(n=>n,StringComparer.Ordinal).ToArray();
            AcceptanceCheck(names.Length==320,"11-9E 320 unique fixture assets");
            AcceptanceCheck(plan9FacePatches.Count==0,"11-9E face fixture starts without existing render targets");
            quality119FixtureFraming=names.Select((name,i)=>new HeroinePortraitDef{heroineId="heroine.load."+i.ToString("D3"),resourcePath=name,faceCenterX=.5f,faceCenterY=.4f,faceHeight=.2f}).ToDictionary(f=>f.heroineId);
            var report=new Quality119ImageReport{images=320,persons=256,width=1536,height=1024,cacheLimit=24,bundleOpenMs=bundleClock.Elapsed.TotalMilliseconds,bundleBytes=new FileInfo(path).Length,scope="320 synthetic textures through production portrait cache / framing / drawing; person count belongs to paired Core stress fixture. LZ4 asset I/O, not product artwork or Resources packaging. Fixture container opening is measured separately before page readiness starts. Queued page readiness includes waiting for all portraits and one complete draw; not placeholder-only timing. DXT5 storage estimate excludes driver, allocator, bundle, and upload overhead; not measured VRAM. Process memory sampled externally."};
            var times=new List<double>();
            try{
                for(int first=0;first<names.Length;first+=12){
                    var clock=System.Diagnostics.Stopwatch.StartNew();
                    quality119FixturePage=Enumerable.Range(first,Math.Min(12,names.Length-first)).Select(i=>"heroine.load."+i.ToString("D3")).ToArray();
                    int paints=quality119FixtureRepaints;
                    // Exercise the same frame-budgeted display path as the production roster.
                    bool ready=false;
                    while(!ready){ready=true;foreach(string id in quality119FixturePage)if(RequestHeroPortrait(id)==null)ready=false;
                        report.maxPendingRequests=Math.Max(report.maxPendingRequests,heroinePortraitRequests.Count);
                        AcceptanceCheck(heroinePortraitRequests.Count<=HeroinePageSize,"11-9E in-flight portrait requests are bounded");
                        if(!ready){yield return null;report.maxLoadingFrameMs=Math.Max(report.maxLoadingFrameMs,Time.unscaledDeltaTime*1000);}
                    }
                    paints=quality119FixtureRepaints;
                    report.maxResidentBeforeCleanup=Math.Max(report.maxResidentBeforeCleanup,Quality119ResidentImages());
                    report.peakTextureStorageEstimateBytes=Math.Max(report.peakTextureStorageEstimateBytes,Quality119ImageBytes());
                    // Include real OnGUI drawing and frame submission, excluding page animation.
                    while(quality119FixtureRepaints==paints)yield return null;
                    yield return new WaitForEndOfFrame();clock.Stop();times.Add(clock.Elapsed.TotalMilliseconds);
                    while(heroineAssetCleanupPending)yield return null;
                    yield return Resources.UnloadUnusedAssets();
                    int resident=Quality119ResidentImages();
                    report.maxResidentImages=Math.Max(report.maxResidentImages,resident);
                    report.maxResidentFacePatches=Math.Max(report.maxResidentFacePatches,Quality119ResidentFacePatches());
                    report.peakTextureStorageEstimateBytes=Math.Max(report.peakTextureStorageEstimateBytes,Quality119ImageBytes());
                    AcceptanceCheck(heroinePortraits.Count<=24 && resident<=24,"11-9E evicted textures are released: page "+first+" resident="+resident);
                    AcceptanceCheck(plan9FacePatches.Count<=12 && Quality119ResidentFacePatches()<=12,"11-9E evicted face render targets are destroyed");
                }
                string stale="heroine.load.000";
                AcceptanceCheck(!heroinePortraits.TryGetValue(stale,out _),"11-9E first page was evicted before stale-request check");
                RequestHeroPortrait(stale);heroinePortraitRequests[stale]=Time.frameCount-10;
                yield return null;yield return null;
                AcceptanceCheck(!heroinePortraitRequests.ContainsKey(stale) && !heroinePortraits.TryGetValue(stale,out _),"11-9E abandoned page requests do not load or repopulate the cache");
                report.textureFormat=HeroPortrait(quality119FixturePage[0]).format.ToString();
            }finally{
                quality119FixturePage=null;heroinePortraits.Clear();quality119FixtureFraming=null;
                AcceptanceCheck(heroinePortraitRequests.Count==0,"11-9E no in-flight loads before fixture unload");
                plan9FacePatches.Clear();
                quality119FixtureBundle.Unload(true);quality119FixtureBundle=null;
            }
            while(heroineAssetCleanupPending)yield return null;
            yield return new WaitForEndOfFrame();
            yield return Resources.UnloadUnusedAssets();
            report.finalResidentImages=Quality119ResidentImages();
            report.finalResidentFacePatches=Quality119ResidentFacePatches();
            report.maxPortraitLoadCpuMs=quality119MaxPortraitLoadCpuMs;
            report.pageFrameMs=times.ToArray();report.maxPageFrameMs=times.Max();report.meanPageFrameMs=times.Average();report.p95PageFrameMs=times.OrderBy(t=>t).ElementAt((int)Math.Ceiling(times.Count*.95)-1);
            AcceptanceCheck(report.finalResidentImages==0,"11-9E fixture textures are released after test");
            AcceptanceCheck(report.finalResidentFacePatches==0,"11-9E face render targets are released after test");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(capturePath),"texture-stress.json"),JsonUtility.ToJson(report,true));
            Debug.Log("PLAN11_9_IMAGE_STREAMING_PASS images=320 cache=24 maxResident="+report.maxResidentImages+" finalResident="+report.finalResidentImages+" maxPageFrameMs="+report.maxPageFrameMs.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" textureStorageEstimateBytes="+report.peakTextureStorageEstimateBytes);
        }
    }
}
