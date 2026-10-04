# 計画9-4：アイコノクラスト制作ロット1

更新：2026-10-04。人物ID `heroine.iconoclast`、バーサーカー。状態：基準12用途候補を制作・接続。追加CG4／UI肖像1は別枠、最終採用前。

## 制作元と識別

作者／制作方式：Codexによるbuilt-in imagegen。利用先：newASTERの育成・戦闘表示。生成PNG原本と[全制作指示](../../game/art-source/plan9/iconoclast/prompts.json)を編集元として保持する。PNG内部の描画レイヤーは未納品で、再編集は再生成を要する。

ローカル動画 `005戦闘_アイコノクラスト.mkv` の15秒を人物意匠の観察資料に使用。資料画像・動画はGit／配布へ入れない。短い桃色髪・金色の瞳・赤と金の上着・白桃色の胴衣・格子柄の短衣・白い翼・白金のブーツ・片手剣を保持し、新しい立ち／攻撃ポーズとして描き起こした。生成物のサービス利用条件に従う。人物意匠を含む最終権利審査は公開ゲートで未完了として保持する。

| 素材ID | ファイル | 用途・状態 |
| --- | --- | --- |
| iconoclast-standing | `Illustrations/iconoclast-standing-candidate-v1` | 育成立ち絵・戦闘待機。1024×1536 RGBA。正式寸法1600×2400目安には未到達 |
| iconoclast-attack | `Illustrations/iconoclast-attack-candidate-v1` | 戦闘の攻撃／詠唱発動。実寸は検査JSONを正とする。透過縁・衣装差を最終修正対象に残す |
| iconoclast-portrait | `Illustrations/iconoclast-portrait-candidate-v1` | 育成カード用の上半身。1536×1024 RGBA。翼外縁／胴体の切取りは用途上の意図。頭頂を画角修正。人物12画像基準とは別のUI補助絵 |
| iconoclast-hit | `Illustrations/iconoclast-hit-candidate-v1` | 全身の被弾反応。1254角RGBA。衣装は損傷なし、実際の被弾対象IDで選択 |
| iconoclast-cutin | `Illustrations/iconoclast-cutin-candidate-v1` | 大技／フルチェインの上半身。1536×1024 RGBA。外翼／下半身は意図した切取り、顔と剣先を収める |

初回立ち絵では翼が画像端に接したため不採用。透過修正と画角修正を実施した。ツールのプレビューは背景RGBを表示していたが、実データの角はalpha=0である。採用候補の透過量とalpha>32の輪郭範囲を自動照合し、背景除去を見た目だけで判断しない。拡大書出しで正式原画の解像度を満たした扱いにしない。

## 接続と残件

ロット3で下記の未制作用途へ候補を追加した。ロット1／2の記述は当時の検証記録である。

表情3種類は生成元PNGの顔部分のみを元の立ち絵へ転写。全身生成元の差替えは不採用のまま。喜びの不採用試作も顔の内部のみ再利用する。編集元は元立ち絵・生成元PNG・HomeExperienceFixtureの転写矩形・FacePatch.shaderの楕円ぼかし。矩形のみの初稿には継ぎ目があったため改修。[描画比較](plan9-iconoclast-expression-render-validation.json)で720p／1080pとも顔領域外変化0。生成元全身そのものが不変という意味ではない。

SD idle／sit／work／lookを本人へ接続し、家具接点・手の前後マスクを人物別に指定。[28描画](plan9-iconoclast-home-validation.json)で配置・移動・解除・再読込を検証。ADV先行画像は翼の左右が切れたため横幅を広げた。[最終ADV見本](plan9-iconoclast-adv-720.png)。Unity JSONが未指定領域を空オブジェクトにするため、切取りをusePortraitCropで明示する。

[交流設計](plan9-iconoclast-event-design.md)とCG5候補はevent.0〜4に一対一対応。原本1672×941、正式寸法目安への到達とはしない。CG0／3の髪切れ初稿を画角修正。[最終18描画](plan9-iconoclast-cg-validation.json)でADV4表情／CG5場面を両解像度で確認。正式台本・解放・報酬の接続は9-6、CGは制作審査画面への接続である。

基準12用途は立ち1＋表情合成3＋戦闘3＋SD4＋CG0の1、別枠はCG1〜4／UI肖像。候補の最終採用は未完了。Unity1,195 assertions／368,685,030 bytesと自動回帰5,717 assertions合格。家具テストは新たな本人利用を追加し、未制作人物のidle検証をアンダーマインへ移した。旧validate-unity-core.ps1はUnity専用コードを誤抽出してコンパイルに失敗し、合格根拠には使わない。追加性能測定・実入力・聴取なし、通常保存前後不変。

`battle-formal.json`の人物IDへ待機／攻撃画像を接続し、placeholderを維持する。攻撃絵を被弾・カットインの完成数に数えない。育成画面は本人の絵を表示し、制作候補と明示する。他3人の表示は本人素材の制作待ちを維持する。診断で表示人物だけを選択し、戦闘の手番／HP／費用や保存内容を変えない。

12画像基準に対して本ロットは4画像。残りはSD4、表情3、正式イベントCG1の既定用途を照合して制作する。UI肖像は別枠。衣装と左右意匠、手指、羽根と透過縁、接地anchor、顔だけの表情差、正式原画寸法を最終採用前に確認する。残り3人とスレイヤーの最終審査も未完了。

育成の全身縮小では顔が小さくなったため、上半身のUI補助絵を追加。[肖像制作指示](../../game/art-source/plan9/iconoclast/portrait-prompts.json)とPNG原本を同じ編集元フォルダへ保存した。基準12画像の表情3差分へ勝手に計上しない。全身と攻撃の可視輪郭は全キャンバス内、肖像の翼／胴体は意図した切取りとして検査を分ける。

## ロット2：被弾・カットインと表情の差分審査

[戦闘制作指示](../../game/art-source/plan9/iconoclast/battle-prompts.json)と原本を保存。被弾表示のスレイヤー固定条件を除き、敵イベントの対象IDに一致する人物を表示する。複数対象は編成順の最初の対象を代表表示する。診断指定は実際の手番と区別し、既存の攻撃／カットイン優先条件を維持する。5人それぞれの被弾対象と攻撃／待機の非被弾状態をUnity検証に追加した。

喜び表情の試作は[画素差分審査](plan9-iconoclast-expression-rejection.json)で不採用。顔を広めに含む領域外にも834,153画素の変化があり、衣装・ポーズ・輪郭の同一性を満たさなかった。未採用画像をResourcesや素材数へ入れず、[制作指示と不採用理由](../../game/art-source/plan9/iconoclast/expression-attempt-prompts.json)のみを保存。表情3差分は未完了のまま。
