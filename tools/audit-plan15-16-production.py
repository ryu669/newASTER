"""Read-only source/alpha audit and review evidence for Plan11-5/6."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageStat

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'game/unity/Assets/Game/Resources/Illustrations'
BASE_PROMPT = (
    'Create a production-ready transparent-background Japanese anime chibi garden-life sprite '
    'of the exact adult fantasy heroine in the reference. Reference image is character/style '
    'reference: preserve magenta twin spiral hair, amber eyes, white round hat with pink ribbon '
    'and gold blue-gem brooch, gold floating halo, white feathered wings with intricate gold and '
    'turquoise clock ornaments, white lace blouse, black ruffled corset dress, black tights and '
    'boots, pink shoulder bag. Match the reference\'s detailed hand-painted anime rendering, '
    'proportions, outline and palette. Full body and both wings fully within a square canvas, '
    'centered with clear transparent margins. One single character, no background, no ground, '
    'no text, no watermark, no other character.'
)
POSES = {
    'sit': 'Pose: seated peacefully as if using a low garden bench, knees bent naturally and both '
           'boots visible, hands resting on lap, calm friendly expression. Do not draw the bench '
           'or any furniture; transparent behind and under her.',
    'work': 'Pose: gently tending a garden plant using a small elegant gold watering can held at '
            'waist level with both hands, leaning forward slightly, attentive friendly expression. '
            'No plant, soil, furniture or scenery; the watering can is the only added prop.',
    'look': 'Pose: standing quietly in three-quarter view looking upward with curiosity at the '
            'distant sky, one hand shading her eyes lightly, other hand resting near the dress, '
            'relaxed posture. No added props or scenery.',
}


def main():
    combat = json.loads((ROOT / 'game/unity/Assets/Game/Resources/Combat/battle-plan10-shangrila.json')
                       .read_text(encoding='utf-8-sig'))
    assets = []
    for pose, prompt in POSES.items():
        path = ART / f'arcane-sd-{pose}-candidate-v1.png'
        with Image.open(path) as im:
            alpha = im.getchannel('A')
            hist = alpha.histogram()
            assert im.width >= 1000 and im.height >= 1000 and hist[0] > im.width * im.height / 10
            assert hist[255] > 0 and path.with_suffix('.png.meta').exists()
            assets.append(dict(path=path.relative_to(ROOT).as_posix(), size=list(im.size),
                               sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                               nativeAlphaPreserved=True, fullyTransparentFraction=hist[0] / (im.width * im.height),
                               generator='built-in image_gen; reference edit',
                               reference='game/unity/Assets/Game/Resources/Illustrations/arcane-sd-idle-candidate-v1.png',
                               finalPrompt=BASE_PROMPT + ' ' + prompt))
    roster = []
    for hero in combat['heroines']:
        prefix = hero['id'].removeprefix('heroine.')
        present = [pose for pose in ('idle', 'sit', 'work', 'look', 'walk', 'talk', 'react')
                   if (ART / f'{prefix}-sd-{pose}-candidate-v1.png').exists()]
        roster.append(dict(heroineId=hero['id'], presentPoses=present,
                           missingFurniturePoses=[p for p in ('sit', 'work', 'look') if p not in present],
                           optionalUnwiredPoses=[p for p in ('walk', 'talk', 'react') if p not in present]))
    report = dict(schemaVersion=1, plans=['Plan11-5', 'Plan11-6'],
                  generatedAssets=assets, authoredForms=len(roster), heroinePoseAudit=roster,
                  missingFurniturePoseCount=sum(len(r['missingFurniturePoses']) for r in roster),
                  runtimeFallback='Missing optional furniture poses remain visible using the heroine idle sprite; no hidden actors.',
                  productionFindings=[
                      '256-person stable-ID navigation/search, twelve-card paging and affection save paths pass synthetic scale tests.',
                      'The production catalog contains 15 authored forms. It is not a finished 256-person content pack.',
                      'ProductionEconomyCatalog WeaponOwners/BranchNames/Attacks/Powers remain authored tables; another owner needs definitions.',
                      'Portrait framing, individual skills/traits, life poses, ADV/CG and event conditions need each heroine content registration.',
                      'GardenArtComposition furniture contact/masks need pose-specific art review; the watering-can work sprite is a gardening candidate.',
                      'Normal movement currently animates idle sprites. Optional walk/talk/react files are not automatically consumed.',
                  ],
                  balance=json.loads((ROOT / 'tmp/plan9-balance.json').read_text(encoding='utf-8')))
    basic = (ROOT / 'tools/validate-plan10-ui-player.ps1').read_text(encoding='utf-8-sig').split('[int]$Width')[0]
    extra = (ROOT / 'tools/validate-plan15-16-ui.ps1').read_text(encoding='utf-8-sig').split('$views=@(')[1].split(')')[0]
    views = sorted(set(re.findall(r"'([^']+)'", basic + extra)))
    frames = []
    for width, height in ((1280, 720), (1920, 1080)):
        folder = ROOT / f'tmp/plan10-ui/player-{width}x{height}'
        for view in views:
            path = folder / f'ui-{view}.png'
            log = path.with_suffix('.log').read_text(encoding='utf-8', errors='replace')
            assert f'PLAN10_UI_AUDIT_PLAYER_PASS view={view} ' in log, view
            assert not re.search(r'(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException)', log), view
            with Image.open(path) as im:
                assert im.size == (width, height) and max(ImageStat.Stat(im.convert('RGB')).stddev) > 20, view
            frames.append(dict(view=view, size=[width, height], sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    report['uiVerification'] = dict(uniqueViews=len(views), verifiedFrames=len(frames), frames=frames,
                                    method='Unity Player rendered captures, log exception checks and visual contact-sheet review; physical input recorded separately')
    automatic = (ROOT / 'tmp/plan15-16-automatic-final.log').read_text(encoding='utf-8-sig')
    report['automaticAssertions'] = int(re.search(r'AUTOMATIC_CHAIN_PASS (\d+) assertions', automatic)[1])
    report['navigation1000RefreshMs'] = float(re.search(r'BK13_NAVIGATION_1000_REFRESH_MS ([\d.]+)', automatic)[1])
    report['search1000QueryMs'] = float(re.search(r'BK13_SEARCH_1000_QUERY_MS ([\d.]+)', automatic)[1])
    report['sleepClockDocumentation'] = 'https://learn.microsoft.com/en-us/windows/win32/api/realtimeapiset/nf-realtimeapiset-queryunbiasedinterrupttime'
    report['manualLimitations'] = ['Full PC sleep/resume has not been performed during this remote run.',
                                   '256-person tests use synthetic stable-ID/roster/affection data; rendered production UI uses the current 15 authored forms.',
                                   'Native window resize drag was blocked by the automation surface boundary. Resizable style and aspect enforcement are implemented; free resize needs a hands-on check.',
                                   'Physical minimize stopped rewards, but the automation surface could not restore that minimized window. Pause/resume state is covered by automatic tests.']
    report['physicalInputVerification'] = {
        'platform': 'Windows, Unity Player, native mouse/keyboard through computer-use',
        'saveIsolation': 'Dedicated plan15 manual fixture and watch.settings.plan15-test; no production campaign fixture edits',
        'passed': [
            '720p restart restores Arcane Academy, information face, and arcane search with two results',
            'Page turns remain within the two search results; changing subject restores its overview',
            'Arcane level 21 to 22 completes and returns to the same information face',
            'Five low-level materials cost 2500 Nectar; inventory 10000 to 10005 and balance 19950 to 17450',
            'Exchange confirmation survives automatic clock-save revisions and blocks bookmark input',
            'Time reward crosses 1799.75 seconds, awards 100 stones, and restart does not duplicate the daily reward',
            'Locked saved red colossus safely restores green colossus and retains selected level 32',
            'Same native window switches to 480x270, shows at most four agents, and exposes only return',
            'Topmost ON and OFF confirmed by native adapter logs; normal topmost state restored',
            'Escape restores 1280x720; return button restores 1920x1080',
            '1080p battle starts at green colossus level 32; skill damage and manual pause respond',
            'Retreat saves its result and returns to green colossus level 32',
        ],
        'watchFpsLog': 'tmp/plan15-16-manual/player-return-input.log',
        'watchFpsObserved': '29.84 to 29.99 FPS over repeated 30-second intervals, four agents',
        'growthControllerAssertions': 11,
    }
    regression = (ROOT / 'tmp/plan15-16-manual/battle-regression.log').read_text(encoding='utf-8', errors='replace')
    assert 'BATTLE_START' in regression and not re.search(r'(?m)^\w*Exception:', regression)
    report['physicalInputVerification']['battleRegression'] = 'Native 1080p skill action without Slayer: no exceptions after guarding the absent model actor index.'
    build = (ROOT / 'tmp/plan15-16-build-release.log').read_text(encoding='utf-8', errors='replace')
    assert 'PLAN15_16_UNITY_SAVE_PASS' in build and 'PLAN15_16_BUILD_PASS' in build
    report['unityBuild'] = dict(version='6000.6.3f1', target='Windows x64', result='Succeeded',
                               player='game/Builds/plan11-5-6/newASTER.exe',
                               sha256=hashlib.sha256((ROOT / 'game/Builds/plan11-5-6/newASTER.exe').read_bytes()).hexdigest())
    preview = ROOT / 'docs/production/plan11-5-6-preview'
    preview.mkdir(exist_ok=True)
    selected = sorted(set(re.findall(r"'([^']+)'", extra))) + ['battle-targets']
    for start in range(0, len(selected), 12):
        sheet = Image.new('RGB', (1440, 936), (18, 30, 38))
        draw = ImageDraw.Draw(sheet)
        for i, view in enumerate(selected[start:start + 12]):
            path = ROOT / f'tmp/plan10-ui/player-1280x720/ui-{view}.png'
            im = Image.open(path).convert('RGB')
            im.thumbnail((480, 210))
            x, y = i % 3 * 480, i // 3 * 234
            sheet.paste(im, (x, y))
            draw.text((x + 8, y + 215), view + ' 720p', fill='white')
        sheet.save(preview / f'ui-{start // 12 + 1}.png')
    destination = ROOT / 'docs/production/plan11-5-6-validation.json'
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN15_16_ART_AUDIT_PASS {len(assets)} native transparent assets / {len(roster)} forms / '
          f'{report["missingFurniturePoseCount"]} remaining furniture poses')


if __name__ == '__main__':
    main()
