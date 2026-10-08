using System;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string titlePanel;
        private readonly Color titleGold = new Color(.79f, .65f, .37f);
        private readonly Color titleInk = new Color(.035f, .05f, .10f);
        private GUIStyle titleLogo, titleSubtitle;
        private bool titleSettingsDraftReady;
        private float titleDraftBgm, titleDraftSe;
        private int titleDraftTextSpeed;
        private bool titleDraftShortened, titleDraftMotion, titleDraftFlash,titleDraftLargeText,titleDraftFullscreen;
        private Vector2 titleCreditsScroll;
        private string titleSettingsError;

        private void OpenTitlePanel(string panel)
        {
            titlePanel=panel;titleCreditsScroll=Vector2.zero;titleSettingsDraftReady=false;titleSettingsError=null;
        }

        private void BeginTitleSettings()
        {
            titleDraftBgm=ArtSampleSettings.Bgm;titleDraftSe=ArtSampleSettings.Se;
            titleDraftTextSpeed=PlayerPrefs.GetInt("plan6.text-speed",30);
            titleDraftShortened=ArtSampleSettings.Shortened;titleDraftMotion=ArtSampleSettings.ReducedMotion;
            titleDraftFlash=ArtSampleSettings.ReducedFlash;titleDraftLargeText=ArtSampleSettings.LargeText;
            titleDraftFullscreen=PlayerPrefs.GetInt("art.fullscreen",Screen.fullScreenMode==FullScreenMode.FullScreenWindow?1:0)==1;
            titleSettingsDraftReady=true;
        }

        private void CloseTitlePanel()
        {titlePanel=null;titleSettingsDraftReady=false;titleSettingsError=null;}

        private void ApplyTitleSettings()
        {
            try {
                PlayerPrefs.SetFloat("art.bgm",titleDraftBgm);PlayerPrefs.SetFloat("art.se",titleDraftSe);
                PlayerPrefs.SetInt("plan6.text-speed",titleDraftTextSpeed);
                PlayerPrefs.SetInt("art.shortened",titleDraftShortened?1:0);
                PlayerPrefs.SetInt("art.motion",titleDraftMotion?1:0);PlayerPrefs.SetInt("art.flash",titleDraftFlash?1:0);
                PlayerPrefs.SetInt("art.large-text",titleDraftLargeText?1:0);PlayerPrefs.SetInt("art.fullscreen",titleDraftFullscreen?1:0);
                PlayerPrefs.Save();
                if(!formalDiagnostic)Screen.fullScreenMode=titleDraftFullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
                text=null;heading=null;small=null;button=null;skillButton=null;
                growthTitleStyle=null;growthTextStyle=null;growthSmallStyle=null;growthButtonStyle=null;
                titleLogo=null;titleSubtitle=null;entranceLogoStyle=null;entranceCaptionStyle=null;
                CloseTitlePanel();
            } catch(Exception e){titleSettingsError="設定を保存できません。もう一度お試しください。";Debug.LogWarning("TITLE_SETTINGS_SAVE_FAILED "+e.GetType().Name);}
        }

        private void TitleFill(Rect rect, Color color)
        {
            ImageUiSkin.Surface(rect,color);
        }

        // Thin rules complement the image frames.
        private void TitleBorder(Rect rect, float weight = 1)
        {
            TitleFill(new Rect(rect.x, rect.y, rect.width, weight), titleGold);
            TitleFill(new Rect(rect.x, rect.yMax-weight, rect.width, weight), titleGold);
            TitleFill(new Rect(rect.x, rect.y, weight, rect.height), titleGold);
            TitleFill(new Rect(rect.xMax-weight, rect.y, weight, rect.height), titleGold);
            foreach (float x in new[] { rect.x, rect.xMax-26 })
                foreach (float y in new[] { rect.y, rect.yMax-26 })
                    TitleFill(new Rect(x, y, 26, 3), titleGold);
        }

        private bool TitleButton(float x, float y, float w, float h, string caption, bool enabled=true)
        { GrowthStyles(); return GrowthButton(x,y,w,h,caption,enabled); }

        private void DrawFormalTitle()
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && titlePanel == null;
            TitleFill(new Rect(0, 0, 1600, 900), titleInk);
            DrawTitleAtmosphere();
            if(FormalEntranceVisible){DrawFormalEntrance();GUI.enabled=previousEnabled;return;}
            SampleImage(new Rect(700, 0, 900, 900), "slayer-standing");
            TitleFill(new Rect(0, 0, 775, 900), new Color(.035f, .05f, .10f, .96f));
            TitleFill(new Rect(775, 0, 825, 900), new Color(.035f, .05f, .10f, .12f));
            if (titleLogo == null)
            {
                titleLogo = new GUIStyle(heading) {fontSize = 78, alignment = TextAnchor.MiddleCenter};
                titleSubtitle = new GUIStyle(small) {alignment = TextAnchor.MiddleCenter};
            }
            Label(80, 120, 650, 35, "記憶を綴り、世界を咲かせる", titleSubtitle, titleGold);
            Label(65, 176, 680, 120, "newASTER", titleLogo, new Color(.96f, .91f, .78f));
            Label(80, 307, 650, 45, "巨神と誓女2", titleSubtitle, titleGold);
            TitleFill(new Rect(175, 378, 460, 1), titleGold);
            Label(105, 404, 600, 72, "失われた世界の記憶が、\nあなたの開く一頁から芽吹く。", titleSubtitle, Color.white);
            if (TitleButton(140, 510, 530, 68, "万物の書をひらく", BookInputAllowed))
            {
                book.Reenter();heroineRosterOpen=false;growthScreen=book.Face==NewAster.Core.BookFace.Details?GrowthScreen.Information:GrowthScreen.Overview; title = false; CloseTitlePanel();
            }
            if (TitleButton(140, 594, 255, 50, "設定")) OpenTitlePanel("settings");
            if (TitleButton(415, 594, 255, 50, "クレジット")) OpenTitlePanel("credits");
            if (TitleButton(140, 660, 255, 50, "操作説明")) OpenTitlePanel("help");
            if (TitleButton(415, 660, 255, 50, "ゲームを終了")) OpenTitlePanel("exit");
            Label(105, 771, 600, 38, "記憶は、ここから新しい世界になる。", titleSubtitle, titleGold);
            if (!ProductionStoryActive && TitleButton(1320, 805, 220, 42, "制作・試遊メニュー")) OpenTitlePanel("development");
            GUI.enabled = previousEnabled;
            if (titlePanel != null) DrawTitlePanel();
        }

        private void DrawFormalBookSurface()
        {
            TitleFill(new Rect(0, 0, 1600, 900), titleInk);
            // The image owns its complete border; legacy paper layers and straight outlines are removed.
            Panel(0, 80, 1010, 720, paper);
        }

        private void DrawTitlePanel()
        {
            // The foreground blocks the entire title; buttons cannot click through.
            if (Event.current.type == EventType.MouseDown && !new Rect(350, 140, 900, 620).Contains(Event.current.mousePosition))
            { Event.current.Use(); return; }
            TitleFill(new Rect(0, 0, 1600, 900), new Color(0, 0, 0, .7f));
            TitleFill(new Rect(350, 140, 900, 620), titleInk);
            if (titlePanel == "settings")
            {
                if(!titleSettingsDraftReady)BeginTitleSettings();
                Label(400, 185, 750, 60, "設定", heading, titleGold);
                Label(400, 280, 240, 40, "BGM音量", text, Color.white);
                titleDraftBgm=ImageUiSkin.HorizontalSlider(new Rect(650, 295, 400, 30),titleDraftBgm,0,1);
                Label(1070,280,90,40,Mathf.RoundToInt(titleDraftBgm*100)+"%",small,Color.white);
                Label(400, 365, 240, 40, "効果音量", text, Color.white);
                titleDraftSe=ImageUiSkin.HorizontalSlider(new Rect(650, 380, 400, 30),titleDraftSe,0,1);
                Label(1070,365,90,40,Mathf.RoundToInt(titleDraftSe*100)+"%",small,Color.white);
                if (TitleButton(400, 440, 720, 45, "演出短縮："+(titleDraftShortened ? "ON" : "OFF")))titleDraftShortened=!titleDraftShortened;
                if (TitleButton(400, 497, 350, 45, "揺れ軽減："+(titleDraftMotion ? "ON" : "OFF")))titleDraftMotion=!titleDraftMotion;
                if (TitleButton(770, 497, 350, 45, "フラッシュ軽減："+(titleDraftFlash ? "ON" : "OFF")))titleDraftFlash=!titleDraftFlash;
                if(TitleButton(400,554,350,45,"文字："+(titleDraftLargeText?"大きめ":"標準")))titleDraftLargeText=!titleDraftLargeText;
                if(TitleButton(770,554,350,45,"表示："+(titleDraftFullscreen?"全画面":"ウィンドウ")))titleDraftFullscreen=!titleDraftFullscreen;
                if(TitleButton(400,605,350,40,"文章速度："+titleDraftTextSpeed+"字／秒")){int[] speeds={15,30,60,120};titleDraftTextSpeed=speeds[(Array.IndexOf(speeds,titleDraftTextSpeed)+1)%4];}
                Label(770,605,350,40,titleSettingsError??"適用して保存 ／ 取消で元に戻す",small,titleSettingsError==null?Color.white:titleGold);
                if(TitleButton(400,655,350,52,"取消"))CloseTitlePanel();
                if(TitleButton(770,655,350,52,"適用して閉じる"))ApplyTitleSettings();
                return;
            }
            else if (titlePanel == "credits")
            {
                Label(400, 185, 750, 60, "クレジット", heading, titleGold);
                string credits="newASTER / 巨神と誓女2\nVersion "+Application.version+"\n\n日本語フォント：Noto Sans CJK JP\nSIL Open Font License 1.1\n\n同梱の ThirdPartyNotices/NotoSansCJKjp に\nライセンス全文とNOTICEを収録。\n\n計画10 追加天使を含む開発版\n人物・巨神獣・背景・家具・CG：\nnewASTER用に制作したAI生成美術\n本文・戦闘ルール・UI・音：独自制作\nBGM4曲と操作音4種、戦闘音5種を収録。\n\n参考ゲーム映像は制作上の観察資料です。\n原作の画像・音声を本配布物へ収録しません。\n\n素材ごとの採用記録と既知の制限は\n同梱のCREDITS・READMEを参照してください。";
                float height=Math.Max(320,text.CalcHeight(new GUIContent(credits),700));
                titleCreditsScroll=GUI.BeginScrollView(new Rect(400,275,750,320),titleCreditsScroll,new Rect(0,0,700,height));
                Label(0,0,700,height,credits,text,Color.white);GUI.EndScrollView();
            }
            else if(titlePanel=="help"){
                Label(400,185,750,60,"万物の書の読み方",heading,titleGold);
                Label(400,280,750,290,"しおり：巨神獣・誓女・庭・物語の分類を選ぶ。\nめくり：同じ分類の中で、次の対象へ移る。\n裏返し：同じ対象の能力・部位・記憶を読む。\n\n戦闘と庭は「操作を開く」からメニューを展開。\nEscapeでパネルを閉じ、一つ前の状態へ戻ります。\n\n育成・召喚・交換は費用を確認し、確定後に保存。\n本文は一度の入力で全文表示、次の入力で進みます。",text,Color.white);
            }
            else if (titlePanel == "exit")
            {
                Label(400, 205, 750, 60, "ゲームを終了しますか？", heading, titleGold);
                Label(400, 300, 750, 100, "確定済みの記憶は、次に本を開くときも残ります。", text, Color.white);
                if (TitleButton(400, 475, 720, 64, "終了する")) Application.Quit();
            }
            else
            {
                Label(400, 185, 750, 60, "制作・試遊メニュー", heading, titleGold);
                if (TitleButton(400, 300, 720, 56, "計画8 ／ オリジナル試遊", BookInputAllowed)) {titlePanel=null; EnterPlan8StoryTrial();}
                if (TitleButton(400, 385, 720, 56, "計画7 ／ 美術見本", BookInputAllowed)) {titlePanel=null; OpenArtSample();}
                if (TitleButton(400, 470, 720, 56, "星の恵み", BookInputAllowed)) {titlePanel=null; OpenEngagement();}
            }
            if (TitleButton(400, 655, 720, 52, "閉じる")) CloseTitlePanel();
        }
    }
}
