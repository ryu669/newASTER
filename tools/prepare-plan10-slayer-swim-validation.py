from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];B=ROOT/'game/unity/Assets/Game'
def clone(path):return (B/path).read_text(encoding='utf8').replace('Nighthawk','SlayerSwim').replace('NIGHTHAWK','SLAYER_SWIM').replace('nighthawk','slayer-swim')
t=clone(Path('Scripts/Presentation/Plan10NighthawkAcceptance.cs'));a=t.index('            else{\n                StartBattle');b=t.index('            if(encounter!=null)',a)
t=t[:a]+'''            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadySlayerSwim();AcceptanceCheck(encounter.CommanderActor==0,"Summer general is commander");
                if(view=="buff"){AcceptanceCheck(encounter.Act(0,1,"body"),"Allies summer buff executes");ReadySlayerSwim();}
                if(view=="attack")AcceptanceCheck(encounter.Act(0,0,"body"),"Defense ignoring debuff attack executes");
                if(view=="ultimate")AcceptanceCheck(encounter.Act(0,2,"body"),"Magic all-enemy attack executes");
                if(view=="command"){encounter.State.Heroes[0].GainResource(15);AcceptanceCheck(encounter.ActivateGeneralCommand(0),"General command strengthens five slots");}
            }
'''+t[b:]
t=t.replace('else if(view=="formation")formationOpen=true;','else if(view=="formation" || view=="formation-member" || view=="formation-roster"){formationOpen=true;formationSlot=0;formationLayer=view=="formation"?0:view=="formation-member"?1:2;}')
(B/'Scripts/Presentation/Plan10SlayerSwimAcceptance.cs').write_text(t,encoding='utf8')
t=clone(Path('Editor/Plan10NighthawkBuild.cs')).replace('eleven forms / ten people','twelve forms / ten people');(B/'Editor/Plan10SlayerSwimBuild.cs').write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/PrototypeBootstrap.cs';t=p.read_text(encoding='utf8').replace('PreparePlan10NighthawkCapture(args);','PreparePlan10NighthawkCapture(args);\n            PreparePlan10SlayerSwimCapture(args);').replace('Environment.GetCommandLineArgs().Contains("-captureNighthawk")?', 'Environment.GetCommandLineArgs().Contains("-captureSlayerSwim")?"Combat/battle-plan10-slayer-swim":Environment.GetCommandLineArgs().Contains("-captureNighthawk")?');p.write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/ProductionStoryExperience.cs';t=p.read_text(encoding='utf8').replace('Environment.GetCommandLineArgs().Contains("-captureNighthawk") ||','Environment.GetCommandLineArgs().Contains("-captureSlayerSwim") || Environment.GetCommandLineArgs().Contains("-captureNighthawk") ||');p.write_text(t,encoding='utf8')
t=(ROOT/'tools/validate-plan10-nighthawk-player.ps1').read_text(encoding='utf8').replace('Nighthawk','SlayerSwim').replace('NIGHTHAWK','SLAYER_SWIM').replace('nighthawk','slayer-swim').replace("'gear2','gear3','ignition','nitro'","'formation-member','formation-roster','command'");(ROOT/'tools/validate-plan10-slayer-swim-player.ps1').write_text(t,encoding='utf8')
print('Slayer summer Unity/player acceptance prepared.')
