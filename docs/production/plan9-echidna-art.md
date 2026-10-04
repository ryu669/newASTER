# エキドナ：人物美術の制作記録

更新：2026-10-04。人物ID heroine.echidna／ブラスターを維持。基準12用途候補＋追加CG4／UI肖像を制作・接続。最終採用前。

Codex built-in imagegenで新規描画。ローカル動画023の15秒は人物の外見観察のみ。原本と制作指示はgame/art-source/plan9/echidna。銀髪・巻角・白い花弁衣装・金と青石・濃紫の花弁状の翼を統一。立ちと攻撃の初稿は翼先端の欠けを画角編集で修正。CG0は上の枝、CG2は足元の欠けを画角編集で修正した。

[画像検査](plan9-echidna-asset-validation.json)で9透過PNGの原本一致・RGBA・画像端の輪郭を検証。[CG検査](plan9-echidna-cg-assets.json)で5独立CGの原本一致・横長・不透明を検証。立ち1024×1536、戦闘／SD1254角、肖像／カットイン1536×1024、CG1672×941。正式寸法目安と手指・左右意匠・透過縁は最終審査に残る。PNG内部の描画レイヤーは未納品。

表情3種は元の立ち絵を保持し、生成元の顔領域466,210,113,76／1024×1536を楕円ぼかしで合成。全身の生成元をそのまま差替えない。編集可能な構成は原本・生成元・HomeExperienceFixtureの領域指定・FacePatch.shader。[描画比較](plan9-echidna-expression-render-validation.json)で720p／1080pとも顔外変化0。

[戦闘・育成10画面](plan9-echidna-battle-validation.json)、[表情・ADV・庭園28画面](plan9-echidna-home-validation.json)、[CG10画面](plan9-echidna-cg-validation.json)をビルドhashと共に記録。庭園は本人の4ポーズ、家具利用／移動／解除を確認。作業の手は机より手前に描く。通常セーブ前後不変、追加性能測定・実入力・聴取なし。

[交流設計](plan9-echidna-event-design.md)とCGを一対一に対応。雨と夜明けは旅行用外套を追加。基準12用途は立ち1・表情3・戦闘3・SD4・CG0の1、CG1〜4／肖像は別枠。CG表示は制作審査画面であり、正式本文・条件・報酬・回想への接続は9-6。

最終Unity1,195項目／510,256,998 bytes、自動回帰5,724項目合格。候補の制作を計画9全体の完了へ読み替えない。
