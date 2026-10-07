# Unityゲーム実装

現行は計画10、15形態・12人・13ジョブ。Unity **6000.6.3f1**で `unity/` を開き、`Assets/Game/Scenes/Bootstrap.unity` から起動する。Windowsビルドは `Plan10ShangrilaBuild.ValidateAndBuild`、出力は `Builds/plan10-shangrila/newASTER.exe`。

通常入口のResourcesは `Combat/battle-plan10-shangrila.json` と `Story/plan10-shangrila-story-content.json`。[実装とUI確認](../docs/production/plan10-four-heroines-and-ui.md)、[ヒロイン追加方法](../docs/production/heroine-addition-guide.md)、[仕様索引](../docs/README.md)を参照する。

## 実装の配置

- `unity/Assets/Game/Scripts/Core/`：描画に依存しない戦闘・ジョブ・成長・世界・保存規則。
- `unity/Assets/Game/Scripts/Data/`：人物ID・物語・世界・経済・美術の実行カタログ。
- `unity/Assets/Game/Scripts/Presentation/`：万物の書、人物・編成・戦闘・庭・ADV・保存確認のUI。
- `unity/Assets/Game/Editor/`：素材・参照・Unity JSON往復・Windowsビルドの検証。
- `unity/Assets/Game/Resources/`：ランタイムJSONと採用画像・音・日本語フォント。
- `art-source/`：編集元、採用原本、生成指示。計画10は生成物の画素を加工せず採用する。

## 検証

リポジトリルートで `tools/validate-plan10-expanded-roster.ps1` を実行する。Core/Data8,546項目とUnity C#185ソースが合格。Playerの画面検証は最新ビルド後に `tools/validate-plan10-ui-player.ps1` と人物別スクリプトを実行する。隔離した保存データを使用し、撮影と同じ出力先への再ビルドを同時に行わない。

## 過去の試作

`core/` のJavaScript縦切りと `Builds/playable/` は過去の試作。現在の正式カタログ・本文・ジョブをこれらの固定編成へ合わせない。旧セーブの扱いは[保存仕様](../docs/systems/save-and-migration.md)、当時のビルドや検証は[履歴](../docs/archive/README.md)を参照する。
