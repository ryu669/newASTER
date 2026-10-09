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
                case "heroine.slayer":name="花翼の誓い";master="星を断つ剣";icon="sword";bonus="会心率＋5%";break;
                case "heroine.iconoclast":name="理を砕く意志";master="解放の刻印";icon="flame";bonus="攻撃＋5%";break;
                case "heroine.undermine":name="森の守り手";master="根深き生命";icon="leaf";bonus="最大HP＋5%";break;
                case "heroine.echidna":name="紅蓮の記憶";master="竜の叡智";icon="star";bonus="魔法防御＋5%";break;
                case "heroine.excalipan":name="月光の祈り";master="不屈の輪舞";icon="moon";bonus="物理防御＋5%";break;
                case "heroine.r":name="星音を聴く耳";master="ふたりの共鳴";icon="star";bonus="攻撃＋5%";break;
                case "heroine.annihilator":name="紅蝶の守り手";master="刃を置く勇気";icon="sword";bonus="会心率＋5%";break;
                case "heroine.annihilator-holy":name="聖夜の贈り手";master="黒翼の祝福";icon="leaf";bonus="最大HP＋5%";break;
                case "heroine.shell":name="支えを選ぶ翼";master="帰ってくる装甲";icon="star";bonus="物理防御＋5%";break;
                case "heroine.oriflamme":name="消えない正義の火";master="帰るための炎";icon="flame";bonus="攻撃＋5%";break;
                case "heroine.nighthawk":name="夜道を照らす灯り";master="隣を歩く勇気";icon="star";bonus="会心率＋5%";break;
                case "heroine.slayer-swim":name="白い傘を分ける翼";master="五人で帰る夏";icon="leaf";bonus="物理防御＋5%";break;
                case "heroine.arcane":name="空欄を残す探検者";master="ふたりの展示室";icon="star";bonus="物理防御＋5%";break;
                case "heroine.arcane-academy":name="正解を急がない翼";master="次の頁を選ぶ手";icon="star";bonus="魔法防御＋5%";break;
                case "heroine.shangrila":name="静かな照準";master="休息へ帰る射手";icon="star";bonus="会心率＋5%";break;
                default:throw new ArgumentException("人物固有の特性定義がありません。");
            }
            var personal=HeroinePersonalAbility.For(h.id)??throw new ArgumentException("Missing personal trait.");
            string innate=(h.traitHpPercent>0?"最大HP＋"+(FormalGrowthMath.TraitAmount(h.traitHpPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"")+(h.traitAttackPercent>0?"攻撃＋"+(FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"");
            var cards=new[]{new HeroineTraitCard{id=h.traitId,name=name,icon=icon,description=innate+"重複強化で効果量が成長します。",active=true},new HeroineTraitCard{id=h.id+".trait.personal",name=personal.Name,icon=personal.Icon,description=personal.Description,active=true},new HeroineTraitCard{id=h.id+".trait.mastery",name=master,icon="crown",description="3スキルすべてLv7で解放："+bonus+"。速度・チェイン率は変わりません。",active=HeroineTraitRules.Mastered(growth)}};
            InteractionTraitCatalog.Validate(h);
            cards=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(cards,System.Linq.Enumerable.Select(h.interactionTraitIds??Array.Empty<string>(),id=>{
                var def=InteractionTraitCatalog.Get(id);
                return new HeroineTraitCard{id=id,name=def.displayName,icon="star",description="交流特性。能力効果・重複強化なし。スキルなどの条件に使用します。",active=true};
            })));
            ValidateTraits(cards);return cards;
        }
    }
}
