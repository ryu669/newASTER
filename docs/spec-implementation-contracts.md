# 実装契約 v0.1：時系列・2Dデータ・箱庭・ADV・保存

更新：2026-10-02。親は[要求仕様](game-requirements-v2.md)。本書は実装が分かれる箇所を統一する。**確定要件を変更せず、未確定の方式は「暫定実装契約」と明記する。** 暫定方式も一つに固定し、別方式を同じ正式画面へ混在させない。実装・素材・セーブを変更した記録ではない。

## 1. 優先順位と判断台帳

要件・数量・禁止事項は要求仕様、世界と解放順は世界記憶仕様、横断するデータと状態遷移は本書、用途ごとの表示は2D詳細仕様を適用する。旧着手計画・3D資料・試作記録を現在の実装根拠へ戻さない。同じ優先度の矛盾は実装者が推測せず台帳へ記載する。

| ID | 統一判断 | 区分 |
| --- | --- | --- |
| C-01 | 速度順の論理イベントを使用し、固定ラウンドの未行動者連鎖を廃止 | 速度順は確定要件 |
| C-02 | チェインは時系列上の連続する別人物の即時攻撃で接続。割込み・追加行動なし | 暫定実装契約 `timeline-sequential-v1` |
| C-03 | 勝敗確定後、新しい判定は停止。確定済みの演出は最後まで再生して結果表示 | 表示契約。スキップ／撤退は例外を明示 |
| C-04 | 座標原点は左上、正規化0〜1、接地Y昇順で奥から手前へ描画 | 2D実装契約 |
| C-05 | 箱庭は自由座標、グリッド吸着なし、同一配置ゾーンのfootprint重なりを禁止 | 暫定実装契約。家具別例外は初回未対応 |
| C-06 | ADVは線形命令列。途中再開は未対応、再入場は冒頭から。行単位既読を保存 | 暫定実装契約。選択肢は後続仕様 |
| C-07 | 保存形式変更は版管理・移行・正常バックアップを必要とし、今回実施しない | 保存要件 |

基本チェイン率、補正の抽選率、各スキル正式時間、素材量、採用人物、CG数はTBD。これらは定義データに置き、欠落を0等へ黙って補完しない。検証用値と正式値は`status`で分離する。

## 2. 速度順とチェイン

戦闘は整数tickの予定列を使用する。コマンド選択中は論理時計停止。即時攻撃、詠唱開始、発動、敵行動を順序付きで確定する。同時刻は詠唱発動→敵→味方コマンド、味方同士は編成index順を暫定規則として固定する。速度式・同時刻規則の正式バランス判断はTBD。

### 2.1 チェイン状態と接続

```text
ChainState { mode, length:0..5, participantIds:unique[], lastAttackEventId? }
```

最初の即時攻撃はlength=1。次のイベントも生存する別人物の即時攻撃で、間に終了要因がなく、人数が5未満なら、その攻撃コマンドに表示済みの確率で接続判定する。成功ならlength+1、失敗なら現在の攻撃を新しいlength=1にする。抽選失敗でも選んだ攻撃自体は実行する。同じ人物の再登場または5人後の攻撃は新しいlength=1。待機時間による無イベントの空白だけでは終了しない。

敵行動、回復、防御、支援、パス、詠唱開始、詠唱発動／中断、勝敗、撤退で終了。詠唱攻撃をチェインに含める別方式は今回採用しない。部位破壊は攻撃結果の一部なので、それ自体では終了しない。予約済み／選択済み対象の有効性判定は攻撃実行前に行う。無効入力ではチェイン・乱数・資源を変更しない。

タイムラインを無視した次人物の割込み、追加コマンド、待機免除は行わない。人物・武器・オーパーツ・好感度・家具・重複による率の変更は禁止。速度によるイベント順序の変化と、抽選率の変化は区別する。

### 2.2 補正の寿命

補正はコマンド受付一回を単位とする。受付開始時に`modifierBatchId`を一度生成し、+10%判定と最大2人の+5%対象を記録する。+5%対象は生存者から重複なしで最大2人、各人最大+5%。+10%は受付中の行動者へ最大一回。同一行動者への併存は+15%。各人物の3スキルは同じ人物補正を参照する。対象者と適用／不適用を表示する。

このバッチは受付中コマンドの確定、パス、詠唱開始、戦闘終了で失効する。他人物の次受付へ持ち越さない。他人物への+5%はバッチ内の対象情報であり、将来の行動へ蓄積する永続効果ではない。この適用単位は暫定で、最終チェイン方式の判断時に改訂可能とする。

確率はbasis point（bp）整数で保持し、100bp=1%。接続率は`min(10000,max(0,baseRateBp+currentActorBonusBp))`。基本率と付与率はTBD。抽選候補と結果を一度保存し、予測表示の再描画・キャンセル・ヘルプ・演出スキップで再抽選しない。

