# 共通データ・JSON交換形式・イベント・検証

更新：2026-10-04。状態：現行仕様。版はGit履歴で管理し、ファイル名に版番号を付けません。仕様整理のみで実装完了を意味しません。

## 計画8の基準定義と診断境界

`TrialDefinition`（schemaVersion=1、status=development-trial）は`Resources/Trial/plan8-baseline.json`から読む。id、contentVersion、colossusId、heroineIds、敵Lv範囲と最強技境界、本文制作目標、definitions、measurementsを持つ。definitionsはrole／resourcePathの4参照、measurementsは時間・戦闘・収集・経済・庭・読書・導線・保存・音・性能の10分類。採用用途、対象と編成順、範囲、参照集合、欠落／重複を検査する。制作目標の件数は本文制作済みの証明ではなく、正式コンテンツの公開ゲートを解除しない。

`TrialDiagnosticBoundary`は完全修飾のrepositoryRoot／normalSaveRootと安全なrunIdから、tmp内の診断ディレクトリ・SavePath・run固有identityを導出する。通常保存との重複、相対パス、トラバーサル、Windows予約名、既存run、junction／symlinkを拒否する。この操作はファイルを作成・読込せず、通常のFormalCampaignSave.Identityを変更しない。診断identityはrunの名前空間であり、通常payloadのsaveIdへ代入するものではない。実保存・codec接続は計画8の後続でこの境界と通常payloadの契約を適用して検証する。

基準診断は通常の保存store初期化より前に終了する。`-validatePlan8Baseline`、`-plan8RepositoryRoot`、`-plan8RunId`を使用し、storeOpened=False／saveWritten=Falseと本文目標未制作を記録する。計測レコードの型は8-2で別途追加する。[実装記録](../production/plan8-progress.md)を参照。

## 共通データ型と座標

| 型 | 定義・検証 |
| --- | --- |
| Id | 1〜96文字、ASCII英小文字・数字・`.`・`-`・`_`。種別内一意、表示名から生成しない |
| Tick／sequence | 非負整数、JSONで厳密に扱うため0〜9007199254740991。sequenceは1以上 |
| ProbabilityBp | 0〜10000の整数、UIはbp/100で%表示 |
| Point01 | `{x,y}`有限数、原点左上、右と下が正、0〜1 |
| Rect01 | `{x,y,width,height}`有限数、幅高さ>0、全体が0〜1内 |
| positiveSize01 | `{x,y}`有限数、各0より大きく1以下。Point01の位置ではなく幅高さ |
| Scale2 | `{x,y}`有限数>0。負のscaleによる無断左右反転は禁止 |
| drawOrder／inputPriority | 整数。drawOrderが大きいほど前、inputPriorityが大きいほど入力優先 |
| status | `placeholder | review | accepted | retired`。制作研究用`study`も素材台帳で許可 |

戦闘レイアウトは1920×1080の設計矩形を基準とし、対象領域の正規化座標を画面へ変換する。非16:9は比率を保ってletterbox表示。余白をクリック対象へ含めない。解像度変更・UI文字サイズ変更でIDと進行を変えない。

pivotは画像内左上原点0〜1。anchorは親領域内のPoint01。描画サイズは`size01 × 親領域サイズ × scale`。部位画像はボスの親領域を基準にする。選択領域も同じ親領域を参照し、異なる座標系を混ぜない。

## 2D定義形式

UTF-8 JSONを交換形式とし、Unityの実行用クラス／ScriptableObjectへの変換方式は実装者が選択する。キー名と意味、検証結果は同じにする。未知schemaVersionを黙って読まない。

```json
{
  "schemaVersion": 1,
  "status": "placeholder",
  "colossusId": "colossus.green-return-dragon",
  "layoutId": "layout.battle.wide",
  "bodyAssetId": "art.dragon.body",
  "parts": [
    {
      "partId": "crystal-horn-crown",
      "normalAssetId": "art.dragon.horn.normal",
      "brokenAssetId": "art.dragon.horn.broken",
      "hideWhenBroken": false,
      "anchor": {"x": 0.5, "y": 0.2},
      "pivot": {"x": 0.5, "y": 0.5},
      "size01": {"x": 0.2, "y": 0.2},
      "scale": {"x": 1, "y": 1},
      "drawOrder": 20,
      "hitRegionId": "hit.dragon.horn"
    }
  ]
}
```

上記は一部位の構造例で**完成データではない**。読み込み可能な個体は本体と4〜6部位を必須とする。`size01`は正の幅高さとして扱い0〜1、anchorとpivotはPoint01。領域からはみ出す描画は個体の演出用clip規則で制御し、選択領域の範囲外は拒否する。

```text
HitRegion { id:Id, parentRegionId:Id, shape:"rect", rect:Rect01, inputPriority:int }
ArtManifest { schemaVersion:1, assets:ArtAsset[], canvasGroups:CanvasGroup[] }
ArtAsset { assetId:Id, path:string, usage:string, status, required:bool,
           width:positiveInt, height:positiveInt, pivot:Point01, canvasGroupId?:Id,
           author:string, provenance:string, rightsNote:string,
           ownerId?:Id,createdDate?:string,sourcePath?:string,usageNote?:string,
           colorSpace:"sRGB",alphaMode:"opaque"|"rgba" }
CanvasGroup { id:Id, width:positiveInt, height:positiveInt, pivot:Point01 }
```

初回hitRegionは矩形のみ。多角形は後続schemaVersionで追加する。素材pathはプロジェクト素材ルート相対で`..`と絶対パスを拒否、対応拡張子はPNG。width／heightはファイルから測定した値と一致させる。差分canvasGroupは寸法・pivotの共通値を持つ。RGBA/sRGB等の書き出し条件は素材詳細を適用する。

## 表示イベント契約

```text
VisualEvent {
 battleId:Id, sequence:positiveInt, tick:Tick,
 kind:attack|heal|support|pass|castStart|castResolve|castCanceled|enemy|poem|battleEnded|chainCheck|chainAction|chainEnded,
 chainId?:Id, chainStep?:positiveInt, chainActionId?:Id,
 major:bool,
 actorId?:Id, targetIds:Id[], skillId?:Id, presentationId?:Id,
 preSnapshot:BattleSnapshot, postSnapshot:BattleSnapshot,
 effects:EffectResult[], poemIds:Id[], resultReason?:ResultReason
}
BattleSnapshot {
 tick:Tick, bodyHp:nonnegativeInt, bodyMaxHp:positiveInt,
 gauge:nonnegativeInt, gaugeMax:positiveInt,
 parts:[{partId:Id,hp:nonnegativeInt,maxHp:positiveInt,broken:bool}],
 heroes:[{heroineId:Id,hp:nonnegativeInt,maxHp:positiveInt,resource:nonnegativeInt,
          casting:bool,castAt?:Tick,statusIds:Id[]}],
 nextEvents:[{actorId:Id,kind:command|castResolve|enemy,at:Tick}],
 chain:{length:0..5,participantIds:Id[]}
}
EffectResult { type:damage|heal|statusAdded|statusRemoved|partBroken|gaugeChanged,
               targetId:Id, amount?:nonnegativeInt, delta?:int, statusId?:Id }
```

