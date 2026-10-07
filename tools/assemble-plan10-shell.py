"""Append Shell without changing previous eight forms or their story."""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
    if isinstance(v,str):return v.replace('heroine.slayer','heroine.shell')
    if isinstance(v,list):return [owner(x) for x in v]
    if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
    return v
c=read(RES/'Combat/battle-plan10-annihilator.json')
c['jobs'].append(dict(id='job.panzer',resourceName='装甲',resourceMax=0,initialResource=0,gainAtReady=0,gainOnAttack=0,gainOnHit=0,hp=320,attack=42,defense=260,magicDefense=90,speed=95,criticalBp=1000))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='シェル',jobId='job.panzer',personId='heroine.shell',variantId='normal',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,traitHpPercent=5,traitAttackPercent=0);c['heroines'].append(h)
for slot,(name,power,wait,attributes,statuses) in enumerate([
    ('コンバット・フッキング',2,100,[],[dict(kind='bleed',amount=60)]),
    ('スパーキング・シグネチュア',2.2,125,['火'],[dict(kind='burn',amount=100)]),
    ('コンティニュード・インファイト',2.8,150,['火'],[])],1):
    s=dict(id=f'heroine.shell.{slot}',sourceSkillId=f'heroine.shell.{slot}',ownerId='heroine.shell',slot=slot,name=name,sourceFile='048戦闘_シェル.mkv',sourceSecond=5,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=statuses,conditions=[],damageType='physical',attributes=attributes,ignoreDefenseBp=0)
    if slot==3:s.update(targetStatusBonusKind='burn',targetStatusBonusPercent=80)
    c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=[],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
    if x['ownerId']=='heroine.shell':x['status']='implemented'
write(RES/'Combat/battle-plan10-shell.json',c)
write(RES/'Illustrations/shell-battle-binding.json',dict(heroineId='heroine.shell',placeholder=False,fullCanvas=True,resourcePath='Illustrations/shell-standing-candidate-v1',attackResourcePath='Illustrations/shell-attack-candidate-v1',hitResourcePath='Illustrations/shell-hit-candidate-v1',cutinResourcePath='Illustrations/shell-cutin-candidate-v1'))
story=read(RES/'Story/plan10-annihilator-story-content.json');extra=read(ROOT/'docs/production/plan10-shell-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/'Story/plan10-shell-story-content.json',story)
print('Nine forms / eight people / eight jobs; 72 chapters and 45 events.')
