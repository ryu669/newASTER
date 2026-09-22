# 資料一覧

正式版の入口は[要件定義](requirements.md)。古い企画・調査記録は根拠と検討履歴として残している。仕様書が存在することと、実装済みであることは別。

## 正式版

| 文書 | 役割 |
| --- | --- |
| [要件定義](requirements.md) | 最新の対象範囲、必須機能、受入条件、未決事項 |
| [継承ヒロイン](inherited-heroines.md) | 指定7人、主人公、時系列、原作との関係 |
| [導入・加入構成案](continuity-opening.md) | 初期3人・加入順の提案、プレイヤー名 |
| [歌・好感度・シーン](songs-affection-scenes.md) | 収集、交流、回想、進行と保存 |
| [本制作着手仕様](production-ready-spec.md) | 戦闘時計、保存復旧、移植基準。旧人物案を含む |
| [3D素材仕様](production-assets.md) | モデル、演出、素材の工程。旧人物案を含む |
| [本制作検討書](production-plan.md) | 方式・制作段階の検討履歴。旧6人案を含む |

## 現在遊べるブラウザ試作

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

起動・梱包・資料リンクを保つため、試作コードと既存資料のパスは維持する。正式版実装は将来`game/`へ分離し、試作は移植基準として保持する。
