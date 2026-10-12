"""Append-only Viper draft from the accepted Invisible catalog. Never publishes."""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'game/unity/Assets/Game/Resources'
OUT=ROOT/'game/unity/Assets/Game/ContentDrafts/Viper'
HERO='heroine.viper'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):
 p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def remap(x):
 if isinstance(x,str):return x.replace('heroine.excalipan',HERO)
 if isinstance(x,list):return [remap(v) for v in x]
 if isinstance(x,dict):return {k:remap(v) for k,v in x.items()}
 return x
CHAPTERS=[
('余白のある時刻表','colossus.black-smoke-citadel',[
'黒煙城塞の謳を読み終えると、ドロシーは壁の当番表を見上げた。休む時刻まで細かく埋まっている。',
'「飯の時間は助かるけどさ、散歩まで命令されんの？」。彼女は表を破らず、あなたへ空いた紙を求めた。',
'城塞の門番は、鐘が鳴れば必ず同じ場所へ戻る。戻らなかった人の理由は、謳に残っていなかった。',
'あなたは当番の交代が必要だと説明した。ドロシーは頷き、仕事の欄だけ先に書き写した。',
'「そこは分かってるって」。彼女が消したのは、休憩中の席を指定する一行だった。',
'試しに半日だけ変えることにした。彼女は約束の時刻に戻り、銃の整備を引き受けた。',
'庭を歩いていた人から、橋の手すりが緩んでいると聞いたという。表の余白へ修理の印を足す。',
'あなたが次の散歩も頼むと、ドロシーは眉を上げた。「頼まれなくても、見つけたら言うよ」',
'夕方、二人で橋を確かめた。鐘を聞きながら戻る道は、朝より少し遠回りだった。',
'翌日の表には、皆がそれぞれ選ぶ時間が残った。ドロシーはそこへ、あなたとの昼食だけ小さく書いた。']),
('二枚のナプキン','colossus.memory-crystal-dragon',[
'記憶晶竜の謳には、訪れる客ごとに違う顔を見せる主人がいた。ドロシーは器を磨きながら笑った。',
'「愛想よくすりゃ、開く扉もあるしね」。そう言う彼女の指は、縁の欠けた皿を迷わず脇へよけた。',
'あなたは今夜の食事を庭で取ろうと誘った。誰かを迎える練習ではなく、二人の休憩だと添えた。',
'彼女は白い布を二枚用意し、風で飛ばないよう端を折った。座る姿勢にも、いつもの軽口とは違う静けさがあった。',
'あなたが見ていることに気づくと、「別に、珍しくもないだろ」と椀を寄せる。',
'食卓の作法を教わった話は短かった。あなたは続きを迫らず、パンを分ける位置だけ尋ねた。',
'ドロシーはあなたの手を見て、刃を置く場所を示した。「ほら、その方が安心して取れる」',
'謳の主人が最後に誰を招いたかは分からない。二人は読み残した頁へ栞を挟み、温かいうちに食べた。',
'「今日はいい子のフリ、いらないんだな」。あなたが頷くと、彼女はようやく声を立てて笑った。',
'片付ける時にも皿は丁寧に重ねられた。その所作も乱暴な冗談も、次の食事へそのまま持ってきてほしいと伝えた。']),
('自分の名前で帰る','colossus.sky-tower-machine',[
'塔の謳を読んでいたドロシーが、窓に映る自分へ目を向けた。誰かの代わりに立つ話だった。',
'「似せるのは得意なんだけどさ」。あなたは、今日の記録には誰の名前を書くかと尋ねた。',
'彼女は少し考え、自分の名前を差し出した。借り物の肩書きではなく、ドロシー・ナイナーと。',
'見張り台の点検へ二人で上がる。途中の階段で、彼女はあなたの歩幅に合わせて速度を落とした。',
'上から見れば庭の道は細い。帰ってくる人のための灯りが、昼のうちにも一つずつ確かめられていた。',
'「あたしが戻んなかったら、呼びに来る？」。あなたは探しに行くと答え、困る前に知らせてほしいとも言った。',
'彼女は笑い飛ばしかけ、やめた。戻る時刻を自分で決め、あなたの手帳へ書き込む。',
'謳の塔は同じ顔の住人を増やしていった。二人の記録には、名前の違う筆跡が並んだ。',
'階段を降りたところで、ドロシーはあなたを昼食へ誘った。「今日はあたしが誘ってんの。間違えんなよ」',
'点検の報告書に本人が署名する。あなたは隣へ日付を足し、約束した席へ一緒に向かった。'])]
EVENTS=[
('一杯ぶんの反則','昼下がりの庭で、ドロシーが二つのカップを並べた。|仕事の話なら終わってからにしろと、あなたへ椅子を引く。|香りを確かめる手つきは意外なほど丁寧だった。|「指揮官、砂糖いくつ？」|答えると、彼女は自分の分と取り違えないよう向きを変えた。|今日は誰の頼みでもなく、あなたと飲みたかったという。|あなたが礼を言うと、彼女は軽く肩をすくめた。|空いたカップは、二人が次の約束をするまで片付けられなかった。'),
('合図は撃たずに','射場を片付けた後、ドロシーが紙の標的を折っていた。|あなたが近づくと、銃は既に安全を確かめて置かれていた。|折った紙には、明日の散歩の時刻だけが書いてある。|「毎回大声で呼ぶのも面倒だし、これで分かるだろ」|あなたが受け取ると、彼女は行きたい場所を一つ足した。|任務の経路にはない、庭の奥の小さな花壇だった。|遅れたら先に行くと言いながら、待ち合わせ場所も丁寧に書く。|あなたは返事の印をつけ、明日は自分から迎えに行くと約束した。'),
('名前を呼んで','夕暮れの回廊で、ドロシーがあなたを呼び止めた。|軽い冗談の続きを探すように、少しだけ言葉が止まる。|あなたが待つと、彼女は正面へ向き直った。|「あたし、あんたが好き。誰かの代わりじゃなく、あたしを見ててほしい」|あなたはドロシーが好きだと返し、恋人としてこれからも誘い合いたいと伝えた。|彼女は笑い、今度はドロシーと呼んでほしいと言った。|あなたが名前を呼ぶと、差し出された手を自分から握った。|帰り道の約束は、彼女自身の名前で交わされた。'),
('ほどかない結び目','食事の支度中、ドロシーが襟のリボンを結び直していた。|あなたに見られると、曲がっていないかと尋ねる。|ほんの少し傾いていると答え、直してよいか聞いた。|彼女は頷いて、手が届くところまで近づいた。|結び終わっても、彼女はすぐには離れなかった。|「こういうの、あんたに頼むのは嫌じゃないんだよな」|あなたは次も呼んでほしいと答え、恋人の手を取った。|二人の食卓には、まだ湯気の立つ皿が待っていた。'),
('休みの日の帰り道','朝の庭で、ドロシーが約束より少し早く待っていた。|今日は行く先を決めずに歩こうと言う。|あなたが頷くと、彼女は銃のない方の手を差し出した。|「帰る場所は決まってるし、途中くらい適当でいいだろ」|二人は花壇を回り、橋の上でしばらく足を止めた。|あなたが寒くないか尋ねると、彼女は握る手を少し寄せた。|帰ったら一緒に温かいものを飲みたいという。|予定のない一日にも、恋人と帰る約束だけは自然に残った。')]
def build():
 combat=read(RES/'Combat/battle-plan12-invisible.json');story=read(RES/'Story/plan12-invisible-story-content.json')
 baseline=copy.deepcopy(combat);oldstory=copy.deepcopy(story)
 h=remap(copy.deepcopy(next(x for x in combat['heroines'] if x['id']=='heroine.excalipan')))
 h.update(name='ヴァイパー',personId=HERO,variantId='normal',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,reactionStyleMain='teasing',reactionStyleSub='cool',interactionTraitIds=['hair.blonde','taste.books','personality.active'])
 combat['heroines'].append(h)
 template=next(s for s in combat['skills'] if s['id']=='heroine.excalipan.1')
 for i,(name,power,wait) in enumerate([('ディバインプレジャー',1.26,100),('洗礼レインオブバレット',1.78,125),('アヴェンジャー・帰還の斉射',2.1,155)],1):
  s=remap(copy.deepcopy(template));s.update(id=f'{HERO}.{i}',sourceSkillId=f'{HERO}.{i}',ownerId=HERO,name=name,powerScale=power,recoveryPercent=wait,conditions=[],statusEffects=[],selfEffects=[],sourceFile='017戦闘_ヴァイパー.mkv' if i<3 else '',sourceSecond=5 if i<3 else 0,observedSkillLevel=7 if i<3 else 0,ruleOrigin='video-observation-plus-newaster-original' if i<3 else 'newaster-original')
  if i==1:s.update(enemyAttackReductionPercent=10,enemyAttackReductionTurns=3)
  combat['skills'].append(s)
 chain=remap(copy.deepcopy(next(c for c in combat['chainActions'] if c['heroineId']=='heroine.excalipan')));chain.update(powerScale=.8,attributes=['銃弾']);combat['chainActions'].append(chain)
 combat['contentReferences'] += [remap(copy.deepcopy(r)) for r in combat['contentReferences'] if r['ownerId']=='heroine.excalipan']
 extra=dict(schemaVersion=story['schemaVersion'],contentVersion=story['contentVersion'],provenance='newaster-original',chapters=[],events=[])
 for ci,(title,source,pages) in enumerate(CHAPTERS,1):
  src=next(c for c in story['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
  poems=[dict(id=f'{HERO}.collection.poem.{(ci-1)*6+pi:02}',text=body.split('。')[0]+'。',body=body,sourcePoemId=src['poems'][pi-1]['id'],reason=f'「{src["poems"][pi-1]["text"]}」を受け、{title}で自由と約束を選ぶ本人の経験を描く。') for pi,body in enumerate(pages[1:7],1)]
  extra['chapters'].append(dict(id=f'{HERO}.poem-chapter.{ci}',ownerId=HERO,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
 for i,(title,body) in enumerate(EVENTS):extra['events'].append(dict(id=f'{HERO}.event.{i}',ownerId=HERO,title=title,cgResourcePath=f'Illustrations/viper-event-{i}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][i],establishesLover=i==2,paragraphs=body.split('|'),expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
 story['chapters']+=extra['chapters'];story['events']+=extra['events']
 assert len(combat['heroines'])==22
 for key in ('heroines','skills','chainActions','contentReferences'):assert combat[key][:len(baseline[key])]==baseline[key],key
 for key in ('chapters','events'):assert story[key][:len(oldstory[key])]==oldstory[key],key
 assert len({s['name'] for s in combat['skills'] if s['ownerId']==HERO})==3
 assert all(len(c['pages'])==10 and len(c['poems'])==6 for c in extra['chapters'])
 assert all(len(e['paragraphs'])==8 for e in extra['events'])
 write(OUT/'combat.json',combat);write(OUT/'story.json',story);write(OUT/'viper-story.json',extra)
 write(OUT/'manifest.json',dict(schemaVersion=1,available=False,stage='authored-draft-art-in-progress',formIds=[HERO],baselineForms=21,targetForms=22,requiredAssetsPerForm=21,sourceAdaptations='映像の2技をLv1基準へ採用。第3技210%/WT155とチェイン80%は本作独自。原作祝福の効果は不明なので本作の木の効果として別途調整。ジョブマスタリーはキャラ重複のみ。',remaining=['trait-and-identity-connection','dedicated-art-21','weapon-tree','skill-level-and-save-tests','default-player-review','normal-play-promotion']))
 lines=['# ヴァイパー — 本作オリジナル本文','']
 for c in extra['chapters']:lines += ['## '+c['title'],'']+c['pages']+['']
 for e in extra['events']:lines += ['## '+e['title'],'']+e['paragraphs']+['']
 (ROOT/'docs/story-text/plan12-viper.txt').write_text('\n\n'.join(lines)+'\n',encoding='utf8')
 print('VIPER_DRAFT_PASS baseline=21 preserved; target=22; skills=3 chapters=3 pages=30 poems=18 events=5; NOT PUBLISHED')
if __name__=='__main__':build()
