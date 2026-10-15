# Unityゲーム実装

現行は26形態・23人・13ジョブ。Unity **6000.6.3f1**で `unity/` を開き、`Assets/Game/Scenes/Bootstrap.unity` から起動する。Windowsビルドは `Plan12EstreyaBuild.ValidateAndBuild`、出力は `Builds/plan12-estreya/newASTER.exe`。

通常入口は `ProductionContentPaths` で戦闘・物語を指定する。現行Resourcesは `Combat/battle-plan12-estreya` と `Story/plan12-estreya-story-content`。[5人の完了記録](../docs/production/plan12-first-batch-runtime-validation.json)、[ヒロイン追加方法](../docs/production/heroine-addition-guide.md)、[仕様索引](../docs/README.md)を参照する。

## 実装の配置

- `unity/Assets/Game/Scripts/Core/`：描画に依存しない戦闘・ジョブ・成長・世界・保存規則。
- `unity/Assets/Game/Scripts/Data/`：人物ID・物語・世界・経済・美術の実行カタログ。
- `unity/Assets/Game/Scripts/Presentation/`：万物の書、人物・編成・戦闘・庭・ADV・保存確認のUI。
- `unity/Assets/Game/Editor/`：素材・参照・Unity JSON往復・Windowsビルドの検証。
- `unity/Assets/Game/Resources/`：ランタイムJSONと採用画像・音・日本語フォント。
- `art-source/`：編集元、採用原本、生成指示。計画10は生成物の画素を加工せず採用する。

## 検証

リポジトリルートで `tools/validate-automatic-chain.ps1` と `tools/compile-unity-scripts.ps1` を実行する。Core/Data12,435項目とUnity C#278ソースが合格。Playerは `tools/validate-plan12-first-batch-player.ps1` と人物別スクリプトを使用する。確認済みの形態は変更のある画像・表示経路だけを再確認する。隔離した保存データを使用し、撮影と同じ出力先への再ビルドを同時に行わない。

## 過去の試作

`core/` のJavaScript縦切りと `Builds/playable/` は過去の試作。現在の正式カタログ・本文・ジョブをこれらの固定編成へ合わせない。旧セーブの扱いは[保存仕様](../docs/systems/save-and-migration.md)、当時のビルドや検証は[履歴](../docs/archive/README.md)を参照する。

インヴィジブルの追加・画像・戦闘・保存の受入記録は [完了記録](../docs/production/plan12-invisible-form-record.json)を参照。

ヴァイパーの専用21画像・3技・旧セーブ移行・標準画面19ケースの [完了記録](../docs/production/plan12-viper-form-record.json)。ヴィンディーケーターとウェポンマスターも通常プレイへ追加済み。装備の木は全24形態で共通化し、吊り下げ装飾を廃止した。

ウェポンマスターの[完了記録](../docs/production/plan12-weapon-master-form-record.json)、全24形態の[共通の木の確認記録](../docs/production/plan12-common-weapon-tree-review.json)。

エイプリル通常版の専用20画像・3章・5交流・3技と旧24形態セーブ移行を確認。[完了記録](../docs/production/plan12-april-form-record.json)。当時の通常プレイは25形態・22人物。

2026-10-15：エストレヤ（アズ・ボーンフリー／バーサーカー）を追加。通常プレイは26形態・23人物。専用20画像、3章30ページ・18詩・5交流、共通13ノードの木、HP条件付き攻撃・反動・ヒートアップ、旧25形態セーブからの加入を確認。[完了記録](../docs/production/plan12-estreya-form-record.json)。
