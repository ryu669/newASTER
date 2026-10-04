"""Inspect candidate PNGs without editing them or measuring runtime performance."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'game/unity/Assets/Game/Resources/Illustrations'
rows = []
for pose in ('standing', 'attack', 'portrait'):
    path = FOLDER / f'iconoclast-{pose}-candidate-v1.png'
    with Image.open(path) as image:
        if image.mode != 'RGBA':
            raise ValueError(f'RGBA required: {path.name}')
        alpha = image.getchannel('A')
        hist = alpha.histogram()
        if hist[0] == 0 or sum(hist[240:]) == 0:
            raise ValueError(f'Missing transparency or visible character: {path.name}')
        bbox = alpha.point(lambda value: 255 if value > 32 else 0).getbbox()
        if bbox is None or (pose != 'portrait' and (bbox[0] == 0 or bbox[1] == 0 or bbox[2] == image.width or bbox[3] == image.height)):
            raise ValueError(f'Visible silhouette touches canvas edge: {path.name}')
        rows.append(dict(assetId=f'iconoclast-{pose}', heroineId='heroine.iconoclast',
                         path=str(path.relative_to(ROOT)).replace('\\', '/'),
                         sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                         width=image.width, height=image.height, mode=image.mode,
                         visibleAlphaBounds=bbox, transparentPixels=hist[0],
                         status='candidate', finalDeliveryApproved=False))
output = ROOT / 'docs/production/plan9-iconoclast-asset-validation.json'
output.write_text(json.dumps(dict(schemaVersion=1, performanceMeasured=False, assets=rows),
                             ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('PLAN9_CHARACTER_ASSET_PASS 3 RGBA candidates / visible bounds / SHA256; portrait allows intentional torso/wing crop')
