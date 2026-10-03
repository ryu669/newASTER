# スレイヤー全身候補 v1

制作日：2026-10-03。assetId：`art.candidate.slayer.standing.v1`。人物ID：`heroine.slayer`。用途：戦闘の全身表示接続確認。状態：候補、正式採用前。

制作者：Codex、内蔵imagegenによるAI生成。参照：リポジトリ既存の `Art/Concepts/slayer-quality-target-v2.png`。外部ゲームの抽出素材は使用していない。参照画像の権利記録は本生成によって追加確認されたものではなく、正式採用前に確認する。個別ライセンス文書は未添付。

出力：`Resources/Illustrations/slayer-standing-candidate-v1.png`、1024×1536、RGBA。角のalpha=0を確認。翼・剣・足先は画面内。立ち絵の目標約1600×2400と、足元基準y=.94には未到達。表情・ポーズ差分と同一キャンバス確認は未実施。原参照、旧候補絵は保持する。

戦闘の正式ID用マニフェストへ候補のまま接続する。`fullCanvas=true` は全キャンバスの比率を維持して描画し、旧バスト候補の固定切り抜きと区別する。候補であることを画面に表示する。描画以外の定義・セーブ・戦闘結果は変更しない。

## 最終プロンプト（内蔵ツール使用）

接続検証：実Unity 1,174 assertions、Windows build 128,205,974 bytes成功。最終実行ファイルの720p／1080p画像を `tmp/plan7-standing-20261003033344/` に保存し、透過・翼・剣・足先が名札帯に隠れないことを確認した。画像欠落警告・例外なし。全身表示では顔が小さくなるため、最終構図・行動ポーズは後続で調整する。

Use case: stylized-concept. Asset type: original Unity game full-body standing character sprite, candidate art for newASTER heroine.slayer. Input image is the project's existing character design reference, not an edit target. Create ONE single full-body female adult character with the same blonde bob, blue eyes, looping forelock, pink flower ornaments, pink fitted high-neck sleeveless knitted dress, detached pink long sleeves, white boots with gold accents, white feathered wings with pink flowers, and slender floral gold sword. Polished Japanese fantasy illustration with clean detailed linework and luminous soft painted shading. Neutral gentle expression, relaxed standing three-quarter frontal view, sword held down along her side. Fully visible head, forelock, boots, entire sword, both complete wings. Portrait canvas approximately1600x2400, character centered with generous transparent margin, feet grounded at y=.94; no background, shadow plane, text, watermark, inset panels or multiple figures. Genuine RGBA transparency. Preserve design continuity rather than redesigning.
