using System;
using System.Linq;

namespace NewAster.Core
{
    // Interaction traits deliberately contain no numeric effects or rank values.
    [Serializable] public sealed class InteractionTraitDef
    {
        public string id,displayName,category;
    }

    public static class InteractionTraitCatalog
    {
        private static readonly InteractionTraitDef[] definitions = {
            Def("hair.blonde","黄金の冠"),Def("hair.black","烏羽の艶"),Def("hair.silver","月光の糸"),Def("hair.red","紅蓮の髪"),
            Def("body.slender","エアロダイナミクス"),Def("body.glamorous","豊穣の曲線"),Def("body.muscular","鋼の輪郭"),Def("body.petite","小さな巨人"),
            Def("taste.muscle","筋肉は裏切らない"),Def("taste.books","紙魚の友"),Def("taste.sweets","砂糖の誘惑"),Def("taste.cooking","台所の錬金術師"),Def("taste.mechanics","歯車の恋人"),Def("taste.flowers","花語り"),
            Def("personality.cool","氷面の微笑"),Def("personality.active","静止不能"),Def("personality.shy","頬染めの名人"),Def("personality.night","月の住人"),
            Def("appearance.animal","アニマルちっく"),Def("appearance.glasses","知性の窓枠"),Def("appearance.horns","角ある者"),
            Def("outfit.swimsuit","夏の解放者"),Def("outfit.halloween","百鬼夜行の招待状"),Def("outfit.christmas","聖夜の贈り物"),Def("outfit.newyear","初春の彩り"),Def("outfit.valentine","甘い共犯者"),Def("outfit.school","青春の方程式")
        };
        private static InteractionTraitDef Def(string id,string name) =>
            new InteractionTraitDef {id=id,displayName=name,category=id.Split('.')[0]};
        public static string[] Ids=>definitions.Select(d=>d.id).ToArray();
        public static string Flavor(string id)
        {
            switch(id){
                case "hair.blonde":return "陽光を梳いたような髪が、翼の影まで明るくする。";
                case "hair.black":return "夜を映す艶やかな髪。その奥の眼差しは、いつもあなたを見ている。";
                case "hair.silver":return "月明かりを束ねた髪が、風の通り道に淡くきらめく。";
                case "hair.red":return "炎を思わせる髪には、胸の奥の熱まで隠せない。";
                case "body.slender":return "風をすり抜ける軽やかな輪郭。翼を広げれば、誰より遠くへ。";
                case "body.glamorous":return "豊かな曲線にも、誇りにも、自分らしさが息づいている。";
                case "body.muscular":return "積み重ねた鍛錬は、いざという時に誰かを支える強さになる。";
                case "body.petite":return "小さな背中に、大きな覚悟。見上げるだけの空では終わらない。";
                case "taste.muscle":return "今日のひと頑張りは明日の力。鍛えた分だけ、笑顔にも自信が増える。";
                case "taste.books":return "本を開けば、まだ見ぬ世界が隣に座る。続きはあなたと読んでみたい。";
                case "taste.sweets":return "甘いひと口で、難しい顔もほどけてしまう。最後のひとつは、半分ずつ。";
                case "taste.cooking":return "鍋から立つ湯気に、帰ってきた実感が混ざる。今日は何が食べたい？";
                case "taste.mechanics":return "歯車の噛み合う音を聞けば、時間を忘れる。直せないものほど気になるらしい。";
                case "taste.flowers":return "咲く日を急がず、水をやる。小さな蕾にも、きちんと名前を呼びかけて。";
                case "personality.cool":return "涼しい表情の下にも、譲れない想いがある。気づいてほしい相手は、ひとり。";
                case "personality.active":return "思いついたら、もう一歩目。次の景色にも、あなたを連れていく。";
                case "personality.shy":return "伝えたい言葉ほど、頬に先回りされる。少しだけ、待っていてほしい。";
                case "personality.night":return "皆が眠る頃、ようやく心が静かになる。月の下なら、話せることもある。";
                case "appearance.animal":return "耳も尻尾も、気持ちに正直。隠したつもりの嬉しさまで、そっと揺れている。";
                case "appearance.glasses":return "レンズ越しの眼差しは細かな変化も見逃さない。あなたの疲れにも、きっと気づく。";
                case "appearance.horns":return "誇り高い角も、彼女を知れば見慣れた輪郭。怖がらずに、隣へどうぞ。";
                case "outfit.swimsuit":return "波音に翼を休める日。戦いを忘れた笑顔が、夏の光にほどける。";
                case "outfit.halloween":return "今夜だけは、いたずらもおめかしも大胆に。驚く顔が見たいから。";
                case "outfit.christmas":return "包みに込めたのは、あなたを想う時間。寒い夜にも、温かな居場所を。";
                case "outfit.newyear":return "袖を整え、新しい朝へ。最初の願いは、今年も一緒に帰れること。";
                case "outfit.valentine":return "甘さの加減に迷った時間まで、贈り物にして。返事は、目を見て聞きたい。";
                case "outfit.school":return "教科書の余白にも、放課後の約束にも。まだ知らない自分を書き足していく。";
                default:throw new ArgumentException("Missing interaction trait flavor: "+id);
            }
        }
        public static string[] Warnings(HeroineCombatDef hero)
        {
            Validate(hero);
            var ids=(hero.interactionTraitIds??Array.Empty<string>()).Union(hero.profileTraitIds??Array.Empty<string>()).ToArray();
            return new[]{"hair","outfit"}.Where(category=>ids.Count(id=>Get(id).category==category)>1).Select(category=>"Multiple "+category+" traits on "+hero.id).ToArray();
        }
        public static InteractionTraitDef Get(string id)
        {
            var item=definitions.SingleOrDefault(d=>d.id==id);
            if(item==null)throw new ArgumentException("Undefined interaction trait: "+id);
            return new InteractionTraitDef {id=item.id,displayName=item.displayName,category=item.category};
        }
        public static void Validate(HeroineCombatDef hero)
        {
            if(hero==null)throw new ArgumentNullException(nameof(hero));
            var visible=hero.interactionTraitIds??Array.Empty<string>();
            var profile=hero.profileTraitIds??Array.Empty<string>();
            foreach(var ids in new[]{visible,profile}) {
                if(ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)throw new ArgumentException("Duplicate interaction trait.");
                foreach(var id in ids)Get(id);
            }
            if(!string.IsNullOrEmpty(hero.secondTraitId))CombatTraitCatalog.Resolve(hero);
            if(string.IsNullOrEmpty(hero.secondTraitId) && visible.Length+3>HeroinePersonalAbility.MaximumTraits)throw new ArgumentException("A heroine cannot exceed eight visible traits.");
            if(visible.Contains(hero.traitId) || visible.Contains(hero.id+".trait.personal") || visible.Contains(hero.id+".trait.mastery"))
                throw new ArgumentException("Duplicate visible trait ID.");
        }
        public static string[] VisibleIds(HeroineCombatDef hero)
        {
            Validate(hero);
            if(!string.IsNullOrEmpty(hero.secondTraitId))return CombatTraitCatalog.Resolve(hero).VisibleIds;
            return new[]{hero.traitId,hero.id+".trait.personal",hero.id+".trait.mastery"}
                .Where(id=>!string.IsNullOrEmpty(id)).Concat(hero.interactionTraitIds??Array.Empty<string>()).ToArray();
        }
        public static bool Has(HeroineCombatDef hero,string id,bool includeProfile=false)
        {
            Validate(hero);
            return (hero.interactionTraitIds??Array.Empty<string>()).Contains(id) ||
                includeProfile && (hero.profileTraitIds??Array.Empty<string>()).Contains(id);
        }
        public static string[] ActiveIds(HeroineCombatDef hero,FormalHeroineGrowth growth) =>
            VisibleIds(hero).Where(id=>id!=hero.id+".trait.mastery" || HeroineTraitRules.Mastered(growth)).ToArray();
    }
}
