"""Read-only pixel audit; no images are modified or exported."""
import hashlib
import json
from pathlib import Path
from PIL import Image

repo = Path(__file__).resolve().parents[1]
manifest = json.loads((repo / 'game/unity/Assets/Game/Resources/Illustrations/battle-formal.json').read_text(encoding='utf-8-sig'))
part = next(p for p in manifest['parts'] if p['partId'] == 'crystal-horn-crown')
placement = part['placement']
assert placement['enabled'], 'Authored placement must be explicitly enabled'
records = []
for version in ('v1', 'v2'):
    relative = f'game/unity/Assets/Game/Resources/Illustrations/green-crown-candidate-{version}.png'
    path = repo / relative
    with Image.open(path) as image:
        assert image.mode == 'RGBA', 'Crown requires native RGBA'
        alpha = image.getchannel('A')
        histogram = alpha.histogram()
        box = alpha.point(lambda value: 255 if value > 16 else 0).getbbox()
        assert box, 'Empty crown is invalid'
        transform = placement if version == 'v2' else {'x': 0, 'y': 0, 'scale': 1}
        mapped = [box[0]*transform['scale']+image.width*transform['x'], box[1]*transform['scale']+image.height*transform['y'], box[2]*transform['scale']+image.width*transform['x'], box[3]*transform['scale']+image.height*transform['y']]
        records.append({'version': version, 'path': relative, 'width': image.width, 'height': image.height, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest().upper(), 'alphaAbove16Bounds': box, 'alpha1Through16Pixels': sum(histogram[1:17]), 'placement': transform, 'placedAlphaAbove16Bounds': mapped})
assert records[0]['width'] == records[1]['width'] == 1254 and records[0]['height'] == records[1]['height'] == 1254, 'Shared canvas must stay native'
assert records[1]['placedAlphaAbove16Bounds'][1] >= 40, 'Crown tips require at least 40 canvas pixels of top margin'
assert all(value >= 0 for value in records[1]['placedAlphaAbove16Bounds']) and max(records[1]['placedAlphaAbove16Bounds']) <= 1254
assert part['resourcePath'] == 'Illustrations/green-crown-candidate-v2' and part['hideWhenDestroyed']
output = repo / 'tmp/plan7-crown-audit.json'
output.parent.mkdir(exist_ok=True)
output.write_text(json.dumps({'status': 'candidate', 'pixelThreshold': 16, 'assets': records, 'scope': 'Native canvas and top clipping only. Tiny-alpha noise, paint layers and final quality are not accepted by this audit.'}, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'PLAN7_CROWN_AUDIT_PASS topMargin={records[1]["placedAlphaAbove16Bounds"][1]:.2f}px {output}')
