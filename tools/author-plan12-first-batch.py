"""Rebuild four append-only heroine definitions and their authored original stories.
Video observations are recorded separately from adaptations to newASTER rules.
Never promotes incomplete art into normal play.
"""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'game/unity/Assets/Game/ContentDrafts/FirstBatch'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
PEOPLE=[
 dict(key='iron-maiden',number=2,name='アイアンメイデン',realName='エリザベート・アイゼナッハ',job='defender',second='miracle-body',styles=['teasing','cool'],traits=['hair.silver','taste.books','personality.cool'],hp=10500,attack=9500,defense=10000,speed=9500,
      innate='鋼鉄の棺',personal='鋼鉄の棺',mastery='サラマンドラの祝福',weapon='サラマンドラ',flavor='閉じた扉の外にも、守りたい朝がある。鋼の決意で、隣に立つ人の帰り道を支える。',personalKind='reduction',personalPercent=8,personalText='自身が受ける攻撃ダメージを8%軽減。',bonus='hp',icon='moon',
      skills=[['ディバインパニッシュメント',1.93,130],['サラマンドラの守火',0,100],['鋼鉄の棺―開かれた守り',1.4,150]],
      scenes=['garden tea','letter in study','garden confession','greenhouse flower','twilight bench']),
 dict(key='avalanche',number=6,name='アヴァランチ',realName='キイラ・エアンド',job='gunner',second='hunter-eye',styles=['teasing','active'],traits=['hair.black','personality.active','taste.sweets'],hp=9500,attack=10500,defense=10000,speed=10500,
      innate='濡羽の御髪',personal='乗るぜ勝ち馬！',mastery='Yin&Yangの祝福',weapon='Yin&Yang',flavor='借りを忘れず、次の一手は自分で選ぶ。軽口の奥に、仲間を置き去りにしない熱がある。',personalKind='gauge',personalPercent=15,personalText='弾丸の追加獲得率＋15%。',bonus='physical-defense',icon='flame',
      skills=[['ダンガンロンド',1.26,75],['バッドバレットバレエド',1.78,110],['Yin&Yang―返礼の二重奏',2.1,145]],
      scenes=['garden fan','brass tokens','bridge confession','noodle stall','sunrise']),
 dict(key='anathema',number=11,name='アナテマ',realName='アナスタシア・テンプル',job='fighter',second='full-power',styles=['elegant','affectionate'],traits=['hair.blonde','taste.sweets','personality.active'],hp=10000,attack=10500,defense=9500,speed=9500,
      innate='令嬢の極悪覇気',personal='騎士の誓い',mastery='青薔薇の祝福',weapon='青薔薇の夜想曲',flavor='なくした席を数えるより、今日の約束に胸を張る。青い薔薇の刃は、選んだ人のために。',personalKind='wounded-defense',personalPercent=15,personalText='HPが50%以下のとき、受ける攻撃ダメージを15%軽減。',bonus='critical',icon='sword',
      skills=[['おセレブ斬',1.48,85],['おロイヤルX斬',1.63,110],['おファビュラス豪華剣乱',2.08,140]],
      scenes=['rose tea','dog letter','blue rose confession','biscuits','evening window']),
 dict(key='shiva',number=45,name='シヴァ',realName='柴まりか',job='healer',second='muscle-nurse',styles=['active','affectionate'],traits=['hair.blonde','personality.active','taste.cooking'],hp=10000,attack=10500,defense=9500,speed=10000,
      innate='みんなでがんばろう！',personal='ポジティブ思考',mastery='あたしがついてるから！',weapon='フリスビーテ',flavor='元気な声は、隣で休む人にも届く。手を伸ばす強さと、返事を待つ優しさを忘れない。',personalKind='regen',personalPercent=2,personalText='毎ターン、自身の最大HPの2%を回復。',bonus='hp',icon='leaf',
      skills=[['フリスビーテ・タタク',1.48,100],['シヴァ・イスフウ・オーエン',1.4,120],['円環の愛―最強破壊パンチ',2.4,170]],
      scenes=['meadow ring','friend letter','sunset confession','picnic','star bench'])]

