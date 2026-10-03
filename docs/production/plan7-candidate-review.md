# 計画7：開発用見本の採用と仕上げ残件

更新：2026-10-04。計画7は改訂条件で完了。スレイヤー1人・緑還竜1体・背景1セット・家具3点・CG・6音源を開発用見本として採用する。[完了条件と証拠](plan7-completion-status.md)を参照。台帳のcandidateは公開版への最終採用が未確定であることを引き続き表す。

Windows版 `game/Builds/playable/newASTER.exe` の「計画7 ／ 美術見本を見る」で比較する。通常の実戦と箱庭にも同じ素材を接続し、絵を大きく表示して必要時に操作を展開する。[戦闘](battle-menu.md)、[箱庭](garden-menu.md)を参照。見本の操作で通常の保存進行を変更しない。

人物は全身、表情3差分、待機／攻撃／被弾／カットイン、SDの待機・座る・作業・眺める、CGを制作。表情は通常立ち絵に顔領域だけを重ねる。残り4人の画像を完成扱いにしない。

敵は本体＋結晶角冠・左右翼・尾を別PNGにし、同じキャンバスへ描画する。破壊は部位IDに対応した明示消失。大技の全体絵は全部位健在時だけ使用する。[角冠修正](plan7-crown.md)で顔への被りを解消した。

背景3層、ベンチ・作業台・噴水とSDの接地・前後マスク、移動／撤去による使用解除と別プロセスの再読込を検証した。[箱庭の合成](plan7-garden-composition.md)を参照。

BGM16秒＋ヒット・防壁・回復・破壊・撃破は独自の再生成コードを保持する。原本とUnity取り込み後の41境界、全6音の端点・波形、音量保存と停止復帰を検証済み。[音源v2](plan7-audio-v2.md)を参照。聴感は未実施で、公開前の調整項目へ引き継ぐ。

[素材台帳](plan7-candidate-assets.json)の24画像・6音源、[制作指示](plan7-art-prompts.md)、[合成SVG8点](../../game/art-source/2d/README.md)を保存する。合成の位置・倍率・レイヤー・マスクと音源を再生成できる。PNG内部の筆致レイヤーは未納品。現行の実寸で開発画面を成立させる条件と、量産時の推奨寸法を[素材仕様](../presentation/assets.md)で区別する。

現在の受入れは開発見本の表示・操作・データ整合である。残り132〜160画像、最終の美術品質と聴感、正式本文、実制作時間、低性能PC等は[量産準備](plan7-production-estimate.md)と[引継ぎ](plan7-completion-status.md)で管理する。正式コンテンツの公開ゲートは解除しない。

代表画面：[実戦1080p](plan7-crown-screenshots/closed-1080.png)、[部位操作720p](plan7-crown-screenshots/targets-720.png)、[全破壊](plan7-crown-screenshots/broken-720.png)、[座る](plan7-screenshots/sit-1080.png)、[作業](plan7-screenshots/work-1080.png)、[眺める](plan7-screenshots/look-1080.png)、[箱庭の通常画面](garden-menu-screenshots/closed-720.png)。撮影PNGは加工していない。
