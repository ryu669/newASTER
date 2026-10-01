/* Standalone visual review. Values and effects are not authoritative combat. */
const initialHp=[640,430,790,260,880],maxHp=1000;
let hp=[...initialHp],actor=0,target='本体',effect=null,paused=false,last=0;
const party=document.getElementById('party'),formation=document.getElementById('formation');
const label=document.getElementById('action-label'),status=document.getElementById('status');
for(let i=0;i<5;i++){
 const card=document.createElement('button');card.className='member';card.dataset.actor=i;
 card.innerHTML=`<img class="portrait" src="slayer-2d-direction-study-v1.png" alt="枠${i+1}のスレイヤー仮画像"><div class="member-info"><div class="member-title"><strong>スレイヤー</strong><span>0${i+1}</span></div><div class="hp"><i></i></div><small></small></div><div class="fx"></div><div class="amount"></div>`;
 card.onclick=()=>select(i);party.append(card);
 const figure=document.createElement('button');figure.className='standee';figure.dataset.actor=i;
 figure.setAttribute('aria-label',`全身の味方${i+1}を選ぶ`);
 figure.innerHTML=`<img src="slayer-2d-fullbody-v1.png" alt=""><span>0${i+1}</span>`;
 figure.onclick=()=>select(i);formation.append(figure);
}
function render(){
 [...party.children].forEach((card,i)=>{card.classList.toggle('selected',i===actor);card.setAttribute('aria-pressed',String(i===actor));card.setAttribute('aria-label',`味方${i+1} スレイヤー HP ${hp[i]}`);card.style.setProperty('--hp',`${hp[i]/maxHp*100}%`);card.querySelector('small').textContent=`HP ${hp[i]} / ${maxHp}`});
 [...formation.children].forEach((f,i)=>{f.classList.toggle('selected',i===actor);f.setAttribute('aria-pressed',String(i===actor))});
 document.getElementById('focus-index').textContent=`0${actor+1}`;document.getElementById('actor-label').textContent=`0${actor+1} · スレイヤー`;
 document.getElementById('timeline').innerHTML=[actor,(actor+2)%5,'enemy',(actor+1)%5,(actor+4)%5,(actor+3)%5].map((id,i)=>`<span class="order-chip ${i===0?'active':''} ${id==='enemy'?'enemy-chip':''}">${id==='enemy'?'巨神獣':`味方 ${id+1}`}</span>`).join('');
 const n=Number(document.getElementById('count').value);document.getElementById('heal-label').textContent=n===1?'選択中の1人へ':`HPの低い${n}人へ`;
}
function select(i){if(effect)return;actor=i;render()}
function clearFx(){[...party.children,...formation.children,document.querySelector('.focus')].forEach(e=>{e.style.setProperty('--flash','0');e.style.setProperty('--advance','0px');e.style.setProperty('--float','0px')});document.getElementById('impact').style.setProperty('--impact','0');label.style.opacity=0}
function start(kind){
 if(effect||paused)return;
 const n=Number(document.getElementById('count').value);
 const targets=kind==='heal'?(n===1?[actor]:[0,1,2,3,4].sort((a,b)=>hp[a]-hp[b]||a-b).slice(0,n)):kind==='enemy'?[0,1,2,3,4]:[];
 effect={kind,actor,targets,elapsed:0,applied:false};
 label.textContent=kind==='attack'?`味方${actor+1} → ${target} / 剣撃`:kind==='heal'?`回復対象：${targets.map(i=>i+1).join('・')} / ${targets.length}人`:'巨神獣 → 味方5人 / 全体攻撃';label.style.opacity=1;
 status.textContent=label.textContent;document.querySelectorAll('.commands button').forEach(b=>b.disabled=true);
}
function frame(now){
 const dt=last?Math.min((now-last)/1000,.05):0;last=now;
 if(effect&&!paused){
  effect.elapsed+=dt;const t=effect.elapsed;
  if(effect.kind==='attack'){
   const advance=18*Math.sin(Math.min(1,t/.8)*Math.PI);
   document.querySelector('.focus').style.setProperty('--advance',`${advance}px`);formation.children[effect.actor].style.setProperty('--advance',`${advance}px`);
   document.getElementById('impact').style.setProperty('--impact',String(t>.25&&t<.7?Math.sin((t-.25)/.45*Math.PI)*.7:0));
  }else{
   const flash=Math.sin(Math.min(1,t/1.1)*Math.PI)*.8;
   effect.targets.forEach(i=>{const c=party.children[i];c.style.setProperty('--flash',String(flash));c.style.setProperty('--float',`${-18*t}px`);c.style.setProperty('--fx-color',effect.kind==='heal'?'#99efc6':'#f5a2a2');c.querySelector('.amount').textContent=effect.kind==='heal'?'+180':'−120'});
   if(t>=.35&&!effect.applied){effect.targets.forEach(i=>hp[i]=Math.max(0,Math.min(maxHp,hp[i]+(effect.kind==='heal'?180:-120))));effect.applied=true;render()}
  }
  if(t>=1.2){effect=null;clearFx();document.querySelectorAll('.commands button').forEach(b=>b.disabled=false);status.textContent='表示完了。別の味方・表示方式でも比較できます'}
 }
 requestAnimationFrame(frame);
}
document.getElementById('attack').onclick=()=>start('attack');document.getElementById('heal').onclick=()=>start('heal');document.getElementById('enemy-hit').onclick=()=>start('enemy');
document.getElementById('count').onchange=render;
document.querySelectorAll('[data-part]').forEach(b=>b.onclick=()=>{if(effect)return;target=b.dataset.part;document.querySelectorAll('[data-part]').forEach(x=>x.setAttribute('aria-pressed',String(x===b)))});
for(const mode of ['portrait','fullbody'])document.getElementById(mode).onclick=()=>{document.getElementById('battle').dataset.mode=mode;for(const id of ['portrait','fullbody'])document.getElementById(id).setAttribute('aria-pressed',String(id===mode));document.getElementById('observation').textContent=mode==='portrait'?'顔重視：選択中の顔を大きく見せ、5人の状態を下段で読む構成です。':'全身5人：羽根・剣・髪が重なります。顔のサイズと人物の見分けやすさを比較してください。'};
document.getElementById('pause').onclick=()=>{paused=!paused;document.body.classList.toggle('paused',paused);document.getElementById('pause').textContent=paused?'再開':'一時停止';document.querySelectorAll('.commands button').forEach(b=>b.disabled=paused||!!effect);status.textContent=paused?'一時停止中':effect?label.textContent:'味方を選んで、攻撃・回復を確認できます'};
document.getElementById('reset').onclick=()=>{hp=[...initialHp];effect=null;paused=false;actor=0;target='本体';document.body.classList.remove('paused');document.getElementById('pause').textContent='一時停止';document.querySelectorAll('.commands button').forEach(b=>b.disabled=false);document.querySelectorAll('[data-part]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.part===target)));clearFx();render();status.textContent='初期状態へ戻しました'};
document.addEventListener('visibilitychange',()=>{if(document.hidden&&!paused)document.getElementById('pause').click()});
render();requestAnimationFrame(frame);
