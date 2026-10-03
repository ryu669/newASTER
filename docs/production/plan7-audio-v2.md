# 計画7：音源v2の切り替えとループ境界

2026-10-03。BGMの0.5秒ごとの旋律、回復SEの0.2秒ごとの音程、撃破SEの0.3秒ごとの音程切り替えへ、raised-cosineの立ち上がりと終端を加えた。伴奏の4秒区間もsinの二乗で立ち上がり／終端の傾きを0へ寄せる。全6音の両端を0とし、非BGMの全体attack／releaseも滑らかにした。撃破音の最後の音は残り0.9秒を使って減衰する。

旧WAVと [v1元コード](../../game/art-source/audio/Plan7AudioSource-v1.cs) を保持し、[現行編集元](../../tools/Plan7AudioSource.cs)から `candidate-*-v2.wav` を再生成する。全6音は独自合成、44,100Hz・mono・16bit PCMで、BGM16秒、回復1.2秒、撃破2.4秒、ヒット／防壁／破壊0.65秒。見本・実戦・ADVのBGM／SEと素材台帳をv2へ接続した。進行ID・保存形式・報酬を変更しない。

## 波形の確認

`tools/audit-plan7-audio-transitions.ps1` は固定PCMヘッダー、サンプル数、両端、音程境界の波形を検査する。切り替えの直前・直後の傾きから推定する中央の差と、実際のサンプル差との最大誤差を比較する。単位はsigned 16bit PCMで、v2の許容上限を8とする。

| 対象 | v1最大境界誤差 | v2最大境界誤差 | v2検査境界数 |
| --- | ---: | ---: | ---: |
| BGM | 363.5 | 1.5 | 31 |
| 回復 | 2219.5 | 0 | 5 |
| 撃破 | 1349.5 | 0 | 5 |

両端の差は両版とも0。BGMのループ継ぎ目の傾き誤差も0。v2は境界付近の傾きも抑え、端点値だけでは検出できない音程切り替えを改善した。これは波形の連続性の検査で、曲の快さ、効果の聞き分け、ループの聴感合格を示さない。[公開用波形記録](plan7-audio-transition-metrics.json)に両版の境界ごとの値を保持する。再生成後の6WAVのSHA-256も同一である。

新規v2音源のUnity既定取り込み設定はVorbis・正規化有効だった。v2はPCM・正規化無効・事前ロード有効へ明示固定し、元波形を維持する。`ValidateCandidateAudio` はWindows実行版でAudioClip.GetDataを読み、周波数・mono・サンプル数、全6音の無音／クリップ／非有限値・両端と、3音の41境界を検査する。実際の取り込み後もBGM1.5、回復・撃破0の誤差だった。

Windowsの設定／停止復帰720p・1080pは `tmp/plan7-sample-20261003121456`、4方式の戦闘・報酬一致720p・1080pは `tmp/plan7-sample-20261003121520` で合格。波形記録は `tmp/plan7-audio-transitions.json`。Unity1,174 assertions、C#92ファイル（既存警告2件）、Windowsビルド286,776,726 bytes。ログ `tmp/plan7-audio-v2-pcm-build.log`、Assembly-CSharp SHA-256 `E15B1FA676B2747D34E89A90CAC7F1CCDEDB3EBF938FDD68B1947B287BB7B8E9`。この版の性能測定は実施していない。

新保存からのADV720p／1080pも `tmp/plan6-home-player-20261003121702-65e3d13628b84a5993fee483c3f1fe62` で合格。今回の最終ビルドは計6起動を確認した。

後続で同SHAの連続戦闘・箱庭・CGを測定し、さらに通知修正版の戦闘を720p／1080pで測定した。[ビルド別性能記録](plan7-active-performance.md)を参照。上記の「性能未実施」は音源v2取り込み完了時点の記録である。
