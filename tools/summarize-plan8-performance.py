"""Preserve measured failures and distinguish process restart from OS-cold startup."""
import argparse
import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--battle', action='append', default=[])
parser.add_argument('--home', action='append', default=[])
parser.add_argument('--deferred', action='append', default=[])
parser.add_argument('--rejected', action='append', default=[])
parser.add_argument('--readiness-policy-log', required=True)
args = parser.parse_args()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def directory(name):
    path = root / 'tmp' / name
    assert path.resolve().parent == (root / 'tmp').resolve()
    return path

def environment(path):
    raw = read(path)
    # Keep load and counter availability without publishing application identities.
    return {'sourceSha256': sha(path), 'timeUtc': raw['timeUtc'], 'clear': raw['clear'],
            'checkMode': raw.get('checkMode', 'full-three-sample'),
            'fullClearAtUtc': raw.get('fullClearAtUtc'), 'fullCheckSha256': raw.get('fullCheckSha256'),
            'systemCpuPercent': raw['systemCpuPercent'], 'gpuCountersAvailable': raw['gpuCountersAvailable'],
            'availableMemoryMiB': raw['availableMemoryMiB'],
            'gpuMaxPercent': max((g['percent'] for g in raw['gpu']), default=0),
            'otherAppsClosed': False}

battles, homes, deferred, rejected = [], [], [], []
for name in args.battle:
    path = directory(name)
    for joined_path in sorted(path.glob('*-plan8-measurement.json')):
        joined = read(joined_path)
        prefix = joined_path.name.removesuffix('-plan8-measurement.json')
        env = environment(path / joined['environmentBefore'])
        assert env['clear']
        telemetry = read(path / 'plan8-telemetry-result.json')
        assert telemetry['normalSaveUnchanged'] and telemetry['performanceMeasured']
        assert telemetry['runId'] == joined['runId'] and telemetry['buildHash'] == joined['buildHash']
        assert telemetry['resourceHash'] == joined['resourceHash']
        assert telemetry['frameBudgetPassed'] == joined['frameBudgetPassed'] == (joined['under16_7ms'] >= .95)
        log = (path / (prefix + '.log')).read_text(encoding='utf-8-sig')
        assert 'PLAN7_FULL_COMBAT_PASS' in log and 'Exception:' not in log
        line = next(line for line in joined['performance'] if line.startswith('PLAN7_PERFORMANCE '))
        metrics = {key: float(re.search(r'\b' + key + r'=([0-9.]+)', line)[1])
                   for key in ('frames', 'meanMs', 'p95Ms', 'p99Ms', 'maxMs', 'under16_7ms', 'resourceValidationSeconds')}
        before = path / 'normal-save-before.json'
        after = path / 'normal-save-after.json'
        assert before.read_bytes() == after.read_bytes()
        battles.append({'source': name, 'sourceSha256': sha(joined_path), 'logSha256': sha(path / (prefix + '.log')),
                        'height': joined['memory']['height'], 'runId': joined['runId'], 'profile': joined['profile'],
                        'buildHash': joined['buildHash'], 'resourceHash': joined['resourceHash'],
                        'result': 'passed' if joined['frameBudgetPassed'] else 'failed',
                        'environment': env, 'metrics': metrics, 'memory': joined['memory'],
                        'effects': [line.strip() for line in joined['performance'] if line.startswith('PLAN7_EFFECT_PERFORMANCE ')],
                        'guiCpu': [line for line in log.splitlines() if line.startswith('PLAN8_GUI_CPU ')],
                        'firstRepaint': [line for line in log.splitlines() if line.startswith('PLAN8_FIRST_REPAINT ')],
                        'normalSaveUnchanged': True, 'renderedEquivalencePassed': True,
                        'sameRunTelemetryJoinPassed': True, 'instrumentationEnabled': True,
                        'normalVsync': 1, 'playbackSpeed': 1,
                        'startupSecondsFieldMeaning': 'time at capture; not startup latency'})
