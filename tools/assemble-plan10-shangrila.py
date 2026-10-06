import copy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1];RES=R/'game/unity/Assets/Game/Resources';S=RES.parent/'Scripts';key='shangrila';hero='heroine.'+key
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer',hero)
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-arcane-academy.json')
c['jobs'].append(dict(id='job.sniper',resourceName='狙撃',resourceMax=15,initialResource=0,gainAtReady=0,gainOnAttack=0,gainOnHit=0,hp=370,attack=108,defense=100,magicDefense=95,speed=90,criticalBp=2500))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='シャングリラ',jobId='job.sniper',personId=hero,variantId='normal',hpBp=10000,attackBp=10500,defenseBp=9500,speedBp=10000,traitHpPercent=0,traitAttackPercent=5);c['heroines'].append(h)
for slot,(name,power,wait) in enumerate([('社畜にも五分の魂',1.78,100),('ハードボイルドショット',1.26,100),('血意のクリティカ魔弾',6.66,150)],1):
 s=dict(id=f'{hero}.{slot}',sourceSkillId=f'{hero}.{slot}',ownerId=hero,slot=slot,name=name,sourceFile='050戦闘_シャングリラ.mkv',sourceSecond=15,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='physical',attributes=['銃弾'],ignoreDefenseBp=0)
 if slot==1:s.update(selfDamageMaxHpPercent=20)
 if slot==2:s.update(statusEffects=[dict(kind='bleed',amount=30)],selfHealingBaseAttackPercent=45)
 if slot==3:s.update(selfDamageMaxHpPercent=40,selfEffects=[dict(kind='attack-reduction',percent=35,turns=2)])
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=['銃弾'],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']==hero:x['status']='implemented'
write(RES/f'Combat/battle-plan10-{key}.json',c)
write(RES/f'Illustrations/{key}-battle-binding.json',dict(heroineId=hero,placeholder=False,fullCanvas=True,resourcePath=f'Illustrations/{key}-standing-candidate-v1',attackResourcePath=f'Illustrations/{key}-attack-candidate-v1',hitResourcePath=f'Illustrations/{key}-hit-candidate-v1',cutinResourcePath=f'Illustrations/{key}-cutin-candidate-v1'))
story=read(RES/'Story/plan10-arcane-academy-story-content.json');extra=read(R/f'docs/production/plan10-{key}-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/f'Story/plan10-{key}-story-content.json',story)
def edit(path,old,new):
 p=S/path;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf8')
edit(Path('Presentation/PrototypeBootstrap.cs'),'?"Combat/battle-formal":"Combat/battle-plan10-arcane-academy"','?"Combat/battle-formal":"Combat/battle-plan10-shangrila"')
edit(Path('Presentation/ProductionStoryExperience.cs'),'combatDefinitions.HeroineIds.Contains("heroine.arcane-academy")?', 'combatDefinitions.HeroineIds.Contains("heroine.shangrila")?"Story/plan10-shangrila-story-content":combatDefinitions.HeroineIds.Contains("heroine.arcane-academy")?')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"heroine.arcane-academy"};','"heroine.arcane-academy","heroine.shangrila"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"卒業の先の翼"};','"卒業の先の翼","選び直す照準","休息の防壁","帰る日の魔弾"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'9,5,15,8,7,14};','9,5,15,8,7,14,11,6,16};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'1.2f,1.12f,1.36f};','1.2f,1.12f,1.36f,1.22f,1.14f,1.4f};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"書翼"};','"書翼","銃翼"};')
edit(Path('Data/HeroineIdentityCatalog.cs'),'default:throw new ArgumentException','case "heroine.shangrila":name="静かな照準";master="休息へ帰る射手";icon="star";bonus="会心率＋5%";break;\n                default:throw new ArgumentException')
edit(Path('Data/HeroineIdentityCatalog.cs'),'string resource=job=="job.gambler"?', 'string resource=job=="job.sniper"?"通常行動で狙撃＋3。指定した1人の攻撃を支援。狙撃15で詠唱中は全員を支援し、5倍弾を発射。":job=="job.gambler"?')
edit(Path('Core/PlayableBattle.cs'),'formationIds[i]=="heroine.arcane-academy"))','formationIds[i]=="heroine.arcane-academy" || formationIds[i]=="heroine.shangrila"))')
art=(S/'Data/Plan10ArcaneHomeArt.cs').read_text(encoding='utf8').replace('Plan10ArcaneHomeArt','Plan10ShangrilaHomeArt').replace('"arcane"','"shangrila"');(S/'Data/Plan10ShangrilaHomeArt.cs').write_text(art,encoding='utf8')
edit(Path('Data/ProductionStoryCatalog.cs'),'Plan10ArcaneAcademyHomeArt.Apply(home);','Plan10ArcaneAcademyHomeArt.Apply(home);\n            Plan10ShangrilaHomeArt.Apply(home);')
print('Shangrila: fifteenth form / twelfth person / all thirteen jobs.')
