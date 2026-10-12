"""Connect Invisible's authored rules; keep the live catalog unchanged until accepted."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
S=ROOT/'game/unity/Assets/Game/Scripts'
def replace(file,old,new):
    p=S/file;s=p.read_text(encoding='utf-8')
    if new in s:return
    assert old in s,(file,old)
    p.write_text(s.replace(old,new),encoding='utf-8')
def case(file,row):replace(file,'                default:',row+'\n                default:')
case('Core/HeroineAuthoredNames.cs','                case "heroine.invisible":return new HeroineAuthoredIdentity("私もみんなも逃げないよ！","お人好し","ユニコーンはみんなの味方☆","可能性の獣","誰にも見えない友達と、隣で返事をくれる人。星の灯りを結んで、みんなの帰る道を照らす。");')
case('Core/HeroinePersonalAbility.cs','                case "heroine.invisible":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.invisible").Personal,"star","自身の回復スキルによる回復量＋10%。","healing",10);')
case('Core/DailyReactionProfiles.cs','                case "heroine.invisible":return new ReactionStyleProfile{main="reserved",sub="affectionate"};')
case('Core/HeroineTraitRules.cs','                case "heroine.invisible":key="magic-defense";break;')
case('Data/HeroineIdentityCatalog.cs','                case "heroine.invisible":icon="star";bonus="魔法防御＋5%";break;')
replace('Core/WeaponNodeNames.cs','heroineId=="heroine.shiva"?new[]{"ソレイユ","エスポワール","エラン"}:','heroineId=="heroine.shiva"?new[]{"ソレイユ","エスポワール","エラン"}:\n                heroineId=="heroine.invisible"?new[]{"曙光","星虹","帰路"}:')
replace('Core/TimedSelfEffects.cs','kind!="speed" &&','kind!="magic-defense" && kind!="speed" &&')
replace('Core/TimedSelfEffects.cs','=> kind=="speed"?','=> kind=="magic-defense"?"魔法防御＋":kind=="speed"?')
replace('Core/BattleState.cs','GeneralMagicDefensePercent+RelicDefensePercent','GeneralMagicDefensePercent+EffectPercent("magic-defense")+RelicDefensePercent')
replace('Core/PlayableBattle.cs','private int HealingPower(HealingSkillDefinition d) => d.BaseHealing+(int)Math.Floor(State.Heroes[d.Actor].Attack*d.AttackScale)+support[d.Actor]*15;', '''private int HealingPower(HealingSkillDefinition d)
        {
            var hero=State.Heroes[d.Actor];var trait=hero.PersonalTraitsEnabled?HeroinePersonalAbility.For(hero.Id):null;
            int bonus=trait?.Kind=="healing"?trait.Percent:0;
            return (int)Math.Min(int.MaxValue,((long)d.BaseHealing+(long)Math.Floor(hero.Attack*d.AttackScale)+support[d.Actor]*15)*(100+bonus)/100);
        }''')
replace('Data/ProductionEconomyCatalog.cs','"heroine.anathema","heroine.shiva"};','"heroine.anathema","heroine.shiva","heroine.invisible"};')
p=S/'Data/ProductionEconomyCatalog.cs';s=p.read_text(encoding='utf-8')
if len(s.split('private static readonly int[] Attacks={')[1].split('};')[0].split(','))==60:
    import re
    s=re.sub(r'(int\[\] Attacks=\{[^}]+)(\};)',r'\1,8,6,14\2',s)
    s=re.sub(r'(float\[\] Powers=\{[^}]+)(\};)',r'\1,1.18f,1.14f,1.38f\2',s)
    p.write_text(s,encoding='utf-8')
replace('Data/Plan12FirstBatchHomeArt.cs','"anathema","shiva"}','"anathema","shiva","invisible"}')
for file,expr in [('Core/PlayableBattle.cs','formationIds[i]'),('Presentation/HeroineSanctuaryArt.cs','hero')]:
    replace(file,expr+'=="heroine.shiva"',expr+'=="heroine.shiva" || '+expr+'=="heroine.invisible"')
replace('Presentation/LayeredWeaponTree.cs','hero=="heroine.shiva";','hero=="heroine.shiva" || hero=="heroine.invisible";')
replace('Presentation/PrototypeBootstrap.cs','var source=Resources.Load<TextAsset>(', 'var source=Resources.Load<TextAsset>(Environment.GetCommandLineArgs().Contains("-plan12InvisiblePreview")?"Combat/battle-plan12-invisible":')
replace('Presentation/ProductionStoryExperience.cs','combatDefinitions.HeroineIds.Contains("heroine.iron-maiden")?', 'combatDefinitions.HeroineIds.Contains("heroine.invisible")?"Story/plan12-invisible-story-content":combatDefinitions.HeroineIds.Contains("heroine.iron-maiden")?')
replace('Presentation/Plan12FirstBatchAcceptance.cs','"heroine.anathema","heroine.shiva"}','"heroine.anathema","heroine.shiva","heroine.invisible"}')
replace('Presentation/Plan12ArtSweepAcceptance.cs','"heroine.anathema","heroine.shiva"}','"heroine.anathema","heroine.shiva","heroine.invisible"}')
replace('Presentation/GardenArtComposition.cs','"heroine.anathema","heroine.shiva"}','"heroine.anathema","heroine.shiva","heroine.invisible"}')
print('INVISIBLE_CODE_CONNECTED; normal catalog remains accepted first batch')
