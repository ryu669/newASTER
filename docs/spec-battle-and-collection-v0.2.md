# 詳細仕様：巨神獣戦・詩収集・ヒロイン進行 v0.3（2D対応）

親仕様は[ゲーム要求仕様 v0.3](game-requirements-v2.md)。本書はそのうち戦闘、詩、ヒロイン進行を実装可能な単位へ分解する。未定の数値は`TBD`で保持し、仮の数値をゲームデータへ混入させない。

行動順・スキル使用後待機・ブラスター詠唱の最新版は[速度とスキル待機と詠唱](battle-speed-casting.md)。味方全員1回の固定ラウンドや自由順ではなく、速度とスキル時間で決まる。以下の旧ターン表現と衝突する場合、この追補を優先する。仮の時間係数は試遊データに限り、正式値はTBD。

更新：2026-10-02。表示イベント、部位レイヤー、差分・カットイン、演出時計と短縮／復帰は[2D戦闘表示詳細](spec-2d-battle-presentation.md)を適用する。画像や演出コールバックから判定・乱数・詩・報酬を更新しない。実装修正は後日ローカルで実施する。

## 1. 戦闘開始から確定まで

### 1.1 出撃前

1. プレイヤーは2Dマップまたは万物の書の巨神獣ページで、解放済みの巨神獣を選ぶ。
2. Lv1〜50を選択する。初回はLv1を初期選択する。前回選択Lvを記憶してもよいが、再召喚時に変更可能にする。
3. 所持ヒロインから異なる5人を編成する。5枠未満で開始可能にするかはTBD。開始可能なら空枠はチェイン参加者にならない。
4. 各人に装備の樹で選択中の武器とオーパーツを反映し、部位、報酬種別、Lv、Lv45以上の最強技警告を出撃前に表示する。
5. 出撃確定時に`battleId`、対象ID、選択Lv、編成スナップショット、乱数シード、開始時刻を作る。戦闘中に編成・装備・選択Lvを変更できない。

撤退は許可するが、勝利専用の討伐、ページ解放、詩、素材、テラフォーミング、交流権を付与しない。敗北も同様。リトライは新しい`battleId`で開始する。

### 1.2 戦闘状態

`Preparing → ScheduledEvent → PlayerCommand | CastingResolve | EnemyAction → ScheduledEvent → ResultPending → Closed`。結果理由はvictory／defeat／retreatとして別管理する。

- `Preparing`：対象、部位、各戦闘者の初期HP・リソース・パッシブを生成する。
- `PlayerCommand`：行動可能なヒロイン、選べる3スキル、対象、コスト、予測値、チェイン補正を表示する。未確認のジョブ固有操作をこの状態の外で自動処理にしない。
- `Resolving`：入力済みのスキルを、対象確認→コスト→命中／状態判定→ダメージ／回復→部位破壊→追加効果の順で一度だけ処理する。
- `EnemyAction`：生存部位を含む行動表から実行可能な行動を判定する。大技ゲージが最大なら大技を優先する。破壊済み部位に依存する行動は除外または個体定義の代替行動へ置換する。
- `ResultPending`：勝敗を確定した戦闘は以降の行動と報酬計算を停止する。勝利報酬はこの状態から一度だけ保存トランザクションへ渡す。

休止・結果確定・再起動の境界は[実装契約](spec-implementation-contracts.md)3章を適用する。戦闘途中再開は初回未対応。一つのbattleIdで報酬を二重確定しない。

## 2. 巨神獣・部位・Lv

### 2.1 定義データ

```text
ColossusDef {
  id, displayName, worldLineId, pageOrder, unlockAfterId,
  baseStats, levelScalingRuleId, parts[4..6], actionTableId,
  ultimateSkillId, ultimateAvailableLevel: 45,
  poemChapters[3], rewardTableId, terraformingRuleId
}
PartDef {
  id, displayName, maxHpRuleId, targetable, breakEffectId,
  enabledActions[], disabledActions[], replacementActions[]
}
```

- `pageOrder`は万物の書・2Dマップ双方の順序であり、解放条件は直前ページの一度以上の勝利で判定する。
- `parts`は本体を除く4〜6部位。部位HPが0になったら`broken=true`に固定し、同じ部位の破壊効果を再発火させない。
- 本体HPが0なら勝利。部位をすべて破壊しても本体HPが残る場合は勝利にしない。
- `levelScalingRuleId`は数式をデータとして参照する。HP、攻撃、防御、状態耐性、報酬、テラフォーミングがLvでどう変わるかは個体別に明示する。Lv45〜50だけ最強技可否を追加する。

