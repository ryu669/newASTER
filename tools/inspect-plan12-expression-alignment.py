"""Read-only QA sheets: captured faces and authored expression mapping."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]


def main():
    folder = Path(sys.argv[1]).resolve()
    home = json.loads((folder / 'home-catalog.json').read_text(encoding='utf-8-sig'))
    assets = {a['id']: a for a in home['assets']}
    output = folder / 'expression-inspection'
    output.mkdir(exist_ok=True)
    records = []
    for d in home['displays']:
        hero = d['heroineId']
        key = hero.removeprefix('heroine.')
        if not (folder / (key + '-expression.normal.png')).exists():
            continue
        board = Image.new('RGB', (1600, 1000), '#20252b')
        draw = ImageDraw.Draw(board)
        for i, v in enumerate(d['expressions']):
            screenshot = folder / (key + '-' + v['id'] + '.png')
            if not screenshot.exists():
                break
            asset = assets[v['assetId']]
            with Image.open(screenshot) as im:
                sx, sy = im.width / 1920, im.height / 1080
                face = im.crop(tuple(round(n * (sx if j % 2 == 0 else sy)) for j, n in enumerate((800, 160, 1150, 510)))).convert('RGB')
                face.thumbnail((390, 390))
            draw.text((i * 400 + 8, 5), key + ' / ' + v['id'], fill='white')
            board.paste(face, (i * 400 + 5, 28))
            sourcepath = ROOT / 'game/unity/Assets/Game/Resources' / (asset['resourcePath'] + '.png')
            with Image.open(sourcepath) as source:
                source_size = source.size
                preview = Image.new('RGBA', source.size, '#354050')
                preview.alpha_composite(source.convert('RGBA'))
                preview.thumbnail((390, 430))
                board.paste(preview.convert('RGB'), (i * 400 + (400 - preview.width) // 2, 490))
                ratio = None
                if asset['mappedOverlay']:
                    sr, tr = asset['overlaySourceRegion01'], asset['overlayRegion01']
                    standing = assets[d['standingAssetId']]
                    with Image.open(ROOT / 'game/unity/Assets/Game/Resources' / (standing['resourcePath'] + '.png')) as base:
                        source_ratio = source.width * sr['width'] / (source.height * sr['height'])
                        target_ratio = base.width * tr['width'] / (base.height * tr['height'])
                        ratio = target_ratio / source_ratio
                    patch = source.crop((round(source.width * sr['x']), round(source.height * sr['y']), round(source.width * (sr['x'] + sr['width'])), round(source.height * (sr['y'] + sr['height'])))).convert('RGB')
                    patch.thumbnail((280, 140))
                    board.paste(patch, (i * 400 + 60, 340))
            draw.text((i * 400 + 8, 930), 'source ' + str(source_size) + (' mapped X/Y=' + str(round(ratio, 4)) if ratio else ' full-frame/normal'), fill='white')
            records.append(dict(heroineId=hero, expression=v['id'], resource=asset['resourcePath'], sourceSize=list(source_size), mappedAspectScaleRatio=ratio, screenshot=screenshot.relative_to(ROOT).as_posix()))
        else:
            board.save(output / (key + '-expressions.jpg'), quality=95)
    (output / 'mapping-measurements.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('PLAN12_EXPRESSION_INSPECTION sheets=' + str(len(list(output.glob('*.jpg')))))


if __name__ == '__main__':
    main()