for name in args.home:
    path = directory(name)
    for measurement_path in sorted(path.glob('*-measurement.json')):
        item = read(measurement_path)
        prefix = measurement_path.name.removesuffix('-measurement.json')
        env = environment(path / (prefix + '-environment-before.json'))
        assert env['clear'] and item['peakWorkingBytes'] > 0
        assert not item['humanInput'] and not item['coldOsCacheMeasured']
        log_path = path / (prefix + '.log')
        log = log_path.read_text(encoding='utf-8-sig')
        assert 'PLAN6_HOME_PLAYER_PASS' in log and 'Exception:' not in log
        homes.append({'source': name, 'sourceSha256': sha(measurement_path), 'logSha256': sha(log_path),
                      'environment': env, **item})
for name in args.deferred:
    path = directory(name)
    for env_path in sorted(path.glob('*environment-*.json')):
        item = environment(env_path)
        if not item['clear']:
            deferred.append({'source': name, 'file': env_path.name, 'benchmarkLaunchedOnThisAttempt': False, **item})

assert battles
for name in args.rejected:
    path = directory(name)
    logs = list(path.glob('activecombat-*.log'))
    assert len(logs) == 1 and 'PLAN7_FOCUS_FAIL fully rendered battle matches four playback modes including rewards' in logs[0].read_text(encoding='utf-8-sig')
    rejected.append({'source': name, 'logSha256': sha(logs[0]), 'performanceAdopted': False,
                     'reason': 'Diagnostic active-time flush mutated comparison save during prolonged playback.',
                     'fix': 'Disable automatic engagement ticking in captures; preserve normal trial and explicit clock injection.'})
data = root / 'game/Builds/playable/newASTER_Data'
policy_log = root / 'tmp' / args.readiness_policy_log
assert policy_log.resolve().parent == (root / 'tmp').resolve()
assert 'PLAN8_READINESS_REUSE_POLICY_PASS' in policy_log.read_text(encoding='utf-8-sig')
report = {'schemaVersion': 1, 'scope': 'normal synchronized diagnostic player; measured failures retained',
          'currentBuildHash': sha(data / 'Managed/Assembly-CSharp.dll'), 'currentResourceHash': sha(data / 'resources.assets'),
          'hardware': {'cpu': 'Intel Core i7-13700F', 'gpu': 'NVIDIA GeForce RTX 4070'},
          'frameGoal': {'height': 1080, 'maxMilliseconds': 16.7, 'minimumFraction': .95},
          'sameRunTelemetryJoinPassed': True,
          'frameGoalPassed': any(b['height'] == 1080 and b['result'] == 'passed' and b['buildHash'] == sha(data / 'Managed/Assembly-CSharp.dll') for b in battles),
          'coldOsCacheMeasured': False, 'humanInputMeasured': False, 'otherAppsClosed': False,
          'readinessReusePolicy': {'passed': True, 'logSha256': sha(policy_log),
                                  'sourceSha256': hashlib.sha256((root / 'tools/check-plan8-measurement-readiness.ps1').read_text(encoding='utf-8-sig').encode('utf-8')).hexdigest().upper(),
                                  'sourceHashNormalization': 'UTF-8 without BOM; LF newlines',
                                  'methods': ['UTC DateTime and ISO string age', '120s/600s boundaries', 'expired/future/failed/invalid cache'],
                                  'automaticRetries': 0, 'maximumGapSeconds': 120, 'maximumBatchSeconds': 600},
          'battleRuns': battles, 'homeRuns': homes, 'deferredAttempts': deferred, 'rejectedDiagnostics': rejected,
          'limitations': ['GUI CPU excludes Update, GPU work and synchronization waits.',
                          'Home preparation includes diagnostic navigation; assets are prevalidated before first repaint.',
                          'Process-to-first-repaint observation includes 250ms polling and log flush, and is not first displayed pixel latency.',
                          'Process restarts retain uncontrolled OS cache; no reboot or cache purge was performed.',
                          'RGBA byte estimate is not measured GPU VRAM.',
                          'External peak working set covers each process, including diagnostic setup and asset prevalidation.']}
output = root / 'docs/production/plan8-performance-validation.json'
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('PLAN8_PERFORMANCE_REPORT', len(battles), 'battle runs;', len(homes), 'home runs; frameGoalPassed=', report['frameGoalPassed'])
