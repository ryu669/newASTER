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
            // Existing authored combat cards occupy three slots until combat-trait migration.
            if(visible.Length+3>HeroinePersonalAbility.MaximumTraits)throw new ArgumentException("A heroine cannot exceed eight visible traits.");
            if(visible.Contains(hero.traitId) || visible.Contains(hero.id+".trait.personal") || visible.Contains(hero.id+".trait.mastery"))
                throw new ArgumentException("Duplicate visible trait ID.");
        }
        public static string[] VisibleIds(HeroineCombatDef hero)
        {
            Validate(hero);
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