人物は編成5人、部位は本体を除く4〜6。最大HP・ゲージの範囲外とID重複を拒否する。castAtはcasting=trueで必須、falseなら省略。damage／healはamount必須、status系はstatusId必須。gaugeChangedは符号付きdelta必須（減少を負値で表す）、partBrokenは対象部位ID必須。資源が将来複数種類になる場合はバージョン付きjobResourceオブジェクトへ拡張し、初回単一検証資源と混在させない。

既存のMajorフラグはmajorへ対応し、敵／人物の大技カットイン可否を示す。詩イベントはpoemIdsが1件以上、battleEndedだけresultReason必須。

イベントのbefore／afterをヒット時点で切り替える。次行動列はpostSnapshotの計算済み情報と明示し、過去HPから未来を再計算しない。同一tickの複数イベントはsequence順。actorId欠落はpoem／battleEndedのみ許可し、敵actorIdも安定IDを持つ。既存コアのindex配列は戦闘開始時のID対応表を通して変換する。

effectの差分はpost-preと一致し、破壊はhp=0、結果終了は最終sequence。再描画で効果音や通知を再発火させない。通知／ヒットのmarkerMsとdurationMsは非負整数、marker<=duration、通常短縮それぞれで定義する。未定の尺はplaceholderデータで試験し正式採用とは扱わない。

### 乱数と再現ログ

戦闘開始時に`rngAlgorithmId`、`rngVersion`、`seed`を記録し、コアの既存アルゴリズムを識別する。既存と違う方式へ無断に置換しない。抽選ログは`battleId / decisionId / sequence / purpose / candidateIds / drawIndex / sampledValue / result`を持つ。purposeはchainModifier／chainConnect／poem／rewardを区別する。同じ入力と版でdrawIndexが一致すること。表示層は乱数APIを呼ばない。具体アルゴリズム名は既存コード確認時に固定し、未指定の正式データを受け入れない。

## 表示定義の追加型

```text
BattleArtDef { schemaVersion:1,status,colossusId:Id,backgroundSetId:Id,
               bodyAssetId:Id,parts:PartArtDef[4..6],layoutId:Id }
PartArtDef { partId:Id,normalAssetId:Id,brokenAssetId?:Id,hideWhenBroken:bool,
             anchor:Point01,pivot:Point01,size01:positiveSize01,scale:Scale2,
             drawOrder:int,hitRegionId:Id }
HeroBattleArtDef { heroineId:Id,idleAssetId:Id,attackAssetId:Id,hitAssetId:Id,
                  castingAssetId?:Id,skillArtBySkillId:map<Id,Id>,cutinAssetId?:Id }
GardenArtDef { gardenId:Id,backgroundAssetId:Id,layoutId:Id,occlusionMaskAssetIds:Id[] }
AdvSceneDef { sceneId:Id,scriptId:Id,layoutId:Id,backgroundAssetId:Id,
              cast:ActorDisplay[],cgAssetIds:Id[],audioCueIds:Id[] }
ActorDisplay { heroineId:Id,positionSlotId:Id,outfitId:Id,expressionId:Id,poseId:Id }
```

hideWhenBroken=falseではbrokenAssetId必須。戦闘部位IDは個体定義と一対一。ヒロインの固有チェイン行動の絵はChainActionDef.presentationIdから解決し、通常スキルの選択UIへ変換しない。layoutIdはGardenLayout／AdvLayout等の用途別定義を参照する。アセット一覧と一致する参照だけを受け入れる。

現行Unityの`BattleIllustrationManifest` v1は部位に任意の`placement:{enabled:bool,x:float,y:float,scale:float}`を持つ。未指定またはenabled=falseは共有キャンバス全域を使用する。enabled=trueでは有限のx／y≥0、scale>0、x+scale≤1、y+scale≤1を必須とする。一様倍率で正方形・縦横比を保ち、同寸法の元PNGを移動・縮小表示する。通常／破壊で同じ配置を使い、選択用x／y／width／heightやHP・コアを変更しない。[角冠の実装・互換検証](../production/plan7-crown.md)を参照。

## 戦闘・チェイン定義型

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
```text
HeroineDef { id, name, jobId, baseRarity: 6, skills[3], traitId, weaponTreeId, chainActionId,
             poemChapters[3], poemLinks:HeroinePoemLink[], affinityEventIds[3], loverEventIds[2] }
SkillDef { id, ownerId, targetRuleId, costRuleId, powerRuleId, waitRuleId,
           castRuleId, attributeId?, statusEffects[], partModifierId?, chainEligible }
JobDef { id, resourceType, resourceInitRuleId, resourceGainRules[], resourceSpendRules[], commandRules[] }
```
```text
ChainState {
 chainId:Id, mode:"automatic-fixed-action-v1",
 originActorId:Id, originActionId:Id,
 phase:checking|executing|fullChainExecuting|ended,
 length:1..5, participantIds:unique[],
 candidateId?:Id, fixedActionId?:Id, modifierBatchId:Id,
 endReason?:failedRoll|noCandidate|originIneligible|fullChainCompleted|battleEnded
}
ChainActionDef {
 id:Id, heroineId:Id, effectRuleId:Id, targetRuleId:Id,
 resourcePolicy:"none", timelinePolicy:"preserve", commandInteractionPolicy:"none", presentationId:Id
}
```

チェインの進行は戦闘仕様が定義する。chainActionIdはHeroineDefの必須参照で、人物ごとの固定行動を指す。値未定のルールを0へ自動補完しない。

### ローカル実装の戦闘用JSON境界（2026-10-02）

`game/unity/Assets/Game/Resources/Combat/battle-preview.json`をUnityのJsonUtilityで読み込む。CombatDefinitionCatalogは描画APIに依存しないCore型。schemaVersion=1、status=placeholder、heroines=5、skills=15、chainActions=5を必須にする。HeroineDef全体の完成形ではなく、戦闘部分の先行実装である。物語・特性・装備の樹・ジョブの正式資源ルールは未統合。

人物はid／name／jobId／baseRarity=6／skills[3]／chainActionIdを持つ。skillsの順番がコマンド枠を決め、JSON配列全体の並び順は意味を持たない。IDは共通Id規則を検査し、スキルとチェイン行動の所有者を人物参照と照合する。現行試作の編成IDはhero-0〜hero-4。任意編成や正式人物IDへの移行は別作業である。

