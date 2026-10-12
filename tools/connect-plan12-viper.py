"""Register Viper rules and preview paths, never promote the normal catalog."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1];S=ROOT/'game/unity/Assets/Game/Scripts'
def replace(file,old,new):
 p=S/file;s=p.read_text(encoding='utf8')
 if new in s:return
 assert old in s,(file,old)
 p.write_text(s.replace(old,new),encoding='utf8')
def case(file,row):replace(file,'                default:',row+'\n                default:')
case('Core/HeroineAuthoredNames.cs','                case "heroine.viper":return new HeroineAuthoredIdentity("喧嘩上等","金髪美女","約束の帰投","アヴェンジャー","乱暴な冗談の合間に、皿を置く手だけは静か。自由に出かけ、交わした約束へ自分の名前で帰ってくる。");')
case('Core/HeroinePersonalAbility.cs','                case "heroine.viper":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.viper").Personal,"sword","本体への与ダメージ＋8%。","body",8);')
case('Core/DailyReactionProfiles.cs','                case "heroine.viper":return new ReactionStyleProfile{main="teasing",sub="cool"};')
case('Core/HeroineTraitRules.cs','                case "heroine.viper":key="physical-defense";break;')
case('Data/HeroineIdentityCatalog.cs','                case "heroine.viper":icon="sword";bonus="物理防御＋5%";break;')
replace('Core/WeaponNodeNames.cs','heroineId=="heroine.invisible"?new[]{"曙光","星虹","帰路"}:','heroineId=="heroine.invisible"?new[]{"曙光","星虹","帰路"}:\n                heroineId=="heroine.viper"?new[]{"暁鐘","花火","帰路"}:')
replace('Data/ProductionEconomyCatalog.cs','"heroine.shiva","heroine.invisible"};','"heroine.shiva","heroine.invisible","heroine.viper"};')
p=S/'Data/ProductionEconomyCatalog.cs';s=p.read_text(encoding='utf8')
if len(s.split('private static readonly int[] Attacks={')[1].split('};')[0].split(','))==63:
 s=re.sub(r'(int\[\] Attacks=\{[^}]+)(\};)',r'\1,8,10,14\2',s);s=re.sub(r'(float\[\] Powers=\{[^}]+)(\};)',r'\1,1.18f,1.25f,1.38f\2',s);p.write_text(s,encoding='utf8')
replace('Data/Plan12FirstBatchHomeArt.cs','"shiva","invisible"}','"shiva","invisible","viper"}')
replace('Data/Plan12FirstBatchHomeArt.cs','if(key=="invisible")','if(key=="invisible" || key=="viper")')
for file,expr in [('Core/PlayableBattle.cs','formationIds[i]'),('Presentation/HeroineSanctuaryArt.cs','hero')]:replace(file,expr+'=="heroine.invisible"',expr+'=="heroine.invisible" || '+expr+'=="heroine.viper"')
replace('Presentation/LayeredWeaponTree.cs','hero=="heroine.invisible";','hero=="heroine.invisible" || hero=="heroine.viper";')
replace('Presentation/PrototypeBootstrap.cs','var source=Resources.Load<TextAsset>(', 'var source=Resources.Load<TextAsset>(Environment.GetCommandLineArgs().Contains("-plan12ViperPreview")?"Combat/battle-plan12-viper":')
replace('Presentation/ProductionStoryExperience.cs','combatDefinitions.HeroineIds.Contains("heroine.invisible")?', 'combatDefinitions.HeroineIds.Contains("heroine.viper")?"Story/plan12-viper-story-content":combatDefinitions.HeroineIds.Contains("heroine.invisible")?')
for file in ['Presentation/Plan12FirstBatchAcceptance.cs','Presentation/Plan12ArtSweepAcceptance.cs','Presentation/GardenArtComposition.cs']:replace(file,'"heroine.shiva","heroine.invisible"}','"heroine.shiva","heroine.invisible","heroine.viper"}')
replace('Presentation/AdvExperience.cs','if(actor.HeroineId=="heroine.invisible")','if(actor.HeroineId=="heroine.invisible" || actor.HeroineId=="heroine.viper")')
print('VIPER_RULES_CONNECTED preview only; live catalog remains 21')
if __name__=='__main__':pass
