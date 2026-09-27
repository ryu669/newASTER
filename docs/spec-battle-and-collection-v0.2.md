# 詳細仕様：巨神獣戦・詩収集・ヒロイン進行 v0.2

親仕様は[ゲーム要求仕様 v0.2](game-requirements-v2.md)。本書はそのうち戦闘、詩、ヒロイン進行を実装可能な単位へ分解する。未定の数値は`TBD`で保持し、仮の数値をゲームデータへ混入させない。

## 1. 戦闘開始から確定まで

### 1.1 出撃前

1. プレイヤーは2Dマップまたは万物の書の巨神獣ページで、解放済みの巨神獣を選ぶ。
2. Lv1〜50を選択する。初回はLv1を初期選択する。前回選択Lvを記憶してもよいが、再召喚時に変更可能にする。
3. 所持ヒロインから異なる5人を編成する。5枠未満で開始可能にするかはTBD。開始可能なら空枠はチェイン参加者にならない。
4. 各人に装備の樹で選択中の武器とオーパーツを反映し、部位、報酬種別、Lv、Lv45以上の最強技警告を出撃前に表示する。
5. 出撃確定時に`battleId`、対象ID、選択Lv、編成スナップショット、乱数シード、開始時刻を作る。戦闘中に編成・装備・選択Lvを変更できない。

撤退は許可するが、勝利専用の討伐、ページ解放、詩、素材、テラフォーミング、交流権を付与しない。敗北も同様。リトライは新しい`battleId`で開始する。

### 1.2 戦闘状態

`Preparing → PlayerCommand → Resolving → EnemyAction → ResultPending → Victory | Defeat | Retreat`

- `Preparing`：対象、部位、各戦闘者の初期HP・リソース・パッシブを生成する。
- `PlayerCommand`：行動可能なヒロイン、選べる3スキル、対象、コスト、予測値、チェイン補正を表示する。未確認のジョブ固有操作をこの状態の外で自動処理にしない。
- `Resolving`：入力済みのスキルを、対象確認→コスト→命中／状態判定→ダメージ／回復→部位破壊→追加効果の順で一度だけ処理する。
- `EnemyAction`：生存部位を含む行動表から実行可能な行動を判定する。大技ゲージが最大なら大技を優先する。破壊済み部位に依存する行動は除外または個体定義の代替行動へ置換する。
- `ResultPending`：勝敗を確定した戦闘は以降の行動と報酬計算を停止する。勝利報酬はこの状態から一度だけ保存トランザクションへ渡す。

戦闘中の操作不能、画面遷移、再読み込みの復旧要件はTBD。ただし一つの`battleId`で勝利報酬を二重に確定してはならない。

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
           attributeId?, statusEffects[], partModifierId?, chainEligible }
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

### 4.1 補正の抽選

各プレイヤーターン開始時、当該ターンに行動可能なヒロインのスキルへ`ChainModifier`を生成し、確定前から表示する。

```text
ChainModifier { source: "turn-random", targetHeroineId, skillId?, additiveRate, stackIndex }
```

- 基本チェイン率、`CHAIN +10%`の付与確率、`+5%`の付与確率はTBD。
- `+5%`は一ターンで最大2ヒロインへ累積付与できる。対象が重複可能か、スキル単位かヒロイン単位かはTBDだが、採用した規則を表示する。
- 補正はターン終了時に消える。所持ヒロイン、武器、オーパーツ、好感度、家具、ガチャ重複、編成順による永続チェイン率補正は禁止する。

### 4.2 解決規則

1. 起点スキルを解決する。
2. 起点が攻撃で、戦闘が継続し、チェイン回数が5未満なら、表示済みの発生率で一回抽選する。
3. 成功時、未行動かつ戦闘不能でない次のヒロインを、編成順と当該ターンの行動規則に従って選ぶ。選出規則は実装前に固定し、ランダムならシードへ記録する。
4. 次のスキル選択と対象選択を処理する。敵撃破、味方全滅、行動不能、チェイン不適格で終了する。
5. 各連鎖の開始・成功・失敗・終了理由を戦闘ログに記録する。

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
- 勝利以外で巨神獣詩・対応詩を確定するかはTBD。決定後は結果画面・図鑑・試験で同じ条件を示す。

## 6. 好感度・イベント

```text
AffectionState { heroineId, value, unlockedEventIds, readEventIds, loverStatus }
EventDef { id, heroineId, kind: affinity|lover, prerequisiteIds[], affectionRequirement?, scriptId }
```

- 1人につき好感度イベント3本、恋人後ラブラブ特別イベント2本。
- 好感度はヒロイン別。原作からの関係を消去する初対面値には使わない。
- イベントは条件達成で`unlocked`、最後まで読んで`read`とする。途中終了、回想、既読スキップで二重の好感度・報酬を発生させない。
- 恋人化は明示イベントの読了でのみ`loverStatus=true`へ遷移させる。好感度数値だけで自動遷移するかはTBD。
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
