# newASTER 仕様体系

更新：2026-10-02。現行ブランチ：`docs/kyoshin-requirements-v2`。このページを実装の入口とします。文書名の版番号を廃止し、変更版はGitコミットで管理します。実装・素材・セーブは今回変更しません。

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
| チェイン率・補正・固定行動の威力／効果／自動対象 | systems/battle.md | チェイン本実装前 |
| 正式速度式・スキル時間・状態効果 | systems/battle-timing.md | 個別戦闘データ作成前 |
| ジョブ資源・正式人物・固有スキル | systems/battle.md | 5人の完成見本前 |
| 詩対応・取得・イベント条件と本文 | systems/collection-and-affection.md | 収集・ADV接続前 |
| 家具・人物方式・専用差分の量 | systems/garden.md、presentation/garden-and-adv.md | 箱庭完成見本前 |
| 素材量・抽選・オーパーツ各係数 | systems/progression.md | 経済検証前 |
| 現行保存形式・変換表・乱数実装の版 | systems/save-and-migration.md、data/contracts.md | ローカル変更前 |
| 画像解像度・圧縮・読込・メモリ予算・演出尺 | presentation | 実機品質・性能検証時 |

## 現行ではない資料

[参考資料一覧](references/README.md)は原作観察・調査、[旧資料一覧](archive/README.md)は旧仕様・3D制作・旧試作・過去実装記録です。どちらも現行の必須規則ではありません。旧ブラウザゲームは廃棄対象で、比較・移植基準として採用しません。今回は文書の整理だけを行います。

## 今回具体化した実装境界

条件式と依存グラフ、費用の合算・一括確定、ガチャの二段均等抽選、オーパーツ80%の整数判定、ADVの保存境界、万物の書の並びと空状態、チェイン判定ログのstep、定義パックの診断を具体化した。処理の定義元は各systems、型はdata、ケースはproduction/acceptanceに置く。

正式バランス、詩取得の勝敗扱い、100回交換の持越し、キャラ固有行動とジョブ数値、現行セーブの具体移行表は未決のまま。確定済みの原作仕様と今回の本作向け処理契約を混同しない。今回の確定は詳細仕様に限り、既存実装・セーブファイルは変更しない。

編成順候補、行動不能者のスキップ、詠唱発動時判定、コマンド・資源・通常予定列との非干渉はユーザー確定済み。詳細はsystems/battle.md、型はdata/contracts.md、受入れはCH-01〜CH-07を参照する。

起点への戻り接続成功でフルチェインの追加一周を実行する仕様を確定。通常参加人数と追加行動回数を分離し、追加周回は一回のみ。systems/battle.md、data/contracts.md、FC-01〜FC-07を参照する。
