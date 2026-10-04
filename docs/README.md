# newASTER 仕様体系

更新：2026-10-05。現行ブランチ：`codex/plan9-production`。初期5人の正式配布候補RC1を実装・検証済み。[計画9の完了記録](production/plan9-completion-status.md)と[配布内容台帳](production/plan9-release-content-inventory.json)を参照。残り8ジョブと200人超の制作は[継続計画](production/post-plan9-jobs.md)。変更版はGitコミットで管理します。

## 読む順序と唯一の定義元

| 層 | 文書 | 定義するもの |
| --- | --- | --- |
| 要求 | [ゲーム要求](requirements/game.md) | 体験・対象範囲・数量・禁止事項・ユーザー確定方針 |
| 要求 | [2D表現要求](requirements/presentation.md) | 2D化の範囲・維持／廃止・必要な体験 |
| 世界 | [世界記憶](world/world-memory.md) | 世界法則・7世界・15巨神獣・解放順・環境 |
| システム | [戦闘](systems/battle.md) | 部位・ジョブ境界・独立自動チェイン・結果確定 |
| システム | [速度・詠唱](systems/battle-timing.md) | 通常予定列・待機・詠唱・予約対象 |
| システム | [詩・好感度](systems/collection-and-affection.md) | 収集・章・イベント条件・恋人進行 |
| システム | [育成・経済](systems/progression.md) | Lv・覚醒・装備・オーパーツ・キンダーガーデン |
| システム | [万物の書](systems/book-navigation.md) | しおり・対象・情報面・遷移・入力 |
| システム | [箱庭](systems/garden.md) | 配置・衝突・確定／取消・家具利用 |
| システム | [ADV](systems/adv.md) | 命令実行・行既読・読了・中断・回想 |
| システム | [保存](systems/save-and-migration.md) | 原子性・失敗・復旧・版移行 |
| データ | [共通データ契約](data/contracts.md) | 型・ID・座標・JSON・イベント・参照検証・乱数ログ |
| 表示 | [2D戦闘](presentation/battle.md) | 画面領域・レイヤー・部位差分・カットイン |
| 表示 | [箱庭・ADV表示](presentation/garden-and-adv.md) | 背景・人物・家具絵・CG・欠落時表示 |
| 素材 | [素材契約](presentation/assets.md) | 制作・書き出し・差分・採用・権利確認 |
| 制作 | [実装計画](production/implementation-plan.md) | 着手順・完了条件 |
| 制作 | [計画9の実装記録](production/plan9-progress.md) | 内容・全画面台帳、UI実装と検証結果、量産不足 |
| 制作 | [計画9の詳細計画](production/plan9-implementation-plan.md) | 正式量産・起動から全画面の豪華UI・保存互換・配布候補、9-1〜9-10の順序 |
| 制作 | [計画8の詳細計画](production/plan8-implementation-plan.md) | 通し試遊・計測・本文・難度・経済・操作・性能、8-1〜8-10の順序 |
| 制作 | [計画7の完了記録](production/plan7-completion-status.md) | 開発見本の改訂条件・検証範囲・量産／公開受入れへの引継ぎ |
| 制作 | [縦切り](production/vertical-slice.md) | 最初の完成範囲・測る内容 |
| 検証 | [受入試験](production/acceptance.md) | 試験ID・品質ゲート・性能記録 |
| 検証 | [プレイテスト](production/playtest.md) | 操作観察と計測手順 |

要求が詳細に優先します。領域内の動作はsystems、データ形はdata、見せ方はpresentationを定義元とし、他文書は参照にします。世界の解放・記憶元はworldを参照します。受入期待値が動作と矛盾する場合は定義元を確認し、実装者が独自の別規則を採用しません。

## 文書変更の規則

- 動作変更は担当systemsを変更し、影響するデータ型・表示・受入ケースを同じコミットで確認する。別文書に同じアルゴリズムをコピーしない。
- 要求の確定数量はrequirementsを定義元とする。詳細に数量を表示する場合は参照用であり、独立して変更しない。
- 暫定契約は担当文書に区分を明記する。ユーザー確定と暫定判断を混同しない。
- TBDには決める時点・影響する文書・検証項目を対応づける。未指定値を0や既存試作値へ黙って補完しない。
- JSON等の例は構造例と完成データを区別する。schemaVersionとcontentVersionを混同しない。
- 旧パスは移動案内として維持し、現行本文を二重保管しない。旧資料はreferences／archiveに区分する。

