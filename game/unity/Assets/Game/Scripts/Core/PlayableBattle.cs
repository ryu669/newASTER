using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed partial class PlayableBattle
    {
        public BattleState State { get; }
        public bool[] Acted { get; } = new bool[5];
        public int Turn { get; private set; } = 1;
        public int Chain { get; private set; }
        public int LastActionChain { get; private set; }
        public int RemainingActions => Enumerable.Range(0, 5).Count(i => State.Heroes[i].IsAlive && !Acted[i]);
        public bool Guarded { get; private set; }
        public const decimal BaseChainRate = .50m;
        public int Seed { get; }
        public bool NextAttackIsMajor => !RolePart("gauge",0).IsBroken && !RolePart("gauge",0).Status.Active("stun") && State.BossGauge + NextColossusGaugeGain >= State.BossGaugeMax;
        // Temporary encounter tuning; final per-colossus action tables remain TBD.
        public bool IsEnraged => !State.IsVictory && (long)State.BossHitPoints*100 <= (long)State.BossMaxHitPoints*(colossusDefinition?.enrageHpPercent??50);
        public string NextEnemyAction => NextAttackIsMajor
            ? (State.UltimateUnlocked ? colossusDefinition?.ultimateAction??"極大技：星還の奔流" : colossusDefinition?.majorAction??"大技：緑晶の嵐")
            : (NextColossusStep!=null?NextColossusStep.name+(IsEnraged?"・"+colossusDefinition.enragedAction:""):(IsEnraged ? colossusDefinition?.enragedAction??"怒りの翼撃" : colossusDefinition?.normalAction??"翼撃"));
        public bool Ended => State.IsVictory || !State.Heroes.Any(h => h.IsAlive);
        public string Log { get; private set; } = "行動者のスキルを選択。速度・待機・詠唱で行動順が変わります。";
        private readonly int[] defense;
        private readonly int[] support;
        private readonly Random random;
        private readonly ColossusCombatDef colossusDefinition;
        private readonly FormalCollectionLedger collectionGrowth;
        private readonly HomeExperienceCatalog homeCatalog;
        private readonly CollectionCatalog relicCatalog;
        private readonly FormalHomeProgress homeProgress;
        private BattlePart RolePart(string role,int legacyIndex)=>colossusDefinition==null?State.Parts[legacyIndex]:State.Parts.Single(p=>p.Id==colossusDefinition.parts.Single(d=>d.role==role).id);
        private bool chainPending;
        private bool lastEnemyWasMajor;
        private readonly HealingSkillDefinition[] healingSkills;
        private readonly SkillCombatDef[,] commandDefinitions;
        private readonly string[] formationIds;
        private readonly JobCombatDef[] jobProfiles;
        private readonly string[] heroineNames;
        public bool IsFormal => jobProfiles!=null;
        public string HeroineName(int actor) => heroineNames?[actor]??("味方"+(actor+1));
        public string ResourceName(int actor) => jobProfiles?[actor].resourceName??"資源";
        public string AttackTargetDescription(int actor,int slot) => commandDefinitions?[actor,slot].targetRuleId=="target.all-enemies"?"全体・合計":commandDefinitions?[actor,slot].targetRuleId=="target.enemy-range"?"範囲・合計":"単体";
        public string EnemyStatusDescription(string target) => State.EnemyStatus(target).Description;
        public int SkillResourceCost(int actor,int slot) => commandDefinitions!=null?(commandDefinitions[actor,slot].chargeConsumeMax>0?Math.Min(commandDefinitions[actor,slot].chargeConsumeMax,State.Heroes[actor].JobResource):commandDefinitions[actor,slot].resourceCost):HealingSkill(actor,slot)?.ResourceCost??(slot==0?0:3);
        public string SkillName(int actor,int slot) => commandDefinitions!=null?commandDefinitions[actor,slot].name:HealingSkill(actor,slot)?.Name??(slot==0?"通常攻撃":slot==1?"強撃":SupportName(actor));
        private bool ChainEligible(int actor,int slot) => commandDefinitions==null || commandDefinitions[actor,slot].chainEligible;
        public bool IsSelfBuff(int actor,int slot) => commandDefinitions?[actor,slot].effectRuleId=="effect.self-buff";
        public bool IsAttackSkill(int actor,int slot) => commandDefinitions!=null?commandDefinitions[actor,slot].effectRuleId=="effect.damage":slot<2 && HealingSkill(actor,slot)==null;
        public bool ConditionsSatisfied(int actor,int slot) => SkillConditionDef.AllSatisfied(State,actor,commandDefinitions?[actor,slot].conditions);
        public int PreviewCriticalChanceBp(int actor,int slot) => IsAttackSkill(actor,slot)?Math.Min(10000,State.Heroes[actor].CriticalChanceBp+(commandDefinitions?[actor,slot].criticalBonusBp??0)):0;
        public string SelfBuffDescription(int actor,int slot) => IsSelfBuff(actor,slot)?string.Join(" / ",commandDefinitions[actor,slot].selfEffects.Select(e=>TimedSelfEffectDef.Label(e.kind)+(e.kind=="forced-target"?"":e.percent+"%・")+e.turns+"行動")):"";
        public IReadOnlyList<int> LastHealingTargets { get; private set; } = Array.Empty<int>();
        public HealingSkillDefinition HealingSkill(int actor,int slot) => healingSkills.FirstOrDefault(x=>x.Actor==actor && x.Slot==slot);
        public static HealingSkillDefinition[] DefaultHealingSkills() => new[] {
            new HealingSkillDefinition(0,2,"再起の誓い",HealingTargetRule.Self,1,3,35,1m),
            new HealingSkillDefinition(3,1,"癒しのひと節",HealingTargetRule.SelectedAllies,1,3,35,1m),
            new HealingSkillDefinition(3,2,"癒しの歌",HealingTargetRule.AllLivingAllies,5,3,20,.6m)
        };
        public string HealingDescription(int actor,int slot)
        {
            var d=HealingSkill(actor,slot); if(d==null) return "";
            return (d.TargetRule==HealingTargetRule.Self?"自身":d.TargetRule==HealingTargetRule.AllLivingAllies?"生存する味方全員":"味方"+d.TargetCount+"人を選択")+" / 回復 "+HealingPower(d)+"ずつ";
        }
        private int HealingPower(HealingSkillDefinition d) => d.BaseHealing+(int)Math.Floor(State.Heroes[d.Actor].Attack*d.AttackScale)+support[d.Actor]*15;
        public static string SupportName(int heroIndex)
        {
            var names = new[] { "再起の誓い", "結晶の封印", "護りの誓い", "癒しの歌", "星の補給" };
            return heroIndex >= 0 && heroIndex < names.Length ? names[heroIndex] : "";
        }
        public string SupportDescription(int heroIndex)
        {
            if(HealingSkill(heroIndex,2)!=null) return HealingDescription(heroIndex,2);
            switch (heroIndex) {
                case 1: return "大技ゲージ −" + (1 + support[1] / 2);
                case 2: return "全体軽減・次の敵行動まで";
                case 4: return "他の生存者に資源 ＋" + (2 + support[4]);
                default: return "";
            }
        }
        public PlayableBattle(int level, PlayableProgress progress, int seed = 1, IEnumerable<HealingSkillDefinition> healingDefinitions = null, bool useTimeline = true, SkillTimingDefinition[,] skillTimings = null, IEnumerable<HeroineChainAction> heroineChainActions = null, CombatDefinitionCatalog combatDefinitions = null, FormalGrowthSave formalGrowth = null, ColossusCombatDef colossusDefinition = null, FormalCollectionLedger collectionGrowth = null, FormalHomeProgress homeProgress=null,HomeExperienceCatalog homeCatalog=null,CollectionCatalog relicCatalog=null)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            formalGrowth?.Validate();
            this.homeCatalog=homeCatalog;this.homeProgress=homeProgress;
            relicCatalog?.Validate();this.relicCatalog=relicCatalog?.Copy();
            if(homeProgress!=null){homeProgress.Validate();if(homeCatalog==null)throw new ArgumentException("Weapon definitions required.");homeCatalog.Validate();}
            collectionGrowth?.Validate();this.collectionGrowth=collectionGrowth;
            if(colossusDefinition!=null){colossusDefinition.Validate();this.colossusDefinition=colossusDefinition.Copy();}
            formationIds=Enumerable.Range(0,5).Select(i=>"hero-"+i).ToArray();
            if(combatDefinitions!=null) {
                if(healingDefinitions!=null || skillTimings!=null || heroineChainActions!=null) throw new ArgumentException("Use one combat definition source, not mixed overrides.");
                combatDefinitions.Validate();healingDefinitions=combatDefinitions.Healing();skillTimings=combatDefinitions.Timings();heroineChainActions=combatDefinitions.Chain();
                formationIds=combatDefinitions.FormationIds;
                heroineNames=formationIds.Select(id=>combatDefinitions.Hero(id).name).ToArray();
                if(combatDefinitions.IsFormal) {
                    if(!useTimeline) throw new ArgumentException("Formal combat requires timeline mode.");
                    jobProfiles=formationIds.Select(id=>combatDefinitions.Job(combatDefinitions.Hero(id).jobId).Copy()).ToArray();
                }
                commandDefinitions=new SkillCombatDef[5,3];
                for(int i=0;i<5;i++) for(int slot=0;slot<3;slot++) {
                    var baseSkill=combatDefinitions.Skill(formationIds[i],slot);var growth=formalGrowth?.heroines.SingleOrDefault(h=>h.heroineId==formationIds[i]);var s=HeroineSkillRules.AtLevel(baseSkill,growth?.SkillLevel(slot)??1);
                    commandDefinitions[i,slot]=new SkillCombatDef {id=s.id,name=s.name,effectRuleId=s.effectRuleId,targetRuleId=s.targetRuleId,resourceCost=s.resourceCost,powerScale=s.powerScale,partScale=s.partScale,chainEligible=s.chainEligible,selfHealingBaseAttackPercent=s.selfHealingBaseAttackPercent,selfDamageMaxHpPercent=s.selfDamageMaxHpPercent,selfEffects=s.selfEffects?.Select(e=>e.Copy()).ToArray(),criticalBonusBp=s.criticalBonusBp,damageCap=s.damageCap,conditions=s.conditions?.Select(c=>c.Copy()).ToArray(),damageType=s.damageType,ignoreDefenseBp=s.ignoreDefenseBp,statusEffects=s.statusEffects?.Select(e=>e.Copy()).ToArray(),enemyWaitAdd=s.enemyWaitAdd,selfWaitReductionPercent=s.selfWaitReductionPercent,chargeConsumeMax=s.chargeConsumeMax,chargeBonusPercent=s.chargeBonusPercent,specialWeaponBonusPercent=s.specialWeaponBonusPercent};
                }
                if(homeProgress!=null)for(int i=0;i<5;i++){var e=homeProgress.weaponEquipment.SingleOrDefault(w=>w.heroineId==formationIds[i]);if(e==null)continue;var n=homeCatalog.weaponNodes.Single(w=>w.id==e.nodeId && w.heroineId==formationIds[i]);if(!(n.abilityId=="ability.home-fixture.attack" && n.skillId=="skill.home-fixture.preview" || homeCatalog.contentVersion==HomeExperienceCatalog.ProductionVersion && n.abilityId=="ability.production.weapon-attack" && n.skillId=="skill.production.weapon-basic") || n.attackBonus<0 || float.IsNaN(n.skillPower) || n.skillPower<=0)throw new ArgumentException("Unsupported weapon effect.");commandDefinitions[i,0].powerScale=WeaponGrowthRules.Power(n,homeProgress.WeaponLevel(n.id))*HeroineSkillRules.Multiplier(formalGrowth?.heroines.Single(h=>h.heroineId==formationIds[i]).SkillLevel(0)??1);commandDefinitions[i,0].name=(n.terminal??"誓いの根");}
                healingDefinitions=combatDefinitions.Healing().Select(d=>{var g=formalGrowth?.heroines.Single(h=>h.heroineId==formationIds[d.Actor]);float m=HeroineSkillRules.Multiplier(g?.SkillLevel(d.Slot)??1);return new HealingSkillDefinition(d.Actor,d.Slot,d.Name,d.TargetRule,d.TargetCount,d.ResourceCost,(int)Math.Floor(d.BaseHealing*m),d.AttackScale*(decimal)m);}).ToArray();
            }
            healingSkills=(healingDefinitions??DefaultHealingSkills()).ToArray();
            if(healingSkills.Any(d=>d==null) || healingSkills.GroupBy(d=>new {d.Actor,d.Slot}).Any(g=>g.Count()>1)) throw new ArgumentException("Duplicate or null healing definition.");
            Seed = seed;
            UsesTimeline=useTimeline;
            if(!UsesTimeline && commandDefinitions!=null && commandDefinitions.Cast<SkillCombatDef>().Any(s=>s.effectRuleId=="effect.self-buff")) throw new ArgumentException("Timed self effects require owner-command timeline mode.");
            timings=skillTimings==null?DefaultTimings():(SkillTimingDefinition[,])skillTimings.Clone();
            if(timings.GetLength(0)!=5 || timings.GetLength(1)!=3 || timings.Cast<SkillTimingDefinition>().Any(t=>t==null)) throw new ArgumentException("Timing requires five heroes and three skills each.");
            if(healingSkills.Any(d=>timings[d.Actor,d.Slot].CastPercent>0) || Enumerable.Range(0,5).Any(i=>timings[i,2].CastPercent>0 && !IsAttackSkill(i,2))) throw new ArgumentException("Deferred support effects are not implemented.");
            random = new Random(seed);
            chainActions=(heroineChainActions??Enumerable.Range(0,5).Select(i=>new HeroineChainAction("hero-"+i,"placeholder-chain-"+i,.6m))).ToArray();
            if(chainActions.Length!=5 || chainActions.Any(a=>a==null) || chainActions.Select(a=>a.Id).Distinct().Count()!=5 || Enumerable.Range(0,5).Any(i=>chainActions[i].HeroId!=formationIds[i])) throw new ArgumentException("Explicit chain definitions must match formation heroes.");
            defense = Enumerable.Range(0, 5).Select(i => IsFormal?0:progress.Branches[i * 3 + 1]).ToArray();
            support = Enumerable.Range(0, 5).Select(i => IsFormal?0:progress.Branches[i * 3 + 2]).ToArray();
            if(formalGrowth!=null) {
                formalGrowth.Validate();
                if(!IsFormal || formationIds.Any(id=>!formalGrowth.heroines.Any(h=>h.heroineId==id))) throw new ArgumentException("Formal formation must be owned.");
                formalGrowth=formalGrowth.Copy();
            }
            State = new BattleState(level,
                Enumerable.Range(0, 5).Select(i => IsFormal?CreateFormalHero(i,combatDefinitions,formalGrowth):new BattleHero(formationIds[i],
                    130 + progress.Levels[i] * 12 + defense[i] * 25 + progress.TraitRanks[i] * PlayableProgress.DuplicateHitPointGain,
                    20 + progress.Levels[i] * 3 + progress.Branches[i * 3] * 8 + progress.TraitRanks[i] * PlayableProgress.DuplicateAttackGain, 10, new[]{110,95,80,105,100}[i])),
                colossusDefinition!=null ? colossusDefinition.parts.Select(p=>new BattlePart(p.id,checked(p.baseHp+level*p.hpPerLevel),p.breakEffect,combatDefinitions?.enemyPhysicalDefense??0,combatDefinitions?.enemyMagicDefense??0,combatDefinitions?.enemyStatusResistances,p.role)) : new[] { "crystal-horn-crown", "left-wing-root", "right-wing-root", "vine-wrapped-tail" }
                    .Select((id,i) => new BattlePart(id, (IsFormal?300:45) + level * (IsFormal?12:3), i == 0 ? "gauge-down" : "",combatDefinitions?.enemyPhysicalDefense??0,combatDefinitions?.enemyMagicDefense??0,combatDefinitions?.enemyStatusResistances)),
                colossusDefinition!=null?checked(colossusDefinition.baseHp+level*colossusDefinition.hpPerLevel):(IsFormal?1500:320) + level * (IsFormal?120:24), colossusDefinition?.gaugeMax??4,combatDefinitions?.enemyPhysicalDefense??0,combatDefinitions?.enemyMagicDefense??0,combatDefinitions?.enemyStatusResistances);
            BeginTurn();
            if(UsesTimeline) InitializeTimeline();
        }
        private BattleHero CreateFormalHero(int actor,CombatDefinitionCatalog catalog,FormalGrowthSave growth)
        {
            var h=catalog.Hero(formationIds[actor]);var j=jobProfiles[actor];
            if(growth!=null) {
                var g=growth.heroines.Single(x=>x.heroineId==h.id);
                int hp=FormalGrowthMath.Stat(j.hp,h.hpBp,g.level,g.duplicateRank);
                int attack=FormalGrowthMath.Stat(j.attack,h.attackBp,g.level,g.duplicateRank);
                var weapon=homeProgress?.weaponEquipment.SingleOrDefault(e=>e.heroineId==h.id);if(weapon!=null)attack=checked(attack+WeaponGrowthRules.Attack(homeCatalog.weaponNodes.Single(n=>n.id==weapon.nodeId),homeProgress.WeaponLevel(weapon.nodeId)));
                var relic=FormalRelicRules.Equipped(collectionGrowth,h.id);
                if(relic!=null){var def=relicCatalog?.relics.Single(r=>r.id==relic.id);hp=checked((int)((long)(hp+FormalRelicRules.Hp(relic))*(100+(def?.hpPercent??0))/100));attack=checked((int)((long)(attack+FormalRelicRules.Attack(relic))*(100+(def?.attackPercent??5))/100));}
                int hpTrait=h.traitHpPercent==0?0:FormalGrowthMath.TraitAmount(h.traitHpPercent*100,g.duplicateRank);
                int attackTrait=h.traitAttackPercent==0?0:FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,g.duplicateRank);
                hp=(int)((long)hp*(100+HeroineTraitRules.MasteryBonus(j.id,"hp",g))/100);attack=(int)((long)attack*(100+HeroineTraitRules.MasteryBonus(j.id,"attack",g))/100);
                int physical=j.defense==0?0:FormalGrowthMath.Stat(j.defense,h.defenseBp,g.level,g.duplicateRank),magic=j.magicDefense==0?0:FormalGrowthMath.Stat(j.magicDefense,h.defenseBp,g.level,g.duplicateRank);
                physical=(int)((long)physical*(100+HeroineTraitRules.MasteryBonus(j.id,"physical-defense",g))/100);magic=(int)((long)magic*(100+HeroineTraitRules.MasteryBonus(j.id,"magic-defense",g))/100);
                return new BattleHero(h.id,(int)((long)hp*(10000+hpTrait)/10000),(int)((long)attack*(10000+attackTrait)/10000),j.resourceMax,FormalGrowthMath.Speed(j.speed,h.speedBp),j.criticalBp+HeroineTraitRules.MasteryBonus(j.id,"critical",g),physical,magic,h.traitId);
            }
            // Lv1 without growth is reserved for definition regression tests.
            return new BattleHero(h.id,(int)((long)j.hp*h.hpBp*(100+h.traitHpPercent)/1000000),(int)((long)j.attack*h.attackBp*(100+h.traitAttackPercent)/1000000),j.resourceMax,(int)((long)j.speed*h.speedBp/10000),j.criticalBp,(int)((long)j.defense*h.defenseBp/10000),j.magicDefense,h.traitId);
        }
        private void BeginTurn()
        {
            Array.Clear(Acted, 0, 5); Chain = 0; Guarded = false; chainPending = false;
            State.BeginTurn(unchecked(Seed + Turn * 97 + State.SelectedLevel), .25m);
            for(int i=0;i<5;i++) State.Heroes[i].GainResource(IsFormal?jobProfiles[i].initialResource:3);
        }
        public decimal ChainRate(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= 5) return 0m;
            if(UsesTimeline) return BaseChainRate+(HasCumulativeChainBonus(heroIndex)?.05m:0m);
            // Legacy validation-only mode is isolated from the current automatic chain.
            return Math.Min(1m, .65m + State.TurnChainModifiers[heroIndex].AdditiveRate);
        }
        private decimal AttackPower(int heroIndex, int skill, string target, int chain)
        {
            decimal power = (commandDefinitions!=null?(decimal)commandDefinitions[heroIndex,skill].powerScale:skill == 1 ? 1.8m : 1m) * (1m + Math.Max(0, chain - 1) * .15m);
            var d=commandDefinitions?[heroIndex,skill];
            if(d!=null && d.chargeConsumeMax>0) power*=1m+Math.Min(d.chargeConsumeMax,State.Heroes[heroIndex].JobResource)*d.chargeBonusPercent/100m;
            if(d!=null && d.specialWeaponBonusPercent>0 && State.Heroes[heroIndex].JobResource>=6) power+=d.specialWeaponBonusPercent/100m;
            if(d!=null && (d.targetRuleId=="target.all-enemies" || d.targetRuleId=="target.enemy-range")) return power;
            if(target!="body") power*=commandDefinitions!=null?(decimal)commandDefinitions[heroIndex,skill].partScale:heroIndex==1?1.5m:1m;
            if (target == "body" && !RolePart("armor",2).IsBroken && !RolePart("armor",2).Status.Active("stun")) power *= .7m;
            return power;
        }
        private BattleSkill AttackDefinition(int actor,int slot,decimal power,int cost,int? attackSnapshot=null)
        {
            var d=commandDefinitions?[actor,slot];
            bool multiple=d!=null && (d.targetRuleId=="target.all-enemies" || d.targetRuleId=="target.enemy-range");
            return new BattleSkill(d?.id??"skill-"+slot,power,cost,d?.selfHealingBaseAttackPercent??0,d?.selfDamageMaxHpPercent??0,attackSnapshot,PreviewCriticalChanceBp(actor,slot),State.Heroes[actor].CriticalMultiplierPercent,d?.damageCap??0,string.IsNullOrEmpty(d?.damageType)?"physical":d.damageType,d?.ignoreDefenseBp??0,d?.targetRuleId??"target.selected-enemy",d?.statusEffects,multiple?(decimal)d.partScale:1m,multiple);
        }
        public string AttackFollowUpDescription(int actor,int slot)
        {
            var d=commandDefinitions?[actor,slot];if(d==null) return "";
            return (d.selfHealingBaseAttackPercent>0?" / 攻撃後に自己回復（基礎攻撃の"+d.selfHealingBaseAttackPercent+"%）":"")+
                (d.selfDamageMaxHpPercent>0?" / 行動後反動（最大HPの"+d.selfDamageMaxHpPercent+"%）":"");
        }
        private void RecordAttackFollowUps(int actor,BattleActionResult outcome)
        {
            LastHealingTargets=outcome.SelfHealing>0?Array.AsReadOnly(new[]{actor}):Array.Empty<int>();
            if(outcome.SelfHealing>0) {
                string message="攻撃後の自己回復 / HP ＋"+outcome.SelfHealing;Log+="\n"+message;
                RecordPresentation(BattlePresentationKind.Healing,actor,"body",message,healingTargets:new[]{actor});
            }
            if(outcome.SelfDamage>0) {
                string message="行動後の反動 / HP −"+outcome.SelfDamage;Log+="\n"+message;
                RecordPresentation(BattlePresentationKind.Support,actor,"body",message,targetIds:new[]{State.Heroes[actor].Id});
            }
        }
        private bool ApplyAttackTimedEffects(int actor,int slot,bool completeCommand)
        {
            var effects=commandDefinitions?[actor,slot].selfEffects;
            if(effects==null || effects.Length==0) return false;
            if(completeCommand) State.Heroes[actor].CompleteOwnerCommand();
            if(State.Heroes[actor].ApplySelfEffects(effects))
                RecordPresentation(BattlePresentationKind.Support,actor,"body","攻撃後の自己効果 / "+string.Join(" / ",effects.Select(e=>TimedSelfEffectDef.Label(e.kind)+e.percent+"・"+e.turns+"行動")),targetIds:new[]{State.Heroes[actor].Id},standalone:true);
            return true;
        }
        public int PreviewDamage(int heroIndex, int skill, string target)
        {
            if (Ended || heroIndex < 0 || heroIndex >= 5 || skill < 0 || skill > 2 || !IsAttackSkill(heroIndex,skill) || !ConditionsSatisfied(heroIndex,skill) || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return 0;
            var targets=EnemyAttackTargets.Resolve(State,commandDefinitions?[heroIndex,skill].targetRuleId??"target.selected-enemy",target);
            if(targets.Length==0) return 0;
            if (State.Heroes[heroIndex].JobResource < SkillResourceCost(heroIndex,skill)) return 0;
            int previewChain=UsesTimeline && CastDelay(heroIndex,skill)>0?1:NextChain(heroIndex);
            var attack=AttackDefinition(heroIndex,skill,AttackPower(heroIndex,skill,target,previewChain),0);
            return (int)Math.Min(int.MaxValue,targets.Sum(t=>(long)Math.Min(BattleActionResolver.CalculateDamage(State,State.Heroes[heroIndex],attack,t),t=="body"?State.BossHitPoints:State.Parts.First(p=>p.Id==t).HitPoints)));
        }
        public int PreviewEnemyDamage(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= 5) return 0;
            int damage = (colossusDefinition?.baseDamage??12) + State.SelectedLevel * (colossusDefinition?.damagePerLevel??2) + (NextAttackIsMajor ? (State.UltimateUnlocked ? colossusDefinition?.ultimateBonus??45 : colossusDefinition?.majorBonus??20) : 0);
            if(!NextAttackIsMajor && NextColossusStep!=null)damage=(int)Math.Min(int.MaxValue,(long)damage*NextColossusStep.damagePercent/100);
            if (IsEnraged) damage=(int)Math.Min(int.MaxValue,(long)damage*(colossusDefinition?.enrageDamagePercent??125)/100);
            if (RolePart("attack",1).IsBroken || RolePart("attack",1).Status.Active("stun")) damage=(int)((long)damage*(colossusDefinition?.attackBreakDamagePercent??75)/100);
            if (Guarded) damage = damage * Math.Max(20, 50 - support[2] * 8) / 100;
            if(IsFormal) {
                // The preview encounter's major move is magic; normal wing attacks are physical.
                bool magic=NextAttackIsMajor?(colossusDefinition?.majorDamageType??"magic")=="magic":NextColossusStep?.damageType=="magic";
                if(State.BossStatus.Active("sickness") || RolePart("attack",1).Status.Active("sickness")) damage=damage*80/100;
                int protection=magic?State.Heroes[heroIndex].MagicDefense:State.Heroes[heroIndex].PhysicalDefense;
                damage=(int)((long)damage*1000/(1000L+protection));
                return magic?Math.Max(1,damage):State.Heroes[heroIndex].ProtectPhysicalDamage(Math.Max(1,damage));
            }
            return State.Heroes[heroIndex].ProtectPhysicalDamage(Math.Max(1, damage - defense[heroIndex] * 3));
        }
        public int PreviewHealing(int actor, int ally, int slot = 1)
        {
            var d=HealingSkill(actor,slot);
            if (d==null || Ended || ally<0 || ally>=5 || !ConditionsSatisfied(actor,slot) || Acted[actor] || !State.Heroes[actor].IsAlive || !State.Heroes[ally].IsAlive || State.Heroes[actor].JobResource<d.ResourceCost || d.TargetRule==HealingTargetRule.Self && ally!=actor) return 0;
            return Math.Min(State.Heroes[ally].MaxHitPoints-State.Heroes[ally].HitPoints,HealingPower(d));
        }
        public bool CanHeal(int ally) => PreviewHealing(3, ally) > 0;
        public int[] HealingTargets(int actor,int slot,IEnumerable<int> selected)
        {
            var d=HealingSkill(actor,slot); if(d==null) return Array.Empty<int>();
            if(d.TargetRule==HealingTargetRule.Self) return new[]{actor};
            if(d.TargetRule==HealingTargetRule.AllLivingAllies) return Enumerable.Range(0,5).Where(i=>State.Heroes[i].IsAlive).ToArray();
            return (selected??Array.Empty<int>()).ToArray();
        }
        public bool CanHealTargets(int actor,int slot,IEnumerable<int> selected)
        {
            var d=HealingSkill(actor,slot); if(d==null) return false;
            var targets=HealingTargets(actor,slot,selected);
            if(targets.Length==0 || targets.Distinct().Count()!=targets.Length || targets.Any(i=>i<0 || i>=5 || !State.Heroes[i].IsAlive)) return false;
            if(d.TargetRule==HealingTargetRule.SelectedAllies && (targets.Length!=d.TargetCount || targets.Any(i=>PreviewHealing(actor,i,slot)==0))) return false;
            return targets.Any(i=>PreviewHealing(actor,i,slot)>0);
        }
        public bool Act(int heroIndex, int skill, string target, int allyTarget = -1)
            => ActWithAllies(heroIndex,skill,target,allyTarget<0?Array.Empty<int>():new[]{allyTarget});
        public bool ActWithAllies(int heroIndex,int skill,string target,IEnumerable<int> selectedAllies)
        {
            if (Ended || heroIndex < 0 || heroIndex > 4 || skill < 0 || skill > 2 || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return false;
            if(!ConditionsSatisfied(heroIndex,skill)) {Log="スキルの使用条件を満たしていません。";return false;}
            var hero = State.Heroes[heroIndex];
            var healing=HealingSkill(heroIndex,skill);
            LastFullChain=false; LastChainActionCount=0; LastChainChecks=Array.Empty<ChainConnection>();
            LastActionWasCastStart=false; LastCastResolvedActor=-1;
            if(UsesTimeline && CastDelay(heroIndex,skill)>0) return StartCasting(heroIndex,skill,target);
            bool selfBuff=IsSelfBuff(heroIndex,skill);
            bool commandCompleted=false;
            if(selfBuff)
            {
                if(!hero.SpendResource(SkillResourceCost(heroIndex,skill))) {Log="資源が不足しています。";return false;}
                Chain=0;chainPending=false;LastActionChain=0;chainMembers.Clear();LastHealingTargets=Array.Empty<int>();
                Log=SkillName(heroIndex,skill)+"："+SelfBuffDescription(heroIndex,skill);
            }
            else if(healing!=null)
            {
                var targets=HealingTargets(heroIndex,skill,selectedAllies);
                if(!CanHealTargets(heroIndex,skill,targets)) { Log="スキルに必要な回復対象を確認してください。"; return false; }
                var amounts=targets.Select(i=>PreviewHealing(heroIndex,i,skill)).ToArray();
                if(!hero.SpendResource(healing.ResourceCost)) return false;
                for(int i=0;i<targets.Length;i++) State.Heroes[targets[i]].Heal(amounts[i]);
                LastHealingTargets=Array.AsReadOnly(targets);
                Log=healing.Name+"："+string.Join(" / ",targets.Select((t,i)=>"味方"+(t+1)+" HP ＋"+amounts[i]));
                Chain=0; chainPending=false; LastActionChain=0;
                chainMembers.Clear();
                RecordPresentation(BattlePresentationKind.Healing,heroIndex,"body",Log,healingTargets:targets);
            }
            else if (skill == 2 && !IsAttackSkill(heroIndex,skill))
            {
                if(heroIndex==0 || heroIndex==3) { Log="支援スキルの定義がありません。"; return false; }
                if (!hero.SpendResource(SkillResourceCost(heroIndex,skill))) { Log = "資源が不足しています。"; return false; }
                switch (heroIndex) {
                    case 1: State.ReduceBossGauge(1 + support[1] / 2); break;
                    case 2: Guarded = true; break;
                    case 4: foreach (var h in State.Heroes.Where(h => h.Id != hero.Id && h.IsAlive)) h.GainResource(2 + support[4]); break;
                }
                Log = SkillName(heroIndex,skill) + "：" + SupportDescription(heroIndex) + "。";
                Chain = 0; chainPending = false;
                LastActionChain = 0;
                chainMembers.Clear();
                RecordPresentation(BattlePresentationKind.Support,heroIndex,"body",Log);
                LastHealingTargets=Array.Empty<int>();
            }
            else
            {
                int nextChain = NextChain(heroIndex);
                decimal power = AttackPower(heroIndex, skill, target, nextChain);
                var result = BattleActionResolver.Resolve(State, hero.Id, AttackDefinition(heroIndex,skill,power,SkillResourceCost(heroIndex,skill)), target,max=>random.Next(max));
                if (!result.Accepted) { Log = "対象または資源を確認してください。"; return false; }
                Chain = nextChain;
                if(nextChain==1) chainMembers.Clear();
                chainMembers.Add(heroIndex);
                LastActionChain = Chain;
                decimal roll = UsesTimeline?1m:(decimal)random.NextDouble();
                chainPending = !UsesTimeline && roll < ChainRate(heroIndex);
                Log = $"{Chain} CHAIN / {result.Damage} ダメージ" + (result.Critical?" / CRITICAL":"")+(result.CriticalRoll>=0?"（会心判定 "+result.CriticalRoll+"）":"")+(result.PartBroken ? " / 部位破壊！" : "")
                    + (chainPending ? " / 次の攻撃へ接続" : " / チェイン終了");
                RecordPresentation(BattlePresentationKind.Attack,heroIndex,target,Log,broken:result.PartBroken,damage:result.Damage,targetIds:result.TargetIds);
                RecordAttackFollowUps(heroIndex,result);
                ApplyCommandAttackEffects(heroIndex,skill);
                commandCompleted=ApplyAttackTimedEffects(heroIndex,skill,true);
                if(UsesTimeline && ChainEligible(heroIndex,skill)) ResolveAutomaticChain(heroIndex,skillChainBonuses[skill],(bool[])cumulativeChainActors.Clone());
            }
            bool hadEffects=hero.TimedEffects.Count>0;
            if(!commandCompleted) hero.CompleteOwnerCommand();
            if(hadEffects && !selfBuff && !commandCompleted) RecordPresentation(BattlePresentationKind.Support,heroIndex,"body","持続効果の残り行動を更新。",targetIds:new[]{hero.Id},standalone:true);
            if(selfBuff) {
                hero.ApplySelfEffects(commandDefinitions[heroIndex,skill].selfEffects);
                RecordPresentation(BattlePresentationKind.Support,heroIndex,"body",Log,targetIds:new[]{hero.Id});
            }
            Acted[heroIndex] = true;
            if(UsesTimeline) { readyAt[heroIndex]=Clock+(IsAttackSkill(heroIndex,skill)?CommandRecoveryDelay(heroIndex,skill):RecoveryDelay(heroIndex,skill)); AvailableHero=-1; AdvanceTimeline(); }
            else if (!Ended && Enumerable.Range(0, 5).All(i => Acted[i] || !State.Heroes[i].IsAlive)) EndTurn();
            return true;
        }
        public void EndTurn()
        {
            if (Ended) return;
            if(UsesTimeline) { Pass(); return; }
            ResolveEnemyAction();
            if (!State.Heroes.Any(h => h.IsAlive)) return;
            Turn++; BeginTurn();
        }
        public Action CompletedEnemyAction {get;set;}
        private void ResolveEnemyAction()
        {
            if(UsesTimeline) {LastFullChain=false;LastChainActionCount=0;LastActionChain=0;LastChainChecks=Array.Empty<ChainConnection>();}
            lastEnemyWasMajor=false;
            lastColossusWaitPercent=100;
            if(IsFormal) {
                long dot=0;bool dotBroken=false;var dotTargets=new List<string>();
                int bodyDot=State.BossStatus.Dot(State.BossMaxHitPoints);if(bodyDot>0) {dot+=State.ApplyBossDamage(bodyDot);dotTargets.Add("body");}
                foreach(var part in State.Parts.Where(p=>!p.IsBroken)) {int damage=part.Status.Dot(part.MaxHitPoints);if(damage>0) {dot+=Math.Min(part.HitPoints,damage);dotBroken=State.BreakPart(part.Id,damage)||dotBroken;dotTargets.Add(part.Id);}}
                if(dotTargets.Count>0) RecordPresentation(BattlePresentationKind.Support,-1,"body","状態異常の継続ダメージ",damage:(int)Math.Min(int.MaxValue,dot),broken:dotBroken,targetIds:dotTargets,standalone:true);
                if(State.IsVictory) return;
                if(State.BossStatus.Active("stun")) {TickEnemyStatuses();Log+="\n巨神獣はスタンで行動不能。";RecordPresentation(BattlePresentationKind.Enemy,-1,"body","スタン：巨神獣行動を一回阻止",standalone:true);return;}
            }
            bool major=NextAttackIsMajor;string action=NextEnemyAction;lastEnemyWasMajor=major;
            var step=NextColossusStep;int gaugeGain=NextColossusGaugeGain;lastColossusWaitPercent=step?.waitPercent??100;
            long actualDamage=0;var damagedHeroes=new List<string>();
            foreach(int i in NextEnemyTargets) {
                int beforeHp=State.Heroes[i].HitPoints;
                int damage=PreviewEnemyDamage(i);State.Heroes[i].TakeDamage(damage);
                int lost=beforeHp-State.Heroes[i].HitPoints;if(lost>0){actualDamage+=lost;damagedHeroes.Add(State.Heroes[i].Id);}
                if(IsFormal && damage>0 && State.Heroes[i].IsAlive) State.Heroes[i].GainResource(jobProfiles[i].gainOnHit);
            }
            State.AdvanceBossGauge(RolePart("gauge",0).IsBroken || RolePart("gauge",0).Status.Active("stun") ? 0 : gaugeGain);
            if (major) State.TryConsumeMajorGauge();
            if (!RolePart("drain",3).IsBroken && !RolePart("drain",3).Status.Active("stun")) foreach (var hero in State.Heroes.Where(h=>h.IsAlive)) hero.SpendResource(Math.Min(step?.drainAmount??1, hero.JobResource));
            if(IsFormal) TickEnemyStatuses();
            Log += "\n巨神獣の" + action + "！";
            RecordPresentation(BattlePresentationKind.Enemy,-1,"body","巨神獣の"+action+"！",major:major,damage:(int)Math.Min(int.MaxValue,actualDamage),targetIds:damagedHeroes);
            CompletedEnemyAction?.Invoke();
            if (!State.Heroes.Any(h => h.IsAlive)) { Log += " 育成して再挑戦できます。"; return; }
        }
        private void TickEnemyStatuses() {State.BossStatus.Tick();foreach(var part in State.Parts) part.Status.Tick();}
        private void ApplyCommandAttackEffects(int actor,int slot)
        {
            if(!IsFormal) return;
            var d=commandDefinitions[actor,slot];int before=State.Heroes[actor].JobResource;State.Heroes[actor].GainResource(jobProfiles[actor].gainOnAttack);
            bossAt+=d.enemyWaitAdd;
            if(before!=State.Heroes[actor].JobResource || d.enemyWaitAdd>0) RecordPresentation(BattlePresentationKind.Support,actor,"body",ResourceName(actor)+" ＋"+(State.Heroes[actor].JobResource-before)+(d.enemyWaitAdd>0?" / 巨神獣の待機 ＋"+d.enemyWaitAdd:""),targetIds:new[]{State.Heroes[actor].Id},standalone:true);
        }
        private long CommandRecoveryDelay(int actor,int slot) => Math.Max(1,RecoveryDelay(actor,slot)*(100-(commandDefinitions?[actor,slot].selfWaitReductionPercent??0))/100);
    }
}
