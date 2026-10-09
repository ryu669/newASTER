物語・交流の本文確認用書き出し

人物資料：docs/characters/README.md（12人物・15形態）。性格・口調・関係の段階を確認して推敲する。
現行収録：game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json
物語本文.txt：90章・630詩。表示ページがある章は全ページを収録。
回想本文.txt：75交流。個別ファイルは人物・形態別の確認用。
生成：tools/export-story-text.py。文字コード：UTF-8 BOM付き。

修正元は初期5形態がgame/story-source/plan9の原稿JSON、追加10形態がtools/author-plan10-*-story.pyの本文。
本文を直接修正し、収録JSONと本書き出しを同期する。書き出しTXTの編集だけではゲームに反映されない。
主人公の基本呼称は「指揮官」。人物資料に沿った「指揮官さん」「指揮官くん」などを使用する。
検証：tools/validate-story-character-consistency.py（呼称、ID・解放条件の維持、再生成結果）。
見直し記録：docs/production/story-character-review-2026-10-10.txt。
