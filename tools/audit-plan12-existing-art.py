"""Read-only PNG inspection of current forms; visual acceptance stays separate."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / 'game/unity/Assets/Game/Resources'


def main():
    index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8-sig'))
    combat = json.loads((ROOT / index['combatSource']).read_text(encoding='utf-8-sig'))
    story = json.loads((ROOT / index['storySource']).read_text(encoding='utf-8-sig'))
    framing = json.loads((RESOURCES / 'UI/heroine-portrait-framing.json').read_text(encoding='utf-8-sig'))
    portraits = {e['heroineId']: e for e in framing['entries']}
    acceptance_path = ROOT / 'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs'
    accepted = dict(re.findall(r'\{"([^"]+)",\s*"([0-9a-f]{64})"\}', acceptance_path.read_text(encoding='utf-8-sig')))
    rows, problems = [], []

    def inspect(form, role, resource, full_body=False):
        path = RESOURCES / (resource + '.png')
        row = {'formId': form, 'role': role, 'path': path.relative_to(ROOT).as_posix(), 'issues': []}
        if not path.is_file():
            row['issues'].append('missing_png')
        else:
            row['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
            row['registeredSha256'] = accepted.get(resource)
            if resource not in accepted:
                row['issues'].append('acceptance_hash_not_registered')
            elif row['sha256'] != accepted[resource]:
                row['issues'].append('acceptance_hash_mismatch')
            with Image.open(path) as im:
                im.load()
                row.update(width=im.width, height=im.height, mode=im.mode)
                if role == 'cg' and im.mode not in ('RGB', 'RGBA'):
                    row['issues'].append('unsupported_cg_mode')
                elif role != 'cg' and im.mode != 'RGBA':
                    row['issues'].append('not_rgba')
                if im.mode == 'RGBA':
                    alpha = im.getchannel('A')
                    hist = alpha.histogram()
                    bbox = alpha.point(lambda a: 255 if a > 32 else 0).getbbox()
                    row.update(alphaRange=list(alpha.getextrema()), visibleBounds=bbox)
                    if role == 'cg' and alpha.getextrema() != (255, 255):
                        row['issues'].append('nonopaque_cg')
                    if not bbox:
                        row['issues'].append('no_visible_pixels')
                    if role != 'cg' and not hist[0]:
                        row['issues'].append('no_fully_transparent_pixels')
                    if full_body and bbox and (bbox[0] == 0 or bbox[1] == 0 or bbox[2] == im.width or bbox[3] == im.height):
                        row['issues'].append('silhouette_touches_canvas_edge_review_required')
                if role == 'portrait' and (im.width < 1536 or im.height < 1024 or im.width * 2 != im.height * 3):
                    row['issues'].append('portrait_size_or_ratio')
                if role == 'cg' and abs(im.width / im.height - 16 / 9) > .02:
                    row['issues'].append('cg_non_16_9_runtime_framing_review_required')
        rows.append(row)
        if row['issues']:
            problems.append(row)

    for form in combat['heroines']:
        fid, key = form['id'], form['id'].removeprefix('heroine.')
        if fid not in portraits:
            raise ValueError(f'Missing portrait framing: {fid}')
        inspect(fid, 'portrait', portraits[fid]['resourcePath'])
        # Inspect actual files, retaining the legacy forms' distinct SD role names.
        for role in ['standing', 'attack', 'hit', 'cutin', 'expression-joy', 'expression-puzzled',
                     'expression-determined', 'sd-idle', 'sd-walk', 'sd-talk', 'sd-react',
                     'sd-sit', 'sd-work', 'sd-look']:
            resource = f'Illustrations/{key}-{role}-candidate-v1'
            if (RESOURCES / (resource + '.png')).exists():
                inspect(fid, role, resource, full_body=role != 'cutin')
        events = [e for e in story['events'] if e.get('ownerId') == fid]
        for event in events:
            inspect(fid, 'cg', event['cgResourcePath'])
    report = {
        'scope': 'current PNG metadata and silhouette bounds; not runtime or final visual acceptance',
        'forms': len(combat['heroines']), 'inspectedImages': len(rows),
        'status': 'review_required' if problems else 'metadata_checks_passed',
        'limitations': ['optional role discovery does not prove runtime binding completeness',
                       'alpha bounds do not prove anatomical completeness',
                       'no new Player build, input, listening or performance measurement'],
        'issueCount': len(problems), 'assets': rows,
    }
    output = ROOT / 'docs/production/plan12-existing-art-audit.json'
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN12_ART_AUDIT forms={report["forms"]} images={len(rows)} issues={len(problems)}')
    for row in problems:
        print(row['formId'], row['role'], ','.join(row['issues']))


if __name__ == '__main__':
    main()
