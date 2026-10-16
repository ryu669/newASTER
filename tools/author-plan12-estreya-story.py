"""Author original Estreya chapters and events without publishing the draft."""
import copy
import json
from pathlib import Path

R=Path(__file__).resolve().parents[1]
H='heroine.estreya'
O=R/'game/unity/Assets/Game/ContentDrafts/Estreya'
CHAPTERS=[('拳に残る拍子','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を読み終えたアズは、机の端を二度、指で叩いた。稽古の始まりに師が鳴らしていた拍子だという。',
'「次が来るって分かると、足が動くんだ」。彼女は立ち上がり、椅子を避けて軽く構えた。',
'あなたが同じ拍子を返すと、彼女の踏み込みが止まった。音は合っていても、思い出の間合いとは少し違うらしい。',
'アズは困ったように笑い、もう一度頼もうとして口を閉じた。「おっちゃんの真似をさせたいわけじゃないんだ」',
'あなたは彼女が動きやすい拍子を教えてほしいと伝えた。昔の音を当てるのでなく、今の足に合わせてみる。',
'床板の線を目印にすると、重い拳甲を着けた時だけ半歩が短くなった。アズは自分でも気づかなかったと目を丸くした。',
'謳が残す記憶にも、今の体重や靴の重さまでは預けられない。彼女は今日の踏み込みを紙に描いた。',
'師に教わった印の隣へ、自分で考えた矢印を書き足す。「これを見せたら、まず試せって言われるかな」',
'夕方、あなたの二拍に合わせて拳が伸びた。戻る足まで滑らかに収まり、アズは息を弾ませて振り向いた。',
'紙をしまう前に、彼女は今日の日付を書いた。受け継いだ拍子に、自分で確かめた一日が増えた。']),
('立ち止まる勇気','colossus.black-smoke-citadel',[
'黒煙城塞の謳を読んだ朝、アズはいつもより早く訓練場へ来た。昨日うまく決まらなかった技を、出発前に試したいという。',
'二度目の踏み切りで、着地が揺れた。彼女は手首を回し、拳甲の留め具を締め直した。',
'「あと一回なら」。あなたは返事を急がず、さっきから片足をかばっていることを伝えた。',
'アズは靴を脱いだ。擦れた踵を見て、ようやく朝から痛かったと認める。「ここでやめたら、逃げた気がしてさ」',
'あなたは今日の目標を尋ねた。彼女は黙ってから、次の戦いでも同じように踏み切れることだと答えた。',
'城塞の謳に残る、退けないまま積もった損耗を二人で読み返す。アズは痛みに慣れる練習をしたいわけではないと言った。',
'手当てを済ませ、彼女は椅子に座ったまま拳甲の動きを確認した。足を使わずに直せる留め具が一つ見つかった。',
'翌日の予定には休養と書き込んだ。あなたに止められたからではなく、自分が次も戦いたいからだと付け加える。',
'数日後、同じ床板で踏み切る足に迷いはなかった。アズは技の出来と一緒に、痛みがないことも知らせた。',
'帰り際、彼女は椅子を訓練場の隅へ戻した。休む場所も練習に必要なのだと、今度は照れずに言えた。']),
('帰り道の歓声','colossus.sky-tower-machine',[
'塔の謳を閉じる頃、窓の外から訓練終了の鐘が聞こえた。アズは窓へ寄り、音が届く先を眺めた。',
'「勝ったら、聞いてもらいたくなるんだよね」。昔は観客の歓声の奥に、たった一人の声を探していたという。',
'あなたが今日は誰に話したいか尋ねると、彼女は指を折った。道具を直してくれた仲間、包帯を分けた仲間、それからあなた。',
'報告に回る道を考えるうち、遠回りになった。アズは地図の上を指でなぞり、それでも全部寄りたいと笑った。',
'塔の高みから一度に知らせるより、一人ずつ顔を見たい。今日できた技の話には、手伝ってくれた人の名前が入る。',
'最後の角を曲がったところで、彼女は急に静かになった。どれほど報告する相手が増えても、師にはもう会えない。',
'あなたは答えを探す代わりに、持っていた花を彼女へ渡した。アズが選んでおいた、小さな黄色い花だった。',
'彼女は花を手向け、今日の勝ち方を短く話した。返事を待たずに立ち上がるまで、あなたも隣にいた。',
'帰り道でアズのお腹が鳴った。二人で笑うと、彼女は次の勝負の前にも食事へ行こうと言った。',
'勝った日にも負けた日にも、帰る道は続いている。アズは先に駆け出しかけて、あなたと並ぶ速さへ足を緩めた。'])]
EVENTS=[('新しい包帯',[
'訓練場のベンチに、ほどけた包帯が長く伸びていた。アズは端をつかみ、巻き直す順番を考えている。',
'あなたが反対の端を持つと、彼女は助かったと笑った。今日は拳甲の内側を少し調整したらしい。',
'「緩いとずれるし、締めすぎると指が動かない。けっこう難しいんだよ」',
'彼女の言う位置で布を止め、握ったり開いたりする手を見守った。今度は痛くないと頷く。',
'アズは試しに二度、空へ拳を伸ばした。戻した手を見て、明日の稽古も楽しみになったと言う。',
'あなたも今日の調整を覚えておきたいと伝えた。彼女は紙に巻き方を描き、あなたの分の印をつけた。',
'「次はアタシが一人でやってみるから、出来を見てよ、指揮官」',
'使い終えた布を畳む手が、さっきより軽い。二人は道具箱を閉じ、夕食の席へ向かった。']),
('半分の柘榴',[
'庭のテーブルで、アズが柘榴の実を皿へ外していた。弾けた赤い粒を見つけて、慌てて袖を引く。',
'「こういうの、一息で食べるほうが得意なんだけどね」。それでも粒を潰さないよう指先を動かしている。',
'あなたが皿をもう一枚出すと、彼女は同じくらいの量に分け始めた。',
'大きさが違う粒を何度か入れ替え、最後には二人で笑った。厳密に半分でなくても、一緒に食べればいい。',
'ひと粒口へ運んだアズが、酸っぱさに目を細める。その後から甘さが来ると嬉しそうに教えてくれた。',
'あなたの感想を聞きながら、彼女は食事の好みを尋ねた。次の休みには、好きなものを持ち寄りたいという。',
'「今日はアタシが選んだから、次は指揮官の番」。皿の上の赤い粒が、日差しを受けて透ける。',
'二枚の皿が空くまで、次に食べたいものの話は途切れなかった。']),
('勝った日の約束',[
'稽古試合を終えたアズが、誰もいないテラスであなたを待っていた。勝利の報告は、もう皆に済ませたという。',
'「でも、一番言いたいことが残ってる」。彼女は拳甲をベンチへ置き、素手をぎゅっと握った。',
'あなたが隣へ立つと、アズは顔を上げた。試合の時より緊張していると、自分で小さく笑う。',
'「アタシ、指揮官が好き。勝った日だけじゃなくて、何もない日にも会いたい」',
'あなたも彼女への想いを伝え、恋人として一緒に過ごしたいと答えた。アズは確かめるようにもう一度目を合わせた。',
'それから、彼女の手があなたの手を取った。「じゃあ、最初の約束。明日は試合の話抜きで、散歩しよう」',
'あなたが頷くと、彼女はやっと肩の力を抜いた。勝つために用意した言葉ではなかったから、ずっと照れくさかったらしい。',
'二人で時刻を決め、テラスの灯りを消した。アズは手を離さず、帰り道の最初の一歩を揃えた。']),
('休みの日の歩幅',[
'約束の散歩に来たアズは、拳甲を置いて身軽だった。今日は遠くへ走る予定もないという。',
'通りを抜ける途中、いつもの調子で先へ出た彼女が振り返った。あなたも追いつこうとしていたことに気づく。',
'「速かった？　じゃ、今日はこっち」。アズは隣へ戻り、あなたの手を取った。',
'二人の歩幅に合わせると、店先の小さな風車が目に入った。普段なら走り過ぎていた場所だった。',
'彼女は羽根が回り始めるまで待ち、あなたにも見えたか尋ねた。何度も確認する声が楽しそうだった。',
'休憩の席で、次の休みも同じ道を歩くか相談した。アズは途中の角を曲がってみたいと言う。',
'あなたが賛成すると、彼女は手の甲へそっと頬を寄せた。「こういう予定、増やしたいな」',
'風車をもう一度眺めてから、二人は立ち上がった。帰り道にも、急ぐ理由はなかった。']),
('朝焼けの指定席',[
'夜明け前の庭で、アズがベンチの露を拭いていた。あなたを見つけると、温かい飲み物の入った水筒を掲げた。',
'「ここ、朝日が一番きれいに見えるんだ」。隣の席へ座ると、まだ空は青暗かった。',
'稽古で早起きした日に見つけ、いつか一緒に来たかったという。今日は練習の前ではなく、この時間のために起きた。',
'二人でカップを包み、空の色が変わるのを待った。アズは肩を寄せ、眠くないか小声で尋ねる。',
'あなたが今は目を閉じたくないと答えると、彼女も同じだと笑った。木々の先に、最初の光が触れた。',
'アズはその光を眺めたまま、来年の今頃も隣にいたいと言った。あなたも一緒に来ようと約束した。',
'「それまでにも、何回だって来ていいよね」。頷くあなたを見て、彼女の笑顔が朝日にほどけた。',
'空いたカップをしまい、二人は朝食へ向かった。ベンチの隣の席は、次に来る日にも空けておくことにした。'])]

