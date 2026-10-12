"""Author Weapon Master's original story and retain the published baseline."""
import copy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1];H='heroine.weapon-master';O=R/'game/unity/Assets/Game/ContentDrafts/WeaponMaster'
CHAPTERS=[('明日も開く店','colossus.black-smoke-citadel',[
'黒煙城塞の謳には、門の前で品物を売る商人がいた。イサカは読み終えると、今日の売上帳へ視線を落とした。',
'包帯を買いに来た人から事情を聞き、代金を半分にしたという。空いた棚には、次の仕入れの印だけが残っていた。',
'「損得だけで決められない日もあるでしょ」。あなたは頷き、明日の包帯をどう用意するか尋ねた。',
'彼女は帳面を隠さず開いた。運賃を足すと、残りの金では同じ量を仕入れられない。',
'謳の商人は、最後の品を渡して門を閉じた。その先に誰かが訪れたかは書かれていなかった。',
'イサカはしばらく黙り、古い布を仕分け始めた。清潔に処理して使えるものと、使ってはいけないものを分ける。',
'あなたは共同で仕入れる相談をしようと提案した。彼女は売る物と救護用の備えを別の欄へ書いた。',
'「安くした理由も残しておこうかな」。困っていた事情を、名前を出さずに短く記す。',
'夕方、二人で棚を拭いた。彼女は今日助けられたことと、明日も助けられることを別々に確かめた。',
'翌朝の開店札は、イサカ自身が表へ返した。あなたに最初に勧めたのは、一緒に飲む温かい茶だった。']),
('包帯の余白','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を閉じたイサカが、包帯の端を結び直した。誰かを待つ部屋の描写で、読む手が止まっていた。',
'今日は応急処置の練習をする約束だった。あなたは見本の人形を机へ置き、急がず始めようと声をかけた。',
'彼女は手順をよく覚えていた。結び目の圧を確かめた後も、同じ箇所へ何度も指を戻す。',
'「これで足りるって、どうしたら分かるんだろ」。あなたは自分だけで判断せず、医療担当へ引き継ぐ印を示した。',
'謳の部屋では、訪れる人の役目が少しずつ違っていた。手当をする人、知らせる人、道を空ける人。',
'イサカはウィルにも手当をしたと話した。その先は言葉にならず、あなたは続きを急かさなかった。',
'彼女が目を上げてから、連絡係を呼ぶところまで練習しようと提案する。包帯だけで終わらせない手順だった。',
'二度目には、彼女自身が助けを呼んだ。合図が届いたことを確かめ、結び終えた両手を机から離す。',
'「一人で全部じゃなくていいんだね」。あなたが頷くと、彼女は練習帳へ今日の連絡先を足した。',
'片付けの後、二人で庭へ出た。失った人の話を置き去りにせず、今夜はどの道を歩くかを一緒に決めた。']),
('値札のない午後','colossus.sky-tower-machine',[
'塔の謳には、街の機械を直し続ける職人がいた。イサカは感心しながらも、その人の休日を探して頁を戻した。',
'売店の棚には修理を待つ留め具が並んでいる。彼女は休憩の札を持ったまま、一本だけと工具を手にした。',
'あなたが今日の散歩はどうするか聞くと、イサカは窓の空を見た。約束の時刻が近づいていた。',
'「次の人が困ると思うとさ」。あなたは急ぎの修理を確認し、残りには受取日を付けようと提案した。',
'謳の職人が休めなかった理由は残っていない。二人は物語の空白を本人の義務で埋めず、本を閉じた。',
'イサカは工具を置き、明日の欄へ修理の予定を書いた。店を離れる前に、受取日を知らせる札を表へ出す。',
'庭の橋では、彼女が先に足を止めた。水面に映る葉の色は、品定めをする時の目でも飽きないらしい。',
'「こんな午後にも、値段を付けたら売れるかな」。あなたが二人で持っていようと返すと、彼女は笑った。',
'帰り道、イサカは次の休みも誘ってよいか尋ねた。あなたは自分からも誘いたいと答えた。',
'売店へ戻っても、休憩の札はすぐに裏返さなかった。二人の茶が冷めるまでが、今日の休みになった。'])]
EVENTS=[('売り物ではないお茶',[
'店先のベンチで、イサカが二つのカップを並べていた。',
'あなたを見つけると、ちょうど淹れたところだと手招きする。',
'値段を尋ねる冗談に、今日は店主の休憩へ付き合ってほしいのだと笑った。',
'隣へ座ると、彼女はあなたのカップへ先に茶を注いだ。',
'午前中に直した留め具の話をしながら、指の小さな擦り傷を確かめる。',
'あなたが手当を手伝うと、彼女は素直に手を差し出した。',
'「お客さんじゃない時にも、来てよ」。あなたは次は茶菓子を持ってくると答えた。',
'午後の開店まで、二人は日向のベンチでカップを温めた。']),
('結び目を確かめて',[
'夕方、イサカが応急処置の練習帳を見せに来た。',
'新しい手順を覚えたので、見本の人形で確かめてほしいと言う。',
'あなたは隣に座り、彼女が包帯を巻き終えるまで見守った。',
'締めすぎていないか尋ねられ、一緒に見本の目盛りを確かめる。',
'「今日はここまでで大丈夫」。自分からそう言えた彼女へ、あなたは頷いた。',
'片付けた後も、イサカはすぐには席を立たなかった。',
'練習のない日にも一緒に座りたいと、少し声を落として言う。',
'あなたは喜んで約束し、明日の休憩の時刻を二人で決めた。']),
('あたしからの約束',[
'月の明るい庭で、イサカがあなたを待っていた。',
'今夜の話は商談ではないと、冗談を途中で引っ込める。',
'あなたが正面を向くと、彼女はゆっくり息を吸った。',
'「あたし、あなたが好き。一緒に帰る相手に、あたしを選んでほしい」',
'あなたもイサカが好きだと答え、恋人としてこれからの時間を重ねたいと伝えた。',
'彼女はほっと笑い、差し出された手を自分から取った。',
'大切な相棒の記憶も持っていてよいと伝えると、彼女はありがとうと頷いた。',
'誰かの代わりになる約束ではなく、二人自身の明日の約束を交わした。']),
('休憩札の裏側',[
'温室の茶卓へ、イサカが休憩札を持ってきた。',
'今日は売店を任せられたので、ゆっくりしてよいと言う。',
'あなたが隣の椅子を引くと、彼女は肩が触れる近さへ座った。',
'札の裏には、次の仕入れではなく二人で行きたい場所が書かれていた。',
'一つずつ読んで相談する間、彼女の手があなたの指へ重なる。',
'「仕事の話をしないと落ち着かないと思ってた」。今はそうでもないと笑った。',
'あなたは今日の茶の好みを聞き、次は自分が淹れると約束した。',
'休憩札をしまってからも、恋人との午後はまだ続いていた。']),
('帰りの荷物',[
'朝の橋で、イサカが小さな鞄を肩へ掛けて待っていた。',
'今日は売る品を入れていないと、あなたへ中を見せる。',
'昼食と水、それから二人分の敷物が丁寧に収まっていた。',
'あなたが鞄を持とうとすると、彼女は帰りに交代しようと笑う。',
'花壇を抜けた先で、次の休みはあなたから誘ってほしいと言った。',
'誘うつもりだったと答えると、彼女は嬉しそうに歩調を寄せた。',
'「帰りの荷物は増えるかな」。あなたは今日の話が増えると返した。',
'イサカは空いた方の手をあなたへ差し出し、一緒に橋を渡った。'])]

