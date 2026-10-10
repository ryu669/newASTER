"""Audit exported runtime display references, without modifying source artwork."""
import hashlib
import json
import re
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'game/unity/Assets/Game/Resources'


def main():
    source = Path(sys.argv[1]).resolve()
    home = json.loads(source.read_text(encoding='utf-8-sig'))
    assets = {a['id']: a for a in home['assets']}
    accepted = dict(re.findall(r'\{"([^"]+)",\s*"([0-9a-f]{64})"\}', (ROOT / 'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs').read_text(encoding='utf-8-sig')))
    rows = []
    fallbacks = []

    def inspect(hero, role, resource):
        path = RES / (resource + '.png')
        assert path.is_file(), (hero, role, resource)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert accepted.get(resource) == digest, (hero, role, resource, 'acceptance hash')
        with Image.open(path) as im:
            assert im.mode in ('RGB', 'RGBA'), (resource, im.mode)
            rows.append(dict(heroineId=hero, role=role, resource=resource, sha256=digest, size=list(im.size), mode=im.mode))

    for d in home['displays']:
        inspect(d['heroineId'], 'standing', assets[d['standingAssetId']]['resourcePath'])
        for e in d['expressions']:
            inspect(d['heroineId'], e['id'], assets[e['assetId']]['resourcePath'])
        for pose in ('idle', 'walk', 'talk', 'react', 'sit', 'work', 'look'):
            resource = 'Illustrations/' + d['heroineId'].removeprefix('heroine.') + '-sd-' + pose + '-candidate-v1'
            if not (RES / (resource + '.png')).is_file():
                fallbacks.append(dict(heroineId=d['heroineId'], requestedPose=pose, fallback='idle'))
                resource = resource.replace('-sd-' + pose + '-', '-sd-idle-')
            inspect(d['heroineId'], 'sd.' + pose, resource)
    scripts = {s['id']: s for s in home['scripts']}
    for e in home['events']:
        if e['heroineId'] not in home['heroineIds']:
            continue
        for c in scripts[e['sceneId']]['commands']:
            if c['kind'] == 'cg':
                inspect(e['heroineId'], e['id'], assets[c['assetId']]['resourcePath'])
    for entry in json.loads((RES / 'UI/heroine-weapon-trees.json').read_text(encoding='utf-8-sig'))['entries']:
        inspect(entry['heroineId'], 'weapon-tree', entry['resourcePath'])
    for entry in json.loads((RES / 'UI/heroine-portrait-framing.json').read_text(encoding='utf-8-sig'))['entries']:
        inspect(entry['heroineId'], 'portrait', entry['resourcePath'])
    manifest = json.loads((RES / 'Illustrations/battle-amber-king-serpent-candidate-v1.json').read_text(encoding='utf-8-sig'))
    bindings = {h['heroineId']: h for h in manifest['heroes']}
    for path in sorted((RES / 'Illustrations').glob('*-battle-binding.json')):
        binding = json.loads(path.read_text(encoding='utf-8-sig'))
        bindings.setdefault(binding['heroineId'], binding)
    missing_battle = []
    for hero in home['heroineIds']:
        binding = bindings[hero]
        for role, field in (('attack', 'attackResourcePath'), ('hit', 'hitResourcePath'), ('cutin', 'cutinResourcePath')):
            resource = binding.get(field)
            if not resource or not (RES / (resource + '.png')).is_file():
                missing_battle.append(dict(heroineId=hero, role=role, fallback=binding['resourcePath']))
                resource = binding['resourcePath']
            inspect(hero, role, resource)
    aliases = []
    for hero in home['heroineIds']:
        groups = {}
        for row in rows:
            if row['heroineId'] == hero and row['role'] != 'expression.normal':
                groups.setdefault(row['sha256'], []).append(row['role'])
        aliases.extend(dict(heroineId=hero, roles=roles) for roles in groups.values() if len(roles) > 1)
    missing_roles = [dict(heroineId=f['heroineId'], role='sd.' + f['requestedPose'], reason='idle_fallback') for f in fallbacks]
    missing_roles.extend(dict(heroineId=f['heroineId'], role=f['role'], reason='standing_fallback') for f in missing_battle)
    for group in aliases:
        for role in group['roles'][1:]:
            if not any(r['heroineId'] == group['heroineId'] and r['role'] == role for r in missing_roles):
                missing_roles.append(dict(heroineId=group['heroineId'], role=role, reason='same_image_as_' + group['roles'][0]))
    report = dict(missingDedicatedRoleCount=len(missing_roles), missingDedicatedRoles=missing_roles, productionAcceptance='insufficient' if fallbacks or missing_battle or aliases else 'visual_review_required', missingDedicatedPoses=fallbacks, missingBattlePoses=missing_battle, sameImageRoles=aliases, status='runtime_references_and_hashes_passed', catalog=source.relative_to(ROOT).as_posix(), catalogSha256=hashlib.sha256(source.read_bytes()).hexdigest(), formCount=len(home['heroineIds']), referenceCount=len(rows), uniqueImageCount=len({r['resource'] for r in rows}), rows=rows, sdIdleFallbacks=fallbacks, scope='display/expressions, all seven garden SD poses and three battle poses, event CGs and weapon trees; visual quality and battle motion are separate')
    (ROOT / 'docs/production/plan12-runtime-art-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f"PLAN12_RUNTIME_ART_PASS forms={report['formCount']} references={len(rows)} unique={report['uniqueImageCount']}")


if __name__ == '__main__':
    main()
