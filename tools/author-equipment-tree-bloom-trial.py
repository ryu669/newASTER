"""Interactive prototype: fixed shared tree, larger ornaments, per-route unlocked blossoms."""
from pathlib import Path
import json,shutil,random
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'game/art-source/equipment-tree-layered-trial-v2';OUT=ROOT/'game/art-source/equipment-tree-layered-trial-v3'
OUT.mkdir(parents=True,exist_ok=True)
for p in SRC.glob('*.png'):
    if p.name!='comparison.png':shutil.copyfile(p,OUT/p.name)
layout=json.loads((SRC/'layout.json').read_text(encoding='utf-8'))
layout.update(status='interactive-prototype',ornamentScale=1.85,flowerColor='#991b35',flower='crimson-flower.png',foliage='silver-foliage-simple-v2.png',bloomRule='flowers surround unlocked non-root nodes only; root never blooms')
# Read alpha for placement only. No source pixels are edited or composited here.
alpha=Image.open(OUT/'common-tree.png').getchannel('A');width,height=alpha.size
layout['nodes']=[[.10+.8*x,.10+.8*y] for x,y in layout['nodes']]
layout['decorations']=[[key,.10+.8*x,.10+.8*y,size*.8,angle] for key,x,y,size,angle in layout['decorations']]
# Flowers decorate unlocked nodes only; the initial root stays flower-free.
slots=[]
for tier in range(1,5):
    for route in range(3):
        x,y=layout['nodes'][1+(tier-1)*3+route]
        for j,(dx,dy) in enumerate([(-.030,.032),(.030,.032)]):
            slots.append(dict(route=route,tier=tier,x=x+dx,y=y+dy,size=.045,angle=route*37+tier*23+j*51))
layout['flowerSlots']=slots
# Dense canopy is behind the wood, so the trunk and branch silhouettes stay visible.
rng=random.Random(731)
canopy=[]
for row in range(23):
    for col in range(37):
        x=.045+col*.025+rng.uniform(-.009,.009)
        y=.055+row*.027+rng.uniform(-.009,.009)
        if ((x-.5)/.49)**2+((y-.35)/.34)**2>1:continue
        if y>.54 and abs(x-.5)<.15:continue
        route=0 if x<.35 else (2 if x>.65 else 1)
        tier=min(4,1+int(max(0,.66-y+(col%5-2)*.012)/.155))
        canopy.append(dict(x=x,y=y,route=route,tier=tier,size=rng.uniform(.064,.085),opacity=.48,mirror=bool((row+col)%2),angle=rng.randrange(360)))
