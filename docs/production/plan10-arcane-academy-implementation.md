# 計画10：アルケイン学園

2026-10-07。ギャンブラーのアルケイン学園を追加し、加入・育成・神器・編成・戦闘・庭・ADVへ接続した。

3×3 SLOTを3行と2対角線の5ラインで判定する。2つ一致・3つ一致、777、重複ラインの発動を検証した。外れではリソースを消費せず通常WT0、状態異常の待機は適用する。スキル直接選択によるSLOTの迂回を禁止した。

人物の概要、性格、口調、動画の観察時刻、元の意匠と本作の創作は[人物資料](../references/characters/arcane-academy.md)に保存。天使の羽と光輪を含む専用美術18点を生成し、無加工で採用した。生成指示は `game/art-source/plan10/arcane-academy-generation.json`。原動画と参考切り出し画像はゲームやGitへ同梱しない。

3章30ページ・18詩・5イベント40段落を追加した。[アルケイン学園の物語](../story-text/アルケイン学園の物語.txt)に全文を保存。衣装違いは同一人物の関係を共有し、育成・神器・読書進行は別に保持する。

[美術検証](plan10-arcane-academy-art-validation.json)、[実行検証](plan10-arcane-academy-acceptance.json)に採用ハッシュと画面を記録する。全4形態を含む最終実行ファイルは `game/Builds/plan10-shangrila/newASTER.exe`。個別段階のビルドは `game/Builds/plan10-arcane-academy/newASTER.exe`。

共通の最終検証はCore8,546項目、Unity C#185ソースが合格。`tools/validate-plan10-expanded-roster.ps1`、`Plan10ArcaneAcademyBuild.ValidateAndBuild`、`tools/validate-plan10-arcane-academy-player.ps1`で再検証できる。画面採取は隔離した保存データを使う。

![ゲーム内検証](images/arcane-academy-game-comparison.jpg)
