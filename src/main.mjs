import { Battle, ROLES, RELICS, cleanSave, grantReward, upgrade } from './engine.mjs';

const $ = id => document.getElementById(id);
const SAVE_KEY = 'newaster.prototype.v1';
let save;
try { save = cleanSave(JSON.parse(localStorage.getItem(SAVE_KEY) || '{}')); }
catch { save = cleanSave({}); $('saveStatus').textContent = '保存データを読めませんでした。初期状態で開始します。'; }
let battle = null, initialTarget = 'wing', preview, lastEvent = 0, effects = [], flash = 0, resultShown = false;
const clock = seconds => `${Math.floor(seconds / 60).toString().padStart(2, '0')}:${Math.floor(seconds % 60).toString().padStart(2, '0')}`;
function persist() {
  try { localStorage.setItem(SAVE_KEY, JSON.stringify(save)); $('saveStatus').textContent = '進行状況をこのブラウザに保存しました'; }
  catch { $('saveStatus').textContent = '保存できません。この画面を閉じると進行状況が失われます。'; }
}
function resetPreview() { preview = new Battle({ order: save.order, level: save.level, relics: save.relics, target: initialTarget }); }
resetPreview();

const portrait = index => `<svg viewBox="0 0 60 70" aria-hidden="true"><path d="M28 38 L3 ${16 + index * 5} 7 42 22 49 M33 38 L57 ${16 + index * 5} 53 42 38 49" fill="currentColor" opacity=".25"/><ellipse cx="30" cy="21" rx="12" ry="15" fill="currentColor" opacity=".65"/><path d="M17 18 Q16 1 31 4 Q48 3 44 30 L38 19 33 12 21 27Z" fill="#142535"/><path d="M23 34 L36 34 47 64 13 64Z" fill="currentColor" opacity=".65"/><path d="M28 35 L33 35 37 65 22 65Z" fill="#dfeaf0" opacity=".3"/><ellipse cx="30" cy="4" rx="16" ry="3" stroke="currentColor" fill="none" opacity=".55"/></svg>`;
function renderHeroes(model) {
  $('heroes').innerHTML = model.heroes.map((h, i) => {
    const active = battle && (battle.phase === 'decision' ? battle.participants().includes(i) : battle.phase === 'chain' && battle.chain[battle.chainIndex] === i);
    const shield = h.shield && h.shieldUntil > model.time;
    return `<article class="hero-card ${active ? 'active' : ''} ${h.hp <= 0 ? 'down' : ''}" style="--role:${h.color}"><div class="portrait">${portrait(i)}</div><div><div class="hero-top"><span class="hero-name">${h.name}</span><span class="hero-role">${h.role}</span></div><p class="hero-desc">${h.description}</p><div class="hero-hp"><span>${h.hp <= 0 ? '戦闘不能' : shield ? '防壁展開中' : 'HP'}</span><span>${h.hp} / ${h.maxHp}</span></div><div class="bar"><i style="width:${h.hp / h.maxHp * 100}%"></i></div></div><div class="formation-controls"><span>0${i + 1} / ${h.skill}</span><div><button data-move="${i}:left" aria-label="${h.name}を前へ" ${battle || i === 0 ? 'disabled' : ''}>←</button> <button data-move="${i}:right" aria-label="${h.name}を後ろへ" ${battle || i === 2 ? 'disabled' : ''}>→</button></div></div></article>`;
  }).join('');
  model.heroes.forEach((h, i) => {
    const equipment = document.createElement('div'); equipment.className = 'relic-equipment';
    equipment.innerHTML = `<label>遺物 <select data-relic="${h.id}" aria-label="${h.name}の遺物" ${battle ? 'disabled' : ''}>${Object.entries(RELICS).map(([id, relic]) => `<option value="${id}" ${id === h.relic ? 'selected' : ''}>${relic.name}</option>`).join('')}</select></label><p class="micro">${RELICS[h.relic].description}</p>${h.ward > 0 && h.wardUntil > model.time ? `<p class="ward-status">吸収残り ${h.ward} ／ ${(h.wardUntil - model.time).toFixed(1)}秒</p>` : ''}`;
    $('heroes').children[i].append(equipment);
    if (h.id === 'support') {
      const rescue = document.createElement('p'); rescue.className = 'rescue-status';
      rescue.textContent = `緊急救援：${model.rescueUsed ? '使用済み' : h.hp <= 0 ? '戦闘不能のため発動不可' : '待機中'}。生存中、被弾後HP25%以下の仲間を最大HPの30%回復。戦闘中1回。`;
      equipment.append(rescue);
    }
  });
}
function renderRelicForecast(model, members, phase) {
  let relayReady = phase === 'chain' && model.relayFrom !== null;
  const remaining = phase === 'chain' ? members.slice(model.chainIndex + 1) : members;
  $('relicForecast').replaceChildren(...remaining.map((index, position) => {
    const hero = model.heroes[index], row = document.createElement('li');
    const notes = [];
    if (hero.id === 'attack' && relayReady) { notes.push('共鳴を受けて攻撃技＋30%'); relayReady = false; }
    if (hero.relic === 'relay') {
      const hasAttack = remaining.slice(position + 1).some(i => model.heroes[i].id === 'attack');
      notes.push(hasAttack ? '後続の攻撃技へ共鳴を渡す' : '後続に攻撃技なし：共鳴は未使用で終了'); relayReady = true;
    } else if (hero.relic === 'ward') notes.push('技の後、全員に吸収30／6秒');
    else if (hero.relic === 'breaker') notes.push(hero.id !== 'attack' ? '攻撃技がないため破砕効果なし' : model.target === 'wing' ? '翼が残っていれば攻撃技＋30%' : '本体対象：破砕効果なし');
    row.textContent = `${hero.name}・${RELICS[hero.relic].name}：${notes.join('。') || '追加効果なし'}`;
    return row;
  }));
  $('relicForecast').hidden = ['victory', 'defeat'].includes(phase);
}
function render() {
  const m = battle || preview, phase = battle?.phase, decision = phase === 'decision', ended = ['victory', 'defeat'].includes(phase);
  $('materials').textContent = save.materials; $('level').textContent = `${save.level} / 3`;
  const complete = save.memory && save.level === 3;
  $('progressTitle').textContent = complete ? '試作クリア — 空に、帰る場所を。' : save.memory ? '次の目標：部隊を3段階まで強化' : '最初の目標：方舟を討伐し、記憶を取り戻す';
  $('progressHint').textContent = complete ? `討伐 ${save.wins}回。編成や遺物を変えて、引き続き遊べます。` : save.memory ? `討伐 ${save.wins}回 ／ 強化 ${save.level} / 3。記憶帳も開いてみましょう。` : '遊び方は右上から。初戦は左翼を狙うと安全です。';
  $('progressTitle').parentElement.classList.toggle('complete', complete);
  $('squadHud').innerHTML = m.heroes.map(h => `<div style="--role:${h.color}"><span>${h.name} <b>${h.hp > 0 ? `${h.hp}/${h.maxHp}` : '戦闘不能'}</b></span><div class="bar"><i style="width:${h.hp / h.maxHp * 100}%"></i></div></div>`).join('');
  $('bossValue').textContent = `${m.boss.hp.toLocaleString()} / ${m.boss.maxHp.toLocaleString()}`;
  $('bossFill').style.width = `${m.boss.hp / m.boss.maxHp * 100}%`;
  $('wingValue').textContent = m.boss.wing > 0 ? `${m.boss.wing} / ${m.boss.maxWing}` : '破壊済み';
  $('wingFill').style.width = `${m.boss.wing / m.boss.maxWing * 100}%`;
  $('timer').textContent = clock(m.time);
  $('phaseLabel').textContent = !battle ? '出撃準備' : battle.paused ? '一時停止' : ({ running: '自動戦闘', decision: '指示待ち / 時間停止', chain: '連携発動 / 時間停止', victory: '討伐成功', defeat: '部隊撤退' })[phase];
  const danger = battle && !ended && battle.bigAt - battle.time <= 6;
  $('warning').hidden = !danger;
  if (danger) $('warning').textContent = battle.skipBig ? '翼破壊成功 ─ 次の大技を中断' : `全体攻撃「落星」まで ${Math.max(0, battle.bigAt - battle.time).toFixed(1)} 秒`;
  $('deploy').hidden = !!battle;
  $('fire').hidden = !battle || ended; $('entrust').hidden = !battle || ended;
  $('fire').disabled = !decision || battle?.paused;
  $('entrust').disabled = !battle?.canEntrust();
  $('pause').disabled = !battle || ended;
  $('retreat').hidden = !battle || ended;
  $('pause').textContent = battle?.paused ? '再開する' : '一時停止';
  $('auto').disabled = ended;
  $('targetWing').disabled = (!!battle && (!decision || battle.paused)) || m.boss.wing <= 0;
  $('targetBody').disabled = !!battle && (!decision || battle.paused);
  $('targetWing').setAttribute('aria-pressed', m.target === 'wing');
  $('targetBody').setAttribute('aria-pressed', m.target === 'body');
  $('upgrade').disabled = !!battle || save.materials < 4 || save.level >= 3;
  $('upgrade').textContent = save.level >= 3 ? '最大強化済み' : '星片 4 で強化';
  $('upgradeHint').textContent = `現在 HP・攻撃力 ＋${save.level * 10}%`;
  $('formationLabel').textContent = battle ? '戦闘中は順番を固定' : '出撃前に順番を変更できます';
  let members = !battle ? [0, 1, 2] : phase === 'chain' ? battle.chain : battle.participants();
  $('sequence').innerHTML = members.map(i => `<span class="seq-name" style="--role:${m.heroes[i].color}">${m.heroes[i].name}</span>`).join('<span class="seq-arrow">→</span>');
  renderRelicForecast(m, members, phase);
  $('oathValue').textContent = `${m.oath} / 3`;
  $('oathHint').textContent = `託すたび＋1。2以上を保持すると通常攻撃＋10%。${m.oath >= 2 ? '保持効果が発動中。' : ''}`;
  $('releaseOath').disabled = !decision || battle?.paused || m.oath === 0 || m.auto;
  const release = $('releaseOath').checked && !$('releaseOath').disabled;
  const amount = phase === 'chain' ? m.chainOath : release ? m.oath : 0;
  const benefits = members.map(i => m.heroes[i].id).map(id => id === 'guard' ? `防壁 ${6 + amount * 2}秒` : id === 'support' ? `回復 最大HPの${Math.round(20 * (1 + amount * .2))}%` : `攻撃技＋${amount * 20}%`);
  $('oathForecast').textContent = amount ? `${phase === 'chain' ? '解放中' : '解放時'}：${benefits.join('／')}。` : '温存：技は通常の効果。誓いは次の指示へ持ち越します。';
  $('fire').textContent = release ? `誓い${m.oath}を解放して発動` : '発動する';
  if (!battle) {
    $('commandTitle').textContent = '翼を折り、道を開く。';
    $('commandHint').textContent = '3人の順番が、連携の順番になります。防御・支援・攻撃をつないで、方舟を討伐しましょう。';
    $('forecast').textContent = '託すと仲間を予約。発動で予約した順に技を使います。';
  } else if (ended) {
    $('commandTitle').textContent = phase === 'victory' ? '誓いは、空に届いた。' : '次の出撃に、託す。';
    $('commandHint').textContent = phase === 'victory' ? '星片で部隊を強化できます。記憶帳に、方舟の記憶が残されました。' : '防御や回復が必要な場面では早めに発動を。翼破壊で大技を弱める方法もあります。';
    $('forecast').textContent = '編成へ戻ると、順番の変更と強化ができます。';
  } else if (decision) {
    const h = battle.heroes[battle.candidate];
    $('commandTitle').textContent = battle.paused ? '一時停止中' : `${h?.name || '仲間'}へ、指示を。`;
    $('commandHint').textContent = h ? `${h.skill}：${h.description}` : '予約済みの仲間で連携を発動します。';
    const next = battle.nextAlive(battle.candidate, battle.participants());
    const target = battle.nextAlive(battle.enemyCursor - 1);
    const nextHit = Math.max(0, battle.enemyAt - battle.time);
    const risk = (battle.skipBig ? '次の大技は中断予定。' : `大技まで残り ${(battle.bigAt - battle.time).toFixed(1)} 秒。`) + (target >= 0 ? `通常攻撃は${nextHit.toFixed(1)}秒後、${battle.heroes[target].name}へ。` : '');
    $('forecast').textContent = (battle.canEntrust() ? `託す → ${battle.heroes[next].name}が参加。発動まで2秒の通常戦闘。` : 'これ以上は託せません。連携を発動しましょう。') + risk;
  } else {
    $('commandTitle').textContent = battle.paused ? '一時停止中' : phase === 'chain' ? '誓いを、つなぐ。' : battle.reserved.length ? '仲間へ、想いを託す。' : '空の戦いを見守る。';
    $('commandHint').textContent = phase === 'chain' ? '予約した仲間が順に技を発動しています。連携中は戦闘時間が停止します。' : `次の指示まで ${Math.max(0, battle.commandAt - battle.time).toFixed(1)} 秒。通常攻撃は自動で進みます。`;
    $('forecast').textContent = `次の大技まで ${Math.max(0, battle.bigAt - battle.time).toFixed(1)} 秒${battle.skipBig ? '（翼破壊により中断予定）' : ''}`;
  }
  $('chainBanner').hidden = phase !== 'chain';
  if (phase === 'chain') {
    const hero = battle.heroes[battle.chain[battle.chainIndex]];
    $('chainBanner').textContent = `${battle.chainIndex + 1} CHAIN / ${hero?.name || ''} ─ ${hero?.skill || ''}`;
  }
  $('result').hidden = !ended;
  if (ended) {
    $('resultEnglish').textContent = phase === 'victory' ? 'OATH FULFILLED' : 'RETURN TO THE SKY';
    $('resultTitle').textContent = phase === 'victory' ? '討伐成功' : '部隊撤退';
    $('resultText').textContent = phase === 'victory' ? `戦闘時間 ${clock(battle.time)} ／ 星片 ＋${3 + Number(battle.boss.wing <= 0)}${battle.boss.wing <= 0 ? '（翼破壊ボーナス込み）' : ''}。記憶帳を解放しました。` : '素材の消費はありません。順番や攻撃対象を変えて、再び挑めます。';
    $('resultStats').textContent = `発動 ${m.stats.chains}回 ／ 最大${m.stats.maxChain}人連携\n託す ${m.stats.entrusts}回 ／ 誓い解放 ${m.stats.released}\n緊急救援 ${m.rescueUsed ? '使用済み' : '未使用'} ／ 生存 ${m.heroes.filter(h => h.hp > 0).length}人`;
    if (!resultShown) { resultShown = true; $('returnButton').focus({ preventScroll: true }); }
  }
  const log = battle ? battle.events.filter(e => e.kind !== 'hit').slice(-5) : [{ time: 0, text: '部隊を編成し、攻撃対象を選んで出撃してください。' }];
  $('log').replaceChildren(...log.map(e => { const li = document.createElement('li'), time = document.createElement('time'), label = document.createElement('span'); time.textContent = clock(e.time); label.textContent = e.text; li.append(time, label); return li; }));
  renderHeroes(m);
}
$('deploy').addEventListener('click', () => {
  battle = new Battle({ order: save.order, level: save.level, relics: save.relics, target: initialTarget, auto: $('auto').checked });
  resultShown = false; $('releaseOath').checked = false;
  lastEvent = 0; effects = []; render();
});
$('fire').addEventListener('click', () => { if (battle?.fire({ release: $('releaseOath').checked && !$('releaseOath').disabled })) $('releaseOath').checked = false; render(); });
$('entrust').addEventListener('click', () => { if (battle?.entrust()) $('releaseOath').checked = false; render(); });
$('releaseOath').addEventListener('change', render);
for (const [id, target] of [['targetWing', 'wing'], ['targetBody', 'body']]) $(id).addEventListener('click', () => {
  if (battle) battle.selectTarget(target); else { initialTarget = target; resetPreview(); }
  render();
});
$('pause').addEventListener('click', () => { if (battle) battle.paused = !battle.paused; render(); });
$('auto').addEventListener('change', () => { $('releaseOath').checked = false; if (battle) battle.auto = $('auto').checked; render(); });
function returnToFormation() { battle = null; effects = []; flash = 0; resultShown = false; $('releaseOath').checked = false; resetPreview(); render(); $('deploy').focus(); }
$('returnButton').addEventListener('click', returnToFormation);
$('retreat').addEventListener('click', () => { if (battle) battle.paused = true; $('retreatDialog').showModal(); render(); });
$('cancelRetreat').addEventListener('click', () => $('retreatDialog').close());
$('confirmRetreat').addEventListener('click', () => { $('retreatDialog').close(); returnToFormation(); });
$('helpButton').addEventListener('click', () => { if (battle) battle.paused = true; $('helpDialog').showModal(); render(); });
for (const id of ['closeHelp', 'helpDone']) $(id).addEventListener('click', () => $('helpDialog').close());
$('heroes').addEventListener('click', event => {
  const button = event.target.closest('[data-move]');
  if (!button || battle) return;
  const [raw, direction] = button.dataset.move.split(':'), i = Number(raw), to = i + (direction === 'left' ? -1 : 1);
  if (to < 0 || to >= save.order.length) return;
  [save.order[i], save.order[to]] = [save.order[to], save.order[i]];
  persist(); resetPreview(); render();
  document.querySelector(`[data-move="${to}:${direction === 'left' ? 'right' : 'left'}"]`)?.focus();
});
$('heroes').addEventListener('change', event => {
  const select = event.target.closest('[data-relic]');
  if (!select || battle || !Object.hasOwn(RELICS, select.value)) return;
  const hero = select.dataset.relic;
  save.relics[hero] = select.value; persist(); resetPreview(); render();
  document.querySelector(`[data-relic="${hero}"]`)?.focus();
});
$('upgrade').addEventListener('click', () => { if (!battle && upgrade(save)) { persist(); resetPreview(); render(); } });
$('memoryButton').addEventListener('click', () => {
  $('memoryText').textContent = save.memory ? '方舟には、鐘がなかった。\n\nかつて人々は、その音を頼りに帰ってきた。\n霧の海を渡る者も、空へ発った者も。\n\n最後の航海で、鐘は地上へ降ろされた。\nもう帰る場所のない人々に、\n夜明けを知らせるために。\n\nそれでも方舟は、空を巡り続けた。\n誰かがまだ、帰ってくると信じて。' : 'まだ、この方舟の記憶は届いていません。\n初めて討伐すると、記憶の断片が開きます。';
  if (battle && !['victory', 'defeat'].includes(battle.phase)) battle.paused = true;
  $('memoryDialog').showModal(); render();
});
$('closeMemory').addEventListener('click', () => $('memoryDialog').close());
const pauseOnLeave = () => { if (battle && !['victory', 'defeat'].includes(battle.phase)) { battle.paused = true; render(); } };
document.addEventListener('visibilitychange', () => { if (document.hidden) pauseOnLeave(); });
window.addEventListener('blur', pauseOnLeave);

