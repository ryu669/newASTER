using System;
using System.Linq;
namespace NewAster.Core
{
    public static class HeroineSkillRules
    {
        public const int MaximumLevel=7;
        public static int UpgradeCost(int current) {if(current<1 || current>=7)throw new ArgumentOutOfRangeException(nameof(current));return 60*current;}
        public static float Multiplier(int level) {if(level<1 || level>7)throw new ArgumentOutOfRangeException(nameof(level));return 1f+.05f*(level-1);}
        // RC1 authored effects are Lv1. Original Lv2..7 improve potency only; no free timing/resource/chain gain.
        public static SkillCombatDef AtLevel(SkillCombatDef s,int level)
        {
            if(s==null)throw new ArgumentNullException(nameof(s));float scale=Multiplier(level);
            return new SkillCombatDef{id=s.id,ownerId=s.ownerId,name=s.name,effectRuleId=s.effectRuleId,targetRuleId=s.targetRuleId,resourceCost=s.resourceCost,recoveryPercent=s.recoveryPercent,castPercent=s.castPercent,targetCount=s.targetCount,baseHealing=(int)Math.Floor(s.baseHealing*scale),powerScale=s.powerScale*scale,partScale=s.partScale,chainEligible=s.chainEligible,selfHealingBaseAttackPercent=s.selfHealingBaseAttackPercent,selfDamageMaxHpPercent=s.selfDamageMaxHpPercent,criticalBonusBp=s.criticalBonusBp,damageCap=s.damageCap,damageType=s.damageType,ignoreDefenseBp=s.ignoreDefenseBp,enemyWaitAdd=s.enemyWaitAdd,selfWaitReductionPercent=s.selfWaitReductionPercent,chargeConsumeMax=s.chargeConsumeMax,chargeBonusPercent=s.chargeBonusPercent,specialWeaponBonusPercent=s.specialWeaponBonusPercent,conditions=s.conditions?.Select(x=>x.Copy()).ToArray(),statusEffects=s.statusEffects?.Select(x=>x.Copy()).ToArray(),selfEffects=s.selfEffects?.Select(x=>new TimedSelfEffectDef{kind=x.kind,turns=x.turns,percent=x.kind=="forced-target"?x.percent:Math.Min(x.kind=="critical" || x.kind=="physical-protection"?100:1000,(int)Math.Floor(x.percent*scale))}).ToArray()};
        }
        public static string Description(SkillCombatDef source,int level,JobCombatDef job)
        {
            var s=AtLevel(source,level);string result;
            string target=s.targetRuleId=="target.all-enemies"?"敵全体":s.targetRuleId=="target.enemy-range"?"選択部位と隣接部位":s.targetRuleId=="target.self"?"自身":s.targetRuleId=="target.all-living-allies"?"生存する味方全員":s.targetRuleId=="target.selected-allies"?"選択した味方":"選択した敵1体";
            if(s.effectRuleId=="effect.damage")result=target+"に攻撃力"+(s.powerScale*100).ToString("0.#")+"%の"+(s.damageType=="magic"?"魔法":"物理")+"攻撃。";
            else if(s.effectRuleId=="effect.heal")result=target+"を基本"+s.baseHealing+"＋攻撃力"+(s.powerScale*100).ToString("0.#")+"%で回復。";
            else if(s.effectRuleId=="effect.self-buff")result=string.Join("、",s.selfEffects.Select(e=>e.kind=="forced-target"?"敵の狙いを自身へ引きつける（"+e.turns+"行動）":TimedSelfEffectDef.Label(e.kind)+e.percent+"%（自身の"+e.turns+"行動）"))+"。";
            else result="味方を支援します。";
            if(s.criticalBonusBp>0)result+="会心率＋"+(s.criticalBonusBp/100f).ToString("0.#")+"%。";
            if(s.ignoreDefenseBp>0)result+="防御を"+(s.ignoreDefenseBp/100f).ToString("0.#")+"%無視。";
            if(s.enemyWaitAdd>0)result+="敵の次行動を"+s.enemyWaitAdd+"延期。";
            if(s.selfDamageMaxHpPercent>0)result+="自身の最大HP"+s.selfDamageMaxHpPercent+"%を消費。";
            if(s.selfHealingBaseAttackPercent>0)result+="自身を基礎攻撃力"+s.selfHealingBaseAttackPercent+"%回復。";
            if(s.chargeConsumeMax>0)result+="チャージを最大"+s.chargeConsumeMax+"消費し、1個につき威力＋"+s.chargeBonusPercent+"%。";
            if(s.specialWeaponBonusPercent>0)result+="特殊兵装時に威力＋"+s.specialWeaponBonusPercent+"%。";
            if(s.selfWaitReductionPercent>0)result+="自身の次の待機を"+s.selfWaitReductionPercent+"%短縮。";
            if(s.statusEffects!=null && s.statusEffects.Length>0)result+="状態蓄積："+string.Join("、",s.statusEffects.Select(e=>(e.kind=="burn"?"火傷":e.kind=="bleed"?"出血":e.kind=="poison"?"毒":e.kind=="stun"?"気絶":e.kind=="sickness"?"病気":"骨折")+e.amount))+"。";
            if(s.effectRuleId=="effect.damage" && s.selfEffects!=null && s.selfEffects.Length>0)result+="自身に"+string.Join("、",s.selfEffects.Select(e=>TimedSelfEffectDef.Label(e.kind)+e.percent+"%（"+e.turns+"行動）"))+"。";
            if(s.conditions!=null && s.conditions.Length>0)result+="発動条件："+string.Join("、",s.conditions.Select(c=>c.kind=="job-resource-at-least" || c.kind=="resource-at-least"?job.resourceName+c.threshold+"以上":c.kind=="trait-equipped"?"固有特性を所持":c.kind=="hp-at-most-percent"?"HP"+c.threshold+"%以下":c.kind=="broken-parts-at-least"?"破壊済み部位"+c.threshold+"以上":c.kind=="boss-status-active"?"敵に指定の状態異常が有効":"固有条件"))+"。";
            return result+"\n"+job.resourceName+"消費 "+s.resourceCost+" ／ 待機 "+s.recoveryPercent+"%"+(s.castPercent>0?" ／ 詠唱 "+s.castPercent+"%":"");
        }
    }
}
