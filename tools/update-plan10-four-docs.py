"""Refresh current documentation; retain historical release/validation records."""
import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[1];D=R/'docs';RES=R/'game/unity/Assets/Game/Resources'
def read(p):return p.read_text(encoding='utf-8-sig')
def write(p,t):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(t.rstrip()+'\n',encoding='utf8')
def section(p,text):
 t=read(p);start='<!-- plan10-current:start -->';end='<!-- plan10-current:end -->';block=start+'\n'+text.strip()+'\n'+end
 if start in t:t=re.sub(re.escape(start)+r'.*?'+re.escape(end),lambda _:block,t,flags=re.S)
 else:
  at=t.index('\n\n');t=t[:at]+ '\n\n'+block+t[at:]
 t=re.sub(r'更新：2026-10-0[2345]。状態：現行','更新：2026-10-07。状態：現行',t,count=1);write(p,t)
c=json.loads(read(RES/'Combat/battle-plan10-shangrila.json'));s=json.loads(read(RES/'Story/plan10-shangrila-story-content.json'))
assert (len(c['heroines']),len({h.get('personId') or h['id'] for h in c['heroines']}),len(c['jobs']))==(15,12,13)
assert (len(s['chapters']),sum(len(x['poems']) for x in s['chapters']),len(s['events']))==(90,630,75)
forms=[('slayer-swim','スレイヤー（水着）','056','スレイヤー水着','job.general','heroine.slayer','swim',12,10,11),('arcane','アルケイン','012','アルケイン','job.gunner','heroine.arcane','normal',13,11,11),('arcane-academy','アルケイン（学園）','013','アルケイン学園','job.gambler','heroine.arcane','academy',14,11,12),('shangrila','シャングリラ','050','シャングリラ','job.sniper','heroine.shangrila','normal',15,12,13)]
roster=D/'production/plan10-heroine-roster.json';data=json.loads(read(roster));data['contentVersion']='plan10-2026-10-07';data['currentCounts']=dict(forms=15,people=12,jobs=13,chapters=90,poems=630,events=75)
for key,name,no,source,job,person,variant,*counts in forms:
 entry=dict(id='heroine.'+key,personId=person,variantId=variant,referenceName=name,jobId=job,stage='implemented',available=True,profile=f'docs/references/characters/{key}.md',sourceIds=[f'angelica-battle:{no}戦闘_{source}.mkv',f'angelica-angel:{no}_{source}.mkv'],unresolved=['参考調査：全ボイスの文字起こしと全詩の精読'],implementation=f'docs/production/plan10-{key}-implementation.md')
 data['heroines']=[e for e in data['heroines'] if e['id']!=entry['id']]+[entry]
