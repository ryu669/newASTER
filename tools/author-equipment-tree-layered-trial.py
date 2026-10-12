"""Author a shared vector tree and an ornament-only layering preview.
The generated decoration PNG remains untouched and separate from the vector base.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'game/art-source/equipment-tree-layered-trial-v1'
OUT.mkdir(parents=True,exist_ok=True)
anchors=[(.5,.85),(.36,.52),(.5,.58),(.64,.52),(.275,.36),(.5,.43),(.725,.36),(.185,.25),(.5,.28),(.815,.25),(.1,.15),(.5,.13),(.9,.15)]
paths=[
 ('M500 570 C450 615 370 635 270 640 M500 570 C550 615 630 635 730 640',22),
 ('M500 580 C475 515 520 470 500 387 C480 335 520 300 500 287 C480 242 510 220 500 187 C486 155 500 110 500 87',40),
 ('M500 566 C490 465 420 415 360 347 C330 308 305 280 275 240 C235 212 210 194 185 167 C150 141 125 125 100 100',30),
 ('M500 566 C510 465 580 415 640 347 C670 308 695 280 725 240 C765 212 790 194 815 167 C850 141 875 125 900 100',30),
 ('M285 635 C335 600 390 610 450 590 M715 635 C665 600 610 610 550 590',10),
 ('M360 347 Q295 355 255 310 M640 347 Q705 355 745 310',10),
 ('M275 240 Q225 255 202 220 M725 240 Q775 255 798 220',8),
 ('M185 167 Q160 180 135 165 M815 167 Q840 180 865 165',6)]
svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 666"><defs><linearGradient id="bark" x1="0" y1="0" x2="1" y2="0"><stop stop-color="var(--bark-dark,#26272d)"/><stop offset=".5" stop-color="var(--bark-light,#92969f)"/><stop offset="1" stop-color="var(--bark-dark,#26272d)"/></linearGradient></defs><g fill="none" stroke-linecap="round" stroke-linejoin="round">'
for d,w in paths:
    svg+=f'<path d="{d}" stroke="#11131b" stroke-width="{w+5}"/><path d="{d}" stroke="url(#bark)" stroke-width="{w}"/><path d="{d}" stroke="var(--vein,#b8b9bd)" stroke-opacity=".45" stroke-width="2"/>'
svg+='</g></svg>'
(OUT/'common-tree.svg').write_text(svg,encoding='utf-8')
labels=['根','攻撃Ⅰ','守護Ⅰ','技巧Ⅰ','攻撃Ⅱ','守護Ⅱ','技巧Ⅱ','攻撃Ⅲ','守護Ⅲ','技巧Ⅲ','攻撃Ⅳ','守護Ⅳ','技巧Ⅳ']
nodes='<svg class="nodes" viewBox="0 0 1000 666">'
for (x,y),label in zip(anchors,labels):
    nodes+=f'<g transform="translate({x*1000},{y*666})"><circle r="29" fill="#131e2a" stroke="#c9b57d" stroke-width="3"/><circle r="21" fill="var(--node,#8f2437)" stroke="#e8d7aa" stroke-width="1"/><text y="5" text-anchor="middle" fill="#fff5df" font-size="14" font-family="sans-serif">{label}</text></g>'
nodes+='</svg>'
ornaments=''.join(f'<img class="ornament" src="iron-maiden-decoration-v1.png" style="left:{x}%;top:{y}%;width:{size}%;transform:translate(-50%,-50%) rotate({angle}deg)"/>' for x,y,size,angle in [(50,73,21,0),(38,55,24,-42),(62,55,24,42)])
def tree(decorated):return '<div class="tree">'+svg+(ornaments if decorated else '')+nodes+'</div>'
html='''<!doctype html><html lang="ja"><meta charset="utf-8"><title>アイアンメイデン 神器の木 試作</title><style>
*{box-sizing:border-box}body{margin:0;background:#0a101a;color:#eee7d6;font-family:"Yu Gothic",sans-serif}main{max-width:1900px;margin:auto;padding:28px}h1{font-size:27px;margin:0 0 8px}p{color:#b5bfcc;margin:8px 0 24px}.panels{display:grid;grid-template-columns:1fr 1fr;gap:22px}section{border:1px solid #776948;border-radius:12px;background:radial-gradient(ellipse at center,#29313e,#101822 80%);overflow:hidden}h2{font-size:19px;margin:0;padding:16px 24px;border-bottom:1px solid #514b3e;color:#ddc99c}.tree{position:relative;width:100%;aspect-ratio:1000/666;--bark-dark:#24262e;--bark-light:#a1a5ad;--vein:#d5d5d9;--node:#80283b}.tree>svg{position:absolute;inset:0;width:100%;height:100%}.ornament{position:absolute;height:auto;pointer-events:none}.nodes{z-index:2}footer{display:flex;gap:18px;align-items:center;margin-top:24px}button{background:#263445;color:#eee7d6;border:1px solid #b29a65;border-radius:6px;padding:10px 22px;font-size:16px;cursor:pointer}.hide-nodes .nodes{display:none}small{color:#aab5c3}@media(max-width:900px){.panels{grid-template-columns:1fr}}
</style><main><h1>アイアンメイデン — 神器の木の試作</h1><p>銀と黒の木に、赤い宝石と銀の葉の装飾を重ねています。</p><div class="panels"><section><h2>共通の木・配色のみ</h2>'''+tree(False)+'''</section><section><h2>専用装飾を重ねた状態</h2>'''+tree(True)+'''</section></div><footer><button onclick="document.body.classList.toggle('hide-nodes')">ノード表示の切替</button><small>構造と13ノードの位置は共通。装飾だけを別画像で追加した試作です。</small></footer></main></html>'''
(OUT/'preview.html').write_text(html,encoding='utf-8')
(OUT/'layout.json').write_text(json.dumps(dict(schemaVersion=1,status='prototype',base='common-tree.svg',heroineId='heroine.iron-maiden',decoration='iron-maiden-decoration-v1.png',nodes=[dict(x=x,y=y) for x,y in anchors],decorationPlacement=[dict(x=x/100,y=y/100,width=size/100,rotation=angle) for x,y,size,angle in [(50,73,21,0),(38,55,24,-42),(62,55,24,42)]],layerOrder=['shared tree','heroine decoration','interactive nodes and labels']),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(OUT/'preview.html')
