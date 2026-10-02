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
             poemChapters[3], affinityEventIds[3], loverEventIds[2] }
SkillDef { id, ownerId, targetRuleId, costRuleId, powerRuleId, waitRuleId,
           castRuleId, attributeId?, statusEffects[], partModifierId?, chainEligible }
JobDef { id, resourceType, resourceInitRuleId, resourceGainRules[], resourceSpendRules[], commandRules[] }
```
```text
ChainState {
 chainId:Id, mode:"automatic-fixed-action-v1",
 originActorId:Id, originActionId:Id,
 phase:checking|executing|ended,
 length:1..5, participantIds:unique[],
 candidateId?:Id, fixedActionId?:Id, modifierBatchId:Id,
 endReason?:failedRoll|noCandidate|maxParticipants|battleEnded
}
ChainActionDef {
 id:Id, heroineId:Id, effectRuleId:Id, targetRuleId:Id,
 resourceRuleId:Id, timelineImpactRuleId:Id, presentationId:Id
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
WeaponNode { id, parentIds[], tier, displayPosition, weaponId, cost, skillId?, statModifiers[] }
```

```text
OopartDef { id, abilityId, fixedStatGrowthId, randomStatPoolId, levelCap: 120 }
OopartInstance { instanceId, defId, level, fixedStats, randomStats, directUpgradeState }
```
```text
BannerDef { id, heroinePoolIds[], materialPoolIds[], oopartPoolIds[], heroineRate: 0.03,
            exchangeThreshold: 100 }
GachaState { bannerId, totalDrawCount, exchangeAvailableCount, history[] }
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

chainCheck／chainAction／chainEndedはchainIdとchainStepを必須とし、chainActionはchainActionIdも必須。chainCheckは接続確率・候補・判定を参照するログIDを持つ。失敗後のchainAction、未定義固定行動、同一人物の二重参加、通常tickによる候補選出を拒否する。chainActionの解決をユーザー入力待ちにしない。通常予定列への影響は定義したtimelineImpactRuleIdと一致すること。補正率や資源条件の正式数値はこの追補で確定しない。

## 所有権と版管理

本書は項目・型・座標・参照・検証の唯一の詳細定義元。行動規則はsystems、見せ方はpresentation、受入ケースはproduction/acceptanceへ参照する。SaveV2は既存論理項目、SaveEnvelopeは将来の交換・版管理案であり、現行実装の型名と一致するとは限らない。ローカルの現在版確認前に保存形式を変更しない。
