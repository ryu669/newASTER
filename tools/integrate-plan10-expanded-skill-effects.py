"""Source-observed Academy/Shangrila effects; defaults preserve existing content."""
from pathlib import Path
R=Path(__file__).resolve().parents[1];S=R/'game/unity/Assets/Game/Scripts/Core'
def edit(name,old,new):
 p=S/name;t=p.read_text(encoding='utf-8-sig')
 if new not in t:assert old in t,(name,old);p.write_text(t.replace(old,new),encoding='utf8')
edit('CombatDefinitionCatalog.cs','public int enemyAttackReductionPercent,enemyAttackReductionTurns;','public int enemyAttackReductionPercent,enemyAttackReductionTurns;\n        public int bodyDamageBonusPercent,enemyStatusExtensionTurns,alliesEffectExtensionTurns;\n        public string[] enemyStatusExtensionKinds;\n        public TimedSelfEffectDef[] postAttackAlliesEffects;')
edit('CombatDefinitionCatalog.cs','TimedSelfEffectDef.ValidateAll(skill.selfEffects);','TimedSelfEffectDef.ValidateAll(skill.selfEffects);\n                ExtendedSkillEffects.Validate(skill);')
fields='bodyDamageBonusPercent=s.bodyDamageBonusPercent,enemyStatusExtensionTurns=s.enemyStatusExtensionTurns,alliesEffectExtensionTurns=s.alliesEffectExtensionTurns,enemyStatusExtensionKinds=s.enemyStatusExtensionKinds?.ToArray(),postAttackAlliesEffects=s.postAttackAlliesEffects?.Select(e=>e.Copy()).ToArray(),'
for name in ['PlayableBattle.cs','HeroineSkillRules.cs']:edit(name,'enemyAttackReductionPercent=s.enemyAttackReductionPercent,',fields+'enemyAttackReductionPercent=s.enemyAttackReductionPercent,')
edit('BattleActionResolver.cs','public int IgnoreDefenseBp { get; }','public int IgnoreDefenseBp { get; }\n        public int BodyDamageBonusPercent {get;}')
edit('BattleActionResolver.cs','bool bodyPartProtection=false,string[] attributes=null)','bool bodyPartProtection=false,string[] attributes=null,int bodyDamageBonusPercent=0)')
edit('BattleActionResolver.cs','PerTargetPartScale=perTargetPartScale;BodyPartProtection=bodyPartProtection;','if(bodyDamageBonusPercent<0 || bodyDamageBonusPercent>200)throw new ArgumentOutOfRangeException(nameof(bodyDamageBonusPercent));\n            BodyDamageBonusPercent=bodyDamageBonusPercent;PerTargetPartScale=perTargetPartScale;BodyPartProtection=bodyPartProtection;')
edit('BattleActionResolver.cs','if(part!=null) raw*=skill.PerTargetPartScale;','if(targetId=="body")raw*=1m+skill.BodyDamageBonusPercent/100m;\n            if(part!=null) raw*=skill.PerTargetPartScale;')
edit('PlayableBattle.cs','multiple,d?.attributes);','multiple,d?.attributes,d?.bodyDamageBonusPercent??0);')
edit('TimedSelfEffects.cs','kind!="attack" && kind!=','kind!="attack" && kind!="attack-reduction" && kind!=')
edit('TimedSelfEffects.cs','kind=="attack"?"攻撃＋":','kind=="attack"?"攻撃＋":kind=="attack-reduction"?"攻撃−":')
edit('BattleState.cs','100+EffectPercent("attack")+JobAllStatsPercent','Math.Max(1,100+EffectPercent("attack")-EffectPercent("attack-reduction")+JobAllStatsPercent')
# Closing Math.Max before the percentage factor, without changing old arithmetic.
edit('BattleState.cs','JobAttackPercent+GeneralAttackPercent)/100','JobAttackPercent+GeneralAttackPercent))/100')
edit('PlayableBattle.cs','private void ApplyCommandAttackEffects(int actor,int slot)\n        {','private void ApplyCommandAttackEffects(int actor,int slot)\n        {\n            ApplyExtendedSkillEffects(actor,slot);')
edit('HeroineSkillRules.cs','if(s.statusEffects!=null && s.statusEffects.Length>0)result+=','if(s.bodyDamageBonusPercent>0)result+="巨神獣本体に威力＋"+s.bodyDamageBonusPercent+"%。";\n            if(s.enemyStatusExtensionTurns>0)result+="対象の有効な毒・火傷・出血を"+s.enemyStatusExtensionTurns+"ターン延長。";\n            if(s.alliesEffectExtensionTurns>0)result+="味方全員の有効な強化を"+s.alliesEffectExtensionTurns+"ターン延長。";\n            if(s.postAttackAlliesEffects!=null && s.postAttackAlliesEffects.Length>0)result+="攻撃後、味方全員に"+string.Join("、",s.postAttackAlliesEffects.Select(e=>TimedSelfEffectDef.Label(e.kind)+e.percent+"%（"+e.turns+"ターン）"))+"。";\n            if(s.statusEffects!=null && s.statusEffects.Length>0)result+=')
print('Extended source skill effects wired; prior fields default to zero.')