layout['canopySlots']=canopy
layout['layerOrder']=['dense unlocked foliage canopy','shared natural tree','dedicated ornaments','flowers','nodes and labels']
layout['layerOrder']=['shared natural tree','larger dedicated ornaments','foliage clusters and flowers around unlocked branches','nodes and labels']
(OUT/'layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
html='''<!doctype html><html lang="ja"><meta charset="utf-8"><title>アイアンメイデン 神器の木・開花試作</title><style>
*{box-sizing:border-box}body{margin:0;background:#111923;color:#efe7d8;font-family:"Yu Gothic",sans-serif}main{max-width:1920px;margin:auto;padding:22px}h1{font-size:25px;margin:0 0 8px}p{color:#b8c3d0;margin:8px 0 18px}.panels{display:grid;grid-template-columns:repeat(3,1fr);gap:15px}section{border:1px solid #82714f;border-radius:9px;overflow:hidden;background:#c3b8a0}h2{font-size:18px;margin:0;padding:13px 18px;background:#1c2938;color:#e2cd9e}.tree{position:relative;width:100%;aspect-ratio:1.5}.base,.nodes{position:absolute;inset:0;width:100%;height:100%;object-fit:contain}.ornament,.flower{position:absolute;height:auto;pointer-events:none}.flower{filter:drop-shadow(0 1px 1px #2a101090)}.nodes{z-index:3;pointer-events:none}.node{pointer-events:auto;cursor:pointer}.node:hover circle{stroke:#fff4c3;stroke-width:4}.hide-nodes .nodes{display:none}.hide-ornaments .ornament{display:none}.controls{margin-top:18px;display:flex;gap:18px;flex-wrap:wrap;align-items:center}button{background:#27394a;color:#f5e9cd;padding:10px 18px;border:1px solid #b29d6e;border-radius:6px;font-size:15px;cursor:pointer}label{display:flex;gap:12px;align-items:center;padding:12px 18px;background:#1c2938;border-radius:6px}input{accent-color:#a52a45;width:160px}output{min-width:26px;color:#edc3cb}.selection{font-size:19px;color:#e3cb99;margin-top:18px;min-height:29px}small{color:#aebac9}.bud{fill:#733f4a;stroke:#35262b;stroke-width:1.4}.notes{margin-top:12px;line-height:1.8}@media(max-width:1000px){.panels{grid-template-columns:1fr}}
</style><main><h1>アイアンメイデン — 装飾の拡大と開放に合わせた開花</h1><p>根元から開放した枝まで深紅の花が咲きます。中央の木はルートごとに段階を変更できます。</p><div class="panels"><section><h2>根のみ開放</h2><div class="tree" id="rootTree"></div></section><section><h2>開放段階を試す</h2><div class="tree" id="liveTree"></div></section><section><h2>全ノード開放</h2><div class="tree" id="fullTree"></div></section></div><div class="controls"><label>イグニス <input id="r0" type="range" min="0" max="4" value="2"><output id="o0">Ⅱ</output></label><label>アルクス <input id="r1" type="range" min="0" max="4" value="2"><output id="o1">Ⅱ</output></label><label>ウェントゥス <input id="r2" type="range" min="0" max="4" value="2"><output id="o2">Ⅱ</output></label></div><div class="controls"><button onclick="preset(0)">根のみ</button><button onclick="preset(2)">各ルートⅡまで</button><button onclick="preset(4)">すべて開放</button><button onclick="document.body.classList.toggle('hide-nodes')">ノード表示</button><button onclick="document.body.classList.toggle('hide-ornaments')">装飾表示</button></div><div class="selection" id="selected">サラマンドラ・イグニスⅡ</div><p class="notes"><small>ノードを選択して開放できます。未開放の枝は蕾のまま。親ノードから順に開放します。<br>装飾は前の試作の1.85倍。木・装飾・花・操作表示は別々の層です。ゲーム本体への組み込み前の試作です。</small></p></main><script>
const layout=LAYOUT, anchors=layout.nodes, variants=['イグニス','アルクス','ウェントゥス'], numerals=['未開放','Ⅰ','Ⅱ','Ⅲ','Ⅳ'];
function point(route,tier){return anchors[tier===0?0:1+(tier-1)*3+route]}
function blossom(x,y,size,angle){return `<img class="flower" src="crimson-flower.png" style="left:${x*100}%;top:${y*100}%;width:${size*100}%;transform:translate(-50%,-50%) rotate(${angle}deg)">`}
function foliage(x,y,angle,size=.075){return `<img class="foliage" src="silver-foliage-simple-v2.png" style="left:${x*100}%;top:${y*100}%;width:${size*100}%;transform:translate(-50%,-50%) rotate(${angle}deg)">`}
function render(id,levels,interactive=false){
 let content='',leafCount=0;
 for(const slot of layout.canopySlots)if(slot.tier<=levels[slot.route]){content+=foliage(slot.x,slot.y,0,slot.size);leafCount++}
 content+='<img class="base" src="common-tree.png">';
 for(const [key,x,y,size,angle] of layout.decorations)content+=`<img class="ornament" src="${key}.png" style="left:${x*100}%;top:${y*100}%;width:${size*185}%;transform:translate(-50%,-50%) rotate(${angle}deg)">`;
 let count=0;
 // Root is initially available and never produces flowers.
 for(const slot of layout.flowerSlots)if(slot.tier<=levels[slot.route]){content+=blossom(slot.x,slot.y,slot.size,slot.angle);count++}
 let nodes='<svg class="nodes" viewBox="0 0 1000 666">';
 for(let route=0;route<3;route++)for(let tier=1;tier<=4;tier++){
   const a=point(route,tier-1),b=point(route,tier),owned=tier<=levels[route];
   if(!owned)nodes+=`<ellipse class="bud" cx="${b[0]*1000+27}" cy="${b[1]*666-19}" rx="4" ry="7"/>`;
   const name=`サラマンドラ・${variants[route]}${numerals[tier]}`;
   nodes+=`<g class="node" data-route="${route}" data-tier="${tier}" onclick="selectNode('${id}',${route},${tier},${interactive})" transform="translate(${b[0]*1000},${b[1]*666})"><title>${name}${owned?'・開放済み':'・未開放'}</title><circle r="22" fill="${owned?'#a52c45':'#44474a'}" stroke="${owned?'#ead095':'#8c8980'}" stroke-width="3"/><text y="5" text-anchor="middle" fill="${owned?'#fff2da':'#c0bdba'}" font-size="16">${numerals[tier]}</text></g>`;
 }
 const root=anchors[0];nodes+=`<g transform="translate(${root[0]*1000},${root[1]*666})"><circle r="22" fill="#a52c45" stroke="#ead095" stroke-width="3"/><text y="5" text-anchor="middle" fill="#fff2da" font-size="14">根</text></g></svg>`;
 const el=document.getElementById(id);el.innerHTML=content+nodes;el.dataset.flowers=count;el.dataset.foliage=leafCount;el.dataset.levels=levels.join(',');
}
function levels(){return [0,1,2].map(r=>Number(document.getElementById('r'+r).value))}
function update(){const lv=levels();for(let r=0;r<3;r++)document.getElementById('o'+r).textContent=numerals[lv[r]];render('liveTree',lv,true)}
function preset(value){for(let r=0;r<3;r++)document.getElementById('r'+r).value=value;update()}
function selectNode(id,route,tier,interactive){document.getElementById('selected').textContent=`サラマンドラ・${variants[route]}${numerals[tier]}`;if(!interactive)return;const lv=levels();if(tier>lv[route]+1){document.getElementById('selected').textContent+=' — 親ノードから開放してください';return}if(tier>lv[route]){document.getElementById('r'+route).value=tier;update()}}
for(let r=0;r<3;r++)document.getElementById('r'+r).addEventListener('input',update);
render('rootTree',[0,0,0]);render('fullTree',[4,4,4]);update();
</script></html>'''.replace('LAYOUT',json.dumps(layout,ensure_ascii=False))
html=html.replace('根元から開放した枝まで深紅の花が咲きます。','開放した枝の周囲にだけ深紅の花が咲きます。根は初期開放のため花を咲かせません。')
html=html.replace('.ornament,.flower{','.base{inset:10%;width:80%;height:80%}.ornament,.flower,.foliage{')
html=html.replace('開放した枝の周囲にだけ深紅の花が咲きます。','開放した枝の周囲にだけ、銀灰の葉のまとまりと深紅の花が広がります。')
html=html.replace('銀灰の葉のまとまりと深紅の花が広がります。','銀灰の葉が茂り、深紅の花が咲きます。最大開放では枝の周囲が葉で埋まる樹冠になります。')
html=html.replace('開放した枝の周囲にだけ、銀灰の葉が茂り、深紅の花が咲きます。','開放に合わせて銀灰の葉が茂り、開放済みノードの周りだけに深紅の花が咲きます。')
html=html.replace('.flower{filter:', '.foliage{opacity:.48}.flower{filter:')
(OUT/'preview.html').write_text(html,encoding='utf-8')
print(OUT/'preview.html')
