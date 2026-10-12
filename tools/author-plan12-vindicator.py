"""Author Vindicator story input; does not publish or invent unsupported combat effects."""
import json,copy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'game/unity/Assets/Game/ContentDrafts/Vindicator';HERO='heroine.vindicator'
CHAPTERS=[('帰還の確認','colossus.sky-tower-machine',[
'塔の謳を読み終えると、モルトラは点検の記録を閉じた。帰還した人数を、もう一度だけ数える。',
'あなたが手伝おうとすると、彼女は名簿を半分差し出した。「別々に確認すれば、見落としも減る」',
'謳の塔では、戻らなかった者の席が誰にも気づかれぬまま片付けられた。彼女はその一節へ栞を挟んだ。',
'見張り台の点検には二人で上がった。モルトラは足場の傷を示し、あなたにも場所を覚えてほしいと言う。',
'風が強くなる前に降りようと、あなたから提案した。彼女は空を見て、道具をまとめた。',
'「私一人なら続けられる」。言いかけた彼女は、修理する人の帰り道も同じだと気づいた。',
'二人で予備の手すりを運び、今日は危険な区間へ入らない印をつける。残した仕事にも担当の名前を書いた。',
'戻ったところで、モルトラがあなたの名を呼んだ。返事を聞き、自分も帰ったと告げる。',
'記録には作業の遅れと、全員の帰還が並んだ。あなたは明日続ける仕事の欄へ自分の名前を足した。',
'彼女は名簿を数え直さず閉じた。「明日も二人で確認しよう」。今度は最初から、あなたの分の鉛筆も出した。']),
('空席を一つ','colossus.memory-crystal-dragon',[
'記憶晶竜の謳には、去った客のために毎晩席を用意する家があった。モルトラは窓辺の椅子を見た。',
'あなたは昼食へ誘った。彼女はすぐに頷き、それから任務の相談かと尋ねた。',
'一緒に食べたいだけだと伝えると、彼女は手帳を開いたまま黙った。書くべき議題が見つからなかった。',
'庭の席を選ぶ時、モルトラは通路を塞がない位置へ椅子を動かした。あなたの分も隣へ置く。',
'謳の家の主人は、客の好みを覚えていた。あなたも彼女に、飲みたいものを聞いた。',
'「何でも構わない」。一度そう答え、彼女は温かい茶がよいと言い直した。',
'食事は急がず進んだ。あなたがパンを落としかけると、彼女は笑いを隠すようにカップを持ち上げた。',
'別れた人を忘れたわけではないと、モルトラは話した。あなたは、今の食卓にも座っていてほしいと返した。',
'二人で片付ける時、彼女は椅子を一つだけ窓辺に残さなかった。次に使いやすいよう、二つを並べ直す。',
'「また誘ってほしい」。手帳の端に書かれたのは議題ではなく、次の昼食の時刻だった。']),
('剣を置く時間','colossus.black-smoke-citadel',[
'黒煙城塞の謳を読んだ夜、モルトラは刃の手入れを続けていた。庭の灯りは一つずつ消え始めている。',
'あなたが声をかけると、彼女はあと少しと答えた。磨き終わった箇所へ、もう一度布を当てる。',
'謳の門番は、休む者の代わりに立ち続けた。最後には交代する合図を聞いても、動けなくなっていた。',
'あなたは布を取り上げず、隣の席で明日の予定を開いた。一緒に終わる時間を決めようと提案した。',
'モルトラは鞘の傷を一つ示した。今夜直す必要があるか、あなたにも確かめてほしいと言う。',
'危険のない擦り傷だった。二人で記録を残し、彼女自身が剣を架け台へ戻した。',
'何も持たない手を見て、彼女は少し落ち着かなさそうにした。あなたは夜の庭を歩こうと誘った。',
'歩きながら、モルトラは明日飛ぶつもりだと話した。帰る時刻を決め、待つ場所をあなたと相談する。',
'「戻ったら、今日の続きを」。彼女が指したのは剣ではなく、途中で閉じた謳の本だった。',
'庭の入口で別れる前に、あなたは席を空けておくと答えた。モルトラは自分から、その席へ戻ると約束した。'])]
EVENTS=[('風向きの報告','夕方の庭で、モルトラが空を見上げていた。|飛んでくると告げた後、あなたの返事を待つ。|何時に戻るのか尋ねると、彼女は鐘の前にはと答えた。|あなたは温かい茶を用意しておくと伝えた。|戻ってきた彼女は、空の冷たさを報告した。|カップを受け取り、思っていたより長く飛んでいたと打ち明ける。|あなたが寒くないか聞くと、彼女はもう大丈夫だと言った。|茶を飲み終えるまで、二人は今日見た空の色を話した。'),
('二人分の準備','モルトラが小さな籠へ昼食を詰めていた。|あなたに気づくと、庭の奥まで同行してほしいと頼む。|籠には初めから二人分が入っていた。|「任務ではない。君と外で食べたいと思った」|あなたが喜んで頷くと、彼女は籠の留め具を確かめた。|歩く道は、いつもの点検とは違う花壇沿いだった。|彼女が選んだ席には、二人で座れる木陰があった。|あなたは次の休憩には自分が用意すると約束した。'),
('忠誠ではなく','夜の回廊で、モルトラが剣を置いてあなたを待っていた。|話を聞いてほしいと、普段よりゆっくり告げる。|あなたが頷くと、彼女は握っていた手を開いた。|「君といる時間が好きだ。任務のない明日も、その次も、私から会いに行きたい」|あなたは彼女に会えることが嬉しいと返し、恋人として同じ時間を選びたいと伝えた。|モルトラは息をつき、隣へ来てよいか尋ねた。|あなたが手を差し出すと、彼女は確かめるように握った。|明日の約束に命令は要らなかった。二人は会いたい時刻を相談して決めた。'),
('隣の椅子','読書中のあなたの隣へ、モルトラが椅子を運んできた。|ここに座ってよいかと聞かれ、あなたは本を少し寄せた。|彼女は同じ頁が見える距離へ腰を下ろした。|読み終えるたび、どちらからともなく感想を話す。|あなたが手を伸ばすと、彼女は指を重ねた。|「今日は、飛びに行かなくても落ち着く」|それでも飛びたい日は待っていると、あなたは伝えた。|彼女は笑って頷き、次の頁を一緒に開いた。'),
('明日も誘って','朝の庭で、モルトラがあなたを散歩へ誘った。|空は明るく、急いで確かめる仕事もなかった。|あなたが手を取ると、彼女は歩調を合わせた。|途中の橋で、明日も一緒に歩きたいと言う。|あなたは自分からも誘うつもりだったと答えた。|「それなら、先に誘った方が道を選ぼう」|初めての決まりに、二人で笑った。|帰る頃には、モルトラが明日の道をもう一つ考えていた。')]
def main():
 base=json.loads((ROOT/'game/unity/Assets/Game/Resources/Story/plan12-viper-story-content.json').read_text(encoding='utf-8-sig'));story=copy.deepcopy(base);extra=dict(schemaVersion=base['schemaVersion'],contentVersion=base['contentVersion'],provenance='newaster-original',chapters=[],events=[])
 for i,(title,source,pages) in enumerate(CHAPTERS,1):
  src=next(c for c in base['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
  poems=[dict(id=f'{HERO}.collection.poem.{(i-1)*6+j:02}',text=t.split('。')[0]+'。',body=t,sourcePoemId=src['poems'][j-1]['id'],reason=f'「{src["poems"][j-1]["text"]}」から、{title}で守る責任と自分の希望を選ぶ経験へつなぐ。') for j,t in enumerate(pages[1:7],1)]
  extra['chapters'].append(dict(id=f'{HERO}.poem-chapter.{i}',ownerId=HERO,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
 for i,(title,text) in enumerate(EVENTS):extra['events'].append(dict(id=f'{HERO}.event.{i}',ownerId=HERO,title=title,cgResourcePath=f'Illustrations/vindicator-event-{i}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][i],establishesLover=i==2,paragraphs=text.split('|'),expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
 assert all(len(c['pages'])==10 and len(c['poems'])==6 for c in extra['chapters']);assert all(len(e['paragraphs'])==8 for e in extra['events']);assert sum(e['establishesLover'] for e in extra['events'])==1
 existing={t for c in base['chapters'] for t in c.get('pages',[])}|{t for e in base['events'] for t in e['paragraphs']};new=[t for c in extra['chapters'] for t in c['pages']]+[t for e in extra['events'] for t in e['paragraphs']];assert len(new)==len(set(new)) and not existing.intersection(new)
 story['chapters']+=extra['chapters'];story['events']+=extra['events'];assert story['chapters'][:len(base['chapters'])]==base['chapters'] and story['events'][:len(base['events'])]==base['events']
 OUT.mkdir(parents=True,exist_ok=True)
 combat=json.loads((ROOT/'game/unity/Assets/Game/Resources/Combat/battle-plan12-viper.json').read_text(encoding='utf-8-sig'));before=copy.deepcopy(combat)
 def remap(x):
  if isinstance(x,str):return x.replace('heroine.undermine',HERO)
  if isinstance(x,list):return [remap(t) for t in x]
  if isinstance(x,dict):return {k:remap(v) for k,v in x.items()}
  return x
 h=remap(copy.deepcopy(next(h for h in combat['heroines'] if h['id']=='heroine.undermine')));h.update(name='ヴィンディーケーター',personId=HERO,variantId='normal',traitHpPercent=0,traitAttackPercent=5,reactionStyleMain='cool',reactionStyleSub='reserved',interactionTraitIds=['hair.black','taste.books','personality.cool']);combat['heroines'].append(h)
 for i,(name,power,wait) in enumerate([('Eradicate evil',1.5,100),('古傷塗れのアコレード',0,125),('To My Dear Queens',2.2,155)],1):
  skill=dict(id=f'{HERO}.{i}',ownerId=HERO,name=name,sourceSkillId=f'{HERO}.{i}',sourceFile='018戦闘_ヴィンディーケーター.mkv',sourceSecond=10,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.self-buff' if i==2 else 'effect.damage',targetRuleId='target.self' if i==2 else 'target.all-enemies' if i==3 else 'target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=i!=2,damageCap=10000 if i==1 else 50000 if i==3 else 0,criticalBonusBp=0,conditions=[],statusEffects=[],selfEffects=[],damageType='magic' if i==3 else '' if i==2 else 'physical',attributes=['斬撃','闇'] if i==3 else ['斬撃'] if i==1 else [])
  if i==1:skill.update(enemyWaitAdd=40,enemyWaitChancePercent=60)
  if i==2:skill.update(selfDamageMaxHpPercent=20,selfEffects=[dict(kind='attack',percent=40,turns=3),dict(kind='physical-defense',percent=40,turns=3),dict(kind='forced-target',percent=1,turns=1)])
  if i==3:skill.update(selfDamageMaxHpPercent=10,statusEffects=[dict(kind='stun',amount=20)])
  combat['skills'].append(skill)
 chain=remap(copy.deepcopy(next(c for c in combat['chainActions'] if c['heroineId']=='heroine.undermine')));chain.update(powerScale=.8,attributes=['斬撃']);combat['chainActions'].append(chain)
 combat['contentReferences']+=[remap(copy.deepcopy(r)) for r in before['contentReferences'] if r['ownerId']=='heroine.undermine']
 assert len(combat['heroines'])==23
 for k in ('heroines','skills','chainActions','contentReferences'):assert combat[k][:len(before[k])]==before[k]
 (OUT/'combat.json').write_text(json.dumps(combat,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 for name,value in [('story.json',story),('vindicator-story.json',extra),('manifest.json',dict(schemaVersion=1,available=False,stage='story-authored-combat-design-art-in-progress',formIds=[HERO],baselineForms=22,targetForms=23,requiredAssetsPerForm=21,remaining=['traits-and-weapon-tree','dedicated-art-21','skill-and-save-tests','default-player-review','normal-play-promotion']))]:(OUT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 lines=['# ヴィンディーケーター — 本作オリジナル本文','']
 for c in extra['chapters']:lines+=['## '+c['title'],'']+c['pages']+['']
 for e in extra['events']:lines+=['## '+e['title'],'']+e['paragraphs']+['']
 (ROOT/'docs/story-text/plan12-vindicator.txt').write_text('\n\n'.join(lines)+'\n',encoding='utf-8');print('VINDICATOR_STORY_DRAFT_PASS chapters=3 pages=30 poems=18 events=5 originalParagraphs=70 baseline=preserved NOT_PUBLISHED')
if __name__=='__main__':main()
