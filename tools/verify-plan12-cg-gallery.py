"""Compare read-only gallery captures with source art fitted to the launch screen."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[1]
folder = Path(sys.argv[1])
home = json.loads((folder / 'home-catalog.json').read_text(encoding='utf-8-sig'))
assets = {a['id']: a for a in home['assets']}
events = {e['id']: e for e in home['events']}
scripts = {s['id']: s for s in home['scripts']}
records = json.loads((folder / 'sweep.json').read_text(encoding='utf-8-sig'))['records']
assert records and all(r['view'].startswith('cg.') for r in records)
results = []
for row in records:
    script = scripts[events[row['source']]['sceneId']]
    asset = assets[next(c['assetId'] for c in script['commands'] if c['kind'] == 'cg')]
    source = ROOT / 'game/unity/Assets/Game/Resources' / (asset['resourcePath'] + '.png')
    with Image.open(source) as raw, Image.open(folder / row['image']) as capture:
        sw, sh = raw.size
        cw, ch = capture.size
        scale = min(cw / sw, ch / sh)
        fw, fh = round(sw * scale), round(sh * scale)
        left, top = round((cw - fw) / 2), round((ch - fh) / 2)
        expected = raw.convert('RGB').resize((fw, fh), Image.Resampling.BILINEAR)
        observed = capture.convert('RGB').crop((left, top, left + fw, top + fh))
        difference = ImageChops.difference(expected, observed)
        mean_error = sum(ImageStat.Stat(difference).mean) / 3
        # Different GPU filtering is allowed; major cropping/stretching/UI is not.
        assert mean_error < 8, (row['image'], mean_error)
        results.append(dict(image=row['image'], source=source.relative_to(ROOT).as_posix(),
                            sourceSize=[sw, sh], screenSize=[cw, ch],
                            fittedRect=[left, top, fw, fh], meanPixelError=mean_error))
report = dict(status='passed', captures=len(records), method='source vs full-screen contain fit; bilinear tolerance',
              scope='geometry and source correspondence; content quality is reviewed separately', rows=results)
(folder / 'gallery-fit-checked.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print('PLAN12_CG_GALLERY_FIT_PASS captures=' + str(len(records)))
