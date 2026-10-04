using System;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private float formalEntranceStarted=-1;
        private bool formalEntranceComplete;
        private bool formalEntranceCapture;
        private GUIStyle entranceLogoStyle,entranceCaptionStyle;
        private bool FormalEntranceVisible=>!formalEntranceComplete && titlePanel==null;

        private void InitializeFormalEntrance(string[] args)
        {
            formalEntranceCapture=Array.IndexOf(args,"-capturePlan9Intro")>=0;
            formalEntranceComplete=Array.IndexOf(args,"-presentationCapture")>=0 && !formalEntranceCapture;
            if(!formalDiagnostic && Array.IndexOf(args,"-presentationCapture")<0 && PlayerPrefs.HasKey("art.fullscreen"))
                Screen.fullScreenMode=PlayerPrefs.GetInt("art.fullscreen",0)==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
        }

        private void UpdateFormalEntrance()
        {
            if(formalEntranceComplete || formalEntranceCapture || !title || titlePanel!=null)return;
            if(ArtSampleSettings.Shortened){formalEntranceComplete=true;return;}
            if(formalEntranceStarted<0)formalEntranceStarted=Time.unscaledTime;
            if(Time.unscaledTime-formalEntranceStarted>=2.4f)formalEntranceComplete=true;
        }

        private void DrawTitleAtmosphere()
        {
            float time=ArtSampleSettings.ReducedMotion || ArtSampleSettings.Shortened?0:Time.unscaledTime;
            SampleImage(new Rect(660+Mathf.Sin(time*.12f)*12,-20,980,940),"forest-far",true);
            TitleFill(new Rect(660,0,940,900),new Color(.035f,.05f,.10f,.62f));
            for(int i=0;i<12;i++){
                float x=760+i*65+Mathf.Sin(time*.18f+i)*10;
                float y=110+(i*109)%660+Mathf.Sin(time*.25f+i*.7f)*14;
                TitleFill(new Rect(x,y,2,2),new Color(titleGold.r,titleGold.g,titleGold.b,.35f));
            }
        }

        private void DrawFormalEntrance()
        {
            float phase=formalEntranceCapture?.48f:Mathf.Clamp01((Time.unscaledTime-formalEntranceStarted)/2.4f);
            float spread=ArtSampleSettings.ReducedMotion?130:Mathf.SmoothStep(45,130,phase);
            if(entranceLogoStyle==null){
                entranceLogoStyle=new GUIStyle(heading){fontSize=64,alignment=TextAnchor.MiddleCenter};
                entranceCaptionStyle=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};
            }
            TitleFill(new Rect(0,0,1600,900),new Color(titleInk.r,titleInk.g,titleInk.b,.82f));
            TitleBorder(new Rect(28,28,1544,844));
            TitleFill(new Rect(800-spread-209,288,430+spread*2,350),new Color(.40f,.32f,.21f));
            TitleFill(new Rect(800-spread-215,280,215+spread,350),new Color(.94f,.89f,.77f));
            TitleFill(new Rect(800,280,215+spread,350),new Color(.89f,.83f,.69f));
            TitleBorder(new Rect(800-spread-215,280,430+spread*2,350),2);
            TitleFill(new Rect(795,280,10,350),titleGold);
            for(int i=0;i<6;i++){
                TitleFill(new Rect(830,330+i*42,150+spread,1),new Color(.65f,.53f,.32f,.6f));
                TitleFill(new Rect(800-spread-185,330+i*42,150+spread,1),new Color(.65f,.53f,.32f,.6f));
            }
            TitleFill(new Rect(800+spread+115,280,30,92),new Color(.41f,.12f,.18f));
            Label(400,120,800,115,"newASTER",entranceLogoStyle,titleGold);
            Label(450,680,700,65,"記憶の頁が、いま開く。",entranceCaptionStyle,Color.white);
            if(TitleButton(1280,800,250,48,"導入をスキップ"))formalEntranceComplete=true;
        }

        private void DrawFormalStartupError()
        {
            TitleFill(new Rect(0,0,1600,900),titleInk);
            TitleBorder(new Rect(140,120,1320,660),2);
            Label(210,190,1180,80,"記憶をひらけませんでした",heading,titleGold);
            Label(210,300,1180,120,"定義または保存の読み込みに失敗しました。\n元の保存ファイルを上書きせず、ここで停止しています。",text,Color.white);
            Label(210,455,1180,145,combatDefinitionError,small,Color.white);
            if(TitleButton(210,665,1180,60,"ゲームを終了"))Application.Quit();
        }

        private void ValidateTitleSettingsCancel()
        {
            float bgm=ArtSampleSettings.Bgm,se=ArtSampleSettings.Se;
            bool motion=ArtSampleSettings.ReducedMotion,flash=ArtSampleSettings.ReducedFlash,shortened=ArtSampleSettings.Shortened,large=ArtSampleSettings.LargeText;
            int fullscreen=PlayerPrefs.GetInt("art.fullscreen",-1);
            OpenTitlePanel("settings");BeginTitleSettings();
            titleDraftBgm=bgm<.5f?1:0;titleDraftSe=se<.5f?1:0;
            titleDraftMotion=!motion;titleDraftFlash=!flash;titleDraftShortened=!shortened;
            titleDraftLargeText=!large;titleDraftFullscreen=!titleDraftFullscreen;
            CloseTitlePanel();
            if(titlePanel!=null || titleSettingsDraftReady || ArtSampleSettings.Bgm!=bgm || ArtSampleSettings.Se!=se || ArtSampleSettings.ReducedMotion!=motion || ArtSampleSettings.ReducedFlash!=flash || ArtSampleSettings.Shortened!=shortened || ArtSampleSettings.LargeText!=large || PlayerPrefs.GetInt("art.fullscreen",-1)!=fullscreen)
                throw new InvalidOperationException("Title settings cancellation changed saved preferences");
            OpenTitlePanel("settings");BeginTitleSettings();
            if(titleDraftBgm!=bgm || titleDraftSe!=se || titleDraftMotion!=motion || titleDraftFlash!=flash || titleDraftShortened!=shortened || titleDraftLargeText!=large)
                throw new InvalidOperationException("Title settings reopened with discarded values");
            Debug.Log("PLAN9_TITLE_SETTINGS_CANCEL_PASS saved-preferences-unchanged and reopened-original");
        }
    }
}
