from pathlib import Path
R=Path(__file__).resolve().parents[1];S=R/'game/unity/Assets/Game/Scripts/Presentation'
p=S/'BattleJobControls.cs';t=p.read_text(encoding='utf8');old='}else if(j.Id=="job.general"){';new='''}else if(j.Id=="job.gambler"){
                if(Btn(34,484,820,32,"SLOT ／ 3×3・5ライン抽選 ／ 全外れWT0",enabled,jobButton)){if(encounter.SpinGamblerSlot(actor,target)){ResetBattleMenu();QueueBattleEvents();}}
            }else if(j.Id=="job.sniper"){
                if(Btn(34,484,820,32,"狙撃モード ／ MAX消費・詠唱中は全員支援",enabled && h.JobResource==h.JobResourceMax,jobButton)){if(encounter.StartSniperMode(actor,target)){ResetBattleMenu();QueueBattleEvents();}}
            }else if(j.Id=="job.general"){'''
if 'SLOT ／ 3×3・5ライン抽選' not in t:assert old in t;t=t.replace(old,new);p.write_text(t,encoding='utf8')
print('Future Gambler / Sniper operation rows prepared; direct Gambler skill buttons remain disabled by engine.')