SkillCombatDefはid／ownerId／name／effectRuleId／targetRuleIdに加え、試作ルール値resourceCost、powerScale、partScale、recoveryPercent、castPercent、targetCount、baseHealing、chainEligibleを持つ。waitRuleId／castRuleId等の共通ルール台帳は未実装で、ここでは値を直接格納する。消費・威力・部位倍率・回復人数・待機／詠唱時間とUI説明を同じ定義から解決する。戦闘開始時に値を複製し、元データの後からの変更を進行中の戦闘へ適用しない。

初回対応はeffect.damage＋target.selected-enemy、effect.heal＋target.self／target.selected-allies／target.all-living-allies。回復の詠唱と回復／支援からのチェインは未対応として拒否する。既存のゲージ減少・全体防御・他者資源補給は試作の特定人物・第3枠に限定して読み込み、効果量の汎用化は未完了。チェインは人物のchainActionIdで解決し、初回はeffect.damage＋target.boss-body＋powerScale、resourcePolicy=none／timelinePolicy=preserve／commandInteractionPolicy=noneを必須とする。任意の回復・状態効果・対象ルールへ自動変換しない。

未知版、欠落、ID重複、所有者違い、不正数値、未対応効果、複数定義ソースの混在を拒否する。Unityの読込失敗は画面にエラーを表示し出撃を止め、旧ハードコードへ補完しない。定義未指定コンストラクタは回帰試験用に残すが、通常出撃は必ずJSONを使用する。正式値・正式人物内容の採用とは区別する。

固定行動の追加対応：effect.damageはtarget.boss-body／target.lowest-hp-part、effect.healはtarget.self／target.lowest-hp-ally／target.all-living-alliesを許可する。回復量はbaseHealing＋floor(行動者攻撃×powerScale)。敵攻撃はpowerScale>0とbaseHealing=0、回復は非負で少なくとも一方が正。所有者・対象・効果の不整合、状態異常等の未対応効果は拒否する。

低HP味方は負傷している生存者のHP／最大HP比率で選び、同率は編成順。部位は未破壊部位の現在HPで選び、同値は部位定義順。いずれも追加乱数は使わない。全体回復は生存者だけを対象とし、HP上限を超えず戦闘不能を復活させない。イベントのTargetIdsは選択されたID、HealingTargetsは実際にHPが増えた人物indexを保持する。固定行動にはChainActionId／PresentationIdも付ける。これらは現行BattlePresentationEventの追補で、完全VisualEvent移行は未完了。

対象なし・回復対象が全快ではHPを変更しない。部位全破壊時に本体へ自動切替しない。対象なしも一回の固定行動として数え、チェインの次判定へ進む。この不発方針は試作実装値で、正式人物制作時に受入れ確認する。通常資源・時刻・待機／詠唱予約は変更せず、部位破壊の既存効果は適用する。

実行用JSONの仮人物は、暁＝本体攻撃0.6、翼＝低HP部位攻撃0.6、守護＝自己回復10＋攻撃×0.3、森＝低HP割合味方回復10＋攻撃×0.4、星＝本体攻撃0.6へ分けた。これは効果検証用のplaceholderで、ジョブから固有行動を自動決定する規則でも正式人物内容でもない。人物別chainActionIdの参照を編集して決める。

## 収集・育成・経済の定義型

```text
PoemDef { id, ownerType: colossus|heroine, ownerId, chapter: 1..3, textId, sourceRuleId }
StoryChapterDef { id, ownerType, ownerId, chapter: 1..3, requiredPoemIds[], scriptId }
PlayerProgress { collectedPoemIds, unlockedStoryIds, readStoryIds }
```
```text
AffectionState { heroineId, value, unlockedEventIds, readEventIds, loverStatus }
EventDef { id, heroineId, kind: affinity|lover, prerequisiteIds[], affectionRequirement?, scriptId }
```
```text
TerraformMilestone { id, requiredXp, unlockType, unlockIds[] }
FurnitureDef { id, recipeId, placementRuleId, interactionSetId }
```
```text
WeaponTreeDef { heroineId, nodes[] }
WeaponNode { id, parentIds[], tier, displayPosition, weaponId, materialCosts:CostEntry[], skillId?, statModifiers[] }
```

```text
OopartDef { id, abilityId, fixedStatGrowthId, randomStatPoolId, levelCap: 120 }
OopartInstance { instanceId, defId, level, fixedStats, randomStats, directUpgradeState }
```
```text
BannerDef { id, heroinePoolIds[], materialPoolIds[], oopartPoolIds[], heroineRate: 0.03,
            singleDrawCost:300,tenDrawCost:3000,pointsPerDraw:1,exchangeOfferId:Id }
GachaState { bannerId, totalDrawCount, pointAccountId:Id, history[] }
```
## 箱庭・ADV・保存の型

```text
GardenLayout { schemaVersion:1,gardenId:Id,zones:[{zoneId:Id,order:int,bounds:Rect01}] }
FurnitureLayout { defId:Id,size01:positiveSize01,footprint:Rect01,drawAnchor:Point01,
                  allowedOrientationIds:Id[],interactionSlots:[{slotId:Id,offset:Point01,actionIds:Id[]}] }
Placement { instanceId:Id,defId:Id,gardenId:Id,zoneId:Id,x:0..1,y:0..1,orientationId:Id }
Occupant { heroineId:Id,gardenId:Id,slotId:Id,furnitureInstanceId?:Id,actionId?:Id }
```
```text
AdvScript { schemaVersion:1,sceneId:Id,scriptVersion:positiveInt,commands:AdvCommand[] }
AdvCommand.kind = background | actor | hideActor | cg | hideCg | line | sound | end
```
```text
SaveEnvelope { schemaVersion:positiveInt, contentVersion:string, saveId:Id,
               revision:nonnegativeInt, payload:ProgressPayload }
ProgressPayload additions {
 furniturePlacements:Placement[], occupants:Occupant[],
 readLineKeys:[{sceneId:Id,scriptVersion:positiveInt,lineId:Id}],
 unlockedEventIds:Id[],readEventIds:Id[],claimedRewardIds:Id[]
}
```
```text
SaveV2 {
  version, playerId, mapProgress, defeatedColossusIds, highestClearedLevelByColossus,
  terraformingXp, unlockedMilestoneIds, furnitureInventory, furniturePlacements,
  heroineStates, weaponNodeIds, oopartInstances, collectedPoemIds,
  unlockedStoryIds, readStoryIds, affectionStates, gachaStates,
  claimedBattleRewardIds
}
```

