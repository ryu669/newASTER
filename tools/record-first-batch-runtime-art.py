"""Reconcile the manually reviewed Player captures; never infer approval from generation."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def digest(p):return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
path=ROOT/'docs/production/plan12-first-batch-asset-review.json'
review=json.loads(path.read_text(encoding='utf-8'))
for form in review['forms']:
    hero=form['key']
    base=f'tmp/plan12-aeon/player-default' if hero=='aeon' else f'tmp/plan12-batch/player-default/{hero}'
    for asset in form['assets']:
        key=asset['key']
        assert asset['purposeReview']['status']=='approved'
        assert digest(asset['path'])==asset['sha256']
        if key in ('attack','hit','cutin'):capture=f'tmp/plan12-batch/battle-art/{hero}/{key}.png'
        elif key=='equipment-tree-v1':capture=f'{base}/weapon.png' if hero=='aeon' else f'{base}/{hero}-tree.png'
        elif key.startswith('event-'):capture=f'{base}/{hero}-cg.{key.split("-")[1]}.png'
        elif key.startswith('expression-'):capture=f'{base}/{hero}-expression.{key.removeprefix("expression-")}.png'
        elif key.startswith('sd-'):capture=f'{base}/{hero}-sd.{key.removeprefix("sd-")}.png'
        else:capture=f'{base}/{hero}-detail.png' if key=='portrait' else f'{base}/{hero}-battle.png'
        assert (ROOT/capture).is_file(),capture
        asset['runtimeReview']={'status':'approved','reviewedAt':'2026-10-11','reviewedSha256':asset['sha256'],'resolution':'1920x1080 default launch','method':'manual actual Player frame review; physical input excluded','evidence':[capture],'captureFiles':[{'path':capture,'sha256':digest(capture)}]}
    form['status']='art_runtime_approved_form_acceptance_pending'
review['status']='art_runtime_approved_form_acceptance_pending'
path.write_text(json.dumps(review,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('FIRST_BATCH_RUNTIME_ART_RECONCILED 105 approved assets; form publication remains separate')
