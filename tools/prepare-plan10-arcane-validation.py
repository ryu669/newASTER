from pathlib import Path
R=Path(__file__).resolve().parents[1];B=R/'game/unity/Assets/Game'
def clone(path):return (B/path).read_text(encoding='utf8').replace('SlayerSwim','Arcane').replace('SLAYER_SWIM','ARCANE').replace('slayer-swim','arcane')
t=clone(Path('Scripts/Presentation/Plan10SlayerSwimAcceptance.cs'));a=t.index('            else{\n                StartBattle');b=t.index('            if(encounter!=null)',a)
t=t[:a]+'''            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyArcane();AcceptanceCheck(encounter.JobState(0).Id=="job.gunner","Arcane uses Gunner");
                if(view=="buff"){AcceptanceCheck(encounter.Act(0,1,"body"),"Original survey buff executes");ReadyArcane();}
                if(view=="attack")AcceptanceCheck(encounter.Act(0,0,"body"),"Observed adventure shot executes");
                if(view=="ultimate")AcceptanceCheck(encounter.Act(0,2,"body"),"Original all-enemy shot executes");
                if(view=="reload"){AcceptanceCheck(encounter.SelectMagazine(0,1) && encounter.Reload(0),"Magazine two reload");ReadyArcane();}
                if(view=="volley"){encounter.State.Heroes[0].GainResource(15);AcceptanceCheck(encounter.FullVolley(0,"body"),"Full volley executes");ReadyArcane();}
            }
'''+t[b:];(B/'Scripts/Presentation/Plan10ArcaneAcceptance.cs').write_text(t,encoding='utf8')
t=clone(Path('Editor/Plan10SlayerSwimBuild.cs')).replace('twelve forms / ten people','thirteen forms / eleven people');(B/'Editor/Plan10ArcaneBuild.cs').write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/PrototypeBootstrap.cs';t=p.read_text(encoding='utf8').replace('PreparePlan10SlayerSwimCapture(args);','PreparePlan10SlayerSwimCapture(args);\n            PreparePlan10ArcaneCapture(args);').replace('Environment.GetCommandLineArgs().Contains("-captureSlayerSwim")?', 'Environment.GetCommandLineArgs().Contains("-captureArcane")?"Combat/battle-plan10-arcane":Environment.GetCommandLineArgs().Contains("-captureSlayerSwim")?');p.write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/ProductionStoryExperience.cs';t=p.read_text(encoding='utf8').replace('Environment.GetCommandLineArgs().Contains("-captureSlayerSwim") ||','Environment.GetCommandLineArgs().Contains("-captureArcane") || Environment.GetCommandLineArgs().Contains("-captureSlayerSwim") ||');p.write_text(t,encoding='utf8')
t=(R/'tools/validate-plan10-slayer-swim-player.ps1').read_text(encoding='utf8').replace('SlayerSwim','Arcane').replace('SLAYER_SWIM','ARCANE').replace('slayer-swim','arcane').replace("'command'","'reload','volley'");(R/'tools/validate-plan10-arcane-player.ps1').write_text(t,encoding='utf8');print('Arcane build / source and original commands / player acceptance prepared.')
