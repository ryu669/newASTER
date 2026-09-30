using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private CampaignState campaign;
        private BookNavigationState book;
        private PlayableBattle encounter;
        private string battleId, activeColossus, target = "body";
        private string status = "しおりで選び、ページをめくって世界を訪ねましょう。";
        private bool title = true, paused, retreat, help, drawingModal;
        private string result, storyText, storyId;
        private int selectedLevel = 1, storyChapter;
        private GUIStyle text, heading, small, button;
        private Font font;
        private Texture2D paper, dark, teal;
        private Camera viewCamera;
        private Vector2 scroll;
        private GameObject dragon;
        private static readonly string[] Names = { "暁の剣士", "翼の砕き手", "誓いの守護者", "森の歌い手", "星の術師" };
        private static readonly string[] Jobs = { "剣士", "部位破壊", "防御", "回復", "術師" };
        private static readonly string[] PartNames = { "結晶角冠", "左翼の根", "右翼の装甲", "蔓の尾" };
        private static readonly string[] Effects = { "大技ゲージ上昇を止める", "敵の攻撃を弱める", "本体の軽減を解除", "資源妨害を止める" };
        private static readonly string[] Furniture = { "根のベンチ", "苔のランタン", "花のテーブル" };
        private static readonly string[] Chapters = { "最初の種", "忘れられた約束", "帰る場所" };
        private static readonly string[] Stories = {
            "空には、まだ地平線がなかった。\n\n竜が落とした結晶に触れると、歌がひとつ、指先に灯った。\n『土がなくても、種を忘れないで』\n森の歌い手が目を閉じる。失われた世界で、誰かが最後まで庭を守っていた。その記憶が、巨神獣の翼の下に眠っている。\n\nわたしたちはその歌を、本の最初のページに書き留めた。戦いのあとに残るものが、傷だけでないことを願いながら。",
            "竜の角には、雨の降らない季節が刻まれていた。\n\n庭師は毎朝、枯れた泉まで歩いた。水を汲めなくても、そこに待つ子供へ会うために。\n『明日、もう一度来る』\nただそれだけの約束が、崩れゆく世界をつなぎ止めていた。\n\n翼の砕き手が結晶を握る。壊した翼は、かつて誰かを雨雲へ運ぶためのものだった。わたしたちは本を閉じ、芽吹き始めた庭へ戻った。",
            "最後の歌は、竜の名を呼ばなかった。\n\n帰っておいで。木陰はまだ残っている。\n\n新しい星の土に、最初の根が伸びる。巨神獣から取り戻した森は、昔の世界と同じ形にはならない。それでも、根のベンチに腰を下ろした歌い手は笑った。\n『ここで、次の約束をしよう』\n\n本に記された過去が、いまの暮らしにつながる。わたしたちは新しいページをめくる。次の世界を救うために。そして、帰ってくるために。"
        };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => new GameObject("newASTER Playable").AddComponent<PrototypeBootstrap>();
        private void Awake()
        {
            campaign = CampaignSaveStore.TryLoad(out var save) ? new CampaignState(WorldCatalog.ColossusIds, save) : new CampaignState(WorldCatalog.ColossusIds);
            book = new BookNavigationState(new Dictionary<BookBookmark, IReadOnlyList<string>> {
                [BookBookmark.Colossi] = WorldCatalog.ColossusIds,
                [BookBookmark.Heroines] = Enumerable.Range(0,5).Select(i => "hero-" + i).ToArray(),
                [BookBookmark.Gardens] = new[] { "garden.grassland-forest" },
                [BookBookmark.Stories] = new[] { "story.green-return-dragon" }
            });
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 20);
            paper = Texture(new Color(.92f,.87f,.75f)); dark = Texture(new Color(.035f,.065f,.08f,.96f)); teal = Texture(new Color(.09f,.28f,.28f));
            viewCamera = new GameObject("Book View Camera").AddComponent<Camera>();
            viewCamera.transform.position = new Vector3(2,6,-10); viewCamera.transform.rotation = Quaternion.Euler(24,0,0);
            viewCamera.backgroundColor = new Color(.045f,.10f,.11f);
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45,-30,0); light.intensity = 1.4f;
            RenderSettings.ambientLight = new Color(.45f,.55f,.5f);
        }
        private static Texture2D Texture(Color color) { var t=new Texture2D(1,1); t.SetPixel(0,0,color); t.Apply(); return t; }
        private void Styles()
        {
            if(text!=null) return;
            text=new GUIStyle(GUI.skin.label) { font=font, fontSize=21, wordWrap=true }; text.normal.textColor=new Color(.18f,.22f,.21f);
            heading=new GUIStyle(text) { fontSize=31, fontStyle=FontStyle.Bold };
            small=new GUIStyle(text) { fontSize=17 };
            button=new GUIStyle(GUI.skin.button) { font=font, fontSize=19, wordWrap=true, padding=new RectOffset(10,10,6,6) };
            button.normal.background=teal; button.normal.textColor=new Color(.97f,.94f,.83f);
            button.hover.background=teal; button.hover.textColor=Color.white; button.active.background=dark; button.active.textColor=Color.white;
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)) {
                if(storyText!=null) CloseStory();
                else if(help) help=false;
                else if(retreat) { retreat=false; paused=false; }
                else if(encounter!=null && result==null) paused=!paused;
                else if(result==null) help=true;
            }
            viewCamera.rect=new Rect(.64f,0,.36f,1); viewCamera.aspect=Screen.width*.36f/Screen.height;
            bool gardenView=!title && encounter==null && book.Bookmark==BookBookmark.Gardens;
            viewCamera.transform.position=gardenView?new Vector3(-4,5,-8):new Vector3(2,6,-10);
            viewCamera.transform.LookAt(gardenView?new Vector3(-3,1,3):new Vector3(2.5f,2,1.2f));
            if(dragon==null) dragon=GameObject.Find("Green Return Dragon Blockout");
            if(dragon!=null) foreach(Transform part in dragon.transform) {
                int i=part.name.Contains("Horn")?0:part.name.Contains("Left")?1:part.name.Contains("Right")?2:part.name.Contains("Tail")?3:-1;
                if(i>=0) part.GetComponent<Renderer>().enabled=encounter==null || !encounter.State.Parts[i].IsBroken;
            }
        }
        private void OnGUI()
        {
            Styles(); GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1)); drawingModal=false;
            Panel(0,0,1024,900,paper); Panel(0,0,1600,80,dark);
            Label(32,20,950,46,"newASTER  /  巨神と誓女2",heading,Color.white);
            if(title) { DrawTitle(); return; }
            Label(1050,100,510,110,encounter==null?"記憶が、新しい世界を育てる。":"巨神獣との空中戦",heading,Color.white);
            Label(1050,225,510,100,"3D仮素材による試遊版\n5人・4部位の戦術と世界復元",small,Color.white);
            if(encounter==null) DrawBook(); else DrawBattle();
            Panel(0,812,1024,88,dark); Label(28,826,970,60,status,small,Color.white);
            drawingModal=true;
            if(storyText!=null) DrawStory(); else if(help) DrawHelp(); else if(retreat) DrawRetreat(); else if(result!=null) DrawResult();
        }
        private void DrawTitle()
        {
            Label(75,160,860,70,"無限の書をひらく",heading);
            Label(75,260,850,160,"巨神獣の記憶を集め、失われた森を新しい星へ。\n5人の誓女と戦い、武器の樹を育て、庭で物語を紡ぐ。",text);
            if(Btn(75,470,650,64,"冒険をはじめる / 続きから")) title=false;
            Label(75,570,850,150,"進行は自動保存されます。戦闘中の状態は保存せず、再開時は本に戻ります。\n人物が確定するまで5人は役割名で表示します。",small);
        }
        private void DrawBook()
        {
            string[] tabs={"巨神獣","誓女・育成","庭","物語"};
            for(int i=0;i<4;i++) if(Btn(28+i*242,100,230,46,(int)book.Bookmark==i?"◆ "+tabs[i]:tabs[i])) { book.ChangeBookmark((BookBookmark)i); scroll=Vector2.zero; }
            if(Btn(28,160,180,42,"‹ 前のページ")) book.TurnPage(-1);
            if(Btn(218,160,180,42,"次のページ ›")) book.TurnPage(1);
            if(Btn(408,160,180,42,book.Face==BookFace.Overview?"ページを裏返す":"表に戻す")) book.FlipPage();
            if(Btn(600,160,170,42,"保存")) Save(); if(Btn(780,160,200,42,"遊び方")) help=true;
            Label(30,220,950,34,$"素材 {campaign.Progress.Materials}  /  世界復元 {campaign.Progress.TerraformingExperience}  /  翠還竜の詩 {campaign.Progress.CollectedPoemIds.Count}/24",small);
            switch(book.Bookmark) {
                case BookBookmark.Colossi: DrawColossus(); break;
                case BookBookmark.Heroines: DrawHeroine(); break;
                case BookBookmark.Gardens: DrawGarden(); break;
                case BookBookmark.Stories: DrawStories(); break;
            }
        }
        private void DrawColossus()
        {
            var c=WorldCatalog.Colossi[book.SubjectIndex]; bool unlocked=campaign.ColossusUnlocks.IsUnlocked(c.Id);
            Label(32,278,930,65,$"{book.SubjectIndex+1:00}  {(unlocked?c.DisplayName:"？？？")}",heading);
            if(!unlocked) { Label(32,365,920,120,"前の巨神獣を初めて討伐すると、このページが開きます。\n最後の巨神獣には、14体すべての初回討伐が必要です。",text); return; }
            if(book.Face==BookFace.Details) {
                Label(32,365,920,100,"初回討伐で世界へ定着する環境："+string.Join("・",c.EnvironmentTags),text);
                Label(32,490,920,195,"角冠：大技ゲージ / 左翼：攻撃 / 右翼：装甲 / 尾：資源妨害\n部位を破壊してから本体を攻めると安全に戦えます。\n全15ページに共通の仮戦闘を使用しています。詩と物語は翠還竜に実装しています。",text);
            } else {
                Label(32,363,925,110,"巨神獣の体に残った呪歌は、失われた世界の記憶。\n討伐して環境を取り戻し、詩を集めると物語の章が開きます。",text);
                Label(32,485,900,45,$"挑戦 Lv.{selectedLevel}  /  選択可能 1〜{campaign.Playable.HighestLevel}",heading);
                if(Btn(32,548,140,42,"− 5")) selectedLevel=Math.Max(1,selectedLevel-5);
                if(Btn(186,548,140,42,"＋ 5")) selectedLevel=Math.Min(campaign.Playable.HighestLevel,selectedLevel+5);
                if(Btn(340,548,180,42,"最高レベル")) selectedLevel=campaign.Playable.HighestLevel;
                Label(32,612,925,58,"Lv45以上で極大技。勝利すると選択可能なLvが5上がります。",small);
                if(Btn(32,692,910,70,"5人の誓女と出撃する")) StartBattle(c.Id);
            }
        }
        private void DrawHeroine()
        {
            int h=book.SubjectIndex; var p=campaign.Playable;
            Label(32,275,930,55,$"{Names[h]}  /  {Jobs[h]}  /  Lv.{p.Levels[h]}",heading);
            Label(32,338,930,45,$"好感度 {p.Affections[h]}/100  ・  育成や好感度でチェイン率は変化しません。",small);
            if(book.Face==BookFace.Details) {
                Label(32,398,900,125,"固有スキル：通常攻撃 / 資源3の強撃 / 資源3の支援\n砕き手は部位攻撃1.5倍。歌い手の支援は全体回復。",text);
                Label(32,545,925,150,"毎回5人で出撃します。行動順は自由です。\n強撃を温存し、後半のチェインで使うと威力が増えます。\n花の枝を育てると支援スキルが強化されます。",text); return;
            }
            if(Btn(32,398,900,48,$"Lvを上げる  素材 {2+p.Levels[h]/5}  （上限50）",p.Levels[h]<50)) Mutate(p.Train(campaign.Progress,h),"誓女が成長しました。");
            Label(32,464,900,45,"武器の樹  /  根から3つの枝へ",heading);
            DrawWeaponTree(h);
            string[] branches={"剣の枝：攻撃","盾の枝：HP・防御","花の枝：支援"};
            for(int b=0;b<3;b++) {
                int rank=p.Branches[h*3+b]; float x=32+b*308;
                Label(x,525,294,55,branches[b],text);
                if(Btn(x,650,294,55,$"育てる  素材 {3+rank*3}",rank<3)) Mutate(p.Grow(campaign.Progress,h,b),"武器の枝に花が咲きました。");
            }
            Label(32,743,900,44,"初期装備（根） → 開放した枝に花が咲きます。",small);
        }
        private void DrawWeaponTree(int hero)
        {
            Line(new Vector2(494,639),new Vector2(494,616),new Color(.33f,.23f,.13f),9);
            for(int branch=0;branch<3;branch++) {
                float x=179+branch*308;
                Line(new Vector2(494,616),new Vector2(x,609),new Color(.33f,.23f,.13f),5);
                Line(new Vector2(x,609),new Vector2(x,561),new Color(.33f,.23f,.13f),5);
                int rank=campaign.Playable.Branches[hero*3+branch];
                for(int node=0;node<3;node++) Label(x-18,602-node*19,38,28,node<rank?"✿":"○",text,node<rank?new Color(.64f,.24f,.4f):new Color(.4f,.4f,.34f));
            }
            Label(407,619,180,30,"根：初期装備",small);
        }
        private static void Line(Vector2 from,Vector2 to,Color color,float width)
        {
            var matrix=GUI.matrix; var old=GUI.color; GUI.color=color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
            GUI.DrawTexture(new Rect(from.x,from.y-width/2,Vector2.Distance(from,to),width),Texture2D.whiteTexture);
            GUI.matrix=matrix; GUI.color=old;
        }
        private void DrawGarden()
        {
            bool unlocked=campaign.Gardens.UnlockedGardenIds.Count>0;
            Label(32,275,930,65,unlocked?"草原と森の庭":"まだ白い庭",heading);
            if(!unlocked) { Label(32,365,920,130,"翠還竜の初回討伐で草原と森が戻り、庭が開放されます。\n家具を作り、仲間とここで過ごせるようになります。",text); return; }
            if(book.Face==BookFace.Details) {
                Label(32,360,920,60,"庭へ訪ねてきた誓女  /  素材1でお茶と会話",text);
                for(int h=0;h<5;h++) if(Btn(32,440+h*61,910,51,$"{Names[h]}と話す  /  好感度 {campaign.Playable.Affections[h]}",campaign.Playable.Affections[h]<100)) {
                    if(campaign.Playable.Visit(campaign.Progress,h)) {
                        Save(); storyId=null; scroll=Vector2.zero;
                        storyText=$"{Names[h]}\n\n『今日も帰ってきてくれて、ありがとう。』\n\n木漏れ日の下で、ふたりは温かな茶を分け合った。"+(campaign.Playable.Affections[h]>=20?"\n\n『この庭が、あなたの帰る場所になるといいな。次は、わたしから迎えに行くね。』":"\n\n風が新しい葉を揺らす。次の旅まで、少しだけここで休もう。");
                    } else status="素材が不足しています。討伐で集めましょう。";
                } return;
            }
            Label(32,355,925,65,"家具を作り、3つの場所へ配置できます。裏面から誓女との会話へ。",text);
            for(int i=0;i<3;i++) {
                Label(32,443+i*93,285,45,Furniture[i],heading);
                if(!campaign.Playable.Furniture[i]) { if(Btn(330,439+i*93,610,55,$"作る  素材 {4+i*2}")) Mutate(campaign.Playable.Craft(campaign.Progress,i),"家具を作りました。"); }
                else for(int slot=0;slot<3;slot++) if(Btn(330+slot*205,439+i*93,194,55,$"場所{slot+1}に置く")) Mutate(campaign.Playable.Place(i,slot),"家具を配置しました。");
            }
            Label(32,735,925,46,"配置："+string.Join(" / ",campaign.Playable.Slots.Select(i=>i<0?"空き":Furniture[i])),small);
        }
        private void DrawStories()
        {
            Label(32,275,930,65,"翠還竜の記憶",heading);
            Label(32,353,920,65,"討伐ごとに未取得の詩を4つ獲得。8つ集めると1章を読めます。",text);
            for(int i=0;i<3;i++) {
                var chapter=GreenReturnDragonVerticalSlice.StoryChapters[i]; int n=chapter.RequiredPoemIds.Count(id=>campaign.Progress.CollectedPoemIds.Contains(id));
                bool open=campaign.Progress.UnlockedStoryIds.Contains(chapter.StoryId);
                if(Btn(32,457+i*92,910,74,$"第{i+1}章  {Chapters[i]}  /  詩 {n}/8  {(campaign.Progress.ReadStoryIds.Contains(chapter.StoryId)?"既読":open?"読めます":"未開放")}",open)) { storyText=Stories[i]; storyId=chapter.StoryId; storyChapter=i; scroll=Vector2.zero; }
            }
            Label(32,754,910,38,"物語は試遊用のオリジナル短編です。",small);
        }
        private void StartBattle(string colossus)
        {
            activeColossus=colossus; battleId=Guid.NewGuid().ToString("N"); encounter=new PlayableBattle(selectedLevel,campaign.Playable);
            target="body"; paused=false; result=null; status="対象を選び、誓女のスキルで攻撃してください。";
        }
        private void DrawBattle()
        {
            var s=encounter.State;
            Label(28,98,950,53,$"{WorldCatalog.Colossi.First(c=>c.Id==activeColossus).DisplayName}  Lv.{s.SelectedLevel}  /  TURN {encounter.Turn}",heading);
            Label(28,159,910,36,$"本体 HP {s.BossHitPoints}/{s.BossMaxHitPoints}  /  大技 {s.BossGauge}/{s.BossGaugeMax}  /  {encounter.Chain} CHAIN",text);
            if(Btn(28,213,180,48,(target=="body"?"◆ ":"")+"本体")) target="body";
            for(int i=0;i<4;i++) if(Btn(219+i*188,213,178,48,(target==s.Parts[i].Id?"◆ ":"")+PartNames[i],!s.Parts[i].IsBroken)) target=s.Parts[i].Id;
            for(int i=0;i<4;i++) Label(28+i*238,278,232,91,$"{PartNames[i]}：{(s.Parts[i].IsBroken?"破壊済":s.Parts[i].HitPoints.ToString())}\n{Effects[i]}",small);
            var bonuses=string.Join("  /  ",s.TurnChainModifiers.Select((m,i)=>Names[i]+" "+(m.AdditiveRate>0?"+"+(m.AdditiveRate*100)+"%":"—")));
            Label(28,357,948,27,"今ターンのCHAIN補正： "+bonuses,small);
            for(int i=0;i<5;i++) {
                var h=s.Heroes[i]; float y=390+i*65; Label(28,y,265,58,$"{Names[i]}\nHP {h.HitPoints}/{h.MaxHitPoints}  資源 {h.JobResource}",small);
                bool enabled=!paused && result==null && !encounter.Acted[i] && h.IsAlive;
                if(Btn(304,y,202,54,encounter.Acted[i]?"行動済":"通常攻撃",enabled)) Act(i,0);
                if(Btn(518,y,202,54,"強撃  資源3",enabled && h.JobResource>=3)) Act(i,1);
                if(Btn(732,y,244,54,i==3?"癒しの歌  資源3":"護りの誓い  資源3",enabled && h.JobResource>=3)) Act(i,2);
            }
            if(Btn(28,727,294,50,paused?"再開する":"一時停止")) paused=!paused;
            if(Btn(340,727,294,50,"ターンを終える",!paused && result==null)) { encounter.EndTurn(); FinishCheck(); }
            if(Btn(652,727,324,50,"撤退して本へ")) { retreat=true; paused=true; }
            status=paused?"一時停止中。再開するボタンで戻れます。":encounter.Log;
        }
        private void Act(int hero,int skill)
        {
            if(encounter.Act(hero,skill,target)) { if(target!="body" && encounter.State.Parts.First(p=>p.Id==target).IsBroken) target="body"; FinishCheck(); }
        }
        private void FinishCheck()
        {
            if(!encounter.Ended || result!=null) return;
            if(!encounter.State.IsVictory) { result="敗北\n\n報酬はありません。育成や部位破壊を試して再挑戦しましょう。"; return; }
            var c=WorldCatalog.Colossi.First(x=>x.Id==activeColossus);
            var poems=activeColossus==GreenReturnDragonVerticalSlice.ColossusId?GreenReturnDragonVerticalSlice.PoemIds.Where(id=>!campaign.Progress.CollectedPoemIds.Contains(id)).Take(4).ToArray():Array.Empty<string>();
            var reward=campaign.ClaimColossusVictory(activeColossus,c.EnvironmentTags,new VictoryReward(battleId,selectedLevel,10,4,poems),GreenReturnDragonVerticalSlice.StoryChapters,Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            campaign.Playable.RecordVictory(selectedLevel);
            result=$"討伐成功！\n\n素材 +{reward.Reward.Materials} / 世界復元 +{reward.Reward.Terraforming}\n新しい詩 {reward.Reward.NewPoemIds.Count} / 開いた章 {reward.Reward.NewStoryIds.Count}\n";
            if(reward.FirstClear) result+="\n初回討伐：次のページと環境が開放されました。";
            if(reward.NewGardenIds.Count>0) result+="\n庭が開放！庭のしおりから訪ねましょう。";
            if(c.IsIntegrationBoss) result+="\n\n世界統合達成。取り戻した世界に、新しい物語が始まります。";
            Save();
        }
        private void DrawResult()
        {
            Modal(); Label(340,194,890,90,encounter.State.IsVictory?"記憶を取り戻した":"再び、誓いを",heading); Label(340,300,890,285,result,text);
            if(!encounter.State.IsVictory && Btn(340,580,860,52,"同じ巨神獣・難度で再挑戦")) { StartBattle(activeColossus); return; }
            if(Btn(340,650,420,62,"本へ戻る")) { result=null; encounter=null; status="報酬を使って育成・庭を進めましょう。"; }
            if(Btn(780,650,420,62,"育成ページへ")) { result=null; encounter=null; book.ChangeBookmark(BookBookmark.Heroines); }
        }
        private void DrawRetreat()
        {
            Modal(); Label(340,245,880,90,"撤退しますか？",heading); Label(340,365,870,125,"この戦闘の報酬は得られません。これまでの育成や獲得した記憶は保持されます。",text);
            if(Btn(340,605,420,64,"戦闘へ戻る")) { retreat=false; paused=false; }
            if(Btn(780,605,420,64,"撤退する")) { retreat=false; encounter=null; result=null; Save(); }
        }
        private void DrawStory()
        {
            Modal(); Label(340,182,880,65,storyId==null?"庭でのひととき":$"第{storyChapter+1}章  {Chapters[storyChapter]}",heading);
            scroll=GUI.BeginScrollView(new Rect(340,270,890,335),scroll,new Rect(0,0,855,650)); GUI.Label(new Rect(0,0,840,650),storyText,text); GUI.EndScrollView();
            if(Btn(340,656,890,62,storyId==null?"庭へ戻る":"読み終えて本に戻る")) CloseStory();
        }
        private void CloseStory() { if(storyId!=null) campaign.Progress.MarkStoryRead(storyId); storyText=null; storyId=null; Save(); }
        private void DrawHelp()
        {
            Modal(); Label(340,185,880,64,"遊び方",heading);
            Label(340,275,880,355,"1. 巨神獣のページから5人で出撃。\n2. 対象を選び、誓女を好きな順に行動させる。\n3. ターンの後半ほどチェインで攻撃が強くなる。\n4. 部位破壊で敵の能力を弱める。\n5. 報酬で誓女と武器を育て、家具を作る。\n6. 詩を集めたら物語のしおりで読む。\n\nめくる＝対象変更。裏返す＝同じ対象の詳細。\nEsc＝一時停止。進行は操作・討伐後に自動保存。",text);
            if(Btn(340,656,890,62,"閉じる")) help=false;
        }
        private void Mutate(bool success,string message) { if(success) { Save(); status=message; } else status="素材が不足しているか、すでに最大まで開放されています。"; }
        private void Save()
        {
            try { CampaignSaveStore.Save(campaign); status="進行を保存しました。"; }
            catch(Exception e) when(e is System.IO.IOException || e is UnauthorizedAccessException) { status="保存できませんでした。保存先の空き容量と権限を確認してください。"; Debug.LogException(e); }
        }
        private void Modal() { Panel(0,80,1600,820,dark); Panel(300,150,970,620,paper); }
        private static void Panel(float x,float y,float w,float h,Texture2D t) => GUI.DrawTexture(new Rect(x,y,w,h),t);
        private void Label(float x,float y,float w,float h,string value,GUIStyle style,Color? color=null) { var old=style.normal.textColor; if(color.HasValue) style.normal.textColor=color.Value; GUI.Label(new Rect(x,y,w,h),value,style); style.normal.textColor=old; }
        private bool Btn(float x,float y,float w,float h,string value,bool enabled=true)
        {
            bool old=GUI.enabled; GUI.enabled=old && enabled && (drawingModal || !(storyText!=null || help || retreat || result!=null));
            bool clicked=GUI.Button(new Rect(x,y,w,h),value,button); GUI.enabled=old; return clicked;
        }
        private void OnApplicationQuit() { if(campaign!=null) Save(); }
    }
}
