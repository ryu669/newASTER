import test from 'node:test';
import assert from 'node:assert/strict';
import { createProgress, claimVictory, markStoryRead, mergeOopart } from '../game/core/progression-engine.mjs';
import { VERTICAL_SLICE_PROGRESSION as def } from '../game/data/vertical-slice-progression-fixture.mjs';

test('claims a battle once, scales rewards by level band, and unlocks milestones', () => {
  const progress = createProgress(def);
  const result = claimVictory(progress, def, { battleId: 'b-1', level: 21, materials: 3, terraforming: 3, poemIds: ['c-01', 'c-02'] });
  assert.deepEqual(result, { claimed: true, materials: 5, terraforming: 5, poems: ['c-01', 'c-02'], stories: ['colossus-chapter-01'], milestones: ['garden-01'] });
  assert.deepEqual(claimVictory(progress, def, { battleId: 'b-1', level: 21, materials: 3, terraforming: 3 }), { claimed: false });
  assert.equal(progress.materials, 5);
});

test('keeps poems and story read state separate and idempotent', () => {
  const progress = createProgress(def);
  claimVictory(progress, def, { battleId: 'b-2', level: 1, materials: 0, terraforming: 0, poemIds: ['h-01'] });
  assert.equal(progress.unlockedStoryIds.includes('heroine-chapter-01'), true);
  assert.equal(markStoryRead(progress, 'heroine-chapter-01'), true);
  assert.equal(markStoryRead(progress, 'heroine-chapter-01'), false);
  assert.throws(() => markStoryRead(progress, 'colossus-chapter-01'));
});

test('oopart merging never lowers a roll and unlocks direct upgrade at 80 percent', () => {
  const progress = createProgress(def);
  assert.equal(mergeOopart(progress, def, { defId: 'vs-oopart', roll: 79 }).directUpgradeUnlocked, false);
  assert.equal(mergeOopart(progress, def, { defId: 'vs-oopart', roll: 30 }).improved, false);
  const result = mergeOopart(progress, def, { defId: 'vs-oopart', roll: 80 });
  assert.equal(result.roll, 80); assert.equal(result.directUpgradeUnlocked, true);
});
