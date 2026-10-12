"""Append Weapon Master combat preparation; never changes the normal catalog."""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'game/unity/Assets/Game/ContentDrafts/WeaponMaster'
HERO = 'heroine.weapon-master'


def remap(value):
    if isinstance(value, str):
        return value.replace('heroine.excalipan', HERO)
    if isinstance(value, list):
        return [remap(v) for v in value]
    if isinstance(value, dict):
        return {k: remap(v) for k, v in value.items()}
    return value


def main():
    manifest_path = OUT / 'manifest.json'
    if manifest_path.exists() and json.loads(manifest_path.read_text(encoding='utf8')).get('available'):
        raise RuntimeError('Accepted form: do not overwrite its published preparation.')
    source = ROOT / 'game/unity/Assets/Game/Resources/Combat/battle-plan12-vindicator.json'
    baseline = json.loads(source.read_text(encoding='utf-8-sig'))
    combat = copy.deepcopy(baseline)
    heroine = remap(copy.deepcopy(next(h for h in combat['heroines'] if h['id'] == 'heroine.excalipan')))
    heroine.update(name='ウェポンマスター', personId=HERO, variantId='normal',
                   traitHpPercent=0, traitAttackPercent=5,
                   reactionStyleMain='active', reactionStyleSub='affectionate',
                   interactionTraitIds=['hair.black', 'body.slender', 'personality.active'])
    combat['heroines'].append(heroine)
    for index, (name, power, wait) in enumerate([
        ('もってけ泥棒 鉛弾大特売', 1.0, 90),
        ('採算度外視Win-Win', 0, 100),
        ('開け！ 最新兵器の玉手箱', 1.6, 145),
    ], 1):
        skill = dict(id=f'{HERO}.{index}', ownerId=HERO, name=name,
                     sourceSkillId=f'{HERO}.{index}', sourceFile='020戦闘_ウェポンマスター.mkv',
                     sourceSecond=10, observedSkillLevel=7,
                     ruleOrigin='video-observation-plus-newaster-original',
                     effectRuleId='effect.self-buff' if index == 2 else 'effect.damage',
                     targetRuleId='target.self' if index == 2 else 'target.all-enemies' if index == 3 else 'target.selected-enemy',
                     powerScale=power, partScale=1, resourceCost=0,
                     recoveryPercent=wait, castPercent=0, targetCount=0,
                     baseHealing=0, chainEligible=index != 2, damageCap=10000 if index == 1 else 50000 if index == 3 else 0,
                     criticalBonusBp=0, conditions=[], statusEffects=[], selfEffects=[],
                     damageType='' if index == 2 else 'physical',
                     attributes=[] if index == 2 else ['銃弾', '火'] if index == 3 else ['銃弾'])
        if index == 1:
            skill.update(enemyDefenseReductionPercent=20, enemyDefenseReductionTurns=3)
        if index == 2:
            skill['selfEffects'] = [dict(kind=k, percent=p, turns=1) for k, p in
                                    [('attack', 30), ('critical', 20), ('critical-damage', 15)]]
        if index == 3:
            skill['statusEffects'] = [dict(kind='burn', amount=40)]
        combat['skills'].append(skill)
    chain = remap(copy.deepcopy(next(c for c in combat['chainActions'] if c['heroineId'] == 'heroine.excalipan')))
    chain.update(powerScale=.8, attributes=['銃弾'])
    combat['chainActions'].append(chain)
    combat['contentReferences'] += [remap(copy.deepcopy(r)) for r in baseline['contentReferences'] if r['ownerId'] == 'heroine.excalipan']
    assert len(combat['heroines']) == 24
    for key in ('heroines', 'skills', 'chainActions', 'contentReferences'):
        assert combat[key][:len(baseline[key])] == baseline[key], key
    skills = [s for s in combat['skills'] if s['ownerId'] == HERO]
    assert len(skills) == 3 and len({s['name'] for s in skills}) == 3
    assert skills[1]['targetRuleId'] == 'target.self' and not skills[1]['chainEligible']
    assert all(e['turns'] == 1 for e in skills[1]['selfEffects'])
    manifest = dict(schemaVersion=1, available=False, stage='combat-preparation',
                    formIds=[HERO], baselineForms=23, targetForms=24,
                    requiredAssetsPerForm=20,
                    remaining=['skill-1-defense-reduction-runtime-and-tests',
                               'burn-target-personal-trait-runtime-and-tests',
                               'story-30-pages-18-poems-5-events',
                               'weapon-full-name-or-explicit-original-name',
                               'traits-and-weapon-tree', 'dedicated-art-20',
                               'skill-levels-and-old-save-tests',
                               'default-player-review', 'normal-play-promotion'])
    OUT.mkdir(parents=True, exist_ok=True)
    for name, data in [('combat.json', combat), ('manifest.json', manifest)]:
        (OUT / name).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('WEAPON_MASTER_COMBAT_PREPARATION_PASS forms=24 skills=3 baseline=preserved NOT_PUBLISHED')


if __name__ == '__main__':
    main()
