# ジョブ別の実装・確認手順

2026-10-10更新。既存13ジョブの再利用と、新ジョブ・ジョブ仕様変更の手順。ヒロイン全体の制作順は [ヒロイン追加方法](heroine-addition-guide.md) を参照する。

## 現在の基準と変更範囲

既存13ジョブの独立パネルは実装・確認済み。[完了記録](plan11-10-job-panels.txt) と [証跡](plan11-10-job-panels-validation.json) に68操作・227確認項目・27画面を記録している。既存ジョブのヒロイン追加だけで全ジョブの表示確認をやり直さない。追加形態の3スキル、特性、資源との組合せ、人物固有設定を確認する。ジョブ処理を変更した場合はそのジョブ、共通パネルを変更した場合は影響する全ジョブを対象にする。

戦闘データの定義元は `docs/characters/index.json` の `combatSource`。既存ジョブIDは維持する。倍率・消費・WT・期限・マスタリー補正の正本はCoreと現行カタログであり、過去資料の数値を転記しない。マスタリーはキャラ重複のみで強化し、経験値・素材による別経路を作らない。

## 共通の実装順

1. **既存ジョブ再利用か新規かを決める。** 3スキル、固定チェイン行動、ジョブ特性2枠、資源との組合せを決める。人物固有能力とジョブ共通能力を分ける。ファイターのBOOSTとアナイアレイター固有チャージを混同しない。
2. **Coreへ実行規則を接続する。** 主な入口は `game/unity/Assets/Game/Scripts/Core/PlayableBattleJobRules.cs` と下表の専用ファイル。戦闘中の状態は `PlayableBattle` のpartialへ置き、戦闘途中のゲージや投入数を育成セーブへ持ち込まない。READY、対象、行動不能、資源を検証してから一度だけ効果・消費・時間・乱数を確定する。拒否時は状態を変えない。
3. **育成・編成を接続する。** ジョブ定義、`CombatTraitCatalog`、`HeroineTraitRules`、`HeroineIdentityCatalog`、編成の役割表示を揃える。指揮官・護衛・支援・装甲など出撃前設定を追加する場合は人物IDの固定を避け、HomeOperationと保存ジャーナルを接続する。取消は無消費、保存失敗は同一要求で再試行する。旧セーブの欠落項目は既定値で解決する。
4. **独立パネルへ表示する。** `Presentation/BattleJobControls.cs` の `JobPanelResource`、`DrawJobControls`、`JobCommand` を使用する。左は資源・状態・固有コマンド、右は通常3スキル。共通対象変更とパス／防御は下部。`BattleMenuExperience.cs` の既存操作領域内に収め、絵を覆う面積やヘッダを増やさない。論理1600×900の座標を実画面解像度と混同しない。
5. **有効条件・予測・演出を揃える。** UIの無効化とCoreの拒否条件を一致させる。実際の対象・残量・選択値を表示し、UI側に別の計算結果を持たない。設定変更と行動消費を区別する。効果を発生させる操作は既存経路に合わせて `QueueBattleEvents()` へ送り、演出中の二重入力を抑止する。予測・説明・実結果を同じ規則へ接続する。
6. **画像と説明を追加する。** 資源アイコンは `Resources/UI/Jobs/<job>.png`。色だけで区別せず形を分ける。新ジョブは画像、ヘルプ、人物特性、編成表示、診断用フィクスチャと検証対象一覧も追加する。
7. **対象の操作・表示を確認して記録する。** 下表の通常状態と例外状態を自動検証し、デフォルト起動1920×1080の実画像を確認する。物理入力試験は完了条件に含めない。自動成功と目視完了は別々に記録する。

以下のファイル名は、Coreは `game/unity/Assets/Game/Scripts/Core/`、Presentationは `game/unity/Assets/Game/Scripts/Presentation/` を基点とする。

## 13ジョブごとの実装契約

