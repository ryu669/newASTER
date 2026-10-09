using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

internal static class InteractionTraitTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<CombatDefinitionCatalog> load=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-shangrila.json")),options);
        var catalog=load();catalog.Validate();
        const string swim="outfit.swimsuit",academy="outfit.school";
        var h=catalog.Hero("heroine.slayer-swim");
        check(InteractionTraitCatalog.Has(h,swim) && !InteractionTraitCatalog.Has(catalog.Hero("heroine.slayer"),swim),"Costume tags belong to the form, not the person");
        var g=new FormalHeroineGrowth{heroineId=h.id,level=1};
        var cards=HeroineIdentityCatalog.Traits(h,g);
        check(cards.Last().id==swim && cards.Length==6 && cards.Last().active,"Interaction trait follows combat cards");
        g.duplicateRank=5;
        check(HeroineIdentityCatalog.Traits(h,g).Last().description==cards.Last().description,"Interaction trait has no duplicate-rank growth");
        h.profileTraitIds=new[]{academy};
        check(InteractionTraitCatalog.Has(h,academy,true) && !InteractionTraitCatalog.Has(h,academy),"Daily profile attributes are separate from visible traits");
        var original=InteractionTraitCatalog.ActiveIds(h,g);
        var actor=new BattleHero(h.id,100,10,5,traitId:h.traitId,visibleTraits:original);
        original[original.Length-1]=academy;
        check(actor.HasVisibleTrait(swim) && !actor.HasVisibleTrait(academy) && !actor.HasVisibleTrait(h.id+".trait.mastery"),"Battle snapshot ignores profile tags and locked mastery, and copies input");
        var ids=new[]{h.id}.Concat(catalog.FormationIds.Where(id=>catalog.PersonId(id)!=catalog.PersonId(h.id))).Take(5).ToArray();
        var b=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog.WithFormation(ids),useJobRulesV2:true);
        var condition=new SkillConditionDef{kind="trait-equipped",referenceId=swim,threshold=1};
        check(SkillConditionDef.AllSatisfied(b.State,0,new[]{condition}),"Visible interaction trait satisfies actual combat conditions");
        condition.referenceId=academy;
        check(!SkillConditionDef.AllSatisfied(b.State,0,new[]{condition}),"Profile-only trait cannot satisfy combat conditions");
        var without=load();without.Hero(h.id).interactionTraitIds=null;
        var baseline=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:without.WithFormation(ids),useJobRulesV2:true);
        var actual=b.State.Heroes[0];var plain=baseline.State.Heroes[0];
        check(actual.MaxHitPoints==plain.MaxHitPoints && actual.Attack==plain.Attack && actual.Speed==plain.Speed && actual.PhysicalDefense==plain.PhysicalDefense && actual.MagicDefense==plain.MagicDefense,"Interaction traits add no combat stats");
        check(BattleActionResolver.CalculateDamage(b.State,actual,new BattleSkill("tag-check",1m,0),"body")==BattleActionResolver.CalculateDamage(baseline.State,plain,new BattleSkill("tag-check",1m,0),"body"),"Interaction traits add no implicit damage modifier");
        var skill=catalog.Skill(h.id,0);skill.conditions=new[]{new SkillConditionDef{kind="trait-equipped",referenceId=swim,threshold=1}};catalog.Validate();
        skill.conditions[0].referenceId=academy;
        bool rejected=false;try{catalog.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Formal definitions reject profile-only trait conditions");
        h=load().Hero("heroine.slayer-swim");
        var known=new[]{"swimsuit","halloween","christmas","newyear","valentine","school"}.Select(key=>"outfit."+key).ToArray();
        h.interactionTraitIds=known.Take(5).ToArray();check(InteractionTraitCatalog.VisibleIds(h).Length==8,"Eight visible traits accepted");
        foreach(var invalid in new[]{known,new[]{swim,swim},new[]{"trait.interaction.undefined"},new string[]{null}}){
            h.interactionTraitIds=invalid;rejected=false;try{InteractionTraitCatalog.Validate(h);}catch(ArgumentException){rejected=true;}check(rejected,"Invalid interaction list rejected");
        }
        h.interactionTraitIds=null;h.profileTraitIds=null;check(InteractionTraitCatalog.VisibleIds(h).Length==3,"Old definitions require no save migration");
        var def=InteractionTraitCatalog.Get(swim);def.displayName="changed";check(InteractionTraitCatalog.Get(swim).displayName=="夏の解放者","Shared definitions cannot be mutated through lookup");
        Console.WriteLine("INTERACTION_TRAITS_PASS / form scope / conditions / eight slots / legacy compatibility");
    }
}
