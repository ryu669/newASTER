# 計画10：ナイトホーク

2026-10-07。チェイサーのナイトホークを追加。全体は11形態・10人・10ジョブ。残りはスナイパー・ギャンブラー・ジェネラル。

人物・戦闘動画の観察時刻、性格・口調・概要、映像から確認した意匠と本作独自の調整は [人物資料](../references/characters/nighthawk.md) に保存。成人設定の独自美術18点（立ち絵、肖像、表情3、攻撃・被弾・カットイン、SD4、神器、CG5）を生成し、透過・Unity読込・採用ハッシュを検証する。

GEAR 1/2/3、NITRO予約と実行時消費、戦闘時間によるギア回復、攻撃・固定連鎖によるIGNITION、非再帰のOVER IGNITIONを実装。本人の3スキルは攻撃・速度強化、防御無視と凍傷連携、全体火傷・凍傷と連鎖接続率増加。通常ギアは最低WT1、NITRO使用時だけWT0を認める。NITRO予約中の状態異常による強制スキップにも通常待機を適用し、時間が止まるループを防ぐ。

新規ヒロインの追加で神器の割当が一覧のソート順に左右される問題も修正。神器の持ち主は固定IDで決定し、シェル・オリフラムを含む全人物の枝・能力・意匠を安定させた。無効な攻撃対象はダメージ補正の参照前に拒否し、選択済みギアやNITROを消費しない。

所持一覧、図鑑、無償加入、5人混成、育成、13ノードの神器、庭、ADVへ接続。3章30ページ・18詩・5イベント40段落は本作独自の創作で、親愛10のイベントで相互の告白が成立する。全文は [ナイトホークの物語](../story-text/ナイトホークの物語.txt)。

生成指示は `game/art-source/plan10/nighthawk-generation.json`。生成画像は無加工で採用。元動画・切り出した参考画像はゲームやGitHubへ同梱しない。美術検証は [art-validation](plan10-nighthawk-art-validation.json)、実行検証は [acceptance](plan10-nighthawk-acceptance.json) に記録する。

Windows実行ファイル：`game/Builds/plan10-nighthawk/newASTER.exe`。再検証：`tools/validate-automatic-chain.ps1`、`Plan10NighthawkBuild.ValidateAndBuild`、`tools/validate-plan10-nighthawk-player.ps1`。実行時の画面採取は隔離された保存データを使う。

最終Core検証は8,342項目、Unity C#コンパイルは163ソースで合格。Windowsビルドと各画面の検証結果は上記の受入れJSONへ記録。
