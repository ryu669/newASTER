# エクスカリパン：人物美術の制作記録

更新：2026-10-04。人物ID heroine.excalipan／ガンナーを維持。基準12用途候補＋追加CG4／UI肖像を制作・接続。最終採用前。

Codex built-in imagegenで新規描画。ローカル動画024の15秒は髪・帽子・衣装・羽根の観察のみ。原本と指示はgame/art-source/plan9/excalipan。遮蔽された武器は本作独自の木と真鍮の工房式銃として統一。攻撃初稿の銃口光と羽根、CG0初稿の足元の欠けを画角編集で修正した。

[画像検査](plan9-excalipan-asset-validation.json)は9透過PNGの原本一致・RGBA・輪郭。[CG検査](plan9-excalipan-cg-assets.json)は5独立CGの原本一致・横長・不透明。立ち1024×1536、戦闘／SD1254角、肖像／カットイン1536×1024、CG1672×941。正式寸法目安・手指・左右意匠・透過縁は最終審査に残る。PNG内部描画レイヤーは未納品。

表情3種は元の立ち絵を保持し、顔領域432,182,130,104／1024×1536だけを楕円ぼかしで合成。喜びの生成元は顔位置にずれがあるため、生成元yを174として対応。[描画比較](plan9-excalipan-expression-render-validation.json)で両解像度とも顔外変化0。全身生成元をそのまま差替えない。

[戦闘・育成10画面](plan9-excalipan-battle-validation.json)、[表情・ADV・庭園28画面](plan9-excalipan-home-validation.json)、[CG10画面](plan9-excalipan-cg-validation.json)をビルドhashと共に記録。最初の庭園診断は他住人が脚に重なったため、診断配置を下へ離して再確認。着座の脚と座面、机の手前に出る手を確認。通常セーブ前後不変、追加性能測定・実入力・聴取なし。

[交流設計](plan9-excalipan-event-design.md)と5CGを一対一に対応。雨と夜明けの肩掛けを追加。基準12用途は立ち1・表情3・戦闘3・SD4・CG0の1、CG1〜4／肖像は別枠。正式本文・条件・回想への接続は9-6。

最終Unity1,195項目／581,042,230 bytes、自動回帰5,728項目合格。候補制作を計画9全体の完了へ読み替えない。