| 命令 | 必須 | 任意・規則 |
| --- | --- | --- |
| background | commandId,assetId | transition:instant|fade、durationMs。初回背景を必須 |
| actor | commandId,heroineId,slotId,outfitId,expressionId,poseId | 同一slotへ別人物を重複しない |
| hideActor | commandId,heroineId | 未表示でも安全に終了 |
| cg | commandId,assetId,hideActors:bool | 立ち絵保持／隠蔽を明示 |
| hideCg | commandId | 元の人物表示状態へ戻す |
| line | commandId,lineId,textId | speakerId省略は地の文、既読単位はlineId |
| sound | commandId,audioId,channel:bgm|se | 素材欠落時は通知し本文進行を保持 |
| end | commandId | 一つだけ、命令列の末尾に置く |

人物の素材解決は`HeroDisplaySet { heroineId, outfitId, standingAssetId, expressionAssetById, poseAssetById }`と`AdvLayout { layoutId, actorSlots:[{slotId,anchor,pivot,size01,drawOrder}] }`を使う。actor命令の全参照を開始前に検証する。表情未登録は同じ衣装の通常表情へ戻す。pose未登録は用途名付き仮表示と警告。通常表情やstandingの必須欠落は正式受入れ不合格とする。lineのspeakerIdはheroineIdまたは定義されたspeakerIdに対応し、地の文では省略する。

## 表示イベントのチェイン拡張

chainCheck／chainAction／chainEndedはchainIdとchainStepを必須とし、chainActionはchainActionIdも必須。chainCheckは接続確率・候補・判定を参照するログIDを持つ。失敗後のchainAction、未定義固定行動、通常周回内の同一人物の二重参加、追加一周内の二重実行、通常tickによる候補選出を拒否する。chainActionの解決をユーザー入力待ちにしない。通常予定列・詠唱予約・ジョブ資源が変化しないこと。補正率や資源条件の正式数値はこの追補で確定しない。

## 所有権と版管理

本書は項目・型・座標・参照・検証の唯一の詳細定義元。行動規則はsystems、見せ方はpresentation、受入ケースはproduction/acceptanceへ参照する。SaveV2は既存論理項目、SaveEnvelopeは将来の交換・版管理案であり、現行実装の型名と一致するとは限らない。ローカルの現在版確認前に保存形式を変更しない。

## 条件・費用・操作の具体形式

以下は現行実装型名ではなく交換契約。数値・本文が未定の定義はstatus=placeholderで区別する。

```text
ConditionExpr =
 {kind:"all",items:ConditionExpr[1..]} |
 {kind:"any",items:ConditionExpr[1..]} |
 {kind:"flag",domain:"poemOwned"|"storyUnlocked"|"storyRead"|"eventRead"|"heroineOwned"|"lover",id:Id} |
 {kind:"atLeast",domain:"affection"|"terraformingXp"|"highestClearedLevel",ownerId?:Id,value:nonnegativeInt} |
 {kind:"always"}
CostEntry { resourceId:Id, amount:nonnegativeInt }
HeroinePoemLink { colossusPoemId:Id, heroinePoemIds:unique Id[1..] }
BattlePoemState { heardColossusPoemIds:unique Id[],pendingColossusPoemIds:unique Id[] }
TransactionRecord { transactionId:Id, kind:"battle"|"event"|"upgrade"|"craft"|"gacha"|"exchange"|"advRead"|"garden",
                    targetIds:Id[],contentVersion:string,baseRevision:nonnegativeInt,
                    status:"pending"|"committed",costs:CostEntry[],result:TransactionResult }
TransactionResult { grants:ResourceGrant[],addedIds:Id[],changedEntities:EntityChange[],
                    rngDecisionIds:Id[] }
ResourceGrant { resourceId:Id,amount:nonnegativeInt }
EntityChange { entityType:Id,entityId:Id,after:object }
GachaCategoryTable { heroineBp:300,otherBp:9700,heroineIds:unique Id[1..],
                     otherEntries:[{rewardId:Id,weight:positiveInt}] }
RandomStatDef { statId:Id,unitScale:positiveInt,maxValue:positiveInt }
BookSubject { bookmarkId:Id,subjectId:Id,pageOrder:nonnegativeInt,unlockCondition:ConditionExpr }
```

ConditionExprは副作用なし、現在の同一進行snapshotを評価する。all／any空配列、未知kind、未知domain、負値、欠落参照を拒否する。affectionとhighestClearedLevelはownerId必須、terraformingXpは省略。loverのidはheroineId。alwaysは解放条件なしを意図した正式指定のみ許可し、欠落条件の補完には使わない。notや所持消費を含む条件は初回対象外とし、解放を逆戻りさせない。

EventDefへunlockCondition、StoryChapterDefへrequiredPoemIds、各HeroineDefへpoemLinksを持たせる。従来EventDef.prerequisiteIdsとaffectionRequirementを併記する場合はunlockConditionと同値を必須とし、移行後は条件式を定義元にする。ResourceGrantは数量資源だけで、人物や詩の追加はaddedIds、Lv等の変化はchangedEntitiesで型別検証する。EntityChange.afterはentityTypeごとの完全な変更後状態であり、任意コード・任意パスの実行命令ではない。

TransactionRecordは交換／ログ上の論理形式。pendingは初回セッション内のみ、committedのIDは永続化する。進行revisionは成功した保存ごとに1増加し、再試行・再描画では増えない。整数は安全整数範囲を共通に適用、乗算・合算はオーバーフローを検証する。

## チェイン表示の不足項目の補完

chainCheckは `decisionId:Id` を必須とし、抽選ログに `probabilityBp:ProbabilityBp` と `success:bool` を追加する。chainStepは起点コマンドを1とした予定行動番号を表す（最初の接続判定は2）。失敗終了のchainEndedは判定したstep、候補なし／勝敗／追加一周完了では最終実行番号を持つ。通常周回の新規人物のchainAction後だけlengthとparticipantIdsを増やし、追加一周では重複追加しない。chainEnded時のlengthは実行済み参加人数で、失敗候補を参加者に含めない。chainCheck／chainEndedにactorIdがある場合は起点人物、chainActionは実行人物を指す。チェイン前後の資源・通常予定列・詠唱予約は不変。HP・部位・勝敗の変化はpostSnapshotへ反映する。

## 定義パックの受入れと診断

定義パックはcontentVersion単位で全参照検証後に採用する。一部ファイルだけ新しい版へ切り替えない。schemaVersionは各交換形式、contentVersionは意味とID対応の版である。正式データでplaceholder参照、未定ruleId、欠落費用、依存循環があれば受入れ不可。検証用パックはplaceholderを明示し、正式進行セーブと混用しない。

診断は `severity / code / documentId / jsonPointer / referencedId? / message` を持ち、エラー位置と理由を日本語で表示する。最低限のcodeはUNKNOWN_SCHEMA、DUPLICATE_ID、MISSING_REFERENCE、INVALID_RANGE、DEPENDENCY_CYCLE、UNRESOLVED_RULE、PLACEHOLDER_IN_RELEASE。warningだけでは正式必須参照の欠落を許可しない。

