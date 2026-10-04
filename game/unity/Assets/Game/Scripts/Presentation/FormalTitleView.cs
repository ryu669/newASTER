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

        private void TitleFill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        // Code-native ornament: no additional bitmap or borrowed logo is required.
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
            SampleImage(new Rect(700, 0, 900, 900), "slayer-standing");
            TitleFill(new Rect(0, 0, 775, 900), new Color(.035f, .05f, .10f, .96f));
            TitleFill(new Rect(775, 0, 825, 900), new Color(.035f, .05f, .10f, .12f));
            TitleBorder(new Rect(28, 28, 1544, 844));
            TitleBorder(new Rect(48, 48, 720, 804));
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
                book.Reenter(); title = false; titlePanel = null;
            }
            if (TitleButton(140, 594, 255, 50, "設定")) titlePanel = "settings";
            if (TitleButton(415, 594, 255, 50, "クレジット")) titlePanel = "credits";
            if (TitleButton(140, 660, 530, 50, "ゲームを終了")) titlePanel = "exit";
            Label(105, 771, 600, 38, "記憶は、ここから新しい世界になる。", titleSubtitle, titleGold);
            if (TitleButton(1320, 805, 220, 42, "制作・試遊メニュー")) titlePanel = "development";
            GUI.enabled = previousEnabled;
            if (titlePanel != null) DrawTitlePanel();
        }

        private void DrawFormalBookSurface()
        {
            TitleFill(new Rect(0, 0, 1600, 900), titleInk);
            // Layered paper and a shaded binding preserve the book's information layout.
            TitleFill(new Rect(6, 92, 1018, 720), new Color(.52f, .43f, .30f));
            TitleFill(new Rect(3, 87, 1015, 718), new Color(.79f, .72f, .59f));
            Panel(0, 80, 1010, 720, paper);
            TitleFill(new Rect(996, 80, 3, 720), new Color(.70f, .59f, .40f));
            TitleFill(new Rect(1000, 80, 10, 720), new Color(.38f, .30f, .23f));
            TitleBorder(new Rect(12, 90, 976, 710));
        }

        private void DrawTitlePanel()
        {
            // The foreground blocks the entire title; buttons cannot click through.
            if (Event.current.type == EventType.MouseDown && !new Rect(350, 140, 900, 620).Contains(Event.current.mousePosition))
            { Event.current.Use(); return; }
            TitleFill(new Rect(0, 0, 1600, 900), new Color(0, 0, 0, .7f));
            TitleFill(new Rect(350, 140, 900, 620), titleInk);
            TitleBorder(new Rect(360, 150, 880, 600), 2);
            if (titlePanel == "settings")
            {
                Label(400, 185, 750, 60, "設定", heading, titleGold);
                Label(400, 280, 240, 40, "BGM音量", text, Color.white);
                float bgm = GUI.HorizontalSlider(new Rect(650, 295, 470, 30), ArtSampleSettings.Bgm, 0, 1);
                if (Math.Abs(bgm-ArtSampleSettings.Bgm) > .001) PlayerPrefs.SetFloat("art.bgm", bgm);
                Label(400, 365, 240, 40, "効果音量", text, Color.white);
                float se = GUI.HorizontalSlider(new Rect(650, 380, 470, 30), ArtSampleSettings.Se, 0, 1);
                if (Math.Abs(se-ArtSampleSettings.Se) > .001) PlayerPrefs.SetFloat("art.se", se);
                if (TitleButton(400, 470, 720, 52, "演出短縮："+(ArtSampleSettings.Shortened ? "ON" : "OFF")))
                    PlayerPrefs.SetInt("art.shortened", ArtSampleSettings.Shortened ? 0 : 1);
                if (TitleButton(400, 540, 720, 52, "揺れ軽減："+(ArtSampleSettings.ReducedMotion ? "ON" : "OFF")))
                    PlayerPrefs.SetInt("art.motion", ArtSampleSettings.ReducedMotion ? 0 : 1);
            }
            else if (titlePanel == "credits")
            {
                Label(400, 185, 750, 60, "クレジット", heading, titleGold);
                Label(400, 285, 750, 260, "newASTER / 巨神と誓女2\n\n日本語フォント：Noto Sans CJK JP\nSIL Open Font License 1.1\n\n同梱の ThirdPartyNotices にライセンス全文を収録。\n制作素材の最終クレジットは正式採用時に追記します。", text, Color.white);
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
            if (TitleButton(400, 655, 720, 52, "閉じる")) {PlayerPrefs.Save(); titlePanel=null;}
        }
    }
}
