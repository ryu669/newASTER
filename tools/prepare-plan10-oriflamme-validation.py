from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'game/unity/Assets/Game'
t=(BASE/'Scripts/Presentation/Plan10ShellAcceptance.cs').read_text(encoding='utf8')
start=t.index('            else if(view=="formation"');end=t.index('            else if(view=="journey")',start)
t=t[:start]+'            else if(view=="formation")formationOpen=true;\n'+t[end:]
start=t.index('            else{\n                StartBattle');end=t.index('            if(encounter!=null)',start)
t=t[:start]+'''            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShell();AcceptanceCheck(encounter.JobState(0).Id=="job.alchemist","Oriflamme owns alchemist job");
                if(view=="input"){Array.Copy(new[]{2,2,1,0,0},alchemyUnits,5);AcceptanceCheck(encounter.CanTransmute(0,alchemyUnits,"body"),"Five-attribute input preview is executable");}
                if(view=="attack")AcceptanceCheck(encounter.Act(0,0,"body"),"Fire attack executes");
                if(view=="buff"){AcceptanceCheck(encounter.Act(0,1,"body"),"Allies fire amplification executes");ReadyShell();}
                if(view=="ultimate"){encounter.State.BossStatus.Add(new EnemyStatusDef{kind="burn",amount=1000});AcceptanceCheck(encounter.Act(0,2,"body"),"Burning target ultimate executes");}
                if(view=="alchemy" || view=="weakness"){
                    long clock=encounter.Clock;int actor=encounter.AvailableHero,resource=encounter.State.Heroes[0].JobResource;
                    AcceptanceCheck(encounter.Transmute(0,view=="alchemy"?new[]{2,2,1,0,0}:new[]{2,0,0,1,0},"body",view=="weakness"),"Alchemy executes");
                    AcceptanceCheck(encounter.Clock==clock && encounter.AvailableHero==actor && encounter.State.Heroes[0].JobResource==resource-(view=="alchemy"?5:3),"Alchemy spends exactly once and preserves READY");
                }
            }
'''+t[end:]
t=t.replace('Shell','Oriflamme').replace('SHELL','ORIFLAMME').replace('shell','oriflamme')
(BASE/'Scripts/Presentation/Plan10OriflammeAcceptance.cs').write_text(t,encoding='utf8')
t=(BASE/'Editor/Plan10ShellBuild.cs').read_text(encoding='utf8').replace('Shell','Oriflamme').replace('SHELL','ORIFLAMME').replace('shell','oriflamme').replace('!=19','!=18').replace('19 distinct','18 distinct').replace('nine forms / eight people','ten forms / nine people').replace('nineteen images','eighteen images')
(BASE/'Editor/Plan10OriflammeBuild.cs').write_text(t,encoding='utf8')
t=(ROOT/'tools/validate-plan10-shell-player.ps1').read_text(encoding='utf-8-sig').replace('Shell','Oriflamme').replace('SHELL','ORIFLAMME').replace('shell','oriflamme').replace("'setup','setup-pending','setup-save',",'').replace("'bare','defense','call','repair','guard',","'input','alchemy','weakness','buff','ultimate',")
(ROOT/'tools/validate-plan10-oriflamme-player.ps1').write_text(t,encoding='utf8')
print('Build and 22-view player acceptance prepared. Visible interactive previews are required for OnGUI captures.')