## 編成順チェインの確定型

```text
BattleFormation { heroineIds:unique Id[5] }
ChainContext { id:Id,originActorId:Id,originActionId:Id,modifierBatchId:Id }
CastReservation additions { chainContextId:Id }
ChainState additions { formationIds:unique Id[],cursorIndex:nonnegativeInt,chainContextId:Id }
```

formationIdsは戦闘開始時の編成順を保持する。cursorIndexは直前参加者の編成indexで、候補決定は次indexからの循環走査。編成範囲外index・編成外人物・別起点contextを拒否する。modifierBatchIdは予約した起点のものを参照する。通常行動から参照する資源ルールID／待機変更ルールIDをChainActionDefへ持たせない。旧resourceRuleId／timelineImpactRuleIdは正式契約から削除し、none／preserve以外を検証エラーにする。固定行動にコマンド資源変更やコマンド固有ルール発火があれば受入れ拒否。

## フルチェインの型とイベント識別

```text
ChainState additions {
 fullChainTriggered:bool, bonusActionIndex:0..5,
 actionCount:1..10, bonusExecutedIds:unique Id[]
}
VisualEvent additions { chainPhase?:"normal"|"returnCheck"|"fullChain",fullChainTriggered?:bool }
BattleSnapshot.chain additions { fullChainTriggered:bool,bonusActionIndex:0..5,actionCount:0..10 }
```

length／participantIdsは通常周回の異なる参加者数・集合として最大5を維持し、実行回数と分離する。actionCountは起点コマンドと実行済み固有行動の合計。追加一周の人物はbonusExecutedIdsで別管理し各人一回まで。追加一周で通常周回未参加の人物が復帰した場合もparticipantIdsへ混ぜない。

起点へのchainCheckはchainPhase=returnCheck、成功後のchainActionはchainPhase=fullChain。全員行動可能な例では戻り判定のchainStep=6、追加行動A=6/B=7/C=8/D=9/E=10。chainStepを人数として使わない。通常chainActionはchainPhase=normal。fullChainTriggeredは戻り判定成功でのみtrue。追加一周でchainCheckを発行する、同じchainIdで二度fullChainTriggeredを立てる、追加Aを重複実行することを拒否する。chainEndedは通常終了または追加一周完了／途中勝敗で一度だけ発行する。

## ヒロイン所有の詩対応と終了条件

HeroinePoemLinkは格納先HeroineDef.idが所有者。colossusPoemIdはownerType=colossusの詩、heroinePoemIdsは全件ownerType=heroineかつownerIdが格納先ヒロインと一致すること。同ヒロイン内でcolossusPoemId重複を拒否する。複数の巨神獣詩が同じヒロイン詩を指すことは許可し、取得は集合の和集合とする。実際の対応数・対応先は人物別データで指定する。

heardColossusPoemIdsは取得済み詩の再歌唱も含む。pendingColossusPoemIdsは未所持の新規取得だけ。両集合は歌唱完了時に更新し、歌唱回数を所持数へ変換しない。battle結果の詩付与条件はvictory|defeat|retreat、取得上限フィールドは持たない。上限なしは所持定義数を越えた重複所持を意味しない。

## ガチャ費用・ポイント交換・人物別装備素材

```text
GachaPointAccount { id:Id,balance:nonnegativeInt }
GachaTicketDef { id:Id,targetHeroineId:Id,targetBannerId:Id }
GuaranteedHeroineBannerDef { id:Id,ticketId:Id,targetHeroineId:Id,heroineProbabilityBp:10000,
                           ticketCost:1,stoneCost:0,pointGrantRuleId:Id }
TicketInventory { ticketId:Id,count:nonnegativeInt }
GachaExchangeOffer { id:Id,pointAccountId:Id,pointCost:100,ticketId:Id,ticketCount:1 }
MaterialDef { id:Id,displayName:string,kind:"colossus",colossusId:Id }
```

石ガチャはpointsPerDraw=1を必須とし、1回1・10回10ポイントを付与する。専用チケットガチャのpointGrantRuleIdは別途TBDであり、未定の正式データを受け入れない。pointAccountIdの共有関係で共通／バナー別範囲を表すが、正式な範囲はTBD。totalDrawCountは履歴・統計用で、ポイント残高や交換権へ自動変換しない。旧exchangeAvailableCountは新契約から除き、既存保存の変換は正式付与規則と明示移行表の確定後に行う。

WeaponNode.materialCostsは所属WeaponTreeDef.heroineIdのノード専用定義。各resourceIdはMaterialDef.idを参照し、対応巨神獣が存在すること。初期取得ノードは空費用を明示してよいが、素材消費で解放するノードは1件以上・amount>0を必須とする。既定費用の暗黙継承、未知素材、負量、別人物のノード流用を拒否する。費用の合算・不足・原子的保存は共通CostEntry契約に従う。

専用チケットと専用ガチャのtargetHeroineIdは一致を必須とし、別対象への使用を拒否する。石ガチャ抽選の3%テーブルは専用100%ガチャへ適用しない。

## 覚醒・重複・チェイン調整データ

以下は新しい初期調整用データ契約。既存セーブへ無変換で追加せず、定義内容版と必要な保存移行を明示して実装する。数値の定義元はsystems/progression.mdとsystems/battle.md。

```text
AwakeningRule { id:Id, fromStage:0..1, requiredLevel:positiveInt, costs:ResourceCost[] }
DuplicateGrowthRule { id:Id, maxRank:positiveInt, fragmentGrant:positiveInt,
 rankCost:positiveInt, statStepBp:nonnegativeInt, overflowResourceId:Id }
HeroineDef additions { duplicateTraitGrowth:{ targetEffectId:Id,
 unit:integer|bp, baseValue:positiveInt, rankValues:positiveInt[6] } }
HeroineGrowthState additions { duplicateRank:0..5, duplicateFragments:nonnegativeInt }
ChainBalanceDef { id:Id, baseRateBp:ProbabilityBp, skillBonusChanceBp:ProbabilityBp,
 skillBonusBp:ProbabilityBp, cumulativeGrantChanceBp:ProbabilityBp,
 cumulativeBonusBp:ProbabilityBp, cumulativeMaxActors:0..2 }
ChainModifierBatch { id:Id, skillBonuses:{skillId:Id,bonusBp:ProbabilityBp}[],
 cumulativeActorIds:unique Id[] }
ChainState additions { activatedBonusActorIds:unique Id[], accumulatedBonusBp:ProbabilityBp }
```

