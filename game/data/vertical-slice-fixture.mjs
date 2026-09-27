export const VERTICAL_SLICE_FIXTURE = {
  levelScale: .08,
  chain: { baseRate: .20, turnBonusChance: .30 },
  boss: { hp: 900, gaugeMax: 6 },
  parts: [
    { id: 'gauge-organ', hp: 150, effect: 'gauge-down' },
    { id: 'guard-shell', hp: 180, effect: 'open-body', defenseMultiplier: .8 },
    { id: 'interference-node', hp: 120, effect: 'stop-interference' },
    { id: 'shift-core', hp: 160, effect: 'change-pattern' },
  ],
  heroines: ['breaker', 'guardian', 'healer', 'support', 'controller'].map((id, index) => ({
    id, hp: 300 - index * 15, attack: 36 + index * 3, resourceMax: 3,
    skills: [
      { id: `${id}-strike`, cost: 0, power: 1 },
      { id: `${id}-skill-a`, cost: 1, power: 1.5 },
      { id: `${id}-skill-b`, cost: 2, power: 2 },
    ],
  })),
};
