"""Read-only verification of a hero's supplied PNG candidates, without runtime measurements."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('hero')
parser.add_argument('poses', nargs='+')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
rows = []
for pose in args.poses:
    name = f'{args.hero}-{pose}-candidate-v1.png'
    original = root / 'game/art-source/plan9' / args.hero / name
    resource = root / 'game/unity/Assets/Game/Resources/Illustrations' / name
    if original.read_bytes() != resource.read_bytes():
        raise ValueError(f'Original/resource mismatch: {name}')
    with Image.open(resource) as image:
        if image.mode != 'RGBA':
            raise ValueError(f'RGBA required: {name}')
        alpha = image.getchannel('A')
        histogram = alpha.histogram()
        bounds = alpha.point(lambda v: 255 if v > 32 else 0).getbbox()
        intentional_crop = pose in ('portrait', 'cutin')
        if histogram[0] == 0 or sum(histogram[240:]) == 0 or bounds is None:
            raise ValueError(f'Transparency/visible body missing: {name}')
        if not intentional_crop and (bounds[0] == 0 or bounds[1] == 0 or bounds[2] == image.width or bounds[3] == image.height):
            raise ValueError(f'Visible silhouette touches canvas: {name}')
        rows.append(dict(heroineId=f'heroine.{args.hero}',pose=pose,width=image.width,height=image.height,
                         path=resource.relative_to(root).as_posix(),visibleAlphaBounds=bounds,
                         sha256=hashlib.sha256(resource.read_bytes()).hexdigest(),transparentPixels=histogram[0],
                         intentionalCrop=intentional_crop,status='candidate',finalDeliveryApproved=False))
output = root / f'docs/production/plan9-{args.hero}-asset-validation.json'
output.write_text(json.dumps(dict(schemaVersion=1,performanceMeasured=False,assets=rows),indent=2)+'\n',encoding='utf-8')
print(f'PLAN9_HERO_PNG_PASS {args.hero} {len(rows)} candidates / originals match / RGBA / visible bounds')
