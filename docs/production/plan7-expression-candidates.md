# スレイヤー表情候補 v1

制作：2026-10-03、Codex／内蔵imagegen。参照・編集対象は `slayer-standing-candidate-v1.png`。既存全身候補から顔だけを変更する指定で、喜び・困惑・決意の全身型RGBA差分を生成した。全4枚は1024×1536、左上alpha=0。正式採用前。立ち絵の目標解像度は未達で、決意の変化は控えめ。

各出力を `game/unity/Assets/Game/Resources/Illustrations/slayer-expression-{joy,puzzled,determined}-candidate-v1.png` に保存した。assetIdは `art.candidate.slayer.expression.{joy,puzzled,determined}.v1`、人物IDは `heroine.slayer`、表情IDは `expression.{joy,puzzled,determined}`。通常は元の全身候補を使用する。出典・権利の未確認事項は[全身候補の記録](plan7-standing-candidate.md)を引き継ぐ。

## 接続

HomeAssetDefに任意のResourcesパスとfullFrameを追加した。ADVは全身型表情なら立ち絵を差し替え、重ね合わせ型なら同一サイズだけを重ねる。サイズ不一致・画像未ロードは通常の立ち絵へ戻す。候補はplaceholderのままで、fixtureのリリース拒否は維持する。他人物はスレイヤー画像を表示しない。

表情比較sceneは通常／喜び／困惑／決意を順に表示する独立fixture。診断起動だけで回想として開き、通常セーブの既読・好感度・報酬を追加しない。`validate-plan7-expressions-player.ps1` は隔離ファイルで全4種を720p／1080p撮影する。本文パネルの上に全身画像を収め、足先を本文で隠さない。

## 品質の限界

検証結果：コア5,549 assertions、Unityスクリプト86ファイル、実Unity1,174 assertions成功。Windows buildは132,928,982 bytes。全4種×720p／1080pの8ケースが成功し、`tmp/plan7-expressions-20261003060602/` に画像・ログを保存した。通常1080p、喜び720p、困惑1080p、決意720pを目視し、透過と足先・剣・翼、本文欄と操作ボタンの可読性を確認した。決意と通常の違いは小さく、顔の読める最終構図が必要。自動撮影は品質採用の代替ではない。

生成は顔以外の完全なピクセル一致を保証しない。衣装・翼・輪郭の微差を含む候補であり、正式な差分制作の合格を宣言しない。表情のみを載せるレイヤー化と輪郭の一致確認、高解像度、戦闘ポーズは後続。制作原本と通常画像は上書きしない。

## 最終プロンプト（内蔵ツール）

追記：実際の描画は [顔領域だけの重ね描画](plan7-expression-layers.md) へ変更した。上記の全身差し替えと初期検証は履歴であり、現在は通常立ち絵の衣装・翼・剣・足先を固定する。元PNGを加工せず、顔矩形の外の画素を使わない。

決意: Use case: identity-preserve. Edit target: provided transparent full-body standing sprite. Create determined expression variant. Change ONLY facial expression: focused blue eyes, slightly lowered brows, confident closed lips, calm resolve, no anger. Keep head angle, face shape, hair, flowers, clothing, body pose, wings, hands, sword, boots, lighting and all artwork outside the face unchanged. Preserve exact 1024x1536 canvas, scale and placement; no recentering or crop. One figure only. Genuine transparent RGBA background, no text or watermark. Game expression sprite aligned with original.

困惑: Use case: identity-preserve. Edit target: the provided transparent full-body standing sprite. Create a puzzled expression variant. Change ONLY facial expression: mildly worried raised inner eyebrows, blue eyes looking thoughtfully forward, small uncertain parted lips, no tears. Keep head angle, face shape, hair, flowers, clothing, body pose, wings, hands, sword, boots, lighting and all artwork outside the face unchanged. Preserve exact 1024x1536 canvas, scale and placement; no recentering or crop. One figure only. Genuine transparent RGBA background, no text or watermark. Game expression sprite to align with original.

喜び: Use case: identity-preserve. Edit target: the provided transparent full-body standing sprite. Create the joy expression variant. Change ONLY the facial expression: open happy smile, softly raised cheeks, bright happy blue eyes. Keep head angle, face shape, hair, every flower, clothing, body pose, wings, hands, sword, boots, lighting and all artwork outside the face unchanged. Preserve exact 1024x1536 canvas, scale and placement; no recentering or crop. One figure only. Genuine transparent RGBA background, no text or watermark. This is a game expression sprite to align with the original.
