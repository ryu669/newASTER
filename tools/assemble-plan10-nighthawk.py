import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer','heroine.nighthawk')
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-oriflamme.json')
c['jobs'].append(dict(id='job.chaser',resourceName='ギア',resourceMax=15,initialResource=6,gainAtReady=0,gainOnAttack=0,gainOnHit=0,hp=340,attack=85,defense=95,magicDefense=100,speed=145,criticalBp=2000))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='ナイトホーク',jobId='job.chaser',personId='heroine.nighthawk',variantId='normal',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,traitHpPercent=0,traitAttackPercent=5);c['heroines'].append(h)
for slot,(name,power,wait) in enumerate([('つめたいほしあかり',0,75),('そらをめぐる燐の火',1.7,100),('カシオピアと天の川',1.4,125)],1):
 s=dict(id=f'heroine.nighthawk.{slot}',sourceSkillId=f'heroine.nighthawk.{slot}',ownerId='heroine.nighthawk',slot=slot,name=name,sourceFile='066戦闘_ナイトホーク.mkv',sourceSecond=15,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='physical',attributes=['打撃','火','氷'],ignoreDefenseBp=0)
 if slot==1:s.update(effectRuleId='effect.self-buff',targetRuleId='target.self',damageType='',attributes=[],damageCap=0,chainEligible=False,selfEffects=[dict(kind='attack',percent=20,turns=6),dict(kind='speed',percent=20,turns=6)])
 if slot==2:s.update(ignoreDefenseBp=10000,statusEffects=[dict(kind='frostbite',amount=120)],targetStatusBonusKind='frostbite',targetStatusBonusPercent=60)
 if slot==3:s.update(targetRuleId='target.all-enemies',statusEffects=[dict(kind='burn',amount=60),dict(kind='frostbite',amount=60)],chainChanceBonusBp=1500)
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=[],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']=='heroine.nighthawk':x['status']='implemented'
write(RES/'Combat/battle-plan10-nighthawk.json',c)
write(RES/'Illustrations/nighthawk-battle-binding.json',dict(heroineId='heroine.nighthawk',placeholder=False,fullCanvas=True,resourcePath='Illustrations/nighthawk-standing-candidate-v1',attackResourcePath='Illustrations/nighthawk-attack-candidate-v1',hitResourcePath='Illustrations/nighthawk-hit-candidate-v1',cutinResourcePath='Illustrations/nighthawk-cutin-candidate-v1'))
story=read(RES/'Story/plan10-oriflamme-story-content.json');extra=read(ROOT/'docs/production/plan10-nighthawk-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/'Story/plan10-nighthawk-story-content.json',story)
print('Eleven forms / ten people / ten jobs.')
