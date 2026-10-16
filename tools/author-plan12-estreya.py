"""Build Estreya's unpublished combat draft from the accepted April catalog."""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'game/unity/Assets/Game/ContentDrafts/Estreya'
HERO = 'heroine.estreya'


def remap(value):
    if isinstance(value, str):
        return value.replace('heroine.iconoclast', HERO)
    if isinstance(value, list):
        return [remap(v) for v in value]
    if isinstance(value, dict):
        return {k: remap(v) for k, v in value.items()}
    return value


def main():
    manifest = OUT / 'manifest.json'
    if manifest.exists() and json.loads(manifest.read_text(encoding='utf8')).get('available'):
        raise RuntimeError('Accepted form: do not overwrite.')
    source = ROOT / 'game/unity/Assets/Game/Resources/Combat/battle-plan12-april.json'
    baseline = json.loads(source.read_text(encoding='utf-8-sig'))
    combat = copy.deepcopy(baseline)
    heroine = remap(copy.deepcopy(next(h for h in baseline['heroines'] if h['id'] == 'heroine.iconoclast')))
    heroine.update(name='エストレヤ', personId=HERO, variantId='normal', traitHpPercent=5, traitAttackPercent=0,
                   reactionStyleMain='active', reactionStyleSub='bold',
                   interactionTraitIds=['hair.red', 'body.muscular', 'taste.muscle'])
    combat['heroines'].append(heroine)
    # Observed Lv7 percentages become this game's Lv1 starting values.
    # Recovery, damage caps and progression are newASTER balance decisions.
    for slot, (name, scale, wait) in enumerate([
        ('ナックルスピア', 1.16, 70), ('烈熱ムーンサルト', 3.60, 140),
        ('新星爆拳ボンバイエ', 2.60, 180)
    ], 1):
        combat['skills'].append(dict(
            id=f'{HERO}.{slot}', ownerId=HERO, sourceSkillId=f'{HERO}.{slot}', name=name,
            sourceFile='026戦闘_エストレヤ.mkv', sourceSecond=10, observedSkillLevel=7,
            ruleOrigin='video-observation-plus-newaster-original',
            effectRuleId='effect.damage', targetRuleId='target.selected-enemy',
            powerScale=scale, partScale=1, resourceCost=0, recoveryPercent=wait,
            castPercent=0, targetCount=0, baseHealing=0, chainEligible=True,
            damageCap=50000 if slot == 3 else 10000, criticalBonusBp=0,
            conditions=[], selfEffects=[], statusEffects=[], damageType='physical',
            attributes=['打撃'] if slot == 1 else ['打撃', '火'],
            selfDamageMaxHpPercent=5 if slot == 2 else 0,
            selfHpAtLeastPercent=50 if slot == 3 else 0,
            selfHpDamageBonusPercent=100 if slot == 3 else 0))
    chain = remap(copy.deepcopy(next(c for c in baseline['chainActions'] if c['heroineId'] == 'heroine.iconoclast')))
    chain.update(powerScale=.8, attributes=['打撃'], ruleOrigin='newaster-original')
    combat['chainActions'].append(chain)
    combat['contentReferences'] += [remap(copy.deepcopy(r)) for r in baseline['contentReferences'] if r['ownerId'] == 'heroine.iconoclast']
    for key in ('heroines', 'skills', 'chainActions', 'contentReferences'):
        assert combat[key][:len(baseline[key])] == baseline[key], key
    OUT.mkdir(parents=True, exist_ok=True)
    values = {'combat.json': combat, 'manifest.json': dict(
        schemaVersion=1, available=False, stage='combat-and-story-draft', formIds=[HERO],
        baselineForms=len(baseline['heroines']), targetForms=len(combat['heroines']), requiredAssetsPerForm=20,
        remaining=['identity-and-common-tree-connection', 'dedicated-art-20-review',
                   'old-save-and-home-tests', 'default-player-review', 'normal-play-promotion'])}
    for name, value in values.items():
        (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print('ESTREYA_COMBAT_DRAFT_PASS forms=26 skills=3 baseline=preserved NOT_PUBLISHED')


if __name__ == '__main__':
    main()
