"""Connect Weapon Master and its narrowly scoped combat effects; no publication."""
from pathlib import Path
R=Path(__file__).resolve().parents[1]
S=R/'game/unity/Assets/Game/Scripts'
def edit(path,old,new):
 p=R/path;s=p.read_text(encoding='utf8')
 if new in s:return
 assert old in s,(path,old)
 p.write_text(s.replace(old,new),encoding='utf8')
def core(file,old,new):edit('game/unity/Assets/Game/Scripts/'+file,old,new)

# A real target-local defense debuff, separate from fracture/status accumulation.
core('Core/CombatDefinitionCatalog.cs','public int enemyAttackReductionPercent,enemyAttackReductionTurns;', 'public int enemyAttackReductionPercent,enemyAttackReductionTurns;\n        public int enemyDefenseReductionPercent,enemyDefenseReductionTurns;')
core('Core/CombatDefinitionCatalog.cs','if(skills.Any(s=>s.enemyAttackReductionPercent<0', 'if(skills.Any(s=>s.enemyDefenseReductionPercent<0 || s.enemyDefenseReductionPercent>50 || s.enemyDefenseReductionTurns<0 || s.enemyDefenseReductionTurns>10 || (s.enemyDefenseReductionPercent==0)!=(s.enemyDefenseReductionTurns==0) || s.enemyDefenseReductionPercent>0 && s.effectRuleId!="effect.damage"))throw new ArgumentException("Invalid enemy defense reduction.");\n            if(skills.Any(s=>s.enemyAttackReductionPercent<0')
core('Core/EnemyCombatStatus.cs','public int AttackReductionTurns {get;private set;}','public int DefenseReductionPercent {get;private set;}\n        public int DefenseReductionTurns {get;private set;}\n        public void SetDefenseReduction(int percent,int turns){if(percent<1 || percent>50 || turns<1 || turns>10)throw new ArgumentException("Invalid defense reduction.");DefenseReductionPercent=percent;DefenseReductionTurns=turns;}\n        public int AttackReductionTurns {get;private set;}')
core('Core/EnemyCombatStatus.cs','public void Tick() {','public void Tick() {if(DefenseReductionTurns>0 && --DefenseReductionTurns==0)DefenseReductionPercent=0;')
core('Core/EnemyCombatStatus.cs','public string Description =>','public string Description => (DefenseReductionTurns>0?"防御−"+DefenseReductionPercent+"%（残り"+DefenseReductionTurns+"） / ":"")+')
for f in ['Core/HeroineSkillRules.cs','Core/PlayableBattle.cs']:
 core(f,'enemyAttackReductionPercent=s.enemyAttackReductionPercent,','enemyDefenseReductionPercent=s.enemyDefenseReductionPercent,enemyDefenseReductionTurns=s.enemyDefenseReductionTurns,enemyAttackReductionPercent=s.enemyAttackReductionPercent,')