| ジョブ／ID | 接続する操作・状態 | 追加・変更時の確認 |
| --- | --- | --- |
| ファイター `job.fighter` | `PlayableBattleJobRules.cs`。珠の投入選択、捨て身切替、BOOSTゲージ解放。左に珠残量・投入・BOOST・強化状態 | 投入0〜所持上限、±境界、捨て身往復、未蓄積時拒否、解放の一回消費とREADY維持。人物固有チャージとの独立 |
| バーサーカー `job.berserker` | `PlayableBattleJobRules.cs`。捕食段階、資源ゲージ、MAX解放、強化中の二重発動 | MAX未満の拒否、解放後残量・READY、期限切れ。二重発動の効果・対象・消費を既存Core規則と一致させる |
| ディフェンダー `job.defender` | `PlayableBattleJobRules.cs`。出撃前の護衛対象、ゲージ解放、全員護衛・反撃 | 指定対象が表示と一致、MAX未満拒否、READY維持、護衛の有効期限・反撃の非再帰。対象変更の保存互換 |
| ブラスター `job.blaster` | `PlayableBattleJobRules.cs`。詠唱0/50/100/200%、連続1/2/3回。資源と設定を左、スキル予測を右 | 各設定の組合せ、所持資源ちょうど／不足、選択反映、詠唱・連続効果と消費・WTの一致。設定変更自体で行動を進めない |
| ガンナー `job.gunner` | `PlayableBattleJobRules.cs`。2弾倉を別々に表示、使用弾倉選択、全弾倉RELOAD、選択弾倉の全弾射撃 | 弾倉切替、片側のみ空、両側空、リロード、MAX不足。全弾射撃が非選択弾倉まで消費しないことと行動消費 |
| アーティスト `job.artist` | `PlayableBattleJobRules.cs`。歌唱開始・継続・解除、共鳴段階、歌唱ゲージ。歌唱中は右の通常スキル欄を案内へ切替 | 開始不足、継続、解除のREADY維持、歌唱中の全ゲージ取得禁止。人物特性による消費補正を含め実処理と表示を照合 |
| ヒーラー `job.healer` | `PlayableBattleLifeTools.cs`。生命資源、対象変更、heal/overheal/maxhp/revive/investの5操作 | 全5操作、対象別の生死、蘇生対象が生存時の拒否、各消費が一回だけ適用、不足時不変。表示対象と実対象一致 |
| パンツァー `job.panzer` | `PlayableBattlePanzer.cs`、`Plan10ShellExperience.cs`。出撃前耐性・ツール2枠、装甲HP、支援対象、残使用回数、装甲喪失・CALL待ち | 装甲と生身の回復区別、ツール使用・使い切り、再召喚で回数が戻らない。装甲喪失中はスキル・ツール・パス不可で防御導線。新人物では現行シェル固定の設定・保存参照を人物別へ接続して検証 |
| アルケミスト `job.alchemist` | `PlayableBattleAlchemy.cs`、`Plan10AlchemyExperience.cs`。5属性投入数、属性選択、±、クリア、錬成、火弱点付与 | 各属性の増減境界、複数投入、クリア、不足レシピ拒否、錬成消費と投入リセット、READY維持。選択・残量・実消費の一致 |
| チェイサー `job.chaser` | `PlayableBattleChaser.cs`、`Plan10ChaserExperience.cs`。GEAR1/2/3、駆動残量、NITRO蓄積とON/OFF | GEAR別消費はCore参照、資源不足時無効、NITRO未蓄積拒否、ON/OFF、次スキルのWTと消費。消費説明は左資源欄へ収める |
| スナイパー `job.sniper` | `SniperSupportRules.cs`、`PlayableBattleJobRules.cs`。出撃前の支援先、実支援対象表示、MAX消費の狙撃開始、詠唱中全員支援 | 対象設定の保存・実表示、MAX未満拒否、詠唱開始と消費。支援反応が別の支援を呼ばず、支援で通常資源・WTを消費しない |
| ギャンブラー `job.gambler` | `GamblerSlotRules.cs`、`PlayableBattleJobRules.cs`、`BattleMenuExperience.cs`。左SLOT実行、右3×3結果 | 未実行と実行後、3行＋2対角の5ライン、重複ライン・777全発動、全外れだけWT0。9マス・記号・下端装飾の重なり |
| ジェネラル `job.general` | `GeneralFormationRules.cs`、`PlayableBattleJobRules.cs`。本人固有の5枠プロファイル、出撃前指揮官1人、指揮ゲージ・解放 | 選択指揮官のみ効果、非指揮官は固有操作不可、MAX未満拒否、解放READY維持。5枠効果・期限・重複Rank補正と説明の一致 |

## 検証の入口と完了判定

- 共通回帰：`./tools/validate-automatic-chain.ps1`。消費・WT・対象・状態期限・拒否時不変・保存互換を変更に応じて追加する。
- Unity向けC#：`./tools/compile-unity-scripts.ps1`。
- 最新Player：Unity Editorの `Plan119BuildValidation.Build`、出力 `game/Builds/plan12-preparation/newASTER.exe`。同じ出力へのビルドと撮影は直列に実施する。
- ジョブ単位：`./tools/validate-job-panels.ps1 -Views @('job-chaser','job-chaser-low')`。`-PlayerPath` で対象ビルドを指定可能。通常ビューは本番UI分岐の操作検証を実行する。派生ビューは状態別表示用であり、操作件数を二重計上しない。
- 状態別ビュー：`job-artist-singing`、`job-panzer-broken`、`job-gunner-empty`、`job-healer-revive`、`job-alchemist-mix`、`job-gambler-result`、`job-blaster-low`、`job-chaser-low`、`job-general-low`、`job-healer-low`。
- 共通画面変更時：`battle-menu-actions`、`battle-menu-playing`、`battle-menu-healing`、`battle-menu-help` を影響に応じて確認する。

診断フィクスチャ・操作プローブは `Plan10UiAuditAcceptance.cs`。新ジョブは状態準備、成功・拒否条件、結果検証を追加し、`validate-job-panels.ps1` の対象にも登録する。既存プローブの通過だけでは新形態や新効果の受入れにならない。敵HP等を変更する診断は隔離保存のみで実行し、製品定義へ混ぜない。

出力は `tmp/job-panel-validation/`。自動記録の `visualReview=pending` は実画像を見てから別途完了記録へ反映する。対象ジョブ・変更範囲・成功ログ・実画像とハッシュ・撮影時Playerハッシュ・解像度・未解決課題を残す。ツールは `latest-run.json` や同名画像を上書きするため、採用証跡は次の実行前に別名保存する。全13ジョブの再実行は共通変更が影響する場合に限る。