def main():
 if (O/'manifest.json').exists() and json.loads((O/'manifest.json').read_text(encoding='utf8')).get('available'):raise RuntimeError('Accepted form: do not overwrite its published story.')
 base=json.loads((R/'game/unity/Assets/Game/Resources/Story/plan12-vindicator-story-content.json').read_text(encoding='utf-8-sig'));story=copy.deepcopy(base)
 extra=dict(schemaVersion=base['schemaVersion'],contentVersion=base['contentVersion'],provenance='newaster-original',chapters=[],events=[])
 for i,(title,source,pages) in enumerate(CHAPTERS,1):
  src=next(c for c in base['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
  poems=[dict(id=f'{H}.collection.poem.{(i-1)*6+j:02}',text=t.split('。')[0]+'。',body=t,sourcePoemId=src['poems'][j-1]['id'],reason=f'「{src["poems"][j-1]["text"]}」を受け、{title}で助けを続ける方法と自身の希望を考える。') for j,t in enumerate(pages[1:7],1)]
  extra['chapters'].append(dict(id=f'{H}.poem-chapter.{i}',ownerId=H,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
 for i,(title,paragraphs) in enumerate(EVENTS):extra['events'].append(dict(id=f'{H}.event.{i}',ownerId=H,title=title,cgResourcePath=f'Illustrations/weapon-master-event-{i}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][i],establishesLover=i==2,paragraphs=paragraphs,expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
 new=[t for c in extra['chapters'] for t in c['pages']]+[t for e in extra['events'] for t in e['paragraphs']]
 existing={t for c in base['chapters'] for t in c.get('pages',[])}|{t for e in base['events'] for t in e['paragraphs']}
 assert len(new)==70 and len(set(new))==70 and not existing.intersection(new)
 assert all(len(c['pages'])==10 and len(c['poems'])==6 for c in extra['chapters']) and all(len(e['paragraphs'])==8 for e in extra['events'])
 story['chapters']+=extra['chapters'];story['events']+=extra['events']
 for k in ('chapters','events'):assert story[k][:len(base[k])]==base[k]
 for name,value in [('story.json',story),('weapon-master-story.json',extra)]: (O/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 text=['# ウェポンマスター — 本作オリジナル本文','']
 for c in extra['chapters']:text+=['## '+c['title'],'']+c['pages']+['']
 for e in extra['events']:text+=['## '+e['title'],'']+e['paragraphs']+['']
 (R/'docs/story-text/plan12-weapon-master.txt').write_text('\n\n'.join(text)+'\n',encoding='utf8')
 print('WEAPON_MASTER_STORY_PASS chapters=3 pages=30 poems=18 events=5 originalParagraphs=70 baseline=preserved')
if __name__=='__main__':main()
