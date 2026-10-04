# 計画9：全画面台帳

更新：2026-10-05。初期5人RC1の14画面群を正式入口へ接続。最終42ケースを720p／1080pで描画し84画像を個別に確認済み。[画像・ログ・hash](plan9-final-runtime-validation.json)と[完了記録](plan9-completion-status.md)。

| ID | 画面群 | 主実装 | 最終確認ケース・内容 |
| --- | --- | --- | --- |
| UI-01 | 起動・読込 | FormalEntranceView | intro、startup-error：導入と読込失敗 |
| UI-02 | タイトル | FormalTitleView | title、exit：正式入口・終了確認・開発入口非表示 |
| UI-03 | 導入・ホーム | ProductionStoryExperience | title、book、story-book：書と物語への通常導線 |
| UI-04 | 万物の書・図鑑 | BookExperience | book、story-book：金装飾・分類・章進行 |
| UI-05 | 出撃・編成 | DrawColossus / StartBattle | battle：初期5人と固有敵。各15体のLv／部位診断も継承 |
| UI-06 | 戦闘 | BattleIllustrationView / BattleMenuExperience | battle：本体・独立部位・人物・予兆。全15体の各32描画の制作審査も継承 |
| UI-07 | 結果・保存待ち | FormalVictoryView | victory／pending／retry：保存完了・待ち・再試行・正式章の案内 |
| UI-08 | 人物・育成 | GrowthExperience | story-economy.growth：肖像・育成。5人の個別素材審査も継承 |
| UI-09 | 装備の樹・遺物 | HomeExperience / CollectionExperience | tree、relics：固有樹・80%境界・装備／強化 |
| UI-10 | ガチャ・交換・恵み | KinderExperience / EngagementExperience | engagement、kinder-entrance／draw／rates／exchange／tickets／confirm／result：率・費用・対象・結果 |
| UI-11 | 箱庭・家具 | GardenMenuExperience / GardenArtComposition | story-garden.0／8／furniture／residents／placement／confirmation／audio：家具・5住人・配置・確定 |
| UI-12 | 交流・ADV・詩・回想 | AdvExperience / ProductionStoryExperience | story-chapter、初期5人のstory-event：本文・CG・回想。全25交流は自動読了／再読の契約検査 |
| UI-13 | 設定・ヘルプ・クレジット | FormalTitleView / DrawHelp | settings／cancel／large、help、credits：取消・大文字・音量・速度・操作説明・権利 |
| UI-14 | 復旧・エラー | SaveRecoveryView | recovery／confirm／blocked：復旧確認・原本保護・未知版停止 |

通常／無効／確認／保存待ち／保存失敗の代表表示を含む。全状態の組合せを人が操作した保証ではない。不足・未解放・空・衝突・原子保存はCore／Unity契約検査と各制作記録で補う。実入力はエージェント5操作、聴取0秒、追加性能測定なし。
