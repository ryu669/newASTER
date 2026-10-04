"""Read-only pixel audit; rejects face variants that change the base elsewhere."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

p = argparse.ArgumentParser()
p.add_argument('--base', required=True)
p.add_argument('--candidate', required=True)
p.add_argument('--region', type=int, nargs=4, required=True, metavar=('X', 'Y', 'WIDTH', 'HEIGHT'))
p.add_argument('--output', required=True)
args = p.parse_args()
base_path, candidate_path = Path(args.base), Path(args.candidate)
with Image.open(base_path) as base, Image.open(candidate_path) as candidate:
    if base.mode != 'RGBA' or candidate.mode != 'RGBA' or base.size != candidate.size:
        raise ValueError('Same RGBA canvas required')
    x, y, width, height = args.region
    if x < 0 or y < 0 or width <= 0 or height <= 0 or x+width > base.width or y+height > base.height:
        raise ValueError('Invalid face region')
    inside = outside = 0
    for index, (a, b) in enumerate(zip(base.getdata(), candidate.getdata())):
        # Hidden RGB in fully transparent pixels has no effect on compositing.
        if a == b or a[3] == b[3] == 0:
            continue
        px, py = index % base.width, index // base.width
        if x <= px < x+width and y <= py < y+height:
            inside += 1
        else:
            outside += 1
    result = dict(schemaVersion=1, heroineId='heroine.iconoclast',
                  baseSha256=hashlib.sha256(base_path.read_bytes()).hexdigest(),
                  candidateSha256=hashlib.sha256(candidate_path.read_bytes()).hexdigest(),
                  faceRegion=args.region, changedInside=inside, changedOutside=outside,
                  accepted=inside > 0 and outside == 0,
                  reason='face-only-invariant' if inside > 0 and outside == 0 else 'changed-outside-face-or-no-expression-change',
                  performanceMeasured=False)
    Path(args.output).write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print('PLAN9_EXPRESSION_SOURCE_' + ('PASS' if result['accepted'] else 'REJECTED') +
          f' inside={inside} outside={outside}')
