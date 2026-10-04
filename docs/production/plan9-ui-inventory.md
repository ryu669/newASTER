# 計画9：全画面台帳

更新：2026-10-04。各画面は9-2で共通見本に揃え、9-7で全適用を完了する。画像確認と機能診断を分け、未確認を合格にしない。

| ID | 画面群 | 現行の主実装 | 状態 | 依存／完了条件 |
| --- | --- | --- | --- | --- |
| UI-01 | 起動・読込 | PrototypeBootstrap.Awake / FormalEntranceView | 導入・失敗画面を接続、描画診断済み | 素材・保存読込、失敗、設定反映、P9-02 |
| UI-02 | タイトル | FormalTitleView | 接続・描画検証中 | 正式書導線、設定、クレジット、終了、P9-02 |
| UI-03 | 導入・ホーム | HomeExperience | 機能済み・正式美術待ち | 初回導入、通知、P9-03 |
| UI-04 | 万物の書・図鑑 | BookExperience / PrototypeBootstrap.DrawBook | 機能済み・共通枠刷新待ち | 分類／対象／面、遷移、P9-04 |
| UI-05 | 出撃・編成 | DrawColossus / StartBattle | 機能済み・量産待ち | 全敵定義とLv、P9-05 |
| UI-06 | 戦闘 | BattleIllustrationView / BattleMenuExperience | 見本済み・量産待ち | 5人15体の美術と予兆、P9-05 |
| UI-07 | 結果・保存待ち | FormalVictoryView | 共通枠見本あり・正式本文待ち | 全終了理由、保存原子性、P9-08 |
| UI-08 | 人物・育成 | GrowthExperience | 共通枠見本・アイコノクラスト肖像候補を接続。3人美術と全員の採用待ち | 操作／確認／結果、P9-07 |
| UI-09 | 装備の樹・遺物 | HomeExperience / CollectionExperience | 機能済み・正式表示待ち | 固有樹、比較、80%境界、P9-07 |
| UI-10 | ガチャ・交換・恵み | KinderExperience / EngagementExperience | 機能済み・正式素材待ち | 率・費用・保存、P9-07 |
| UI-11 | 箱庭・家具 | GardenMenuExperience / GardenArtComposition | 見本済み・全環境待ち | 配置、制作、利用、P9-07 |
| UI-12 | 交流・ADV・詩・回想 | AdvExperience / Plan8StoryExperience | 機能済み・正式本文待ち | 60章25イベント、P9-06 |
| UI-13 | 設定・ヘルプ・クレジット | FormalTitleView / DrawHelp / DrawArtSettings | タイトル設定の適用／取消、説明を接続・検証。他画面統一と最終採用待ち | 永続設定、操作説明、採用素材権利、P9-03 |
| UI-14 | 復旧・エラー | SaveRecoveryView / FormalVictoryView | 機能済み・共通枠確認待ち | 原本保全、未知版停止、P9-08 |

全画面で720p／1080p、通常／無効／不足／空／未解放／確認／保存待ち／保存失敗の該当状態を自動描画で検査する。対象外状態は理由を記録する。実操作は最大5操作、性能測定は実施しない。