## TBDの管理

| 未確定項目 | 定義元 | 決める時点 |
| --- | --- | --- |
| チェイン率・補正の実戦調整、ヒロイン定義側の固定行動の威力／効果／自動対象 | systems/battle.md | 初期率で検証、人物固有行動は5人の完成見本前 |
| 正式速度式・スキル時間・状態効果 | systems/battle-timing.md | 個別戦闘データ作成前 |
| ジョブ資源・正式人物・固有スキル | systems/battle.md | 5人の完成見本前 |
| 人物別詩対応の具体ID・イベント条件と本文 | systems/collection-and-affection.md | 収集・ADV接続前 |
| 家具・人物方式・専用差分の量 | systems/garden.md、presentation/garden-and-adv.md | 箱庭完成見本前 |
| 素材量・抽選・オーパーツ各係数 | systems/progression.md | 経済検証前 |
| 現行保存形式・変換表・乱数実装の版 | systems/save-and-migration.md、data/contracts.md | ローカル変更前 |
| 画像解像度・圧縮・読込・メモリ予算・演出尺 | presentation | 実機品質・性能検証時 |

## 現行ではない資料

[参考資料一覧](references/README.md)は原作観察・調査、[旧資料一覧](archive/README.md)は旧仕様・3D制作・旧試作・過去実装記録です。どちらも現行の必須規則ではありません。旧ブラウザゲームは廃棄対象で、比較・移植基準として採用しません。今回は文書の整理だけを行います。

## 今回具体化した実装境界

条件式と依存グラフ、費用の合算・一括確定、ガチャの二段均等抽選、オーパーツ80%の整数判定、ADVの保存境界、万物の書の並びと空状態、チェイン判定ログのstep、定義パックの診断を具体化した。処理の定義元は各systems、型はdata、ケースはproduction/acceptanceに置く。

正式バランス、100ポイント交換の管理範囲・持越し・期限、キャラ固有行動とジョブ数値、現行セーブの具体移行表は未決のまま。確定済みの原作仕様と今回の本作向け処理契約を混同しない。今回の確定は詳細仕様に限り、既存実装・セーブファイルは変更しない。

編成順候補、行動不能者のスキップ、詠唱発動時判定、コマンド・資源・通常予定列との非干渉はユーザー確定済み。詳細はsystems/battle.md、型はdata/contracts.md、受入れはCH-01〜CH-07を参照する。

起点への戻り接続成功でフルチェインの追加一周を実行する仕様を確定。通常参加人数と追加行動回数を分離し、追加周回は一回のみ。systems/battle.md、data/contracts.md、FC-01〜FC-07を参照する。

詩の敗北・撤退取得、取得上限なし、再歌唱時の対応取得、ヒロイン側対応定義を確定。各終了理由で取得と章解放を原子的に保存する。PO-01〜PO-07を参照する。

ガチャ費用は1回300石／10回3000石、交換は100ポイントで専用ガチャチケット1枚。装備ノードの巨神獣素材はヒロインごとに定義する。1回1ポイント、チケットは対象キャラ100%の専用ガチャ用。ポイント管理範囲・期限と交換対象一覧は未決。GA-01〜GA-05、EQ-01〜EQ-02を参照する。

Lv育成はネクタル1種、ジョブ基準＋キャラ補正に確定。初期調整値として必要量・13ジョブ基礎値・Lv成長式をsystems/progression.mdへ定義。実戦バランスと配布量は検証後に調整する。LV-01〜LV-07を参照する。

覚醒・重複強化量・チェイン率はユーザーから調整を委任され、初期値と処理をsystems/progression.md、systems/battle.mdに設定した。人物固有チェイン行動はヒロイン定義側で決める。共通データと受入条件も対応させ、実戦未検証と区別する。