rankValuesは基準量と各ランクの値を明示し、普通の数値効果では共通成長式から生成して照合する。特殊特性の表は人物定義で承認し、未定義をゼロや別特性へ補完しない。特性強化からチェイン率・速度・行動数・ジョブ資源規則の変更を拒否する。専用欠片はheroineIdごと、汎用素材・覚醒結晶は共通資源残高として保存する。

補正の対象は編成内ID、重複なし・上限2人。activatedBonusActorIdsは実行済み対象だけで、同一人物を戻り判定で二度追加しない。バッチは起点スキルと詠唱予約のcontextに紐付ける。人物定義に確率欄を追加せず、chainActionIdの欠落は正式データ検証エラーにする。

## 先行戦闘スライス：攻撃後の自己効果

`SkillCombatDef` の `effectRuleId=effect.damage` に限り `selfHealingBaseAttackPercent`（0〜1000）と `selfDamageMaxHpPercent`（0〜100）を指定できる。省略は0で旧版と互換。対象を確認し資源を一度消費した成功攻撃だけ、敵へのダメージ→自己回復→自己反動の順で処理する。対象なし／破壊済み／戦闘不能／資源不足／戦闘終了では全体を拒否する。支援・回復専用技への付与は拒否する。

回復は `floor(BaseAttack * percent / 100)`、反動は `floor(MaxHitPoints * percent / 100)`。整数計算はlongを介し、実適用量は不足HP／残HPで制限。死亡者を回復で復活させない。BaseAttackは不変値、Attackは持続攻撃強化を含む現在値として分離する。武器等をどこまで基礎に含めるかは正式育成・効果定義で解決し、この試遊算式をA ASTERの内部算式確認済みとはしない。

通常・詠唱発動とも同じ解決器を使う。詠唱開始は資源だけ消費し、自己効果は発動成功時のみ。不発は返金も自己効果もなし。勝利打でも同じコマンド内の自己効果は完了する。致死反動の後は起点チェインを始めず、追加乱数も引かない。以上の処理順・勝利打・致死反動方針はnewASTERの先行実装ルールで、動画の実測結果ではない。

演出は攻撃／詠唱発動→実回復がある場合のHealing→実反動がある場合のSupportを同じClockで記録する。対象IDは効果を受けた人物。現在のイベントHPは一コマンドの最終状態スナップショットであり、各効果前後の中間状態の再生は未実装。新コマンド・資源支払い・待機・抽選は追加しない。固定チェイン定義とは独立で、通常技の追加効果をチェインへ暗黙適用しない。

## 先行戦闘スライス：持続自己効果

`SkillCombatDef.effectRuleId=effect.self-buff`、`targetRuleId=target.self` とし、`selfEffects:[{kind,percent,turns}]` を指定する。kindは `attack`、`physical-protection`、`regen` と、下記追加境界の `critical`、`critical-damage`、`forced-target`。攻撃強化と再生は1〜1000%、物理防護は1〜100%、持続は1〜10回。同じkindの重複は拒否する。自己強化専用技は通常3技の任意スロットへ置けるが、詠唱・チェイン開始・敵対象は拒否する。攻撃との混在は下記の攻撃後自己効果として許可し、回復専用技との混在は拒否する。省略/null/空配列は既存技と互換。起動時に入れ子も複製し、読み込み元の変更が進行中の戦闘へ反映されない。

先行ルールでは動画の「ターン」を本人の成功コマンド／パスの完了回数へ変換する。付与コマンド自体は新効果の残り回数を減らさず、既存効果を一回消費してから新効果を入れる。詠唱は予約時に一回、発動時は消費しない。他人物・敵・独立チェインは消費しない。失敗コマンドは消費しない。同種再付与は新しい量・回数で上書きし、加算しない。戦闘不能で効果を消去し、蘇生しない。旧非速度順モードでこの定義を指定すると拒否する。

Attack=`floor(BaseAttack*(100+攻撃強化%)/100)`、int上限へ飽和。通常攻撃・固定攻撃・攻撃基準回復に適用する。基礎攻撃基準の自己回復はBaseAttackを維持する。詠唱開始時には強化込みAttackを予約し、残り回数消費／後の再付与で発動威力が変わらない。再生は本人の通常コマンドが行動可能になる直前に `floor(現在Attack*再生%/100)` をHP上限まで回復し、付与直後・チェイン・詠唱発動では発生させない。物理防護は既存敵攻撃の軽減・防御計算の後に `floor(damage*(100-防護%)/100)` を適用し、100%なら0。

これらの消費単位・再生時点・上書き・端数・詠唱予約方針はnewASTERの先行実装方針であり、A ASTER内部処理の再現確認済みではない。現行試遊敵の攻撃は物理として扱うが、全巨神獣の技を物理扱いにする仕様ではない。会心と単体敵攻撃の強制標的は下記追加境界で実装。魔法攻撃／防護・状態異常は未実装。

各人物の `TimedEffects` は不変スナップショットで渡し、演出イベントの `HeroEffects` に同時記録する。再生と持続更新のイベントはチェイン0／フルチェインfalseを明示し、直前の表示を継承しない。UIの効果量・残り回数は演出中にこのスナップショットを使う。効果更新や表示は論理時刻・資源・抽選を追加変更しない。正式人物の技全体は全効果が解決可能になってから切り替える。

## 戦闘読込v2・使用条件・会心（計画3の追加境界）

### v3正式人物・本作独自ルールの読込

現行ゲームの出撃は `Combat/battle-formal.json`、`schemaVersion=3` / `status=newaster-original`。`designOrigin=user-authorized-newaster-rules-2026-10-02` を必須とする。v1仮人物・v2統合検証は回帰互換のため残すが、起動時にv3が欠けたら旧仮値へ自動フォールバックしない。仕様の定義元は[戦闘仕様：初期5人の実行ルール](../systems/battle.md)。

HeroineCombatDefは従来の3skills・chainActionIdに加え、traitId、weaponTreeId、poemChapters[3]、poemLinks[18]、affinityEventIds[3]、loverEventIds[2]、hpBp／attackBp／defenseBp／speedBp、traitHpPercent／traitAttackPercentを持つ。contentReferencesはid／ownerId／kind／statusを持ち、参照存在・件数・重複・所有者・種別を検証する。戦闘特性はimplemented必須。武器・詩・イベントはreservedを明示して本文／ノード制作を保留できるが、存在しないIDをTBD文字列で補完しない。全5人で140参照。予約コンテンツを遊べる実装と扱わない。

jobs[5]はid、資源名／上限／開始量／行動可能時・攻撃後・被ダメージ後の獲得量、Lv1のHP／攻撃／物防／魔防／速度／会心率。人物のjobIdは実在するJobCombatDefへ参照する。HP・攻撃は1〜1000000、防御は0〜1000000、速度1〜10000、会心0〜10000bp、資源1〜15。HP／攻撃／防御補正9000〜11000bp、合計30000、速度9500〜10500bp。構築後のHP・攻撃が0になる定義も拒否する。現行はLv1だけに適用し、旧インデックス育成値を正式人物へ移し替えない。

