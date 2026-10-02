# 共通データ・JSON交換形式・イベント・検証

更新：2026-10-02。状態：現行仕様。版はGit履歴で管理し、ファイル名に版番号を付けません。仕様整理のみで実装完了を意味しません。

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
