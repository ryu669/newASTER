"""April original chapters and relationship events, unpublished until acceptance."""
import copy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1];H='heroine.april';O=R/'game/unity/Assets/Game/ContentDrafts/April'
CHAPTERS=[('植え直す場所','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を読んだケアトは、古い花壇の図面を広げた。そこには、今は失われた庭の花の配置が描かれていた。',
'「同じように植えたら、少しは戻るでしょうか」。指先が止まったのは、いちばん日当たりのよい区画だった。',
'あなたは今の庭へ一緒に出ようと誘った。図面の庭と違い、午後には建物の影が落ちる。',
'ケアトは土を指でほぐし、ここでは同じ花が弱るかもしれないと言った。懐かしい配置のままでは育たない。',
'謳の中に残る記憶と、目の前の土は違う。あなたは図面を捨てず、余白へ今の日照を書き足した。',
'彼女は日陰に強い苗を選んだ。昔の庭にあった花は、もう一つの鉢へ植えることにする。',
'「昔のことを大事にするほど、変えられなくなっていました」。あなたは二つの鉢を並べるのを手伝った。',
'古い名前の札と新しい育て方の札が、同じ箱に入る。彼女はどちらも自分の字で書き直した。',
'夕方、葉が風に揺れた。ケアトは明日の水やりの時刻を決め、あなたにも見に来てほしいと頼んだ。',
'翌朝の庭で、彼女は図面より先に苗を見た。新しい葉の小さな変化を、嬉しそうにあなたへ知らせた。']),
('よい知らせを待たずに','colossus.black-smoke-citadel',[
'黒煙城塞の謳を閉じると、ケアトは配給用の薬草箱へ向かった。乾燥を終えた葉を、人数分に分ける約束だった。',
'箱の底に湿った束があった。彼女は一つを持ち上げ、使えるかどうか確かめてから、そっと戻した。',
'「わたくしが乾燥棚を選びましたの」。その声は、誰かへ知らせる前から謝罪になっていた。',
'あなたはまず使える束と使えない束を分けようと伝えた。理由を調べるため、濡れた束も印をつけて残す。',
'予定より足りないと分かると、彼女は皆に渡す量を減らす案を口にした。あなたは担当者へ不足を知らせようと提案した。',
'ケアトは数を書き、遅れの見込みも添えた。報告を受けた仲間が、別の棚にある備蓄を分けてくれた。',
'謳の城塞に残る沈黙を、彼女は思い返した。知らせないまま守れるものがあると思っていたと、小さく話した。',
'あなたは今回の報告で、使えない葉が配られずに済んだと伝えた。彼女は結果と自分の責任を別々の行へ記した。',
'棚を直した後、彼女は次の確認をあなたに頼んだ。判断を預けるのでなく、一緒に見落としを探してほしいという。',
'不足の札を外す日まで、ケアトは報告を続けた。よい知らせの日だけでなく、困った日にも顔を上げられるようになった。']),
('庭の外の約束','colossus.sky-tower-machine',[
'塔の謳の頁をめくりながら、ケアトは午後の散歩を楽しみにしていると言った。その横には庭仕事の予定表がある。',
'出発の前、伸びた枝を見つけた彼女は鋏を取った。次の枝へ手を伸ばしたところで、あなたと目が合う。',
'「あと少しだけ、整えてもよろしいかしら」。あなたは帰ってからでも間に合う枝か尋ねた。',
'彼女は葉の状態を確かめ、今日中なら大丈夫だと答えた。鋏を置いても、手はまだ予定表を探していた。',
'塔の謳を読んだ時、誰かのための仕事が途切れないことを彼女は誇らしく思った。今はその人の休みも気に掛かる。',
'あなたは次の水やりの担当を確かめ、庭の鍵を返した。ケアトは散歩の行き先を、自分から一つ選んだ。',
'小川のほとりには名前を知らない花があった。すぐ調べようとした彼女は、今日は色だけ覚えて帰ると笑った。',
'二人で腰を下ろすと、彼女はあなたの休みの過ごし方を尋ねた。答えを聞いてから、次も一緒に来たいと続ける。',
'帰り道の歩調は、庭へ急ぐ時よりゆっくりだった。ケアトは肩を並べ、今日の好きな景色を一つずつ挙げた。',
'予定表の午後の欄には、花の名前ではなくあなたと歩いた道が記された。次の約束の欄は、二人で相談して埋めることにした。'])]
EVENTS=[('一杯目の香り',[
'温室へ入ると、ケアトが二つの茶碗を温めていた。',
'今日は自分で育てた香草を試してほしいと、席を勧めてくれる。',
'あなたが好みを伝えると、彼女は少しずつ葉を合わせた。',
'「指揮官さんの一杯は、こちらですわ」。差し出す手に、かすかな土の匂いが残る。',
'ひと口飲んで感想を返すと、彼女は嬉しそうに覚え書きをつけた。',
'あなたも次は茶菓子を用意したいと伝える。',
'ケアトは茶の出来だけでなく、あなたと飲めたことが嬉しいと言った。',
'窓を少し開け、二人は冷めるまでの香りをゆっくり楽しんだ。']),
('芽を待つ札',[
'苗箱の前で、ケアトが名札を結び直していた。',
'芽の出ない区画にも、同じように水をやっている。',
'あなたが隣へしゃがむと、種を確かめる日を決めたのだと教えてくれた。',
'「待つのと、見ないふりをするのは違いますものね」。彼女は土の乾き具合を記す。',
'あなたは次の確認にも付き合いたいと伝え、日付を一緒に書いた。',
'ケアトは札を見てから、あなたの顔を見上げた。',
'うまく育った時だけ呼ぶつもりだったと、少し照れて打ち明ける。',
'二人で苗箱を日向へ運ぶと、彼女は明日も来てほしいと笑った。']),
('肩書きを置く夜',[
'花のアーチの下で、ケアトがあなたを待っていた。',
'普段より言葉を選び、今日は王家の話をしに来たのではないと言う。',
'あなたは彼女の正面へ立ち、続きを静かに待った。',
'「わたくし、あなたが好きです。肩書きのない時間も、ご一緒したいのです」',
'あなたもケアトへの想いを伝え、恋人としてこれからを歩みたいと答えた。',
'彼女は息をついて笑い、自分からあなたの手を取った。',
'立派に振る舞えない日にも会いたいと言うと、あなたはその日も一緒にいたいと返した。',
'帰り道、二人は次に会う時刻を決めた。誰かに示すためではない、小さな約束だった。']),
('空白の午後',[
'温室のベンチで、ケアトが白い頁のままの手帳を開いていた。',
'庭の仕事は仲間に頼めたので、今日は二人で好きに過ごせると言う。',
'あなたが隣に座ると、彼女は肩の触れる位置へ少し寄った。',
'何から始めるか相談しているうちに、茶がちょうど飲み頃になる。',
'ケアトは手帳を閉じ、今していることを続けたいと笑った。',
'あなたも頷き、彼女の好きな花の話を聞いた。',
'「何も決めていないのに、満ちておりますね」。そっと重ねた指が温かい。',
'午後の光が移るまで、恋人同士の話はゆっくり続いた。']),
('朝の橋で',[
'朝の橋で、ケアトが小さな包みを二つ抱えていた。',
'片方には昼食、もう片方にはあなたの好きな茶葉が入っている。',
'あなたが荷物を受け取ると、彼女は空いた手をそっと差し出した。',
'「今日はわたくしの好きな道へ、ご案内してもよろしいかしら」',
'喜んで頷くと、彼女は橋の先の花壇を指した。',
'庭で過ごした日々の話から、次の季節の約束へ話が移る。',
'あなたはその頃も一緒に歩きたいと伝え、彼女の手を握り返した。',
'ケアトは嬉しそうに歩調を合わせた。朝の道には、まだ二人の足音だけが響いていた。'])]

