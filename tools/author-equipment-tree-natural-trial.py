"""Compose a browser prototype using untouched shared-tree and small-applique images."""
from pathlib import Path
import json,shutil,hashlib,uuid
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'game/art-source/equipment-tree-layered-trial-v2'
p=OUT/'generation.json';manifest=json.loads(p.read_text(encoding='utf-8'))
for a in manifest['assets']:
    dest=OUT/(a['key']+'.png');shutil.copyfile(a['source'],dest)
    im=Image.open(dest);assert im.mode=='RGBA' and im.getchannel('A').getextrema()[0]==0
    a.update(path=dest.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),nativeSize=list(im.size),alphaRange=list(im.getchannel('A').getextrema()))
p.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
anchors=[(.50,.86),(.36,.60),(.48,.66),(.65,.59),(.26,.52),(.50,.49),(.79,.46),(.14,.40),(.54,.32),(.86,.34),(.10,.23),(.48,.14),(.88,.20)]
# All positions are shared between plain and decorated views; per-hero ornaments never move nodes.
placements=[('silver-leaf',.20,.34,.055,-30),('silver-leaf',.32,.43,.05,20),('silver-leaf',.40,.30,.045,-18),('silver-leaf',.61,.22,.055,35),('silver-leaf',.76,.55,.05,65),('silver-leaf',.89,.48,.045,-40),('ruby',.23,.57,.07,0),('ruby',.43,.20,.06,-10),('ruby',.60,.54,.07,10),('ruby',.81,.28,.065,0),('ruby',.56,.79,.07,-5),('iron-clasp',.40,.64,.07,-30),('iron-clasp',.64,.56,.07,20),('iron-clasp',.52,.39,.06,55),('iron-clasp',.43,.79,.07,-40)]
nodes='<svg class="nodes" viewBox="0 0 1000 666">'
for i,(x,y) in enumerate(anchors):
    route=(i-1)%3 if i else 0;tier=(i-1)//3+1 if i else 0
    name='サラマンドラの根' if i==0 else 'サラマンドラ・'+['イグニス','アルクス','ウェントゥス'][route]+['Ⅰ','Ⅱ','Ⅲ','Ⅳ'][tier-1]
    label='根' if i==0 else ['Ⅰ','Ⅱ','Ⅲ','Ⅳ'][tier-1]
    nodes+=f'<g class="node" onclick="document.getElementById(\'selected\').textContent=\'{name}\'" transform="translate({x*1000},{y*666})"><title>{name}</title><circle r="23" fill="#1a1e21" stroke="#c6b382" stroke-width="3"/><circle r="16" fill="#762e39"/><text y="5" text-anchor="middle" fill="#fff4dc" font-size="15">{label}</text></g>'
nodes+='</svg>'
ornaments=''.join(f'<img class="ornament" src="{key}.png" style="left:{x*100}%;top:{y*100}%;width:{size*100}%;transform:translate(-50%,-50%) rotate({angle}deg)"/>' for key,x,y,size,angle in placements)
def tree(decor):return '<div class="tree"><img class="base" src="common-tree.png">'+(ornaments if decor else '')+nodes+'</div>'
html='''<!doctype html><html lang="ja"><meta charset="utf-8"><title>アイアンメイデン・自然な木の試作</title><style>*{box-sizing:border-box}body{margin:0;background:#111823;color:#eee5d3;font-family:"Yu Gothic",sans-serif}main{padding:25px 30px;max-width:1920px;margin:auto}h1{font-size:25px;margin:0 0 10px}p{color:#b4becb;margin:8px 0 20px}.panels{display:grid;grid-template-columns:1fr 1fr;gap:22px}section{border:1px solid #766747;border-radius:10px;background:radial-gradient(ellipse at center,#d5cbb6,#b5aa94);overflow:hidden}h2{font-size:18px;padding:14px 22px;margin:0;background:#1c2735;color:#ddcc9f}.tree{position:relative;width:100%;aspect-ratio:1.5}.base,.nodes{position:absolute;inset:0;width:100%;height:100%;object-fit:contain}.ornament{position:absolute;height:auto;pointer-events:none}.nodes{z-index:2;pointer-events:none}.node{pointer-events:auto;cursor:pointer}.node:hover circle{stroke:#fff4bc;stroke-width:4}.hide-nodes .nodes{display:none}footer{display:flex;gap:22px;align-items:center;margin-top:18px}button{padding:10px 18px;background:#263748;color:#fff1ce;border:1px solid #b8a26e;border-radius:6px;font-size:16px}#selected{font-size:19px;color:#e8d5a5}small{color:#aeb9c6}</style><main><h1>アイアンメイデン — 自然な木と小装飾</h1><p>樹皮・細枝・根のある共通の木に、小さな銀葉・深紅の宝石・黒鉄の留め具を散らした試作です。</p><div class="panels"><section><h2>共通の木</h2>'''+tree(False)+'''</section><section><h2>専用の小装飾を追加</h2>'''+tree(True)+'''</section></div><footer><button onclick="document.body.classList.toggle('hide-nodes')">ノード表示の切替</button><span id="selected">サラマンドラ・イグニスⅠ</span></footer><p><small>ノードを選ぶと武器名を表示します。Ⅰ〜Ⅳはルートごとの強化段階です。</small></p></main></html>'''
(OUT/'preview.html').write_text(html,encoding='utf-8')
(OUT/'layout.json').write_text(json.dumps(dict(status='prototype',nodes=anchors,decorations=placements,layerOrder=['shared natural tree','small dedicated decorations','nodes and labels']),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
meta=ROOT/'game/unity/Assets/Game/Scripts/Core/WeaponNodeNames.cs.meta'
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('NATURAL_TREE_TRIAL_READY 13 shared nodes; 3 ornament assets / 15 placements')
