using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class PlayableBattle
    {
        public BattleState State { get; }
        public bool[] Acted { get; } = new bool[5];
        public int Turn { get; private set; } = 1;
        public int Chain { get; private set; }
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
        public static string SupportName(int heroIndex)
        {
            var names = new[] { "再起の誓い", "結晶の封印", "護りの誓い", "癒しの歌", "星の補給" };
            return heroIndex >= 0 && heroIndex < names.Length ? names[heroIndex] : "";
        }
        public string SupportDescription(int heroIndex)
        {
            switch (heroIndex) {
                case 0: return "自身を回復 " + (35 + State.Heroes[0].Attack + support[0] * 15);
                case 1: return "大技ゲージ −" + (1 + support[1] / 2);
                case 2: return "このターン全体ダメージ軽減";
                case 3: return "全体回復 " + (35 + State.Heroes[3].Attack + support[3] * 15);
                case 4: return "生存する他の仲間に資源 ＋" + (2 + support[4]);
                default: return "";
            }
        }
        public PlayableBattle(int level, PlayableProgress progress, int seed = 1)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
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
            if (Ended || heroIndex < 0 || heroIndex >= 5 || skill < 0 || skill > 1 || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return 0;
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
        public bool Act(int heroIndex, int skill, string target)
        {
            if (Ended || heroIndex < 0 || heroIndex > 4 || skill < 0 || skill > 2 || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return false;
            var hero = State.Heroes[heroIndex];
            if (skill == 2)
            {
                if (!hero.SpendResource(3)) { Log = "資源が不足しています。"; return false; }
                switch (heroIndex) {
                    case 0: hero.Heal(35 + hero.Attack + support[0] * 15); break;
                    case 1: State.ReduceBossGauge(1 + support[1] / 2); break;
                    case 2: Guarded = true; break;
                    case 3: foreach (var h in State.Heroes) h.Heal(35 + hero.Attack + support[3] * 15); break;
                    case 4: foreach (var h in State.Heroes.Where(h => h.Id != hero.Id && h.IsAlive)) h.GainResource(2 + support[4]); break;
                }
                Log = SupportName(heroIndex) + "：" + SupportDescription(heroIndex) + "。";
                Chain = 0; chainPending = false;
            }
            else
            {
                int nextChain = chainPending ? Chain + 1 : 1;
                decimal power = AttackPower(heroIndex, skill, target, nextChain);
                var result = BattleActionResolver.Resolve(State, hero.Id, new BattleSkill("skill-" + skill, power, skill == 1 ? 3 : 0), target);
                if (!result.Accepted) { Log = "対象または資源を確認してください。"; return false; }
                Chain = nextChain;
                decimal roll = (decimal)random.NextDouble();
                chainPending = roll < ChainRate(heroIndex);
                Log = $"{Chain} CHAIN / {result.Damage} ダメージ" + (result.PartBroken ? " / 部位破壊！" : "")
                    + (chainPending ? " / 次の攻撃へ接続" : " / チェイン終了");
            }
            Acted[heroIndex] = true;
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
