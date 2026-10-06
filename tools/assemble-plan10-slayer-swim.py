import copy,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources';SCRIPTS=RES.parent/'Scripts'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def owner(v):
 if isinstance(v,str):return v.replace('heroine.slayer','heroine.slayer-swim')
 if isinstance(v,list):return [owner(x) for x in v]
 if isinstance(v,dict):return {k:owner(x) for k,x in v.items()}
 return v
c=read(RES/'Combat/battle-plan10-nighthawk.json')
c['jobs'].append(dict(id='job.general',resourceName='指揮旗',resourceMax=15,initialResource=0,gainAtReady=3,gainOnAttack=0,gainOnHit=0,hp=420,attack=95,defense=110,magicDefense=100,speed=125,criticalBp=1500))
h=owner(copy.deepcopy(c['heroines'][0]));h.update(name='スレイヤー（水着）',jobId='job.general',personId='heroine.slayer',variantId='swim',hpBp=10000,attackBp=10000,defenseBp=10000,speedBp=10000,traitHpPercent=5,traitAttackPercent=0);c['heroines'].append(h)
for slot,(name,power,wait) in enumerate([('スレイヤーの意志',1.4,75),('君といる夏',0,125),('《継承》呪いと自由の物語',2.4,150)],1):
 s=dict(id=f'heroine.slayer-swim.{slot}',sourceSkillId=f'heroine.slayer-swim.{slot}',ownerId=h['id'],slot=slot,name=name,sourceFile='056戦闘_スレイヤー水着.mkv',sourceSecond=15,observedSkillLevel=7,ruleOrigin='video-observation-plus-newaster-original',effectRuleId='effect.damage',targetRuleId='target.selected-enemy',powerScale=power,partScale=1,resourceCost=0,recoveryPercent=wait,castPercent=0,targetCount=0,baseHealing=0,chainEligible=True,damageCap=10000,criticalBonusBp=0,selfEffects=[],statusEffects=[],conditions=[],damageType='physical',attributes=['刺突','光'],ignoreDefenseBp=0)
 if slot==1:s.update(ignoreDefenseBp=10000,enemyAttackReductionPercent=20,enemyAttackReductionTurns=3,selfEffects=[dict(kind='speed',percent=20,turns=3)])
 if slot==2:s.update(effectRuleId='effect.allies-buff',targetRuleId='target.all-living-allies',damageType='',attributes=[],damageCap=0,chainEligible=False,selfEffects=[dict(kind='attack',percent=30,turns=3),dict(kind='critical',percent=15,turns=3)])
 if slot==3:s.update(targetRuleId='target.all-enemies',damageType='magic',attributes=['火','光'],criticalBonusBp=3000,statusEffects=[dict(kind='burn',amount=50)])
 c['skills'].append(s)
chain=owner(copy.deepcopy(c['chainActions'][0]));chain.update(powerScale=.65,damageType='physical',attributes=[],ignoreDefenseBp=0);c['chainActions'].append(chain)
c['contentReferences'] += [owner(x) for x in c['contentReferences'] if x['ownerId']=='heroine.slayer']
for x in c['contentReferences']:
 if x['ownerId']==h['id']:x['status']='implemented'
slots=[dict(slot=i,label=label,**buff) for i,(label,buff) in enumerate([('攻撃＋15%',dict(attackPercent=15)),('物理防御＋20%',dict(physicalDefensePercent=20)),('魔法防御＋20%',dict(magicDefensePercent=20)),('速度＋10%',dict(speedPercent=10)),('会心率＋10%',dict(criticalBp=1000))])]
c['generalFormations']=[dict(ownerId=h['id'],strengthenPercent=100,durationClock=300,slots=slots)]
write(RES/'Combat/battle-plan10-slayer-swim.json',c)
write(RES/'Illustrations/slayer-swim-battle-binding.json',dict(heroineId=h['id'],placeholder=False,fullCanvas=True,resourcePath='Illustrations/slayer-swim-standing-candidate-v1',attackResourcePath='Illustrations/slayer-swim-attack-candidate-v1',hitResourcePath='Illustrations/slayer-swim-hit-candidate-v1',cutinResourcePath='Illustrations/slayer-swim-cutin-candidate-v1'))
story=read(RES/'Story/plan10-nighthawk-story-content.json');extra=read(ROOT/'docs/production/plan10-slayer-swim-story-content.json');story['chapters']+=extra['chapters'];story['events']+=extra['events'];write(RES/'Story/plan10-slayer-swim-story-content.json',story)
# Stage hook is deliberate: historical capture modes keep their own roster.
def edit(path,old,new):
 p=SCRIPTS/path;t=p.read_text(encoding='utf-8-sig')
 if new not in t:
  assert old in t,(path,old);p.write_text(t.replace(old,new),encoding='utf8')
