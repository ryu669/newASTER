# 正式版ゲーム実装領域

`core/`は縦切り用の描画非依存ルールを置く。現在は仕様検証のためのJavaScript実装であり、既存ブラウザ試作の`src/`とは独立している。

正式版は`unity/`にUnity 6000.0.38f1のWindows向けプロジェクトを初期化して開始する。初回ビルド成功までは、UnityのバージョンやURPを確定済みとして扱わない。

実装順と完了条件は[正式版着手パッケージ](../docs/formal-production-start-v0.2.md)および[縦切り詳細仕様](../docs/vertical-slice-spec-v0.2.md)を参照する。