# Each line is one authored ADV page. These are newASTER scenes, not source quotations.
CHAPTERS={
'iron-maiden':[
('鍵を預ける朝','colossus.black-smoke-citadel',[
'黒煙城塞の謳を読み、エリザベートは門の鍵を手に取った。閉めれば安全だという記録を、最後まで信じてよいのかと尋ねる。',
'城の外に残った畑には、毎朝人が通う。彼女は門番へ、誰が帰る時刻なのかを聞きに行った。',
'鍵束は重かった。あなたが半分持とうとすると、彼女は一つだけ選んで渡した。「全部預けるのは、まだ早いわ」',
'修理中の橋では、荷を運ぶ女性が立ち往生していた。彼女は銃を背へ回し、板の端を支えた。',
'門の閉まる音に、女性の肩が跳ねた。エリザベートは急かさず、扉の脇で帰り道を指さした。',
'庭へ戻ると、彼女は鍵に小さな色糸を結んだ。誰でも区別できる印が、鋼の輪で揺れた。',
'謳の中の門守は、一度も持ち場を離れなかった。その代わり、誰が通れなくなったかを知らなかった。',
'昼食の鐘が鳴る。彼女はあなたを先に行かせかけて、代役へ鍵束を渡した。',
'食堂の扉を自分で開けると、温かい匂いがした。「こちらの戸なら、開いている方がいいわね」',
'あなたと並べた椀の脇で、色糸が光った。守る仕事に、帰って食事をする時間も加わった。']),
('鋼の名前と小さな花','colossus.memory-crystal-dragon',[
'記憶晶竜の謳には、名前の代わりに役目だけを刻まれた人がいた。彼女はその頁で指を止める。',
'アイアンメイデンと呼ばれれば仕事へ向かえる。エリザベートと呼ばれると、返事の後に何を話すか考えてしまうという。',
'あなたは庭の花について尋ねた。彼女は知らないと答え、知らないままで済ませず温室へ歩いた。',
'鉢を持ち上げると、土が袖へこぼれた。鋼の銃を扱う手が、小さな根を傷つけないよう止まった。',
'花の名前を教わり、彼女は札へ書いた。字が細すぎて、あなたが顔を近づけると笑う。',
'謳の石板は、役目を果たした人の暮らしを記していなかった。彼女は札の裏へ、今日水をやった日付を足した。',
'雨が強くなり、温室の屋根が鳴る。彼女はあなたの上着が濡れているのに気づき、乾いた布を取った。',
'布を渡すだけで済むのに、袖口を丁寧に拭いてくれた。あなたが名を呼ぶと、手が少し止まる。',
'「今のは、仕事の返事じゃないわ」。エリザベートはそう言い、もう一度呼んでほしいと頼んだ。',
'温室を出る頃、鉢には新しい札が立っていた。鋼の名前の隣に、育つものの名が一つ増えた。']),
('開いた戸の向こう','colossus.sky-tower-machine',[
'空を閉じた塔の謳を読む夜、彼女は窓の留め金を確かめていた。風が強いが、戸口にはまだ灯りがある。',
'遅く帰る庭師のための灯りだった。あなたが座ると、エリザベートも椅子を引いた。',
'塔の人々は侵入を恐れ、迎える合図まで消した。彼女はその一行を二度読んだ。',
'見張りがいなければ危険はある。けれど、誰も帰れない守り方は選びたくないと話す。',
'あなたは迎えに行く時刻を決めた。彼女は銃を取ったが、あなたが籠を持つと少し笑った。',
'道の角で庭師に会った。籠は野菜でいっぱいで、三人で分けて持つことになった。',
'庭へ着くと、彼女が先に戸を押さえた。腕は重そうでも、急いで閉めようとはしなかった。',
'台所で野菜を洗う間、あなたは次の番を申し出た。彼女は鍵を一つ、迷わず差し出した。',
'「待っていてもらうのも、悪くないわね」。銃を置いた隣に、明日の買い物籠が並ぶ。',
'夜の戸は閉じた。窓辺の灯りだけは残り、エリザベートはあなたと一緒に温かい食事へ向かった。'])],
'avalanche':[
('借りの数え方','colossus.red-crystal-tyrant',[
'紅晶暴君の謳を読み終えると、キイラは帳面へ細い線を引いた。借りを覚える癖だと言う。',
'あなたが茶を運ぶと、新しい線が増えた。礼を言われるより先に数えられ、あなたは少し笑う。',
'謳の王は、受け取った物すべてへ値を付けた。彼女は自分の帳面を伏せ、王と同じにはしたくないと話した。',
'市場で荷車の修理を手伝うことになった。キイラは代金を聞いたが、持ち主はあとで食事に来てと言った。',
'作業は思ったより長引いた。あなたが持つ車輪を、彼女が手早く留めた。',
'終わった時、帳面を出しかけてやめた。食事の約束まで返済にするのは窮屈だと言う。',
'屋台の湯気を前に、彼女は昔助けられた話を少しだけした。恩は忘れないが、今日の人まで疑いたくない。',
'あなたが取り皿を渡すと、彼女は一番大きな具を返した。「これで帳消し、じゃないからね」',
'庭へ帰り、帳面の空欄に料理の名前を書いた。次は自分が作ると、妙に真剣な顔をする。',
'借りの線は消えなかった。その横へ、あなたとまた行きたい店の印が増えた。']),
('勝ち馬を降りる日','colossus.sky-tower-machine',[
'塔の謳には、勝つ側へ乗り換え続けた人がいた。キイラは器用だと言いかけ、最後の頁で黙った。',
'その人は勝った後、迎えてくれる場所を持たなかった。彼女は閉じた扇を机へ置く。',
'あなたと苗木を運ぶ約束の日、近道の橋が壊れていた。彼女は別の道をすぐ見つけた。',
'けれど、後ろの荷車にはその坂を登れない車輪があった。キイラは戻り、車輪の幅を確かめた。',
'先に着けば手間を避けられる。彼女はそう口にしてから、あなたへ縄を渡した。',
'二人で引いた荷車は遅かった。途中で休むと、彼女は汗を拭き、勝ち負けの話をやめた。',
'苗木を植える場所には、ほかの庭師が穴を掘って待っていた。遅れたことより、届いたことを喜んだ。',
'キイラは扇であなたへ風を送った。「今日は、こっちへ乗って正解だった」',
'あなたが休む場所を空けると、彼女も座る。勝った後の席を、自分で残せた気がすると話した。',
'新しい木の影はまだ小さい。それでも彼女は、来年そこで昼食を取る約束を帳面へ書いた。']),
('二挺の銃を置く場所','colossus.memory-crystal-dragon',[
'記憶晶竜の謳に、両手の荷を離せない旅人がいた。キイラは二挺の銃を机へ並べる。',
'何も持たず歩くのは落ち着かない。軽口のあと、あなたの反応を確かめた。',
'あなたは武器庫の鍵を見せ、今夜は庭で食事だけだと話した。彼女は片方を預け、もう片方へ手を置く。',
'扇を探すために手を離すと、銃は動かなかった。当たり前のことを、少し長く見ていた。',
'台所で麺をほぐす仕事を頼まれた。両手が必要になり、彼女は残りの銃も預けた。',
'湯気で前髪が湿る。あなたが布を渡すと、彼女は扇が使えないと笑った。',
'謳の旅人は、手を空けて初めて差し出された椀を受け取った。キイラは自分の椀を両手で持つ。',
'食事の間、物音に振り向くことはあった。それでも席を立つ前に、あなたへ声を掛けた。',
'帰りに銃を受け取り、礼を言った。貸し借りの線を引く前に、また作ろうと誘う。',
'机には扇だけが残った。あなたが届けると、彼女は明日の席をもう一つ空けていた。'])],
'anathema':[
('青薔薇の椅子','colossus.red-crystal-tyrant',[
'紅晶暴君の謳には、誰も座らない豪華な椅子があった。アナスタシアは彫刻を褒めた後、背もたれの高さを測った。',
'庭の茶会にも立派な椅子を置きたいという。あなたは、運ぶ前に座る人へ聞こうと提案した。',
'庭師は作業着を気にし、柔らかな布の椅子を避けた。アナスタシアは驚いて、自分の袖を見た。',
'雨の後は泥の付く道だった。彼女は椅子を運びかけた足を止め、まず板を敷いた。',
'謳の王は椅子を守るために客を追い返した。彼女は席を空けるための準備こそ大切だと呟く。',
'茶器は揃わなかった。柄の違う椀をあなたが並べると、彼女は一つずつ向きを整えた。',
'最初の客が笑って腰掛けた。アナスタシアは礼儀の説明を飲み込み、温かい茶を注いだ。',
'自分の席は最後になった。あなたが隣を引くと、そこでよいのかと小さく確かめる。',
'「ええ、今日はここが上座ですわ」。彼女はあなたへだけ聞こえる声で言い、焼き菓子を分けた。',
'夕方、椅子には泥の跡が残った。アナスタシアは楽しそうに拭き、次も使うと言った。']),
('手紙の中のポメ','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を読む机へ、彼女は犬の小さな絵を置いた。ポメのことを、今日は話してみたいという。',
'失った暮らしの話をすると、昔の家へ全部戻りたくなるのかと自分でも考えてしまう。',
'あなたは絵を急いで持ち上げず、彼女が向けるのを待った。丸い耳と、勢いのある尾が描かれていた。',
'謳の旅人は、離れた相手が幸せでも自分を忘れたと嘆いた。アナスタシアは、その気持ちが分かると言った。',
'しばらく黙った後、幸せに暮らしているならよかったとも話す。どちらも本当で、順番を決められない。',
'庭の犬の散歩を頼まれた。彼女は似た姿を探さず、その犬の歩く速さを聞いた。',
'途中で水を欲しがると、あなたと器を探した。昔の習慣を、そのまま押し付けずに済んだ。',
'帰ってきた犬は飼い主へ駆けた。アナスタシアは手を振り、今度はちゃんと笑った。',
'絵の裏へ今日の日付を書いた。忘れないことと、ここで暮らすことは一緒にできると話した。',
'あなたへ次の散歩も誘った。机の絵をしまう手は、別れをやり直す手ではなく、明日の約束を作る手だった。']),
('誓いを言い直す','colossus.sky-tower-machine',[
'塔の謳に、立派な誓いを守るために助けを断った騎士がいた。彼女は剣の鞘へ手を置く。',
'騎士なら一人で片付けるべきだと、昔なら言ったかもしれない。あなたが荷車を示すと、少し困った顔をした。',
'茶会の道具を運ぶ仕事だった。重さは持てても、扉を開ける手が足りない。',
'彼女は剣を外し、あなたへ戸を頼んだ。小さな依頼なのに、言い終えるまで時間が掛かった。',
'庭の風で布が飛んだ。あなたが荷を置く間、アナスタシアが走って拾った。',
'一人で完璧に運ぶ予定は崩れた。それでも誰もけがをせず、茶器も揃った。',
'謳の騎士は、誓いを言い直す言葉を持たなかった。彼女は新しい紙へ、助けが必要なら頼むと書く。',
'あなたが署名を求めると、大げさですわと笑った。けれど、自分の名前を丁寧に書いた。',
'「明日も、隣にいてくださる？」。仕事の依頼より静かな声へ、あなたは返事をした。',
'青薔薇の剣は椅子の脇に置かれた。騎士の誓いは、隣の人へ椀を渡す仕草にも続いていた。'])],
'shiva':[
('応援の声が届くまで','colossus.sky-tower-machine',[
'塔の謳を読んだまりかは、大きな声で呼べば届くのにと言った。窓の外では、休憩中の庭師が目を閉じている。',
'あなたが声を落とすと、彼女も気づいた。元気を分けたい相手が、今は眠りたいかもしれない。',
'朝の練習へ誘う紙を作った。まりかは集合時刻の隣に、途中で休んでもよいと書き足す。',
'最初の一周は皆で走った。二周目に遅れた人を見つけ、彼女は戻って歩く速さを尋ねた。',
'頑張れと言いかけ、肩の力を抜く。相手が話し終えるまで、環を投げずに待った。',
'謳の塔からは命令だけが響いた。応える声を聞かなければ、閉じた窓と変わらないと彼女は話す。',
'練習の後、温かい飲み物を分けた。走らなかった人にも、まりかは同じ椀を差し出した。',
'あなたが楽しかったと伝えると、彼女はほっと笑う。大きな歓声より、その返事を待っていた。',
'次の予定には、散歩の組もできた。まりかは両方の道を確かめ、戻る場所を一つにした。',
'庭の椅子で並んで休む。応援する声は静かでも、あなたの隣にはちゃんと届いていた。']),
('懐音先輩への便り','colossus.memory-crystal-dragon',[
'記憶晶竜の謳を整理する机で、まりかは便箋を広げた。懐音先輩へ何から書こうか迷っている。',
'楽しい話ばかり並べると、今日の疲れを隠してしまう。彼女は最初の一行を消した。',
'あなたは今日の練習を尋ねた。まりかは走り過ぎたことも、皆で休めたことも話した。',
'謳には、過去の声に返事をし続けた人がいる。彼女は便箋へ、今ここで会った人のことを書いた。',
'懐音のことを大切に思う話を、あなたにも聞いてほしいと言う。あなたは筆を置き、続きを待った。',
'遠い記憶の意味を、一つの答えへ急ぎたくない。まりかは知っている声と、分からないことを分けて話した。',
'窓辺の花が風で傾いた。あなたが鉢を戻すと、彼女はその仕草を手紙へ書き加えた。',
'「今度、この人の話もしたいな」。あなたが照れると、まりかは筆を止めずに笑った。',
'封をする前に、返事を急がなくてよいと添えた。便りを送ることと、相手の暮らしを待つことを一緒に選んだ。',
'庭へ出る時、あなたを明日の散歩へ誘った。思い出を話した後にも、新しい約束をする声が弾んでいた。']),
('世界より小さな約束','colossus.red-crystal-tyrant',[
'紅晶暴君の謳を読んだ夜、まりかは世界を救うという言葉の大きさに黙った。机には明日の買い物表がある。',
'全部を引き受ければ誰かが楽になる。そう考えて、先に返事をしたことはないかと尋ねた。',
'あなたは表の空欄を見せた。頼める人へ聞く前に、まりかの名前ばかり書くつもりはなかった。',
'彼女は魚を選ぶ役を引き受け、重い荷は一緒に持ってほしいと頼んだ。',
'朝市へ行くと、予定の店は閉まっていた。まりかは慌てたが、あなたと別の食材を探した。',
'一つの予定が変わっても、食事は作れる。彼女は籠の葉物を見て、ようやく息を吐いた。',
'謳の王は、自分以外の答えを許さなかった。まりかは台所で皆の好みを聞き、味付けを変えた。',
'食事の後に眠くなると、無理に片付けを続けなかった。あなたへ頼み、明日は自分が洗うと約束した。',
'「あたしがついてる、って。一人で全部やるって意味じゃないよね」。あなたが頷くと、肩を寄せて笑った。',
'世界を変える宣言はなかった。明日の皿を洗う約束が、まりかをあなたの隣へ帰してくれた。'])]}

