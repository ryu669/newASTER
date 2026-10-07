import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer','heroine.oriflamme')
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-shell.json')
c['jobs'].append(dict(id='job.alchemist',resourceName='錬成',resourceMax=15,initialResource=5,gainAtReady=1,gainOnAttack=0,gainOnHit=0,hp=360,attack=100,defense=95,magicDefense=100,speed=110,criticalBp=1800))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='オリフラム',jobId='job.alchemist',personId='heroine.oriflamme',variantId='normal',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,traitHpPercent=0,traitAttackPercent=5);c['heroines'].append(h)
for slot,(name,power,wait,attrs) in enumerate([('インシネレート',1.2,75,['火']),('浄火',1,100,['火']),('アナイアレイション',2.8,150,['斬撃','火','闇'])],1):
 s=dict(id=f'heroine.oriflamme.{slot}',sourceSkillId=f'heroine.oriflamme.{slot}',ownerId='heroine.oriflamme',slot=slot,name=name,sourceFile='032戦闘_オリフラム.mkv',sourceSecond=5,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='magic',attributes=attrs,ignoreDefenseBp=0)
 if slot==1:s.update(statusEffects=[dict(kind='burn',amount=60)],enemyFireVulnerabilityPercent=20,enemyFireVulnerabilityTurns=3,resourceGain=2)
 if slot==2:s.update(powerScale=0,damageCap=0,damageType='',effectRuleId='effect.allies-buff',targetRuleId='target.all-living-allies',chainEligible=False,selfEffects=[dict(kind='attack',percent=30,turns=3),dict(kind='fire-amplification',percent=30,turns=3)],allAlchemistResourceGain=3)
 if slot==3:s.update(targetStatusBonusKind='burn',targetStatusBonusPercent=120,enemyFireVulnerabilityPercent=30,enemyFireVulnerabilityTurns=4)
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='magic',attributes=['火'],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']=='heroine.oriflamme':x['status']='implemented'
write(RES/'Combat/battle-plan10-oriflamme.json',c)
write(RES/'Illustrations/oriflamme-battle-binding.json',dict(heroineId='heroine.oriflamme',placeholder=False,fullCanvas=True,resourcePath='Illustrations/oriflamme-standing-candidate-v1',attackResourcePath='Illustrations/oriflamme-attack-candidate-v1',hitResourcePath='Illustrations/oriflamme-hit-candidate-v1',cutinResourcePath='Illustrations/oriflamme-cutin-candidate-v1'))
story=read(RES/'Story/plan10-shell-story-content.json');extra=read(ROOT/'docs/production/plan10-oriflamme-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/'Story/plan10-oriflamme-story-content.json',story)
print('Ten forms / nine people / nine jobs.')
