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
        public bool NextAttackIsMajor => !State.Parts[0].IsBroken && State.BossGauge + 1 >= State.BossGaugeMax;
        // Temporary encounter tuning; final per-colossus action tables remain TBD.
        public bool IsEnraged => !State.IsVictory && (long)State.BossHitPoints * 2 <= State.BossMaxHitPoints;
        public string NextEnemyAction => NextAttackIsMajor
            ? (State.UltimateUnlocked ? "極大技：星還の奔流" : "大技：緑晶の嵐")
            : (IsEnraged ? "怒りの翼撃" : "翼撃");
        public bool Ended => State.IsVictory || !State.Heroes.Any(h => h.IsAlive);
        public string Log { get; private set; } = "行動者のスキルを選択。速度・待機・詠唱で行動順が変わります。";
        private readonly int[] defense;
        private readonly int[] support;
        private readonly Random random;
        private bool chainPending;
        private readonly HealingSkillDefinition[] healingSkills;
        private readonly SkillCombatDef[,] commandDefinitions;
        public int SkillResourceCost(int actor,int slot) => commandDefinitions!=null?commandDefinitions[actor,slot].resourceCost:HealingSkill(actor,slot)?.ResourceCost??(slot==0?0:3);
        public string SkillName(int actor,int slot) => commandDefinitions!=null?commandDefinitions[actor,slot].name:HealingSkill(actor,slot)?.Name??(slot==0?"通常攻撃":slot==1?"強撃":SupportName(actor));
        private bool ChainEligible(int actor,int slot) => commandDefinitions==null || commandDefinitions[actor,slot].chainEligible;
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
        public PlayableBattle(int level, PlayableProgress progress, int seed = 1, IEnumerable<HealingSkillDefinition> healingDefinitions = null, bool useTimeline = true, SkillTimingDefinition[,] skillTimings = null, IEnumerable<HeroineChainAction> heroineChainActions = null, CombatDefinitionCatalog combatDefinitions = null)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if(combatDefinitions!=null) {
                if(healingDefinitions!=null || skillTimings!=null || heroineChainActions!=null) throw new ArgumentException("Use one combat definition source, not mixed overrides.");
                combatDefinitions.Validate();healingDefinitions=combatDefinitions.Healing();skillTimings=combatDefinitions.Timings();heroineChainActions=combatDefinitions.Chain();
                commandDefinitions=new SkillCombatDef[5,3];
                for(int i=0;i<5;i++) for(int slot=0;slot<3;slot++) {
                    var s=combatDefinitions.Skill("hero-"+i,slot);
                    commandDefinitions[i,slot]=new SkillCombatDef {id=s.id,name=s.name,resourceCost=s.resourceCost,powerScale=s.powerScale,partScale=s.partScale,chainEligible=s.chainEligible,selfHealingBaseAttackPercent=s.selfHealingBaseAttackPercent,selfDamageMaxHpPercent=s.selfDamageMaxHpPercent};
                }
            }
            healingSkills=(healingDefinitions??DefaultHealingSkills()).ToArray();
            if(healingSkills.Any(d=>d==null) || healingSkills.GroupBy(d=>new {d.Actor,d.Slot}).Any(g=>g.Count()>1)) throw new ArgumentException("Duplicate or null healing definition.");
            Seed = seed;
            UsesTimeline=useTimeline;
            timings=skillTimings==null?DefaultTimings():(SkillTimingDefinition[,])skillTimings.Clone();
            if(timings.GetLength(0)!=5 || timings.GetLength(1)!=3 || timings.Cast<SkillTimingDefinition>().Any(t=>t==null)) throw new ArgumentException("Timing requires five heroes and three skills each.");
            if(healingSkills.Any(d=>timings[d.Actor,d.Slot].CastPercent>0) || Enumerable.Range(0,5).Any(i=>timings[i,2].CastPercent>0)) throw new ArgumentException("Deferred support effects are not implemented.");
            random = new Random(seed);
            chainActions=(heroineChainActions??Enumerable.Range(0,5).Select(i=>new HeroineChainAction("hero-"+i,"placeholder-chain-"+i,.6m))).ToArray();
            if(chainActions.Length!=5 || chainActions.Any(a=>a==null) || chainActions.Select(a=>a.Id).Distinct().Count()!=5 || Enumerable.Range(0,5).Any(i=>chainActions[i].HeroId!="hero-"+i)) throw new ArgumentException("Explicit chain definitions must match formation heroes.");
            defense = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 1]).ToArray();
            support = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 2]).ToArray();
            State = new BattleState(level,
                Enumerable.Range(0, 5).Select(i => new BattleHero("hero-" + i,
                    130 + progress.Levels[i] * 12 + defense[i] * 25 + progress.TraitRanks[i] * PlayableProgress.DuplicateHitPointGain,
                    20 + progress.Levels[i] * 3 + progress.Branches[i * 3] * 8 + progress.TraitRanks[i] * PlayableProgress.DuplicateAttackGain, 10, new[]{110,95,80,105,100}[i])),
                new[] { "crystal-horn-crown", "left-wing-root", "right-wing-root", "vine-wrapped-tail" }
                    .Select((id,i) => new BattlePart(id, 45 + level * 3, i == 0 ? "gauge-down" : "")),
                320 + level * 24, 4);
            BeginTurn();
            if(UsesTimeline) InitializeTimeline();
        }
        private void BeginTurn()
        {
            Array.Clear(Acted, 0, 5); Chain = 0; Guarded = false; chainPending = false;
            State.BeginTurn(unchecked(Seed + Turn * 97 + State.SelectedLevel), .25m);
            foreach (var hero in State.Heroes) hero.GainResource(3);
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
            if(target!="body") power*=commandDefinitions!=null?(decimal)commandDefinitions[heroIndex,skill].partScale:heroIndex==1?1.5m:1m;
            if (target == "body" && !State.Parts[2].IsBroken) power *= .7m;
            return power;
        }
        private BattleSkill AttackDefinition(int actor,int slot,decimal power,int cost)
        {
            var d=commandDefinitions?[actor,slot];
            return new BattleSkill(d?.id??"skill-"+slot,power,cost,d?.selfHealingBaseAttackPercent??0,d?.selfDamageMaxHpPercent??0);
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
        public int PreviewDamage(int heroIndex, int skill, string target)
        {
            if (Ended || heroIndex < 0 || heroIndex >= 5 || skill < 0 || skill > 1 || HealingSkill(heroIndex,skill)!=null || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return 0;
            if (target != "body" && !State.Parts.Any(p => p.Id == target && !p.IsBroken)) return 0;
            if (State.Heroes[heroIndex].JobResource < SkillResourceCost(heroIndex,skill)) return 0;
            int previewChain=UsesTimeline && CastDelay(heroIndex,skill)>0?1:NextChain(heroIndex);
            int damage = Math.Max(1, (int)Math.Floor(State.Heroes[heroIndex].Attack * AttackPower(heroIndex, skill, target, previewChain)));
            return Math.Min(damage, target == "body" ? State.BossHitPoints : State.Parts.First(p => p.Id == target).HitPoints);
        }
        public int PreviewEnemyDamage(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= 5) return 0;
            int damage = 12 + State.SelectedLevel * 2 + (NextAttackIsMajor ? (State.UltimateUnlocked ? 45 : 20) : 0);
            if (IsEnraged) damage = damage * 5 / 4;
            if (State.Parts[1].IsBroken) damage = damage * 3 / 4;
            if (Guarded) damage = damage * Math.Max(20, 50 - support[2] * 8) / 100;
            return Math.Max(1, damage - defense[heroIndex] * 3);
        }
        public int PreviewHealing(int actor, int ally, int slot = 1)
        {
            var d=HealingSkill(actor,slot);
            if (d==null || Ended || ally<0 || ally>=5 || Acted[actor] || !State.Heroes[actor].IsAlive || !State.Heroes[ally].IsAlive || State.Heroes[actor].JobResource<d.ResourceCost || d.TargetRule==HealingTargetRule.Self && ally!=actor) return 0;
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
            var hero = State.Heroes[heroIndex];
            var healing=HealingSkill(heroIndex,skill);
            LastFullChain=false; LastChainActionCount=0; LastChainChecks=Array.Empty<ChainConnection>();
            LastActionWasCastStart=false; LastCastResolvedActor=-1;
            if(UsesTimeline && CastDelay(heroIndex,skill)>0) return StartCasting(heroIndex,skill,target);
            if(healing!=null)
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
            else if (skill == 2)
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
                var result = BattleActionResolver.Resolve(State, hero.Id, AttackDefinition(heroIndex,skill,power,SkillResourceCost(heroIndex,skill)), target);
                if (!result.Accepted) { Log = "対象または資源を確認してください。"; return false; }
                Chain = nextChain;
                if(nextChain==1) chainMembers.Clear();
                chainMembers.Add(heroIndex);
                LastActionChain = Chain;
                decimal roll = UsesTimeline?1m:(decimal)random.NextDouble();
                chainPending = !UsesTimeline && roll < ChainRate(heroIndex);
                Log = $"{Chain} CHAIN / {result.Damage} ダメージ" + (result.PartBroken ? " / 部位破壊！" : "")
                    + (chainPending ? " / 次の攻撃へ接続" : " / チェイン終了");
                RecordPresentation(BattlePresentationKind.Attack,heroIndex,target,Log,broken:result.PartBroken,damage:result.Damage);
                RecordAttackFollowUps(heroIndex,result);
                if(UsesTimeline && ChainEligible(heroIndex,skill)) ResolveAutomaticChain(heroIndex,skillChainBonuses[skill],(bool[])cumulativeChainActors.Clone());
            }
            Acted[heroIndex] = true;
            if(UsesTimeline) { readyAt[heroIndex]=Clock+RecoveryDelay(heroIndex,skill); AvailableHero=-1; AdvanceTimeline(); }
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
        private void ResolveEnemyAction()
        {
            if(UsesTimeline) {LastFullChain=false;LastChainActionCount=0;LastActionChain=0;LastChainChecks=Array.Empty<ChainConnection>();}
            bool major = NextAttackIsMajor;
            string action = NextEnemyAction;
            for (int i = 0; i < 5; i++) State.Heroes[i].TakeDamage(PreviewEnemyDamage(i));
            State.AdvanceBossGauge(State.Parts[0].IsBroken ? 0 : 1);
            if (major) State.TryConsumeMajorGauge();
            if (!State.Parts[3].IsBroken) foreach (var hero in State.Heroes) hero.SpendResource(Math.Min(1, hero.JobResource));
            Log += "\n巨神獣の" + action + "！";
            RecordPresentation(BattlePresentationKind.Enemy,-1,"body","巨神獣の"+action+"！",major:major);
            if (!State.Heroes.Any(h => h.IsAlive)) { Log += " 育成して再挑戦できます。"; return; }
        }
    }
}