EVENTS={
'iron-maiden':[
('湯気の向こうの素顔','庭の長机で、エリザベートが空いた椅子を示した。|あなたが茶を注ぐと、彼女は熱さを確かめてから手を添える。|今日は見張りの交代を済ませてきたという。|「急いで飲まなくていいの。私も、そうしてみるわ」|砂糖を一つ入れ、少し考えてもう一つ足した。|あなたが見ていると、甘い物は嫌いではないと笑う。|次は自分が茶を選ぶと、店の場所を尋ねた。|冷める前に飲み終えた椀の脇へ、次の約束が残った。'),
('宛名を選ぶ時間','机の手紙には、エリザベートとだけ書かれていた。|役目ではなく自分へ届いた便りだと、彼女は説明した。|返事の書き方を尋ねられ、あなたは今日の茶の話を挙げる。|彼女は笑い、砂糖の数まで書く必要があるかと聞いた。|便箋へ向かう横顔は、いつもの見張りより穏やかだった。|あなたが立つと、もう少し隣で待ってほしいと言う。|封を折る時、彼女はあなたの名前も確かめた。|次は二人で店へ行ったことを書ける、と手紙を胸へ寄せた。'),
('守りたい席','庭の灯りの下で、エリザベートがあなたを待っていた。|今夜は護衛の相談ではないと、先に話す。|あなたが隣へ座ると、手袋の指をゆっくりほどいた。|「あなたが好き。仕事が終わった後にも、会いたい人なの」|あなたも好きだと答え、恋人としてそばにいたいと伝えた。|彼女は返事を確かめ、つないでよいかと手を差し出す。|握る手の温かさに、いつもの余裕が少し崩れた。|帰り道は交代の時刻ではなく、明日会う時刻を決めながら歩いた。'),
('一輪を自分のために','温室で、彼女はあなたへ花の鉢を見せた。|贈る相手を尋ねると、今日は自分の窓へ置くと言う。|あなたは日当たりのよい場所を一緒に探した。|エリザベートは鉢を抱え、あなたが来た時にも見える高さを選ぶ。|水をやり過ぎないよう、二人で印を付けた。|「会う理由を増やしたいだけかもしれないわね」|あなたが明日も見に来ると言うと、嬉しそうに頷いた。|花がまだ咲かなくても、その窓辺で待つ約束はできた。'),
('鍵を置いた夕方','夕方の庭で、彼女は鍵束を鞄へしまった。|あなたが来ると、空いた手で隣を叩く。|今日の失敗を話すと、彼女も茶葉を取り違えた話をした。|二人で笑う間、見張りの声は遠くで別の人が返した。|「今日は、ここへ帰ってきたかったの」|あなたが肩を寄せると、エリザベートも少し体を預けた。|次の休みに歩きたい道を、小さな地図へ書く。|日が沈んでも急がず、彼女はあなたの返事を聞いてから立ち上がった。')],
'avalanche':[
('扇の裏の約束','キイラは庭の椅子を扇で示した。|あなたが座ると、今日は勝負抜きだと先に言う。|持ってきた菓子を分け、欠けた方を自分で取った。|あなたが替えようとすると、味は同じだと笑った。|昔の癖で損得を考えるが、あなたの前では休みたいという。|あなたは茶を足し、好きなだけ休んでよいと答えた。|彼女は扇の裏へ、次のお茶の時刻を書いた。|帰り際、借りではなく誘いだからねと念を押した。'),
('数えない一枚','机に並べた真鍮の札を、キイラが磨いていた。|余った一枚をあなたへ渡し、何に使うか考えてと言う。|あなたは次の食事の目印にしようと答えた。|彼女は意外そうに見てから、自分の札を隣へ置く。|二枚を同じ袋へ入れ、返済の印ではないと確かめた。|磨いた指先に粉が付いており、あなたが布を渡す。|キイラは礼を言い、今度は帳面を出さなかった。|食事の場所を相談する声が、夕方まで途切れなかった。'),
('逃げない返事','月明かりの橋で、キイラは扇を閉じた。|冗談にして逃げられる言い方を、今日はやめたいという。|あなたが待つと、彼女は真っすぐ顔を上げた。|「あんたが好き。一緒にいる明日を、あたしが選びたい」|あなたも好きだと答え、恋人になりたいと伝えた。|キイラは笑いかけて、少しだけ目を潤ませた。|つないだ手は離さず、帰る速さをあなたへ合わせる。|橋を渡った後も、二人で歩く道を自分から選んだ。'),
('湯気まで半分','屋台の端で、キイラが二つの椀を待っていた。|今日は好みを聞いてから頼んだと、得意そうに話す。|あなたがひと口食べるまで、彼女は箸を止めて見ていた。|おいしいと答えると、ようやく自分も食べ始めた。|辛味を足し過ぎ、二人で水を探して笑う。|「次は、あたしが作る方を食べてよ」|あなたが楽しみだと答えると、真剣に具の名前を覚えた。|空いた椀を並べ、帰りには恋人の手を自然に取った。'),
('朝の席は二つ','日の出前、キイラは庭の席へ扇を置いた。|あなたが眠そうに来ると、温かい茶を渡す。|何か得があるのかと冗談を言い、自分で首を振った。|ただ同じ朝を見たかったと、静かな声で話す。|あなたが隣へ寄ると、肩の間の隙間がなくなった。|光が差すまで、帳面も銃も鞄の中にあった。|次はもっと遅い時間にしようと、二人で相談する。|キイラは席の札を二枚重ね、また来ようと笑った。')],
'anathema':[
('お茶会の招待状','アナスタシアは小さな招待状をあなたへ渡した。|庭のすぐ隣なのに、丁寧な地図まで描いてある。|あなたが時刻通りに着くと、彼女は先に席を温めていた。|茶葉を選んだ理由を話し、好みに合うか少し心配そうに見る。|あなたがおいしいと答えると、胸を張るまで一拍掛かった。|今日は格式より、あなたが来たことが嬉しいという。|焼き菓子を二つに分け、欠けた所を笑い合った。|次の招待は口で伝えてもよいかと、帰り際に尋ねた。'),
('絵の裏の近況','机に置かれた犬の絵を、彼女がそっと向けた。|ポメの話を、今日は嬉しい所まで聞いてほしいという。|あなたは椅子を寄せ、彼女の話す速さに合わせた。|寂しさがなくなったわけではないと、アナスタシアは話す。|それでも幸せでいてほしいと、自分の声で言えた。|あなたが頷くと、絵の裏へ今日の散歩の話を書いた。|「あなたにも、また聞いていただきたいですわ」|その願いを聞いたあなたへ、彼女は新しい茶を淹れた。'),
('名前で交わす誓い','青薔薇の前で、アナスタシアは剣を置いた。|騎士の宣言より、自分の願いを聞いてほしいという。|あなたが向き合うと、誇らしげな声が少し震えた。|「あなたが好きです。アナスタシアとして、隣にいたいの」|あなたも好きだと伝え、恋人として一緒に歩みたいと答えた。|彼女は目を見開き、やがて嬉しそうに頷いた。|手を取ってよいかと尋ね、返事を待ってから指を重ねる。|二人の誓いは小さな声で交わされ、帰る道にも続いていた。'),
('焼き色の相談','台所で、アナスタシアが丸い生地を並べていた。|恋人へ出すのだから完璧に、と言いかけてあなたを呼ぶ。|同じ厚さにする方法を尋ねられ、二人で板を探した。|袖へ粉が付くと、彼女は少しだけ照れて笑った。|焼き上がりの一つは色が濃く、あなたが半分を取った。|彼女も食べてみて、次は時間を短くしようと頷く。|「また一緒に作ってくださる？」|あなたの返事を聞き、失敗した分まで包んで大切に持ち帰った。'),
('窓辺の休息','夕方の窓辺で、彼女は青薔薇の剣を外した。|あなたへ温かい椀を渡し、自分も椅子へ腰掛ける。|今日は何も立派なことをしていないと、少し困った顔をした。|あなたは一緒に食事をしたかったと伝えた。|アナスタシアは安心したように笑い、肩を寄せる。|明日の予定へ、ただ休む時間を一つ残した。|あなたがその時間に会いたいと言うと、嬉しそうに印を付けた。|窓の灯りを消す前に、恋人の名前を静かに呼んだ。')],
'shiva':[
('一周だけの約束','まりかは草原で、投げる環をくるりと回した。|あなたを練習へ誘い、疲れたら歩こうと先に伝える。|一周の途中で息が上がると、自分から速さを落とした。|笑いながら戻る道で、今日の空の色を教えてくれる。|庭へ着くと、冷たい水ではなく温かい茶を選んだ。|「楽しかったって、また言ってもらいたいな」|あなたが次も来たいと答えると、まりかの声が弾んだ。|二人の予定には、走った後に休む時間も書かれた。'),
('便箋の空いた行','まりかは手紙の最後の行で筆を止めていた。|懐音先輩へ、あなたのことも書きたいという。|何と呼ぶか考える顔を、あなたは隣で見守った。|元気な時も疲れた時も会いたい人、と彼女は書いた。|読み上げてから照れ、勝手に決めてよかったかと尋ねる。|あなたは嬉しいと答え、今日の話を一つ足した。|まりかは封を折り、返事が来たらまた話そうと笑う。|遠い友人の話も、今日の約束も、同じ机へ置けた。'),
('夕焼けの返事','夕焼けの草原で、まりかは環を鞄へしまった。|応援の言葉ではなく、今の自分の気持ちを伝えたいという。|あなたが待つと、両手を胸の前で重ねた。|「あなたが好き。明日も、その先も、一緒にいたい」|あなたも好きだと答え、恋人として隣にいたいと伝えた。|まりかは嬉しそうに笑い、もう一度聞いてよいかと尋ねる。|同じ返事をすると、そっとあなたの手を取った。|帰り道は走らず、手の温かさを確かめながら歩いた。'),
('半分ずつの昼ごはん','庭の布の上に、まりかが二つの椀を並べた。|今日は恋人と食事をする日だと、少し照れて話す。|果物を切る役をあなたへ頼み、自分は茶を用意した。|大きさが揃わなくても、先に笑って好きな方を選ぶ。|あなたが疲れていないか聞くと、今日はよく眠ったと答えた。|「休んでから来るのも、約束のうちだね」|食後は片付けを分け、余った時間を隣で過ごした。|明日の弁当の相談が、日陰の移る頃まで続いた。'),
('星を待つ二人','まりかは庭の椅子で、空が暗くなるのを待っていた。|あなたが来ると、羽を少し畳んで席を空ける。|今日うまく言えなかったことを、途中で止まりながら話した。|あなたは続きを急がず、温かい椀を手渡した。|星が一つ見えると、彼女は声を上げかけて笑った。|「何もない日も、あなたと話したいな」|あなたが頷くと、まりかは肩へ静かに寄った。|帰る前に明日会う場所を決め、今夜の星をもう一度見上げた。')]}

