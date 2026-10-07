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
            if(id=="colossus.black-smoke-citadel" || id=="colossus.final-flame-ice-phoenix"){
                enemy.majorAttributes=new[]{"火"};foreach(var action in enemy.actionCycle??Array.Empty<ColossusActionCombatDef>())action.attributes=new[]{"火"};
            }
            enemy.hpPerLevel=id=="colossus.final-flame-ice-phoenix"?360:400;enemy.damagePerLevel=5;enemy.Validate();return enemy;
        }
        private static readonly string[] WeaponOwners={"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan","heroine.r","heroine.annihilator","heroine.annihilator-holy","heroine.shell","heroine.oriflamme","heroine.nighthawk","heroine.slayer-swim","heroine.arcane","heroine.arcane-academy","heroine.shangrila"};
        private static int WeaponOwner(string id){int index=Array.IndexOf(WeaponOwners,id);if(index<0)throw new ArgumentException("Missing authored weapon identity "+id);return index;}
        private static readonly string[] BranchNames={"翼の誓い","星の剣","天の祝福","封印の刻印","砕く意志","理の解放","薬草の息吹","森の守り","命の調和","火花の記憶","竜の鼓動","紅蓮の誓い","月の護り","祈りの剣","光の輪舞","星音の弦","静寂の譜","共鳴の環","鬼面の刃","花守の盾","紅蝶の誓い","雪灯の鈴","聖夜の守り","贈り物の翼","守護の拳","耐える装甲","帰還の回転翼","黒剣の火花","五色の触媒","明日を灯す炎","星図の指針","青い羽根の盾","夜明けの足音","白傘の道標","海風の守り","夏を継ぐ翼","探検の指針","展示の守り","帰還の鍵","検証の頁","書架の結界","卒業の先の翼","選び直す照準","休息の防壁","帰る日の魔弾"};
        private static readonly int[] Attacks={8,5,15,10,6,18,4,7,12,9,6,16,5,8,14,7,5,13,9,6,15,6,8,12,9,7,14,10,6,16,8,6,14,8,6,14,9,5,15,8,7,14,11,6,16};
        private static readonly float[] Powers={1.15f,1.25f,1.4f,1.12f,1.28f,1.38f,1.22f,1.12f,1.35f,1.2f,1.18f,1.42f,1.25f,1.1f,1.36f,1.18f,1.12f,1.38f,1.2f,1.15f,1.4f,1.12f,1.2f,1.32f,1.2f,1.14f,1.36f,1.2f,1.12f,1.4f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.2f,1.12f,1.36f,1.22f,1.14f,1.4f};
        public static void ApplyHome(HomeExperienceCatalog home,CombatDefinitionCatalog combat,CollectionCatalog collection)
        {
            home.abilityIds=new[]{"ability.production.weapon-attack"};home.skillIds=new[]{"skill.production.weapon-basic"};
            foreach(var node in home.weaponNodes){
                int hero=WeaponOwner(node.heroineId),stage=node.initial?0:node.id.EndsWith("alpha")?1:node.id.EndsWith("beta")?2:3;
                node.abilityId=home.abilityIds[0];node.skillId=home.skillIds[0];
                node.terminal=stage==0?"誓いの根":BranchNames[hero*3+stage-1];node.attackBonus=stage==0?0:Attacks[hero*3+stage-1];node.skillPower=stage==0?1:Powers[hero*3+stage-1];
                if(stage>0){var source=WorldCatalog.Colossi.First(c=>c.WorldLineId=="W0"+(hero<6?hero+1:hero==6?1:hero==7?2:6));node.costs=new[]{new HomeCost{resourceId=collection.owners.Single(o=>o.id==source.Id).materialIds[0],amount=stage==3?24:stage==2?16:12}};}
            }
            // Preserve the four RC1 IDs and dependencies, then grow each branch upwards.
            var expanded=home.weaponNodes.ToList();
            foreach(string heroId in combat.HeroineIds){
                int hero=WeaponOwner(heroId);
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
            foreach(var n in expanded.Where(n=>!n.initial)){
                int route=n.id.Contains(".alpha")?0:n.id.Contains(".beta")?1:2;
                int tier=n.id.EndsWith("tier4")?4:n.id.EndsWith("tier3")?3:n.id.EndsWith("tier2")?2:1;
                int heroIndex=WeaponOwner(n.heroineId);
                int sourceIndex=Array.FindIndex(WorldCatalog.Colossi.ToArray(),c=>c.WorldLineId=="W0"+(heroIndex<6?heroIndex+1:heroIndex==6?1:heroIndex==7?2:6));
                var sources=Enumerable.Range(0,3).Select(i=>WorldCatalog.Colossi[(sourceIndex+i)%WorldCatalog.Colossi.Count]).ToArray();
                if(tier>=2)n.costs=Enumerable.Range(0,tier==2?2:3).Select(i=>new HomeCost{resourceId=collection.owners.Single(o=>o.id==sources[i%sources.Length].Id).materialIds[tier==2?1:tier==3?2:i<2?3:2],amount=tier==2?4:tier==3?5:6}).ToArray();
                n.attackBonus=route==0?8+tier*4:route==1?2+tier:4+tier*2;
                n.skillPower=route==0?1.12f+tier*.06f:route==1?1.02f+tier*.025f:1.06f+tier*.04f;
                n.physicalDefenseBonus=route==1?12+tier*6:0;n.magicDefenseBonus=route==1?10+tier*6:0;
                n.speedBonus=route==2?3+tier*2:0;n.criticalBonusBp=route==0?200+tier*100:route==2?100+tier*50:0;
                n.criticalDamageBonus=route==2?5+tier*3:0;
                if(tier==4){int hero=WeaponOwner(n.heroineId);string[] motifs={"花翼","理砕","森命","紅蓮","月祈","星音","紅蝶","雪灯","鋼翼","炎翼","夜星","海翼","遺翼","書翼","銃翼"};
                    n.weaponTraitName=motifs[hero]+(route==0?"の鋭刃":route==1?"の結界":"の疾風");
                    n.traitAttackPercent=route==0?10:0;n.traitDefensePercent=route==1?12:0;n.traitSpeedPercent=route==2?8:0;
                }
            }
            home.weaponNodes=expanded.ToArray();
        }
        public static void ApplyCollection(CollectionCatalog catalog)
        {
            var resources=catalog.resources.ToList();
            foreach(var owner in catalog.owners.Where(o=>o.kind=="colossus")){
                string common=owner.materialIds[0],name=WorldCatalog.Colossi.Single(c=>c.Id==owner.id).DisplayName;
                var baseMaterial=resources.Single(r=>r.id==common);baseMaterial.name=name+"の鱗片";baseMaterial.rarity=1;baseMaterial.minDropLevel=1;
                string[] suffix={"rare","epic","legendary"},names={"結晶","心核","星髄"};int[] levels={5,15,30};
                for(int i=0;i<3;i++)resources.Add(new CollectionResourceDef{id=common+"."+suffix[i],kind="material",ownerId=owner.id,name=name+"の"+names[i],rarity=i+2,minDropLevel=levels[i]});
                owner.materialIds=new[]{common,common+".rare",common+".epic",common+".legendary"};
            }
            catalog.resources=resources.ToArray();
            for(int i=0;i<catalog.relics.Length;i++){
                var relic=catalog.relics[i];relic.abilityId="ability.production.relic."+i;
                relic.attackPercent=i%3==0?6+i/3:i%3==1?0:3+i/3;
                relic.hpPercent=i%3==1?6+i/3:i%3==2?3:0;
            }
        }
        public static string RelicAbility(CollectionRelicDef relic)=>"攻撃 ＋"+relic.attackPercent+"% ／ HP ＋"+relic.hpPercent+"%";
    }
}
