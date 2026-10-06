from pathlib import Path
R=Path(__file__).resolve().parents[1];S=R/'game/unity/Assets/Game/Scripts/Core'
def edit(name,old,new):
 p=S/name;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(name,old);p.write_text(t.replace(old,new),encoding='utf8')
edit('PlayableBattle.cs','InitializeGeneral(deployment,combatDefinitions);','InitializeGeneral(deployment,combatDefinitions);InitializeSniperSupport(deployment);')
edit('PlayableBattle.cs','alchemyHitTargets=outcome.TargetIds.ToArray();','alchemyHitTargets=outcome.TargetIds.ToArray();\n            SniperSupportReaction(actor,outcome.TargetIds.ToArray());')
edit('PlayableBattleTiming.cs','public int Slot; public string Target;','public int Slot; public bool Sniper; public string Target;')
edit('PlayableBattleTiming.cs','casting[actor]=null;\n                    // An already broken','casting[actor]=null;\n                    if(pending.Sniper){ResolveSniperMode(actor,pending);continue;}\n                    // An already broken')
edit('PlayableBattleChain.cs','if(effect.TargetIds.Count>0 && def.Effect==ChainEffect.Damage)ChaserIgnition(actor,effect.TargetIds);','if(effect.TargetIds.Count>0 && def.Effect==ChainEffect.Damage){ChaserIgnition(actor,effect.TargetIds);SniperSupportReaction(actor,effect.TargetIds.ToArray());}')
edit('PlayableBattleJobRules.cs','if(Job(actor,"general") && actor==CommanderActor)','if(Job(actor,"sniper"))State.Heroes[actor].GainResource(3);\n            if(Job(actor,"general") && actor==CommanderActor)')
edit('PlayableBattleJobRules.cs','if(Job(actor,"gambler"))return','if(Job(actor,"sniper"))return "狙撃 "+h.JobResource+"/"+h.JobResourceMax+(IsSniping(actor)?" ／ 詠唱中・全員支援":" ／ 支援対象："+HeroineName(sniperTargets[actor]));\n            if(Job(actor,"gambler"))return')
print('Sniper deployment / nonrecursive support / cast transition integrated.')