### 2.2 大技ゲージ

巨神獣の大技は`current / max`で画面に常時表示する。行動ごとの増加量、最大時の消費量、満タン時に即発か次行動時発動かは`actionTableId`に記録する。大技を止める部位、必要な破壊タイミング、破壊後のゲージ処理は個体ごとに`breakEffectId`で定義する。最強技は`selectedLevel >= 45`で候補に加える。Lv45未満には最強技用の予兆を表示しない。

## 3. ヒロイン、スキル、ジョブ

### 3.1 定義データ

```text
HeroineDef { id, name, jobId, baseRarity: 6, skills[3], traitId, weaponTreeId,
             poemChapters[3], affinityEventIds[3], loverEventIds[2] }
SkillDef { id, ownerId, targetRuleId, costRuleId, powerRuleId, waitRuleId,
           castRuleId, attributeId?, statusEffects[], partModifierId?, chainEligible }
JobDef { id, resourceType, resourceInitRuleId, resourceGainRules[], resourceSpendRules[], commandRules[] }
```

- 一人のヒロインには必ず3つの固有スキルを持たせる。ヒロイン個別のスキルは、ジョブ名だけで自動生成しない。
- `chainEligible=false`のスキルを置く必要が出た場合は、理由とUI文言を仕様化する。現時点では全スキルの可否はTBD。
- ヒロイン特性、武器、オーパーツは威力、属性、状態異常、資源、耐性等を変えられるが、**チェイン発生率を直接・間接に増やしてはならない**。データ検証で`chainRate`を含む効果種別を拒否する。

### 3.2 ジョブの実装境界

13ジョブ全員に、他ジョブと区別できる`resourceType`または`commandRules`を必須とする。現時点で確認済みのスレイヤー（ファイター系）だけは以下を設計要件とする。

| ジョブ | 確定するUI・状態 | TBDとして残すもの |
| --- | --- | --- |
| スレイヤー | ブースト球表示、通常／捨て身モード、球の保持・消費選択、ラストリゾートゲージと発動予告 | 球数上限・増加式、モード補正、赤球、ラストリゾートの最終性能、全スキルの球消費 |
| 残り12ジョブ | ジョブ固有リソースまたはルールを持つこと | 個別リソース名、UI、取得・消費、Auto、数値。原作確認後に別紙で確定 |

原作演習のスレイヤーにおける速度+50%・クリティカル+50%・3ターンは記録値であり、`JobDef`の初期値として固定してはならない。

## 4. チェイン詳細

[実装契約](spec-implementation-contracts.md)2章を適用する。固定ラウンドの「未行動者へ即座に連鎖」は廃止し、速度順の連続即時攻撃へ接続する暫定方式を使用する。最大5人、同じ人物の再登場でリセット、敵／支援／パス／詠唱イベントで終了。割込みや追加行動は発生させない。

補正はコマンド受付一回で一度だけ生成・表示し、確定時に失効する。基本率・付与率はTBD、保存と表示はbp整数。人物や装備による永続／間接の率強化は禁止する。候補・乱数・資源は無効入力や再描画で変えない。

## 5. 詩・章・戦闘終了時の収集

### 5.1 データと状態

```text
PoemDef { id, ownerType: colossus|heroine, ownerId, chapter: 1..3, textId, sourceRuleId }
StoryChapterDef { id, ownerType, ownerId, chapter: 1..3, requiredPoemIds[], scriptId }
PlayerProgress { collectedPoemIds, unlockedStoryIds, readStoryIds }
```

- 巨神獣は各章8詩、計24詩。各ヒロインは各章6詩、計18詩。
- 一つの詩は一つの章にだけ属する。詩本文は当該章の本文から引用するため、`textId`はシナリオ本文と対応づけて制作する。
- `collectedPoemIds`は集合で保持する。同じ詩が再抽選されても通知・収集数・章解放を重複させない。
- 必要詩すべてを取得した時だけ`unlockedStoryIds`へ追加する。読むまでは`readStoryIds`に追加しない。

