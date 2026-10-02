// 描画・UIに依存しない縦切り用の戦闘コア。正式人物／物語は参照しない。
export function seededRandom(seed = 1) {
  let state = (seed >>> 0) || 1;
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0;
    return state / 0x100000000;
  };
}

export function createBattle(def, { level = 1, seed = 1 } = {}) {
  if (!Number.isInteger(level) || level < 1 || level > 50) throw new Error('level must be 1..50');
  if (def.heroines.length !== 5 || new Set(def.heroines.map(h => h.id)).size !== 5) throw new Error('exactly five unique heroines are required');
  if (def.parts.length !== 4) throw new Error('vertical slice requires four parts');
  const scale = 1 + (level - 1) * def.levelScale;
  const rng = seededRandom(seed);
  const parts = def.parts.map(part => ({ ...part, hp: Math.ceil(part.hp * scale), maxHp: Math.ceil(part.hp * scale), broken: false }));
  const battle = {
    id: `vs-${seed}-${level}`, level, rng, turn: 1, phase: 'player', chainCount: 0,
    heroes: def.heroines.map(hero => ({ ...hero, hp: hero.hp, maxHp: hero.hp, resource: 0, defeated: false })),
    boss: { hp: Math.ceil(def.boss.hp * scale), maxHp: Math.ceil(def.boss.hp * scale), gauge: 0, gaugeMax: def.boss.gaugeMax, ultimateUnlocked: level >= 45 },
    parts, modifiers: [], log: [], completed: false,
  };
  startTurn(battle, def);
  return battle;
}

export function startTurn(battle, def) {
  battle.phase = 'player'; battle.chainCount = 0;
  battle.modifiers = battle.heroes.map(hero => ({ heroId: hero.id, chainBonus: battle.rng() < def.chain.turnBonusChance ? 0.10 : 0 }));
  const bonusTargets = battle.heroes.filter(h => !h.defeated).sort(() => battle.rng() - .5).slice(0, 2);
  for (const hero of bonusTargets) {
    const modifier = battle.modifiers.find(m => m.heroId === hero.id);
    modifier.chainBonus = Math.round((modifier.chainBonus + .05) * 100) / 100;
  }
  battle.log.push({ type: 'turn', turn: battle.turn, modifiers: battle.modifiers.map(m => ({ ...m })) });
}

export function useSkill(battle, def, heroId, skillId, targetId) {
  if (battle.completed || battle.phase !== 'player') throw new Error('not ready for a player command');
  const hero = battle.heroes.find(h => h.id === heroId && !h.defeated);
  const skill = hero?.skills.find(s => s.id === skillId);
  if (!skill) throw new Error('unknown usable skill');
  const target = targetId === 'body' ? battle.boss : battle.parts.find(p => p.id === targetId && !p.broken);
  if (!target) throw new Error('invalid target');
  if (hero.resource < skill.cost) throw new Error('insufficient job resource');
  hero.resource -= skill.cost;
  const damage = Math.max(1, Math.floor(hero.attack * skill.power * (target.defenseMultiplier || 1)));
  target.hp = Math.max(0, target.hp - damage);
  battle.log.push({ type: 'skill', heroId, skillId, targetId, damage });
  if (target !== battle.boss && target.hp === 0) breakPart(battle, target);
  if (battle.boss.hp === 0) return victory(battle);
  battle.chainCount++;
  const modifier = battle.modifiers.find(m => m.heroId === heroId)?.chainBonus || 0;
  const canChain = battle.chainCount < 5 && battle.rng() < def.chain.baseRate + modifier;
  if (canChain) { battle.log.push({ type: 'chain', heroId, rate: def.chain.baseRate + modifier, success: true }); return { chained: true }; }
  battle.log.push({ type: 'chain', heroId, rate: def.chain.baseRate + modifier, success: false });
  return enemyAction(battle, def);
}

function breakPart(battle, part) {
  part.broken = true;
  if (part.effect === 'gauge-down') battle.boss.gauge = Math.max(0, battle.boss.gauge - 1);
  battle.log.push({ type: 'part-break', partId: part.id, effect: part.effect });
}

function enemyAction(battle, def) {
  battle.phase = 'enemy';
  const boss = battle.boss;
  const intactGaugeParts = battle.parts.filter(p => !p.broken && p.effect === 'gauge-down').length;
  boss.gauge = Math.min(boss.gaugeMax, boss.gauge + 1 + intactGaugeParts);
  const ultimate = boss.ultimateUnlocked && boss.gauge >= boss.gaugeMax;
  battle.log.push({ type: 'enemy', ultimate, gauge: boss.gauge });
  if (ultimate) boss.gauge = 0;
  battle.turn++;
  for (const hero of battle.heroes) if (!hero.defeated) hero.resource = Math.min(hero.resource + 1, hero.resourceMax);
  startTurn(battle, def);
  return { chained: false, ultimate };
}

function victory(battle) { battle.completed = true; battle.phase = 'victory'; battle.log.push({ type: 'victory' }); return { victory: true }; }
