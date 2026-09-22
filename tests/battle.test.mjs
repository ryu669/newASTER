import test from 'node:test';
import assert from 'node:assert/strict';
import { Battle, DEFAULT_RELICS, cleanSave, grantReward, upgrade } from '../src/engine.mjs';

function until(battle, predicate, limit = 20000) {
  for (let i = 0; i < limit; i++) { if (predicate(battle)) return; battle.step(.05); }
  assert.fail(`Simulation did not reach expected state (${battle.phase}, ${battle.time})`);
}
function decision(b) { until(b, s => s.phase === 'decision'); }
function finishChain(b) { until(b, s => s.phase !== 'chain'); }

test('manual decision freezes combat; entrusting advances exactly 2 combat seconds', () => {
  const b = new Battle(); decision(b); assert.equal(b.time, 12);
  const hp = b.boss.wing; b.step(.2); assert.equal(b.time, 12); assert.equal(b.boss.wing, hp);
  assert.equal(b.entrust(), true); decision(b); assert.equal(b.time, 14); assert.deepEqual(b.participants(), [0, 1]);
  b.entrust(); decision(b); assert.equal(b.time, 16); assert.deepEqual(b.participants(), [0, 1, 2]);
  assert.equal(b.entrust(), false);
});

test('three-person chain applies guard and support before attack and freezes timers', () => {
  const b = new Battle({ target: 'body' }); decision(b); b.entrust(); decision(b); b.entrust(); decision(b);
  const hp = b.boss.hp, time = b.time; b.fire(); finishChain(b);
  assert.equal(b.boss.hp, hp - Math.round(26 * 3 * 1.2 * 1.25));
  assert.equal(b.time, time); assert.equal(b.commandAt, time + 12);
  assert.equal(b.boostUntil, 0); assert.equal(b.lastChain, 3);
});

test('formation order changes whether attack benefits from buffs', () => {
  function chainDamage(order) {
    const b = new Battle({ order, target: 'body' }); b.phase = 'decision'; b.reserved = [0, 1]; b.candidate = 2;
    b.fire(); finishChain(b); return b.boss.maxHp - b.boss.hp;
  }
  assert.ok(chainDamage(['guard', 'support', 'attack']) > chainDamage(['attack', 'guard', 'support']));
});

test('wing damage does not hurt body; break redirects and cancels exactly one big attack', () => {
  const b = new Battle({ auto: true }); b.hitBoss(280);
  assert.equal(b.boss.hp, b.boss.maxHp); assert.equal(b.boss.wing, 0); assert.equal(b.target, 'body');
  assert.equal(b.skipBig, true);
  until(b, s => s.time >= 24);
  assert.equal(b.skipBig, false); assert.equal(b.events.some(e => e.kind === 'blast'), false);
  until(b, s => s.time >= 48);
  assert.equal(b.events.some(e => e.kind === 'blast'), true);
});

test('wing break redirects subsequent skill damage to body', () => {
  const b = new Battle({ order: ['attack', 'guard', 'support'] });
  b.boss.wing = 1; b.phase = 'decision'; b.reserved = [0, 1]; b.candidate = 2; b.fire(); finishChain(b);
  assert.equal(b.boss.wing, 0); assert.equal(b.target, 'body'); assert.equal(b.boss.hp, 1250);
  b.hitBoss(10, true, 0); assert.ok(b.boss.hp < 1250);
});

test('shield mitigates one hit only; expired shield does not protect', () => {
  const b = new Battle(); b.heroes[0].shield = true; b.heroes[0].shieldUntil = 6;
  b.hurt(0, 40); b.hurt(0, 40); assert.equal(b.heroes[0].hp, 200);
  b.heroes[1].shield = true; b.heroes[1].shieldUntil = 1; b.time = 2; b.hurt(1, 40);
  assert.equal(b.heroes[1].hp, 140);
});

