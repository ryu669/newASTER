# 計画6の統合保存対応

更新：2026-10-03。6-1の追加契約。実装と検証結果は計画6の進捗記録に記載する。

ファイルは既存のformal-campaign-v1.json。envelope v1、world v2、growth v2を保持し、任意のhome領域（version=1、contentVersion=home-fixture-2026-10-03）を追加する。正式コンテンツ版への変更時は別途移行表を定め、未知版を読み込まない。

| 保存先 | 論理状態 | 欠落時 |
| --- | --- | --- |
| world.poemIds／unlockedStoryIds／readStoryIds | 詩所持／章解放／章読了 | 既存契約を維持 |
| collection.materials／relics | 巨神獣素材／遺物 | 既存契約を維持 |
| growth.heroines | 正式人物の所持・育成 | 既存契約を維持 |
| home.furnitureInstances | 家具所持個体instanceId→defId | 新領域を明示初期化した場合のみ空一覧 |
| home.furniturePlacements | 個体・庭・ゾーン・接地座標・向き | 同上 |
| home.occupants | 人物・庭・位置枠・家具使用参照 | 同上。戦闘編成と独立 |
| home.weaponNodeIds | 取得ノード | 同上。旧weaponBranchesを転用しない |
| home.weaponEquipment | 人物→装備ノード（1枠） | 以前のhome v1で新項目がない場合は空一覧。取得と装備は別 |
| home.affections／loverHeroineIds | ID別好感度／明示恋人状態 | 同上。旧affectionsを転用しない |
| home.unlockedEventIds／readEventIds | イベント解放／読了 | 同上 |
| home.readLineKeys | sceneId＋scriptVersion＋lineId既読 | 同上。実行位置は保存しない |
| home.claimedRewardIds／receipts | 初回報酬権／確定操作記録 | 同上。再描画・再送で重複付与しない |

rootのhome欠落または明示nullは未設定として読み、空配列を持つ明示homeとは区別する。旧保存をロードしただけでは初期化・保存・解放・revision増加を行わない。明示homeの版・内容版・不正状態は検証する。旧world配列と原ファイルはそのまま保持する。

現在の戦闘編成順はCombatDefinitionCatalog.formationを参照する。home.occupantsは別の配置状態であり、庭への配置によって戦闘編成を変更しない。

最初にrootのenvelope版だけを確認し、次にworld／growth／engagement／collection／homeの安定headerを読む。将来版が内部配列を別型へ変更していても、完全payloadのデシリアライズより先に停止し、古いbackupを代用しない。Unityの書込でもnullの任意領域が既定オブジェクトに変わらないよう正規化する。破損は既存の確認付き復旧へ接続する。

home操作は共通FormalCampaignJournalを使用する。複製上のプレビューと確定を分離し、成功時だけ全体revisionを1増やす。失敗候補は共通pendingへ保持し、保存中／pending中は別操作を拒否する。同一ID・種別・開始revisionの再試行は候補を再構築せず保存する。6-1のhome専用境界はworld／growthを変更できない。6-3以降の型付きHomeOperationは、定義費用とhome変更を全体候補へ組み込み、素材・取得・配置・交流を原子的に確定する。操作payloadの区切りはescapeし、異なるIDの組が同じsignatureにならない。

アニメーション、本文表示途中、auto状態、家具使用の途中タイマーは保存しない。endは読了・初回報酬・恋人成立・後続解放・残る行既読を一括確定する。fixtureは独立検証スロットで使用し、正式本文として通常の読書を有効化しない。

## 6-3以降の検証設定

装備は人物ごとに1枠。取得は他枝を閉鎖せず、装備した1ノードの攻撃加算と武器スキルだけを次戦闘へ適用する。検証用武器スキルは通常攻撃（slot 0）を置換し、その待機時間・資源・チェイン率・速度を維持する。既存の職業スキル2枠は維持する。無装備も選べる。能力と倍率はfixture値であり正式経済ではない。

座標は小数6桁に丸めた正規化値を比較する。辺接触を許し正面積重複を拒否する。家具費用・交流費用は明示巨神獣素材のみ。交流のfixtureは素材1で好感度1、時間待機なし。通常保存では未制作本文の読了を付与しない。検証本文の操作は独立した試験セーブを使用する。

通常セーブはformal-campaign-v1.json。機能検証セーブは同じ保存形式の別ファイルplan6-home-trial-v1.json。本の「計画6の機能検証用セーブを開く」で切り替え、通常へ戻ると開く前の通常snapshotへ戻る。通常の進行へfixtureの家具・装備・好感度・技術本文を混用しない。検証スロットの初期素材100／素材種、2庭開放は試験準備であり討伐報酬ではない。

行既読はsceneId＋scriptVersion＋lineId。過去版のキーは履歴として保持し、新版には適用しない。保存が現在のscriptVersionより新しい場合は拒否。本文改版に合わせた旧版の章読了・報酬の扱いは正式コンテンツ版の移行表で定める。文字速度15／30／60／120は表示用PlayerPrefs、auto待ち1秒・fixture fade200ms。バックログ・ヘルプ・非アクティブ時にauto／skipと音を休止し、手動復帰。ADVは計画4の報酬用アクティブ時間へ加算しない。
