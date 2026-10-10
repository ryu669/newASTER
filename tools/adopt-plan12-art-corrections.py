"""Adopt reviewed replacement assets byte-for-byte, preserving previous sources."""
import hashlib, json, re, shutil, sys, uuid
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
manifest_path = ROOT / sys.argv[1]
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
cs = ROOT / 'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs'
text = cs.read_text(encoding='utf-8-sig')
reports = []
for row in manifest['assets']:
    assert row['visualReview'].startswith('passed:')
    resource = row['resource']
    assert resource.startswith('Illustrations/') and '..' not in resource
    source = Path(row['path'])
    target = ROOT / 'game/unity/Assets/Game/Resources' / (resource + '.png')
    is_new = not target.exists()
    saved = manifest_path.parent / 'adopted' / target.name
    previous = manifest_path.parent / 'previous' / target.name
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    with Image.open(source) as im:
        size = list(im.size)
        if '-event-' in resource:
            assert im.width > im.height
        elif '-sd-' in resource:
            assert im.mode == 'RGBA' and im.getchannel('A').getextrema()[0] == 0
    if target.exists() and not previous.exists():
        previous.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(target, previous)
    saved.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, saved)
    shutil.copy2(source, target)
    meta = Path(str(target) + '.meta')
    if not meta.exists():
        template = target.parent / 'arcane-academy-sd-idle-candidate-v1.png.meta'
        meta.write_text(re.sub(r'guid: [a-f0-9]+', 'guid: ' + uuid.uuid4().hex, template.read_text(encoding='utf-8')), encoding='utf-8')
    pattern = r'("' + re.escape(resource) + r'",")[a-f0-9]{64}("})'
    text, count = re.subn(pattern, lambda m: m[1] + digest + m[2], text)
    if count == 0 and is_new:
        text = text.replace('        });', '            {"' + resource + '","' + digest + '"},\n        });', 1)
    else:
        assert count == 1, (resource, count)
    assert hashlib.sha256(saved.read_bytes()).hexdigest() == digest
    reports.append(dict(resource=resource, source=saved.relative_to(ROOT).as_posix(), sha256=digest, size=size, visualReview=row['visualReview']))
cs.write_text(text, encoding='utf-8')
output = manifest_path.with_name(manifest_path.stem + '-adoption.json')
output.write_text(json.dumps(dict(tool=manifest['tool'], pixelEditing=False, assets=reports, runtimeCapture='pending'), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'PLAN12_CORRECTIONS_ADOPTED count={len(reports)} exact_bytes=True previous_sources_preserved=True')