test('waiting can kill the candidate; dead participants are removed without softlock', () => {
  const b = new Battle(); b.phase = 'decision'; b.time = 4; b.enemyAt = 5; b.enemyCursor = 1;
  b.heroes[1].hp = 1; b.entrust(); decision(b);
  assert.equal(b.heroes[1].hp, 0); assert.equal(b.candidate, 2); assert.deepEqual(b.participants(), [0, 2]);
  assert.equal(b.canEntrust(), false); b.fire(); finishChain(b); assert.equal(b.phase, 'running');
});

test('support never revives dead allies and healing stays below maximum', () => {
  const b = new Battle(); b.heroes[0].hp = 0; b.heroes[2].hp -= 1;
  b.applySkill(1); assert.equal(b.heroes[0].hp, 0); assert.equal(b.heroes[2].hp, b.heroes[2].maxHp);
});

test('victory stops pending enemy attacks and chain actions', () => {
  const b = new Battle({ target: 'body' }); b.time = 4.95; b.enemyAt = 5;
  b.heroes[0].nextAttack = 5; b.boss.hp = 1;
  const hp = b.heroes.map(h => h.hp); b.step(.05);
  assert.equal(b.phase, 'victory'); assert.deepEqual(b.heroes.map(h => h.hp), hp);
  b.step(.25); assert.deepEqual(b.heroes.map(h => h.hp), hp);
});

test('defeat cannot produce a reward; victory rewards are idempotent', () => {
  const s = cleanSave({}), b = new Battle(); b.heroes.forEach(h => h.hp = 0); b.checkEnd();
  assert.equal(b.phase, 'defeat'); assert.equal(grantReward(s, b), 0);
  const win = new Battle(); win.hitBoss(280); win.hitBoss(2000);
  assert.equal(grantReward(s, win), 4); assert.equal(grantReward(s, win), 0);
  assert.equal(s.materials, 4); assert.equal(s.wins, 1); assert.equal(s.memory, true);
  assert.equal(upgrade(s), true); assert.equal(s.materials, 0); assert.equal(s.level, 1);
  assert.equal(upgrade(s), false);
});

test('save validation and reload preserve only valid progress and formation', () => {
  const bad = cleanSave({ level: 100, materials: -5, memory: 'yes', order: ['guard', 'guard', 'attack'] });
  assert.equal(bad.level, 3); assert.equal(bad.materials, 0); assert.equal(bad.memory, false);
  assert.deepEqual(bad.order, ['guard', 'support', 'attack']);
  assert.deepEqual(cleanSave({ order: ['guard', 'support', 'constructor'] }).order, ['guard', 'support', 'attack']);
  const good = cleanSave({ materials: 12, level: 2, memory: true, wins: 4, order: ['attack', 'support', 'guard'] });
  assert.deepEqual(cleanSave(JSON.parse(JSON.stringify(good))), good);
  good.level = 3; assert.equal(upgrade(good), false); assert.equal(good.materials, 12);
});

test('pause and invalid deltas never advance battle; restart resets combat state', () => {
  const b = new Battle(); b.paused = true; b.step(.25); assert.equal(b.time, 0);
  b.paused = false; b.step(NaN); b.step(-1); assert.equal(b.time, 0);
  b.hitBoss(500); const restarted = new Battle();
  assert.equal(restarted.boss.wing, 280); assert.equal(restarted.time, 0); assert.deepEqual(restarted.reserved, []);
});

test('headless complete run: wing-first maximum chain wins and unlocks progression', () => {
  const b = new Battle();
  until(b, s => {
    if (s.phase === 'decision') s.canEntrust() ? s.entrust() : s.fire();
    return ['victory', 'defeat'].includes(s.phase);
  });
  assert.equal(b.phase, 'victory'); assert.ok(b.time >= 50 && b.time <= 100);
  const save = cleanSave({}); grantReward(save, b); assert.equal(save.memory, true); assert.equal(upgrade(save), true);
});

test('auto mode finishes without manual input or decision softlock', () => {
  const b = new Battle({ auto: true }); until(b, s => ['victory', 'defeat'].includes(s.phase));
  assert.ok(b.time < 200);
});

