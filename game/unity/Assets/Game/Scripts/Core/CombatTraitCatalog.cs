using System;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class TraitEffectDef
    {
        public string kind;
        public int[] rankValues;
        public int Value(int rank) { if(rank<1 || rank>5)throw new ArgumentOutOfRangeException(nameof(rank));return rankValues[rank-1]; }
    }
    [Serializable] public sealed class TraitDef
    {
        public string id,displayName,description,category;
        public TraitEffectDef[] effects;
    }
    [Serializable] public sealed class HeroineTraitSet
    {
        public string formId,jobId,masteryTraitId,secondTraitId;
        public string[] uniqueTraitIds,interactionTraitIds;
        public string[] VisibleIds => new[]{masteryTraitId,secondTraitId}.Concat(uniqueTraitIds).Concat(interactionTraitIds).ToArray();
    }
    // Percent, basis-point, clock, and count values are kept separate by effect kind.
    // These are provisional balance tables; IDs and slot ownership are stable.
    public static class CombatTraitCatalog
    {
        private static TraitEffectDef E(string kind,int a,int b,int c,int d,int e)=>new TraitEffectDef{kind=kind,rankValues=new[]{a,b,c,d,e}};
        private static TraitDef D(string id,string name,string text,params TraitEffectDef[] effects)=>new TraitDef{id=id,displayName=name,description=text,category=id.StartsWith("mastery.",StringComparison.Ordinal)?"mastery":"second",effects=effects};
        private static readonly TraitDef[] definitions={
            D("mastery.fighter","ファイターマスタリー","資源を消費する攻撃の威力が上昇。",E("resource-damage",5,8,11,14,18)),
            D("mastery.defender","ディフェンダーマスタリー","かばう時の被害を軽減し、反撃を強化。",E("cover-reduction",5,8,11,14,18),E("counter-damage",10,15,20,25,30)),
            D("mastery.chaser","チェイサーマスタリー","ギア2以上の攻撃とイグニッションを強化。",E("gear-damage",5,8,11,14,18),E("ignition-damage",10,15,20,25,30)),
            D("mastery.berserker","バーサーカーマスタリー","攻撃力と捕食による能力上昇量を強化。",E("attack",5,7,9,11,15),E("predation-power",1,2,3,4,5)),
            D("mastery.gunner","ガンナーマスタリー","攻撃時の追加装填率と会心率が上昇。",E("reload-bp",500,800,1100,1400,1800),E("critical-bp",200,300,400,500,700)),
            D("mastery.sniper","スナイパーマスタリー","会心率と支援・資源消費攻撃の威力が上昇。",E("critical-bp",200,300,400,500,700),E("sniper-damage",5,8,11,14,18)),
            D("mastery.blaster","ブラスターマスタリー","詠唱を短縮し、属性耐性の一部を貫通。",E("cast-reduction",5,7,9,12,15),E("resistance-penetration-bp",500,800,1100,1400,1800)),
            D("mastery.healer","ヒーラーマスタリー","追加ハートを獲得し、味方の危機に即時行動。",E("heart-bp",500,800,1100,1400,1800),E("emergency-first-bp",8000,8500,9000,9500,10000),E("emergency-repeat-bp",500,800,1100,1400,1800)),
            D("mastery.artist","アーティストマスタリー","資源消費バフの効果量を増やし、消費を軽減。",E("resource-buff",5,8,11,14,18),E("resource-discount-bp",500,800,1100,1400,1800)),
            D("mastery.gambler","ギャンブラーマスタリー","スキル威力と7の出現率が上昇。",E("skill-damage",5,8,11,14,18),E("seven-bp",100,150,200,250,300)),
            D("mastery.general","ジェネラルマスタリー","資源獲得率、陣形強化時間、自身が与える支援を強化。",E("resource-gain",5,8,11,14,18),E("empowered-clock",20,30,40,50,60),E("own-support",5,8,11,14,18),E("formation",5,8,11,14,18)),
            D("mastery.alchemist","アルケミストマスタリー","初期資源、毎100クロックの資源、資源消費効果を強化。",E("initial-resource",1,1,2,2,3),E("turn-resource",1,1,1,2,2),E("resource-effect",5,8,11,14,18)),
            D("mastery.panzer","パンツァーマスタリー","装甲HP、被害軽減、初期工具回数が上昇。",E("armor-hp",5,8,11,14,18),E("reduction",2,3,4,5,7),E("tool-uses",1,1,1,2,2)),
            D("full-power","フルパワー","HP80%以上で攻撃力が上昇。",E("healthy-attack",5,8,11,14,18)),
            D("miracle-body","ミラクルボディ","被害を軽減し、スタン耐性が上昇。",E("reduction",2,3,4,5,7),E("stun-resistance-bp",500,800,1100,1400,1800)),
            D("hunter-eye","ハンターアイ","会心率と会心威力が上昇。",E("critical-bp",200,300,400,500,700),E("critical-damage",5,8,11,14,18)),
            D("rocket-start","ロケットスタート","開幕300クロックの速度が大きく上昇。その後も小幅上昇。",E("opening-speed",15,20,25,30,35),E("speed",2,3,4,5,7)),
            D("operative","工作員","対象の状態異常の種類数に応じて与ダメージが上昇。",E("status-damage",2,3,4,5,6)),
            D("impact-resistance","衝撃耐性","斬撃・刺突・打撃・銃弾・ビームの被害を軽減。",E("physical-attribute-reduction",3,5,7,9,12)),
            D("healing-blessing","癒やしの祝福","毎100クロック、自身の最大HPに応じて回復。",E("turn-regen",1,2,3,4,5)),
            D("elemental-resistance","属性耐性","火・水・氷・風・雷・光・闇の被害を軽減。",E("magic-attribute-reduction",3,5,7,9,12)),
            D("giant-killing","ジャイアントキリング","巨神本体への与ダメージが上昇。",E("body-damage",5,8,11,14,18)),
            D("part-break","部位破壊","本体以外の部位への与ダメージが上昇。",E("part-damage",5,8,11,14,18)),
            D("boost-fighter","ブーストファイター","資源獲得時に追加資源を得る確率が上昇。",E("extra-resource-bp",500,800,1100,1400,1800)),
            D("counter-extreme","反撃の極み","反撃の威力が上昇。",E("counter-damage",10,15,20,25,30)),
            D("predator","プレデター","捕食の効果量と上限が上昇。",E("predation-power",1,2,3,4,5),E("predation-cap",1,2,3,4,5)),
            D("feuerschutz","フォイエルシュッツ","支援・資源消費攻撃の威力が上昇。",E("sniper-damage",5,8,11,14,18)),
            D("muscle-nurse","マッスルナース","攻撃力が上昇し、攻撃と回復を強化。",E("attack",5,8,11,14,18)),
            D("leader","指導者","自身のバフと通常・強化両陣形の効果量が上昇。",E("own-support",5,8,11,14,18),E("formation",5,8,11,14,18)),
            D("nanomachine-armor","ナノマシン装甲","毎100クロック、装甲のみを再生。生身は回復しない。",E("armor-regen",1,2,3,4,5))
        };
        private static readonly string[] jobs={"fighter","defender","chaser","berserker","gunner","sniper","blaster","healer","artist","gambler","general","alchemist","panzer"};
        private static readonly string[][] candidates={new[]{"boost-fighter","full-power"},new[]{"counter-extreme","miracle-body"},new[]{"operative","rocket-start"},new[]{"predator","healing-blessing"},new[]{"hunter-eye","full-power"},new[]{"feuerschutz","hunter-eye"},new[]{"elemental-resistance","full-power"},new[]{"muscle-nurse","miracle-body"},new[]{"impact-resistance","rocket-start"},new[]{"operative","rocket-start"},new[]{"leader","miracle-body"},new[]{"giant-killing","rocket-start"},new[]{"nanomachine-armor","impact-resistance"}};
        public static string[] Ids=>definitions.Select(d=>d.id).ToArray();
        public static TraitDef Get(string id)
        {
            var d=definitions.SingleOrDefault(x=>x.id==id)??throw new ArgumentException("Undefined combat trait: "+id);
            return new TraitDef{id=d.id,displayName=d.displayName,description=d.description,category=d.category,effects=d.effects.Select(e=>new TraitEffectDef{kind=e.kind,rankValues=(int[])e.rankValues.Clone()}).ToArray()};
        }
        public static int Rank(int duplicateRank) {if(duplicateRank<0 || duplicateRank>5)throw new ArgumentOutOfRangeException(nameof(duplicateRank));return Math.Min(5,duplicateRank+1);}
        public static string Mastery(string jobId) {int i=Array.IndexOf(jobs,jobId?.Replace("job.",""));if(i<0)throw new ArgumentException("Unknown mastery job.");return "mastery."+jobs[i];}
        public static string[] Candidates(string jobId) {Mastery(jobId);return (string[])candidates[Array.IndexOf(jobs,jobId.Replace("job.",""))].Clone();}
        public static HeroineTraitSet Resolve(HeroineCombatDef h)
        {
            if(h==null)throw new ArgumentNullException(nameof(h));
            if(!Candidates(h.jobId).Contains(h.secondTraitId))throw new ArgumentException("Second trait is not an authored job candidate: "+h.id);
            var known=new[]{h.traitId,h.id+".trait.personal",h.id+".trait.mastery"};
            var unique=h.uniqueTraitIds?.Length>0?h.uniqueTraitIds.ToArray():known;
            if(!unique.Contains(h.traitId) || unique.Any(id=>!known.Contains(id)) || unique.Distinct().Count()!=unique.Length)throw new ArgumentException("Unknown or duplicate authored unique trait.");
            unique=known.Where(unique.Contains).ToArray();
            var set=new HeroineTraitSet{formId=h.id,jobId=h.jobId,masteryTraitId=Mastery(h.jobId),secondTraitId=h.secondTraitId,uniqueTraitIds=unique,interactionTraitIds=(h.interactionTraitIds??Array.Empty<string>()).ToArray()};
            if(set.VisibleIds.Length>8 || set.VisibleIds.Any(string.IsNullOrWhiteSpace) || set.VisibleIds.Distinct().Count()!=set.VisibleIds.Length)throw new ArgumentException("Invalid eight-slot trait set.");
            foreach(var id in set.interactionTraitIds)InteractionTraitCatalog.Get(id);
            return set;
        }
        public static int Effect(HeroineTraitSet set,int rank,string kind)=>new[]{set.masteryTraitId,set.secondTraitId}.Sum(id=>Get(id).effects.Where(e=>e.kind==kind).Sum(e=>e.Value(rank)));
        public static string EffectLabel(TraitEffectDef effect,int rank)
        {
            var names=new System.Collections.Generic.Dictionary<string,string>{
                {"extra-resource-bp","追加資源率"},{"resource-damage","資源攻撃"},{"cover-reduction","かばう軽減"},{"counter-damage","反撃"},{"gear-damage","ギア攻撃"},{"ignition-damage","着火"},{"attack","攻撃"},{"predation-power","捕食効果"},{"reload-bp","追加装填率"},{"critical-bp","会心率"},{"sniper-damage","支援・資源攻撃"},{"cast-reduction","詠唱短縮"},{"resistance-penetration-bp","耐性貫通"},{"heart-bp","追加ハート率"},{"emergency-first-bp","初回緊急行動率"},{"emergency-repeat-bp","以後の緊急行動率"},{"resource-buff","資源バフ"},{"resource-discount-bp","資源軽減率"},{"skill-damage","スキル"},{"seven-bp","7出現率"},{"resource-gain","資源獲得"},{"empowered-clock","強化時間"},{"own-support","自身のバフ"},{"formation","陣形"},{"initial-resource","初期資源"},{"turn-resource","毎ターン資源"},{"resource-effect","資源消費効果"},{"armor-hp","装甲HP"},{"reduction","被害軽減"},{"tool-uses","初期工具"},{"healthy-attack","高HP時攻撃"},{"stun-resistance-bp","スタン耐性"},{"critical-damage","会心威力"},{"opening-speed","開幕速度"},{"speed","以後の速度"},{"status-damage","状態異常1種類につき"},{"physical-attribute-reduction","物理属性軽減"},{"magic-attribute-reduction","魔法属性軽減"},{"turn-regen","HP再生"},{"body-damage","本体攻撃"},{"part-damage","部位攻撃"},{"predation-cap","捕食上限"},{"armor-regen","装甲再生"}
            };
            string kind=effect.kind;int value=effect.Value(rank);
            return names[kind]+"＋"+(kind.EndsWith("-bp",StringComparison.Ordinal)?(value/100m).ToString("0.##")+"%":value+(kind=="empowered-clock"?"クロック":new[]{"initial-resource","turn-resource","tool-uses","predation-cap"}.Contains(kind)?"":"%"));
        }
    }
}
