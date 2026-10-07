import copy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1];RES=R/'game/unity/Assets/Game/Resources';S=RES.parent/'Scripts';key='arcane';hero='heroine.'+key
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer',hero)
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-slayer-swim.json');h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='アルケイン',jobId='job.gunner',personId=hero,variantId='normal',hpBp=10000,attackBp=10500,defenseBp=9500,speedBp=10000,traitHpPercent=0,traitAttackPercent=5);c['heroines'].append(h)
for slot,(name,power,wait) in enumerate([('レッツアドベンチャーですわ！',2.08,150),('遺物の見取り図',0,100),('博物館の守り手',1.5,125)],1):
 observed=slot==1
 s=dict(id=f'{hero}.{slot}',sourceSkillId=f'{hero}.{slot}',ownerId=hero,slot=slot,name=name,sourceFile='012戦闘_アルケイン.mkv' if observed else '',sourceSecond=15 if observed else 0,observedSkillLevel=7 if observed else 0,ruleOrigin='video-observation-plus-newaster-original' if observed else 'newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='physical',attributes=['銃弾'],ignoreDefenseBp=0)
 if slot==2:s.update(effectRuleId='effect.allies-buff',targetRuleId='target.all-living-allies',damageType='',attributes=[],damageCap=0,chainEligible=False,selfEffects=[dict(kind='critical',percent=15,turns=3)])
 if slot==3:s.update(targetRuleId='target.all-enemies',attributes=['銃弾','光'],ignoreDefenseBp=2500)
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=['銃弾'],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']==hero:x['status']='implemented'
write(RES/f'Combat/battle-plan10-{key}.json',c)
write(RES/f'Illustrations/{key}-battle-binding.json',dict(heroineId=hero,placeholder=False,fullCanvas=True,resourcePath=f'Illustrations/{key}-standing-candidate-v1',attackResourcePath=f'Illustrations/{key}-attack-candidate-v1',hitResourcePath=f'Illustrations/{key}-hit-candidate-v1',cutinResourcePath=f'Illustrations/{key}-cutin-candidate-v1'))
story=read(RES/'Story/plan10-slayer-swim-story-content.json');extra=read(R/f'docs/production/plan10-{key}-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/f'Story/plan10-{key}-story-content.json',story)
def edit(path,old,new):
 p=S/path;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf8')
edit(Path('Presentation/PrototypeBootstrap.cs'),'?"Combat/battle-formal":"Combat/battle-plan10-slayer-swim"','?"Combat/battle-formal":"Combat/battle-plan10-arcane"')
edit(Path('Presentation/ProductionStoryExperience.cs'),'combatDefinitions.HeroineIds.Contains("heroine.slayer-swim")?', 'combatDefinitions.HeroineIds.Contains("heroine.arcane")?"Story/plan10-arcane-story-content":combatDefinitions.HeroineIds.Contains("heroine.slayer-swim")?')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"heroine.slayer-swim"};','"heroine.slayer-swim","heroine.arcane"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"夏を継ぐ翼"};','"夏を継ぐ翼","探検の指針","展示の守り","帰還の鍵"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'8,6,14,8,6,14};','8,6,14,8,6,14,9,5,15};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'1.18f,1.14f,1.38f,1.18f,1.14f,1.38f};','1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"海翼"};','"海翼","遺翼"};')
edit(Path('Data/HeroineIdentityCatalog.cs'),'default:throw new ArgumentException','case "heroine.arcane":name="空欄を残す探検者";master="ふたりの展示室";icon="star";bonus="物理防御＋5%";break;\n                default:throw new ArgumentException')
edit(Path('Core/PlayableBattle.cs'),'formationIds[i]=="heroine.slayer-swim"))','formationIds[i]=="heroine.slayer-swim" || formationIds[i]=="heroine.arcane"))')
art=(S/'Data/Plan10SlayerSwimHomeArt.cs').read_text(encoding='utf8').replace('SlayerSwim','Arcane').replace('slayer-swim','arcane');(S/'Data/Plan10ArcaneHomeArt.cs').write_text(art,encoding='utf8')
edit(Path('Data/ProductionStoryCatalog.cs'),'Plan10SlayerSwimHomeArt.Apply(home);','Plan10SlayerSwimHomeArt.Apply(home);\n            Plan10ArcaneHomeArt.Apply(home);')
print('Arcane: thirteenth form, eleventh person, existing Gunner; provenance separates two original skills.')