test('early guard can prevent a knockout that entrusting would allow', () => {
  const setup = () => {
    const b = new Battle(); b.time = 19; b.phase = 'decision'; b.enemyAt = 20; b.heroes[0].hp = 20;
    b.rescueUsed = true; // Isolate guard timing after the one-use rescue has been spent.
    b.heroes.forEach(h => h.nextAttack = 22); return b;
  };
  const early = setup(); early.fire(); finishChain(early); until(early, s => s.time >= 21);
  const delayed = setup(); delayed.entrust(); decision(delayed);
  assert.equal(early.heroes[0].hp, 6); assert.equal(delayed.heroes[0].hp, 0);
});

test('old saves keep progression and gain equipment; invalid relic IDs are sanitized', () => {
  const migrated = cleanSave({ version: 1, materials: 7, level: 2, wins: 3, memory: true });
  assert.equal(migrated.materials, 7); assert.equal(migrated.level, 2); assert.equal(migrated.wins, 3); assert.equal(migrated.memory, true);
  assert.deepEqual(migrated.relics, DEFAULT_RELICS);
  const save = cleanSave({ relics: { guard: 'none', support: 'constructor', attack: {} } });
  assert.deepEqual(save.relics, { guard: 'none', support: 'relay', attack: 'breaker' });
  assert.deepEqual(cleanSave(JSON.parse(JSON.stringify(save))), save);
  const b = new Battle({ order: ['attack', 'guard', 'support'], relics: save.relics });
  assert.deepEqual(b.heroes.map(h => h.relic), ['breaker', 'none', 'relay']);
  save.relics.attack = 'ward'; assert.equal(b.heroes[0].relic, 'breaker');
});

test('relay strengthens a later ally attack, but not itself, normal attacks or future chains', () => {
  const b = new Battle({ target: 'body', relics: { support: 'relay' } });
  b.phase = 'decision'; b.reserved = [1]; b.candidate = 2; b.fire();
  const hp = b.boss.hp;
  b.hitBoss(10, false, 2); assert.equal(b.boss.hp, hp - 10); assert.equal(b.relayFrom, 1);
  finishChain(b); assert.equal(b.boss.hp, hp - 10 - Math.round(78 * 1.25 * 1.3)); assert.equal(b.relayFrom, null);
  const self = new Battle({ target: 'body', relics: { attack: 'relay' } });
  self.phase = 'decision'; self.candidate = 2; self.fire(); finishChain(self);
  assert.equal(self.boss.hp, 1250 - 78); assert.equal(self.relayFrom, null);
  self.phase = 'decision'; self.candidate = 2; self.fire(); finishChain(self);
  assert.equal(self.boss.hp, 1250 - 156);
});

test('relay is lost if its owner dies before the chain and does not stack across owners', () => {
  const b = new Battle({ target: 'body', relics: { guard: 'relay', support: 'relay' } });
  b.phase = 'decision'; b.reserved = [0, 1]; b.candidate = 2; b.fire(); finishChain(b);
  assert.equal(b.boss.hp, 1250 - Math.round(78 * 1.2 * 1.25 * 1.3));
  const dead = new Battle({ target: 'body', relics: { support: 'relay' } });
  dead.heroes[1].hp = 0; dead.phase = 'decision'; dead.reserved = [1]; dead.candidate = 2;
  dead.fire(); finishChain(dead); assert.equal(dead.boss.hp, 1250 - 78);
});

test('breaker applies only to own wing skill, and cannot spill extra damage into body', () => {
  const b = new Battle({ relics: { attack: 'breaker' } });
  b.hitBoss(20, false, 2); assert.equal(b.boss.wing, 260);
  b.hitBoss(20, true, 0); assert.equal(b.boss.wing, 240);
  b.hitBoss(20, true, 2); assert.equal(b.boss.wing, 214);
  b.boss.wing = 1; b.hitBoss(78, true, 2); assert.equal(b.boss.wing, 0); assert.equal(b.boss.hp, 1250);
  b.hitBoss(78, true, 2); assert.equal(b.boss.hp, 1172);
});

