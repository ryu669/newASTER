"""Build an unpublished April draft from the accepted 24-form catalog."""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'game/unity/Assets/Game/ContentDrafts/April'
HERO = 'heroine.april'


def remap(value):
    if isinstance(value, str):
        return value.replace('heroine.slayer', HERO)
    if isinstance(value, list):
        return [remap(v) for v in value]
    if isinstance(value, dict):
        return {k: remap(v) for k, v in value.items()}
    return value


def main():
    manifest_path = OUT / 'manifest.json'
    if manifest_path.exists() and json.loads(manifest_path.read_text(encoding='utf8')).get('available'):
        raise RuntimeError('Accepted form: do not overwrite.')
    baseline = json.loads((ROOT / 'game/unity/Assets/Game/Resources/Combat/battle-plan12-weapon-master.json').read_text(encoding='utf-8-sig'))
    combat = copy.deepcopy(baseline)
    heroine = remap(copy.deepcopy(next(h for h in baseline['heroines'] if h['id'] == 'heroine.slayer')))
    heroine.update(name='エイプリル', personId=HERO, variantId='normal',
                   reactionStyleMain='elegant', reactionStyleSub='affectionate',
                   interactionTraitIds=['hair.purple', 'body.slender', 'taste.flowers'])
    combat['heroines'].append(heroine)
    for slot, (name, scale, wait) in enumerate([
        ('開墾の導', 1.48, 90), ('ローゼンフォート', 1.3, 120), ('切り拓く咲蕾の風', 1.78, 150)
    ], 1):
        healing = slot == 2
        combat['skills'].append(dict(
            id=f'{HERO}.{slot}', ownerId=HERO, sourceSkillId=f'{HERO}.{slot}', name=name,
            sourceFile='021戦闘_エイプリル.mkv', sourceSecond=10, observedSkillLevel=7,
            ruleOrigin='video-observation-plus-newaster-original',
            effectRuleId='effect.heal' if healing else 'effect.damage',
            targetRuleId='target.selected-allies' if healing else 'target.all-enemies' if slot == 3 else 'target.selected-enemy',
            powerScale=scale, partScale=1, resourceCost=0, recoveryPercent=wait, castPercent=0,
            targetCount=1 if healing else 0, baseHealing=0, chainEligible=not healing,
            damageCap=0 if healing else 50000 if slot == 3 else 10000, criticalBonusBp=0, conditions=[], selfEffects=[], statusEffects=[],
            damageType='' if healing else 'magic' if slot == 3 else 'physical',
            attributes=[] if healing else ['風'] if slot == 3 else ['刺突']))
    chain = remap(copy.deepcopy(next(c for c in baseline['chainActions'] if c['heroineId'] == 'heroine.slayer')))
    chain.update(powerScale=.8, attributes=['刺突'])
    combat['chainActions'].append(chain)
    combat['contentReferences'] += [remap(copy.deepcopy(r)) for r in baseline['contentReferences'] if r['ownerId'] == 'heroine.slayer']
    assert len(combat['heroines']) == 25
    for key in ('heroines', 'skills', 'chainActions', 'contentReferences'):
        assert combat[key][:len(baseline[key])] == baseline[key], key
    manifest = dict(schemaVersion=1, available=False, stage='combat-and-story-draft',
                    formIds=[HERO], baselineForms=24, targetForms=25, requiredAssetsPerForm=20,
                    remaining=['traits-and-weapon-tree-connection', 'dedicated-art-20-review',
                               'skill-levels-and-old-save-tests', 'default-player-review', 'normal-play-promotion'])
    OUT.mkdir(parents=True, exist_ok=True)
    for name, value in [('combat.json', combat), ('manifest.json', manifest)]:
        (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print('APRIL_COMBAT_DRAFT_PASS forms=25 skills=3 baseline=preserved NOT_PUBLISHED')


if __name__ == '__main__':
    main()
