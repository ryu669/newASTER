# ADV実行・既読・中断・回想

更新：2026-10-02。状態：現行仕様。版はGit履歴で管理し、ファイル名に版番号を付けません。仕様整理のみで実装完了を意味しません。

## ADV命令と既読

commandId／lineIdはscript内一意、textIdはUTF-8日本語本文辞書へ対応。line本文は空禁止。定義に未知kind、欠落参照、end後命令があると再生を拒否する。初回は分岐・選択肢・任意の進行変更命令を実装しない。将来選択肢を追加する場合は別版で遷移と既読を定義する。

人物の素材解決は`HeroDisplaySet { heroineId, outfitId, standingAssetId, expressionAssetById, poseAssetById }`と`AdvLayout { layoutId, actorSlots:[{slotId,anchor,pivot,size01,drawOrder}] }`を使う。actor命令の全参照を開始前に検証する。表情未登録は同じ衣装の通常表情へ戻す。pose未登録は用途名付き仮表示と警告。通常表情やstandingの必須欠落は正式受入れ不合格とする。lineのspeakerIdはheroineIdまたは定義されたspeakerIdに対応し、地の文では省略する。

未読lineの全文表示後、次へ進む入力またはオート送り確定でlineIdを既読化する。早送りクリック1回目は全文表示、2回目が進行。scene読了とイベント報酬はend到達時に一括確定する。既読スキップは既読lineのみを送り、表示命令を省略して背景・人物状態を壊さない。

中断時は既読line集合を保持しsceneを読了にしない。再入場は冒頭から、途中の命令indexは保存しない。回想は進行のread-only sessionで、既読追加も報酬も行わない。会話速度・オート時間・フェード時間は設定／presentationデータでTBD。

シナリオ命令の項目・型は[共通データ契約](../data/contracts.md)。好感度イベントと報酬条件は詩・好感度仕様を参照する。
