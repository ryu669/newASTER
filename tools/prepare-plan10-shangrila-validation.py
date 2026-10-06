from pathlib import Path
R=Path(__file__).resolve().parents[1];B=R/'game/unity/Assets/Game'
def clone(path):return (B/path).read_text(encoding='utf8').replace('Arcane','Shangrila').replace('ARCANE','SHANGRILA').replace('arcane','shangrila')
t=clone(Path('Scripts/Presentation/Plan10ArcaneAcceptance.cs'));a=t.index('            else{\n                StartBattle');b=t.index('            if(encounter!=null)',a)
t=t[:a]+'''            else if(view.StartsWith("formation-support",StringComparison.Ordinal)){
                formationOpen=true;formationSlot=0;formationLayer=1;var operation=new HomeOperation("sniper-support",hero,save.home.formationIds[2]);ProposeHome(operation);
                if(view=="formation-support-pending"){
                    AcceptanceCheck(formalCampaign.CommitHomeOperation(homeRequest,HomeData(),homeOperation,s=>false)==GrowthCommitResult.SaveFailed,"Support failure preserves current deployment");homeError="保存できませんでした。支援対象は変更していません。";
                }
                if(view=="formation-support-saved"){
                    ConfirmHome();AcceptanceCheck(HomeState.sniperSupports.Single(s=>s.heroineId==hero).targetId==save.home.formationIds[2],"Selected support commits through production UI");
                    AcceptanceCheck(acceptanceStore.Load(out var restored)==FormalLoadResult.Loaded,"Support save reloads");BindFormalCampaign(restored);
                }
            }
            else if(view=="formation-weapon-return"){
                formationSlot=0;returnToFormationFromWeapon=true;formationOpen=false;growthScreen=GrowthScreen.Weapons;ReturnFromFormationWeapon();AcceptanceCheck(formationOpen && formationLayer==1 && formationSlot==0,"Weapon return preserves selected member and hierarchy");
            }
            else{
                StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyShangrila();AcceptanceCheck(encounter.JobState(0).Id=="job.sniper","Shangrila uses Sniper");
                if(view=="recoil")AcceptanceCheck(encounter.Act(0,0,"body"),"Observed recoil shot executes");
                if(view=="heal"){encounter.State.Heroes[0].TakeDamage(100);AcceptanceCheck(encounter.Act(0,1,"body"),"Observed bleeding heal shot executes");}
                if(view=="ultimate")AcceptanceCheck(encounter.Act(0,2,"body"),"Observed negative-effect ultimate executes");
                if(view=="support"){
                    while(encounter.AvailableHero!=1 && !encounter.Ended)encounter.Pass();AcceptanceCheck(encounter.Act(1,0,"body"),"Selected ally triggers support shot");
                }
                if(view=="sniper-mode" || view=="cast-release" || view=="cast-cancel"){
                    encounter.State.Heroes[0].GainResource(15);string selected=view=="cast-cancel"?encounter.State.Parts.First(p=>!p.IsBroken).Id:"body";
                    AcceptanceCheck(encounter.StartSniperMode(0,selected),"Sniper mode begins");
                    if(view=="cast-cancel")encounter.State.BreakPart(selected,int.MaxValue);
                    if(view=="sniper-mode")AcceptanceCheck(encounter.IsSniping(0),"Casting supports all allies");else ReadyShangrila();
                }
                if(!encounter.Ended && view!="sniper-mode")ReadyShangrila();
            }
'''+t[b:];t=t.replace('battlePanel=view=="status"?', 'battlePanel=view=="status" || view=="sniper-mode"?');(B/'Scripts/Presentation/Plan10ShangrilaAcceptance.cs').write_text(t,encoding='utf8')
t=clone(Path('Editor/Plan10ArcaneBuild.cs')).replace('thirteen forms / eleven people','fifteen forms / twelve people / thirteen jobs');(B/'Editor/Plan10ShangrilaBuild.cs').write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/PrototypeBootstrap.cs';t=p.read_text(encoding='utf8').replace('PreparePlan10ArcaneAcademyCapture(args);','PreparePlan10ArcaneAcademyCapture(args);\n            PreparePlan10ShangrilaCapture(args);').replace('Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy")?', 'Environment.GetCommandLineArgs().Contains("-captureShangrila")?"Combat/battle-plan10-shangrila":Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy")?');p.write_text(t,encoding='utf8')
p=B/'Scripts/Presentation/ProductionStoryExperience.cs';t=p.read_text(encoding='utf8').replace('Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy") ||','Environment.GetCommandLineArgs().Contains("-captureShangrila") || Environment.GetCommandLineArgs().Contains("-captureArcaneAcademy") ||');p.write_text(t,encoding='utf8')
t=(R/'tools/validate-plan10-arcane-player.ps1').read_text(encoding='utf8').replace('Arcane','Shangrila').replace('ARCANE','SHANGRILA').replace('arcane','shangrila').replace("'reload','volley','buff','ultimate','attack'","'recoil','heal','ultimate','support','sniper-mode','cast-release','cast-cancel','formation-support-confirm','formation-support-pending','formation-support-saved','formation-weapon-return'");(R/'tools/validate-plan10-shangrila-player.ps1').write_text(t,encoding='utf8')
print('Shangrila build / source skills / mode / support save failure and retry / hierarchy return prepared.')
