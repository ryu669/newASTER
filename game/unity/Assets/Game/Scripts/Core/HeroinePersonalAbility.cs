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
                case "heroine.slayer":return new HeroinePersonalTrait("花翼の先陣","sword","HPが75%以上のとき、与ダメージ＋5%。","healthy",5);
                case "heroine.iconoclast":return new HeroinePersonalTrait("偶像を砕く一撃","flame","本体以外の部位への与ダメージ＋8%。","part",8);
                case "heroine.undermine":return new HeroinePersonalTrait("森の脈動","leaf","毎ターン、自身の最大HPの2%を回復。","regen",2);
                case "heroine.echidna":return new HeroinePersonalTrait("竜光の継承","star","光属性攻撃の与ダメージ＋10%。","light",10);
                case "heroine.excalipan":return new HeroinePersonalTrait("月光の帳","moon","自身が受ける攻撃ダメージを8%軽減。","reduction",8);
                case "heroine.r":return new HeroinePersonalTrait("星音の余韻","star","歌唱ゲージの追加獲得率＋15%。歌唱中は獲得しない。","gauge",15);
                case "heroine.annihilator":return new HeroinePersonalTrait("紅蝶の窮刃","sword","HPが50%以下のとき、与ダメージ＋12%。","wounded",12);
                case "heroine.annihilator-holy":return new HeroinePersonalTrait("聖夜のぬくもり","leaf","毎ターン、自身の最大HPの3%を回復。","regen",3);
                case "heroine.shell":return new HeroinePersonalTrait("帰還を守る外殻","star","装甲が有効な間、受ける攻撃ダメージを12%軽減。","armor",12);
                case "heroine.oriflamme":return new HeroinePersonalTrait("消えない正義の炎","flame","HPが50%以上のとき、火属性攻撃の与ダメージ＋8%。","healthy-fire",8);
                case "heroine.nighthawk":return new HeroinePersonalTrait("夜を裂く駆動","moon","氷属性攻撃の与ダメージ＋10%。","ice",10);
                case "heroine.slayer-swim":return new HeroinePersonalTrait("夏空の号令","crown","指揮ゲージの追加獲得率＋20%。","gauge",20);
                case "heroine.arcane":return new HeroinePersonalTrait("空欄に残す装填式","star","弾丸の追加獲得率＋25%。","gauge",25);
                case "heroine.arcane-academy":return new HeroinePersonalTrait("放課後の休息","leaf","毎ターン、自身の最大HPの4%を回復。","regen",4);
                case "heroine.shangrila":return new HeroinePersonalTrait("核心を射抜く静寂","star","本体への与ダメージ＋8%。","body",8);
                default:return null;
            }
        }
        internal static int Damage(BattleHero hero,BattleSkill skill,string target)
        {
            if(!hero.PersonalTraitsEnabled)return 0;var p=For(hero.Id);if(p==null)return 0;
            bool high=(long)hero.HitPoints*100>=75L*hero.MaxHitPoints,half=(long)hero.HitPoints*2>=hero.MaxHitPoints;
            bool active=p.Kind=="healthy"?high:p.Kind=="wounded"?(long)hero.HitPoints*2<=hero.MaxHitPoints:p.Kind=="part"?target!="body":p.Kind=="body"?target=="body":p.Kind=="light"?skill.Attributes.Contains("光"):p.Kind=="healthy-fire"?half && skill.Attributes.Contains("火"):p.Kind=="ice" && skill.Attributes.Contains("氷");
            return active?p.Percent:0;
        }
        internal static int Reduction(BattleHero hero)
        {
            if(!hero.PersonalTraitsEnabled)return 0;var p=For(hero.Id);return p!=null && (p.Kind=="reduction" || p.Kind=="armor" && hero.IsPanzer && hero.ArmorActive)?p.Percent:0;
        }
    }
    public sealed partial class BattleHero
    {
        internal bool PersonalTraitsEnabled;
    }
    public sealed partial class PlayableBattle
    {
        private void InitializePersonalAbilities()
        {
            if(!IsFormal)return;
            for(int i=0;i<State.Heroes.Count;i++){
                var h=State.Heroes[i];h.PersonalTraitsEnabled=true;var trait=HeroinePersonalAbility.For(h.Id);
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