例：A攻撃→B攻撃成功→C回復→D攻撃→D攻撃ではチェイン長1→2→0→1→1。A→B→C→D→Eが全接続成功なら5であり、次のFは新しい1。5人編成ではFは先行人物の再登場となる。

### 2.3 詠唱と対象破壊

開始時にコストを一度払い、威力の基礎値・対象ID・スキルID・発動tickを予約する。術者戦闘不能は中断、発動前に対象部位が破壊済みなら不発。消費資源は返還しない。本体へ自動転送しない。発動／不発tickから使用後待機を一度加える。これは既存速度仕様の暫定規則を一本化したもので、正式判断はTBD。

一方、プレイヤーの新規選択対象が破壊されていた場合は次コマンドのUIを本体へ戻す。既存の詠唱予約を本体へ変える操作ではない。

## 3. 計算終了と演出終了

```text
BattlePhase: command | advancing | resultPending | closed
PlaybackPhase: idle | playing | paused | completed
ResultReason: victory | defeat | retreat
```

コアが勝敗を判定すると`resultPending`にして後続の攻撃計算・乱数・行動予約を停止する。直前までに確定したイベント列は戦闘終了イベントを含めて順次表示する。コアが勝利済みでも、先行する確定攻撃の演出を勝手に捨てない。最後のイベント後に結果画面を開く。

スキップは残る装飾のみを省略し、最終スナップショットへ同期して結果画面へ進む。計算を再実行しない。撤退は`resultPending`到達前にだけ確定可能。計算上の勝敗が確定済みなら撤退ボタンを無効にし、演出スキップを提供する。撤退確定は未再生演出を破棄して拠点へ戻り勝利報酬なし。

勝利報酬はresultPending到達時に一度だけ同じbattleIdで保存する。保存成功後に結果を確定表示し、失敗時は報酬全体を保留して再試行する。演出終了や結果画面を開き直すことで報酬を再生成しない。結果表示前に終了しても正常保存済み報酬は次回進行へ残る。旧未確定セッションの再開は行わず、戦闘途中は拠点から再挑戦する。

## 4. 共通データ型と座標

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

## 5. 2D定義形式

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
           author:string, provenance:string, rightsNote:string }
