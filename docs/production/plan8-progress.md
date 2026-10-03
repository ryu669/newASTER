# 計画8：実装・検証記録

更新：2026-10-04。8-1を検証済み。8-2の計測層を戦闘・操作・保存へ接続し、8-3の試遊本文を制作・データ検証済み。8-2の全分類統合と8-3のゲーム接続、8-4〜8-10は未完了。計画8全体は未完了。[詳細計画](plan8-implementation-plan.md)と[受入ケース](acceptance.md#計画8縦切り通し試遊と調整)を定義元にする。

## 8-1：基準版と試遊対象

基点はmainの`d83cc95`。策定ブランチ`codex/plan8-detailed-plan`はこのmainから作成済みで、PRのbaseもmain。計画PRへ最初の実装を追加する。ユーザーのProjectSettingsと保存は変更対象に含めない。

`Resources/Trial/plan8-baseline.json`に試遊対象の緑還竜1体、正式5人の順序、敵Lv1〜50・最強技Lv45、4定義のResource参照、10計測分類、必要本文8章・イベント1本を記録した。statusはdevelopment-trial。本文の必要数は制作目標であり、制作済み件数ではない。

`TrialDefinition.Validate`は敵・人物参照、順序、Lv境界、用途、schemaVersion、定義参照の欠落／重複／未知値、計測分類と制作目標を検査する。空のJSONが既定値で合格しない。現行の戦闘・報酬・ガチャ・交流定義を読み取るが、数値変更は行わない。

### 固定した基準と調整箇所

[基準スナップショット](plan8-baseline-source.json)にmainのCore／Data／Presentation／EditorのGit tree、10定義・ソースの基準blobと作業中blob・SHA-256、実装前のローカルPlayer／Assembly-CSharp／resources.assetsを記録した。これはビルド・実行・性能の新しい合格記録ではない。更新したプレイヤーのハッシュは別の実行結果へ記録する。

| 項目 | 現行の基準・所在 | 後続の段階 |
| --- | --- | --- |
| 正式5人・15技・資源・待機・防御 | `Combat/battle-formal.json` | 8-4。人物／装備によるチェイン率強化は追加しない |
| 緑還竜・Lv曲線・4部位・行動 | `Data/ColossusCombatCatalog.cs` | 8-4。Lv44／45境界と固有破壊効果を維持 |
| 詩・章・対応・報酬帯 | `Data/CollectionContractFixture.cs`、`Presentation/FormalVictoryView.cs` | 8-3／8-5。fixtureを正式本文に変更したと記録しない |
| 庭・家具・交流・装備ノード・ADV | `Data/HomeExperienceFixture.cs` | 8-3／8-6。未制作の庭・人物絵・本文を明示 |
| 初期配布 | `Presentation/FormalGrowthView.cs`の5人Lv1・ネクタル2940・結晶20 | 8-6。新規一度だけで、既存保存へ再配布しない |
| 討伐報酬 | `Core/FormalCampaignJournal.cs`のネクタル60＋10L、結晶min(5,1＋L/10)、石30＋2L | 8-6。確定済みreceiptを再計算しない |
| 育成費用・覚醒・重複 | `Core/FormalProgression.cs` | 8-6。Lv上限・原子的確定と不足／上限時無消費を維持 |
| 抽選・交換 | `Economy/kinder-trial.json`、`Core/FormalKinder.cs` | 8-6。300石／3000石・3%均等・100pt・承認済み97%を維持 |
| ログイン・時間 | `Economy/engagement-trial.json` | 8-6。日替わり300石・active1800秒100石は検証用。OS時計を変更しない |

### 欠落・未制作と測定項目

緑還竜の本文3章、人物5人の各第1章、本文由来の詩54件と対応理由、交流イベント1本は8-3で制作する。現行のfixtureの章・poem ID・技術sceneをその完成数へ数えない。他4人の完成絵、他14敵、全正式本文と最終美術は後続範囲。

時間、戦闘、詩収集、経済、庭、読書、導線、保存、音、性能を試遊runへ結ぶ。現在の基準定義は記録項目の集合を固定する段階で、計測や一周の実装・調整値採用は8-2以降。実経過・アクティブ・演出秒・TIMEの計測を混同しない。

### 診断と通常保存の境界

`TrialDiagnosticBoundary`は`<repository>/tmp/plan8-runs/<runId>/formal-campaign-v1.json`とrun固有の診断identityを導出する。作成・読込・保存はしない。通常保存rootとの祖先／子孫重複、相対root、不正run ID・Windows予約名、再利用run、既存のjunction／symlink経由を拒否する。ここでのidentityは診断runの名前空間であり、通常`FormalCampaignSave.Identity`の検証を緩和しない。診断の実ファイル保存・codec接続は後続でこの境界を使用して実装する。

`-validatePlan8Baseline`はAwakeの最初に処理し、通常の保存store・初期配布・報酬初期化より前に終了する。4Resourceの読込とUnity JSON検証を行い、storeOpened=False／saveWritten=Falseを記録する。失敗時はexit2。通常起動は既存の初期化へ進む。

`tools/validate-plan8-baseline-player.ps1`は通常保存・設定ファイルの前後ハッシュを比較し、正常な診断と`-UnsafeRunFixture`によるパストラバーサル拒否を検査する。batchmode・nographicsの定義確認で、操作・外観・性能の合格へ読み替えない。ユーザーの他アプリを終了しない。

### 検証

Core5,617 assertions（基準・参照・保存境界の異常系34項目追加）、C#96ファイルのコンパイルが合格。既存の非推奨APIと未割当フィールドの2警告を維持。Unity1,181 assertions、Windowsビルド293,082,310 bytesが合格。最終ビルドで正常診断・不正run拒否の2起動が合格し、通常保存と設定の前後ハッシュが一致した。通常の戦闘画面も720p／1080pの2起動で回帰し、24画像・6音源・日本語字形の読込が合格。最終ビルドの採用結果は計4起動。720pの画面を目視確認した。再現結果は[検証JSON](plan8-baseline-validation.json)。性能測定は実施していない。

再現：`tools/snapshot-plan8-baseline.ps1`、`tools/validate-automatic-chain.ps1`、`tools/compile-unity-scripts.ps1`。Unityビルド後に`tools/validate-plan8-baseline-player.ps1`と同スクリプトの`-UnsafeRunFixture`を実行する。

## 8-2：任意のrun計測層を接続

`TrialRunTelemetry`は単調増加時計とUTC、出力sinkを注入する。経過秒・アクティブ秒・TIME tick・演出予定秒を分け、eventIdで重複を除く。書込・時計・serializerの例外はゲームへ返さず、runをIncompleteにする。乱数・ゲーム状態・保存writerを保持しない。

Windows起動の`-plan8Telemetry -plan8RepositoryRoot <repo> -plan8RunId <new-id>`で任意に有効化する。ログは8-1境界で導出した`tmp/plan8-runs/<id>/events.jsonl`を排他新規作成する。通常起動は無効。これはログの隔離であり、通常プレイのセーブ先を切り替える指定ではない。診断には既存の保存隔離された`-presentationCapture`を併用する。

各行にrun/session/操作/battle ID、seed、入力方式、基準定義版、assembly・resources.assets・参照定義本文のSHA-256を記録する。戦闘受理／拒否、描画演出、敵行動完了の歌唱、結果と取得、開始残高、正式保存の成否・残高・取引、生活取引、ADVの開始／中断／読了・行既読、共通ボタン操作、画面移動、焦点・停止・判断／演出の区間を観測する。保存を観測するwriterは元の保存を同じ回数だけ呼び、記録失敗で成否を変えない。比較用の内部再生はrunログへ混入させず、診断自動入力を人間の試遊入力と区別する。

Coreでは4 seed×計測OFF／ON／IO例外sinkを比較し、全演出イベント・HP・TIME・資源・部位・歌唱取得・報酬と最終保存JSONが一致。100秒の非アクティブ区間、重複ID、時計逆行、出力失敗も検証した。試遊本文の試験を含めCore5,679 assertions、C#99ファイル、Unity1,184 assertionsが合格。最終Windowsビルドは293,131,126 bytes。

`tools/validate-plan8-telemetry-player.ps1`で720pの実描画戦闘を検証し、159件の記録と66演出、4再生方式の結果・報酬一致、ビルド／素材hash、単調時計、通常保存・ファイル設定の前後hash一致を確認。採用runは`telemetry-4756bdb738604b1d882cbe0cbb38fc3e`、出力は`tmp/plan7-sample-20261003224158`。詳細は[検証JSON](plan8-telemetry-validation.json)。過程の旧ビルドrunは最終証拠に加算しない。

性能測定の既存出力を同じrunへ結ぶ部分と、一周で各生活・読書・経済の不足理由を集計する受入れは残る。T8-02全体を完了にはしない。性能測定は未実施。人間の判断・聴感・実AltTabの証拠ではない。

同じ最終ビルドで1080p通常戦闘のResource・日本語字形回帰も合格し、大きな敵／人物表示と閉じた操作メニューを画像で確認した。出力は`tmp/plan7-sample-20261003224538`。負荷測定には使用していない。

## 8-3：本文と明示対応を制作

[試遊本文の契約](plan8-story-content.md)とResource JSONを追加。緑還竜3章24詩、正式5人の第1章30詩、スレイヤー交流イベント1本をオリジナルで制作した。各詩の本文内引用、章・所有者、異なる30対応と理由、好感度1・恋人非確定のイベントを検証する。人物第2・3章は制作していない。

Coreの異常系とUnity JSON importは合格。既存Collection/Homeはfixture内容版だけを受け入れ、旧読了と実歌唱対応もその版に固定されている。この版を黙って上書きせず、新本文の取得・読書・保存の版切替を接続する作業が次に必要。現段階ではゲーム内の本文・歌唱対応は変えていない。8-3／T8-03は未完了。

新規本文の制作窓を[作業記録](plan8-story-authoring-window.json)に残した。開始時刻からResource作成時刻までの経過であり、独占的な実作業時間は計測できていないためnull。後続ビルド待機・過去の美術工数を本文の実作業時間に加算しない。