def remap(v,old,new):
    if isinstance(v,str):return v.replace(old,new)
    if isinstance(v,list):return [remap(x,old,new) for x in v]
    if isinstance(v,dict):return {k:remap(x,old,new) for k,x in v.items()}
    return v
def build():
    res=ROOT/'game/unity/Assets/Game/Resources'
    combat=read(res/'Combat/battle-plan12-aeon.json');story=read(res/'Story/plan12-aeon-story-content.json')
    for p in PEOPLE:
        hero='heroine.'+p['key'];base='heroine.aeon'
        h=remap(copy.deepcopy(next(x for x in combat['heroines'] if x['id']==base)),base,hero)
        h.update(name=p['name'],jobId='job.'+p['job'],secondTraitId=p['second'],hpBp=p['hp'],attackBp=p['attack'],defenseBp=p['defense'],speedBp=p['speed'],traitHpPercent=5 if p['job'] in ['defender','healer'] else 0,traitAttackPercent=0 if p['job'] in ['defender','healer'] else 5,reactionStyleMain=p['styles'][0],reactionStyleSub=p['styles'][1],interactionTraitIds=p['traits'])
        combat['heroines'].append(h)
        for slot,(name,power,wait) in enumerate(p['skills'],1):
            s=remap(copy.deepcopy(next(s for s in combat['skills'] if s['id']==base+'.'+str(slot))),base,hero)
            s.update(name=name,powerScale=power,recoveryPercent=wait,partScale=1,selfEffects=[],statusEffects=[],attributes=['銃弾'] if p['job']=='gunner' else ['斬撃'] if p['job']=='fighter' else ['火'] if p['job']=='defender' else ['打撃'],targetRuleId='target.selected-enemy',damageType='physical',sourceFile=f'{p["number"]:03}戦闘_{p["name"]}.mkv')
            if p['key']=='iron-maiden':
                if slot==1:s.update(statusEffects=[dict(kind='burn',amount=60)],enemyWaitAdd=30)
                elif slot==2:s.update(effectRuleId='effect.self-buff',targetRuleId='target.self',damageType='',attributes=[],chainEligible=False,damageCap=0,selfEffects=[dict(kind='physical-protection',percent=25,turns=3)],selfHealingBaseAttackPercent=80)
                else:s.update(targetRuleId='target.all-enemies',statusEffects=[dict(kind='burn',amount=40)],postAttackAlliesEffects=[dict(kind='physical-protection',percent=15,turns=2)])
                if slot>1:s.update(ruleOrigin='newaster-original',sourceFile='',sourceSecond=0,observedSkillLevel=0)
            elif p['key']=='avalanche':
                if slot==1:s.update(selfEffects=[dict(kind='attack-reduction',percent=15,turns=3)])
                if slot==3:s.update(chainChanceBonusBp=1000,ruleOrigin='newaster-original',sourceFile='',sourceSecond=0,observedSkillLevel=0)
            elif p['key']=='anathema':
                if slot==2:s.update(ignoreDefenseBp=2000)
            elif p['key']=='shiva':
                if slot==1:s.update(ignoreDefenseBp=10000,attributes=['斬撃'])
                elif slot==2:s.update(effectRuleId='effect.heal',targetRuleId='target.all-living-allies',targetCount=5,damageType='',attributes=[],chainEligible=False,damageCap=0,selfEffects=[dict(kind='attack',percent=20,turns=3),dict(kind='critical',percent=15,turns=3)])
                else:s.update(targetRuleId='target.all-enemies',statusEffects=[dict(kind='stun',amount=40)],alliesHealingBaseAttackPercent=60)
            combat['skills'].append(s)
        chain=remap(copy.deepcopy(next(c for c in combat['chainActions'] if c['heroineId']==base)),base,hero);chain['powerScale']=.6;combat['chainActions'].append(chain)
        combat['contentReferences'] += [remap(copy.deepcopy(r),base,hero) for r in combat['contentReferences'] if r['ownerId']==base]
        extra=dict(schemaVersion=story['schemaVersion'],contentVersion=story['contentVersion'],provenance='newaster-original',chapters=[],events=[])
        for ci,(title,source,pages) in enumerate(CHAPTERS[p['key']],1):
            assert len(pages)==10
            source_chapter=next(c for c in story['chapters'] if c['ownerId']==source and c['id'].endswith('.1'))
            poems=[dict(id=f'{hero}.collection.poem.{(ci-1)*6+pi:02}',text=body.split('。')[0]+'。',body=body,sourcePoemId=source_chapter['poems'][pi-1]['id'],reason=f'「{source_chapter["poems"][pi-1]["text"]}」を受け、{title}で今の暮らしへ応答する。') for pi,body in enumerate(pages[1:7],1)]
            extra['chapters'].append(dict(id=f'{hero}.poem-chapter.{ci}',ownerId=hero,title=title,introduction=pages[0],conclusion=pages[-1],backgroundResourcePath='Illustrations/forest-far-candidate-v1',poems=poems,pages=pages))
        for ei,(title,text) in enumerate(EVENTS[p['key']]):
            paragraphs=text.split('|');assert len(paragraphs)==8
            extra['events'].append(dict(id=f'{hero}.event.{ei}',ownerId=hero,title=title,cgResourcePath=f'Illustrations/{p["key"]}-event-{ei}-cg-candidate-v1',backgroundResourcePath='Illustrations/forest-far-candidate-v1',affectionRequired=[1,5,10,15,20][ei],establishesLover=ei==2,paragraphs=paragraphs,expressions=['normal','puzzled','normal','determined','joy','normal','joy','joy']))
        story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(OUT/(p['key']+'-story.json'),extra)
    write(OUT/'combat.json',combat);write(OUT/'story.json',story);write(OUT/'people.json',PEOPLE)
    write(OUT/'manifest.json',dict(schemaVersion=1,stage='dedicated-art-and-validation-in-progress',available=False,formIds=['heroine.'+p['key'] for p in PEOPLE],baselineForms=16,targetForms=20,requiredAssetsPerForm=21,sourceAdaptations={'iron-maiden':'動画の1技を収録。中断30%は本作の敵WT+30へ変更。2・3技は本作オリジナル。','avalanche':'動画の2技を収録。3技は本作オリジナル。','anathema':'防御-100は本作の防御20%無視へ変更。騎士の誓いは低HP時の被害15%軽減へ調整。','shiva':'動画の3技を収録。ポジティブ思考は本作の毎ターン2%自己回復へ調整。'},sourceStoryPolicy='人物の関係と性格を参考にした本作オリジナル。転生説・年齢・作品横断同一人物説を確定扱いしない。'))
    print('FIRST_BATCH_AUTHORED 4 people / 12 chapters / 120 pages / 72 poems / 20 events / 12 skills; not promoted')
if __name__=='__main__':build()
