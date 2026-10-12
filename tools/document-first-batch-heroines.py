"""Synchronize authored profiles and development index without releasing incomplete forms."""
import importlib.util,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('batch_author',ROOT/'tools/author-plan12-first-batch.py');author=importlib.util.module_from_spec(spec);spec.loader.exec_module(author)
profiles={
'iron-maiden':('永遠教会の修道女。務めと信仰を大切にし、アイオーンへの敬愛を強く持つ。相手を守ろうとする責任感が、戸を閉めてすべてを引き受ける行動に偏りやすい。本作では鍵を預けること、食事の席へ戻ること、役目ではなく名前で呼ばれることを通して、隣に人を迎える。','アイオーンとの関係は、一方向の敬愛に加えて本人からの信頼を保つ。ヴァイパーとは不仲でも共闘できる。主人公との恋愛で教会の関係を消さない。','一人称は「私」。落ち着いた丁寧な言葉を使い、親しい場面では短い冗談や照れを見せる。信仰を他人への説教や無条件の正しさへ置き換えない。'),
'avalanche':('借りと返礼を忘れない、機転の利く天使。軽口を交えながら状況を見極め、勝つ側を選ぶ器用さを持つ。本作では帳面で恩を精算する癖から、食事や次の約束を楽しむ関係へ進む。仲間を置いて先に着くより、一緒に帰る判断を自分で選ぶ。','原典では105に救命された恩がある。105の人物ID、投薬内容、林檎と天使化の因果は未確定として保持する。105とアヴァランチを同一人物にしない。','一人称は「私」。くだけた短い言葉と軽口を使う。恩返しの話を取引だけにせず、反応を確かめる間や照れを残す。'),
'anathema':('誇り高く華やかな振る舞いと、菓子や食事を楽しむ素直さを持つ天使。本作では失った席へのこだわりを、今の暮らしと自分で交わす約束へ結び直す。強さを誇示するだけでなく、隣の人の疲れに気づいて剣を置く行動を描く。','アイコノクラストとの異世界の同行は、アナテマの夢に現れる時点を区別する。デウスとの菓子作りの一件を恋愛関係へ広げない。','一人称は「私」。令嬢らしい整った言葉を基本に、驚きや食事の場面では感情が先に出る。高慢さだけで固定せず、約束を守る誠実さを具体的な行動で示す。'),
'shiva':('明るい声と行動で周囲を励ます天使。本名は柴まりか。本作では休む人の返事を待つことや、誰かに元気を分ける自分も休むことを覚えていく。助けられるだけの人物にせず、何を望むかを自分の言葉で伝える。','尾頭懐音との救助・救済と深い親愛を保つ。1周年の物語で描かれた過干渉への抗議と謝罪も関係の一部として扱う。まりかを死亡扱いにせず、主人公へ懐音の役割を置き換えない。','一人称は「あたし」。率直で明るい口調。励ましを連呼するだけにせず、迷いを認め、休む提案や相手の意思を聞く台詞も持たせる。')}
index_path=ROOT/'docs/characters/index.json';index=json.loads(index_path.read_text(encoding='utf-8'))
for p in author.PEOPLE:
    if any(e['personId']=='heroine.'+p['key'] for e in index['people']):continue
    hero='heroine.'+p['key'];overview,relations,voice=profiles[p['key']]
    lines=[f'# {p["name"]}の人物設定と物語','',f'2026-10-11。人物・形態IDは `{hero}`。本名は{p["realName"]}。データ・専用画像を接続済み、通常プレイへの公開判定は実行版確認記録に従う。','', '## 概要・性格','',overview,'','## 口調','',voice,'','## 関係と採用上の注意','',relations,'','[動画の時刻付き関係記録](../production/heroine-relationships.md)を原典の根拠とし、下記の章・交流は本作オリジナルとして分離する。成人として描き、原典から現在年齢を推測しない。','', '## 戦闘と成長','',f'ジョブは `job.{p["job"]}`。固有特性は「{p["innate"]}」、能力は「{p["personal"]}」（{p["personalText"]}）。神器名は「{p["weapon"]}」。ジョブのマスタリー強化はキャラ重複のみ。装備の木は13ノード、3ルート4段階＋初期の根。ノード名は特性名と重複させず、武器名とルート名に段階Ⅰ〜Ⅳを付ける。','',f'特性フレーバー：{p["flavor"]}','', '| スキル | 基本威力 | 待機倍率 |','| --- | --- | --- |']
    lines += [f'| {name} | {power*100:g}% | {wait}% |' for name,power,wait in p['skills']]
    lines += ['', '数値・効果の細部と原典からの変更は開発用manifestと戦闘カタログを正とする。動画の名称を参考にした本作独自の技・効果を含む。','', '## 収録章と交流','']
    lines += [f'- 第{i}章「{title}」：10ページ。{pages[0]}' for i,(title,source,pages) in enumerate(author.CHAPTERS[p['key']],1)]
    lines += [f'- 交流{i}「{title}」：好感度{[1,5,10,15,20][i]}、8段落。'+('告白と返事を経て恋人成立。' if i==2 else '') for i,(title,text) in enumerate(author.EVENTS[p['key']])]
    lines += ['', '## 画像・実装記録','',f'専用立ち絵・表情・戦闘絵・SD7姿勢・イベント絵5枚を使用。イベント絵に男性は登場させない。装備の木は自然木に小さな専用装飾を分散し、薄い葉群とノード横の花を開放に応じて追加する。根は開花対象外。','',f'[素材の用途・実画面レビュー](../production/plan12-first-batch-asset-review.json)、[装備の木の構成とハッシュ](../production/plan12-{p["key"]}-tree-bundle.json)を個別に管理する。確認済みの項目は素材・表示処理に変更がない限り繰り返さない。','']
    (ROOT/'docs/characters'/(p['key']+'.md')).write_text('\n'.join(lines),encoding='utf-8')
    entries=index.setdefault('developmentPeople',[])
    if not any(e['personId']==hero for e in entries):entries.append(dict(personId=hero,name=p['name'],profile='docs/characters/'+p['key']+'.md',formIds=[hero],combatSource='game/unity/Assets/Game/ContentDrafts/FirstBatch/combat.json',storySource='game/unity/Assets/Game/ContentDrafts/FirstBatch/story.json',manifest='game/unity/Assets/Game/ContentDrafts/FirstBatch/manifest.json',available=False))
index_path.write_text(json.dumps(index,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('FIRST_BATCH_PROFILE_DOCUMENTS_PASS four profiles; development index; no inferred release')