SkillCombatDefはdamageType、targetRuleId（単体／範囲／全体）、ignoreDefenseBp、statusEffects[{kind,amount}]、enemyWaitAdd、selfWaitReductionPercent、chargeConsumeMax、chargeBonusPercent、specialWeaponBonusPercentを持つ。未知状態／重複状態／範囲外数値／攻撃でない技への攻撃効果混在を拒否。状態・ジョブ固有追加効果はv3だけで受け入れ、未対応の旧モードへ読ませて黙って無視しない。形式上の式ではなく解決器へ接続された対応kindだけを許可する。

各技はsourceSkillId・sourceFile・sourceSecond・observedSkillLevel=7・ruleOrigin=video-observation-plus-newaster-originalを持ち、出典欠落を拒否する。固有行動のruleOriginはnewaster-original。資料JSONの観測上限−1・unresolved・装備込み撮影値を実行用の空欄補完に使わない。技の元の確認値保持は全15技の資料対照テストでも検査する。

条件は既存3種に加え、job-resource-at-least（0〜15）、trait-equipped（本人traitId・threshold=1）、boss-status-active（対応状態名・threshold=1）。AND評価であり、特性の別人物参照・上限を超える資源条件・未知状態条件は拒否。条件、持続効果、攻撃型、状態、資源、詠唱プロファイルは戦闘開始／予約で複製し、表示から書換えられない。

状態耐性はenemyStatusResistances[{kind,resistanceBp}]、0〜10000、同kind重複禁止。本体・部位の状態値と寿命は別インスタンス。演出イベントは実際のTargetIdsとEnemyStatusesの不変スナップショットを持ち、複数部位の破壊・合計ダメージ・資源更新を反映する。将来の各巨神獣固有表・別部位耐性・味方の状態異常は後続で、初期5人の15技に必要な実行ルールと区別する。

### 物理／魔法防御と防御無視：newASTER独自仕様

2026-10-02ユーザー承認により未確認算式を本作独自として設計する。資料用JSONの観測値やTBDを、本作の算式で確認済み扱いに置換しない。

攻撃技および固定攻撃は `damageType=physical|magic` と `ignoreDefenseBp=0..10000` を明示できる。旧仮定義で省略された型はphysical、防御無視は0として互換を維持する。回復・支援に攻撃型や防御無視が付く場合は拒否する。敵本体は `enemyPhysicalDefense` / `enemyMagicDefense`、部位は個別の物理／魔法防御を持ち、負数を拒否する。現行読込スライスでは全4部位へ本体と同じ防御を複製するが、Coreの部位防御は独立値を受け入れる。敵固有定義への接続は後続作業。

有効防御=`対応防御×(10000−ignoreDefenseBp)/10000`。ダメージ=`floor(攻撃×技倍率×会心倍率×1000/(1000+有効防御))`、その後に技上限・int上限を適用し最低1。最後に残HPへ制限する。防御計算の途中で切捨てない。会心以外は追加抽選なし。通常攻撃と表示予測は同じ純粋計算を使用する。詠唱予約は攻撃型と防御無視を含む技を不変保持し、発動時に生存する対象の防御へ適用する。固定チェインも同じ防御計算を使うが、会心抽選・資源消費・通常行動予定への干渉を追加しない。攻撃の既存部位倍率と本体防護倍率はこの計算前に適用する。

0防御は旧試遊結果と同じ。防御1000は50%軽減、3000は75%軽減。防御無視10000bpは防御を完全無視する。これはA ASTERの防御算式を再現したものではなく、本作の初期バランス値。動画の「魔法320%」「物理詠唱」「防御無視」という種別は維持し、数式の出典だけを分離する。味方の物理／魔法防御を敵技へ適用する拡張と状態異常は未実装。

戦闘スライスはv1／placeholder（旧hero-0〜4）とv2／integration-trial（明示 `formation[5]`）を許可する。v1のformationは省略/null/空配列で互換、v2は正しいID5件・重複なし・人物の存在・3技の所有者・独立固定行動の所有者を検証する。全配列の格納順で人物を対応させない。未知版／未知statusは拒否し、integration-trialを正式品質合格と扱わない。v2対応は任意人物IDの読込境界であり、現行セーブの仮人物を指定5人へ転用する移行ではない。

全3スロットの `effect.damage` を通常攻撃／詠唱攻撃として許可する。第3枠を人物添字による支援へ固定しない。攻撃にも `selfEffects` を指定でき、成功発動後に持続自己効果を付与する。通常発動は古い効果の消費→新効果付与→独立チェイン、詠唱発動は予約時にすでに回数を消費しているので付与だけを行う。新効果を付与直後に減らさず、資源追加消費・詠唱開始時付与・不発時付与をしない。

`conditions:[{kind,threshold}]` は全件ANDで、resource-at-least（0〜10）、hp-at-most-percent（0〜100）、broken-parts-at-least（0〜4）を先行対応する。未知kind／同種重複／null／閾値範囲外を拒否する。HP割合は整数の交差乗算で判定する。プレビュー・UI受付・コマンド実行で同じ状態を参照し、不成立は時刻・資源・効果を変えない。入れ子条件を実行時に複製し、任意コードを実行する式は採用しない。

持続自己効果へcritical（会心率のポイント加算、1〜100%）、critical-damage（会心倍率のポイント加算、1〜1000%）、forced-target（値1のみ）を追加する。会心率はBaseCriticalChanceBp＋持続会心率×100＋技のcriticalBonusBp、上限10000bp。倍率は基準150%＋持続会心威力。150%という基準と確率の合成はnewASTERの先行値で、A ASTER内部の確認値ではない。旧仮人物の会心基準は0のまま維持し、装備込み撮影値を転用しない。

通常／詠唱攻撃の成功時、確率0／100%は追加抽選なし、その他は引数で明示した乱数源を一回だけ使い、roll < probabilityで会心。使用条件・対象・資源の不成立では抽選しない。乱数源の欠落／範囲外の返り値はHP・資源を変える前に拒否する。非会心プレビューで乱数を進めず、会心率を別表示する。`damageCap` の0は旧仮データとの互換用の上限なし、正数は倍率適用後の上限。正式人物の実上限をTBDの0へ補完する規則ではない。発動結果の会心・抽選値をログへ記録する。

詠唱予約は不変BattleSkillに攻撃力・会心率・会心威力・上限・自己追加効果を保持する。発動時の持続効果失効／再付与で予約済みの値を変えない。独立チェインの固定行動に通常技の会心／条件／追加効果を暗黙適用しない。

