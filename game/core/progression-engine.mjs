// 縦切りの戦闘後進行。UIはこの結果を表示するだけで、状態を直接変更しない。
export function createProgress(def) {
  return {
    version: 1, terraformingXp: 0, unlockedMilestoneIds: [], collectedPoemIds: [],
    unlockedStoryIds: [], readStoryIds: [], claimedBattleRewardIds: [], ooparts: {}, materials: 0,
  };
}

function addUnique(list, values) {
  const added = [];
  for (const value of values) if (!list.includes(value)) { list.push(value); added.push(value); }
  return added;
}

function unlockStories(progress, def) {
  const unlocked = [];
  for (const story of def.stories) {
    if (story.poemIds.every(id => progress.collectedPoemIds.includes(id)) && !progress.unlockedStoryIds.includes(story.id)) {
      progress.unlockedStoryIds.push(story.id); unlocked.push(story.id);
    }
  }
  return unlocked;
}

function unlockMilestones(progress, def) {
  const unlocked = [];
  for (const milestone of def.milestones) {
    if (progress.terraformingXp >= milestone.requiredXp && !progress.unlockedMilestoneIds.includes(milestone.id)) {
      progress.unlockedMilestoneIds.push(milestone.id); unlocked.push(milestone.id);
    }
  }
  return unlocked;
}

export function claimVictory(progress, def, reward) {
  if (!reward?.battleId || progress.claimedBattleRewardIds.includes(reward.battleId)) return { claimed: false };
  if (!Number.isInteger(reward.level) || reward.level < 1 || reward.level > 50) throw new Error('reward level must be 1..50');
  progress.claimedBattleRewardIds.push(reward.battleId);
  const levelBonus = Math.floor((reward.level - 1) / 10);
  const materials = reward.materials + levelBonus;
  const terraforming = reward.terraforming + levelBonus;
  progress.materials += materials;
  progress.terraformingXp += terraforming;
  const poems = addUnique(progress.collectedPoemIds, reward.poemIds || []);
  const stories = unlockStories(progress, def);
  const milestones = unlockMilestones(progress, def);
  return { claimed: true, materials, terraforming, poems, stories, milestones };
}

export function markStoryRead(progress, storyId) {
  if (!progress.unlockedStoryIds.includes(storyId)) throw new Error('story is not unlocked');
  return addUnique(progress.readStoryIds, [storyId]).length === 1;
}

export function mergeOopart(progress, def, incoming) {
  const definition = def.ooparts.find(item => item.id === incoming.defId);
  if (!definition) throw new Error('unknown oopart definition');
  if (!Number.isInteger(incoming.roll) || incoming.roll < 0 || incoming.roll > definition.randomStatMax) throw new Error('invalid oopart roll');
  const current = progress.ooparts[incoming.defId];
  if (!current) {
    progress.ooparts[incoming.defId] = { defId: incoming.defId, level: 1, roll: incoming.roll, directUpgradeUnlocked: incoming.roll >= Math.ceil(definition.randomStatMax * .8) };
    return { created: true, improved: true, ...progress.ooparts[incoming.defId] };
  }
  const improved = incoming.roll > current.roll;
  if (improved) current.roll = incoming.roll;
  current.directUpgradeUnlocked ||= current.roll >= Math.ceil(definition.randomStatMax * .8);
  return { created: false, improved, ...current };
}
