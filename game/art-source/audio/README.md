# 計画7の独自合成音源

現行v2の編集元は `tools/Plan7AudioSource.cs`。メロディのMIDI音程、伴奏の基音、波形、減衰、各音のattack／releaseをコードから編集できる。`tools/generate-plan7-audio.ps1` が6つの44,100Hz・mono・16bit PCM WAVとUnityのPCM取り込み設定を再生成する。既存metaのGUIDは維持する。

v1の元コードを `Plan7AudioSource-v1.cs` に保全した。旧WAVは `game/unity/Assets/Game/Resources/Audio/candidate-*.wav`、現行は `candidate-*-v2.wav`。各版のC#クラス名は同じなので、再生成時は別プロセスで対象の1版だけを読み込む。

外部音源・既存の旋律・サンプルは使っていない。候補として保持し、聴感確認済みと扱わない。取り込み後の音源は実行版の `PLAN7_AUDIO_WAVEFORM_PASS` で寸法・無音／クリップ・端点・音程境界を検査する。