def main():
 if json.loads((O/'manifest.json').read_text(encoding='utf8')).get('available'):
  raise RuntimeError('Do not overwrite accepted story')
 base=json.loads((R/'game/unity/Assets/Game/Resources/Story/plan12-april-story-content.json').read_text(encoding='utf-8-sig'))
 story=copy.deepcopy(base)
 extra=dict(schemaVersion=base['schemaVersion'],contentVersion=base['contentVersion'],provenance='newaster-original',chapters=[],events=[])
 for i,(title,source,pages) in enumerate(CHAPTERS,1):
  src=next(c for c in base['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
  poems=[dict(id=f'{H}.collection.poem.{(i-1)*6+j:02}',text=t.split('。')[0]+'。',body=t,
              sourcePoemId=src['poems'][j-1]['id'],reason=f'「{src["poems"][j-1]["text"]}」を読み、{title}で記憶と今の選択を結び直す。') for j,t in enumerate(pages[1:7],1)]
  extra['chapters'].append(dict(id=f'{H}.poem-chapter.{i}',ownerId=H,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
 for i,(title,paragraphs) in enumerate(EVENTS):
  extra['events'].append(dict(id=f'{H}.event.{i}',ownerId=H,title=title,cgResourcePath=f'Illustrations/estreya-event-{i}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][i],establishesLover=i==2,paragraphs=paragraphs,expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
 new=[t for c in extra['chapters'] for t in c['pages']]+[t for e in extra['events'] for t in e['paragraphs']]
 existing={t for c in base['chapters'] for t in c.get('pages',[])}|{t for e in base['events'] for t in e['paragraphs']}
 assert len(new)==70 and len(set(new))==70 and not existing.intersection(new)
 assert all(len(c['pages'])==10 and len(c['poems'])==6 for c in extra['chapters'])
 for key in ('chapters','events'):
  story[key]+=extra[key]
  assert story[key][:len(base[key])]==base[key]
 for name,value in [('story.json',story),('estreya-story.json',extra)]:
  (O/name).write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
 text=['# エストレヤ — 本作オリジナル本文','']
 for c in extra['chapters']:text+=['## '+c['title'],'']+c['pages']+['']
 for e in extra['events']:text+=['## '+e['title'],'']+e['paragraphs']+['']
 (R/'docs/story-text/plan12-estreya.txt').write_text('\n\n'.join(text).rstrip()+'\n',encoding='utf8')
 print('ESTREYA_STORY_PASS chapters=3 pages=30 poems=18 events=5 baseline=preserved')

if __name__=='__main__':main()