def main():
 if (O/'manifest.json').exists() and json.loads((O/'manifest.json').read_text(encoding='utf8')).get('available'):raise RuntimeError('Accepted form: do not overwrite its published story.')
 base=json.loads((R/'game/unity/Assets/Game/Resources/Story/plan12-weapon-master-story-content.json').read_text(encoding='utf-8-sig'));story=copy.deepcopy(base)
 extra=dict(schemaVersion=base['schemaVersion'],contentVersion=base['contentVersion'],provenance='newaster-original',chapters=[],events=[])
 for i,(title,source,pages) in enumerate(CHAPTERS,1):
  src=next(c for c in base['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
  poems=[dict(id=f'{H}.collection.poem.{(i-1)*6+j:02}',text=t.split('。')[0]+'。',body=t,sourcePoemId=src['poems'][j-1]['id'],reason=f'「{src["poems"][j-1]["text"]}」を受け、{title}で失敗を引き受けながら育て直す方法を考える。') for j,t in enumerate(pages[1:7],1)]
  extra['chapters'].append(dict(id=f'{H}.poem-chapter.{i}',ownerId=H,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
 for i,(title,paragraphs) in enumerate(EVENTS):extra['events'].append(dict(id=f'{H}.event.{i}',ownerId=H,title=title,cgResourcePath=f'Illustrations/april-event-{i}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][i],establishesLover=i==2,paragraphs=paragraphs,expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
 new=[t for c in extra['chapters'] for t in c['pages']]+[t for e in extra['events'] for t in e['paragraphs']]
 existing={t for c in base['chapters'] for t in c.get('pages',[])}|{t for e in base['events'] for t in e['paragraphs']}
 assert len(new)==70 and len(set(new))==70 and not existing.intersection(new)
 assert all(len(c['pages'])==10 and len(c['poems'])==6 for c in extra['chapters']) and all(len(e['paragraphs'])==8 for e in extra['events'])
 story['chapters']+=extra['chapters'];story['events']+=extra['events']
 for k in ('chapters','events'):assert story[k][:len(base[k])]==base[k]
 for name,value in [('story.json',story),('april-story.json',extra)]: (O/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 text=['# エイプリル — 本作オリジナル本文','']
 for c in extra['chapters']:text+=['## '+c['title'],'']+c['pages']+['']
 for e in extra['events']:text+=['## '+e['title'],'']+e['paragraphs']+['']
 (R/'docs/story-text/plan12-april.txt').write_text('\n\n'.join(text)+'\n',encoding='utf8')
 print('APRIL_STORY_PASS chapters=3 pages=30 poems=18 events=5 originalParagraphs=70 baseline=preserved')
if __name__=='__main__':main()
