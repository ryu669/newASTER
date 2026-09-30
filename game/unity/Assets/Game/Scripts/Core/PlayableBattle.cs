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
        public bool Ended => State.IsVictory || !State.Heroes.Any(h => h.IsAlive);
        public string Log { get; private set; } = "対象を選び、5人の行動をつないでください。";
        private readonly int[] defense;
        private readonly int[] support;
        public PlayableBattle(int level, PlayableProgress progress)
        {
            defense = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 1]).ToArray();
            support = Enumerable.Range(0, 5).Select(i => progress.Branches[i * 3 + 2]).ToArray();
            State = new BattleState(level,
                Enumerable.Range(0, 5).Select(i => new BattleHero("hero-" + i,
                    130 + progress.Levels[i] * 12 + defense[i] * 25,
                    20 + progress.Levels[i] * 3 + progress.Branches[i * 3] * 8, 10)),
                new[] { "crystal-horn-crown", "left-wing-root", "right-wing-root", "vine-wrapped-tail" }
                    .Select((id,i) => new BattlePart(id, 45 + level * 3, i == 0 ? "gauge-down" : "")),
                320 + level * 24, 4);
            BeginTurn();
        }
        private void BeginTurn()
        {
            Array.Clear(Acted, 0, 5); Chain = 0; Guarded = false;
            State.BeginTurn(Turn * 97 + State.SelectedLevel, .25m);
            foreach (var hero in State.Heroes) hero.GainResource(3);
        }
        public bool Act(int heroIndex, int skill, string target)
        {
            if (Ended || heroIndex < 0 || heroIndex > 4 || skill < 0 || skill > 2 || Acted[heroIndex] || !State.Heroes[heroIndex].IsAlive) return false;
            var hero = State.Heroes[heroIndex];
            if (skill == 2)
            {
                if (!hero.SpendResource(3)) { Log = "資源が不足しています。"; return false; }
                if (heroIndex == 3) { foreach (var h in State.Heroes) h.Heal(35 + hero.Attack + support[heroIndex] * 15); Log = "癒しの歌：生存している仲間を回復。"; }
                else { Guarded = true; Log = "護りの誓い：このターンの敵の攻撃を半減。"; }
            }
            else
            {
                decimal bonus = State.TurnChainModifiers.First(m => m.HeroId == hero.Id).AdditiveRate;
                decimal power = (skill == 1 ? 1.8m : 1m) * (1m + Chain * .15m + bonus);
                if (heroIndex == 1 && target != "body") power *= 1.5m;
                if (target == "body" && !State.Parts[2].IsBroken) power *= .7m;
                var result = BattleActionResolver.Resolve(State, hero.Id, new BattleSkill("skill-" + skill, power, skill == 1 ? 3 : 0), target);
                if (!result.Accepted) { Log = "対象または資源を確認してください。"; return false; }
                Log = $"{Chain + 1} CHAIN / {result.Damage} ダメージ" + (result.PartBroken ? " / 部位破壊！" : "");
            }
            Acted[heroIndex] = true; Chain++;
            if (!Ended && Enumerable.Range(0, 5).All(i => Acted[i] || !State.Heroes[i].IsAlive)) EndTurn();
            return true;
        }
        public void EndTurn()
        {
            if (Ended) return;
            State.AdvanceBossGauge(State.Parts[0].IsBroken ? 0 : 1);
            bool ultimate = State.TryConsumeUltimateGauge();
            int damage = 12 + State.SelectedLevel * 2 + (ultimate ? 45 : 0);
            if (State.Parts[1].IsBroken) damage = damage * 3 / 4;
            if (Guarded) damage = damage * Math.Max(20, 50 - support.Max() * 8) / 100;
            foreach (var h in State.Heroes.Select((hero, i) => new { hero, i })) h.hero.TakeDamage(Math.Max(1, damage - defense[h.i] * 3));
            if (!State.Parts[3].IsBroken) foreach (var hero in State.Heroes) hero.SpendResource(Math.Min(1, hero.JobResource));
            Log += ultimate ? "\n巨神獣の極大技！" : "\n巨神獣の反撃。";
            if (!State.Heroes.Any(h => h.IsAlive)) { Log += " 育成して再挑戦できます。"; return; }
            Turn++; BeginTurn();
        }
    }
}
