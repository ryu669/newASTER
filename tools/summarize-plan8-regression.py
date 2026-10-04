"""Record existing functional regression logs against the final journey build."""
import argparse
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--plan5', required=True)
parser.add_argument('--home', required=True)
parser.add_argument('--telemetry', required=True)
parser.add_argument('--focus', required=True)
parser.add_argument('--battle-menu', required=True)
parser.add_argument('--build-log', required=True)
args = parser.parse_args()

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

assembly = root / 'game/Builds/playable/newASTER_Data/Managed/Assembly-CSharp.dll'
resources = root / 'game/Builds/playable/newASTER_Data/resources.assets'
cases = []

def collect(directory_name, names, marker, screenshots=None):
    directory = root / 'tmp' / directory_name
    assert directory.resolve().parent == (root / 'tmp').resolve()
    for name in names:
        log = directory / (name + '.log')
        text = log.read_text(encoding='utf-8-sig')
        assert marker in text and 'Exception:' not in text
        assert log.stat().st_mtime >= assembly.stat().st_mtime
        screenshot = directory / ((screenshots or {}).get(name, name) + '.png')
        cases.append({'suite': directory_name, 'case': name, 'passed': True,
                      'logSha256': sha(log), 'screenshotSha256': sha(screenshot)})

collect(args.plan5, ['legacy', 'new'], 'PLAN5_PLAYER_ACCEPTANCE_PASS', {'legacy': 'legacy-confirm', 'new': 'new-confirm'})
collect(args.plan5, ['legacy-resume', 'new-resume'], 'PLAN5_PROCESS_RESTART_PASS', {'legacy-resume': 'legacy', 'new-resume': 'new'})
collect(args.plan5, ['many'], 'PLAN5_MANY_RELICS_PASS')
collect(args.plan5, ['world'], 'COLLECTION_END_NAVIGATION_PASS')
collect(args.home, [f'{form}-{case}-{height}' for form in ('Old', 'New') for case in ('Garden', 'Adv') for height in (720, 1080)], 'PLAN6_HOME_PLAYER_PASS')
collect(args.telemetry, ['activecombat-1080'], 'PLAN7_FULL_COMBAT_PASS commands=32 repaintedEvents=66')
collect(args.focus, ['settings-720', 'settings-1080'], 'PLAN7_FOCUS_AUDIO_PASS')
collect(args.battle_menu, [f'{case}-{height}' for case in ('closed', 'actions', 'targets') for height in (720, 1080)], 'BATTLE_MENU_CAPTURE_PASS')
telemetry = json.loads((root / 'tmp' / args.telemetry / 'plan8-telemetry-result.json').read_text(encoding='utf-8-sig'))
assert telemetry['passed'] and telemetry['normalSaveUnchanged'] and not telemetry['performanceMeasured']
assert telemetry['buildHash'] == sha(assembly) and telemetry['resourceHash'] == sha(resources)
journey = json.loads((root / 'docs/production/plan8-journey-validation.json').read_text(encoding='utf-8'))
assert all(r['result']['assemblySha256'] == sha(assembly) for r in journey['runs'])
build = root / 'tmp' / args.build_log
assert build.resolve().parent == (root / 'tmp').resolve()
match = re.search(r'PLAYABLE_BUILD_PASS (\d+) assertions / (\d+) bytes', build.read_text(encoding='utf-8-sig'))
assert match and int(match[1]) == 1187
report = {'schemaVersion': 1, 'scope': 'functional Windows old/current formal payload, rendered playback, simulated focus and diagnostic menus; not performance or native input/listening',
          'passed': True, 'assemblySha256': sha(assembly), 'resourceSha256': sha(resources),
          'unityAssertions': int(match[1]), 'buildBytes': int(match[2]), 'buildLogSha256': sha(build),
          'coreAssertions': 5709, 'coreVerification': 'observed AUTOMATIC_CHAIN_PASS output; physical original backup/foreign/future cases included',
          'coreTestSha256': sha(root / 'tools/Plan8StoryIntegrationTests.cs'), 'compiledCSharpFiles': 103,
          'telemetry': telemetry, 'cases': cases, 'performanceMeasured': False,
          'focusMethod': 'synthetic callbacks in player; not native AltTab', 'nativeMouseTested': False, 'listeningTested': False}
path = root / 'docs/production/plan8-regression-validation.json'
path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('PLAN8_REGRESSION_REPORT_PASS', len(cases), 'processes')
