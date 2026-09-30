# 正式版ゲーム実装領域

`core/`は縦切り用の描画非依存ルールを置く。現在は仕様検証のためのJavaScript実装であり、既存ブラウザ試作の`src/`とは独立している。

正式版のUnityプロジェクトは`unity/`にあり、使用バージョンは6000.6.3f1。`Assets/Game/Scenes/Bootstrap.unity`から起動する。WindowsビルドとUnity内検証は`PlayableBuild.ValidateAndBuild`を実行し、出力先は`Builds/playable/newASTER.exe`。URP採用はTBD。

Unityがライセンス接続で起動できない場合も、リポジトリのルートで`./tools/validate-unity-core.ps1`を実行すると、Unityに依存しないC#戦闘ルールを検証できる。この検証はWindowsビルド・画面の操作確認・Unityの保存検証を代替しない。

2026-09-30の戦闘判断UIと修正内容は[実装・検証記録](../docs/formal-battle-decisions-v0.1.md)を参照。

育成画面には覚醒2段階（Lv上限50→80→120）と1・5・10Lvの一括育成を接続した。[覚醒・一括育成の仕様と検証](../docs/playable-awakening-v0.1.md)を参照。素材量は縦切り検証用で、正式バランスはTBD。

Unity APIに対する全スクリプトのC#コンパイルは`./tools/compile-unity-scripts.ps1`で確認できる。Unity内の実行・保存検証とWindowsビルドは別途必要。

キンダーガーデンの素材付与・重複強化・汎用素材への変換は[報酬実装記録](../docs/playable-kinder-rewards-v0.1.md)を参照。ガチャ結果は画面内に表示する。

実装順と完了条件は[正式版着手パッケージ](../docs/formal-production-start-v0.2.md)および[縦切り詳細仕様](../docs/vertical-slice-spec-v0.2.md)を参照する。
