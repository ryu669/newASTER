using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NewAster.Core;
namespace NewAster.Presentation
{
    public static class ArtSampleSettings
    {
        public static float Bgm=>PlayerPrefs.GetFloat("art.bgm",.25f);
        public static float Se=>PlayerPrefs.GetFloat("art.se",.5f);
        public static bool ReducedMotion=>PlayerPrefs.GetInt("art.motion",0)==1;
        public static bool ReducedFlash=>PlayerPrefs.GetInt("art.flash",0)==1;
        public static bool Shortened=>PlayerPrefs.GetInt("art.shortened",0)==1;
        public static bool LargeText=>PlayerPrefs.GetInt("art.large-text",0)==1 || Environment.GetCommandLineArgs().Contains("-inspectLargeText");
    }
    public sealed partial class PrototypeBootstrap
    {
        private bool artSample,artSamplePaused;private string artTab="battle",artPose="idle",artExpression="normal";
        private int artBrokenMask,artFurniture;private AudioSource artBgm,artSe;private PlayableBattle audioBattle;private long audioSequence;private bool audioVictory;
        private readonly List<float> artFrameTimes=new List<float>();private readonly bool measureArt=Environment.GetCommandLineArgs().Contains("-measurePlan7");
        private float artResourceValidationSeconds;
        private readonly Dictionary<string,Texture2D> artTextures=new Dictionary<string,Texture2D>();
        private Texture2D SampleImage(string name)
        {
            if(!artTextures.TryGetValue(name,out var texture)){texture=Resources.Load<Texture2D>("Illustrations/"+name+(name=="slayer-cutin" || name=="green-major"?"-candidate-v3":"-candidate-v1"));artTextures[name]=texture;if(texture==null)Debug.LogWarning("PLAN7_ASSET_MISSING "+name);}
            return texture;
        }
        private void SampleImage(Rect rect,string name,bool crop=false)
        {var texture=SampleImage(name);if(texture!=null)GUI.DrawTexture(rect,texture,crop?ScaleMode.ScaleAndCrop:ScaleMode.ScaleToFit,true);}
        private void EnsureArtAudio()
        {if(artBgm!=null)return;artBgm=gameObject.AddComponent<AudioSource>();artSe=gameObject.AddComponent<AudioSource>();artBgm.playOnAwake=false;artSe.playOnAwake=false;artBgm.loop=true;
            if(!Environment.GetCommandLineArgs().Contains("-presentationCapture") && PlayerPrefs.HasKey("art.fullscreen"))Screen.fullScreenMode=PlayerPrefs.GetInt("art.fullscreen")==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;}
        private void PlayArtSound(string name)
        {EnsureArtAudio();var clip=Resources.Load<AudioClip>("Audio/candidate-"+name);if(clip!=null)artSe.PlayOneShot(clip,ArtSampleSettings.Se);}
        private void UpdateArtAudio()
        {
            if(measureArt && Time.realtimeSinceStartup>8)artFrameTimes.Add(Time.unscaledDeltaTime);
            EnsureArtAudio();artBgm.volume=ArtSampleSettings.Bgm;artSe.volume=1;
            bool active=artSample || encounter!=null && adv==null;
            bool stop=!Application.isFocused || (artSample?artSamplePaused:paused || help || retreat);
            if(!active){artBgm.Stop();artSe.Stop();return;}
            if(artBgm.clip==null)artBgm.clip=Resources.Load<AudioClip>("Audio/candidate-bgm");
            if(stop){artBgm.Pause();artSe.Pause();return;}
            artBgm.UnPause();artSe.UnPause();if(!artBgm.isPlaying && artBgm.clip!=null)artBgm.Play();
            if(audioBattle!=encounter){audioBattle=encounter;audioSequence=0;audioVictory=false;}
            var e=playback.Current;if(e==null || e.Sequence==audioSequence)return;audioSequence=e.Sequence;
            if(e.Kind==BattlePresentationKind.Pass || e.Kind==BattlePresentationKind.CastStart || e.Kind==BattlePresentationKind.CastCanceled)return;
            if(e.BossHp==0){if(audioVictory)return;audioVictory=true;}
            PlayArtSound(e.BossHp==0?"victory":e.PartBroken?"break":e.Kind==BattlePresentationKind.Healing?"heal":e.Kind==BattlePresentationKind.Support?"shield":"hit");
        }
        private void OnApplicationFocus(bool focused)
        {if(!focused){if(encounter!=null)paused=true;if(artSample)artSamplePaused=true;if(adv!=null)adv.Pause();}}
        private void OpenArtSample()
        {if(!BookInputAllowed)return;artSample=true;artSamplePaused=false;}
        private void PrepareArtSample(string[] args)
        {
            artSample=true;encounter=null;title=true;int option=Array.IndexOf(args,"-artCase");string name=option<0?"battle":args[option+1];
            if(name.StartsWith("break-")){artTab="battle";artBrokenMask=int.Parse(name.Substring(6));if(artBrokenMask<0 || artBrokenMask>15)throw new ArgumentException("Invalid break mask");}
            else if(new[]{"idle","attack","hit","cutin","joy","puzzled","determined"}.Contains(name)){artTab="actor";if(new[]{"joy","puzzled","determined"}.Contains(name))artExpression=name;else artPose=name;}
            else if(new[]{"sit","work","look"}.Contains(name)){artTab="garden";artFurniture=Array.IndexOf(new[]{"sit","work","look"},name);}
            else artTab=name=="enemycutin"?"battle":name;
            if(name=="enemycutin")artPose="enemycutin";
            ValidateArtSampleResources();
            Debug.Log("PLAN7_SAMPLE_CAPTURE "+name+" / read-only");
        }
        private void ValidateArtSampleResources()
        {
            float started=Time.realtimeSinceStartup;
            if(measureArt && Environment.GetCommandLineArgs().Contains("-measurePlan7Uncapped")){QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;}
            var expected=new[]{"slayer-standing","slayer-attack","slayer-hit","slayer-cutin","slayer-expression-joy","slayer-expression-puzzled","slayer-expression-determined","green-body","green-crown","green-wing-left","green-wing-right","green-tail","forest-far","forest-mid","forest-front","garden-bench","garden-desk","garden-fountain","slayer-sd-idle","slayer-sd-sit","slayer-sd-work","slayer-sd-look","slayer-garden-cg"};
            foreach(string image in expected.Concat(new[]{"green-major"}))if(SampleImage(image)==null)throw new ArgumentException("Mandatory sample asset missing: "+image);
            foreach(string audio in new[]{"hit","shield","heal","break","victory","bgm"})if(Resources.Load<AudioClip>("Audio/candidate-"+audio)==null)throw new ArgumentException("Mandatory sample audio missing: "+audio);
            string required="計画美術完成見本候補緑還竜本体結晶角冠左右翼根装甲蔓尾復元人物通常喜困惑決意待機攻撃被弾箱庭家具利用座作業噴水眺音表示設定一時停止手動再開揺軽減演出短縮背景回想";
            font.RequestCharactersInTexture(required,21);string missing=new string(required.Distinct().Where(c=>!font.HasCharacter(c)).ToArray());
            if(missing.Length>0)throw new ArgumentException("Sample Japanese glyphs missing: "+missing);
            artResourceValidationSeconds=Time.realtimeSinceStartup-started;
            Debug.Log("PLAN7_SAMPLE_ASSETS_PASS 24 textures / 6 audio clips / Japanese glyphs");
        }
        private void ReportArtPerformance()
        {
            if(!measureArt || artFrameTimes.Count==0)return;var ordered=artFrameTimes.OrderBy(x=>x).ToArray();
            double ratio=ordered.Count(x=>x<=.0167f)/(double)ordered.Length;
            long rgbaBytes=artTextures.Values.Where(t=>t!=null).Sum(t=>(long)t.width*t.height*4);long working=-1,peak=-1;
            try{using(var process=System.Diagnostics.Process.GetCurrentProcess()){working=process.WorkingSet64;peak=process.PeakWorkingSet64;}}catch(Exception ex){Debug.Log("PLAN7_MEMORY_UNAVAILABLE "+ex.GetType().Name);}
            if(working<=0 || peak<=0){working=-1;peak=-1;}
            Debug.Log("PLAN7_PERFORMANCE scene="+(artSample?artTab:"gameplay-idle")+" frames="+ordered.Length+" meanMs="+(ordered.Average()*1000).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" p95Ms="+(ordered[(int)((ordered.Length-1)*.95)]*1000).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" under16_7ms="+ratio.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" textureRgbaEstimateBytes="+rgbaBytes+" processWorkingBytes="+working+" processPeakWorkingBytes="+peak+" resourceValidationSeconds="+artResourceValidationSeconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" vsync="+QualitySettings.vSyncCount+" targetFrameRate="+Application.targetFrameRate+" startupSeconds="+Time.realtimeSinceStartup.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" cpu="+SystemInfo.processorType+" gpu="+SystemInfo.graphicsDeviceName);
        }
        private void DrawArtSample()
        {
            var originalText=text;var originalSmall=small;
            if(ArtSampleSettings.LargeText){text=new GUIStyle(text){fontSize=25};small=new GUIStyle(small){fontSize=20};}
            try{DrawArtSampleContent();}finally{text=originalText;small=originalSmall;}
        }
        private void DrawArtSampleContent()
        {
            Panel(0,0,1600,900,dark);Label(40,20,1120,50,"計画7 ／ 美術完成見本の候補",heading,Color.white);
            if(Btn(1270,22,285,45,"タイトルへ戻る")){artSample=false;artBgm?.Stop();artSe?.Stop();return;}
            var tabs=new[]{"battle","actor","garden","cg","settings"};var labels=new[]{"戦闘・部位破壊","人物・差分","箱庭・家具利用","CG","音・表示設定"};
            for(int i=0;i<tabs.Length;i++)if(Btn(40+i*306,90,290,50,labels[i]))artTab=tabs[i];
            if(artTab=="settings"){DrawArtSettings();return;}
            var stageRect=new Rect(40,160,1130,600);SampleImage(stageRect,"forest-far",true);SampleImage(stageRect,"forest-mid",true);
            if(artTab=="battle"){
                SampleImage(new Rect(65,215,470,500),"slayer-attack");
                if(artPose=="enemycutin")illustrationView.DrawEnemyMajorPreview(new Rect(570,200,550,550));else illustrationView.DrawEnemyPreview(new Rect(570,200,550,550),artBrokenMask);
                Label(1200,180,340,60,"緑還竜 ／ 本体＋4部位",heading,Color.white);
                var parts=new[]{"結晶角冠","左翼の根","右翼の装甲","蔓の尾"};for(int i=0;i<4;i++){int bit=1<<i;if(Btn(1200,270+i*70,340,58,parts[i]+((artBrokenMask&bit)!=0?"：破壊":"：健在"))){artPose="idle";artBrokenMask^=bit;}}
                if(Btn(1200,550,340,45,"全部位を復元")){artBrokenMask=0;artPose="idle";}
                if(Btn(1200,610,340,45,"大技カットイン候補")){artBrokenMask=0;artPose="enemycutin";}
                Label(1200,680,340,85,"部位画像だけを切替。\n戦闘のHP・報酬は操作しません。",small,Color.white);
            }else if(artTab=="actor"){
                string name=artPose=="idle"?(artExpression=="normal"?"slayer-standing":"slayer-expression-"+artExpression):"slayer-"+artPose;
                var texture=SampleImage(name);
                if(artPose=="idle"){
                    var standing=SampleImage("slayer-standing");var definition=HomeData().assets.SingleOrDefault(a=>a.resourcePath=="Illustrations/"+name+"-candidate-v1");var region=definition?.regionalOverlay==true?definition.overlayRegion01:null;
                    DrawExpressionLayer(new Rect(65,180,540,560),standing,texture,region);
                    DrawExpressionLayer(new Rect(650,260,460,300),standing,texture,region,new HomeRect{x=.32f,y=.06f,width=.32f,height=.16f});
                }else SampleImage(new Rect(65,180,540,560),name);
                Label(1200,180,340,50,"スレイヤー ／ 候補",heading,Color.white);
                for(int i=0;i<4;i++){string expression=new[]{"normal","joy","puzzled","determined"}[i];if(Btn(1200,250+i*60,340,48,new[]{"通常","喜び","困惑","決意"}[i])){artPose="idle";artExpression=expression;}}
                for(int i=0;i<4;i++){string pose=new[]{"idle","attack","hit","cutin"}[i];if(Btn(1200,510+i*60,340,48,new[]{"待機","攻撃","被弾","カットイン"}[i]))artPose=pose;}
            }else if(artTab=="garden"){
                var props=new[]{"bench","desk","fountain"};var use=GardenUse("furniture.fixture."+artFurniture);
                GUI.BeginGroup(stageRect);try{DrawGardenArtUse(new Rect(280,200,400,400),SampleImage("garden-"+props[artFurniture]),use,true);}finally{GUI.EndGroup();}
                for(int i=0;i<3;i++)if(Btn(1200,260+i*80,340,60,new[]{"ベンチ：座る","作業台：作業する","噴水：眺める"}[i]))artFurniture=i;
                Label(1200,550,340,150,"SD方式の最初の1人。\n家具と人物は別画像。\n座面・接地・手元を重ねて表示。",small,Color.white);
            }else if(artTab=="cg"){SampleImage(stageRect,"slayer-garden-cg",true);Label(1200,230,340,180,"CG候補1枚\n本文・イベントへの正式採用は未確認。",text,Color.white);}
            if(artTab!="cg")SampleImage(stageRect,"forest-front",true);
            Label(40,795,1510,75,"候補素材の比較画面 ／ 正式採用前。解像度・輪郭・接続位置は制作記録を参照。\n旧素材と通常セーブは保持しています。",small,Color.white);
        }
        private void DrawArtSettings()
        {
            Label(80,180,1400,55,"音と表示の設定",heading,Color.white);
            Label(80,260,260,45,"BGM音量",text,Color.white);float bgm=GUI.HorizontalSlider(new Rect(400,275,700,30),ArtSampleSettings.Bgm,0,1);if(Math.Abs(bgm-ArtSampleSettings.Bgm)>.001)PlayerPrefs.SetFloat("art.bgm",bgm);
            Label(80,340,260,45,"SE音量",text,Color.white);float se=GUI.HorizontalSlider(new Rect(400,355,700,30),ArtSampleSettings.Se,0,1);if(Math.Abs(se-ArtSampleSettings.Se)>.001)PlayerPrefs.SetFloat("art.se",se);
            if(Btn(80,430,430,55,"揺れ軽減："+(ArtSampleSettings.ReducedMotion?"ON":"OFF")))PlayerPrefs.SetInt("art.motion",ArtSampleSettings.ReducedMotion?0:1);
            if(Btn(545,430,430,55,"フラッシュ軽減："+(ArtSampleSettings.ReducedFlash?"ON":"OFF")))PlayerPrefs.SetInt("art.flash",ArtSampleSettings.ReducedFlash?0:1);
            if(Btn(1010,430,430,55,"演出短縮："+(ArtSampleSettings.Shortened?"ON":"OFF")))PlayerPrefs.SetInt("art.shortened",ArtSampleSettings.Shortened?0:1);
            var sounds=new[]{"hit","shield","heal","break","victory"};for(int i=0;i<5;i++)if(Btn(80+i*280,530,260,55,new[]{"ヒット","防壁","回復","部位破壊","撃破"}[i]))PlayArtSound(sounds[i]);
            if(Btn(80,590,665,45,"本文・見本文字："+(ArtSampleSettings.LargeText?"大きめ":"標準")))PlayerPrefs.SetInt("art.large-text",ArtSampleSettings.LargeText?0:1);
            if(Btn(785,590,665,45,"画面："+(Screen.fullScreen?"全画面":"ウィンドウ"))){bool fullscreen=!Screen.fullScreen;Screen.fullScreenMode=fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;PlayerPrefs.SetInt("art.fullscreen",fullscreen?1:0);}
            if(Btn(80,650,1370,60,artSamplePaused?"手動で再開":"音を一時停止"))artSamplePaused=!artSamplePaused;
            Label(80,740,1370,95,"新規作成した合成音の候補です。非アクティブ後は手動で再開します。\n表示設定は演出だけに適用し、HP・行動順・抽選・報酬を変えません。",small,Color.white);
        }
    }
}
