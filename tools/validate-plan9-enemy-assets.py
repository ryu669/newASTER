"""Read-only verification of independent enemy layers and their native placement."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('enemy')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
resources = root/'game/unity/Assets/Game/Resources'
manifest_path = resources/f'Illustrations/battle-{args.enemy}-candidate-v1.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
assert 4 <= len(manifest['parts']) <= 6
assert len({p['partId'] for p in manifest['parts']}) == len(manifest['parts'])
rows = []
body_size = None
layers = [('body',manifest['bodyResourcePath'],manifest.get('bodyPlacement'))]
layers += [(p['partId'],p['resourcePath'],p.get('placement')) for p in manifest['parts']]
layers += [('major',manifest['enemyMajorResourcePath'],None)]
for purpose,path,placement in layers:
    resource = resources/(path+'.png')
    original = root/'game/art-source/plan9'/args.enemy/resource.name
    assert original.read_bytes() == resource.read_bytes(), f'Original mismatch: {path}'
    with Image.open(resource) as image:
        assert image.mode == 'RGBA', path
        if body_size is None:
            body_size = image.size
        assert image.size == body_size, f'Canvas mismatch: {path}'
        alpha = image.getchannel('A')
        bounds = alpha.point(lambda v:255 if v>32 else 0).getbbox()
        assert alpha.histogram()[0] > 0 and bounds, f'Transparency missing: {path}'
        assert bounds[0]>0 and bounds[1]>0 and bounds[2]<image.width and bounds[3]<image.height, f'Clipped silhouette: {path}'
        if placement and placement['enabled']:
            assert placement['x']>=0 and placement['y']>=0 and placement['scale']>0
            assert placement['x']+placement['scale']<=1 and placement['y']+placement['scale']<=1
        rows.append(dict(purpose=purpose,path=resource.relative_to(root).as_posix(),width=image.width,height=image.height,
                         alphaBounds=bounds,sha256=hashlib.sha256(resource.read_bytes()).hexdigest(),status='candidate'))
assert len({r['sha256'] for r in rows}) == len(rows), 'Independent layers reuse identical bytes'
background = resources/(manifest['backgroundResourcePath']+'.png')
assert (root/'game/art-source/plan9'/args.enemy/background.name).read_bytes() == background.read_bytes()
with Image.open(background) as image:
    assert abs(image.width/image.height-16/9)<.02
    assert image.mode!='RGBA' or image.getchannel('A').getextrema()==(255,255)
output = root/f'docs/production/plan9-{args.enemy}-asset-validation.json'
output.write_text(json.dumps(dict(schemaVersion=1,colossusId='colossus.'+args.enemy,performanceMeasured=False,
    finalDeliveryApproved=False,manifestSha256=hashlib.sha256(manifest_path.read_bytes()).hexdigest(),layers=rows),indent=2)+'\n',encoding='utf-8')
print(f'PLAN9_ENEMY_ASSETS_PASS {args.enemy} {len(rows)} distinct RGBA layers / originals match / native placements')
