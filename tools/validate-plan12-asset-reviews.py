"""Reject unreviewed, stale, or semantically incorrect batch art before adoption.
Approval is a recorded visual review, never inferred from a file or a hash.
"""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RECORD=ROOT/'docs/production/plan12-first-batch-asset-review.json'

def errors(record,root=ROOT,heroes=('iron-maiden','avalanche','anathema','shiva')):
    problems=[]
    for hero in heroes:
        form=next(f for f in record['forms'] if f['key']==hero)
        if len(form['assets'])!=21 or len({a['key'] for a in form['assets']})!=21:problems.append(hero+': expected 21 distinct asset review records')
        for asset in form['assets']:
            label=hero+'/'+asset['key']
            path=root/asset['path']
            if not path.exists():problems.append(label+': missing source');continue
            if hashlib.sha256(path.read_bytes()).hexdigest()!=asset['sha256']:problems.append(label+': source changed; review required')
            if asset['purposeReview']['status']!='approved':problems.append(label+': '+asset['purposeReview']['status'])
            if asset['key']=='equipment-tree-v1':
                subject=asset['purposeReview'].get('subject')
                if subject=='layered-branching-tree-without-people':
                    bundle=json.loads(path.read_text(encoding='utf-8'))
                    if bundle.get('heroineId')!='heroine.'+hero:problems.append(label+': bundle identity mismatch')
                    components=bundle.get('components',[])
                    layout_path=root/bundle['layoutPath']
                    layout=json.loads(layout_path.read_text(encoding='utf-8'))
                    required={bundle['layoutPath']}|{'game/unity/Assets/Game/Resources/UI/WeaponTrees/'+k+'.png' for k in ['common-tree',layout['foliageAsset'],layout.get('flowerAsset','crimson-flower')]+[p['asset'] for p in layout['decorations']]}
                    if {c['path'] for c in components}!=required:problems.append(label+': incomplete layered bundle')
                    for c in components:
                        p=root/c['path']
                        if c.get('status')!='approved' or not p.exists() or digest(p)!=c['sha256']:problems.append(label+': unreviewed or changed component '+c['path'])
                elif subject!='branching-tree-without-people':problems.append(label+': must depict a branching tree without people')
    return problems

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def validate():
    problems=errors(json.loads(RECORD.read_text(encoding='utf-8')))
    if problems:raise RuntimeError('Asset adoption blocked:\n'+'\n'.join(problems))
    print('FIRST_BATCH_ASSET_PURPOSE_REVIEW_PASS 84 assets; current source hashes')

if __name__=='__main__':validate()