### 5.2 収集処理

巨神獣が詩を歌う行動を完了すると、未所持候補から抽選した`PoemDef`を一時戦闘報酬へ積む。候補がすべて既取得なら進行用の新規詩を付与しない。抽選対象、重複候補の扱い、戦闘あたり上限は個体別仕様でTBDとする。

戦闘終了時、編成にいたヒロインごとに、その戦闘で聞いた巨神獣詩との対応を検索し、対応ヒロイン詩を解放する。以下を必須とする。

- 対応は`ColossusPoemId × HeroineId → HeroinePoemId`の明示データで持ち、名前や章番号だけで推測しない。
- 解放は戦闘終了後の結果画面でまとめて表示する。
- 好感度、好感度イベント、ガチャ排出、装備素材と同じフラグを使わない。
- 初回の暫定実装契約は勝利時のみ巨神獣詩・対応詩を確定する。敗北・撤退では一時取得を破棄する。正式判断はTBDで、変更時は結果画面・図鑑・試験・保存を同時改訂する。

## 6. 好感度・イベント

```text
AffectionState { heroineId, value, unlockedEventIds, readEventIds, loverStatus }
EventDef { id, heroineId, kind: affinity|lover, prerequisiteIds[], affectionRequirement?, scriptId }
```

- 1人につき好感度イベント3本、恋人後ラブラブ特別イベント2本。
- 好感度はヒロイン別。原作からの関係を消去する初対面値には使わない。
- イベントは条件達成で`unlocked`、最後まで読んで`read`とする。途中終了、回想、既読スキップで二重の好感度・報酬を発生させない。
- 初回の暫定実装契約では明示イベントの読了でのみ`loverStatus=true`へ遷移させ、好感度数値だけでは自動遷移させない。正式条件はTBDで、変更時はイベント定義と保存試験を改訂する。
- 物語本編は好感度イベント読了を逆要求しない。イベントの物語章要求は許可するが、循環依存をデータ検証で拒否する。

## 7. 戦闘・収集の受入試験

| ID | 前提 | 操作 | 期待結果 |
| --- | --- | --- | --- |
| B-01 | 未討伐のページn | Lv1で勝利 | ページnの討伐済みと次ページだけを解放 |
| B-02 | 討伐済み、Lv44 | Lv44で開始 | 最強技が行動候補・予兆に現れない |
| B-03 | 討伐済み、Lv45 | Lv45で開始 | 最強技が個体定義どおり候補に現れる |
| B-04 | 破壊可能部位 | 条件を満たして部位HPを0にする | 一度だけ破壊効果が反映され、関連大技・行動が定義どおり変化 |
| B-05 | 同じシード | 同じ入力列を2回実行 | チェイン、詩、報酬を含む結果が一致 |
| B-06 | 全ヒロイン効果データ | 検証を実行 | `chainRate`を変える人物・装備・特性が0件 |
| B-07 | 章の詩を7/8または5/6取得 | 最後の1詩を取得 | 該当章のみ解放し、好感度・他章は変化しない |
| B-08 | 恋人イベント未解放 | 回想・中断を繰返す | `loverStatus`、好感度、初回報酬が不正に増えない |


## 8. 2D表示との接続

戦闘スナップショットは本体HP、大技ゲージ、各部位IDとHP／破壊状態、各人物IDとHP／資源／詠唱、次行動、選択対象を含む。確定イベントはbattleIdと一戦内のsequenceで識別する。表示層は[2D戦闘表示詳細](spec-2d-battle-presentation.md)の契約へ変換し、描画の終了時に勝利を追加確定しない。

部位選択、予約済み攻撃の対象変更、勝敗確定は戦闘側で処理する。2Dのクリック領域や描画順をダメージ計算へ使わない。詩通知を省略しても戦闘終了時の収集結果は同じで、物語に使う一文はオリジナル本文と対応する。

通常／短縮／スキップ／休止復帰で同じ入力・乱数種の戦闘状態、詩、報酬を照合する。画像欠落でもID・進行を壊さない。旧ブラウザ試作のテスト・移植fixtureへ一致させる要求は廃止する。


## 9. 横断データ契約

[実装契約](spec-implementation-contracts.md)がチェイン・結果再生・座標・VisualEventの型と検証を定義する。基本戦闘数量と詩の数量は変更しない。