// Original procedural concept art. No reference-game assets are loaded.
const canvas = $('scene'), ctx = canvas.getContext('2d');
const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
function path(points, fill, stroke, width = 1) {
  ctx.beginPath(); points.forEach(([x, y], i) => i ? ctx.lineTo(x, y) : ctx.moveTo(x, y)); ctx.closePath();
  if (fill) { ctx.fillStyle = fill; ctx.fill(); } if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = width; ctx.stroke(); }
}
function line(points, color, width = 1) { ctx.beginPath(); points.forEach(([x, y], i) => i ? ctx.lineTo(x, y) : ctx.moveTo(x, y)); ctx.strokeStyle = color; ctx.lineWidth = width; ctx.stroke(); }
function ellipse(x, y, rx, ry, fill, stroke) { ctx.beginPath(); ctx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2); if (fill) { ctx.fillStyle = fill; ctx.fill(); } if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = 1; ctx.stroke(); } }
const heroPositions = [[740, 400], [835, 475], [650, 505]];
function drawHero(x, y, color, scale, index, time) {
  ctx.save(); ctx.translate(x, y + Math.sin(time * 1.4 + index) * 6); ctx.scale(scale, scale);
  const glow = ctx.createRadialGradient(0, 0, 0, 0, 0, 56); glow.addColorStop(0, `${color}25`); glow.addColorStop(1, `${color}00`); ctx.fillStyle = glow; ctx.fillRect(-60, -60, 120, 120);
  for (const side of [-1, 1]) for (let k = 0; k < 5; k++) path([[side * 4, -5], [side * (45 - k * 5), -38 + k * 10], [side * (21 - k), 12 + k * 3]], `rgba(206,225,235,${.72 - k * .08})`, '#e9f3ff35');
  path([[-5, 0], [7, 0], [15, 29], [-14, 25]], '#d6dce1', color);
  path([[-5, -4], [5, -4], [9, 12], [-8, 12]], color, '#dae7ea88');
  line([[-5, 28], [-7, 44]], '#b9ccd7', 3); line([[5, 28], [8, 42]], '#b9ccd7', 3);
  line([[4, 4], [20, 15], [40, -16]], color, 2); line([[35, -23], [44, -16]], '#f9e5bd', 2);
  ellipse(0, -12, 6, 8, '#d5c6b5');
  path([[-7, -12], [-7, -20], [0, -24], [7, -18], [8, -5], [3, -16], [-2, -17]], '#e0e4e9', '#ffffff55');
  ellipse(0, -30, 10, 2.5, null, '#edcf81');
  ctx.restore();
}
function draw(time, delta) {
  const bounds = canvas.getBoundingClientRect(), dpr = Math.min(devicePixelRatio || 1, 2);
  if (canvas.width !== Math.round(bounds.width * dpr) || canvas.height !== Math.round(bounds.height * dpr)) { canvas.width = Math.round(bounds.width * dpr); canvas.height = Math.round(bounds.height * dpr); }
  ctx.setTransform(canvas.width / 1000, 0, 0, canvas.height / 650, 0, 0);
  const m = battle || preview, t = reduced || battle?.paused ? 0 : time;
  const bg = ctx.createLinearGradient(0, 0, 600, 650); bg.addColorStop(0, '#112435'); bg.addColorStop(.55, '#395464'); bg.addColorStop(1, '#183241'); ctx.fillStyle = bg; ctx.fillRect(0, 0, 1000, 650);
  const light = ctx.createRadialGradient(680, 170, 20, 650, 180, 490); light.addColorStop(0, '#dfd9b932'); light.addColorStop(1, '#d7d7be00'); ctx.fillStyle = light; ctx.fillRect(0, 0, 1000, 650);
  ellipse(770, 168, 83, 83, '#b6c5ca13', '#dce5df13'); ellipse(770, 168, 72, 72, null, '#dce5df0d');
  for (let k = 0; k < 36; k++) { const x = (k * 173 + 93) % 1000, y = (k * 67 + 41) % 460; ellipse(x, y, k % 4 === 0 ? 1.3 : .6, k % 4 === 0 ? 1.3 : .6, '#d0e0e05c'); }
  for (let k = 0; k < 10; k++) {
    const x = ((k * 197 + t * (k % 3 + 1) * 2) % 1400) - 200, y = 445 + k % 3 * 65;
    const mist = ctx.createRadialGradient(x, y, 0, x, y, 240); mist.addColorStop(0, '#b0c5c720'); mist.addColorStop(1, '#a5c0cb00'); ctx.save(); ctx.translate(0, y); ctx.scale(1, .35); ctx.translate(0, -y); ctx.fillStyle = mist; ctx.fillRect(x - 250, y - 250, 500, 500); ctx.restore();
  }
  // Distant ruins provide scale, while the foreground silhouette remains readable.
  for (let k = 0; k < 7; k++) { const x = k * 171 - 15, y = 435 + k % 3 * 27; path([[x, y], [x + 8, y - 75], [x + 17, y - 91], [x + 27, y - 72], [x + 34, y], [x + 18, y + 40]], '#122b3b45'); }
  ctx.save(); ctx.translate(360, 300 + Math.sin(t * .5) * 7);
  ctx.rotate(-.10 + Math.sin(t * .18) * .014);
  // Left wing is an articulated array of stone and gilded vanes.
  for (const side of [-1, 1]) {
    const broken = side === -1 && m.boss.wing <= 0;
    ctx.save(); ctx.scale(side, 1);
    if (broken) { ctx.translate(-5, 50); ctx.rotate(.35); ctx.globalAlpha = .23; }
    for (let k = 0; k < 7; k++) {
      const x = 70 + k * 28;
      path([[55, -28 + k * 8], [x + 20, -156 + k * 17], [x + 68, -184 + k * 20], [x + 49, -58 + k * 20], [65, 72]], k % 2 ? '#293e49' : '#3f5660', '#a7986b99', 1.5);
      line([[x + 21, -136 + k * 17], [x + 43, -162 + k * 20], [x + 28, -53 + k * 20]], '#a5d0cc55');
    }
    line([[60, 25], [234, -12], [295, -49]], '#b5a478', 3);
    ctx.restore();
  }
  ellipse(0, 6, 142, 142, null, '#c0ad7050'); ellipse(0, 6, 133, 133, null, '#9eafab25');
  for (let k = 0; k < 12; k++) { const a = k * Math.PI / 6; line([[Math.cos(a) * 137, 6 + Math.sin(a) * 137], [Math.cos(a) * 145, 6 + Math.sin(a) * 145]], '#c5b27a88'); }
  path([[-102, -28], [-56, -78], [0, -100], [61, -73], [121, 12], [53, 104], [8, 155], [-57, 98], [-98, 32]], '#1a303d', '#b6a67a', 2);
  path([[-80, -18], [-41, -65], [0, -79], [46, -51], [84, 20], [34, 88], [7, 128], [-32, 88]], '#435862', '#7d8e8b');
  for (let k = -2; k <= 2; k++) {
    const x = k * 25, top = -110 - (2 - Math.abs(k)) * 21;
    path([[x - 9, 5], [x - 8, top], [x, top - 29], [x + 8, top], [x + 10, 8]], '#223946', '#9e976f');
    line([[x, top + 10], [x, -16]], '#9bddcd99', 2);
  }
  ellipse(4, 31, 28, 35, '#133543', '#bfb182'); ellipse(4, 31, 19, 25, '#8fc5c8', '#e1d7ae');
  const core = ctx.createRadialGradient(4, 31, 0, 4, 31, 65); core.addColorStop(0, '#baf5e65a'); core.addColorStop(1, '#baf5e600'); ctx.fillStyle = core; ctx.fillRect(-64, -37, 136, 136);
  path([[-11, 24], [4, 6], [18, 28], [4, 55]], '#e4eedb');
  for (const side of [-1, 1]) { line([[side * 62, 55], [side * 58, 106], [side * 73, 145]], '#c6b38699'); ellipse(side * 73, 148, 4, 6, '#b7a673'); }
  ctx.restore();
  // Target marker.
  const target = m.target === 'wing' ? [183, 217] : [364, 331];
  ctx.save(); ctx.translate(...target); ctx.rotate(t * .1); for (let k = 0; k < 4; k++) { ctx.rotate(Math.PI / 2); line([[-25, -35], [-35, -35], [-35, -25]], '#e2c38c99', 1.3); } ctx.restore();
  ctx.font = '10px sans-serif'; ctx.fillStyle = '#d3c9af'; ctx.fillText(m.target === 'wing' ? 'TARGET / WING' : 'TARGET / CORE', target[0] - 40, target[1] + 53);
  for (let i = 0; i < 3; i++) {
    const h = m.heroes[i]; if (h.hp <= 0) continue;
    const active = battle?.phase === 'chain' && battle.chain[battle.chainIndex] === i;
    const [x, y] = heroPositions[i], lunge = active && h.id === 'attack' ? Math.sin(Math.min(1, battle.chainClock / .6) * Math.PI) * 130 : 0;
    drawHero(x - lunge, y - lunge * .45, h.color, i === 2 ? 1.15 : .95, i, t);
    if (h.shield && h.shieldUntil > m.time) ellipse(x, y, 38, 45, '#78cde50d', '#78cde56a');
  }
  if (battle) {
    for (const event of battle.events) if (event.id > lastEvent) {
      if (event.kind === 'hit') effects.push({ x: target[0] + Math.sin(event.id) * 35, y: target[1] - 30, text: event.text.match(/\d+/)?.[0] || '', life: 1, actor: event.actor, color: m.heroes[event.actor]?.color || '#e8d7b8' });
      if (event.kind === 'blast') flash = .4;
    }
    lastEvent = battle.serial;
  }
  for (const effect of effects) {
    effect.life -= delta; ctx.globalAlpha = Math.max(0, effect.life); ctx.fillStyle = effect.color; ctx.font = 'italic 22px Georgia'; ctx.fillText(effect.text, effect.x, effect.y - (1 - effect.life) * 35);
    if (effect.life > .8 && effect.actor !== null) line([heroPositions[effect.actor], [effect.x, effect.y + 30]], `${effect.color}66`, 1.5);
  }
  effects = effects.filter(e => e.life > 0); ctx.globalAlpha = 1;
  if (flash > 0) { flash -= delta; ctx.fillStyle = `rgba(240,180,130,${Math.max(0, flash * .25)})`; ctx.fillRect(0, 0, 1000, 650); }
  const vignette = ctx.createRadialGradient(500, 300, 160, 500, 300, 620); vignette.addColorStop(0, '#08172400'); vignette.addColorStop(1, '#0817249c'); ctx.fillStyle = vignette; ctx.fillRect(0, 0, 1000, 650);
}
let lastFrame = performance.now(), renderClock = 0;
function frame(now) {
  const delta = Math.min(.1, (now - lastFrame) / 1000); lastFrame = now;
  if (battle) {
    battle.step(delta);
    if (grantReward(save, battle)) persist();
    renderClock += delta;
    if (renderClock >= .1) { render(); renderClock = 0; }
  }
  draw(now / 1000, delta); requestAnimationFrame(frame);
}
render(); requestAnimationFrame(frame);
