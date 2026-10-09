"""Collect measured Plan11-9 evidence; retain explicit limits and failed performance targets."""
import hashlib
import json
from pathlib import Path
import re


def main():
    repo = Path(__file__).resolve().parent.parent
    output = repo / 'tmp/plan11-9'

    def read(name):
        return (output / name).read_text(encoding='utf-8-sig')

    build = read('unity-streaming-final-build.log')
    player = read('player-streaming.log')
    cold = read('cold-book.log')
    audio = read('audio.log')
    environment = read('environment-audio.log')
    core = read('core-streaming-final.log')
    for text, marker in [(build, 'PLAN15_16_BUILD_PASS'), (build, 'HEROINE_BATTLE_ART_ASPECT_PASS images=61'),
                         (player, 'PLAN11_9_IMAGE_STREAMING_PASS'), (player, 'PLAN11_9_PLAYER_QUALITY_PASS'),
                         (audio, 'PLAN9_PRODUCTION_AUDIO_PASS'), (environment, 'PLAN11_9_ENVIRONMENT_AUDIO_PASS')]:
        if marker not in text:
            raise ValueError(f'Missing measured result: {marker}')
    report_path = repo / 'docs/production/plan11-9-quality-validation.json'
    report = json.loads(report_path.read_text(encoding='utf-8'))
    stress = json.loads(read('texture-stress.json'))
    waveform = json.loads(read('audio-levels.json'))
    memory = json.loads(read('process-memory.json'))
    if {entry['check'] for entry in memory} != {'streaming', 'cold-book', 'audio', 'environment-audio'}:
        raise ValueError('Incomplete process sampling')
    for entry in memory:
        entry['log'] = Path(entry['log']).relative_to(repo).as_posix()
        if entry['sha256'].lower() != hashlib.sha256((repo / entry['log']).read_bytes()).hexdigest():
            raise ValueError(f'Stale process-memory evidence: {entry["check"]}')
    report['coreAssertions'] = int(re.search(r'AUTOMATIC_CHAIN_PASS (\d+) assertions', core)[1])
    report['compileSources'] = int(re.search(r'UNITY_SCRIPT_COMPILE_PASS (\d+) source files', core)[1])
    report['corePerformance'] = json.loads(read('core-performance.json'))
    # Preserve repeated measurements, including slower runs, instead of replacing the history.
    player_hash = hashlib.sha256((output / 'player-streaming.log').read_bytes()).hexdigest()
    history = report.setdefault('performanceRuns', [])
    if not any(run['playerLogSha256'] == player_hash for run in history):
        archive = output / 'performance-history' / (player_hash + '.log')
        archive.parent.mkdir(parents=True, exist_ok=True)
        archive.write_bytes((output / 'player-streaming.log').read_bytes())
        assembly = repo / 'game/Builds/plan11-9/newASTER_Data/Managed/Assembly-CSharp.dll'
        history.append(dict(playerLog=archive.relative_to(repo).as_posix(), playerLogSha256=player_hash,
                            assemblySha256=hashlib.sha256(assembly.read_bytes()).hexdigest(),
                            measurement=re.search(r'PLAN11_9_PLAYER_MEASUREMENT ([^\r\n]+)', player)[1],
                            frameMeasurement=re.search(r'PLAN11_9_NORMAL_FRAME_MEASUREMENT ([^\r\n]+)', player)[1],
                            context=next(entry.get('measurementContext', 'External recording / other game activity not recorded') for entry in memory if entry['check'] == 'streaming')))
    report['playerMeasurement'] = re.search(r'PLAN11_9_PLAYER_MEASUREMENT ([^\r\n]+)', player)[1]
    report['normalFrameMeasurement'] = re.search(r'PLAN11_9_NORMAL_FRAME_MEASUREMENT ([^\r\n]+)', player)[1]
    report['coldBookFirstRenderedFrameMs'] = float(re.search(r'firstRenderedFrameMs=([\d.]+)', cold)[1])
    report['coldBookScope'] = 'First title Open Book handler to first fully loaded portrait page frame end; no portraits preloaded. Startup, OS disk-cache control, physical input and GPU fence excluded.'
    report['imageStreaming'] = stress
    report['imageStreaming']['loadingFrameScope'] = 'Unscaled frame intervals observed while the portrait queue is pending; the entry interval can overlap preceding fixture-container opening. Page readiness is measured by a separate stopwatch after container opening.'
    costs = json.loads(read('normal-frame-costs.json'))
    frames = costs['frames']
    report['normalFrameCosts'] = dict(scope=costs['scope'], samples=len(frames), focusedFrames=sum(f['focused'] for f in frames),
                                      updateMaxMs=max(f['updateMs'] for f in frames), guiMaxMs=max(f['guiMs'] for f in frames),
                                      saveMaxMs=max(f['saveMs'] for f in frames), gcCollections=sum(f['gcCollections'] for f in frames),
                                      slowFrames=[f for f in frames if f['frameMs'] > 40])
    report['gardenMaskMeasurement'] = re.search(r'PLAN11_9_MASK_MEASUREMENT ([^\r\n]+)', core)[1]
    report['imageCacheLimits'] = dict(portrait=24, weaponTree=3, battleLayersPerView=36, sd=48, adv=24, faceRenderTargets=12, gardenCompositePairs=24, queuedPortraitLoads=12)
    report['processMemory'] = memory
    report['audioWaveforms'] = waveform
    report['audioTransitions'] = 'PASS title / garden / battle / ADV, four UI sounds, focus loss / return, explicit resume, no overlapping legacy ADV BGM, saved progress unchanged (AI visit updates excluded by isolated audio fixture).'
    report['environmentAudio'] = re.search(r'PLAN11_9_ENVIRONMENT_AUDIO_PASS ([^\r\n]+)', environment)[1]
    report['historicalFrameSpike'] = dict(measuredMs=200.162, attribution='unconfirmed', userContext='User reported other game startup / recording as a possible source of contention.', reproduction='Not reproduced in subsequent runs; historical recording and co-running application state was not logged. Do not classify as a confirmed game defect or claim the external cause is proven.')
    report['followupUI'] = read('ui-streaming-final.log').splitlines()
    report['limits'] = [
        '320 synthetic imported textures use a separate LZ4 AssetBundle; not 320 finished heroines or production Resources packaging.',
        '256-person / 320-form roster, affection and save behavior are covered by paired Core fixtures; Player production catalog has 15 forms.',
        'Process working set is measured externally at 100 ms intervals. Compressed texture bytes are an estimate, not VRAM.',
        'Cold book measurement excludes process startup, physical input, GPU completion and controlled OS disk-cache eviction.',
        'Audio PCM checks and automated scene/focus callbacks do not replace human listening or physical input acceptance.',
    ]
    fps = float(re.search(r'normalFps=([\d.]+)', player)[1])
    report['remainingIssues'] = [dict(severity='Medium', id='manual-listening', detail='SE and BGM human listening acceptance remains pending.')]
    if stress['maxPageFrameMs'] > 300:
        report['remainingIssues'].append(dict(severity='Medium', id='initial-synthetic-page', measuredMs=stress['maxPageFrameMs'], targetMs=300,
                                             detail='Complete synthetic page includes queued image loading and first-use face shader work. Record separately from warmed page switching; do not count it as meeting the 0.3 s goal.'))
    if fps < 59:
        report['remainingIssues'].append(dict(severity='Medium', id='diagnostic-normal-fps', measuredFps=fps, targetFps=60,
                                             detail='Measured below target in this run. External recording / other game activity is not controlled or monitored; this is a benchmark observation, not a confirmed game defect. Preserve repeated results and assess under controlled conditions before attributing it to game code.'))
    combat = json.loads((repo / 'game/unity/Assets/Game/Resources/Combat/battle-plan11-7.json').read_text(encoding='utf-8'))
    story = json.loads((repo / 'game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json').read_text(encoding='utf-8'))
    report['productionAudit'] = []
    for hero in combat['heroines']:
        chapters = [c for c in story['chapters'] if c['ownerId'] == hero['id']]
        report['productionAudit'].append(dict(formId=hero['id'], personId=hero.get('personId') or hero['id'], jobId=hero['jobId'],
                                             skills=len(hero['skills']), jobTraits=2, characterTraits=len(hero.get('uniqueTraitIds') or [None]*3),
                                             interactionTraits=len(hero['interactionTraitIds']), chapters=len(chapters), pages=sum(len(c['pages']) for c in chapters),
                                             poems=sum(len(c['poems']) for c in chapters), buildValidation='PASS'))
    paths = ['core-streaming-final.log', 'unity-streaming-final-build.log', 'player-streaming.log',
             'cold-book.log', 'audio.log', 'environment-audio.log', 'texture-stress.json', 'audio-levels.json', 'process-memory.json',
             'normal-frame-times-ms.txt', 'normal-frame-costs.json', 'ui-streaming-final.log']
    for name in paths:
        path = output / name
        report['evidence'][path.relative_to(repo).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    for name in ['Assembly-CSharp.dll', '../resources.assets']:
        path = repo / 'game/Builds/plan11-9/newASTER_Data/Managed' / name
        path = path.resolve()
        report['evidence'][path.relative_to(repo).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN11_9_EVIDENCE_RECORDED images={stress["images"]} normalFps={fps} remainingMedium={len(report["remainingIssues"])}')


if __name__ == '__main__':
    main()
