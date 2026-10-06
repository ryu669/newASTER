using System;
using NewAster.Core;
namespace NewAster.Data
{
    public sealed class HeroineTraitCard {public string id,name,icon,description;public bool active;}
    public static class HeroineIdentityCatalog
    {
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
            var job=h.jobId;string resource=job=="job.sniper"?"通常行動で狙撃＋3。指定した1人の攻撃を支援。狙撃15で詠唱中は全員を支援し、5倍弾を発射。":job=="job.gambler"?"SLOTのみで3スキル抽選。3行＋2対角の全ライン発動。777は各スキル2回、全外れWT0。":job=="job.general"?"指揮官を1人選ぶ。本人の5枠効果のみ適用。指揮15で300 Clock強化。":job=="job.chaser"?"駆動は毎Battle Turn＋2。GEAR2/3は2/4消費・NITRO＋1。5で次スキルWT0。対象Flag3で全体IGNITION。":job=="job.alchemist"?"錬成資源：通常行動＋1。火・雷・闇・光・斬撃を投入し、READYを消費せず錬成。":job=="job.panzer"?"装甲HPは通常回復不可。ツール2枠・各2回。生身は防御のみ、300 Clock生存で装甲復帰。":job=="job.artist"?"通常行動で歌唱ゲージ＋2。歌唱中はスキル選択とゲージ獲得なし。":job=="job.healer"?"生命力：行動準備＋1。回復・蘇生・最大HP強化・生命投資を選べます。":job=="job.fighter"?"ブースト：行動準備＋1、攻撃後＋1。":job=="job.berserker"?"怒気：攻撃後＋3、被弾時＋1。":job=="job.defender"?"チャージ：行動準備＋1、被弾時＋2。":job=="job.blaster"?"マジック：行動準備＋2。":"弾丸：行動準備＋2、攻撃後＋1。特殊兵装で攻撃を切り替える。";
            string innate=(h.traitHpPercent>0?"最大HP＋"+(FormalGrowthMath.TraitAmount(h.traitHpPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"")+(h.traitAttackPercent>0?"攻撃＋"+(FormalGrowthMath.TraitAmount(h.traitAttackPercent*100,growth.duplicateRank)/100f).ToString("0.#")+"%。":"");
            return new[]{new HeroineTraitCard{id=h.traitId,name=name,icon=icon,description=innate+"重複強化で効果量が成長します。",active=true},new HeroineTraitCard{id=h.id+".trait.job",name=JobName(job)+"の心得",icon="resource."+job.Replace("job.",""),description=resource+"資源上限・消費はジョブとスキルに従います。",active=true},new HeroineTraitCard{id=h.id+".trait.mastery",name=master,icon="crown",description="3スキルすべてLv7で解放："+bonus+"。速度・チェイン率は変わりません。",active=HeroineTraitRules.Mastered(growth)}};
        }
    }
}