core('Core/HeroineSkillRules.cs','if(s.enemyAttackReductionPercent>0)', 'if(s.enemyDefenseReductionPercent>0)result+="対象の防御−"+s.enemyDefenseReductionPercent+"%・"+s.enemyDefenseReductionTurns+"ターン。";\n            if(s.enemyAttackReductionPercent>0)')
core('Core/PlayableBattle.cs','var d=commandDefinitions[actor,slot];if(d.enemyAttackReductionPercent>0)', 'var d=commandDefinitions[actor,slot];if(d.enemyDefenseReductionPercent>0)foreach(string target in alchemyHitTargets)State.EnemyStatus(target).SetDefenseReduction(d.enemyDefenseReductionPercent,d.enemyDefenseReductionTurns);if(d.enemyAttackReductionPercent>0)')
core('Core/BattleActionResolver.cs','decimal effectiveDefense=(decimal)defense*(10000-skill.IgnoreDefenseBp)/10000m;', 'decimal effectiveDefense=(decimal)defense*(10000-skill.IgnoreDefenseBp)/10000m;\n            effectiveDefense*=1m-battle.EnemyStatus(targetId).DefenseReductionPercent/100m;')
core('Core/HeroinePersonalAbility.cs','default:return null;', 'case "heroine.weapon-master":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.weapon-master").Personal,"flame","火傷状態の対象への与ダメージ＋20%。","burned",20);\n                default:return null;')
core('Core/HeroinePersonalAbility.cs','BattleSkill skill,string target)', 'BattleSkill skill,string target,BattleState battle=null)')
core('Core/HeroinePersonalAbility.cs','bool active=p.Kind=="counter"?', 'bool active=p.Kind=="burned"?battle!=null && battle.EnemyStatus(target).Active("burn"):p.Kind=="counter"?')
core('Core/BattleActionResolver.cs','HeroinePersonalAbility.Damage(hero,skill,targetId)', 'HeroinePersonalAbility.Damage(hero,skill,targetId,battle)')
core('Core/HeroineAuthoredNames.cs','default:throw new ArgumentException(', 'case "heroine.weapon-master":return new HeroineAuthoredIdentity("強装弾","火遊び","諸兵科連合","換装銃架ハルモニア","値札を直す手と、包帯を巻く手。明日も店を開けられるように、今日の助け方を一緒に考える。");\n                default:throw new ArgumentException(')
core('Core/HeroineTraitRules.cs','case "heroine.vindicator":key="magic-defense";break;', 'case "heroine.vindicator":key="magic-defense";break;\n                case "heroine.weapon-master":key="attack";break;')
core('Data/HeroineIdentityCatalog.cs','case "heroine.vindicator":icon="moon";bonus="魔法防御＋5%";break;', 'case "heroine.vindicator":icon="moon";bonus="魔法防御＋5%";break;\n                case "heroine.weapon-master":icon="sword";bonus="攻撃＋5%";break;')
core('Core/DailyReactionProfiles.cs','case "heroine.vindicator":', 'case "heroine.weapon-master":return new ReactionStyleProfile{main="active",sub="affectionate"};\n                case "heroine.vindicator":')
core('Core/WeaponNodeNames.cs','heroineId=="heroine.vindicator"?new[]{"誓衛","静空","黎明"}:','heroineId=="heroine.vindicator"?new[]{"誓衛","静空","黎明"}:\n                heroineId=="heroine.weapon-master"?new[]{"黎光","翠雨","旅路"}:')
core('Data/ProductionEconomyCatalog.cs','"heroine.viper","heroine.vindicator"}', '"heroine.viper","heroine.vindicator","heroine.weapon-master"}')
core('Data/ProductionEconomyCatalog.cs','8,10,14,8,10,14};','8,10,14,8,10,14,8,10,14};')
core('Data/ProductionEconomyCatalog.cs','1.18f,1.25f,1.38f,1.18f,1.25f,1.38f};','1.18f,1.25f,1.38f,1.18f,1.25f,1.38f,1.18f,1.25f,1.38f};')
for f,expr in [('Core/PlayableBattle.cs','formationIds[i]'),('Presentation/HeroineSanctuaryArt.cs','hero'),('Presentation/LayeredWeaponTree.cs','hero')]:
 core(f,expr+'=="heroine.vindicator"',expr+'=="heroine.vindicator" || '+expr+'=="heroine.weapon-master"')
core('Data/Plan12FirstBatchHomeArt.cs','"viper","vindicator"}', '"viper","vindicator","weapon-master"}')
core('Data/Plan12FirstBatchHomeArt.cs','key=="vindicator"', 'key=="vindicator" || key=="weapon-master"')
core('Presentation/PrototypeBootstrap.cs','var source=Resources.Load<TextAsset>(', 'var source=Resources.Load<TextAsset>(Environment.GetCommandLineArgs().Contains("-plan12WeaponMasterPreview")?"Combat/battle-plan12-weapon-master":')
core('Presentation/ProductionStoryExperience.cs','combatDefinitions.HeroineIds.Contains("heroine.vindicator")?', 'combatDefinitions.HeroineIds.Contains("heroine.weapon-master")?"Story/plan12-weapon-master-story-content":combatDefinitions.HeroineIds.Contains("heroine.vindicator")?')
for f in ['Presentation/Plan12FirstBatchAcceptance.cs','Presentation/Plan12ArtSweepAcceptance.cs','Presentation/GardenArtComposition.cs']:
 core(f,'"heroine.viper","heroine.vindicator"}', '"heroine.viper","heroine.vindicator","heroine.weapon-master"}')
core('Presentation/AdvExperience.cs','actor.HeroineId=="heroine.vindicator")','actor.HeroineId=="heroine.vindicator" || actor.HeroineId=="heroine.weapon-master")')
edit('game/unity/Assets/Game/Editor/HeroinePortraitBuildValidation.cs','"heroine.viper","heroine.vindicator"}', '"heroine.viper","heroine.vindicator","heroine.weapon-master"}')
edit('game/unity/Assets/Game/Editor/HeroinePortraitBuildValidation.cs','"viper","vindicator"}', '"viper","vindicator","weapon-master"}')
print('WEAPON_MASTER_PREVIEW_CONNECTED normal remains 23')
