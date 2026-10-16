using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class HeroinePersonalTrait
    {
        public string Name {get;} public string Icon {get;} public string Description {get;}
        public string Kind {get;} public int Percent {get;}
        public HeroinePersonalTrait(string name,string icon,string description,string kind,int percent)
        {Name=name;Icon=icon;Description=description;Kind=kind;Percent=percent;}
    }
    public static class HeroinePersonalAbility
    {
        public const int MaximumTraits=8;
        public static HeroinePersonalTrait For(string id)
        {
            switch(id){
                case "heroine.iron-maiden":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.iron-maiden").Personal,"moon","自身が受ける攻撃ダメージを8%軽減。","reduction",8);
                case "heroine.avalanche":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.avalanche").Personal,"flame","弾丸の追加獲得率＋15%。","gauge",15);
                case "heroine.anathema":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.anathema").Personal,"sword","HPが50%以下のとき、受ける攻撃ダメージを15%軽減。","wounded-defense",15);
                case "heroine.shiva":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.shiva").Personal,"leaf","毎ターン、自身の最大HPの2%を回復。","regen",2);
                case "heroine.aeon":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.aeon").Personal,"moon","自身が受ける攻撃ダメージを8%軽減。","reduction",8);
                case "heroine.slayer":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.slayer").Personal,"sword","HPが75%以上のとき、与ダメージ＋5%。","healthy",5);
                case "heroine.iconoclast":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.iconoclast").Personal,"flame","本体以外の部位への与ダメージ＋8%。","part",8);
                case "heroine.undermine":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.undermine").Personal,"leaf","毎ターン、自身の最大HPの2%を回復。","regen",2);
                case "heroine.echidna":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.echidna").Personal,"star","光属性攻撃の与ダメージ＋10%。","light",10);
                case "heroine.excalipan":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.excalipan").Personal,"moon","自身が受ける攻撃ダメージを8%軽減。","reduction",8);
                case "heroine.r":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.r").Personal,"star","歌唱ゲージの追加獲得率＋15%。歌唱中は獲得しない。","gauge",15);
                case "heroine.annihilator":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.annihilator").Personal,"sword","HPが50%以下のとき、与ダメージ＋12%。","wounded",12);
                case "heroine.annihilator-holy":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.annihilator-holy").Personal,"leaf","毎ターン、自身の最大HPの3%を回復。","regen",3);
                case "heroine.shell":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.shell").Personal,"star","装甲が有効な間、受ける攻撃ダメージを12%軽減。","armor",12);
                case "heroine.oriflamme":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.oriflamme").Personal,"flame","HPが50%以上のとき、火属性攻撃の与ダメージ＋8%。","healthy-fire",8);
                case "heroine.nighthawk":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.nighthawk").Personal,"moon","氷属性攻撃の与ダメージ＋10%。","ice",10);
                case "heroine.slayer-swim":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.slayer-swim").Personal,"crown","指揮ゲージの追加獲得率＋20%。","gauge",20);
                case "heroine.arcane":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.arcane").Personal,"star","弾丸の追加獲得率＋25%。","gauge",25);
                case "heroine.arcane-academy":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.arcane-academy").Personal,"leaf","毎ターン、自身の最大HPの4%を回復。","regen",4);
                case "heroine.shangrila":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.shangrila").Personal,"star","本体への与ダメージ＋8%。","body",8);
                case "heroine.invisible":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.invisible").Personal,"star","自身の回復スキルによる回復量＋10%。","healing",10);
                case "heroine.viper":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.viper").Personal,"sword","本体への与ダメージ＋8%。","body",8);
                case "heroine.vindicator":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.vindicator").Personal,"sword","反撃の与ダメージ＋20%。","counter",20);
                case "heroine.weapon-master":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.weapon-master").Personal,"flame","火傷状態の対象への与ダメージ＋20%。","burned",20);
                case "heroine.april":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.april").Personal,"leaf","自身の回復スキルによる回復量＋10%。","healing",10);
                case "heroine.estreya":return new HeroinePersonalTrait(HeroineAuthoredNames.For("heroine.estreya").Personal,"flame","自身の攻撃で本体を倒すか部位を破壊するたび、戦闘中の攻撃＋5%（上限30%）。","heat-up",5);
                default:return null;
            }
        }
        internal static int Damage(BattleHero hero,BattleSkill skill,string target,BattleState battle=null)
        {
            if(!hero.PersonalTraitsEnabled)return 0;var p=For(hero.Id);if(p==null)return 0;
            bool high=(long)hero.HitPoints*100>=75L*hero.MaxHitPoints,half=(long)hero.HitPoints*2>=hero.MaxHitPoints;
            bool active=p.Kind=="burned"?battle!=null && battle.EnemyStatus(target).Active("burn"):p.Kind=="counter"?skill.Id=="reaction.defender":p.Kind=="healthy"?high:p.Kind=="wounded"?(long)hero.HitPoints*2<=hero.MaxHitPoints:p.Kind=="part"?target!="body":p.Kind=="body"?target=="body":p.Kind=="light"?skill.Attributes.Contains("光"):p.Kind=="healthy-fire"?half && skill.Attributes.Contains("火"):p.Kind=="ice" && skill.Attributes.Contains("氷");
            return active?p.Percent:0;
        }
        internal static int Reduction(BattleHero hero)
        {
            if(!hero.PersonalTraitsEnabled)return 0;var p=For(hero.Id);return p!=null && (p.Kind=="reduction" || p.Kind=="wounded-defense" && (long)hero.HitPoints*2<=hero.MaxHitPoints || p.Kind=="armor" && hero.IsPanzer && hero.ArmorActive)?p.Percent:0;
        }
    }
    public sealed partial class BattleHero
    {
        internal bool PersonalTraitsEnabled;
        public int PersonalKillAttackPercent {get;private set;}
        internal void PersonalEnemyDefeated()
        {
            if(PersonalTraitsEnabled && HeroinePersonalAbility.For(Id)?.Kind=="heat-up")
                PersonalKillAttackPercent=Math.Min(30,PersonalKillAttackPercent+5);
        }
    }
    public sealed partial class PlayableBattle
    {
        private void InitializePersonalAbilities()
        {
            if(!IsFormal)return;
            for(int i=0;i<State.Heroes.Count;i++){
                var h=State.Heroes[i];h.PersonalTraitsEnabled=h.HasVisibleTrait(h.Id+".trait.personal");var trait=h.PersonalTraitsEnabled?HeroinePersonalAbility.For(h.Id):null;
                if(trait?.Kind=="gauge")h.PermanentGaugePercent+=trait.Percent;
                if(trait?.Kind=="regen")h.PermanentRegenPercent+=trait.Percent;
                var equipped=homeProgress?.weaponEquipment.SingleOrDefault(e=>e.heroineId==h.Id);
                var node=equipped==null?null:homeCatalog.weaponNodes.Single(n=>n.id==equipped.nodeId);
                if(node==null || node.initial)continue;
                if(node.uniqueAbilityKind=="damage")h.PermanentDamagePercent+=node.uniqueAbilityPercent;
                if(node.uniqueAbilityKind=="regen")h.PermanentRegenPercent+=node.uniqueAbilityPercent;
                if(node.uniqueAbilityKind=="reduction")h.PermanentReductionPercent+=node.uniqueAbilityPercent;
                if(node.uniqueAbilityKind=="gauge")h.PermanentGaugePercent+=node.uniqueAbilityPercent;
            }
        }
    }
}
