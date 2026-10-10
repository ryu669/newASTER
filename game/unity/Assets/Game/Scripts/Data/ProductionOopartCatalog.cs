using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
namespace NewAster.Data
{
    public static class ProductionOopartCatalog
    {
        private static OopartEffectDef Effect(string id,string type,string target,int amount,EffectCondition condition=null)=>new OopartEffectDef{id=id,effectType=type,targetId=target,baseValue=amount,maxValue=type=="status"?20:type=="tool-uses"?10:type=="cast-percent"?90:1000,conditions=condition==null?Array.Empty<EffectCondition>():new[]{condition}};
        public static void Apply(CollectionCatalog c)
        {
            var defs=new List<OopartDef>();
            foreach(var r in c.relics){
                var effects=new List<OopartEffectDef>();
                if(r.jobStatPercent>0){var condition=new EffectCondition{kind="job",targetId=r.jobId};effects.Add(Effect(r.id+".job","stat-percent","all",r.jobStatPercent,condition));
                    effects.Add(Effect(r.id+".special",r.jobId=="job.general" || r.jobId=="job.artist"?"buff-power":r.jobId=="job.panzer"?"tool-uses":r.jobId=="job.blaster"?"cast-percent":r.jobId=="job.fighter"?"skill-power":"gauge",r.jobId=="job.fighter"?"slot.1":"all",r.jobId=="job.panzer"?1:r.jobId=="job.fighter"?25:r.jobId=="job.blaster" || r.jobId=="job.artist" || r.jobId=="job.general"?20:15,condition));
                }else if(r.turnEffect!=null){var e=Effect(r.id+".clock","stat-percent","attack",r.turnEffect=="wane"?60:0);e.scalingType=r.turnEffect=="wane"?"turn-decay":"turn-growth";e.scalingValue=10;e.maxValue=60;if(r.turnEffect=="wane")e.durationClock=700;effects.Add(e);}
                else{if(r.attackPercent>0)effects.Add(Effect(r.id+".attack","stat-percent","attack",r.attackPercent));if(r.hpPercent>0)effects.Add(Effect(r.id+".hp","stat-percent","hp",r.hpPercent));if(r.defensePercent>0)effects.Add(Effect(r.id+".defense","stat-percent","defense",r.defensePercent));if(r.speedPercent>0)effects.Add(Effect(r.id+".speed","stat-percent","speed",r.speedPercent));}
                defs.Add(Definition(c,r,effects.ToArray()));
            }
            var additions=new[]{
                new{key="fire",name="焔のレンズ",effects=new[]{Effect("fire.damage","attribute","火",20),Effect("fire.part","part","all",20)}},
                new{key="quick-cast",name="詠唱の羅針盤",effects=new[]{Effect("quick.cast","cast-percent","all",20),Effect("quick.wait","wt","attack",10)}},
                new{key="stun",name="雷鳴の印",effects=new[]{Effect("stun.meter","status","stun",20),Effect("stun.attack","stat-percent","attack",20,new EffectCondition{kind="target-status",targetId="stun"})}},
                new{key="wounded",name="背水の心石",effects=new[]{new OopartEffectDef{id="wounded.attack",effectType="stat-percent",targetId="attack",scalingType="hp-missing",scalingValue=50,maxValue=50},Effect("wounded.gauge","gauge","all",15,new EffectCondition{kind="hp-at-most",value=50})}},
                new{key="hit-shell",name="歴戦の盾片",effects=new[]{new OopartEffectDef{id="hit.defense",effectType="stat-percent",targetId="defense",scalingType="hits",scalingValue=5,maxValue=30}}},
                new{key="first-flame",name="初光の灯",effects=new[]{new OopartEffectDef{id="first.attack",effectType="stat-percent",targetId="attack",baseValue=60,maxValue=60,durationClock=100},Effect("first.buff","buff-power","all",20)}},
                new{key="tools",name="整備士の鍵",effects=new[]{Effect("tools.count","tool-uses","all",1,new EffectCondition{kind="job",targetId="job.panzer"}),Effect("tools.buff","buff-power","all",20)}},
                new{key="skill-two",name="二の刻印",effects=new[]{Effect("two.power","skill-power","slot.1",25),Effect("two.gauge","gauge","all",15)}},
                new{key="support",name="共鳴の標",effects=new[]{Effect("support.buff","buff-power","all",20,new EffectCondition{kind="any",items=new[]{new EffectCondition{kind="job",targetId="job.general"},new EffectCondition{kind="job",targetId="job.artist"}}}),Effect("support.speed","stat-percent","speed",10)}}
            };
            var relics=c.relics.ToList();foreach(var a in additions){string id="relic.tactic."+a.key;var r=new CollectionRelicDef{id=id,name=a.name,abilityId="ability.production."+id,attackPercent=0,materialIds=new[]{c.owners.First(o=>o.kind=="colossus").materialIds[0]}};relics.Add(r);defs.Add(Definition(c,r,a.effects));foreach(var b in c.rewardBands)b.relicIds=b.relicIds.Concat(new[]{id}).ToArray();}
            c.relics=relics.ToArray();c.oopartDefs=defs.ToArray();
        }
        private static OopartDef Definition(CollectionCatalog c,CollectionRelicDef r,OopartEffectDef[] effects)=>new OopartDef{id=r.id,name=r.name,directMaterialId=c.resources.First(m=>m.kind=="material" && m.ownerId==c.resources.Single(x=>x.id==r.materialIds[0]).ownerId && m.minDropLevel>=15).id,effects=effects,randomStats=new[]{new RandomStatDef{stat="hp",maximum=400},new RandomStatDef{stat="attack",maximum=100},new RandomStatDef{stat="physical-defense",maximum=100},new RandomStatDef{stat="magic-defense",maximum=100},new RandomStatDef{stat="speed",maximum=12}}};
    }
}
