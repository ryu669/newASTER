using System;
using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    // Initial-five authored rules. Historical IDs remain stable for existing inventories.
    public static class ProductionEconomyCatalog
    {
        public const string Version="economy-initial-five-2026-10-05";
        public static FormalKinderBanner Kinder(string[] heroes)=>new FormalKinderBanner{
            id="kinder.initial-five",contentVersion=FormalKinderBanner.ProductionVersion,status="production-candidate",heroineIds=(string[])heroes.Clone(),
            materials=new[]{new KinderMaterialEntry{kind="nectar",weight=9000,amount=200},new KinderMaterialEntry{kind="crystal",weight=700,amount=2}}};
        public static FormalEngagementRules Engagement()=>new FormalEngagementRules{version=FormalEngagementRules.ProductionVersion,loginStones=300,periodStones=100,periodSeconds=1800};
        public static ColossusCombatDef Enemy(string id)
        {
            var enemy=ColossusCombatCatalog.Get(id);enemy.contentVersion=ColossusCombatDef.ProductionVersion;enemy.status="production-candidate";
            enemy.hpPerLevel=id=="colossus.final-flame-ice-phoenix"?360:400;enemy.damagePerLevel=5;enemy.Validate();return enemy;
        }
        private static readonly string[] BranchNames={"翼の誓い","星の剣","天の祝福","封印の刻印","砕く意志","理の解放","薬草の息吹","森の守り","命の調和","火花の記憶","竜の鼓動","紅蓮の誓い","月の護り","祈りの剣","光の輪舞"};
        private static readonly int[] Attacks={8,5,15,10,6,18,4,7,12,9,6,16,5,8,14};
        private static readonly float[] Powers={1.15f,1.25f,1.4f,1.12f,1.28f,1.38f,1.22f,1.12f,1.35f,1.2f,1.18f,1.42f,1.25f,1.1f,1.36f};
        public static void ApplyHome(HomeExperienceCatalog home,CombatDefinitionCatalog combat,CollectionCatalog collection)
        {
            home.abilityIds=new[]{"ability.production.weapon-attack"};home.skillIds=new[]{"skill.production.weapon-basic"};
            foreach(var node in home.weaponNodes){
                int hero=Array.IndexOf(combat.FormationIds,node.heroineId),stage=node.initial?0:node.id.EndsWith("alpha")?1:node.id.EndsWith("beta")?2:3;
                node.abilityId=home.abilityIds[0];node.skillId=home.skillIds[0];
                node.terminal=stage==0?"誓いの根":BranchNames[hero*3+stage-1];node.attackBonus=stage==0?0:Attacks[hero*3+stage-1];node.skillPower=stage==0?1:Powers[hero*3+stage-1];
                if(stage>0){var source=WorldCatalog.Colossi.First(c=>c.WorldLineId=="W0"+(hero+1));node.costs=new[]{new HomeCost{resourceId=collection.owners.Single(o=>o.id==source.Id).materialIds[0],amount=stage==3?24:stage==2?16:12}};}
            }
            // Preserve the four RC1 IDs and dependencies, then grow each branch upwards.
            var expanded=home.weaponNodes.ToList();
            foreach(string heroId in combat.FormationIds){
                int hero=Array.IndexOf(combat.FormationIds,heroId);
                var root=expanded.Single(n=>n.heroineId==heroId && n.initial);root.treePosition=new HomePoint{x=.5f,y=.96f};
                string[] routes={"alpha","beta","gamma"};
                for(int route=0;route<3;route++){
                    var first=expanded.Single(n=>n.id==heroId+".weapon."+routes[route]);first.treePosition=new HomePoint{x=.2f+route*.3f,y=.72f};
                    string parent=first.id;
                    for(int step=2;step<=4;step++){
                        float bend=(hero%2==0?1:-1)*.025f*step;
                        var n=new HomeWeaponNode{id=first.id+".tier"+step,heroineId=heroId,abilityId=first.abilityId,skillId=first.skillId,parentIds=new[]{parent},terminal=first.terminal+" "+(step==4?routes[route]=="alpha"?"α":routes[route]=="beta"?"β":"γ":step==2?"II":"III"),attackBonus=first.attackBonus+step*4,skillPower=first.skillPower+.08f*step,treePosition=new HomePoint{x=Math.Max(.08f,Math.Min(.92f,.2f+route*.3f+bend)),y=.72f-(step-1)*.2f},costs=first.costs.Select(c=>new HomeCost{resourceId=c.resourceId,amount=c.amount+step*4}).ToArray()};
                        expanded.Add(n);parent=n.id;
                    }
                }
            }
            home.weaponNodes=expanded.ToArray();
        }
        public static void ApplyCollection(CollectionCatalog catalog)
        {
            for(int i=0;i<catalog.relics.Length;i++){
                var relic=catalog.relics[i];relic.abilityId="ability.production.relic."+i;
                relic.attackPercent=i%3==0?6+i/3:i%3==1?0:3+i/3;
                relic.hpPercent=i%3==1?6+i/3:i%3==2?3:0;
            }
        }
        public static string RelicAbility(CollectionRelicDef relic)=>"攻撃 ＋"+relic.attackPercent+"% ／ HP ＋"+relic.hpPercent+"%";
    }
}