CanvasGroup { id:Id, width:positiveInt, height:positiveInt, pivot:Point01 }
```

初回hitRegionは矩形のみ。多角形は後続schemaVersionで追加する。素材pathはプロジェクト素材ルート相対で`..`と絶対パスを拒否、対応拡張子はPNG。width／heightはファイルから測定した値と一致させる。差分canvasGroupは寸法・pivotの共通値を持つ。RGBA/sRGB等の書き出し条件は素材詳細を適用する。

## 6. 表示イベント契約

```text
VisualEvent {
 battleId:Id, sequence:positiveInt, tick:Tick,
 kind:attack|heal|support|pass|castStart|castResolve|castCanceled|enemy|poem|battleEnded,
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

### 6.1 乱数と再現ログ

戦闘開始時に`rngAlgorithmId`、`rngVersion`、`seed`を記録し、コアの既存アルゴリズムを識別する。既存と違う方式へ無断に置換しない。抽選ログは`battleId / decisionId / sequence / purpose / candidateIds / drawIndex / sampledValue / result`を持つ。purposeはchainModifier／chainConnect／poem／rewardを区別する。同じ入力と版でdrawIndexが一致すること。表示層は乱数APIを呼ばない。具体アルゴリズム名は既存コード確認時に固定し、未指定の正式データを受け入れない。

## 7. 箱庭配置契約

```text
GardenLayout { schemaVersion:1,gardenId:Id,zones:[{zoneId:Id,order:int,bounds:Rect01}] }
FurnitureLayout { defId:Id,size01:positiveSize01,footprint:Rect01,drawAnchor:Point01,
                  allowedOrientationIds:Id[],interactionSlots:[{slotId:Id,offset:Point01,actionIds:Id[]}] }
Placement { instanceId:Id,defId:Id,gardenId:Id,zoneId:Id,x:0..1,y:0..1,orientationId:Id }
Occupant { heroineId:Id,gardenId:Id,slotId:Id,furnitureInstanceId?:Id,actionId?:Id }
```

Placement.x/yは庭全体の左上原点に対する接地anchor。size01は庭全体に対する描画幅高さ（各0より大きく1以下）。描画左上は `(x-drawAnchor.x*size01.x, y-drawAnchor.y*size01.y)`。footprintは家具の描画矩形内を基準とし、左上＋footprint位置×size01で庭座標へ変換する。変換後の全footprintがzone.bounds内に収まること。同一zoneのfootprintは面積のある重なりを拒否し、辺の接触は許可。初回向きは`orientation.default`のみ。画像の重なりとfootprintの衝突は別で、葉や屋根が重なるだけでは拒否しない。

描画はzone.order昇順→接地y昇順→instanceId辞書順。大きいyほど手前。人物も同じ規則で合成し、固定前景maskはその後に表示する。未対応利用slotは通常待機へ戻す。利用中の動作状態は保存しない。

プレビューはcloneした作業状態。確定時に在庫・衝突・重複・ゾーンを再検証して一括保存。失敗は元状態を保持。撤去は家具所持を保持し配置だけを削除する。人物の二重配置は全庭を通して拒否する。

## 8. ADV命令と既読

```text
AdvScript { schemaVersion:1,sceneId:Id,scriptVersion:positiveInt,commands:AdvCommand[] }
AdvCommand.kind = background | actor | hideActor | cg | hideCg | line | sound | end
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

commandId／lineIdはscript内一意、textIdはUTF-8日本語本文辞書へ対応。line本文は空禁止。定義に未知kind、欠落参照、end後命令があると再生を拒否する。初回は分岐・選択肢・任意の進行変更命令を実装しない。将来選択肢を追加する場合は別版で遷移と既読を定義する。

人物の素材解決は`HeroDisplaySet { heroineId, outfitId, standingAssetId, expressionAssetById, poseAssetById }`と`AdvLayout { layoutId, actorSlots:[{slotId,anchor,pivot,size01,drawOrder}] }`を使う。actor命令の全参照を開始前に検証する。表情未登録は同じ衣装の通常表情へ戻す。pose未登録は用途名付き仮表示と警告。通常表情やstandingの必須欠落は正式受入れ不合格とする。lineのspeakerIdはheroineIdまたは定義されたspeakerIdに対応し、地の文では省略する。

未読lineの全文表示後、次へ進む入力またはオート送り確定でlineIdを既読化する。早送りクリック1回目は全文表示、2回目が進行。scene読了とイベント報酬はend到達時に一括確定する。既読スキップは既読lineのみを送り、表示命令を省略して背景・人物状態を壊さない。

中断時は既読line集合を保持しsceneを読了にしない。再入場は冒頭から、途中の命令indexは保存しない。回想は進行のread-only sessionで、既読追加も報酬も行わない。会話速度・オート時間・フェード時間は設定／presentationデータでTBD。

## 9. 保存と移行契約

既存CampaignSaveV2へ追加するか別版へ移すかはローカルの現在形式確認後に確定する。新しい自由座標・ADV既読を使う場合は次の論理項目を版管理する。

```text
SaveEnvelope { schemaVersion:positiveInt, contentVersion:string, saveId:Id,
               revision:nonnegativeInt, payload:ProgressPayload }
ProgressPayload additions {
 furniturePlacements:Placement[], occupants:Occupant[],
 readLineKeys:[{sceneId:Id,scriptVersion:positiveInt,lineId:Id}],
 unlockedEventIds:Id[],readEventIds:Id[],claimedRewardIds:Id[]
}
```

既読はsceneId＋scriptVersion＋lineIdの複合キー。本文差替時の既読維持はID対応表で判断し、indexだけで移行しない。報酬IDはbattle／event／gachaの種別付き安定IDで保存する。

一時ファイルへ全payload書込み→再読込検証→正常現行をbackupへ保全→現行を置換。失敗時はメモリ進行も保存前状態へ戻し、同じtransactionIdで再試行する。未知の将来版は上書き拒否し元ファイルを保持。未知家具／人物は保持して未配置へ退避し通知する。

旧固定スロットからの変換はslotId→gardenId／zoneId／接地座標の明示表を用意する。変換表がない版は推測で移行せず、旧版の保持／バックアップ復旧を提示する。今回の文書更新では既存セーブの版番号を変更しない。

## 10. 契約受入試験

| ID | 入力・条件 | 合格条件 |
| --- | --- | --- |
| C-T01 | A攻撃→B成功→C支援→D攻撃 | 長さ1→2→0→1、追加行動なし |
| C-T02 | 同じ人物再登場／5人完了後 | 新しいチェイン1 |
| C-T03 | 同じ受付で再描画・取消・ヘルプ | modifierBatchと乱数位置不変 |
| C-T04 | 部位破壊後の新コマンド／詠唱予約 | 新規UIは本体、予約は不発・資源返還なし |
| C-T05 | 最終攻撃前にコア勝利確定 | 確定済み演出を順次再生、最後に結果、報酬一度 |
| C-T06 | resultPending中に撤退／スキップ | 撤退不可、スキップで結果と報酬不変 |
| C-D01 | 4／6部位、欠落参照、重複ID、非有限座標 | 正常だけ読込、不正の場所と理由を通知 |
| C-G01 | 接地y違い／辺接触／面積重なり | y順描画、辺接触可、重なり拒否 |
| C-A01 | 未読line全文表示→中断→再入場 | 進行確定済み行だけ既読、冒頭から、scene未読 |
| C-A02 | 回想・既読スキップ・end反復 | 状態を再構成、二重報酬なし、回想は既読不変 |
| C-S01 | 保存失敗／将来版／未知家具 | 元進行とファイル保全、勝手な変換・上書きなし |

正式確率や描画尺の調整前でも、仮データで上記契約は検証できる。試験の合格記録はローカル実装後に追加する。
