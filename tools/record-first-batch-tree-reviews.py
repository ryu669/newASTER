"""Record the 2026-10-11 visual review, retaining rejected tree history.
Run only after visually reviewing the source sheets and generated ornaments.
Runtime approval is deliberately separate and is never inferred here.
"""
import copy,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
record_path=ROOT/'docs/production/plan12-first-batch-asset-review.json'
record=json.loads(record_path.read_text(encoding='utf-8'))
for form in record['forms']:
    hero=form['key']
    if hero=='aeon':continue
    for asset in form['assets']:
        if asset['key']=='equipment-tree-v1':
            path='game/unity/Assets/Game/Resources/UI/WeaponTrees/'+hero+'.json'
            layout=json.loads((ROOT/path).read_text(encoding='utf-8'))
            names={'common-tree',layout['foliageAsset'],layout.get('flowerAsset','crimson-flower')}|{p['asset'] for p in layout['decorations']}
            paths=[path]+['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+n+'.png' for n in sorted(names)]
            bundle={'schemaVersion':1,'heroineId':'heroine.'+hero,'layoutPath':path,'components':[{'path':p,'sha256':digest(ROOT/p),'status':'approved'} for p in paths]}
            bp=ROOT/'docs/production'/('plan12-'+hero+'-tree-bundle.json');write(bp,bundle)
            if asset['purposeReview']['status']=='rejected':asset.setdefault('previousVersions',[]).append(copy.deepcopy({k:v for k,v in asset.items() if k!='previousVersions'}))
            asset.update(path=bp.relative_to(ROOT).as_posix(),sha256=digest(bp),purposeReview={'status':'approved','subject':'layered-branching-tree-without-people','reason':'了承済みの自然木・薄い葉群・ノード横の花・専用小装飾を採用。根は葉と花の対象外。構成画像は原画を視認、各部品と配置をハッシュ固定。','reviewedAt':'2026-10-11'},runtimeReview={'status':'pending','evidence':[]})
        elif asset['purposeReview']['status']=='pending':
            asset['purposeReview']={'status':'approved','subject':None,'reason':'2026-10-11に原画の比較シートを視認。CGは男性なし、表情差分は同一人物・衣装・姿勢、SDは指定の独立姿勢を確認。','reviewedAt':'2026-10-11'}
write(record_path,record)
print('FIRST_BATCH_PURPOSE_REVIEWS_RECORDED; rejected images retained in history; runtime remains pending')
