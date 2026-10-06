import copy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1];RES=R/'game/unity/Assets/Game/Resources';S=RES.parent/'Scripts';key='arcane-academy';hero='heroine.'+key
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer',hero)
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-arcane.json')
c['jobs'].append(dict(id='job.gambler',resourceName='SLOT',resourceMax=0,initialResource=0,gainAtReady=0,gainOnAttack=0,gainOnHit=0,hp=350,attack=82,defense=100,magicDefense=110,speed=135,criticalBp=1750))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='アルケイン（学園）',jobId='job.gambler',personId='heroine.arcane',variantId='academy',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,traitHpPercent=0,traitAttackPercent=5);c['heroines'].append(h)
for slot,(name,power,wait) in enumerate([('フルスイングですわ！',2,100),('レッツ飽和攻撃ですわ！',3.2,125),('ワールドイズマインですわ！',2.4,150)],1):
 s=dict(id=f'{hero}.{slot}',sourceSkillId=f'{hero}.{slot}',ownerId=hero,slot=slot,name=name,sourceFile='013戦闘_アルケイン学園.mkv',sourceSecond=15,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='physical',attributes=['打撃'],ignoreDefenseBp=0)
 if slot==1:s.update(postAttackAlliesEffects=[dict(kind='critical-damage',percent=30,turns=3)])
 if slot==2:s.update(attributes=['斬撃','刺突'],statusEffects=[dict(kind='bleed',amount=120)],enemyWaitAdd=60,enemyStatusExtensionTurns=2,enemyStatusExtensionKinds=['poison','burn','bleed'])
 if slot==3:s.update(targetRuleId='target.all-enemies',attributes=['銃弾','火'],bodyDamageBonusPercent=80,alliesEffectExtensionTurns=1)
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=['打撃'],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']==hero:x['status']='implemented'
write(RES/f'Combat/battle-plan10-{key}.json',c)
write(RES/f'Illustrations/{key}-battle-binding.json',dict(heroineId=hero,placeholder=False,fullCanvas=True,resourcePath=f'Illustrations/{key}-standing-candidate-v1',attackResourcePath=f'Illustrations/{key}-attack-candidate-v1',hitResourcePath=f'Illustrations/{key}-hit-candidate-v1',cutinResourcePath=f'Illustrations/{key}-cutin-candidate-v1'))
story=read(RES/'Story/plan10-arcane-story-content.json');extra=read(R/f'docs/production/plan10-{key}-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/f'Story/plan10-{key}-story-content.json',story)
def edit(path,old,new):
 p=S/path;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf8')
edit(Path('Presentation/PrototypeBootstrap.cs'),'?"Combat/battle-formal":"Combat/battle-plan10-arcane"','?"Combat/battle-formal":"Combat/battle-plan10-arcane-academy"')
edit(Path('Presentation/ProductionStoryExperience.cs'),'combatDefinitions.HeroineIds.Contains("heroine.arcane")?', 'combatDefinitions.HeroineIds.Contains("heroine.arcane-academy")?"Story/plan10-arcane-academy-story-content":combatDefinitions.HeroineIds.Contains("heroine.arcane")?')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"heroine.arcane"};','"heroine.arcane","heroine.arcane-academy"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"帰還の鍵"};','"帰還の鍵","検証の頁","書架の結界","卒業の先の翼"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'8,6,14,8,6,14,9,5,15};','8,6,14,8,6,14,9,5,15,8,7,14};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f};','1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.2f,1.12f,1.36f};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"遺翼"};','"遺翼","書翼"};')
edit(Path('Data/HeroineIdentityCatalog.cs'),'default:throw new ArgumentException','case "heroine.arcane-academy":name="正解を急がない翼";master="次の頁を選ぶ手";icon="star";bonus="魔法防御＋5%";break;\n                default:throw new ArgumentException')
edit(Path('Data/HeroineIdentityCatalog.cs'),'string resource=job=="job.general"?', 'string resource=job=="job.gambler"?"SLOTのみで3スキル抽選。3行＋2対角の全ライン発動。777は各スキル2回、全外れWT0。":job=="job.general"?')
edit(Path('Core/PlayableBattle.cs'),'formationIds[i]=="heroine.arcane"))','formationIds[i]=="heroine.arcane" || formationIds[i]=="heroine.arcane-academy"))')
art=(S/'Data/Plan10ArcaneHomeArt.cs').read_text(encoding='utf8').replace('Plan10ArcaneHomeArt','Plan10ArcaneAcademyHomeArt').replace('"arcane"','"arcane-academy"');(S/'Data/Plan10ArcaneAcademyHomeArt.cs').write_text(art,encoding='utf8')
edit(Path('Data/ProductionStoryCatalog.cs'),'Plan10ArcaneHomeArt.Apply(home);','Plan10ArcaneHomeArt.Apply(home);\n            Plan10ArcaneAcademyHomeArt.Apply(home);')
print('Arcane academy: fourteenth form / eleventh person / twelfth job; shared normal Arcane identity.')
