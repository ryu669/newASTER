# 計画10：シャングリラ

2026-10-07。スナイパーのシャングリラを追加し、加入・育成・神器・編成・戦闘・庭・ADVへ接続した。

通常支援は選択した味方1人、MAX15の詠唱中は本人以外の味方全員へ支援する。支援から支援を再発動せず、支援で本人のWTやリソースを消費しない。MAXは攻撃力のスナップショットを使用し、死亡・気絶・不在・対象部位の消失で中止する。

人物の概要、性格、口調、動画の観察時刻、元の意匠と本作の創作は[人物資料](../references/characters/shangrila.md)に保存。天使の羽と光輪を含む専用美術18点を生成し、無加工で採用した。生成指示は `game/art-source/plan10/shangrila-generation.json`。原動画と参考切り出し画像はゲームやGitへ同梱しない。

3章30ページ・18詩・5イベント40段落を追加した。[シャングリラの物語](../story-text/シャングリラの物語.txt)に全文を保存。衣装違いは同一人物の関係を共有し、育成・神器・読書進行は別に保持する。

[美術検証](plan10-shangrila-art-validation.json)、[実行検証](plan10-shangrila-acceptance.json)に採用ハッシュと画面を記録する。全4形態を含む最終実行ファイルは `game/Builds/plan10-shangrila/newASTER.exe`。個別段階のビルドは `game/Builds/plan10-shangrila/newASTER.exe`。

共通の最終検証はCore8,546項目、Unity C#185ソースが合格。`tools/validate-plan10-expanded-roster.ps1`、`Plan10ShangrilaBuild.ValidateAndBuild`、`tools/validate-plan10-shangrila-player.ps1`で再検証できる。画面採取は隔離した保存データを使う。

![ゲーム内検証](images/shangrila-game-comparison.jpg)
