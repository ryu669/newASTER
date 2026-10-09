import fs from 'node:fs';
import path from 'node:path';

// Initial authoring follows the existing character records in docs/references/characters.
const forms = {
  'heroine.annihilator': {visible: ['taste.flowers', 'personality.cool', 'appearance.horns'], profile: ['hair.black']},
  'heroine.annihilator-holy': {visible: ['taste.flowers', 'personality.cool', 'outfit.christmas'], profile: ['hair.black', 'appearance.horns']},
  'heroine.slayer-swim': {visible: ['hair.blonde', 'personality.active', 'outfit.swimsuit'], profile: []},
  'heroine.arcane': {visible: ['taste.books', 'taste.mechanics', 'personality.active'], profile: []},
  'heroine.arcane-academy': {visible: ['taste.books', 'personality.active', 'outfit.school'], profile: ['taste.mechanics']},
  'heroine.nighthawk': {visible: ['taste.books', 'personality.night'], profile: []},
  'heroine.shangrila': {visible: ['hair.silver', 'appearance.glasses', 'personality.cool'], profile: ['appearance.horns']},
};
const root = 'game/unity/Assets/Game/Resources/Combat';
for (const name of fs.readdirSync(root).filter(n => /^battle.*\.json$/.test(n))) {
  const file = path.join(root, name);
  const before = fs.readFileSync(file, 'utf8');
  const after = before.replace(/("id": "(heroine\.[a-z-]+)",[\s\S]*?"variantId": "[^"]+")(\s*,\s*"interactionTraitIds":\s*\[[^\]]*\])?(\s*,\s*"profileTraitIds":\s*\[[^\]]*\])?/g,
    (all, prefix, id) => {
      const def = forms[id];
      if (!def) return all;
      return prefix + ',\n      "interactionTraitIds": ' + JSON.stringify(def.visible) + ',\n      "profileTraitIds": ' + JSON.stringify(def.profile);
    });
  if (after !== before) {
    JSON.parse(after);
    fs.writeFileSync(file, after);
  }
}
