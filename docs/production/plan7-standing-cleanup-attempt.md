# 計画7：立ち絵透過修正の不採用記録

2026-10-03。内蔵imagegenの背景抽出・人物維持編集を実施。通常立ち絵v1を参照して、背景の霞・微小粒子を除去し、人物・顔・衣装・武器・位置を維持するよう指定した。生成呼出しの所要時間は約37秒で、人の制作工数とは区別する。

出力原本：`C:/Users/nishi/.codex/generated_images/01a0fe98-69ef-7480-a79f-9b12fef7cf82/exec-684db4a5-75ad-4f16-912f-c93556beff60.png`。比較用コピー：`tmp/slayer-standing-cleanup-v2-rejected.png`。不採用なのでゲームのResourceと表情素材を変更していない。

実際の出力は1024×1536。指定した1600×2400以上には未到達。アルファ1〜16の画素はv1の102,058から54,978へ減ったが、アルファ16超の最大連結領域外の画素は16から28へ増えた（上下左右4近傍）。白い羽・衣装の微差があり、原本との厳密な位置・形の維持を満たさない。既存表情の顔領域をそのまま新立ち絵へ接続したとは記録しない。低アルファや独立領域の画素数は透過品質を調べる指標であり、個々の画素がすべてノイズであることを証明しない。

## 最終プロンプト

Use case: background-extraction / identity-preserve. Edit target: attached existing newASTER Slayer standing sprite. Remove ALL background haze, gray/white glow, stray colored particles and disconnected speckles, producing a clean genuinely transparent RGBA character cutout. Preserve EXACTLY the existing adult character identity, face with neutral gentle expression, blonde bob, blue eyes, flower decorations, pink knitted dress and detached sleeves, floral gold sword, white/gold boots, complete white feather wings, pose, proportions, and their image-relative alignment. Do not redesign or change expression. Preserve feather detail inside the silhouette, use clean antialiased alpha along the silhouette with fully zero alpha in empty space. No floor shadow, no checkerboard drawn into the image, no text or backdrop. Keep the whole forelock, wings, sword and feet inside generous transparent margins. Deliver highest native portrait resolution available, ideally 1600x2400 or greater, with 2:3 aspect ratio. One single sprite.

`transparent_background=true` と原本の `referenced_image_paths` を使用。解像度・原画描画レイヤーの納品条件は未解決のまま。
