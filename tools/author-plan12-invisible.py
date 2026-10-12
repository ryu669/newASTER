"""Append Invisible to the accepted first batch without rewriting earlier content."""
import copy, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'game/unity/Assets/Game/Resources'
OUT=ROOT/'game/unity/Assets/Game/ContentDrafts/Invisible'
HERO='heroine.invisible'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def remap(d):
    if isinstance(d,str):return d.replace('heroine.shiva',HERO)
    if isinstance(d,list):return [remap(x) for x in d]
    if isinstance(d,dict):return {k:remap(v) for k,v in d.items()}
    return d
CHAPTERS=[
('空いた椅子の返事','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を読む席で、ゆにこは空いた椅子を少し横へ寄せた。そこにも誰かが座るのだと言う。',
'あなたが椀を二つ置くと、彼女は三つ目を頼まなかった。友達はお茶を飲むより、湯気を追う方が好きらしい。',
'謳には、誰にも見えない客のために門を開け続けた人がいた。最後の頁にも、客の姿は記されていない。',
'「指揮官には、見えないんだよね」。ゆにこは確かめるように尋ねた。あなたは見えるふりをせず、彼女の話を聞きたいと答えた。',
'彼女は椅子の背へ青いリボンを結んだ。友達がここにいると知らせる目印で、誰かを試すためではない。',
'風が吹き、栞が床へ落ちた。ゆにこは空いた席へ何か言い、あなたと同時に手を伸ばして笑った。',
'謳の客が本当にいたかは、今も分からない。あなたは今日の記録へ、三人分の場所を空けて読んだと書いた。',
'「ボクの話、途中でやめなくてもいい？」。頷くと、ゆにこは友達の好きな星の形を指で描いた。',
'あなたには見えないままでも、彼女が楽しそうに話す理由は少しずつ分かった。問いかけると、答えを考える声が弾む。',
'椅子を片付ける前に、ゆにこは明日もこの席で読もうと言った。見えるものだけを並べるより、話せる場所を一つ増やす約束になった。']),
('退却路に置く灯り','colossus.black-smoke-citadel',[
'黒煙城塞の謳には、門を守った者の名が並ぶ。ゆにこは、帰ってきた人の名はどこにあるのかと尋ねた。',
'あなたは後ろの頁を開いたが、そこには門の図しかなかった。彼女はしばらく、その図を見ていた。',
'庭の避難訓練で集合の声を頼まれた。ゆにこは杖を握り、声を出す前に道の端へ灯りを置いた。',
'歩く速さの違う人が集まり、列はすぐに伸びた。彼女は先頭へ急がず、最後の人へ声をかけた。',
'大きな力を見せれば従ってもらえるのかと、ゆにこは小さく言った。あなたは、今の灯りで道が分かったと答えた。',
'曲がり角で一人が迷った。彼女は叱らず戻り、次は目印をここにも置こうと一緒に確かめた。',
'謳にない帰り道を、二人で紙へ描き足す。勝った話の下にも、帰った人数を書く欄ができた。',
'「怖かったって言っても、次を任せてもらえるかな」。あなたは頷き、困った場所も聞かせてほしいと頼んだ。',
'訓練の終わりに、ゆにこは皆の返事を一人ずつ待った。強い声ではなかったが、最後まで数を間違えなかった。',
'灯りを片付ける手をあなたが手伝う。ゆにこは今度も一緒に帰ろうと言い、杖より先に荷物を半分預けた。']),
('明日の色を選ぶ','colossus.sky-tower-machine',[
'塔の謳に、空の色を一つに決めた人々がいた。ゆにこは机へピンクと青の糸を置き、どちらも好きだと言う。',
'庭で使う目印を作っていた。遠くから見えることと、見た人が笑えることを両方叶えたいらしい。',
'あなたが青を指すと、彼女はピンクを隣へ重ねた。一つだけに決めなくても、結び方はある。',
'最初の結び目は大きすぎ、風ですぐ裏返った。ゆにこは失敗作を隠さず、あなたへ持っていてと頼んだ。',
'二人で細い糸へ替えると、星の形がはっきりした。空いた椅子へ話しかける声も、今日は途切れなかった。',
'謳の塔は違う色を見つけるたび窓を閉じた。ゆにこは窓を開け、できた星を昼の光へかざした。',
'あなたが似合うと伝えると、誰にと尋ね返された。ゆにこの色だと答えると、指先が少し止まる。',
'「ボクも、明日の予定に入れてね」。仕事がある時だけでなく、お茶を飲む時にも呼んでほしいという。',
'あなたは二人分の時刻を紙へ書いた。ゆにこはその脇に小さな星を描き、友達の場所も忘れなかった。',
'できた目印が庭へ揺れる。ゆにこはあなたの隣で次の色を考え、帰り際には自分から手を差し出した。'])]
EVENTS=[
('星の栞をひとつ','庭の机で、ゆにこは青いリボンを折っていた。|本へ挟む星を作るのだと、あなたにも端を持たせる。|力を入れ過ぎると曲がるから、と指で細く示した。|「指揮官、上手。ボクより星っぽいかも」|あなたが笑うと、彼女は自分の少し丸い星も並べた。|違う形なら本を取り違えないねと、嬉しそうに言う。|空いた椅子へ何か話し、次の色をあなたへ尋ねた。|読みかけの頁には、それぞれ違う星が挟まった。'),
('書き直した宛名','読書室で、ゆにこが封筒を裏返した。|昔の部隊へ送る手紙ではなく、今日のお礼だという。|あなたが椅子を引くと、宛名をもう一度確かめた。|「指揮官へ、でいい？　役目じゃなくて、ちゃんとあなたに」|あなたは受け取りたいと答え、書き終えるまで待った。|彼女は昨日の小さな失敗も、助かったことも書いた。|封を閉じる手はまだ迷ったが、隠すようには握らなかった。|渡した後、次は返事を一緒に読んでよと笑った。'),
('見つけてくれた人','月の明るい橋で、ゆにこは杖を手すりへ立てかけた。|今日は集合の合図ではないと、先にあなたへ言う。|声を探して黙った後、あなたの顔をまっすぐ見た。|「ボク、指揮官が好き。任務がなくても、隣にいたい」|あなたも好きだと答え、恋人として一緒にいたいと伝えた。|ゆにこは返事を聞き直さず、嬉しそうに目を細めた。|つないでよいかと尋ねてから、そっと手を伸ばした。|帰る道でも友達の話は続き、その途中へ二人の明日の約束が加わった。'),
('同じ鉢にはしないで','温室で、ゆにこはピンクと青の花を見比べていた。|あなたが並べようとすると、土の好みが違うらしいと言う。|鉢を分け、置く場所だけ隣にしてみた。|「一緒にいるって、全部そろえることじゃないんだね」|あなたは恋人の手へ小さな札を渡し、名前を書いてもらった。|彼女は自分の字の横へ、あなたにも日付を書かせた。|水やりの順番を決めると、明日も会えるねと笑う。|窓を閉める前に、二人はそれぞれの鉢をもう一度確かめた。'),
('集合より早い朝','夜明けの庭で、ゆにこは両手で椀を温めていた。|あなたを見ると、空いていた隣の席を少し拭く。|集合にはまだ早いけれど、待ちたかったという。|「ボクが呼ぶ前に来てくれたの、嬉しいな」|あなたが肩を寄せると、彼女も静かに体を預けた。|見えない友達は今日は向こうで遊んでいるらしい。|お茶が冷めるまで、二人は明日の予定を少しずつ話した。|誰かを集める声を出す前に、ゆにこは恋人の名前を優しく呼んだ。')]
def build():
    combat=read(RES/'Combat/battle-plan12-first-batch.json');story=read(RES/'Story/plan12-first-batch-story-content.json')
    h=remap(copy.deepcopy(next(x for x in combat['heroines'] if x['id']=='heroine.shiva')))
    h.update(name='インヴィジブル',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,secondTraitId='miracle-body',reactionStyleMain='reserved',reactionStyleSub='affectionate',interactionTraitIds=['taste.flowers','taste.sweets','personality.shy'])
    combat['heroines'].append(h)
    template=next(s for s in combat['skills'] if s['id']=='heroine.aeon.1')
    for i,(name,power,wait) in enumerate([('キラキラヒーリング',1.3,75),('マジカルミラクルレインボー',1.7,110),('プリティドリームシャワー',1.3,155)],1):
        s=remap(copy.deepcopy(template));s.update(id=f'{HERO}.{i}',sourceSkillId=f'{HERO}.{i}',ownerId=HERO,slot=i,name=name,sourceFile='016戦闘_インヴィジブル.mkv',sourceSecond=15,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',powerScale=power,recoveryPercent=wait,partScale=1,resourceCost=0,baseHealing=0,castPercent=0,conditions=[],selfEffects=[],statusEffects=[],ignoreDefenseBp=0,criticalBonusBp=0)
        if i!=2:
            s.update(effectRuleId='effect.heal',targetRuleId='target.selected-allies' if i==1 else 'target.all-living-allies',targetCount=1 if i==1 else 5,chainEligible=False,damageCap=0,damageType='',attributes=[],selfEffects=[dict(kind='attack',percent=20,turns=2 if i==1 else 3),dict(kind='magic-defense',percent=40 if i==1 else 30,turns=2 if i==1 else 3)])
        else:s.update(effectRuleId='effect.damage',targetRuleId='target.selected-enemy',targetCount=0,chainEligible=True,damageCap=10000,damageType='magic',attributes=['刺突','光'],enemyAttackReductionPercent=15,enemyAttackReductionTurns=3,selfEffects=[dict(kind='speed',percent=15,turns=2)])
        combat['skills'].append(s)
    chain=remap(copy.deepcopy(next(c for c in combat['chainActions'] if c['heroineId']=='heroine.shiva')));chain.update(powerScale=.6,damageType='magic',attributes=['光']);combat['chainActions'].append(chain)
    combat['contentReferences'] += [remap(copy.deepcopy(r)) for r in combat['contentReferences'] if r['ownerId']=='heroine.shiva']
    extra=dict(schemaVersion=story['schemaVersion'],contentVersion=story['contentVersion'],provenance='newaster-original',chapters=[],events=[])
    for ci,(title,source,pages) in enumerate(CHAPTERS,1):
        src=next(c for c in story['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
        poems=[dict(id=f'{HERO}.collection.poem.{(ci-1)*6+pi:02}',text=body.split('。')[0]+'。',body=body,sourcePoemId=src['poems'][pi-1]['id'],reason=f'「{src["poems"][pi-1]["text"]}」を受け、{title}で本人の選択と暮らしを描く。') for pi,body in enumerate(pages[1:7],1)]
        extra['chapters'].append(dict(id=f'{HERO}.poem-chapter.{ci}',ownerId=HERO,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
    for ei,(title,body) in enumerate(EVENTS):
        extra['events'].append(dict(id=f'{HERO}.event.{ei}',ownerId=HERO,title=title,cgResourcePath=f'Illustrations/invisible-event-{ei}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][ei],establishesLover=ei==2,paragraphs=body.split('|'),expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
    story['chapters']+=extra['chapters'];story['events']+=extra['events']
    assert len(combat['heroines'])==21 and all(len(c['pages'])==10 for c in extra['chapters'])
    write(OUT/'combat.json',combat);write(OUT/'story.json',story);write(OUT/'invisible-story.json',extra)
    manifest=OUT/'manifest.json'
    if not manifest.exists():write(manifest,dict(schemaVersion=1,available=False,stage='dedicated-art-and-validation-in-progress',formIds=[HERO],baselineForms=20,targetForms=21,requiredAssetsPerForm=21,sourceAdaptations='Lv7の単体回復130%を基準。単体の攻撃40/魔防80%を20/40%へ、全体160%・6ターンを130%・3ターンへ調整。回避3回は本作独自の速度15%・2ターンへ変更。弱体は15%・3ターン。固有回復+10%は維持。ジョブマスタリーは重複のみ。'))
    lines=['# インヴィジブル — 収録本文（本作オリジナル）','']
    for c in extra['chapters']:lines += ['## '+c['title'],'']+c['pages']+['']
    for e in extra['events']:lines += ['## '+e['title'],'']+e['paragraphs']+['']
    (ROOT/'docs/story-text/plan12-invisible.txt').write_text('\n\n'.join(lines)+'\n',encoding='utf-8')
    print('INVISIBLE_AUTHORED 3 skills / 3 chapters / 30 pages / 18 poems / 5 events; staged only')
if __name__=='__main__':build()