test('ward absorbs after guard reduction, depletes, expires and never revives or stacks', () => {
  const b = new Battle({ relics: { guard: 'ward', support: 'ward' } });
  b.heroes[2].hp = 0; b.applySkill(0); assert.equal(b.heroes[2].ward, 0);
  b.hurt(0, 40); assert.equal(b.heroes[0].hp, 260); assert.equal(b.heroes[0].ward, 10);
  b.hurt(0, 40); assert.equal(b.heroes[0].hp, 230); assert.equal(b.heroes[0].ward, 0);
  b.applySkill(1); b.applySkill(1); assert.equal(b.heroes[0].ward, 30); assert.equal(b.heroes[2].hp, 0);
  b.time = 6; const hp = b.heroes[0].hp; b.hurt(0, 40); assert.equal(b.heroes[0].hp, hp - 40);
});

test('equipped full chain and auto runs finish without softlocks', () => {
  for (const auto of [false, true]) {
    const b = new Battle({ relics: DEFAULT_RELICS, auto });
    until(b, s => {
      if (!auto && s.phase === 'decision') s.canEntrust() ? s.entrust() : s.fire();
      return ['victory', 'defeat'].includes(s.phase);
    });
    assert.equal(b.phase, 'victory');
    assert.ok(b.time < 200);
  }
});

test('entrusting caps oath at three, preserves it between chains, and rejects invalid inputs', () => {
  const b = new Battle();
  assert.equal(b.entrust(), false); assert.equal(b.oath, 0);
  decision(b); b.entrust(); decision(b); b.entrust(); decision(b);
  assert.equal(b.oath, 2); b.fire(); finishChain(b); assert.equal(b.oath, 2);
  decision(b); b.entrust(); decision(b); b.entrust(); decision(b);
  assert.equal(b.oath, 3); b.paused = true;
  assert.equal(b.fire({ release: true }), false); assert.equal(b.oath, 3);
  b.paused = false; b.auto = true; b.step(.05); finishChain(b);
  assert.equal(b.oath, 3); assert.equal(b.chainOath, 0);
});

test('release enhances all three roles for this chain only and consumes the stored oath', () => {
  const b = new Battle({ target: 'body' }); b.oath = 3;
  b.phase = 'decision'; b.reserved = [0, 1]; b.candidate = 2;
  b.heroes[0].hp = 100; b.fire({ release: true });
  assert.equal(b.oath, 0); assert.equal(b.heroes[0].shieldUntil, 12);
  finishChain(b);
  assert.equal(b.heroes[0].hp, 100 + Math.round(260 * .32));
  assert.equal(b.boss.hp, 1250 - Math.round(78 * 1.6 * 1.2 * 1.25));
  assert.equal(b.chainOath, 0);
  b.phase = 'decision'; b.candidate = 0; b.fire();
  assert.equal(b.heroes[0].shieldUntil, b.time + 6);
});

test('holding at least two oath strengthens normal attacks without spending charge', () => {
  for (const oath of [0, 1, 2, 3]) {
    const b = new Battle({ target: 'body' }); b.oath = oath;
    until(b, s => s.time >= 1);
    assert.equal(b.boss.hp, 1250 - Math.round(13 * (oath >= 2 ? 1.1 : 1)));
    assert.equal(b.oath, oath);
  }
});

test('rescue triggers once on surviving damage at threshold, even after reordering', () => {
  const b = new Battle({ order: ['attack', 'guard', 'support'] });
  b.heroes[1].hp = 66; b.hurt(1, 1);
  assert.equal(b.heroes[1].hp, 65 + 78); assert.equal(b.rescueUsed, true);
  b.heroes[1].hp = 66; b.hurt(1, 1); assert.equal(b.heroes[1].hp, 65);
  assert.equal(b.events.filter(e => e.kind === 'rescue').length, 1);
  const fresh = new Battle(); assert.equal(fresh.rescueUsed, false); assert.equal(fresh.oath, 0);
});

