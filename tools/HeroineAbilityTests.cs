using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class HeroineAbilityTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-shangrila.json")),options);
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-shangrila-story-content.json")),options);
        var home=ProductionStoryCatalog.Home(combat,story);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion)};
        Func<FormalCampaignSave,FormalCampaignSave> clone=s=>JsonSerializer.Deserialize<FormalCampaignSave>(JsonSerializer.Serialize(s,options),options);
        WeaponGrowthRules.EnsureRoots(save,home);
        foreach(string id in combat.HeroineIds){
            var root=home.weaponNodes.Single(n=>n.heroineId==id && n.initial);
            check(save.home.weaponNodeIds.Contains(root.id) && save.home.weaponEquipment.Single(e=>e.heroineId==id).nodeId==root.id && save.home.WeaponLevel(root.id)==0,"Root starts acquired and equipped without a level: "+id);
            check(root.uniqueAbilityPercent==0 && string.IsNullOrEmpty(root.uniqueAbilityKind) && WeaponGrowthRules.Power(root,0)==1,"Root has no unique ability or skill bonus");
            bool denied=false;try{HomeRules.Apply(save,home,new HomeOperation("weapon-level",root.id,"1"));}catch(ArgumentException){denied=true;}check(denied,"Root cannot consume materials for levels");
            var traits=HeroineIdentityCatalog.Traits(combat.Hero(id),save.growth.heroines.Single(g=>g.heroineId==id));
            var personalCard=traits.Single(t=>t.id.EndsWith(".trait.personal"));
            check(traits.Length<=8 && traits.All(t=>!t.id.EndsWith(".trait.job")) && personalCard.name==HeroinePersonalAbility.For(id).Name && personalCard.description.EndsWith(HeroinePersonalAbility.For(id).Description,StringComparison.Ordinal) && personalCard.description.Contains(HeroineAuthoredNames.For(id).Flavor),"Personal trait flavor retains its exact combat effect: "+id);
            foreach(var n in home.weaponNodes.Where(n=>n.heroineId==id && !n.initial))check(new[]{"damage","regen","reduction","gauge"}.Contains(n.uniqueAbilityKind) && n.uniqueAbilityPercent>0 && !string.IsNullOrEmpty(WeaponGrowthRules.Icon(n)),"One ability and one icon per nonroot node");
            foreach(int route in new[]{0,1,2}){
                var branch=home.weaponNodes.Where(n=>n.heroineId==id && WeaponGrowthRules.Route(n)==route).OrderBy(WeaponGrowthRules.Tier).ToArray();
                check(branch.Length==4 && branch.Select(n=>n.uniqueAbilityKind).Distinct().Count()==1,"Branch identity remains stable across its four stages: "+id+" / "+route);
                for(int tier=1;tier<branch.Length;tier++){
                    var previous=branch[tier-1];var next=branch[tier];
                    check(next.uniqueAbilityPercent>previous.uniqueAbilityPercent && WeaponGrowthRules.Attack(next,1)>WeaponGrowthRules.Attack(previous,7) && WeaponGrowthRules.Power(next,1)>WeaponGrowthRules.Power(previous,7) && WeaponGrowthRules.Physical(next,1)>=WeaponGrowthRules.Physical(previous,7) && WeaponGrowthRules.Magic(next,1)>=WeaponGrowthRules.Magic(previous,7) && WeaponGrowthRules.Speed(next,1)>=WeaponGrowthRules.Speed(previous,7) && WeaponGrowthRules.Critical(next,1)>=WeaponGrowthRules.Critical(previous,7) && WeaponGrowthRules.CriticalDamage(next,1)>=WeaponGrowthRules.CriticalDamage(previous,7),"Next stage at Lv1 exceeds prior stage at Lv7 without losing its ability: "+next.id);
                }
            }
        }
        var eight=Enumerable.Range(0,8).Select(i=>new HeroineTraitCard{id="trait."+i,name="固有"+i,icon="star",description="固有効果"+i}).ToArray();HeroineIdentityCatalog.ValidateTraits(eight);bool over=false;try{HeroineIdentityCatalog.ValidateTraits(eight.Concat(new[]{new HeroineTraitCard{id="trait.9",name="9",description="9"}}).ToArray());}catch(ArgumentException){over=true;}check(over,"Ninth trait is rejected rather than silently clipped");
        var old=clone(save);string advanced="heroine.slayer.weapon.alpha";old.home.weaponNodeIds=old.home.weaponNodeIds.Concat(new[]{advanced}).ToArray();old.home.weaponEquipment[0].nodeId=advanced;old.home.weaponLevels=new[]{new HomeWeaponLevel{nodeId="heroine.slayer.weapon.root",level=7},new HomeWeaponLevel{nodeId=advanced,level=4}};old.home.readEventIds=new[]{"preserved.event"};
        WeaponGrowthRules.EnsureRoots(old,home);string migrated=JsonSerializer.Serialize(old,options);WeaponGrowthRules.EnsureRoots(old,home);
        check(JsonSerializer.Serialize(old,options)==migrated && old.home.weaponEquipment[0].nodeId==advanced && old.home.WeaponLevel(advanced)==4 && old.home.WeaponLevel("heroine.slayer.weapon.root")==0 && old.home.readEventIds.Single()=="preserved.event","Root migration is idempotent and retains equipped branch, branch level and read events");
        Func<string,string,PlayableBattle> battle=(id,node)=>{
            var s=clone(save);s.home.weaponNodeIds=home.weaponNodes.Select(n=>n.id).ToArray();s.home.weaponEquipment.Single(e=>e.heroineId==id).nodeId=node;
            string[] ids=new[]{id}.Concat(combat.FormationIds.Where(x=>combat.PersonId(x)!=combat.PersonId(id))).Take(5).ToArray();
            return new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:combat.WithFormation(ids),formalGrowth:s.growth,homeProgress:s.home,homeCatalog:home,useJobRulesV2:true);
        };
        foreach(string kind in new[]{"damage","regen","reduction","gauge"}){
            var n=home.weaponNodes.Where(x=>x.uniqueAbilityKind==kind).OrderByDescending(x=>x.uniqueAbilityPercent).First();var b=battle(n.heroineId,n.id);var h=b.State.Heroes[0];
            if(kind=="damage"){var skill=new BattleSkill("ability-check",1m,0);int with=BattleActionResolver.CalculateDamage(b.State,h,skill,"body");h.PermanentDamagePercent=0;check(with>BattleActionResolver.CalculateDamage(b.State,h,skill,"body"),"Equipped damage ability increases real skill damage");}
            if(kind=="regen"){var baseHero=battle(n.heroineId,n.heroineId+".weapon.root").State.Heroes[0];check(h.PermanentRegenPercent==baseHero.PermanentRegenPercent+n.uniqueAbilityPercent,"Weapon regeneration adds to existing personal and job regeneration");h.TakeDamage(100);int before=h.HitPoints;h.TickBattleTurn();check(h.HitPoints-before==Math.Min(100,h.MaxHitPoints*h.PermanentRegenPercent/100),"Equipped regeneration heals by maximum HP on a battle turn");}
            if(kind=="reduction"){int with=b.PreviewEnemyDamage(0);h.PermanentReductionPercent=0;check(with<b.PreviewEnemyDamage(0),"Equipped protection reduces actual enemy attack damage");}
            if(kind=="gauge"){h.SpendResource(h.JobResource);for(int i=0;i<5;i++)h.GainResource(1);check(h.JobResource==Math.Min(h.JobResourceMax,5+5*n.uniqueAbilityPercent/100),"Small resource gains accumulate fractional ability bonus");}
        }
        var fire=battle("heroine.echidna","heroine.echidna.weapon.root");var fh=fire.State.Heroes[0];int plain=BattleActionResolver.CalculateDamage(fire.State,fh,new BattleSkill("plain",1m,0),"body"),flame=BattleActionResolver.CalculateDamage(fire.State,fh,new BattleSkill("fire",1m,0,attributes:new[]{"光"}),"body");check(flame>plain,"Echidna personal trait recognizes actual Japanese attribute IDs");
        foreach(var id in new[]{"heroine.echidna","heroine.nighthawk"}){
            string attribute=id=="heroine.echidna"?"光":"氷";
            check(combat.skills.Any(s=>s.ownerId==id && s.attributes.Contains(attribute)),"Personal attribute ability matches the heroine's actual skill attributes: "+id);
        }
        var r=battle("heroine.r","heroine.r.weapon.root");var singer=r.State.Heroes[0];singer.ResourceGainBlocked=()=>true;int resource=singer.JobResource;singer.GainResource(10);check(singer.JobResource==resource,"Personal gauge gain cannot bypass singing restrictions");
        Console.WriteLine("HEROINE_ABILITY_PASS 15 forms / roots / eight-trait cap / four abilities / migration");
    }
}
