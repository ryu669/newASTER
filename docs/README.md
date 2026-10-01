# 資料一覧

正式版の入口は[ゲーム要求仕様](game-requirements-v2.md)。古い企画・調査記録は根拠と検討履歴として残している。仕様書が存在することと、実装済みであることは別。

## 現行2D詳細仕様

現行要求は[ゲーム要求仕様 v0.3](game-requirements-v2.md)。以下は2D版の詳細仕様で、旧3D制作・旧ブラウザ試作の記述より優先します。実装修正はローカルで後日実施し、今回の更新は文書のみです。

| 文書 | 役割 |
| --- | --- |
| [2D表示要件](2d-illustration-spec.md) | 維持／2D化／廃止／TBDと全体方針 |
| [2D戦闘表示](spec-2d-battle-presentation.md) | レイヤー、部位選択、差分、イベントと演出時計 |
| [2D箱庭・交流・ADV](spec-2d-garden-and-adv.md) | 自由配置、家具利用、人物・CG、回想、保存 |
| [2D素材契約・受入れ](spec-2d-assets-and-acceptance.md) | 台帳、納品、差分整合性、欠落、品質ゲート |
| [戦闘・収集詳細](spec-battle-and-collection-v0.2.md) | 既存ルールを維持した2D表示接続 |
| [成長・経済・書UI詳細](spec-progression-ui-v0.2.md) | 育成・抽選・配置・ページ演出と保存 |

## 正式版

[速度とスキル待機と詠唱による行動順](battle-speed-casting.md)：最新の戦闘時間仕様。エキドナ動画とユーザー確認に基づき、自由順の固定ラウンドを置き換える。

[5人戦闘の動画検討と修正仕様](five-hero-battle-review.md)：ザクロ花嫁の参照動画、スキル別の自己・単体・複数・全体回復、行動人数、部位破壊通知。試遊版の最新支援仕様はこの文書を参照。

| 文書 | 役割 |
| --- | --- |
| [ゲーム要求仕様 v0.2](game-requirements-v2.md) | **最新・優先**。世界再生、巨神獣15体、詩、5人戦闘、育成、ガチャ、箱庭、万物の書UI、受入条件、TBD |
| [世界記憶・15巨神獣詳細仕様 v0.2](world-memory-colossi-v0.2.md) | 呪歌、万物の書、世界記述、過去7世界、初期15巨神獣、環境・箱庭の接続 |
| [戦闘・詩・ヒロイン詳細仕様 v0.2](spec-battle-and-collection-v0.2.md) | 巨神獣・部位・大技、5人戦闘、チェイン、13ジョブ、詩、好感度、データと試験 |
| [進行・経済・万物の書UI詳細仕様 v0.2](spec-progression-ui-v0.2.md) | テラフォーミング、箱庭、育成、装備の樹、オーパーツ、ガチャ、UI遷移、保存と試験 |
| [制作ギャップ分析・確定順序 v0.2](production-gap-analysis-v0.2.md) | 実装を止める不足項目、制作ゲート、検討の優先順位 |
| [縦切り詳細仕様 v0.2](vertical-slice-spec-v0.2.md) | 最初の1体・5人・詩・箱庭・成長を一周完成させる範囲と受入条件 |
| [正式版着手パッケージ v0.2](formal-production-start-v0.2.md) | 開始判定、技術構成、最初の10作業、初回ビルドの完了条件 |
| [正式版実装ロードマップ v0.2](implementation-roadmap-v0.2.md) | 現在地、実装順、各段階の完了条件 |
| [正式版制作：戦闘更新 v0.3](production-combat-update-v0.3.md) | 覚醒・重複強化の継承、5人の個別支援、巨神獣の段階変化・行動予告、正式データのTBD |
| [動画を参考にした戦闘UI改良](battle-video-ui-update.md) | 3D中心・右側3スキル・下部5人カード・部位選択の画面構成 |
| [3Dアセット制作方針 v0.2](asset-production-policy-v0.2.md) | 独自モデルの制作範囲、参考作品との区別、最初のモデル制作順 |
| [旧・要件定義](requirements.md) | 旧方針の統合記録。現行仕様と矛盾する場合はv0.2を優先 |
| [継承ヒロイン](inherited-heroines.md) | 指定7人、主人公、時系列、原作との関係 |
| [導入・加入構成案](continuity-opening.md) | 初期3人・加入順の提案、プレイヤー名 |
| [歌・好感度・シーン](songs-affection-scenes.md) | 収集、交流、回想、進行と保存 |
| [本制作着手仕様](production-ready-spec.md) | 戦闘時計、保存復旧、移植基準。旧人物案を含む |
| [2D素材仕様](production-assets.md) | 現行の2D詳細仕様への入口 |
| [本制作検討書](production-plan.md) | 方式・制作段階の検討履歴。旧6人案を含む |

## 旧ブラウザ試作の記録（廃棄対象）

以下は旧記録への参照であり、現行仕様・比較・移植基準には使いません。ファイル撤去は今回の文書更新に含めません。

- [起動・遊び方](../README.md)
- [v0.2完成記録](prototype-release.md)、[検証記録](prototype-validation.md)
- [戦闘仕様](battle-spec.md)、[誓いと救援](oath-rescue-prototype.md)
- [移植照合データ](../tests/fixtures/battle-parity-v1.json)

## 人物と参考作品の調査

- [7人の人物調査](heroine-research.md)、[巨神EDの観察](kyoshin-ending-observations.md)
- [戦闘録画の観察](battle-observations.md)
- [ANGELICA本体の分析](angelica-client-analysis.md)、[ジョブ演習](angelica-job-observations.md)、[ヘルプ追加確認](angelica-rules-followup.md)
- [参考資料と出典](references.md)
- 個人保全台帳・録画一覧・保全用ツールは手元専用としてGit管理外に保持。公開文書には個人の保存先を記載しない。

## 初期の検討履歴

- [初期企画](concept.md)、[方式比較・提案](proposal.md)、[ANGELICA要素の検討](angelica-elements.md)

## フォルダの役割

| 場所 | 内容 | Git管理 |
| --- | --- | --- |
| ルート・`src/` | ブラウザ試作と起動・梱包 | 対象 |
| `tests/` | 試作の回帰試験と移植基準 | 対象 |
| `docs/` | 要件・詳細設計・調査履歴 | 対象 |
| `tools/` | 移植照合。個人保全用ツールは手元のみ | 移植照合のみ対象 |
| `game/` | 将来の正式版3Dプロジェクト。現在は未作成 | ソースを対象、キャッシュは除外 |
| `dist/` | 試作の生成ZIP | 除外 |
| `.reference-analysis/` | 参考録画の確認画像 | 除外 |
| `.local-archive/` | 原作クライアント・プロフィール・録画等の個人保全 | 除外 |

起動・梱包・資料リンクを保つため、試作コードと既存資料のパスは維持する。正式版実装は将来`game/`へ分離し、試作は移植基準として保持する。過去資料に旧仕様が残る場合は、[ゲーム要求仕様 v0.2](game-requirements-v2.md)を優先する。