test('rescue requires living support and actual damage, and never revives', () => {
  const b = new Battle(); b.heroes[0].hp = 20; b.hurt(0, 0);
  assert.equal(b.rescueUsed, false); b.hurt(0, 20);
  assert.equal(b.heroes[0].hp, 0); assert.equal(b.rescueUsed, false);
  b.heroes[1].hp = 0; b.heroes[2].hp = 30; b.hurt(2, 1);
  assert.equal(b.heroes[2].hp, 29); assert.equal(b.rescueUsed, false);
  const self = new Battle(); self.heroes[1].hp = 46; self.hurt(1, 1);
  assert.equal(self.heroes[1].hp, 99); assert.equal(self.rescueUsed, true);
});

test('repeated manual releases complete an equipped battle without softlocks', () => {
  const b = new Battle({ relics: DEFAULT_RELICS });
  until(b, s => {
    if (s.phase === 'decision') s.canEntrust() ? s.entrust() : s.fire({ release: true });
    return ['victory', 'defeat'].includes(s.phase);
  });
  assert.equal(b.phase, 'victory'); assert.ok(b.time < 200);
  assert.ok(b.events.some(e => e.kind === 'oath'));
});

test('battle summary counts accepted commands only and resets for a new sortie', () => {
  const b = new Battle(); b.fire(); b.entrust();
  assert.deepEqual(b.stats, { entrusts: 0, chains: 0, maxChain: 0, released: 0 });
  decision(b); b.entrust(); decision(b); b.entrust(); decision(b); b.entrust();
  b.fire({ release: true }); finishChain(b);
  assert.deepEqual(b.stats, { entrusts: 2, chains: 1, maxChain: 3, released: 2 });
  for (let i = 0; i < 100; i++) b.log('Test log');
  assert.equal(b.stats.entrusts, 2); assert.equal(b.events.length, 80);
  assert.equal(new Battle().stats.chains, 0);
});

test('full progression reaches maximum upgrade, survives save reload and allows replay', () => {
  let save = cleanSave({});
  for (let sortie = 0; sortie < 4; sortie++) {
    const b = new Battle({ level: save.level, order: save.order, relics: save.relics });
    until(b, s => {
      if (s.phase === 'decision') s.canEntrust() ? s.entrust() : s.fire({ release: true });
      return ['victory', 'defeat'].includes(s.phase);
    });
    assert.equal(b.phase, 'victory'); assert.equal(grantReward(save, b), 4);
    assert.equal(grantReward(save, b), 0);
    assert.equal(upgrade(save), sortie < 3);
    save = cleanSave(JSON.parse(JSON.stringify(save)));
    assert.equal(save.level, Math.min(3, sortie + 1)); assert.equal(save.memory, true);
  }
  assert.equal(save.wins, 4); assert.equal(save.materials, 4);
});

test('all formations, targets and upgrade levels terminate for auto, hold and release strategies', () => {
  const orders = [
    ['guard', 'support', 'attack'], ['guard', 'attack', 'support'],
    ['support', 'guard', 'attack'], ['support', 'attack', 'guard'],
    ['attack', 'guard', 'support'], ['attack', 'support', 'guard'],
  ];
  for (const order of orders) for (const target of ['wing', 'body']) for (const level of [0, 1, 2, 3]) for (const strategy of ['auto', 'hold', 'release']) {
    const b = new Battle({ order, target, level, relics: DEFAULT_RELICS, auto: strategy === 'auto' });
    until(b, s => {
      if (!s.auto && s.phase === 'decision') s.canEntrust() ? s.entrust() : s.fire({ release: strategy === 'release' });
      return ['victory', 'defeat'].includes(s.phase);
    });
    assert.ok(b.time < 200, `${order}/${target}/${level}/${strategy}`);
    assert.ok(b.oath >= 0 && b.oath <= 3);
    assert.ok(b.heroes.every(h => Number.isFinite(h.hp) && h.hp >= 0 && h.hp <= h.maxHp));
    const state = JSON.stringify(b); b.step(.25); assert.equal(JSON.stringify(b), state);
  }
});
