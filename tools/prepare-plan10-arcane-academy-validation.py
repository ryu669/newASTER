from pathlib import Path
R=Path(__file__).resolve().parents[1];B=R/'game/unity/Assets/Game'
def clone(path):return (B/path).read_text(encoding='utf8').replace('Arcane','ArcaneAcademy').replace('ARCANE','ARCANE_ACADEMY').replace('arcane','arcane-academy')
t=clone(Path('Scripts/Presentation/Plan10ArcaneAcceptance.cs'));a=t.index('            else{\n                StartBattle');b=t.index('            if(encounter!=null)',a)
t=t[:a]+'''            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyArcaneAcademy();AcceptanceCheck(encounter.JobState(0).Id=="job.gambler","Academy uses Gambler");
                if(view=="slot-miss")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>0),"Fruit miss WT0");
                if(view=="slot-match")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>2),"Five enhanced skill-one lines");
                if(view=="slot-jackpot")AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>5),"All five 777 lines execute");
                if(view=="status-extension"){
                    encounter.State.BossStatus.Add(new EnemyStatusDef{kind="burn",amount=1000});
                    int[] symbols={3,3,0,0,1,0,1,0,1};int at=0;
                    AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>symbols[at++]),"Observed second skill extends active statuses");
                }
                if(view=="buff-extension"){
                    encounter.State.Heroes[1].ApplySelfEffects(new[]{new TimedSelfEffectDef{kind="attack",percent=20,turns=3}});
                    int[] symbols={4,4,0,0,1,0,1,0,1};int at=0;
                    AcceptanceCheck(encounter.SpinGamblerSlot(0,"body",max=>symbols[at++]),"Observed third skill extends allied buffs");
                }
                if(!encounter.Ended)ReadyArcaneAcademy();
            }
'''+t[b:];(B/'Scripts/Presentation/Plan10ArcaneAcademyAcceptance.cs').write_text(t,encoding='utf8')
t=clone(Path('Editor/Plan10ArcaneBuild.cs')).replace('thirteen forms / eleven people','fourteen forms / eleven people');(B/'Editor/Plan10ArcaneAcademyBuild.cs').write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/PrototypeBootstrap.cs';t=p.read_text(encoding='utf8').replace('PreparePlan10ArcaneCapture(args);','PreparePlan10ArcaneCapture(args);\n            PreparePlan10ArcaneAcademyCapture(args);').replace('Environment.GetCommandLineArgs().Contains("-captureArcane")?', 'Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy")?"Combat/battle-plan10-arcane-academy":Environment.GetCommandLineArgs().Contains("-captureArcane")?');p.write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/ProductionStoryExperience.cs';t=p.read_text(encoding='utf8').replace('Environment.GetCommandLineArgs().Contains("-captureArcane") ||','Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy") || Environment.GetCommandLineArgs().Contains("-captureArcane") ||');p.write_text(t,encoding='utf8')
t=(R/'tools/validate-plan10-arcane-player.ps1').read_text(encoding='utf8').replace('Arcane','ArcaneAcademy').replace('ARCANE','ARCANE_ACADEMY').replace('arcane','arcane-academy').replace("'attack','buff','ultimate','reload','volley'","'slot-miss','slot-match','slot-jackpot','status-extension','buff-extension'");(R/'tools/validate-plan10-arcane-academy-player.ps1').write_text(t,encoding='utf8')
print('Academy build, isolated player saves, all-five-line slot validation prepared.')
