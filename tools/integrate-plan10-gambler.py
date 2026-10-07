from pathlib import Path
R=Path(__file__).resolve().parents[1];S=R/'game/unity/Assets/Game/Scripts/Core'
def edit(name,old,new):
 p=S/name;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(name,old);p.write_text(t.replace(old,new),encoding='utf8')
edit('PlayableBattleJobRules.cs','=> !UsesJobRulesV2 || !RequiresPanzerDefense(actor)', '=> !UsesJobRulesV2 || (!Job(actor,"gambler") || resolvingGamblerSlot && gamblerSlotActor==actor) && !RequiresPanzerDefense(actor)')
edit('PlayableBattle.cs','power*=BoostScale(heroIndex,skill);','power*=BoostScale(heroIndex,skill);\n            if(resolvingGamblerSlot && heroIndex==gamblerSlotActor)power*=gamblerSlotMultiplier;')
edit('PlayableBattle.cs','if(UsesTimeline && ChainEligible(heroIndex,skill))','if(UsesTimeline && !resolvingGamblerSlot && ChainEligible(heroIndex,skill))')
edit('PlayableBattle.cs','int statusWait=optionalResourceBoost?FinishHeroStatusAction(heroIndex,IsAttackSkill(heroIndex,skill)):0;','int statusWait=optionalResourceBoost && !resolvingGamblerSlot?FinishHeroStatusAction(heroIndex,IsAttackSkill(heroIndex,skill)):0;')
edit('PlayableBattle.cs','if(UsesTimeline) { readyAt[heroIndex]=Clock+statusWait+', 'if(UsesTimeline && !resolvingGamblerSlot) { readyAt[heroIndex]=Clock+statusWait+')
edit('PlayableBattleJobRules.cs','if(Job(actor,"general"))return', 'if(Job(actor,"gambler"))return "SLOT ／ 資源なし・3×3・5ライン"+(lastSlotSymbols.Length==9?" ／ "+string.Join(" ",lastSlotSymbols.Select(GamblerSlotRules.Label))+" ／ "+LastSlotTriggerCount+"発動":"");\n            if(Job(actor,"general"))return')
print('SLOT holds one action timeline; evaluates all five lines and applies all triggers.')
