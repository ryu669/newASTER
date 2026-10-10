using System;
using NewAster.Core;
namespace NewAster.Data
{
    public sealed class HeroineTraitCard {public string id,name,icon,description;public bool active;}
    public static class HeroineIdentityCatalog
    {
        public static void ValidateTraits(HeroineTraitCard[] cards)
        {if(cards==null || cards.Length==0 || cards.Length>HeroinePersonalAbility.MaximumTraits || System.Linq.Enumerable.Any(cards,c=>c==null || string.IsNullOrEmpty(c.id) || string.IsNullOrEmpty(c.name) || string.IsNullOrEmpty(c.description)) || System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(cards,c=>c.id)))!=cards.Length)throw new ArgumentException("A heroine needs one to eight unique authored traits.");}
        public static string JobName(string id)=>id=="job.fighter"?"ファイター":id=="job.berserker"?"バーサーカー":id=="job.defender"?"ディフェンダー":id=="job.blaster"?"ブラスター":id=="job.gunner"?"ガンナー":id=="job.artist"?"アーティスト":id=="job.healer"?"ヒーラー":id=="job.panzer"?"パンツァー":id=="job.alchemist"?"アルケミスト":id=="job.chaser"?"チェイサー":id=="job.general"?"ジェネラル":id=="job.gambler"?"ギャンブラー":id=="job.sniper"?"スナイパー":id.Replace("job.","");
        public static HeroineTraitCard[] Traits(HeroineCombatDef h,FormalHeroineGrowth growth)
        {
            string name,master,icon,bonus;
            switch(h.id){
                case "heroine.slayer":icon="sword";bonus="会心率＋5%";break;
                case "heroine.iconoclast":icon="flame";bonus="攻撃＋5%";break;
                case "heroine.undermine":icon="leaf";bonus="最大HP＋5%";break;
                case "heroine.echidna":icon="star";bonus="魔法防御＋5%";break;
                case "heroine.excalipan":icon="moon";bonus="物理防御＋5%";break;
                case "heroine.r":icon="star";bonus="攻撃＋5%";break;
                case "heroine.annihilator":icon="sword";bonus="会心率＋5%";break;
                case "heroine.annihilator-holy":icon="leaf";bonus="最大HP＋5%";break;
                case "heroine.shell":icon="star";bonus="物理防御＋5%";break;
                case "heroine.oriflamme":icon="flame";bonus="攻撃＋5%";break;
                case "heroine.nighthawk":icon="star";bonus="会心率＋5%";break;
                case "heroine.slayer-swim":icon="leaf";bonus="物理防御＋5%";break;
                case "heroine.arcane":icon="star";bonus="物理防御＋5%";break;
                case "heroine.arcane-academy":icon="star";bonus="魔法防御＋5%";break;
                case "heroine.shangrila":icon="star";bonus="会心率＋5%";break;
                default:throw new ArgumentException("人物固有の特性定義がありません。");
            }
            var authored=HeroineAuthoredNames.For(h.id);name=authored.Innate;master=authored.Mastery;
            var personal=HeroinePersonalAbility.For(h.id)??throw new ArgumentException("Missing personal trait.");
            string innate=(h.traitHpPercent>0?"最大HP＋"+(FormalGrowthMath.TraitAmount(h.traitHpPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"")+(h.traitAttackPercent>0?"攻撃＋"+(FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"");
            var cards=new[]{new HeroineTraitCard{id=h.traitId,name=name,icon=icon,description=authored.Flavor+"\n\n"+innate,active=true},new HeroineTraitCard{id=h.id+".trait.personal",name=personal.Name,icon=personal.Icon,description=authored.Flavor+"\n\n"+personal.Description,active=true},new HeroineTraitCard{id=h.id+".trait.mastery",name=master,icon="crown",description=authored.Flavor+"\n\n3スキルすべてLv7で解放："+bonus+"。",active=HeroineTraitRules.Mastered(growth)}};
            InteractionTraitCatalog.Validate(h);
            if(!string.IsNullOrEmpty(h.secondTraitId) && h.uniqueTraitIds?.Length>0)cards=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(cards,card=>System.Linq.Enumerable.Contains(h.uniqueTraitIds,card.id)));
            if(!string.IsNullOrEmpty(h.secondTraitId))cards=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(System.Linq.Enumerable.Select(new[]{CombatTraitCatalog.Mastery(h.jobId),h.secondTraitId},id=>{
                var def=CombatTraitCatalog.Get(id);int rank=CombatTraitCatalog.Rank(growth.duplicateRank);
                return new HeroineTraitCard{id=id,name=def.displayName,icon="star",description=authored.Flavor+"\n\nRank "+rank+" / 5　"+def.description+"\n"+string.Join(" / ",System.Linq.Enumerable.Select(def.effects,e=>CombatTraitCatalog.EffectLabel(e,rank))),active=true};
            }),cards));
            cards=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(cards,System.Linq.Enumerable.Select(h.interactionTraitIds??Array.Empty<string>(),id=>{
                var def=InteractionTraitCatalog.Get(id);
                return new HeroineTraitCard{id=id,name=def.displayName,icon="star",description=InteractionTraitCatalog.Flavor(id),active=true};
            })));
            ValidateTraits(cards);return cards;
        }
    }
}
