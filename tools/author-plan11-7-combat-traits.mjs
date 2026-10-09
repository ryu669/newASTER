import fs from 'node:fs';
import {applyInteractionTraits} from './author-plan11-7-traits.mjs';

const root = 'game/unity/Assets/Game/Resources/Combat';
const catalog = JSON.parse(fs.readFileSync(`${root}/battle-plan10-shangrila.json`, 'utf8'));
// Initial character-authoring choices, fixed per form. Balance remains provisional.
const second = {
  fighter: 'boost-fighter', defender: 'counter-extreme', chaser: 'rocket-start',
  berserker: 'predator', gunner: 'hunter-eye', sniper: 'feuerschutz',
  blaster: 'elemental-resistance', healer: 'muscle-nurse', artist: 'impact-resistance',
  gambler: 'operative', general: 'leader', alchemist: 'giant-killing', panzer: 'nanomachine-armor',
};
catalog.combatTraitVersion = 1;
applyInteractionTraits(catalog);
for (const hero of catalog.heroines) {
  hero.secondTraitId = second[hero.jobId.replace('job.', '')];
  if (!hero.secondTraitId) throw new Error(`Missing second trait: ${hero.id}`);
}
// Independent authored mode, deliberately not a runtime multiplier of normal slots.
for (const formation of catalog.generalFormations) {
  if (formation.ownerId !== 'heroine.slayer-swim') throw new Error('Author enhanced formation for new general');
  formation.enhancedSlots = [
    {slot: 0, label: '攻撃＋30%', attackPercent: 30},
    {slot: 1, label: '物理防御＋40%', physicalDefensePercent: 40},
    {slot: 2, label: '魔法防御＋40%', magicDefensePercent: 40},
    {slot: 3, label: '速度＋20%', speedPercent: 20},
    {slot: 4, label: '会心率＋20%', criticalBp: 2000},
  ];
}
fs.writeFileSync(`${root}/battle-plan11-7.json`, JSON.stringify(catalog, null, 2) + '\n');
