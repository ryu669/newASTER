# アナイアレイター — 本作の物語制作用人物資料

更新：2026-10-10。対象は実装済み人物。本作採用設定と収録本文の要約。原作の逐語再現ではない。
人物ID：`heroine.annihilator`。別衣装は同一人物として性格・好感度・恋人関係を継続する。

## 概要・性格

破壊の力と過去を抱える黒翼の天使。用心深く責任を集めやすいが、植物の世話は細やか。本作では人に任せることを覚える。

## 口調

一人称「私」、主人公の基本呼称「指揮官」。落ち着いた丁寧語で短く答える。照れは作業の話へ戻す。花の話では量や待つ時間を具体的に話す。
主人公の基本呼称は「指揮官」。敬称・親しさによる変化は性格と場面に合わせる。地の文の「あなた」と主人公を呼ぶ台詞は区別する。収録台詞の確認例は現行本文の引用として保持し、台本改稿時に前後の文脈に合わせて見直す。

## 葛藤・関係の進め方

過去を忘れず、力を使わずに守る仕事も選ぶ。聖夜は温もりを渡す同じ人物。

## 避ける描写

聖夜を陽気な別人格にしない。過去の赦しを強要しない。チャージは本人固有で、ファイターの球と混同しない。

## 実装形態と収録物語

### アナイアレイター

形態ID：`heroine.annihilator`。ジョブ：`job.fighter`。交流特性ID：`taste.flowers`、`personality.cool`、`appearance.horns`。
交流タグは候補抽選用の分類であり、性格や物語の全てを定義しない。

- **黒翼の下の花壇**（`heroine.annihilator.poem-chapter.1`）：万物の書から現れた天使は、黒い翼で自分の輪郭を隠した。鬼面の傍らに赤い花が揺れる。「アナイアレイター。それだけ覚えれば十分です」。主人公が差し出した手を取らず、彼女は庭の端に立った。
- **刃を置く場所**（`heroine.annihilator.poem-chapter.2`）：銀海鯨の記憶を還した庭に、潮の匂いが混じった。水位が上がると、彼女は鎌で流れを切ろうとする。主人公が待ってと呼んだ瞬間、刃の赤い光が根の傍らを走った。
- **帰るための曼珠沙華**（`heroine.annihilator.poem-chapter.3`）：空塔機関の振動が、庭の花を一斉に揺らした。彼女の鎌は呼応し、さらに強い力を欲しがる。主人公の前で翼を広げた彼女は、戦いの後に戻る場所を初めて見渡した。

交流・回想：

- 好感度1：花の名を教えて（`heroine.annihilator.event.0`）
- 好感度5：雨宿りの鬼面（`heroine.annihilator.event.1`）
- 好感度10：刃より近い約束（`heroine.annihilator.event.2`）／恋人成立
- 好感度15：恋人の湯気（`heroine.annihilator.event.3`）
- 好感度20：夜明けの花守り（`heroine.annihilator.event.4`）

収録台詞の確認例（本作オリジナル。発話者は前後の本文で確認）：

- 「踏まないでください。まだ根が浅いので」— `heroine.annihilator.event.0`
- 「思い出せたら、話します」— `heroine.annihilator.event.1`
### アナイアレイター（聖夜）

形態ID：`heroine.annihilator-holy`。ジョブ：`job.healer`。交流特性ID：`taste.flowers`、`personality.cool`、`outfit.christmas`。
交流タグは候補抽選用の分類であり、性格や物語の全てを定義しない。

- **黒翼の贈り物**（`heroine.annihilator-holy.poem-chapter.1`）：庭に聖夜の灯りを飾る日、彼女は赤と白の衣装で現れた。角の飾りに小さな鈴が鳴る。「服が変わっただけです。別人のように扱わないで」。主人公はいつもの水器を渡した。
- **憎しみの傍らの灯り**（`heroine.annihilator-holy.poem-chapter.2`）：聖夜の準備に使う香辛料の匂いが、彼女の古い記憶を刺激した。かつて傷つけられた土地で食べた料理の匂いだ。主人公が鍋を遠ざけると、彼女はまだ捨てないでと言った。
- **雪解けの祝福**（`heroine.annihilator-holy.poem-chapter.3`）：空塔機関の記憶が寒い夜へ振動を伝えた。仲間を癒す光を放ち続けた彼女は、指先が冷えていることに気づかなかった。主人公が手を取ると、鈴の音が止まった。

交流・回想：

- 好感度1：包みの中の種（`heroine.annihilator-holy.event.0`）
- 好感度5：灯籠を直す手（`heroine.annihilator-holy.event.1`）
- 好感度10：黒翼の聖夜（`heroine.annihilator-holy.event.2`）／恋人成立
- 好感度15：香辛料の晩餐（`heroine.annihilator-holy.event.3`）
- 好感度20：春まで残す鈴（`heroine.annihilator-holy.event.4`）

収録台詞の確認例（本作オリジナル。発話者は前後の本文で確認）：

- 「こちらを持ってください」— `heroine.annihilator-holy.event.0`
- 「私が笑えない夜にも、来ますか」— `heroine.annihilator-holy.event.1`

## 参考資料・採用判断

人物・戦闘動画の指定元：`D:/M02_gamemovie/ANGELICAASTER/天使`、`D:/M02_gamemovie/ANGELICAASTER/天使戦闘`。映像から断定できない話し方・関係の進め方は、本作の収録本文との整合を優先した独自設定として上記を採用する。原作未確認を本作の仕様未決定と混同しない。

- [時刻付き観察・意匠・戦闘資料](../references/characters/annihilator.md)
- [実装済み全人物の索引](README.md)
- 収録本文：`game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json`。
- 実行定義：`game/unity/Assets/Game/Resources/Combat/battle-plan11-7.json`。

## 次の物語を作るとき

上記の性格・口調と既読の関係段階を出発点にする。新しい事件、本人の具体的な選択、相手の応答、日常へ残る変化を決める。衣装やタグの変更だけで人物像を変えない。追加した設定・本文ID・根拠を本資料にも追記する。
