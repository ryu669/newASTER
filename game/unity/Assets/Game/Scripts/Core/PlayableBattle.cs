using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed class PlayableBattle
    {
        public BattleState State { get; }
        public bool[] Acted { get; } = new bool[5];
        public int Turn { get; private set; } = 1;
        public int Chain { get; private set; }
        public int LastActionChain { get; private set; }
        public int RemainingActions => Enumerable.Range(0, 5).Count(i => State.Heroes[i].IsAlive && !Acted[i]);
        public bool Guarded { get; private set; }
        public const decimal BaseChainRate = .65m;
        public int Seed { get; }
        public bool NextAttackIsMajor => !State.Parts[0].IsBroken && State.BossGauge + 1 >= State.BossGaugeMax;
        // Temporary encounter tuning; final per-colossus action tables remain TBD.
        public bool IsEnraged => !State.IsVictory && (long)State.BossHitPoints * 2 <= State.BossMaxHitPoints;
        public string NextEnemyAction => NextAttackIsMajor
            ? (State.UltimateUnlocked ? "極大技：星還の奔流" : "大技：緑晶の嵐")
            : (IsEnraged ? "怒りの翼撃" : "翼撃");
        public bool Ended => State.IsVictory || !State.Heroes.Any(h => h.IsAlive);
        public string Log { get; private set; } = "対象を選び、5人の行動をつないでください。";
        private readonly int[] defense;
        private readonly int[] support;
        private readonly Random random;
        private bool chainPending;
        private readonly HealingSkillDefinition[] healingSkills;
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
                case 2: return "全体軽減・このターン";
                case 4: return "他の生存者に資源 ＋" + (2 + support[4]);
                default: return "";
            }
        }
        public PlayableBattle(int level, PlayableProgress progress, int seed = 1, IEnumerable<HealingSkillDefinition> healingDefinitions = null)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            healingSkills=(healingDefinitions??DefaultHealingSkills()).ToArray();
            if(healingSkills.Any(d=>d==null) || healingSkills.GroupBy(d=>new {d.Actor,d.Slot}).Any(g=>g.Count()>1)) throw new ArgumentException("Duplicate or null healing definition.");
            Seed = seed;
            random = new Random(seed);
            defense = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 1]).ToArray();
            support = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 2]).ToArray();
            State = new BattleState(level,
                Enumerable.Range(0, 5).Select(i => new BattleHero("hero-" + i,
                    130 + progress.Levels[i] * 12 + defense[i] * 25 + progress.TraitRanks[i] * PlayableProgress.DuplicateHitPointGain,
                    20 + progress.Levels[i] * 3 + progress.Branches[i * 3] * 8 + progress.TraitRanks[i] * PlayableProgress.DuplicateAttackGain, 10)),
                new[] { "crystal-horn-crown", "left-wing-root", "right-wing-root", "vine-wrapped-tail" }
                    .Select((id,i) => new BattlePart(id, 45 + level * 3, i == 0 ? "gauge-down" : "")),
                320 + level * 24, 4);
            BeginTurn();
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
            return Math.Min(1m, BaseChainRate + State.TurnChainModifiers[heroIndex].AdditiveRate);
        }
        private decimal AttackPower(int heroIndex, int skill, string target, int chain)
        {
            decimal power = (skill == 1 ? 1.8m : 1m) * (1m + Math.Max(0, chain - 1) * .15m);
            if (heroIndex == 1 && target != "body") power *= 1.5m;
            if (target == "body" && !State.Parts[2].IsBroken) power *= .7m;
            return power;
        }
        public int PreviewDamage(int heroIndex, int skill, string target)
        {
            if (Ended || heroIndex < 0 || heroIndex >= 5 || skill < 0 || skill > 1 || HealingSkill(heroIndex,skill)!=null || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return 0;
            if (target != "body" && !State.Parts.Any(p => p.Id == target && !p.IsBroken)) return 0;
            if (State.Heroes[heroIndex].JobResource < (skill == 1 ? 3 : 0)) return 0;
            int damage = Math.Max(1, (int)Math.Floor(State.Heroes[heroIndex].Attack * AttackPower(heroIndex, skill, target, chainPending ? Chain + 1 : 1)));
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
            }
            else if (skill == 2)
            {
                if(heroIndex==0 || heroIndex==3) { Log="支援スキルの定義がありません。"; return false; }
                if (!hero.SpendResource(3)) { Log = "資源が不足しています。"; return false; }
                switch (heroIndex) {
                    case 1: State.ReduceBossGauge(1 + support[1] / 2); break;
                    case 2: Guarded = true; break;
                    case 4: foreach (var h in State.Heroes.Where(h => h.Id != hero.Id && h.IsAlive)) h.GainResource(2 + support[4]); break;
                }
                Log = SupportName(heroIndex) + "：" + SupportDescription(heroIndex) + "。";
                Chain = 0; chainPending = false;
                LastActionChain = 0;
            }
            else
            {
                int nextChain = chainPending ? Chain + 1 : 1;
                decimal power = AttackPower(heroIndex, skill, target, nextChain);
                var result = BattleActionResolver.Resolve(State, hero.Id, new BattleSkill("skill-" + skill, power, skill == 1 ? 3 : 0), target);
                if (!result.Accepted) { Log = "対象または資源を確認してください。"; return false; }
                Chain = nextChain;
                LastActionChain = Chain;
                decimal roll = (decimal)random.NextDouble();
                chainPending = roll < ChainRate(heroIndex);
                Log = $"{Chain} CHAIN / {result.Damage} ダメージ" + (result.PartBroken ? " / 部位破壊！" : "")
                    + (chainPending ? " / 次の攻撃へ接続" : " / チェイン終了");
            }
            Acted[heroIndex] = true;
            if(healing==null) LastHealingTargets=Array.Empty<int>();
            if (!Ended && Enumerable.Range(0, 5).All(i => Acted[i] || !State.Heroes[i].IsAlive)) EndTurn();
            return true;
        }
        public void EndTurn()
        {
            if (Ended) return;
            bool major = NextAttackIsMajor;
            string action = NextEnemyAction;
            for (int i = 0; i < 5; i++) State.Heroes[i].TakeDamage(PreviewEnemyDamage(i));
            State.AdvanceBossGauge(State.Parts[0].IsBroken ? 0 : 1);
            if (major) State.TryConsumeMajorGauge();
            if (!State.Parts[3].IsBroken) foreach (var hero in State.Heroes) hero.SpendResource(Math.Min(1, hero.JobResource));
            Log += "\n巨神獣の" + action + "！";
            if (!State.Heroes.Any(h => h.IsAlive)) { Log += " 育成して再挑戦できます。"; return; }
            Turn++; BeginTurn();
        }
    }
}