write(roster,json.dumps(data,ensure_ascii=False,indent=2))
root=R/'README.md';t=read(root);a=t.index('計画10の現行実装');b=t.index('\n\n計画9の初期5人',a)
t=t[:a]+'''計画10の現行実装は15形態・12人・13ジョブ。スレイヤー水着、アルケイン通常／学園、シャングリラを順に追加し、戦闘・育成・神器・庭・物語へ接続しました。性格・口調・概要を保存し、現行物語は90章630詩・75交流です。編成を「配置 → 隊員の設定 → 入れ替え候補」へ分け、将来のオーパーツ1人1枠を表示。ジョブ資源13種は形の異なる画像へ変更し、画面比率・CG・神器・人物表示を見直しました。

最新の開発ビルドは `game/Builds/plan10-shangrila/newASTER.exe`。[今回の実装とUI確認](docs/production/plan10-four-heroines-and-ui.md)、[ヒロイン追加手順](docs/production/heroine-addition-guide.md)、[計画10](docs/production/plan10-implementation-plan.md)を参照してください。個別オーパーツの装備操作は後続実装です。実行ファイル・元動画・調査画像はGit管理の対象外です。

![追加4形態の実行画面](docs/production/images/four-heroines-game-comparison.jpg)'''+t[b:];write(root,t)
p=D/'README.md';t=read(p);t=re.sub(r'更新：2026-10-05。現行ブランチ：.*?変更版はGitコミットで管理します。','更新：2026-10-07。現行ブランチ：`codex/plan9-production`。計画10は15形態・12人・13ジョブ、90章630詩・75交流を接続済み。現在の入口は[今回の実装](production/plan10-four-heroines-and-ui.md)。計画9 RC1の初期5人は[履歴](production/plan9-completion-status.md)として保存し、追加版と区別します。',t,count=1)
t=re.sub(r'2026-10-06：\[計画10.*?人物実装・提供は未完了。','[計画10](production/plan10-implementation-plan.md)に各ロットの順序と受入れを記録。[追加手順](production/heroine-addition-guide.md)、[人物台帳](production/plan10-heroine-roster.json)、[全体UI確認](production/plan10-ui-validation.json)から制作・検証へ進めます。',t,count=1);write(p,t)
p=D/'production/plan10-implementation-plan.md';t=read(p);t=re.sub(r'更新：2026-10-06。状態：.*?こと。','更新：2026-10-07。状態：今回の4形態追加ロットを実装・受入れ。現在15形態・12人・全13ジョブ。200人超の量産は今後の小ロットで進める。性格・口調・概要を物語制作用に残す方針を継続する。',t,count=1)
t=t.replace('まず未登場8ジョブを対象とする。','最初の段階で未登場8ジョブを対象とし、今回ジェネラル・ギャンブラー・スナイパーまで揃えた。').replace('今回指定の2本','開始時に指定された2本').replace('他ジョブの人物選定は資料照合後に決める。','同ジョブの追加人物は資料照合とロットごとの受入れで増やす。')
t=t.replace('| ガンナー | エクスカリパン | 計画9提供済み |','| ガンナー | エクスカリパン／アルケイン | 通常版アルケイン追加・弾倉操作対応 |').replace('| スナイパー | 未選定 | 資料照合待ち |','| スナイパー | シャングリラ | 指定支援・狙撃詠唱・中断対応 |').replace('| ギャンブラー | 未選定 | 資料照合待ち |','| ギャンブラー | アルケイン（学園） | 3×3 SLOT・全5ライン対応 |').replace('| ジェネラル | 未選定 | 資料照合待ち |','| ジェネラル | スレイヤー（水着） | 指揮官・本人固有5枠強化対応 |')
t=t.replace('次は残り7ジョブの人物選定へ進む。','この段階から他ジョブを順次追加し、現在は全13ジョブへ接続した。').replace('現在は11形態・10人・10ジョブ。残り3ジョブはスナイパー・ギャンブラー・ジェネラル。','ナイトホーク追加時点は11形態・10人・10ジョブだった。')
write(p,t)
section(p,'''## 今回の順次追加（2026-10-07）

スレイヤー水着 → アルケイン → アルケイン学園 → シャングリラの順に、本人専用18画像・3章30ページ・18詩・5交流と各ジョブを受入れた。通常入口は `Combat/battle-plan10-shangrila` と `Story/plan10-shangrila-story-content`。15形態のうち3組の衣装違いがあるため人物数は12人。全13ジョブの資源画像を差別化し、編成の階層・将来のオーパーツ1枠・比率保持を更新した。

[今回の実装](plan10-four-heroines-and-ui.md)、[追加手順](heroine-addition-guide.md)、[人物台帳](plan10-heroine-roster.json)、[全体UI検証](plan10-ui-validation.json)。各段階の数量は履歴として保持する。''')
p=D/'production/post-plan9-jobs.md';t=read(p);t=re.sub(r'継続工程は、.*?将来の追加人数・公開日は未確定。','継続工程は資料照合 → 人物と入手条件 → 固有戦闘・育成・神器 → 美術・詩・交流 → 保存・実行版受入れ。計画10で未登場8ジョブを揃え、現在15形態・12人・13ジョブ。次は同ジョブの人物を小ロットで増やす。[今回の追加とUI改修](plan10-four-heroines-and-ui.md)、[追加手順](heroine-addition-guide.md)、[計画10台帳](plan10-heroine-roster.json)を参照。200人超の量産と公開日は今後の対象である。',t,count=1);write(p,t)
section(D/'production/implementation-plan.md','''## 現行の計画10（2026-10-07）

初期5人の計画9配布候補を基準に追加を継続。今回4形態を順次受入れ、15形態・12人・全13ジョブ、90章630詩・75交流になった。[計画10](plan10-implementation-plan.md)と[今回の実装・UI確認](plan10-four-heroines-and-ui.md)を現行の進捗とする。後続は同ジョブの人物量産と個別オーパーツ装備。以下は各計画策定・検証時点の履歴であり、現在の提供人数や未完了項目を上書きするものではない。''')
blocks={
'systems/battle.md':'''現行入口は `Combat/battle-plan10-shangrila`、15形態・12人・13ジョブ。育成済みの所持人物から5人を選び、v0.2の時間・固有操作を使う。以下の初期5人資源表はv1の記録であり、現行固有操作は[計画10のジョブ反映](../production/battle-v0.2-adoption.md)と段階別JSONを参照する。

- ジェネラル：選択した指揮官1人の固有5枠だけを適用。スレイヤー水着は攻撃15%／物防20%／魔防20%／速度10%／会心10%。通常行動で指揮+3、MAX15で300 Clockの間2倍。パスでは増えず、戦闘不能で効果を止める。
- ギャンブラー：6種を3×3へ一様抽選。3行＋2対角をすべて評価し、2記号で通常・3記号で1.5倍。777の各ラインで3スキルを各2回。重複も全発動。全外れは資源なし・WT0、状態異常の待機は免除しない。通常3スキルを直接選べない。
- スナイパー：指定した味方1人の実際の攻撃へ0.45倍の非再帰支援。支援に資源・通常WTを使わず、自身の通常行動で狙撃+3。MAX15で200%待機の詠唱中は全員支援、開始時の攻撃・会心を保持する5倍弾、発射後150%待機。中断・対象消失で返却や自動対象変更をしない。

学園版の発症中状態延長・正の味方効果延長・本体限定倍率、シャングリラの自己反動・基礎攻撃回復・攻撃低下を共通解決器へ追加。本人のLv7観察を本作Lv1基準へ採用した数値と、創作の待機・抽選・成長を人物資料で区別する。''',
'systems/battle-timing.md':'''SLOTは複数発動を1つのコマンドとして解決し、最大の成功スキル待機で一度だけ予定を進める。全外れ・状態ペナルティなしの時だけ同じREADYを保持。スナイパー詠唱は200%、発射後150%。死亡・スタン・うわの空で狙撃を中断し、対象の部位消失で別対象へ撃ち直さない。指揮強化は300 Clockで失効する。[ジョブ実装](../production/plan10-four-heroines-and-ui.md)と各受入れを参照。''',
'systems/progression.md':'''現行は15形態の育成・スキルLv1〜7・人物固有の神器13ノードを接続。衣装違いは好感度・恋人関係を共有し、育成・神器・読了は形態ごとに保持する。新規4形態の神器は本人の攻撃スキルへ倍率を掛け、元の技名や効果を保持する。

編成は「5人の配置 → 隊員の設定 → 入れ替え候補」。隊員設定に神器1枠、後続のオーパーツ1枠、護衛・指揮官・スナイパー支援・パンツァー設定を分ける。神器から戻ると元の隊員へ戻る。個別オーパーツの装備操作・能力適用・装備保存は後続実装で、既存の収集遺物による補正とは別。''',
'systems/save-and-migration.md':'''FormalHomeProgressに `commanderHeroineId` と `sniperSupports[{heroineId,targetId}]` を追加。旧正式保存で欠落していれば指揮官は自動、支援は既定の隣接枠を使う。HomeOperationの `commander`／`sniper-support` を原子保存し、失敗時は元状態と同じリクエストを保持、成功再試行は一度だけ反映。未所有・違うジョブ・自己支援を拒否し、出撃していない指定は自動へ戻す。

スレイヤー通常／水着、アルケイン通常／学園は同じ人物ID。後から加入した衣装にも確定済みの親愛・恋人関係を同じ保存で引き継ぐ。加入失敗時は関係を公開しない。イベント読了と育成は独立。オーパーツ装備の新保存形式は今回追加していない。''',
'systems/book-navigation.md':'''所持一覧は12形態ずつ、編成候補は10形態ずつ、追加加入は5形態ずつ表示。現在15形態を扱い、後続ページも保持する。編成の三層と神器の往復は、万物の書のしおり・対象・情報面とは別の操作階層。戻る／Escapeは一層ずつ戻り、保存確認中は変更を受け付けない。''',
'systems/garden.md':'''追加4形態の専用SD4状態を人物IDに接続。同じ人物の別衣装を同じ庭へ重ねて配置しない。背景の遠景・中景・前景は同じキャンバス比率で切り出し、人物SDは縦横比を保つ。庭9種・家具10種の数量は今回増やしていない。''',
'systems/collection-and-affection.md':'''現行収録は90章630詩・75交流。追加4形態はそれぞれ3章30表示ページ・18詩・5イベント。人物の好感度・恋人関係を衣装間で共有し、章・イベントの既読は形態別。通常・別衣装を二人として同時編成しない。詩取得単位と長編の表示ページ数は別に保持する。''',
'systems/adv.md':'''追加4形態に30ページと5交流ずつを接続。本文・性格記録・CGを本人と衣装のIDで束ねる。CGは論理画面の `y=120..615` へ比率を保って収め、下の本文に隠れない。表情画像も切り抜き領域の縦横比を保つ。衣装間の関係共有が別衣装の行既読を済ませることはない。''',
'data/contracts.md':'''現行カタログは `battle-plan10-shangrila.json` と `plan10-shangrila-story-content.json`。段階12／13／14形態のJSONは順次受入れの証拠として保持する。`personId`／`variantId`で人物と衣装を分離。`sourceFile`／`sourceSecond`／`observedSkillLevel`／`ruleOrigin`で観察と創作を区別し、通常アルケインの2つの創作技は出典を空にし観察Lv=0とする。

追加効果は `postAttackAlliesEffects`、`enemyStatusExtensionTurns/Kinds`、`alliesEffectExtensionTurns`、`bodyDamageBonusPercent`、自己効果 `attack-reduction`。発症中・正の効果・本体限定の対象境界と数値範囲を検証する。配備は `BattleDeployment` に指揮官と支援対象を渡し、外部の配列や要素変更から防御コピーする。''',
'data/initial-five-heroines.md':'''初期5人の本作独自正式接続は計画9で完了し、計画10は追加形態を含む15形態・12人・13ジョブを扱う。この文書の動画未確認事項・初期実装記録は資料調査時点の履歴。現在の実行定義は `Combat/battle-plan10-shangrila.json`、段階ごとの観察と創作は[計画10人物台帳](../production/plan10-heroine-roster.json)と人物資料を参照。''',
'presentation/battle.md':'''論理1600×900へ均等倍率と中央余白を適用し、4:3や横長の画面でも人物・ボタンの比率を変えない。SLOTは通常スキルカード領域に3×3の結果を表示し、天使を覆わない。固有資源は `UI/Jobs/<job>` の13画像へ分化。指揮・支援・狙撃状態は既存の状態／行動表示へ統合する。''',
'presentation/garden-and-adv.md':'''背景レイヤーを同じScaleAndCropで揃え、人物・表情・CGはScaleToFit／切り抜き領域の比率保持。CGは本文より上の表示域へ収める。追加4形態のSDに人物重複がないことを確認し、学園版SDは検査で見つかった重複をImageGenで再制作した。''',
'presentation/assets.md':'''今回4形態×18点=72点の本人専用PNGを追加。生成元・プロンプトを `game/art-source/plan10/<key>-generation.json`、無加工の採用PNGをart-sourceとResources、SHA-256をProductionAssetAcceptanceへ保存する。参考動画と切り出し資料はGit・配布へ入れない。ジョブ資源13種は既存コードのアイコン体系を拡張した固有形状のPNGで、色替えだけではない。[追加手順](../production/heroine-addition-guide.md)を参照。'''
}
for relative,body in blocks.items():section(D/relative,'## 計画10の現行接続（2026-10-07）\n\n'+body)
p=D/'systems/battle.md';t=read(p).replace('現在の戦闘はLv1固定編成で、育成接続と既存保存移行は計画4。','当時の検証はLv1固定編成だった。現在は育成と所持5人編成を接続している。');write(p,t)
p=D/'systems/progression.md';t=read(p).replace('装備の樹・オーパーツによる補正はまだ接続せず、旧枝や共通装備を正式装備として代用しない。','人物固有の神器補正と収集遺物補正を接続済み。個別オーパーツ1枠の装備は後続実装とし、旧枝や共通装備を本人の神器として代用しない。');write(p,t)
p=D/'requirements/game.md';t=read(p);t=t.replace('更新：2026-10-02。状態：現行仕様。','更新：2026-10-07。状態：現行仕様。',1).replace('状態：現行。実装修正はローカルで後日実施。','状態：確定方針の記録。').replace('今回の更新は要件文書のみで、ファイル撤去は含めない。既存Unity実装の2D化は未実施であり、本書の記載を実装完了とはしない。','この段落の要件整理は2026-10-02時点。現在は計画9の2D化と計画10の追加実装を接続し、実装・受入れ範囲は各記録で示す。');write(p,t)
section(p,'''## 計画10の追加要求と現在地（2026-10-07）

ユーザー指定の4形態を一人ずつ追加し、現在15形態・12人・全13ジョブ。性格・口調・概要と追加方法を文書へ残す。元の意匠と天使の翼・光輪を本人の新規美術へ反映。ジョブ資源は画像自体を差別化する。画像の歪み・位置ずれを画面全体で確認し、編成は今後の1人1オーパーツを見据えて階層化する。今回の個別装備操作は後続とする。200人超の最終目標・将来の公開規模を、この15形態の受入れで完了扱いにしない。''')
section(D/'requirements/presentation.md','''## 今回の画面要求（2026-10-07）

人物・CG・神器の装飾画像を歪めず、異なる画面比率でも論理16:9を保つ。背景レイヤーの切り出しを揃える。ジョブ資源は13種の固有形状画像へ分ける。編成の配置・隊員装備／役割・候補を別階層にし、1人1オーパーツの後続装備を置ける表示枠を用意する。保存確認と戻る経路も含め[全体UI確認](../production/plan10-ui-validation.json)に実行画像を記録する。''')
p=D/'references/README.md';t=read(p).replace('計画10のアーティスト候補。','計画10で正式追加したアーティスト。');write(p,t)
section(p,'''## 今回追加した人物記録

- [スレイヤー水着](characters/slayer-swim.md)：通常版と同じ人物、ジェネラル。
- [アルケイン](characters/arcane.md)：ガンナー、観察1技と創作2技を区別。
- [アルケイン学園](characters/arcane-academy.md)：同じ成人の別衣装、ギャンブラー。
- [シャングリラ](characters/shangrila.md)：スナイパー、休息と自由を描く人物。

各記録に性格・口調・概要・天使と元の意匠・観察スキル・本作の調整・物語方針を残す。全文音声と全詩の精読は調査の未完了項目として保持する。''')
section(R/'game/README.md','''## 現行の計画10ビルド

最新入口は `unity/Assets/Game/Resources/Combat/battle-plan10-shangrila.json`、15形態・12人・13ジョブ。Windows開発版は `Builds/plan10-shangrila/newASTER.exe`。`Plan10ShangrilaBuild.ValidateAndBuild` でビルドし、`../tools/validate-plan10-expanded-roster.ps1` でCore/DataとUnity C#を検証する。[今回の接続とUI](../docs/production/plan10-four-heroines-and-ui.md)、[追加手順](../docs/production/heroine-addition-guide.md)を参照。以下のplayable／初期5人ビルドは当時の記録で、現行配布入口とは別。''')
p=D/'story-text/README.txt';t=read(p);block='2026-10-07 計画10の現行本文は90章630詩・75交流。今回追加した4形態は各3章30表示ページ・18詩・5交流。\n個別原稿：スレイヤー水着の物語.txt、アルケインの物語.txt、アルケイン学園の物語.txt、シャングリラの物語.txt。\n現行収録：game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json。\n初期5人の物語本文.txt／回想本文.txtと以下の原本・生成案内は、計画9の書き出し履歴。\n\n';
if not t.startswith('2026-10-07 計画10'):write(p,block+t)
print('Current roster, README/index, plan, requirements, systems, data, presentation, profiles and story index refreshed.')
