# 実装順と完了条件

更新：2026-10-08。現行は[計画10](plan10-implementation-plan.md)。15形態・12人・13ジョブ、90章630詩・75交流を接続した。[今回の実装とUI](plan10-four-heroines-and-ui.md)と[人物台帳](plan10-heroine-roster.json)を現行の進捗とする。

## 次の実装

1. オーパーツは[Plan11-4](plan11-4-ooparts.md)で編成枠装備・5能力・特殊効果・原子的保存まで実装。数値・費用・入手率は最終バランス調整で確定する。
2. 人物追加：全13ジョブが揃ったため、資料を指定された小ロットごとに追加する。200人超は将来の目標で、未制作人物を提供人数へ数えない。
3. 物語拡張：人物資料と既存世界・他人物の関係を確認し、章・詩・交流・回想・読書状態へ接続する。

具体的な新規着手はユーザーの指定範囲で進める。この文書の後続一覧だけで次の機能実装を自動開始しない。

## 人物ロットの完了条件

[追加方法](heroine-addition-guide.md)に従い、観察と創作の区別、人物／形態ID、3スキルと固有ジョブ、成長、神器、18画像、3章30ページ・18詩・5交流、加入・編成・庭・ADV、保存互換と失敗再試行を揃える。画像比率と増員した一覧の最後の人物まで検証し、台帳をavailableへ変更する。

Core/DataとUnity C#、Windowsビルド、隔離したPlayer画面、原本と採用画像のSHA-256、本文・参照・ドキュメントリンクを確認する。別形態は人物関係を共有しても、成長・神器・読書進行を混同しない。基準値の最終バランスや物理操作の確認範囲は、結果と分けて記録する。

## 検証方針とGit運用

ユーザーの継続方針として追加性能測定は行わず、実操作・聴取は必要最小限。[計画8の検証範囲変更](plan8-verification-scope.md)を参照する。現行の主要検証は `tools/validate-plan10-expanded-roster.ps1`、`Plan10ShangrilaBuild.ValidateAndBuild`、Player検証スクリプト。

新規作業ブランチはfetchした最新mainから作成し、PRの宛先はmain。計画間のブランチへマージを積み重ねない。継続中のブランチはその差分とmainとの関係を確認する。今回のPRには計画10の追加と文書整理を含め、既存の無関係なUnity設定変更は含めない。元動画、調査画像、ビルド、保存データはGitへ同梱しない。

## 完了した計画の履歴

過去の工程・試験件数・当時の未完了項目は各記録を参照し、この文書で重複維持しない。

- [計画9の完了記録](plan9-completion-status.md)、[進捗](plan9-progress.md)、[詳細計画](plan9-implementation-plan.md)
- [計画8の完了ゲート](plan8-completion-gate.md)、[詳細計画](plan8-implementation-plan.md)
- [計画7の完了記録](plan7-completion-status.md)
- [計画6の完了記録](plan6-completion-gate.md)
- [計画5の完了ゲート](plan5-completion-gate.md)
- [計画3の完了ゲート](plan3-completion-gate.md)
