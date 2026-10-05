# アンダーマイン：人物美術の制作記録

更新：2026-10-04。状態：基準12用途候補＋追加CG4／UI肖像1を制作接続、最終採用前。人物ID heroine.undermine／ディフェンダーを維持。

Codex built-in imagegenで新規描画。ローカル動画015の15秒は衣装・髪・目・羽根の観察のみ、抽出画像は配布しない。武器の遮蔽部分は本作独自の猫布飾り付き打撃槌として統一。原本と全制作指示はgame/art-source/plan9/undermineに保存。PNGの描画内部レイヤーは未納品、再編集は再生成を要する。

[画像検査](plan9-undermine-asset-validation.json)は9透過PNGの輪郭・RGBA・原本一致・SHA256。[CG検査](plan9-undermine-cg-assets.json)は5独立CGの原本一致・不透明・横長比。立ち1024×1536、戦闘／SD1254角、肖像／カットイン1536×1024、CG1672×941。正式寸法目安は最終審査に残す。画角修正済み候補も、手指・左右意匠・透過縁の最終採用は未完了。

表情3種類は元の立ち絵を保持し、生成元の顔領域473,302,100,82／1024×1536だけを楕円ぼかしで合成。編集元は元PNG・顔生成元PNG・HomeExperienceFixtureの領域とFacePatch.shader。全身生成元をそのまま差替えない。[描画比較](plan9-undermine-expression-render-validation.json)で両解像度とも顔外変化0。

[先行6画面](plan9-undermine-ui-validation.json)、[修正後6画面](plan9-undermine-battle-validation.json)、[表情／ADV／箱庭28画面](plan9-undermine-home-validation.json)は別ビルドのhashで記録。家具接点は人物別、workは手を机より前に合成。診断で別住人が座面前へ重なったため、診断配置だけを離して再確認した。通常保存は前後不変。追加性能測定・実入力・聴取なし。

基準12用途は立ち1・表情合成3・攻撃／被弾／カットイン3・SD4・CG0の1。CG1〜4と肖像は別枠。[交流設計](plan9-undermine-event-design.md)に一対一でCGを対応する。雨CG初稿は生成結果がツールの安全審査で拒否されたため、外套を加えた休息の構図に変更し設計を更新。拒否画像は採用しない。CG表示は制作審査画面、正式台本・解放・報酬・回想への接続は9-6。

自動回帰5,720項目・Unity1,195項目合格。家具利用テストは本人の作業成功と、対応一覧から一時的に外した人物のidleへの戻りを検証する。候補制作を計画9全体の完了へ読み替えない。

CGビルドの初回は完了を確認せず診断を開始し、旧ビルドがアイコノクラストを表示した。tmp/plan9-ui-78f55ee982644a20851bd3a35145f084は不採用の検証記録。CG人物ID／イベントIDのログ照合を追加し、ビルド終了とPLAYABLE_BUILD_PASSを確認して再実施。[最終CG10画面](plan9-undermine-cg-validation.json)は本人表示を確認、通常保存前後不変。最終ビルド1,195項目／439,471,286 bytes。
