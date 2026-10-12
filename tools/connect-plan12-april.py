"""Register April's authored identity and reuse existing battle rules."""
from pathlib import Path
R = Path(__file__).resolve().parents[1]
S = R / 'game/unity/Assets/Game/Scripts'


def edit(file, old, new):
    p = S / file
    s = p.read_text(encoding='utf8')
    if new in s:
        return
    assert s.count(old) == 1, (file, old)
    p.write_text(s.replace(old, new), encoding='utf8')


edit('Core/HeroineAuthoredNames.cs', 'default:throw new ArgumentException(',
     'case "heroine.april":return new HeroineAuthoredIdentity("芽吹きの誇り","お人好し","木々の生命力","リクレイマー","枯れた庭にも、植え直す場所はある。土に触れた手で、今度は自分の願いも育てていく。");\n                default:throw new ArgumentException(')
edit('Core/HeroinePersonalAbility.cs', 'default:return null;',
     'case "heroine.april":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.april").Personal,"leaf","自身の回復スキルによる回復量＋10%。","healing",10);\n                default:return null;')
edit('Core/HeroineTraitRules.cs', 'default:return 0;',
     'case "heroine.april":key="hp";break;\n                default:return 0;')
edit('Data/HeroineIdentityCatalog.cs', 'default:throw new ArgumentException(',
     'case "heroine.april":icon="leaf";bonus="最大HP＋5%";break;\n                default:throw new ArgumentException(')
edit('Core/DailyReactionProfiles.cs', 'default:throw new ArgumentException(',
     'case "heroine.april":return new ReactionStyleProfile{main="elegant",sub="affectionate"};\n                default:throw new ArgumentException(')
edit('Core/WeaponNodeNames.cs', 'new[]{"ルーメン","クストス","ウェント"};',
     'heroineId=="heroine.april"?new[]{"萌芽","花冠","再生"}:\n                new[]{"ルーメン","クストス","ウェント"};')
edit('Core/InteractionTraitCatalog.cs', 'Def("hair.red","紅蓮の髪"),',
     'Def("hair.red","紅蓮の髪"),Def("hair.purple","藤霞の髪"),')
edit('Core/InteractionTraitCatalog.cs', 'case "hair.red":return',
     'case "hair.purple":return "淡い藤色の髪に、庭を渡る風が遊ぶ。花の香りに振り向けば、穏やかな笑顔がある。";\n                case "hair.red":return')
edit('Data/ProductionEconomyCatalog.cs', '"heroine.vindicator","heroine.weapon-master"}',
     '"heroine.vindicator","heroine.weapon-master","heroine.april"}')
# Append exactly three owner entries, preserving every accepted weapon's index.
edit('Data/ProductionEconomyCatalog.cs', '8,10,14,8,10,14,8,10,14};',
     '8,10,14,8,10,14,8,10,14,8,10,14};')
edit('Data/ProductionEconomyCatalog.cs', '1.18f,1.25f,1.38f,1.18f,1.25f,1.38f,1.18f,1.25f,1.38f};',
     '1.18f,1.25f,1.38f,1.18f,1.25f,1.38f,1.18f,1.25f,1.38f,1.18f,1.25f,1.38f};')
print('APRIL_IDENTITY_CONNECTED no normal catalog change')
