using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed partial class BattleHero
    {
        internal Func<string,int> OopartStat, OopartValue;
        internal Func<BattleSkill,string,int> OopartDamage,OopartTargetAttack;
        internal Func<long> OopartClock;
        internal Func<bool> ResourceGainBlocked;
        internal int OopartHitCount;
        internal void InitializeOopartHitPoints(){HitPoints=MaxHitPoints;if(IsPanzer)FleshHitPoints=FleshMaxHitPoints;}
        private string lastOopartHit;
        private readonly List<BuffInstance> oopartBuffs=new List<BuffInstance>();
        public IReadOnlyList<BuffInstance> OopartBuffs=>Array.AsReadOnly(oopartBuffs.Where(b=>b.Active(OopartClock?.Invoke()??0)).ToArray());
        internal int OopartStatPercent(string key)=>OopartStat?.Invoke(key)??0;
        internal int OopartBuffPercent(string key)=>oopartBuffs.Where(b=>b.kind==key && b.Active(OopartClock?.Invoke()??0)).Sum(b=>b.value);
        internal void RegisterOopartHit(string attack){if(OopartClock!=null && lastOopartHit!=attack){lastOopartHit=attack;OopartHitCount++;}}
        internal bool ApplyOopartBuffs(IEnumerable<TimedSelfEffectDef> definitions,string sourceActor,string sourceId,int boost)
        {
            var defs=definitions.ToArray();TimedSelfEffectDef.ValidateAll(defs);if(!IsAlive)return false;long now=OopartClock();
            foreach(var d in defs){oopartBuffs.RemoveAll(b=>b.sourceActorId==sourceActor && b.sourceId==sourceId && b.kind==d.kind);int value=d.percent*(100+boost)/100;oopartBuffs.Add(new BuffInstance{sourceActorId=sourceActor,sourceId=sourceId,kind=d.kind,initialValue=value,value=value,expiresAt=now+100L*d.turns,nextDecayAt=now+100});}return true;
        }
        internal void AddOopartBuff(BuffInstance buff){oopartBuffs.RemoveAll(b=>b.sourceActorId==buff.sourceActorId && b.sourceId==buff.sourceId && b.kind==buff.kind);oopartBuffs.Add(buff);}
        internal void TickOopartBuffs(){if(OopartClock==null)return;long now=OopartClock();foreach(var b in oopartBuffs)b.Tick(now);oopartBuffs.RemoveAll(b=>b.expired);}
        internal void ExtendOopartBuffs(){if(OopartClock==null)return;foreach(var b in oopartBuffs.Where(b=>b.kind!="attack-reduction"))b.Extend(OopartClock(),100);}
    }
    public sealed partial class PlayableBattle
    {
        private bool UsesOoparts=>relicCatalog?.oopartDefs.Length>0 && collectionGrowth?.ooparts!=null;
        private OopartDef ActorOopart(int actor){if(!UsesOoparts)return null;string id=collectionGrowth.ooparts.slots[actor].equippedOopartId;return id==null?null:relicCatalog.Oopart(id);}
        private StatValues OopartFixed(int actor)
        {var d=ActorOopart(actor);return d==null?new StatValues():OopartService.Fixed(collectionGrowth.ooparts.progress.Single(p=>p.oopartId==d.id),d);}
        private OopartEffectContext OopartContext(int actor,int slot=-1,string target=null,BattleSkill skill=null,bool hpReference=false)=>new OopartEffectContext{jobId=jobProfiles[actor].id,skillId=skill?.Id??(slot>=0?commandDefinitions[actor,slot].id:null),skillSlot=slot>=0?slot:skill==null?-1:Enumerable.Range(0,3).Where(i=>commandDefinitions[actor,i].id==skill.Id).DefaultIfEmpty(-1).First(),action=slot>=0?(IsAttackSkill(actor,slot)?"attack":"skill"):skill!=null?"attack":"utility",attributes=skill?.Attributes??(slot>=0?commandDefinitions[actor,slot].attributes??Array.Empty<string>():Array.Empty<string>()),hp=State.Heroes[actor].HitPoints,maxHp=hpReference?State.Heroes[actor].OopartReferenceMaxHp:State.Heroes[actor].MaxHitPoints,hits=State.Heroes[actor].OopartHitCount,clock=Clock,targetStatuses=target==null?Array.Empty<string>():EnemyStatusState.Kinds.Where(k=>State.EnemyStatus(target).Active(k)).ToArray(),formationJobs=jobProfiles.Select(j=>j.id).ToArray()};
        private int OopartBonus(int actor,string type,string target="all",int slot=-1,string enemy=null,BattleSkill skill=null)
        {
            var d=ActorOopart(actor);if(d==null)return 0;var x=OopartContext(actor,slot,enemy,skill,type=="stat-percent" && target=="hp");
            return d.effects.Where(e=>e.effectType==type && (e.targetId==target || e.targetId=="all" || type=="stat-percent" && e.targetId=="defense" && (target=="physical-defense" || target=="magic-defense"))).Sum(e=>e.durationClock==0?OopartEffectEngine.Value(e,x):OopartEffectEngine.Active(e,x)==true?State.Heroes[actor].OopartBuffs.SingleOrDefault(b=>b.sourceId==e.id)?.value??0:0);
        }
        private void InitializeOoparts()
        {
            if(!UsesOoparts)return;
            for(int i=0;i<5;i++){
                int actor=i;var h=State.Heroes[i];h.OopartClock=()=>Clock;h.OopartStat=k=>OopartBonus(actor,"stat-percent",k);h.OopartValue=k=>OopartBonus(actor,k);h.ResourceGainBlocked=()=>JobState(actor)?.Singing??false;
                h.OopartTargetAttack=(skill,target)=>(int)((long)h.BaseAttack*(OopartBonus(actor,"stat-percent","attack",enemy:target,skill:skill)-OopartBonus(actor,"stat-percent","attack"))/100);
                h.OopartDamage=(skill,target)=>OopartBonus(actor,"skill-power","slot."+OopartContext(actor,skill:skill).skillSlot,enemy:target,skill:skill)+skill.Attributes.Sum(a=>OopartBonus(actor,"attribute",a,enemy:target,skill:skill))+(target=="body"?0:OopartBonus(actor,"part","all",enemy:target,skill:skill));
                var d=ActorOopart(i);if(d!=null)foreach(var e in d.effects.Where(e=>e.durationClock>0)){
                    int initial=e.baseValue*(100+OopartBonus(actor,"buff-power"))/100;
                    h.AddOopartBuff(new BuffInstance{sourceActorId=h.Id,sourceId=e.id,kind=e.effectType=="stat-percent"?"stat."+e.targetId:e.effectType,initialValue=initial,value=initial,decayAmount=e.scalingType=="turn-decay"?e.scalingValue:0,nextDecayAt=100,expiresAt=e.durationClock,extendable=e.extendable});
                }

            }
            for(int i=0;i<5;i++)State.Heroes[i].InitializeOopartHitPoints();
            if(CommanderActor>=0)generalOopartBoost=OopartBonus(CommanderActor,"buff-power");
            for(int i=0;i<5;i++){if(Job(i,"panzer"))for(int tool=0;tool<2;tool++)jobStates[i].ToolUses[tool]+=OopartBonus(i,"tool-uses");UpdateJobStatsIfAvailable(i);}
        }
        private void UpdateJobStatsIfAvailable(int actor){if(UsesJobRulesV2)UpdateJobStats(actor);}
        private EnemyStatusDef[] OopartStatuses(int actor,int slot,EnemyStatusDef[] effects)=>(effects??Array.Empty<EnemyStatusDef>()).Select(s=>new EnemyStatusDef{kind=s.kind,amount=Math.Min(1000,s.amount+OopartBonus(actor,"status",s.kind,slot))}).ToArray();
        private bool ApplySourceBuffs(int source,int target,string id,TimedSelfEffectDef[] effects)=>State.Heroes[target].OopartClock==null?State.Heroes[target].ApplySelfEffects(effects):State.Heroes[target].ApplyOopartBuffs(effects,State.Heroes[source].Id,id,OopartBonus(source,"buff-power"));
        private bool JobEmpowered(int actor)=>UsesOoparts?State.Heroes[actor].OopartBuffs.Any(b=>b.kind=="job.empowered"):jobStates[actor].Empowered(Clock);
    }
}
