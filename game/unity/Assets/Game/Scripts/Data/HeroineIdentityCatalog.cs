using System;
using NewAster.Core;
namespace NewAster.Data
{
    public sealed class HeroineTraitCard {public string id,name,icon,description;public bool active;}
    public static class HeroineIdentityCatalog
    {
        public static string JobName(string id)=>id=="job.fighter"?"ファイター":id=="job.berserker"?"バーサーカー":id=="job.defender"?"ディフェンダー":id=="job.blaster"?"ブラスター":id=="job.gunner"?"ガンナー":id.Replace("job.","");
        public static HeroineTraitCard[] Traits(HeroineCombatDef h,FormalHeroineGrowth growth)
        {
            string name,master,icon,bonus;
            switch(h.id){
                case "heroine.slayer":name="花翼の誓い";master="星を断つ剣";icon="sword";bonus="会心率＋5%";break;
                case "heroine.iconoclast":name="理を砕く意志";master="解放の刻印";icon="flame";bonus="攻撃＋5%";break;
                case "heroine.undermine":name="森の守り手";master="根深き生命";icon="leaf";bonus="最大HP＋5%";break;
                case "heroine.echidna":name="紅蓮の記憶";master="竜の叡智";icon="star";bonus="魔法防御＋5%";break;
                case "heroine.excalipan":name="月光の祈り";master="不屈の輪舞";icon="moon";bonus="物理防御＋5%";break;
                default:throw new ArgumentException("人物固有の特性定義がありません。");
            }
            var job=h.jobId;string resource=job=="job.fighter"?"ブースト：行動準備＋1、攻撃後＋1。":job=="job.berserker"?"怒気：攻撃後＋3、被弾時＋1。":job=="job.defender"?"チャージ：行動準備＋1、被弾時＋2。":job=="job.blaster"?"マジック：行動準備＋2。":"弾丸：行動準備＋2、攻撃後＋1。特殊兵装で攻撃を切り替える。";
            string innate=(h.traitHpPercent>0?"最大HP＋"+(FormalGrowthMath.TraitAmount(h.traitHpPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"")+(h.traitAttackPercent>0?"攻撃＋"+(FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"");
            return new[]{new HeroineTraitCard{id=h.traitId,name=name,icon=icon,description=innate+"重複強化で効果量が成長します。",active=true},new HeroineTraitCard{id=h.id+".trait.job",name=JobName(job)+"の心得",icon="resource",description=resource+"資源上限・消費はジョブとスキルに従います。",active=true},new HeroineTraitCard{id=h.id+".trait.mastery",name=master,icon="crown",description="3スキルすべてLv7で解放："+bonus+"。速度・チェイン率は変わりません。",active=HeroineTraitRules.Mastered(growth)}};
        }
    }
}
