"""Record authoring prerequisites; never declare production acceptance."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8-sig'))
    combat = json.loads((ROOT / index['combatSource']).read_text(encoding='utf-8-sig'))
    story = json.loads((ROOT / index['storySource']).read_text(encoding='utf-8-sig'))
    forms = combat['heroines']
    ids = [form['id'] for form in forms]
    if len(ids) != len(set(ids)):
        raise ValueError('Duplicate runtime form IDs')
    expected = {}
    for form in forms:
        expected.setdefault(form.get('personId', form['id']), set()).add(form['id'])
    actual = {p['personId']: set(p['formIds']) for p in index['people']}
    if len(actual) != len(index['people']) or actual != expected:
        raise ValueError('Character documentation coverage differs from runtime')
    for form_id in ids:
        if not any(c.get('ownerId') == form_id for c in story['chapters']):
            raise ValueError(f'Missing story chapters: {form_id}')
    template_path = 'docs/production/plan12-form-record-template.json'
    template = json.loads((ROOT / template_path).read_text(encoding='utf-8'))
    if template['personId'] is not None or template['formId'] is not None:
        raise ValueError('Template must not reserve a product ID')
    if len(template['images']) != 21 or len(set(template['images'])) != 21:
        raise ValueError('Template requires 21 distinct image roles')
    required_checks = {'player_default_launch', 'job_commands_resources_and_states',
                       'pre_addition_save_compatibility', 'real_asset_size_loading_memory'}
    if not required_checks.issubset(template['checks']) or 'player_1080p_720p' in template['checks']:
        raise ValueError('Template must use current implementation and default-launch checks')
    if template['acceptancePolicy']['resolution'] != 'default launch only':
        raise ValueError('Visual acceptance must use default launch resolution only')
    paths = [
        'docs/characters/index.json', index['combatSource'], index['storySource'],
        'docs/production/heroine-addition-guide.md',
        'docs/production/job-implementation-guide.md',
        'docs/production/plan11-10-job-panels.txt',
        'docs/production/plan11-10-job-panels-validation.json',
        'docs/production/plan12-production-handoff.txt',
        'docs/production/plan12-preparation.txt', template_path,
        'docs/production/navigation-route-validation.json',
        'docs/production/commercial-quality-fixes-validation.json',
        'docs/production/commercial-audio-quality-2026-10-10-validation.json',
        'tools/validate-character-docs.py', 'tools/validate-automatic-chain.ps1',
        'tools/compile-unity-scripts.ps1', 'tools/validate-plan11-9.ps1',
        'tools/audit-plan12-existing-art.py', 'tools/validate-plan12-art-review-player.ps1',
        'docs/production/plan12-existing-art-audit.json',
        'docs/production/plan12-art-review-followup.txt',
        'docs/production/plan12-full-review.txt',
        'docs/production/plan12-runtime-art-audit.json',
        'tools/audit-plan12-runtime-art.py', 'tools/validate-plan12-art-sweep.ps1',
        'tools/record-plan12-art-sweep.py',
        'tools/inspect-plan12-expression-alignment.py', 'tools/verify-plan12-cg-gallery.py',
        'docs/production/plan12-event-cg-display.txt',
        'docs/production/plan12-event-cg-content-review.json',
        'docs/production/plan12-full-review-validation.json',
    ]
    evidence = [{'path': p, 'sha256': hashlib.sha256((ROOT / p).read_bytes()).hexdigest()}
                for p in paths]
    result = {
        'schemaVersion': 1, 'scope': 'authoring prerequisites only',
        'people': len(expected), 'forms': len(ids),
        'jobs': len({f['jobId'] for f in forms}),
        'preparationCheck': 'passed', 'firstLot': [],
        'productionAcceptance': 'not_evaluated',
        'remaining': ['first lot selection', 'first added form end-to-end acceptance',
                      'real asset growth and pre-addition save compatibility',
                      'final listening review', 'minimum-spec endurance review',
                      'resolve any Critical/High findings'],
        'evidence': evidence,
    }
    output = ROOT / 'docs/production/plan12-preparation-validation.json'
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN12_PREPARATION_PASS people={len(expected)} forms={len(ids)}; acceptance=not_evaluated')


if __name__ == '__main__':
    main()
