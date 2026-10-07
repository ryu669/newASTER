"""Compose two forms of one angel; preserve the six-form R fixture."""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'game/unity/Assets/Game/Resources'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def owner(v,old,new):
    if isinstance(v,str):return v.replace(old,new)
    if isinstance(v,list):return [owner(x,old,new) for x in v]
    if isinstance(v,dict):return {k:owner(x,old,new) for k,x in v.items()}
    return v
c=read(RES/'Combat/battle-plan10.json')
c['jobs'].append(dict(id='job.healer',resourceName='生命力',resourceMax=10,initialResource=6,gainAtReady=1,gainOnAttack=0,gainOnHit=0,hp=300,attack=36,defense=90,magicDefense=210,speed=105,criticalBp=1000))
for holy in [False,True]:
    hid='heroine.annihilator'+('-holy' if holy else '')
    prefix='annihilator'+('-holy' if holy else '')
    h=owner(copy.deepcopy(c['heroines'][0]),'heroine.slayer',hid)
    h.update(name='アナイアレイター'+('（聖夜）' if holy else ''),jobId='job.healer' if holy else 'job.fighter',personId='heroine.annihilator',variantId='holy-night' if holy else 'normal',hpBp=10000 if holy else 10500,attackBp=9500 if holy else 10500,defenseBp=10500 if holy else 9000,speedBp=10000,traitHpPercent=5,traitAttackPercent=0)
    c['heroines'].append(h)
    specs=[('ひとひらの愛',1.3,'effect.heal','target.selected-allies',75),('仲間たちへの贈り物',0,'effect.allies-buff','target.selected-allies',100),('破壊者より哀を込めて',2,'effect.damage','target.all-enemies',150)] if holy else [('撃滅の宣告',0,'effect.self-buff','target.self',75),('紅き地獄蝶の誘い',2,'effect.damage','target.selected-enemy',75),('鬼面新境・曼珠沙華',2.4,'effect.damage','target.selected-enemy',150)]
    for slot,(name,power,effect,target,recovery) in enumerate(specs,1):
        s=dict(id=f'{hid}.{slot}',sourceSkillId=f'{hid}.{slot}',ownerId=hid,slot=slot,name=name,sourceFile=('010戦闘_アナイアレイター(聖夜).mkv' if holy else '009戦闘_アナイアレイター.mkv'),sourceSecond=5,powerScale=power,targetRuleId=target,recoveryPercent=recovery,conditions=[],criticalBonusBp=0,damageType='magic' if holy else 'physical',attributes=['光'] if holy else [],ignoreDefenseBp=0,damageCap=10000 if effect=='effect.damage' else 0,chainEligible=effect=='effect.damage',effectRuleId=effect,selfEffects=[],resourceCost=8 if holy and slot>1 else 0,resourceGain=6 if holy and slot==1 else 4 if not holy and slot==1 else 0,targetCount=1 if target=='target.selected-allies' else 0,baseHealingAmount=0,chargeConsumeMax=0,chargeBonusPercent=0)
        s.update(partScale=1,baseHealing=0,ruleOrigin='video-observation-plus-newaster-original',observedSkillLevel=7)
        if effect!='effect.damage':s['damageType']=None;s['attributes']=[]
        if holy and slot==1:s['cleanseAll']=True
        if holy and slot==2:s['selfEffects']=[dict(kind='attack',percent=40,turns=3),dict(kind='critical',percent=25,turns=3),dict(kind='critical-damage',percent=30,turns=3)]
        if holy and slot==3:s['alliesHealingBaseAttackPercent']=60
        if not holy and slot==1:s['selfHealingBaseAttackPercent']=90;s['selfEffects']=[dict(kind='attack',percent=30,turns=2)]
        if not holy and slot>1:s['chargeConsumeMax']=3 if slot==2 else 5;s['chargeBonusPercent']=20
        if not holy and slot==3:s['selfDamageMaxHpPercent']=10
        c['skills'].append(s)
    chain=owner(copy.deepcopy(c['chainActions'][0]),'heroine.slayer',hid)
    chain.update(powerScale=.65,damageType='magic' if holy else 'physical',attributes=['光'] if holy else [],ignoreDefenseBp=0)
    c['chainActions'].append(chain)
    c['contentReferences'] += [owner(x,'heroine.slayer',hid) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
    for x in c['contentReferences']:
        if x['ownerId']==hid:x['status']='implemented'
    write(RES/f'Illustrations/{prefix}-battle-binding.json',dict(heroineId=hid,placeholder=False,fullCanvas=True,resourcePath=f'Illustrations/{prefix}-standing-candidate-v1',attackResourcePath=f'Illustrations/{prefix}-attack-candidate-v1',hitResourcePath=f'Illustrations/{prefix}-hit-candidate-v1',cutinResourcePath=f'Illustrations/{prefix}-cutin-candidate-v1'))
write(RES/'Combat/battle-plan10-annihilator.json',c)
story=read(RES/'Story/plan10-story-content.json');extra=read(ROOT/'docs/production/plan10-annihilator-story-content.json')
story['chapters']+=extra['chapters'];story['events']+=extra['events']
write(RES/'Story/plan10-annihilator-story-content.json',story)
print('Eight forms, seven people; 69 chapters and 40 events.')
