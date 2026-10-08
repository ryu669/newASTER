# newASTER 文書索引

更新：2026-10-07。計画10は15形態・12人・13ジョブ、90章630詩・75交流。現行の実装と受入れは[計画10の実装記録](production/plan10-four-heroines-and-ui.md)へ集約する。

## 制作を進める

- [ヒロイン追加方法](production/heroine-addition-guide.md)：資料確認、人物・形態ID、戦闘、物語、18画像、接続、保存、検証の順序。
- [人物台帳](production/plan10-heroine-roster.json)と[計画10](production/plan10-implementation-plan.md)：制作順と提供済み形態。
- [人物資料](references/README.md)：動画の観察時刻、性格、口調、概要、元の意匠と本作の創作。
- [物語本文一覧](story-text/README.txt)：詩・章・交流の本文。
- [実装順と後続範囲](production/implementation-plan.md)：個別オーパーツ装備、人物追加ロットなど。

## 仕様の定義元

|領域|文書|担当する内容|
|---|---|---|
|要求|[ゲーム要求](requirements/game.md)、[2D表現要求](requirements/presentation.md)|ユーザー確定方針・対象範囲・体験|
|世界|[世界記憶](world/world-memory.md)|7世界・15巨神獣・解放と庭の接続|
|戦闘|[戦闘](systems/battle.md)、[速度と詠唱](systems/battle-timing.md)|部位・13ジョブ・通常予定列・独立チェイン・勝敗|
|進行|[詩と好感度](systems/collection-and-affection.md)、[育成と経済](systems/progression.md)|収集・章・親愛・恋人・成長・神器・遺物・抽選|
|拠点|[万物の書](systems/book-navigation.md)、[庭](systems/garden.md)、[ADV](systems/adv.md)|移動・配置・家具・交流・命令・既読|
|保存|[保存と移行](systems/save-and-migration.md)|原子性・失敗と再試行・旧保存の扱い|
|データ|[共通契約](data/contracts.md)|安定ID・型・JSON・座標・イベント・参照検証|
|表示|[戦闘表示](presentation/battle.md)、[庭とADV](presentation/garden-and-adv.md)|表示領域・レイヤー・人物・部位・CG|
|素材|[素材契約](presentation/assets.md)|原本・書き出し・採用・ハッシュ・差分の整合|
|検証|[受入試験](production/acceptance.md)、[UI検証記録](production/plan10-ui-validation.json)|受入条件・実在する結果・確認の方法と限界|

要求を優先し、動作はsystems、データ形はdata、表示はpresentationで定義する。別文書に同じ規則を複製しない。原作の観察、本作の設計、実装済みの結果を分ける。実装変更時は担当仕様と参照する検証を同時に更新する。本文と数量の詳細はランタイムJSON・人物台帳・採用記録へ対応づける。

## 計画10の人物別記録

- [R](production/plan10-r-implementation.md)
- [アナイアレイター通常／聖夜](production/plan10-annihilator-implementation.md)
- [シェル](production/plan10-shell-implementation.md)
- [オリフラム](production/plan10-oriflamme-implementation.md)
- [ナイトホーク](production/plan10-nighthawk-implementation.md)
- [スレイヤー水着](production/plan10-slayer-swim-implementation.md)
- [アルケイン](production/plan10-arcane-implementation.md)
- [アルケイン学園](production/plan10-arcane-academy-implementation.md)
- [シャングリラ](production/plan10-shangrila-implementation.md)

段階別の提供人数、テスト件数、ビルドと美術記録は、その段階の履歴。現在の数量へ一律に書き換えない。通常入口の最新数量は冒頭の計画10を参照する。

## 後続項目

1人1つのオーパーツ装備・能力適用・保存、200人超へ向けた小ロットの人物制作を継続する。現在の15形態の受入れで最終目標を完了扱いにしない。最終バランスや将来の量産規模は提供済みの数値と区別して調整する。

## 資料と履歴

[参考資料](references/README.md)は原作の観察・調査。[履歴](archive/README.md)は失効した仕様と過去の制作・配布候補の記録。現行の必須規則として自動採用しない。古い移動案内は削除し、リンクを実際の定義元へ直接向ける。[今回の文書整理](production/documentation-cleanup.md)に削除・更新範囲を記録した。

- [Plan11-2 箱庭生活の実装と検証](production/plan11-2-garden-life.md)
- [Plan11-2 提供仕様](production/plan11-2-garden-life-spec.md)
