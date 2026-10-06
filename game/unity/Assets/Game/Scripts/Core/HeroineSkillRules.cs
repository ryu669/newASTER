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
            return new SkillCombatDef{id=s.id,ownerId=s.ownerId,name=s.name,effectRuleId=s.effectRuleId,targetRuleId=s.targetRuleId,resourceCost=s.resourceCost,recoveryPercent=s.recoveryPercent,castPercent=s.castPercent,targetCount=s.targetCount,resourceGain=s.resourceGain,cleanseAll=s.cleanseAll,alliesHealingBaseAttackPercent=s.alliesHealingBaseAttackPercent,baseHealing=(int)Math.Floor(s.baseHealing*scale),powerScale=s.powerScale*scale,partScale=s.partScale,chainEligible=s.chainEligible,selfHealingBaseAttackPercent=s.selfHealingBaseAttackPercent,selfDamageMaxHpPercent=s.selfDamageMaxHpPercent,targetStatusBonusKind=s.targetStatusBonusKind,targetStatusBonusPercent=s.targetStatusBonusPercent,enemyFireVulnerabilityPercent=s.enemyFireVulnerabilityPercent,enemyFireVulnerabilityTurns=s.enemyFireVulnerabilityTurns,allAlchemistResourceGain=s.allAlchemistResourceGain,criticalBonusBp=s.criticalBonusBp,chainChanceBonusBp=s.chainChanceBonusBp,damageCap=s.damageCap,damageType=s.damageType,attributes=s.attributes?.ToArray(),ignoreDefenseBp=s.ignoreDefenseBp,enemyWaitAdd=s.enemyWaitAdd,selfWaitReductionPercent=s.selfWaitReductionPercent,chargeConsumeMax=s.chargeConsumeMax,chargeBonusPercent=s.chargeBonusPercent,specialWeaponBonusPercent=s.specialWeaponBonusPercent,conditions=s.conditions?.Select(x=>x.Copy()).ToArray(),statusEffects=s.statusEffects?.Select(x=>x.Copy()).ToArray(),selfEffects=s.selfEffects?.Select(x=>new TimedSelfEffectDef{kind=x.kind,turns=x.turns,percent=x.kind=="forced-target"?x.percent:Math.Min(x.kind=="critical" || x.kind=="physical-protection"?100:1000,(int)Math.Floor(x.percent*scale))}).ToArray()};
        }
        public static string Description(SkillCombatDef source,int level,JobCombatDef job)
        {
            var s=AtLevel(source,level);string result;
            string target=s.targetRuleId=="target.all-enemies"?"敵全体":s.targetRuleId=="target.enemy-range"?"選択部位と隣接部位":s.targetRuleId=="target.self"?"自身":s.targetRuleId=="target.all-living-allies"?"生存する味方全員":s.targetRuleId=="target.selected-allies"?"選択した味方":"選択した敵1体";
            if(s.effectRuleId=="effect.damage")result=target+"に攻撃力"+(s.powerScale*100).ToString("0.#")+"%の"+(s.damageType=="magic"?"魔法":"物理")+"攻撃。";
            else if(s.effectRuleId=="effect.heal")result=target+"を基本"+s.baseHealing+"＋攻撃力"+(s.powerScale*100).ToString("0.#")+"%で回復。";
            else if(s.effectRuleId=="effect.self-buff" || s.effectRuleId=="effect.allies-buff")result=(s.effectRuleId=="effect.allies-buff"?target+"に":"自身に")+string.Join("、",s.selfEffects.Select(e=>e.kind=="forced-target"?"敵の狙いを自身へ引きつける（"+e.turns+"行動）":TimedSelfEffectDef.Label(e.kind)+e.percent+"%"+(s.effectRuleId=="effect.allies-buff"?"（"+e.turns+"Battle Turn）":"（"+e.turns+"Battle Turn）")))+"。";
            else result="味方を支援します。";
            if(s.enemyFireVulnerabilityPercent>0)result+="対象に火弱点＋"+s.enemyFireVulnerabilityPercent+"%（"+s.enemyFireVulnerabilityTurns+"Battle Turn）。";
            if(s.allAlchemistResourceGain>0)result+="生存するアルケミスト全員の錬成資源＋"+s.allAlchemistResourceGain+"。";
            if(s.resourceGain>0)result+=job.resourceName+"＋"+s.resourceGain+"。";
            if(s.cleanseAll)result+="対象の状態異常を解除。";
            if(s.alliesHealingBaseAttackPercent>0)result+="攻撃後、味方全体を基礎攻撃力"+s.alliesHealingBaseAttackPercent+"%回復。";
            if(s.chainChanceBonusBp>0)result+="自動チェイン接続率＋"+(s.chainChanceBonusBp/100f).ToString("0.#")+"%。";
            if(s.criticalBonusBp>0)result+="会心率＋"+(s.criticalBonusBp/100f).ToString("0.#")+"%。";
            if(s.targetStatusBonusPercent>0)result+=EnemyStatusState.Label(s.targetStatusBonusKind)+"中の選択対象に威力＋"+s.targetStatusBonusPercent+"%。";
            if(s.ignoreDefenseBp>0)result+="防御を"+(s.ignoreDefenseBp/100f).ToString("0.#")+"%無視。";
            if(s.enemyWaitAdd>0)result+="敵の次行動を"+s.enemyWaitAdd+"延期。";
            if(s.selfDamageMaxHpPercent>0)result+="自身の最大HP"+s.selfDamageMaxHpPercent+"%を消費。";
            if(s.selfHealingBaseAttackPercent>0)result+="自身を基礎攻撃力"+s.selfHealingBaseAttackPercent+"%回復。";
            if(s.chargeConsumeMax>0)result+="任意の強化ではチャージを最大"+s.chargeConsumeMax+"消費し、1個につき威力＋"+s.chargeBonusPercent+"%。";
            if(s.specialWeaponBonusPercent>0)result+="特殊兵装時に威力＋"+s.specialWeaponBonusPercent+"%。";
            if(s.selfWaitReductionPercent>0)result+="自身の次の待機を"+s.selfWaitReductionPercent+"%短縮。";
            if(s.statusEffects!=null && s.statusEffects.Length>0)result+="状態蓄積："+string.Join("、",s.statusEffects.Select(e=>EnemyStatusState.Label(e.kind)+e.amount))+"。";
            if(s.effectRuleId=="effect.damage" && s.selfEffects!=null && s.selfEffects.Length>0)result+="自身に"+string.Join("、",s.selfEffects.Select(e=>TimedSelfEffectDef.Label(e.kind)+e.percent+"%（"+e.turns+"行動）"))+"。";
            if(s.conditions!=null && s.conditions.Length>0)result+="発動条件："+string.Join("、",s.conditions.Select(c=>c.kind=="job-resource-at-least" || c.kind=="resource-at-least"?job.resourceName+c.threshold+"以上":c.kind=="trait-equipped"?"固有特性を所持":c.kind=="hp-at-most-percent"?"HP"+c.threshold+"%以下":c.kind=="broken-parts-at-least"?"破壊済み部位"+c.threshold+"以上":c.kind=="boss-status-active"?"敵に指定の状態異常が有効":"固有条件"))+"。";
            string resource=job.id=="job.chaser"?"毎Battle Turnに駆動＋2。GEAR2/3で待機短縮、NITRO5で次スキルWT0、対象Flag3で全体攻撃":job.id=="job.alchemist"?"通常行動で錬成＋1。5属性の錬成はREADYを消費しません":job.id=="job.panzer"?"装甲中のみ使用可。装甲HPは通常回復不可、ツールで修復。生身は防御のみ":job.id=="job.healer"?"生命操作は3スキルと別に選択。生命投資で上限を増やせます":job.id=="job.artist"?"通常行動で歌唱ゲージ＋2。歌唱中はスキル不可、毎Battle Turnに2消費":job.resourceName+"は任意消費で効果強化（1個+10%）。標準発動は消費なし";
            return result+"\n属性："+CombatAttributeRules.Labels(s.attributes)+"\n"+resource+" ／ 待機 "+s.recoveryPercent+"%"+(s.castPercent>0?" ／ 詠唱 "+s.castPercent+"%":"");
        }
    }
}
