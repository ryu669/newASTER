"""Validate captures and produce read-only diagnostic comparison sheets."""
import hashlib
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageStat

ROOT = Path(__file__).resolve().parents[1]


def main():
    folder = Path(sys.argv[1])
    size = tuple(map(int, sys.argv[2:4]))
    report = json.loads((folder / 'sweep.json').read_text(encoding='utf-8-sig'))
    records = report['records']
    index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8-sig'))
    combat = json.loads((ROOT / index['combatSource']).read_text(encoding='utf-8-sig'))
    expected = {h['id'] for h in combat['heroines']}
    filtered = len(sys.argv) > 4
    if filtered:
        expected = {'heroine.' + sys.argv[4]}
        expected_views = set(sys.argv[5].split(','))
    gallery = len(records) == 75
    count = 75 if gallery else 240
    if filtered:
        count = len(expected_views)
    assert len(records) == count and {r['heroineId'] for r in records} == expected
    assert len({(r['heroineId'], r['view']) for r in records}) == count
    boards = []
    for hero in sorted(expected):
        rows = [r for r in records if r['heroineId'] == hero]
        if filtered:
            assert len(rows) == len(expected_views) and {r['view'] for r in rows} == expected_views
        else:
            assert len(rows) == (5 if gallery else 16)
            assert {r['view'] for r in rows} == ({'cg.' + str(i) for i in range(5)} if gallery else {'detail', 'tree', 'battle', *('expression.' + e for e in ('normal', 'joy', 'puzzled', 'determined')), *('sd.' + e for e in ('idle', 'sit', 'work', 'look')), *('cg.' + str(i) for i in range(5))})
        board = Image.new('RGB', (1920, 1200), '#15191e')
        draw = ImageDraw.Draw(board)
        for i, row in enumerate(rows):
            path = folder / row['image']
            with Image.open(path) as im:
                assert im.size == size, (path, im.size, size)
                stats = ImageStat.Stat(im.convert('RGB'))
                assert max(stats.mean) > 20 and max(stats.stddev) > 20, f'Black capture: {path}'
                preview = im.convert('RGB')
                preview.thumbnail((480, 270))
            x, y = (i % 4) * 480, (i // 4) * 300
            draw.text((x + 5, y + 4), hero + ' / ' + row['view'], fill='white')
            board.paste(preview, (x, y + 26))
            row['path'] = path.relative_to(ROOT).as_posix()
            row['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        boardpath = folder / (hero.removeprefix('heroine.') + '-review.jpg')
        board.save(boardpath, quality=92)
        boards.append({'heroineId': hero, 'path': boardpath.relative_to(ROOT).as_posix(),
                       'sha256': hashlib.sha256(boardpath.read_bytes()).hexdigest()})
    report.update(galleryOnly=gallery, size=size, captureChecks='passed', visualReview='pending',
                  scope='diagnostic state fixtures; not manual journey or sound acceptance', boards=boards)
    (folder / 'checked.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN12_ART_SWEEP_IMAGES_PASS captures={len(records)} boards={len(boards)}')


if __name__ == '__main__':
    main()
