import test from 'node:test';
import assert from 'node:assert/strict';
import { createBattle, useSkill } from '../game/core/vertical-slice-engine.mjs';
import { VERTICAL_SLICE_FIXTURE as def } from '../game/data/vertical-slice-fixture.mjs';

test('enforces the 1..50 summon level and unlocks ultimate at 45', () => {
  assert.throws(() => createBattle(def, { level: 0 }));
  assert.equal(createBattle(def, { level: 44 }).boss.ultimateUnlocked, false);
  assert.equal(createBattle(def, { level: 45 }).boss.ultimateUnlocked, true);
});

test('uses deterministic turn modifiers and never stores a heroine chain rate', () => {
  const a = createBattle(def, { seed: 8 }); const b = createBattle(def, { seed: 8 });
  assert.deepEqual(a.modifiers, b.modifiers);
  assert.ok(a.modifiers.every(m => m.chainBonus === 0 || m.chainBonus === .05 || m.chainBonus === .10 || m.chainBonus === .15));
  assert.ok(a.heroes.every(hero => !Object.hasOwn(hero, 'chainRate')));
});

test('breaks a part once and applies its declared effect', () => {
  const battle = createBattle(def, { seed: 2 });
  battle.heroes[0].attack = 200; battle.boss.gauge = 2;
  useSkill(battle, def, 'breaker', 'breaker-strike', 'gauge-organ');
  assert.equal(battle.parts[0].broken, true);
  assert.equal(battle.boss.gauge, 1);
  assert.equal(battle.log.filter(e => e.type === 'part-break').length, 1);
});
