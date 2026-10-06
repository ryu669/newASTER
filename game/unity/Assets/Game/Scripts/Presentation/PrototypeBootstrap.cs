using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap : MonoBehaviour
    {
        private CampaignState campaign;
        private BookNavigationState book;
        private PlayableBattle encounter;
        private string battleId, activeColossus, target = "body";
        private string status = "しおりで選び、ページをめくって世界を訪ねましょう。";
        private bool title = true, paused, retreat, help, kinderGarden, drawingModal;
        private string result, storyText, storyId;
        private string kinderResult = "素材で育成し、重複した誓女でステータスを強化できます。";
        private int selectedLevel = 1, storyChapter, storyPage, selectedHero;
        private bool selectingAlly;
        private readonly HashSet<int> selectedAllies = new HashSet<int>();
        private int healingActor, healingSlot;
        private string breakNotice = "";
        private float breakNoticeRemaining;
        private GUIStyle text, heading, small, button, skillButton;
        private Font font;
        private Texture2D paper, dark, teal;
        private Camera viewCamera;
        private string capturePath;
        private int captureFrame,capturedAtFrame=-1;
        private Vector2 scroll;
        private VerticalSliceBlockout stage;
        private BattleIllustrationView illustrationView;
        private CombatDefinitionCatalog combatDefinitions;
        private HeroineReferenceCatalog heroineReferences;
        private string combatDefinitionError;
        private float illustrationElapsed;
        private readonly BattlePlaybackQueue playback=new BattlePlaybackQueue();
        private long shownEvent;
        private bool slayerReview;
        private bool modelViewer, portraitFace=true;
        private float portraitYaw=-20, portraitZoom=1;
        private static readonly string[] Names = { "暁の剣士", "翼の砕き手", "誓いの守護者", "森の歌い手", "星の術師" };
        private static readonly string[] Jobs = { "剣士", "部位破壊", "防御", "回復", "ブラスター検証" };
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
            // Exit before opening any ordinary save store or initializing gameplay.
            if (TrialBaselineResources.RunDiagnostic(Environment.GetCommandLineArgs())) { enabled = false; return; }
            InitializeTrialTelemetry();
            campaign = new CampaignState(WorldCatalog.ColossusIds);
            book = new BookNavigationState(new Dictionary<BookBookmark, IReadOnlyList<string>> {
                [BookBookmark.Colossi] = WorldCatalog.ColossusIds,
                [BookBookmark.Heroines] = Enumerable.Range(0,5).Select(i => "hero-" + i).ToArray(),
                [BookBookmark.Gardens] = new[] { "garden.grassland-forest" },
                [BookBookmark.Stories] = new[] { "story.green-return-dragon" }
            });
            font = Resources.Load<Font>("Fonts/NotoSansCJKjp-Regular");
            if (font == null) throw new InvalidOperationException("Bundled Japanese font is missing: Fonts/NotoSansCJKjp-Regular");
            foreach (char glyph in "庭戦闘部位破壊設定喜困決意帰還図鑑0123456789！？")
                if (!font.HasCharacter(glyph)) throw new InvalidOperationException("Bundled font is missing glyph: " + glyph);
            Debug.Log("PLAN7_BUNDLED_FONT_PASS NotoSansCJKjp-Regular");
            paper = Texture(new Color(.92f,.87f,.75f)); dark = Texture(new Color(.035f,.065f,.08f,.96f)); teal = Texture(new Color(.075f,.145f,.20f));
            illustrationView=new BattleIllustrationView("Illustrations/battle-formal");
            viewCamera = new GameObject("Book View Camera").AddComponent<Camera>();
            viewCamera.gameObject.AddComponent<AudioListener>();
            viewCamera.transform.position = new Vector3(2,6,-10); viewCamera.transform.rotation = Quaternion.Euler(24,0,0);
            viewCamera.backgroundColor = new Color(.045f,.10f,.11f);
            viewCamera.allowMSAA=true; QualitySettings.antiAliasing=4;
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45,-30,0); light.intensity = 1.4f;
            RenderSettings.ambientLight = new Color(.45f,.55f,.5f);
            try {
                var referenceSource=Resources.Load<TextAsset>("Combat/heroine-reference");
                if(referenceSource==null) throw new ArgumentException("Combat/heroine-reference missing");
                heroineReferences=JsonUtility.FromJson<HeroineReferenceCatalog>(referenceSource.text);heroineReferences.Validate();
                Debug.Log("HEROINE_REFERENCE_PASS version=1 status=reference-only heroes=5 skills=15");
                var source=Resources.Load<TextAsset>(Environment.GetCommandLineArgs().Contains("-captureNighthawk")?"Combat/battle-plan10-nighthawk":Environment.GetCommandLineArgs().Contains("-captureOriflamme")?"Combat/battle-plan10-oriflamme":Environment.GetCommandLineArgs().Contains("-captureShell")?"Combat/battle-plan10-shell":Environment.GetCommandLineArgs().Contains("-captureAnnihilator")?"Combat/battle-plan10-annihilator":Environment.GetCommandLineArgs().Contains("-capturePlan10")?"Combat/battle-plan10":Environment.GetCommandLineArgs().Contains("-presentationCapture")?"Combat/battle-formal":"Combat/battle-plan10-nighthawk");
                if(source==null) throw new ArgumentException("Combat/battle-formal missing");
                combatDefinitions=JsonUtility.FromJson<CombatDefinitionCatalog>(source.text);combatDefinitions.Validate();
                Debug.Log("COMBAT_DEFINITIONS_PASS version=3 status=newaster-original heroes="+combatDefinitions.heroines.Length+" skills="+combatDefinitions.skills.Length+" chains="+combatDefinitions.chainActions.Length);
                InitializeFormalGrowth();
                InitializeKinder();
                InitializeEngagement();
                if(recoveryActive)return;
            } catch(Exception e) { combatDefinitionError=e.Message;Debug.LogError("COMBAT_DEFINITIONS_ERROR "+combatDefinitionError);return; }
            var args=Environment.GetCommandLineArgs();
            InitializeFormalEntrance(args);
            InitializePlan9EnemyReview(args);
            int expressionIndex=Array.IndexOf(args,"-inspectPlan9Expression");
            if(expressionIndex>=0 && expressionIndex+1<args.Length)plan9Expression=args[expressionIndex+1];
            if(args.Contains("-inspectPlan9ArtSlayer"))plan9ArtHero="heroine.slayer";
            if(args.Contains("-inspectPlan9ArtUndermine"))plan9ArtHero="heroine.undermine";
            if(args.Contains("-inspectPlan9ArtEchidna"))plan9ArtHero="heroine.echidna";
            if(args.Contains("-inspectPlan9ArtExcalipan"))plan9ArtHero="heroine.excalipan";
            int cgIndex=Array.IndexOf(args,"-inspectPlan9Cg");
            if(cgIndex>=0 && cgIndex+1<args.Length)plan9Cg=args[cgIndex+1];
            if(plan9Cg!=null)Debug.Log("PLAN9_CG_CAPTURE heroine="+plan9ArtHero+" event="+plan9Cg);
            slayerReview=args.Contains("-captureSlayerCloseup");
            for(int i=0;i<args.Length-1;i++) if(args[i]=="-presentationCapture") { capturePath=args[i+1]; title=false; StartBattle(WorldCatalog.ColossusIds[0]); }
            if(capturePath!=null && args.Contains("-captureSixParts")) PrepareSixPartCapture(args);
            if(capturePath!=null && args.Contains("-captureAllySelection")) {
                Debug.LogWarning("Formal roster has no selected-allies healing skill; legacy capture option ignored.");
            }
            if(capturePath!=null && args.Contains("-captureCasting")) {
                while(encounter.AvailableHero!=3 && !encounter.Ended) encounter.Pass();
                encounter.Act(3,1,"body"); SelectNextHero();
            }
            if(capturePath!=null && args.Contains("-captureCasterCommand")) {
                while(encounter.AvailableHero!=3 && !encounter.Ended) encounter.Pass();
                SelectNextHero();
            }
            if(capturePath!=null) { Application.runInBackground=true;encounter.DrainPresentationEvents(); }
            if(capturePath!=null && args.Contains("-captureGrowth")) {
                heroineRosterOpen=false;
                encounter=null;book.ChangeBookmark(BookBookmark.Heroines);
                int portraitIndex=Array.IndexOf(args,"-captureGrowthHero");
                if(portraitIndex>=0 && portraitIndex+1<args.Length){
                    string requestedHero=args[portraitIndex+1];
                    if(!combatDefinitions.HeroineIds.Contains(requestedHero))throw new ArgumentException("Unknown growth capture heroine");
                    if(book.SubjectId!=requestedHero && !book.RequestSubject(BookBookmark.Heroines,requestedHero))throw new InvalidOperationException("Growth capture navigation failed");
                    book.CompleteTransition();
                }
                if(args.Contains("-captureGrowthNavigation")) ValidateGrowthScreenNavigation();
                if(args.Contains("-captureGrowthLevel")) GrowthSelect(GrowthScreen.Level,formalProgression.Snapshot.heroines[0]);
                if(args.Contains("-captureGrowthAwakening")) GrowthSelect(GrowthScreen.Awakening,formalProgression.Snapshot.heroines[0]);
                if(args.Contains("-captureGrowthDuplicate")) GrowthSelect(GrowthScreen.Duplicate,formalProgression.Snapshot.heroines[0]);
                if(args.Contains("-captureGrowthConfirm")) {growthScreen=GrowthScreen.Level;GrowthConfirm(GrowthOperation.Level,combatDefinitions.FormationIds[0],formalProgression.Snapshot,11);}
            }
            if(capturePath!=null && args.Contains("-captureKinder")) PrepareKinderCapture(args);
            if(capturePath!=null && args.Contains("-captureVictory")) PrepareVictoryCapture(args);
            if(capturePath!=null && args.Contains("-captureCollection")) PrepareCollectionCapture(args);
            if(capturePath!=null && args.Contains("-capturePlan5Acceptance")) PreparePlan5Acceptance(args);
            if(capturePath!=null && args.Contains("-captureRecovery")) PrepareRecoveryCapture(args);
            if(capturePath!=null && args.Contains("-captureHeroineSanctuary"))PrepareHeroineSanctuaryCapture(args);
            if(capturePath!=null && args.Contains("-capturePlan9Title")) {
                encounter=null;title=true;
                int panel=Array.IndexOf(args,"-plan9TitlePanel");
                if(panel>=0 && panel+1<args.Length)titlePanel=args[panel+1];
                if(args.Contains("-validatePlan9SettingsCancel"))ValidateTitleSettingsCancel();
                if(args.Contains("-capturePlan9StartupError"))combatDefinitionError="保存形式を確認できません。対応するゲーム版と保存ファイルをご確認ください。（診断用表示）";
            }
            if(capturePath!=null && args.Contains("-captureEngagement")) PrepareEngagementCapture(args);
            if(capturePath!=null && args.Contains("-captureBook"))PrepareBookCapture(args);
            if(capturePath!=null && args.Contains("-capturePlan6Home"))PreparePlan6Acceptance(args);
            if(capturePath!=null && args.Contains("-capturePlan8Story"))PreparePlan8StoryCapture(args);
            if(capturePath!=null && args.Contains("-capturePlan9Story"))PreparePlan9StoryCapture(args);
            if(capturePath!=null && args.Contains("-capturePlan7Sample"))PrepareArtSample(args);
            if(capturePath!=null && (args.Contains("-measurePlan7") || args.Contains("-validatePlan7Assets")) && !artSample)ValidateArtSampleResources();
            if(capturePath!=null && args.Contains("-plan8Performance"))PreparePlan8Performance(args);
            if(capturePath!=null && args.Contains("-validatePlan7Playback"))PreparePlan7Playback();
            if(capturePath!=null && args.Contains("-capture2DActor0")) {
                while(encounter.AvailableHero!=0 && !encounter.Ended) encounter.Pass();
                encounter.DrainPresentationEvents(); SelectNextHero();
            }
            int battleMenuIndex=Array.IndexOf(args,"-captureBattleMenu");
            PreparePlan9ColossusCapture(args);
            if(capturePath!=null && battleMenuIndex>=0 && battleMenuIndex+1<args.Length)PrepareBattleMenuCapture(args[battleMenuIndex+1]);
            PrepareBattleJobCapture(args);
            if(capturePath!=null && args.Contains("-captureHealingPlayback")) {
                while(encounter.AvailableHero!=4 && !encounter.Ended) encounter.Pass();
                encounter.DrainPresentationEvents();
                foreach(var h in encounter.State.Heroes) h.TakeDamage(20);
                encounter.State.Heroes[4].GainResource(3);
                encounter.Act(4,1,"body");playback.Enqueue(encounter.DrainPresentationEvents());paused=true;
            }
            if(capturePath!=null && args.Contains("-capturePlayback")) {
                while(encounter.AvailableHero!=4) encounter.Pass();
                encounter.DrainPresentationEvents(); encounter.Act(4,1,"body");
                playback.Enqueue(encounter.DrainPresentationEvents());
                paused=true;
            }
            if(slayerReview) {
                modelViewer=true; encounter=null;
                portraitFace=!args.Contains("-captureSlayerFull");
                portraitYaw=args.Contains("-captureSlayerProfile")?90:args.Contains("-captureSlayerFront")?0:-20;
            }
            PreparePlan10RCapture(args);
            PreparePlan10AnnihilatorCapture(args);
            PreparePlan10ShellCapture(args);
            PreparePlan10OriflammeCapture(args);
            PreparePlan10NighthawkCapture(args);
        }
        private static Texture2D Texture(Color color) { var t=new Texture2D(1,1); t.SetPixel(0,0,color); t.Apply(); return t; }
        private void Styles()
        {
            if(text!=null) return;
            text=new GUIStyle(GUI.skin.label) { font=font, fontSize=ArtSampleSettings.LargeText?24:21, wordWrap=true }; text.normal.textColor=new Color(.18f,.22f,.21f);
            heading=new GUIStyle(text) { fontSize=31, fontStyle=FontStyle.Bold };
            small=new GUIStyle(text) { fontSize=ArtSampleSettings.LargeText?20:17 };
            button=new GUIStyle(GUI.skin.button) { font=font, fontSize=19, wordWrap=true, padding=new RectOffset(10,10,6,6) };
            button.normal.background=teal; button.normal.textColor=new Color(.97f,.94f,.83f);
            button.hover.background=teal; button.hover.textColor=Color.white; button.active.background=dark; button.active.textColor=Color.white;
            skillButton=new GUIStyle(button) { fontSize=16,padding=new RectOffset(6,6,4,4) };
        }
        private void Update()
        {
            UpdateTrialTelemetry();
            UpdateFormalEntrance();
            UpdateBookTransition();
            UpdateAdv();
            UpdateEngagement();
            if(!plan7FocusStarted && capturePath!=null && Environment.GetCommandLineArgs().Contains("-validatePlan7Focus") && Time.realtimeSinceStartup>1 && Application.isFocused){plan7FocusStarted=true;StartCoroutine(ValidatePlan7Focus());}
            if(capturePath!=null && ProductionStoryActive)TryStartProductionAudioCapture();
            if(Input.GetKeyDown(KeyCode.Escape) && !plan7ActiveCombat) {
                if(artSample){artSample=false;artBgm?.Stop();artSe?.Stop();}
                else if(adv!=null){if(advBacklog || advHelp){advBacklog=false;advHelp=false;}else CloseAdv();}
                else if(homeRequest!=null){if(!formalCampaign.HasPending){homeRequest=null;homeOperation=null;}}
                else if(panzerSetupOpen)panzerSetupOpen=false;
                else if(placing){placing=false;selectedFurniture=null;}
                else if(CloseGardenMenuLayer()){}
                else if(recoveryActive)recoveryConfirm=false;
                else if(collectionOpen)CollectionBack();
                else if(engagementOpen)EngagementBack();
                else if(kinderGarden && formalProgression!=null) KinderBack();
                else if(!title && encounter==null && book.Bookmark==BookBookmark.Heroines && formalProgression!=null) GrowthBack();
                else if(modelViewer) modelViewer=false;
                else if(selectingAlly) { selectingAlly=false; selectedAllies.Clear(); }
                else if(storyText!=null) CloseStory();
                else if(help) help=false;
                else if(kinderGarden) kinderGarden=false;
                else if(retreat) { retreat=false; paused=false; }
                else if(CloseBattleMenuLayer()){}
                else if(title && titlePanel!=null)CloseTitlePanel();
                else if(title && FormalEntranceVisible)formalEntranceComplete=true;
                else if(title)OpenTitlePanel("exit");
                else if(encounter!=null && result==null) paused=!paused;
                else if(result==null) help=true;
            }
            bool battleView=encounter!=null;
            bool formalHeroView=!title && encounter==null && book.Bookmark==BookBookmark.Heroines && formalProgression!=null;
            viewCamera.cullingMask=recoveryActive || engagementOpen || battleView || formalHeroView?0:~0;
            viewCamera.orthographic=modelViewer;
            viewCamera.backgroundColor=modelViewer?new Color(.42f,.44f,.48f):new Color(.045f,.10f,.11f);
            viewCamera.rect=battleView?new Rect(0f,.22f,.72f,.60f):new Rect(.64f,.27f,.36f,.51f);
            viewCamera.aspect=Screen.width*viewCamera.rect.width/(Screen.height*viewCamera.rect.height);
            viewCamera.fieldOfView=battleView?35f:60f;
            bool gardenView=!title && encounter==null && book.Bookmark==BookBookmark.Gardens;
            viewCamera.transform.position=gardenView?new Vector3(-4,5,-8):battleView?new Vector3(-.5f,4.5f,-10):new Vector3(-1,7,-15);
            viewCamera.transform.LookAt(gardenView?new Vector3(-3,1,3):new Vector3(-.5f,battleView?2.8f:1.8f,1.2f));
            if(modelViewer) {
                if(Input.GetMouseButton(0) && Input.mousePosition.y>Screen.height*(95f/900) && Input.mousePosition.y<Screen.height*(1-125f/900)) portraitYaw+=Input.GetAxis("Mouse X")*4;
                portraitZoom=Mathf.Clamp(portraitZoom-Input.mouseScrollDelta.y*.07f,.65f,1.5f);
                viewCamera.rect=new Rect(0,0,1,1); viewCamera.aspect=Screen.width/(float)Screen.height;
                var focus=new Vector3(-4.8f,portraitFace?1.49f:1.01f,-1.5f);
                float angle=portraitYaw*Mathf.Deg2Rad;
                viewCamera.transform.position=focus+new Vector3(Mathf.Cos(angle)*3,.025f,Mathf.Sin(angle)*3);
                viewCamera.transform.LookAt(focus); viewCamera.orthographicSize=(portraitFace?.19f:.73f)*portraitZoom;
            }
            if(stage==null) stage=FindFirstObjectByType<VerticalSliceBlockout>();
            UpdatePlayback();
            UpdatePlan7ActiveCombat();
            UpdateArtAudio();
            if(stage!=null) stage.Synchronize(gardenView,campaign.Gardens.UnlockedGardenIds.Count>0,campaign.Playable,encounter,target,paused || retreat || help || result!=null,playback.Current);
            if(stage!=null) stage.SetPortraitView(modelViewer);
            bool bookPreviewVisible=title || modelViewer || book.HasSubject && (book.Bookmark==BookBookmark.Colossi && book.SubjectId==WorldCatalog.ColossusIds[0] || book.Bookmark==BookBookmark.Gardens && book.SubjectId=="garden.grassland-forest" && campaign.Gardens.UnlockedGardenIds.Contains(book.SubjectId));
            if(!bookPreviewVisible)viewCamera.cullingMask=0;
            if(!title && encounter==null && book.Bookmark==BookBookmark.Gardens)viewCamera.cullingMask=0;
            if(stage!=null) stage.gameObject.SetActive(!recoveryActive && !battleView && !formalHeroView && bookPreviewVisible);
            // Wait for the player splash to finish before capturing. Fast machines
            // can otherwise reach 150 frames and exit before any game UI is visible.
            if(capturePath!=null && !Environment.GetCommandLineArgs().Contains("-plan9ManualSmoke") && Time.realtimeSinceStartup>=8) {
                captureFrame++;
                if(captureFrame==85 && Environment.GetCommandLineArgs().Contains("-bookTransition"))RequestBookFlip();
                int captureAt=measureArt?(plan7ActiveCombat?1800:600):90;
                bool ready=plan7FullCombat?plan7FullCombatComplete:Environment.GetCommandLineArgs().Contains("-validatePlan7Focus")?captureFrame>=captureAt && plan7FocusComplete:captureFrame==captureAt;
                if(ProductionAudioCapture)ready=captureFrame>=captureAt && productionAudioComplete;
                if(capturedAtFrame<0 && ready){capturedAtFrame=captureFrame;ReportPlan7ActiveCombat();ReportArtPerformance();ScreenCapture.CaptureScreenshot(capturePath,Environment.GetCommandLineArgs().Contains("-captureDoubleResolution")?2:1);}
                if(capturedAtFrame>=0 && captureFrame==capturedAtFrame+60) Application.Quit();
            }
        }
        private void UpdatePlayback()
        {
            if(encounter==null) { playback.Reset(); shownEvent=0; return; }
            bool stopped=paused || retreat || help;
            float playbackRate=plan7FullCombat?1:(ArtSampleSettings.Shortened?2:1);
            if(!stopped && playback.Busy) illustrationElapsed+=Time.unscaledDeltaTime*playbackRate;
            if(!stopped) breakNoticeRemaining=Mathf.Max(0,breakNoticeRemaining-Time.unscaledDeltaTime);
            // Show a newly queued event at least once before its duration starts ticking.
            if(playback.Current==null || playback.Current.Sequence==shownEvent) playback.Tick(Time.unscaledDeltaTime*playbackRate,stopped);
            var e=playback.Current;
            if(e!=null && e.Sequence!=shownEvent) {
                TrialObserve("battle","presentation",$"kind={e.Kind};actor={e.Actor};target={e.Target};damage={e.Damage};broken={e.PartBroken};major={e.Major};chain={e.Chain};actions={e.ChainActionCount};bossHp={e.BossHp};heroes={string.Join(",",e.HeroHp)};resources={string.Join(",",e.Resources)}",battleId+"/presentation/"+e.Sequence,e.Clock,BattleVisualCue.Duration(e.Kind,e.Major));
                illustrationElapsed=0; shownEvent=e.Sequence; if(e.Actor>=0) selectedHero=e.Actor;
                if(stage!=null) {
                    stage.ClearActionEffects();
                    stage.BeginPresentation(e);
                    switch(e.Kind) {
                        case BattlePresentationKind.Healing: stage.PlayHealing(e.Actor,e.HealingTargets); break;
                        case BattlePresentationKind.Support: stage.PlayAction(e.Actor,true,e.Target); break;
                        case BattlePresentationKind.Enemy: stage.PlayEnemyAction(e.Major); break;
                    }
                }
                if(e.PartBroken) {
                    var indices=Enumerable.Range(0,encounter.State.Parts.Count).Where(i=>e.TargetIds.Contains(encounter.State.Parts[i].Id) && e.PartHp[i]==0).ToArray();
                    if(indices.Length>0) {breakNotice=string.Join(" / ",indices.Select(i=>ColossusCombatCatalog.PartName(encounter.State.Parts[i],i)+"：部位破壊！ "+ColossusCombatCatalog.PartEffect(encounter.State.Parts[i])));breakNoticeRemaining=6f;}
                }
            }
            if(!playback.Busy && shownEvent!=0) {
                shownEvent=0; if(stage!=null) stage.ClearActionEffects(); SelectNextHero(); FinishCheck();
            }
        }
        private void QueueBattleEvents()
        {
            ResetBattleMenu();
            playback.Enqueue(encounter.DrainPresentationEvents());
            if(!playback.Busy) { SelectNextHero(); FinishCheck(); }
        }
        private void OnGUI()
        {
            double started=measureArt?MeasurementClock:0;
            try{DrawGameGui();}finally{RecordMeasuredGui(started);}
        }
        private void DrawGameGui()
        {
            if(plan7ActiveCombat && Event.current.type!=EventType.Layout && Event.current.type!=EventType.Repaint)return;
            Styles(); ImageUiSkin.ApplyControls(GUI.skin); GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1)); drawingModal=false;
            if(recoveryActive){DrawSaveRecovery();return;}
            if(plan9EnemyPreview!=null){DrawPlan9EnemyArt();return;}
            if(plan9Expression!=null){DrawPlan9CharacterArt();return;}
            if(plan9Cg!=null){DrawPlan9EventCg();return;}
            if(artSample){DrawArtSample();return;}
            if(adv!=null){DrawAdv();return;}
            if(collectionOpen){DrawCollectionExperience();return;}
            if(engagementOpen){DrawEngagement();return;}
            if(combatDefinitionError!=null) { DrawFormalStartupError();return; }
            if(modelViewer) { DrawModelViewer(); return; }
            if(!title && kinderGarden && formalProgression!=null) { DrawKinderExperience();return; }
            if(!title && encounter==null && book.Bookmark==BookBookmark.Heroines && book.HasSubject && formalProgression!=null) { DrawGrowthExperience();return; }
            if(!title && encounter!=null) {
                DrawBattle(); drawingModal=true;
                if(help) DrawHelp(); else if(retreat) DrawRetreat(); else if(result!=null) DrawResult();
                return;
            }
            if(!title && encounter==null && book.Bookmark==BookBookmark.Gardens && book.HasSubject){DrawGardenHome();return;}
            if(title) { DrawFormalTitle(); return; }
            DrawFormalBookSurface(); Panel(0,0,1600,80,dark);
            Label(32,20,950,46,"newASTER  /  巨神と誓女2",heading,Color.white);
            Panel(1024,80,576,118,dark);
            Label(1050,100,510,70,encounter==null?"記憶が、新しい世界を育てる。":"巨神獣との空中戦",heading,Color.white);
            if(ProductionStoryActive)DrawBookSubjectArt();
            else if(encounter==null && book.HasSubject && book.Bookmark==BookBookmark.Colossi && book.SubjectId==WorldCatalog.ColossusIds[0]) {
                SampleImage(new Rect(1024,198,576,459),"forest-far",true);
                SampleImage(new Rect(1050,208,510,430),"green-body");
            }
            if(!ProductionStoryActive && encounter==null && (!book.HasSubject || book.Bookmark!=BookBookmark.Colossi || book.SubjectId!=WorldCatalog.ColossusIds[0]))Label(1050,275,510,100,book.HasSubject?"この対象の絵は制作待ちです。":"表示する対象はありません。",text,Color.white);
            Panel(1024,657,576,243,dark);
            if(encounter==null) DrawBook(); else DrawBattle();
            Panel(0,812,1024,88,dark);if(encounter!=null || !book.IsTransitioning)Label(28,826,970,60,encounter==null?"しおりで分類、めくりで対象、裏返しで同じ対象の情報へ。":status,small,Color.white);
            if(encounter==null)DrawBookTransition();
            drawingModal=true;
            if(storyText!=null) DrawStory(); else if(help) DrawHelp(); else if(kinderGarden) DrawKinderGarden(); else if(retreat) DrawRetreat(); else if(result!=null) DrawResult();
        }
        private void DrawBook()
        {
            string[] tabs={"巨神獣","誓女・育成","庭","物語"};
            for(int i=0;i<4;i++) if(Btn(28+i*242,100,230,46,(int)book.Bookmark==i?"◆ "+tabs[i]:tabs[i],BookInputAllowed && !book.IsTransitioning))RequestBookBookmark((BookBookmark)i);
            if(Btn(28,160,180,42,"‹ 前のページ",BookInputAllowed && book.CanTurnPrevious))RequestBookTurn(-1);
            if(Btn(218,160,180,42,"次のページ ›",BookInputAllowed && book.CanTurnNext))RequestBookTurn(1);
            if(Btn(408,160,180,42,book.Face==BookFace.Overview?"ページを裏返す":"表に戻す",BookInputAllowed && book.CanFlip))RequestBookFlip();
            if(Btn(600,160,120,42,"保存",BookInputAllowed)) Save(); if(Btn(730,160,170,42,"召喚・交換",BookInputAllowed)) kinderGarden=true; if(Btn(910,160,70,42,"？",BookInputAllowed)) help=true;
            Label(30,220,950,34,$"素材 {AvailableCollectionMaterials}  /  世界復元 {campaign.Progress.TerraformingExperience}  /  所持する詩 {campaign.Progress.CollectedPoemIds.Count}",small);
            if(Btn(1050,814,510,42,"本を閉じて表紙へ",BookInputAllowed)){book.Close();title=true;}
            if(!book.HasSubject){Label(32,320,920,110,"この分類にはまだ対象がありません。解放された対象はここで確認できます。",text);return;}
            bool previousBookEnabled=GUI.enabled;GUI.enabled=previousBookEnabled && (BookInputAllowed || placing || homeRequest!=null) && !book.IsTransitioning;
            switch(book.Bookmark) {
                case BookBookmark.Colossi: DrawColossus(); break;
                case BookBookmark.Heroines: DrawHeroine(); break;
                case BookBookmark.Gardens: DrawFormalGarden(); break;
                case BookBookmark.Stories: DrawStories(); break;
            }
            GUI.enabled=previousBookEnabled;
            DrawHomeConfirmation();
            if(homeTrial){if(Btn(1050,740,510,48,"検証用の別セーブ ／ 通常へ戻る",BookInputAllowed))ExitHomeTrial();}
            else if(!ProductionStoryActive && Btn(1050,740,510,48,"制作メニュー ／ 機能検証用セーブ",BookInputAllowed))EnterHomeTrial();
        }
        private void DrawColossus()
        {
            SyncBookSelectedLevel();
            var c=WorldCatalog.Colossi.Single(x=>x.Id==book.SubjectId); bool unlocked=campaign.ColossusUnlocks.IsUnlocked(c.Id);
            Label(32,278,930,65,$"{book.SubjectIndex+1:00}  {(unlocked?c.DisplayName:"？？？")}",heading);
            if(!unlocked) { Label(32,365,920,120,"前の巨神獣を初めて討伐すると、このページが開きます。\n最後の巨神獣には、14体すべての初回討伐が必要です。",text); return; }
            if(book.Face==BookFace.Details) {
                Label(32,365,920,100,"初回討伐で世界へ定着する環境："+string.Join("・",c.EnvironmentTags),text);
                if(ColossusCombatCatalog.CanSummon(c.Id)){
                    var definition=ColossusCombatCatalog.Get(c.Id);
                    string parts=string.Join(" / ",definition.parts.Select((p,i)=>ColossusCombatCatalog.PartName(new BattlePart(p.id,p.baseHp,p.breakEffect,role:p.role),i)));
                    Label(32,490,920,195,parts+"\n部位を破壊してから本体を攻めると安全に戦えます。\n大技："+definition.majorAction+" / Lv45以上："+definition.ultimateAction,text);
                }else Label(32,490,920,195,"この巨神獣の戦闘は制作中です。",text);
            } else {
                Label(32,363,925,110,"巨神獣の体に残った呪歌は、失われた世界の記憶。\n討伐して環境を取り戻し、詩を集めると物語の章が開きます。",text);
                Label(32,485,900,45,$"挑戦 Lv.{selectedLevel}  /  選択可能 1〜{campaign.Playable.HighestLevel}",heading);
                if(Btn(32,548,90,42,"− 1")) selectedLevel=Math.Max(1,selectedLevel-1);
                if(Btn(132,548,90,42,"＋ 1")) selectedLevel=Math.Min(campaign.Playable.HighestLevel,selectedLevel+1);
                if(Btn(232,548,90,42,"− 5")) selectedLevel=Math.Max(1,selectedLevel-5);
                if(Btn(332,548,90,42,"＋ 5")) selectedLevel=Math.Min(campaign.Playable.HighestLevel,selectedLevel+5);
                if(Btn(432,548,180,42,"最高レベル")) selectedLevel=campaign.Playable.HighestLevel;
                Label(32,612,925,58,"Lv45以上で極大技。勝利すると選択可能なLvが5上がります。",small);
                if(Btn(32,692,910,70,ColossusCombatCatalog.CanSummon(c.Id)?"5人の誓女と出撃する":"戦闘定義は未制作 ／ 出撃できません",BookInputAllowed && !book.IsTransitioning && ColossusCombatCatalog.CanSummon(c.Id))) StartBattle(c.Id);
            }
        }
        private void DrawHeroine()
        {
            if(formalProgression!=null) { DrawFormalGrowth();return; }
            int h=book.SubjectIndex; var p=campaign.Playable;
            Label(32,275,930,55,$"{Names[h]}  /  {Jobs[h]}  /  Lv.{p.Levels[h]}/{p.LevelCap(h)}  覚醒{p.Awakenings[h]}",heading);
            Label(32,338,930,45,$"好感度 {p.Affections[h]}/100  ・  育成や好感度でチェイン率は変化しません。",small);
            if(book.Face==BookFace.Details) {
                var previewBattle=new PlayableBattle(1,p);
                var recovery=previewBattle.HealingSkill(h,1);
                Label(32,398,900,125,$"スキル：通常攻撃 / {(recovery==null?"資源3の強撃":recovery.Name+"（味方1人・資源3）")} / {PlayableBattle.SupportName(h)}\n支援：{previewBattle.SupportDescription(h)}（資源3・チェイン終了）\n重複強化 {p.TraitRanks[h]}/{PlayableProgress.MaximumTraitRank}：HP ＋{p.TraitRanks[h]*PlayableProgress.DuplicateHitPointGain} / 攻撃 ＋{p.TraitRanks[h]*PlayableProgress.DuplicateAttackGain}",text);
                Label(32,545,925,150,"毎回5人で出撃します。速度と使用スキルで行動順が変わります。\n長い待機と、発動までの詠唱は別の時間です。\n花の枝を育てると支援スキルが強化されます。",text);
                if(h==0 && stage!=null) {
                    if(Btn(32,700,905,40,"スレイヤーを全画面で見る")) { modelViewer=true; portraitFace=true; portraitYaw=-20; portraitZoom=1; }
                    if(Btn(32,746,445,44,"ローズ衣装")) stage.SetSlayerOutfit("rose");
                    if(Btn(492,746,445,44,"訓練衣装")) stage.SetSlayerOutfit("training");
                }
                return;
            }
            int[] steps={1,5,10};
            for(int i=0;i<steps.Length;i++) {
                int step=steps[i], gain=Math.Min(step,p.LevelCap(h)-p.Levels[h]), cost=p.TrainingCost(h,step);
                string caption=gain==0?"Lv上限に到達":$"Lv ＋{gain}  素材 {cost}"+(campaign.Progress.Materials<cost?"（不足）":"");
                if(Btn(32+i*308,398,294,48,caption,gain>0 && campaign.Progress.Materials>=cost)) Mutate(p.Train(campaign.Progress,h,step),"誓女が成長しました。");
            }
            int awakenCost=p.AwakeningCost(h);
            bool atCap=p.Levels[h]==p.LevelCap(h);
            string awakenLabel=awakenCost==0?"覚醒2達成 / 最終Lv上限120"
                : $"覚醒{p.Awakenings[h]+1}  素材 {awakenCost}  /  "+(!atCap?$"Lv.{p.LevelCap(h)}到達が必要":campaign.Progress.Materials<awakenCost?"素材が不足":$"Lv上限を{(p.Awakenings[h]==0?80:120)}へ開放");
            if(Btn(32,455,910,42,awakenLabel,awakenCost>0 && atCap && campaign.Progress.Materials>=awakenCost)) Mutate(p.Awaken(campaign.Progress,h),"覚醒し、Lv上限が開放されました。");
            Label(32,501,900,25,"武器の樹  /  根から3つの枝へ",small);
            DrawWeaponTree(h);
            string[] branches={"剣の枝：攻撃","盾の枝：HP・防御","花の枝：支援"};
            for(int b=0;b<3;b++) {
                int rank=p.Branches[h*3+b]; float x=32+b*308;
                Label(x,525,294,55,branches[b],text);
                if(Btn(x,650,294,55,$"育てる  素材 {3+rank*3}",rank<3)) Mutate(p.Grow(campaign.Progress,h,b),"武器の枝に花が咲きました。");
            }
            Label(32,707,900,28,"初期装備（根） → 開放した枝に花が咲きます。",small);
            string duplicateLabel=p.TraitRanks[h]>=PlayableProgress.MaximumTraitRank
                ? $"重複を汎用素材に変換 / 残り{p.Duplicates[h]}"
                : $"重複強化 {p.TraitRanks[h]}/{PlayableProgress.MaximumTraitRank} / 重複{p.Duplicates[h]}";
            if(Btn(32,743,448,45,duplicateLabel,p.Duplicates[h]>0)) Mutate(p.StrengthenDuplicate(h),"重複した誓女を強化・変換しました。");
            if(Btn(492,743,448,45,$"汎用強化素材で強化 / 所持{p.OverflowEnhancementMaterials}",p.OverflowEnhancementMaterials>0 && p.TraitRanks[h]<PlayableProgress.MaximumTraitRank)) Mutate(p.UseOverflowEnhancement(h),"汎用素材で誓女を強化しました。");
        }
        private void DrawModelViewer()
        {
            Label(32,24,700,45,"スレイヤー  /  人物鑑賞",heading,Color.white);
            if(Btn(1390,22,180,45,"本へ戻る")) modelViewer=false;
            string[] expressions={"Neutral","Smile","Joy","Sad","Angry","Surprise","Talk"};
            string[] labels={"通常","微笑み","喜び","悲しみ","怒り","驚き","口の動き"};
            for(int i=0;i<expressions.Length;i++) if(Btn(32+i*117,82,108,36,labels[i]) && stage!=null) stage.SetSlayerExpression(expressions[i]);
            Panel(20,815,1560,66,dark);
            if(Btn(32,827,145,42,"全身")) { portraitFace=false; portraitZoom=1; }
            if(Btn(187,827,145,42,"顔")) { portraitFace=true; portraitZoom=1; }
            if(Btn(342,827,145,42,"正面")) portraitYaw=0;
            if(Btn(497,827,145,42,"斜め")) portraitYaw=-25;
            if(Btn(652,827,145,42,"横顔")) portraitYaw=90;
            if(Btn(807,827,210,42,"衣装を替える") && stage!=null) stage.SetSlayerOutfit(stage.SlayerOutfitId=="rose"?"training":"rose");
            Label(1040,835,500,32,"ドラッグで回転・ホイールで拡大",small,Color.white);
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
            bool unlocked=campaign.Gardens.UnlockedGardenIds.Contains(book.SubjectId);
            if(book.SubjectId!="garden.grassland-forest"){
                var requirement=GardenCatalog.Requirements.Single(r=>r.GardenId==book.SubjectId);
                Label(32,275,930,65,string.Join("と",requirement.RequiredEnvironmentTags)+"の庭",heading);
                Label(32,365,920,160,"解放条件："+string.Join("・",requirement.RequiredEnvironmentTags)+"\n"+(unlocked?"区画の条件は達成済みです。配置画面は制作待ちです。":"この環境を世界へ取り戻すと、区画が解放されます。"),text);return;
            }
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
            var owner=CollectionData().owners.Single(o=>o.id==book.SubjectId);
            Label(32,270,920,50,CollectionOwnerName(owner),heading);
            Label(32,333,920,48,book.Face==BookFace.Overview?(ProductionStoryActive?"詩をそろえると、物語の章が開きます。":"物語の章 ／ 正式本文は未制作です。"):"本文読了と詩の進捗 ／ 好感度とは別に記録します。",small);
            var world=formalCampaign.Snapshot.world;
            for(int i=0;i<owner.chapterIds.Length;i++){
                var chapter=CollectionData().chapters.Single(c=>c.id==owner.chapterIds[i]);string state=world.readStoryIds.Contains(chapter.id)?"読了":world.unlockedStoryIds.Contains(chapter.id)?"解放・未読":"未解放";
                bool visible=owner.kind=="heroine" || campaign.ColossusUnlocks.IsUnlocked(owner.id);
                string name=ProductionStoryActive?(visible?ProductionStoryTitle(chapter.id):"未解放の章"):"第"+(i+1)+"章";
                Label(32,410+i*66,HomeOperationsAllowed?520:920,50,book.Face==BookFace.Overview?$"{name}　{state}":$"第{i+1}章　詩 {chapter.poemIds.Count(world.poemIds.Contains)}/{chapter.poemIds.Length}　／　{state}",text);
                if(HomeOperationsAllowed){if(Btn(560,405+i*66,210,50,ProductionStoryActive?"読む／再開":"検証ADV",world.unlockedStoryIds.Contains(chapter.id)))BeginAdv(chapter.id,false);if(Btn(785,405+i*66,170,50,"回想",world.readStoryIds.Contains(chapter.id)))BeginAdv(chapter.id,true);}
            }
            if(Btn(32,635,920,58,"この対象の詩と章の一覧",BookInputAllowed && !book.IsTransitioning))OpenCollectionForBook();
            if(Btn(32,713,920,58,"オーパーツと素材を確認",BookInputAllowed && !book.IsTransitioning)){collectionOpen=true;collectionTab=1;}
        }
        private void StartBattle(string colossus,int? diagnosticSeed=null)
        {
            if(diagnosticSeed.HasValue && !formalDiagnostic)throw new InvalidOperationException("Seeded battle requires diagnostic isolation.");
            if(!ColossusCombatCatalog.CanSummon(colossus))throw new ArgumentException("巨神獣の戦闘定義は未制作です。");
            var id=Guid.NewGuid();
            if(stage!=null)stage.SetFormation(combatDefinitions.FormationIds,CurrentFormation());
            activeColossus=colossus; battleId=id.ToString("N"); encounter=new PlayableBattle(selectedLevel,campaign.Playable,diagnosticSeed??BitConverter.ToInt32(id.ToByteArray(),0),combatDefinitions:combatDefinitions.WithFormation(CurrentFormation()),formalGrowth:formalProgression.Snapshot,colossusDefinition:ActiveColossusDefinition(colossus),collectionGrowth:formalCampaign.Snapshot.collection,homeProgress:formalCampaign.Snapshot.home,homeCatalog:HomeData(),relicCatalog:CollectionData(),useJobRulesV2:true,protectedSlot:protectedFormationSlot,panzerLoadout:SavedPanzerLoadout());
            illustrationView=new BattleIllustrationView(ColossusCombatCatalog.IllustrationResource(colossus));
            Debug.Log($"BATTLE_START id={battleId} seed={encounter.Seed} level={selectedLevel}");
            TrialObserve("battle","start","colossus="+colossus+";level="+selectedLevel);
            StartCollection();
            target="body"; paused=false; result=null; status="対象を選び、威力とチェイン率を確認して行動してください。";
            selectedHero=encounter.AvailableHero;
            playback.Reset(); shownEvent=0; if(stage!=null) stage.ClearActionEffects();
            selectingAlly=false; selectedAllies.Clear(); breakNotice=""; breakNoticeRemaining=0;
            ResetBattleMenu();
        }
        private void PrepareSixPartCapture(string[] args)
        {
            if(!formalDiagnostic)throw new InvalidOperationException("Six-part fixture requires diagnostic save isolation.");
            var def=ColossusCombatCatalog.Get(activeColossus);
            def.parts=def.parts.Where(p=>p.role!="armor").Concat(new[]{
                new ColossusPartCombatDef{id="fixture.aux.left",role="auxiliary",breakEffect="",baseHp=312},
                new ColossusPartCombatDef{id="fixture.aux.right",role="auxiliary",breakEffect="",baseHp=312}
            }).Concat(def.parts.Where(p=>p.role=="armor")).ToArray();
            encounter=new PlayableBattle(1,campaign.Playable,8,combatDefinitions:combatDefinitions,formalGrowth:formalProgression.Snapshot,colossusDefinition:def);
            StartCollection();selectedHero=encounter.AvailableHero;target=def.parts[4].id;
            if(args.Contains("-captureSixPartsBroken")) {
                encounter.Act(selectedHero,0,target);
                var events=encounter.DrainPresentationEvents();
                if(!events.Any(e=>e.TargetIds.Contains(target) && e.PartHp.Count==6))throw new InvalidOperationException("Six-part target event missing.");
                encounter.State.BreakPart(target,int.MaxValue);
                breakNotice=ColossusCombatCatalog.PartName(encounter.State.Parts[4],4)+"：部位破壊！ "+ColossusCombatCatalog.PartEffect(encounter.State.Parts[4]);breakNoticeRemaining=60;
                target=def.parts[5].id;SelectNextHero();
                Debug.Log("SIX_PART_TARGET_PASS fifth target hit, broken part disabled, sixth armor selected");
            }
        }
        private void DrawAllySelection()
        {
            var Names=Enumerable.Range(0,5).Select(encounter.HeroineName).ToArray();
            var d=encounter.HealingSkill(healingActor,healingSlot);
            var affected=encounter.HealingTargets(healingActor,healingSlot,selectedAllies.OrderBy(i=>i));
            bool selectable=d.TargetRule==HealingTargetRule.SelectedAllies;
            Modal(); Label(340,165,900,50,d.Name+"："+(selectable?$"味方{d.TargetCount}人を選択":"対象を確認"),heading);
            Label(340,218,900,28,selectable?$"選択 {selectedAllies.Count}/{d.TargetCount}人・同じ人は重複不可":encounter.HealingDescription(healingActor,healingSlot),small);
            for(int i=0;i<5;i++) {
                var h=encounter.State.Heroes[i]; int gain=encounter.PreviewHealing(healingActor,i,healingSlot);
                bool supportTarget=encounter.IsAllyBuff(healingActor,healingSlot),canChoose=encounter.CanChooseAlly(healingActor,healingSlot,i);
                string info=gain>0?$"HP {h.HitPoints} → {h.HitPoints+gain}/{h.MaxHitPoints}（＋{gain}）":$"HP {h.HitPoints}/{h.MaxHitPoints}　"+(!h.IsAlive?"戦闘不能（蘇生不可）":d.TargetRule==HealingTargetRule.Self && i!=healingActor?"対象外":h.HitPoints==h.MaxHitPoints?"HP満タン・回復0":"使用不可");
                string caption=(affected.Contains(i)?"◆ ":"")+Names[i]+"　"+info;
                if(supportTarget)caption=(affected.Contains(i)?"◆ ":"")+Names[i]+" ／ "+(h.IsAlive?$"HP {h.HitPoints}/{h.MaxHitPoints}・支援を受ける":"戦闘不能・対象外");
                else if(canChoose && gain==0)caption=(affected.Contains(i)?"◆ ":"")+Names[i]+" ／ HP満タン・状態異常を解除";
                if(canChoose && selectable) { if(Btn(340,250+i*68,900,58,caption,!paused && (selectedAllies.Contains(i)||selectedAllies.Count<d.TargetCount))) { if(!selectedAllies.Remove(i)) selectedAllies.Add(i); } }
                else if(canChoose) { Panel(340,250+i*68,900,58,teal); Label(358,264+i*68,864,36,caption,small,Color.white); }
                else { Panel(340,250+i*68,900,58,dark); Label(358,264+i*68,864,36,caption,small,Color.white); }
            }
            if(encounter.IsAllyBuff(healingActor,healingSlot))Label(340,601,900,34,encounter.SelfBuffDescription(healingActor,healingSlot),new GUIStyle(small){fontSize=16,wordWrap=false});
            if(Btn(340,650,420,62,"取消（消費なし）")) { selectingAlly=false; selectedAllies.Clear(); }
            if(Btn(820,650,420,62,encounter.IsAllyBuff(healingActor,healingSlot)?"支援を実行":"回復を実行",!paused && encounter.CanHealTargets(healingActor,healingSlot,selectedAllies))) { selectingAlly=false; Act(healingActor,healingSlot,selectedAllies.OrderBy(i=>i).ToArray()); selectedAllies.Clear(); }
        }
        private void SelectNextHero()
        {
            if(encounter.UsesTimeline) { if(encounter.AvailableHero>=0) selectedHero=encounter.AvailableHero; return; }
            for(int step=1;step<=5;step++) { int next=(selectedHero+step)%5; if(encounter.State.Heroes[next].IsAlive && !encounter.Acted[next]) { selectedHero=next; return; } }
        }
        private void Act(int hero,int skill,int[] allies=null)
        {
            if(playback.Busy || paused || retreat || help) return;
            bool accepted=encounter.ActWithAllies(hero,skill,target,allies);
            TrialObserve("battle",accepted?"input-accepted":"input-rejected",$"hero={hero};skill={skill};target={target}",tick:encounter.Clock);
            if(accepted) {
                if(target!="body" && encounter.State.Parts.First(p=>p.Id==target).IsBroken) target="body";
                QueueBattleEvents();
            }
        }
        private void FinishCheck()
        {
            if(plan7ActiveCombat)return;
            if(playback.Busy || !encounter.Ended || result!=null) return;
            if(!encounter.State.IsVictory) { PrepareFormalBattleEnd(BattleEndReason.Defeat); return; }
            PrepareFormalVictory();
        }
        private void DrawResult()
        {
            if(formalBattleEndRequest!=null) { DrawVictorySavePending();return; }
            DrawFormalVictoryComplete();
        }
        private void DrawRetreat()
        {
            Modal(); Label(340,245,880,90,"撤退しますか？",heading); Label(340,365,870,125,"聞いた詩と対応する人物の詩は持ち帰ります。素材・石・世界復元は勝利時だけ取得できます。",text);
            if(Btn(340,605,420,64,"戦闘へ戻る")) { retreat=false; paused=false; }
            if(Btn(780,605,420,64,"撤退する")) { playback.Reset(); shownEvent=0; if(stage!=null) stage.ClearActionEffects(); retreat=false; PrepareFormalBattleEnd(BattleEndReason.Retreat); }
        }
        private void DrawStory()
        {
            Modal(); Label(340,182,880,65,storyId==null?"庭でのひととき":$"第{storyChapter+1}章  {Chapters[storyChapter]}",heading);
            var pages=storyText.Split(new[] { "\n\n" },StringSplitOptions.RemoveEmptyEntries);
            storyPage=Math.Min(storyPage,pages.Length-1);
            Label(340,270,870,300,pages[storyPage],text);
            Label(690,580,170,35,$"{storyPage+1} / {pages.Length}",small);
            if(Btn(340,575,280,48,"‹ 前のページ",storyPage>0)) storyPage--;
            if(Btn(930,575,280,48,"次のページ ›",storyPage<pages.Length-1)) storyPage++;
            if(Btn(340,656,420,62,"本を閉じる")) CloseStory();
            if(Btn(790,656,420,62,"読了して戻る",storyPage==pages.Length-1)) CloseStory(true);
        }
        private void CloseStory(bool completed=false) { if(completed && storyId!=null) campaign.Progress.MarkStoryRead(storyId); storyText=null; storyId=null; storyPage=0; Save(); }
        private void DrawHelp()
        {
            Modal(); Label(340,185,880,64,"遊び方",heading);
            Label(340,275,880,355,"1. 巨神獣のページから5人で出撃。\n2. 上の部位ボタンで対象を選ぶ。\n3. 下の誓女カードを選び、右の3スキルで行動。\n4. 接続成功で次の攻撃が強化。支援で終了。\n5. 部位破壊で敵を弱め、報酬で育成・家具を作る。\n6. 詩を集めたら物語のしおりで読む。\n\nめくる＝対象変更。裏返す＝同じ対象の詳細。\nEsc＝一時停止。進行は操作・討伐後に自動保存。",text);
            if(Btn(340,656,890,62,"閉じる")) help=false;
        }
        private void DrawKinderGarden()
        {
            if(formalProgression!=null) {
                DrawKinderExperience();
                return;
            }
            Modal(); var p=campaign.Playable;
            Label(340,182,880,65,"キンダーガーデン",heading);
            Label(340,250,880,65,"★6 3%（5人各0.6%） / 素材97%（4種各24.25%）\n素材の獲得量：4・6・8・10。100回ごとに好きな誓女を交換。",small);
            Label(340,320,880,48,$"石 {p.KinderStones} / 素材 {campaign.Progress.Materials} / 累計 {p.KinderDrawCount}回 / 交換 {p.AvailableKinderExchanges}回",text);
            Label(340,375,880,90,kinderResult,text);
            if(Btn(340,475,420,55,"石1個で迎える",p.KinderStones>0)) {
                // Random.value includes 1; integer sampling stays strictly below 1.
                decimal heroineRoll=UnityEngine.Random.Range(0,1000000)/1000000m;
                decimal targetRoll=UnityEngine.Random.Range(0,1000000)/1000000m;
                if(p.TryKinderDraw(campaign.Progress,heroineRoll,targetRoll,out var heroine,out var index)) {
                    kinderResult=heroine ? (p.TraitRanks[index]>=PlayableProgress.MaximumTraitRank
                        ? $"★6 {Names[index]} → 汎用強化素材＋1"
                        : $"★6 {Names[index]} → 重複強化用＋1（所持{p.Duplicates[index]}）")
                        : $"育成・家具用素材 ＋{PlayableProgress.KinderMaterialReward(index)}（所持{campaign.Progress.Materials}）";
                    Save(kinderResult); kinderResult=status;
                } else {
                    kinderResult="迎えられませんでした。石・所持上限を確認してください。";
                }
            }
            Label(790,475,410,55,"石は初回配布と討伐で獲得。\nスタミナ消費なし。",small);
            for(int i=0;i<5;i++) if(Btn(340+(i%2)*440,545+(i/2)*48,420,42,$"{Names[i]} と交換",p.AvailableKinderExchanges>0)) {
                if(p.TryKinderExchange(i)) {
                    kinderResult=$"{Names[i]} と交換しました。"+(p.TraitRanks[i]>=PlayableProgress.MaximumTraitRank?"汎用強化素材＋1。":"重複強化用＋1。");
                    Save(kinderResult); kinderResult=status;
                } else kinderResult="交換できませんでした。交換回数・所持上限を確認してください。";
            }
            if(Btn(340,700,860,50,"万物の書へ戻る")) kinderGarden=false;
        }
        private void Mutate(bool success,string message) { if(success) Save(message); else status="素材が不足しているか、すでに最大まで開放されています。"; }
        private void Save(string successMessage="進行を保存しました。")
        {
            if(formalDiagnostic)return;
            if(formalCampaign==null || formalCampaign.HasPending || formalProgression.HasPending){status="保存待ちの操作を先に完了してください。";return;}
            TrialObserve("save","start");
            try { if(!formalCampaign.CommitWorld(campaign.CreateSave(),SaveTrialObservedCampaign))throw new System.IO.IOException("Save rejected");status=successMessage;TrialObserve("save","committed","revision="+formalCampaign.Snapshot.revision); }
            catch(Exception e) {campaign=new CampaignState(WorldCatalog.ColossusIds,formalCampaign.Snapshot.world);status="保存できませんでした。今回の世界変更は確定していません。空き容量と権限を確認してください。";Debug.LogException(e);TrialObserve("save","failed",e.GetType().Name);}
        }
        private void Modal() { Panel(0,80,1600,820,dark); Panel(300,150,970,620,paper); }
        private static void Panel(float x,float y,float w,float h,Texture2D t) {if(t.width==1 && t.height==1)ImageUiSkin.Surface(new Rect(x,y,w,h),t.GetPixel(0,0));else GUI.DrawTexture(new Rect(x,y,w,h),t);}
        private static void Meter(float x,float y,float width,float height,int current,int maximum,Color fill)
        {
            var old=GUI.color; GUI.color=new Color(.2f,.23f,.22f); GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
            GUI.color=fill; GUI.DrawTexture(new Rect(x,y,width*Mathf.Clamp01(maximum>0?(float)current/maximum:0f),height),Texture2D.whiteTexture); GUI.color=old;
        }
        private void Label(float x,float y,float w,float h,string value,GUIStyle style,Color? color=null) { var old=style.normal.textColor; if(color.HasValue) style.normal.textColor=color.Value; GUI.Label(new Rect(x,y,w,h),value,style); style.normal.textColor=old; }
        private bool Btn(float x,float y,float w,float h,string value,bool enabled=true,GUIStyle style=null)
        {
            bool old=GUI.enabled; GUI.enabled=old && enabled && (drawingModal || !(storyText!=null || help || kinderGarden || retreat || result!=null));
            bool clicked=ImageUiSkin.Button(new Rect(x,y,w,h),value,style??button);
            Color edge=GUI.enabled?gold:new Color(.23f,.25f,.25f);
            TitleFill(new Rect(x,y,w,1),edge);TitleFill(new Rect(x,y+h-1,w,1),edge);
            GUI.enabled=old; if(clicked){TrialObserve("navigation","button",value);PlayProductionUiSound(value);}return clicked;
        }
        private void OnApplicationQuit() { if(!recoveryActive && campaign!=null && capturePath==null && combatDefinitionError==null && formalCampaign!=null && !formalCampaign.HasPending && !formalProgression.HasPending){FlushActiveTime();Save();} FinishTrialTelemetry(); }
    }
}
