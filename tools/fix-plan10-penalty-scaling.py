from pathlib import Path
p=Path(__file__).resolve().parents[1]/'game/unity/Assets/Game/Scripts/Core/HeroineSkillRules.cs'
t=p.read_text(encoding='utf8');t=t.replace('percent=x.kind=="forced-target"?x.percent:','percent=x.kind=="forced-target" || x.kind=="attack-reduction"?x.percent:');p.write_text(t,encoding='utf8')
print('Self attack penalty stays fixed across skill levels; upgrading does not worsen recoil.')