edit(Path('Presentation/PrototypeBootstrap.cs'),'?"Combat/battle-formal":"Combat/battle-plan10-nighthawk"','?"Combat/battle-formal":"Combat/battle-plan10-slayer-swim"')
edit(Path('Presentation/ProductionStoryExperience.cs'),'combatDefinitions.HeroineIds.Contains("heroine.nighthawk")?', 'combatDefinitions.HeroineIds.Contains("heroine.slayer-swim")?"Story/plan10-slayer-swim-story-content":combatDefinitions.HeroineIds.Contains("heroine.nighthawk")?')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"heroine.nighthawk"};','"heroine.nighthawk","heroine.slayer-swim"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"夜明けの足音"};','"夜明けの足音","白傘の道標","海風の守り","夏を継ぐ翼"};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'8,6,14};','8,6,14,8,6,14};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'1.18f,1.14f,1.38f};','1.18f,1.14f,1.38f,1.18f,1.14f,1.38f};')
edit(Path('Data/ProductionEconomyCatalog.cs'),'"夜星"};','"夜星","海翼"};')
edit(Path('Data/HeroineIdentityCatalog.cs'),'id.Replace("job.","");','id=="job.general"?"ジェネラル":id=="job.gambler"?"ギャンブラー":id=="job.sniper"?"スナイパー":id.Replace("job.","");')
edit(Path('Data/HeroineIdentityCatalog.cs'),'default:throw new ArgumentException','case "heroine.slayer-swim":name="白い傘を分ける翼";master="五人で帰る夏";icon="leaf";bonus="物理防御＋5%";break;\n                default:throw new ArgumentException')
edit(Path('Data/HeroineIdentityCatalog.cs'),'string resource=job=="job.chaser"?', 'string resource=job=="job.general"?"指揮官を1人選ぶ。本人の5枠効果のみ適用。指揮15で300 Clock強化。":job=="job.chaser"?')
edit(Path('Core/PlayableBattle.cs'),'formationIds[i]=="heroine.nighthawk"))','formationIds[i]=="heroine.nighthawk" || formationIds[i]=="heroine.slayer-swim"))')
art=(SCRIPTS/'Data/Plan10NighthawkHomeArt.cs').read_text(encoding='utf8').replace('Nighthawk','SlayerSwim').replace('nighthawk','slayer-swim');(SCRIPTS/'Data/Plan10SlayerSwimHomeArt.cs').write_text(art,encoding='utf8')
edit(Path('Data/ProductionStoryCatalog.cs'),'Plan10NighthawkHomeArt.Apply(home);','Plan10NighthawkHomeArt.Apply(home);\n            Plan10SlayerSwimHomeArt.Apply(home);')
# Battle bindings are data; additions no longer require a C# ID list.
p=SCRIPTS/'Presentation/BattleIllustrationView.cs';t=p.read_text(encoding='utf8');a=t.index('                var extra=Resources.Load<TextAsset>');b=t.index('                portraits=',a);t=t[:a]+'''                foreach(var binding in Resources.LoadAll<TextAsset>("Illustrations").Where(a=>a.name.EndsWith("-battle-binding",StringComparison.Ordinal))){var hero=JsonUtility.FromJson<HeroIllustrationBinding>(binding.text);if(!manifest.heroes.Any(h=>h.heroineId==hero.heroineId))manifest.heroes=manifest.heroes.Concat(new[]{hero}).ToArray();}
'''+t[b:];p.write_text(t,encoding='utf8')
print('Slayer summer catalog / shared identity / art / weapon integration assembled.')
