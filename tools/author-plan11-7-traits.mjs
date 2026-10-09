import fs from 'node:fs';
import {pathToFileURL} from 'node:url';

// newASTER authoring choices based on existing narrative and art, not source-game trait observations.
export const forms = {
  'heroine.slayer': {visible: ['hair.blonde', 'personality.active', 'taste.flowers'], profile: []},
  'heroine.iconoclast': {visible: ['personality.active', 'taste.mechanics', 'body.slender'], profile: []},
  'heroine.undermine': {visible: ['personality.shy', 'taste.mechanics', 'personality.cool'], profile: []},
  'heroine.echidna': {visible: ['hair.silver', 'appearance.horns', 'taste.flowers'], profile: []},
  'heroine.excalipan': {visible: ['hair.red', 'taste.cooking', 'taste.sweets'], profile: []},
  'heroine.r': {visible: ['personality.active', 'taste.mechanics', 'body.slender'], profile: []},
  'heroine.shell': {visible: ['hair.silver', 'taste.mechanics', 'personality.shy'], profile: []},
  'heroine.oriflamme': {visible: ['hair.blonde', 'personality.active', 'taste.mechanics'], profile: []},
  'heroine.annihilator': {visible: ['taste.flowers', 'personality.cool', 'appearance.horns'], profile: ['hair.black']},
  'heroine.annihilator-holy': {visible: ['taste.flowers', 'personality.cool', 'outfit.christmas'], profile: ['hair.black', 'appearance.horns']},
  'heroine.slayer-swim': {visible: ['hair.blonde', 'personality.active', 'outfit.swimsuit'], profile: []},
  'heroine.arcane': {visible: ['taste.books', 'taste.mechanics', 'personality.active'], profile: []},
  'heroine.arcane-academy': {visible: ['taste.books', 'personality.active', 'outfit.school'], profile: ['taste.mechanics']},
  'heroine.nighthawk': {visible: ['taste.books', 'personality.night', 'personality.shy'], profile: []},
  'heroine.shangrila': {visible: ['hair.silver', 'appearance.glasses', 'personality.cool'], profile: ['appearance.horns']},
};
export function applyInteractionTraits(catalog) {
  for (const hero of catalog.heroines) {
    const def = forms[hero.id];
    if (!def || def.visible.length !== 3 || new Set(def.visible).size !== 3) throw new Error(`Author three interaction traits: ${hero.id}`);
    hero.interactionTraitIds = [...def.visible];
    hero.profileTraitIds = [...def.profile];
  }
}
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const file = 'game/unity/Assets/Game/Resources/Combat/battle-plan11-7.json';
  const catalog = JSON.parse(fs.readFileSync(file, 'utf8'));
  applyInteractionTraits(catalog);
  fs.writeFileSync(file, JSON.stringify(catalog, null, 2) + '\n');
}
