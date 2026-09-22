// Frozen v0.2 reference for the C# port. No production balance changes here.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import assert from 'node:assert/strict';
import { Battle, DEFAULT_RELICS, cleanSave, grantReward } from '../src/engine.mjs';

const output = new URL('../tests/fixtures/battle-parity-v1.json', import.meta.url);
const definitions = [
  { id: 'wing-auto', options: { auto: true, relics: DEFAULT_RELICS }, policy: 'auto' },
  { id: 'wing-hold', options: { relics: DEFAULT_RELICS }, policy: 'hold' },
  { id: 'wing-release', options: { relics: DEFAULT_RELICS }, policy: 'release' },
  { id: 'body-reordered', options: { order: ['attack', 'support', 'guard'], target: 'body', relics: DEFAULT_RELICS }, policy: 'release' },
  { id: 'upgraded-release', options: { level: 3, relics: DEFAULT_RELICS }, policy: 'release' },
  { id: 'lethal-no-reward', options: { auto: true }, initialHp: [1, 1, 1], policy: 'auto' },
];

function snapshot(b) {
  return {
    time: b.time, phase: b.phase, target: b.target, candidate: b.candidate,
    reserved: [...b.reserved], chain: [...b.chain], chainIndex: b.chainIndex,
    oath: b.oath, chainOath: b.chainOath, rescueUsed: b.rescueUsed,
    boss: { ...b.boss }, commandAt: b.commandAt, enemyAt: b.enemyAt, bigAt: b.bigAt,
    skipBig: b.skipBig, boostUntil: b.boostUntil, relayFrom: b.relayFrom,
    stats: { ...b.stats },
    heroes: b.heroes.map(h => ({ id: h.id, hp: h.hp, maxHp: h.maxHp, nextAttack: h.nextAttack,
      shield: h.shield, shieldUntil: h.shieldUntil, ward: h.ward, wardUntil: h.wardUntil })),
  };
}

function simulate(def) {
  const b = new Battle(def.options);
  if (def.initialHp) b.heroes.forEach((hero, i) => { hero.hp = def.initialHp[i]; });
  const initial = snapshot(b), operations = [], checkpoints = [];
  let pendingSteps = 0;
  const flushSteps = () => {
    if (!pendingSteps) return;
    operations.push({ action: 'step', delta: .05, count: pendingSteps });
    checkpoints.push(snapshot(b)); pendingSteps = 0;
  };
  for (let frame = 0; frame < 20000 && !['victory', 'defeat'].includes(b.phase); frame++) {
    if (!b.auto && b.phase === 'decision') {
      flushSteps();
      if (b.canEntrust()) { assert.equal(b.entrust(), true); operations.push({ action: 'entrust' }); }
      else { assert.equal(b.fire({ release: def.policy === 'release' }), true); operations.push({ action: 'fire', release: def.policy === 'release' }); }
      checkpoints.push(snapshot(b));
    }
    const before = b.phase;
    b.step(.05); pendingSteps++;
    if (b.phase !== before) flushSteps();
  }
  flushSteps();
  assert.ok(['victory', 'defeat'].includes(b.phase), `${def.id} did not finish`);
  const save = cleanSave({}), reward = grantReward(save, b);
  assert.equal(grantReward(save, b), 0);
  return { ...def, initial, operations, checkpoints, reward, savedAfterReward: save };
}

// Replay the recorded inputs independently, rather than trusting policy generation alone.
function replay(fixture) {
  const b = new Battle(fixture.options);
  if (fixture.initialHp) b.heroes.forEach((h, i) => { h.hp = fixture.initialHp[i]; });
  assert.deepEqual(snapshot(b), fixture.initial);
  fixture.operations.forEach((op, i) => {
    if (op.action === 'step') for (let n = 0; n < op.count; n++) b.step(op.delta);
    else if (op.action === 'entrust') assert.equal(b.entrust(), true);
    else if (op.action === 'fire') assert.equal(b.fire({ release: op.release }), true);
    else throw new Error(`Unknown operation: ${op.action}`);
    assert.deepEqual(snapshot(b), fixture.checkpoints[i], `${fixture.id}: operation ${i}`);
  });
  const save = cleanSave({});
  assert.equal(grantReward(save, b), fixture.reward);
  assert.equal(grantReward(save, b), 0);
  assert.deepEqual(save, fixture.savedAfterReward);
}

const result = {
  formatVersion: 1,
  engineSha256: createHash('sha256').update(readFileSync(new URL('../src/engine.mjs', import.meta.url))).digest('hex'),
  cases: definitions.map(simulate),
};
if (process.argv.includes('--write')) {
  mkdirSync(new URL('../tests/fixtures/', import.meta.url), { recursive: true });
  writeFileSync(output, JSON.stringify(result, null, 2) + '\n');
} else {
  const stored = JSON.parse(readFileSync(output, 'utf8'));
  assert.deepEqual(result, stored, 'Reference changed: inspect the rule change before regenerating fixtures.');
}
result.cases.forEach(replay);
console.log(`Parity reference: ${result.cases.length} cases, ${result.cases.reduce((n, c) => n + c.checkpoints.length, 0)} checkpoints verified.`);
for (const c of result.cases) {
  const end = c.checkpoints.at(-1);
  console.log(`${c.id}: ${end.phase}, ${end.time}s, reward ${c.reward}`);
}
