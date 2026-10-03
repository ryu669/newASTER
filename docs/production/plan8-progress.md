# 計画8：実装・検証記録

更新：2026-10-04。8-1の基準定義・診断境界を実装し、対象の検証を完了。8-2以降は未着手。計画8全体は未完了。[詳細計画](plan8-implementation-plan.md)と[受入ケース](acceptance.md#計画8縦切り通し試遊と調整)を定義元にする。

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
