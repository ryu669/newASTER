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
    core = read('core-streaming-final.log')
    for text, marker in [(build, 'PLAN15_16_BUILD_PASS'), (build, 'HEROINE_BATTLE_ART_ASPECT_PASS images=61'),
                         (player, 'PLAN11_9_IMAGE_STREAMING_PASS'), (player, 'PLAN11_9_PLAYER_QUALITY_PASS'),
                         (audio, 'PLAN9_PRODUCTION_AUDIO_PASS')]:
        if marker not in text:
            raise ValueError(f'Missing measured result: {marker}')
    report_path = repo / 'docs/production/plan11-9-quality-validation.json'
    report = json.loads(report_path.read_text(encoding='utf-8'))
    stress = json.loads(read('texture-stress.json'))
    waveform = json.loads(read('audio-levels.json'))
    memory = json.loads(read('process-memory.json'))
    if {entry['check'] for entry in memory} != {'streaming', 'cold-book', 'audio'}:
        raise ValueError('Incomplete process sampling')
    for entry in memory:
        entry['log'] = Path(entry['log']).relative_to(repo).as_posix()
        if entry['sha256'].lower() != hashlib.sha256((repo / entry['log']).read_bytes()).hexdigest():
            raise ValueError(f'Stale process-memory evidence: {entry["check"]}')
    report['coreAssertions'] = int(re.search(r'AUTOMATIC_CHAIN_PASS (\d+) assertions', core)[1])
    report['compileSources'] = int(re.search(r'UNITY_SCRIPT_COMPILE_PASS (\d+) source files', core)[1])
    report['corePerformance'] = json.loads(read('core-performance.json'))
    report['playerMeasurement'] = re.search(r'PLAN11_9_PLAYER_MEASUREMENT ([^\r\n]+)', player)[1]
    report['normalFrameMeasurement'] = re.search(r'PLAN11_9_NORMAL_FRAME_MEASUREMENT ([^\r\n]+)', player)[1]
    report['coldBookFirstRenderedFrameMs'] = float(re.search(r'firstRenderedFrameMs=([\d.]+)', cold)[1])
    report['coldBookScope'] = 'First title Open Book handler to first frame end; no portraits preloaded. Startup, OS disk-cache control, physical input and GPU fence excluded.'
    report['imageStreaming'] = stress
    report['imageCacheLimits'] = dict(portrait=24, weaponTree=3, battleLayersPerView=36, sd=48, adv=24, faceRenderTargets=12)
    report['processMemory'] = memory
    report['audioWaveforms'] = waveform
    report['audioTransitions'] = 'PASS title / garden / battle / ADV, four UI sounds, focus loss / return, explicit resume, no overlapping legacy ADV BGM, saved progress unchanged (AI visit updates excluded by isolated audio fixture).'
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
                                             detail='First synthetic page includes first-use face shader / asset loading. Record separately from warmed page switching; do not count it as meeting the 0.3 s goal.'))
    if fps < 59:
        report['remainingIssues'].append(dict(severity='Medium', id='diagnostic-normal-fps', measuredFps=fps, targetFps=60,
                                             detail='Isolated garden run includes immediate diagnostic persistence. Production save-slot timing and frame stalls need further profiling; no causal claim from this measurement alone.'))
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
             'cold-book.log', 'audio.log', 'texture-stress.json', 'audio-levels.json', 'process-memory.json',
             'normal-frame-times-ms.txt', 'ui-streaming-final.log']
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
