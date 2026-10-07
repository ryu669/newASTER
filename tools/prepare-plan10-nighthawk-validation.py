from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'game/unity/Assets/Game'
t=(BASE/'Scripts/Presentation/Plan10OriflammeAcceptance.cs').read_text(encoding='utf8').replace('Oriflamme','Nighthawk').replace('ORIFLAMME','NIGHTHAWK').replace('oriflamme','nighthawk')
start=t.index('            else{\n                StartBattle');end=t.index('            if(encounter!=null)',start)
t=t[:start]+'''            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyNighthawk();AcceptanceCheck(encounter.JobState(0).Id=="job.chaser","Nighthawk owns chaser job");
                if(view=="gear2" || view=="gear3")AcceptanceCheck(encounter.SelectChaserGear(0,view=="gear2"?2:3),"Gear selection succeeds");
                if(view=="buff"){AcceptanceCheck(encounter.Act(0,0,"body"),"Attack and speed buff executes");ReadyNighthawk();}
                if(view=="attack")AcceptanceCheck(encounter.Act(0,1,"body"),"Defense ignoring frost attack executes");
                if(view=="ultimate")AcceptanceCheck(encounter.Act(0,2,"body"),"All enemy burn and frost attack executes");
                if(view=="ignition"){encounter.State.BossStatus.AddIgnition();encounter.State.BossStatus.AddIgnition();AcceptanceCheck(encounter.Act(0,1,"body"),"Third ignition flag triggers reaction");}
                if(view=="nitro"){
                    // Isolate the Chaser cycle from the encounter's independent resource-drain part.
                    encounter.State.BreakPart(encounter.State.Parts.Single(p=>p.Role=="drain").Id,int.MaxValue);
                    for(int n=0;n<5;n++){ReadyNighthawk();int guard=0;while(encounter.State.Heroes[0].JobResource<2 && !encounter.Ended && guard++<40){encounter.Pass();ReadyNighthawk();}AcceptanceCheck(encounter.SelectChaserGear(0,2) && encounter.Act(0,0,"body"),"Paid gear fills nitro");}
                    ReadyNighthawk();long clock=encounter.Clock;AcceptanceCheck(encounter.SelectChaserNitro(0) && encounter.Act(0,0,"body"),"NITRO executes");
                    AcceptanceCheck(encounter.Clock==clock && encounter.AvailableHero==0 && encounter.JobState(0).NitroCount==0,"NITRO consumes five and returns immediately to READY");
                }
            }
'''+t[end:]
(BASE/'Scripts/Presentation/Plan10NighthawkAcceptance.cs').write_text(t,encoding='utf8')
t=(BASE/'Editor/Plan10OriflammeBuild.cs').read_text(encoding='utf8').replace('Oriflamme','Nighthawk').replace('ORIFLAMME','NIGHTHAWK').replace('oriflamme','nighthawk').replace('ten forms / nine people','eleven forms / ten people')
(BASE/'Editor/Plan10NighthawkBuild.cs').write_text(t,encoding='utf8')
t=(ROOT/'tools/validate-plan10-oriflamme-player.ps1').read_text(encoding='utf8').replace('Oriflamme','Nighthawk').replace('ORIFLAMME','NIGHTHAWK').replace('oriflamme','nighthawk').replace("'input','alchemy','weakness'","'gear2','gear3','ignition','nitro'")
(ROOT/'tools/validate-plan10-nighthawk-player.ps1').write_text(t,encoding='utf8')
t=(ROOT/'tools/adopt-plan10-oriflamme-art.py').read_text(encoding='utf8').replace('oriflamme','nighthawk')
(ROOT/'tools/adopt-plan10-nighthawk-art.py').write_text(t,encoding='utf8')
print('Nighthawk build and player acceptance prepared.')