EnemyTargetSelectorは単体攻撃の対象を生存する強制標的人物へ変更する。複数いる場合は編成順。全体攻撃は全生存者のまま、死亡した標的は除外する。現行試遊敵の技は全体攻撃なので、この追加だけで敵の技を単体へ変更しない。正式巨神獣の行動テーブルとの接続は後続。

## Lv育成・ジョブ基準・キャラ補正

初期正式編成はスレイヤー／アイコノクラスト／アンダーマイン／エキドナ／エクスカリパン。[動画確認値と正式実装への変換](initial-five-heroines.md)を参照。`heroine-reference.json` の観測型を実行用HeroineDefとして扱わず、装備込みの値を基礎値へ昇格させない。

```text
LevelGrowthDef { id:Id,nectarResourceId:"resource.nectar",costBase:10,costPerCurrentLevel:2,
                 statGrowthBasePct:100,statGrowthPerLevelPct:3,speedGrowsWithLevel:false }
JobBaseStats { jobId:Id,hp:positiveInt,attack:positiveInt,defense:positiveInt,speed:positiveInt }
CharacterStatModifiers { hpBp:9000..11000,attackBp:9000..11000,defenseBp:9000..11000,
                         speedBp:9500..10500 }
HeroineDef additions { growthDefId:Id,statModifiers:CharacterStatModifiers }
JobDef additions { baseStats:JobBaseStats }
HeroineLevelState { heroineId:Id,level:1..120,awakeningStage:0..2 }
LevelUpRequest { transactionId:Id,heroineId:Id,targetLevel:1..120,baseRevision:nonnegativeInt }
```

resource.nectarはLv育成専用の単一数量資源として定義し、MaterialDef.kind=colossusへ混ぜない。ネクタルの大小・品質別IDは作らない。JobBaseStats.jobIdは格納先JobDef.idと一致。キャラのHP／攻撃／防御bp合計30000、全参照と上限Lvを検証する。速度補正はチェイン接続へ適用しない。

費用・成長算式はsystems/progression.mdを唯一の定義元とする。計算は広い整数で行い、UIと保存結果は同じ算式を使う。基礎ステータスはLv・ジョブ・キャラ補正とcontentVersionから導出し、セーブの重複正値を定義元にしない。内容版を変更するときの既存キャラ再計算は版移行規則に明示する。

## 計画4・独立育成保存の先行実装

現行通常ゲームは`FormalCampaignSave {version:1,saveId:"newaster.formal-campaign",revision:nonnegativeLong,world:CampaignSaveV2,growth:FormalGrowthSave}`へ統合済み。育成payloadの契約は下記を維持し、独立ファイルは正常な直前正式保存の読取専用統合元だけとする。`FormalCampaignJournal`は世界・育成・討伐の確定を一つの書込callbackへ渡す。worldの各配列長・数量・ID集合を検証し、unknown IDを推測で削除しない。

保存codecはpayloadとは独立した`FormalCampaignHeader {version,saveId}`の読込を提供する。将来版でpayloadの型が変わっても、読込不能な「破損」と誤判定して古いbackupへ戻さない。`FormalRecoveryOffer`はstatusと、確認した保存場所・raw bytes指紋・候補payloadを不変保持する。`RecoveryPreview`は独立したコピー、`RestoreConfirmed`は同じ場所・指紋・候補の再検証を必須にする。確認前のoffer取得・previewは状態やファイルを変更しない。

`FormalVictoryRequest`はbattleId・colossusId・召喚Lv・基準envelope revisionを不変保持し、receipt.signatureに検証用報酬版・colossusId・Lvを記録する。receipt IDはworld.claimedBattleIdsの同じbattleIdと対になる。初回組立だけ世界callbackを実行し、失敗後は候補全体を再利用する。確定済みID再送はcallback・書込・付与を再実行しない。世界と育成のrevisionは別で、世界のみの操作ではgrowth revisionを増やさない。

2026-10-02ユーザー確定により旧試遊互換は不要。`FormalGrowthSave`はCampaignSaveV2を入力として受け入れず、正式人物IDで別管理する。現行`version=2`、`contentVersion=growth-2026-10-02`、`saveId`、非負long `revision`、非負int `nectar/awakeningCrystals/overflow/stones/kinderPoints/totalKinderDraws`、`heroines[]`、`tickets[{heroineId,count}]`、`receipts[]`を必須とする。人物状態は`heroineId/level/awakeningStage/duplicateRank/fragments`、receiptは`transactionId/signature/kinderOutcomes[]`。outcomeはkind・heroineId・amount・grantKind（owned/fragments/overflow）を持ち、育成receiptの結果列は空。配列と要素は深く複製し、負数・重複チケットID・不正結果種別を拒否する。既存正式v1だけ新経済を0で補完する。件数上限・将来の保存サイズ制限は配布受入れで追加する。

`GrowthRequest`は不変で、内容版・人物ID・操作種別・目標Lv・基準revisionを署名に含める。Level以外のtargetLevelは0。入力／出力／I/Oへ渡すpayloadは深いコピー。確定済みIDの別内容再使用、未知人物への操作、不足、上限超過、オーバーフローを状態変更前に拒否する。未知人物の既存状態は保存往復で保持する。

`GrowthPreview`はネクタル・結晶・専用欠片・汎用の消費内訳、最大時の汎用化量、対象人物の変更後状態を返す。汎用化と強化を同一payloadで確定する。`ReceiveHeroine`は排出／報酬の内部付与入口で、画面から無償付与する操作ではない。`KinderRequest`は操作ID・revision・種別・回数・対象ID・定義版を不変保持する。石／ポイント／チケット／全排出／重複変換を同一payloadで保存し、人物付与だけ別保存しない。抽選定義は検証後に複製、プレビューは乱数を消費しない。結果再送はreceiptを返して乱数・費用・保存を再実行しない。

`FormalGrowthMath`はジョブ基準を引数に取る純粋算式。Lvの基礎値は最後に一度切捨て、重複の+2%／段階を別に切捨てる。特性量は明示単位の基準量に対して算出する。bp特性を整数percentへ先に丸めない。

正式5人の通常育成・戦闘への接続を実装済み。`PlayableBattle.formalGrowth`は正式定義でのみ受理し、出撃5人全員の所持を必須とする。成長値は出撃時に導出して固定する。現行5ジョブの実行基準は`battle-formal.json.jobs`で、旧試遊のLv／枝／重複配列は使用しない。物理／魔法防御に同じ人物defenseBpと成長式を適用する。HP／攻撃の人物特性は明示されたtraitHpPercent／traitAttackPercentだけをbpへ変換し、rankの成長を掛けた後、基礎値・重複の外側へ乗算する。Lv50や最大rankでも速度とチェイン確率は変えない。追加特性種別を勝手に同じ式へ補完しない。
