import json, hashlib
from pathlib import Path

root=Path(__file__).resolve().parent.parent
work=root/'tmp/plan8-battle-measurement'
def read(name):
    return json.loads((work/name).read_text(encoding='utf-8-sig'))
baseline=read('full-damage--1.json')
adopted=read('full-adopted.json')
collection=read('collection.json')
for matrix in (baseline,adopted):
    assert matrix['totalRuns']==1500 and len(matrix['rows'])==75 and len(matrix['runs'])==1500
    keys={(r['heroLevel'],r['enemyLevel'],r['policy'],r['seed']) for r in matrix['runs']}
    assert len(keys)==1500
    assert all(r['outcome'] in ('victory','defeat','step-cap') and len(r['inputSha256'])==64 for r in matrix['runs'])
assert baseline['definition']['damagePerLevel']==2 and baseline['definition']['hpPerLevel']==120
assert adopted['definition']['contentVersion']=='colossus-plan8-2026-10-04'
assert adopted['definition']['damagePerLevel']==8 and adopted['definition']['hpPerLevel']==600
assert all(r['censored']==0 for r in adopted['rows'])
assert all(r['wins']==20 for r in adopted['rows'] if r['heroLevel']==1 and r['enemyLevel']==1)
assert all(r['wins']==0 for r in adopted['rows'] if r['heroLevel']==1 and r['enemyLevel']==50)
assert all(r['wins']==20 for r in adopted['rows'] if r['heroLevel']>=30 and r['enemyLevel']==50)
assert len(collection['results'])==80 and all(r['censored']==0 for r in collection['summary'])
comparisons=[]
for name in ('pilot-damage-8.json','pilot-damage-12.json','pilot-damage-24.json','pilot-damage-40.json','pilot-damage-2-hp-600.json','pilot-damage-8-hp-600.json'):
    data=read(name)
    comparisons.append({'file':name,'sha256':hashlib.sha256((work/name).read_bytes()).hexdigest(),'definition':data['definition'],'rows':data['rows']})
report={'schemaVersion':1,'date':'2026-10-04','scope':'8-4/8-5 deterministic functional simulations; not performance, human duration or listening acceptance','difficultyCriteriaSetAfterBaselineBeforeAdoption':{'hero1Enemy1':'20/20 wins in all three policies','hero1Enemy50':'at most 5/20 wins in all three policies','hero30AndAboveEnemy50':'at least 16/20 wins in all three policies','stepCaps':'none in adopted full matrix'},'appliesTo':'Plan8 original story trial only; ordinary baseline and existing receipts retained','baseline':baseline,'adopted':adopted,'pilots':comparisons,'collection':collection,'realTimeMedianSeconds':None,'realTimeP90Seconds':None,'performanceMeasured':False,'humanPlaytest':False}
path=root/'docs/production/plan8-balance-measurements.json'
path.write_text(json.dumps(report,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
print('PLAN8_ADJUSTMENT_REPORT_PASS',path,'bytes='+str(path.stat().st_size))
