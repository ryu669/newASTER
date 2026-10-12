"""Connect Vindicator preview rendering without publishing."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];S=ROOT/'game/unity/Assets/Game/Scripts'
def replace(file,old,new):
 p=S/file;s=p.read_text(encoding='utf-8')
 if new in s:return
 assert old in s,(file,old);p.write_text(s.replace(old,new),encoding='utf-8')
replace('Data/Plan12FirstBatchHomeArt.cs','"shiva","invisible","viper"}','"shiva","invisible","viper","vindicator"}')
replace('Data/Plan12FirstBatchHomeArt.cs','key=="invisible" || key=="viper"','key=="invisible" || key=="viper" || key=="vindicator"')
for file,expr in [('Core/PlayableBattle.cs','formationIds[i]'),('Presentation/HeroineSanctuaryArt.cs','hero')]:replace(file,expr+'=="heroine.viper"',expr+'=="heroine.viper" || '+expr+'=="heroine.vindicator"')
replace('Presentation/LayeredWeaponTree.cs','hero=="heroine.viper";','hero=="heroine.viper" || hero=="heroine.vindicator";')
replace('Presentation/PrototypeBootstrap.cs','var source=Resources.Load<TextAsset>(', 'var source=Resources.Load<TextAsset>(Environment.GetCommandLineArgs().Contains("-plan12VindicatorPreview")?"Combat/battle-plan12-vindicator":')
replace('Presentation/ProductionStoryExperience.cs','combatDefinitions.HeroineIds.Contains("heroine.viper")?', 'combatDefinitions.HeroineIds.Contains("heroine.vindicator")?"Story/plan12-vindicator-story-content":combatDefinitions.HeroineIds.Contains("heroine.viper")?')
for file in ['Presentation/Plan12FirstBatchAcceptance.cs','Presentation/Plan12ArtSweepAcceptance.cs','Presentation/GardenArtComposition.cs']:replace(file,'"heroine.invisible","heroine.viper"}','"heroine.invisible","heroine.viper","heroine.vindicator"}')
replace('Presentation/AdvExperience.cs','actor.HeroineId=="heroine.viper")','actor.HeroineId=="heroine.viper" || actor.HeroineId=="heroine.vindicator")')
p=ROOT/'game/unity/Assets/Game/Editor/HeroinePortraitBuildValidation.cs';s=p.read_text(encoding='utf-8').replace('"heroine.invisible","heroine.viper"}','"heroine.invisible","heroine.viper","heroine.vindicator"}').replace('"invisible","viper"}','"invisible","viper","vindicator"}');p.write_text(s,encoding='utf-8')
print('VINDICATOR_PREVIEW_CONNECTED normal remains 22')
