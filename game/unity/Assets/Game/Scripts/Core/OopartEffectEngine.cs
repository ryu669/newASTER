using System;
using System.Linq;
namespace NewAster.Core
{
    public sealed class OopartEffectContext
    {
        public string jobId,skillId,action,attribute,targetStatus;public string[] attributes=Array.Empty<string>(),targetStatuses=Array.Empty<string>(),formationJobs=Array.Empty<string>();public int hp=100,maxHp=100,hits;public int skillSlot=-1;public long clock;public bool inBattle=true;
    }
    public static class OopartEffectEngine
    {
        public static void Validate(OopartEffectDef e)
        {
            if(!CollectionCatalog.ValidId(e.id) || !new[]{"stat-percent","attribute","skill-power","cast-percent","wt","gauge","part","status","buff-power","tool-uses"}.Contains(e.effectType) || !new[]{"constant","turn-growth","turn-decay","hp-missing","hits"}.Contains(e.scalingType) || e.minValue<0 || e.maxValue<e.minValue || e.maxValue>1000 || e.baseValue<0 || e.baseValue>e.maxValue || e.scalingValue<0 || e.durationClock<0 || e.conditions==null || e.effectType=="status" && (!EnemyStatusState.Kinds.Contains(e.targetId) || e.maxValue>20) || e.effectType=="tool-uses" && (e.maxValue>10 || e.scalingType!="constant" || e.durationClock!=0) || e.effectType=="cast-percent" && e.maxValue>90 || e.effectType=="stat-percent" && e.targetId!="all" && e.targetId!="defense" && !StatValues.Names.Contains(e.targetId))throw new ArgumentException("Invalid oopart special effect.");
            foreach(var c in e.conditions)ValidateCondition(c,0);
        }
        private static void ValidateCondition(EffectCondition c,int depth)
        {
            if(c==null || depth>16 || c.items==null || !new[]{"always","all","any","job","skill","action","attribute","hp-at-most","hp-at-least","hits-at-least","turn-at-least","turn-at-most","target-status","formation-job"}.Contains(c.kind) || c.value<0 || (c.kind=="all" || c.kind=="any") && c.items.Length==0 || c.kind.StartsWith("hp-",StringComparison.Ordinal) && c.value>100)throw new ArgumentException("Invalid oopart condition.");
            foreach(var i in c.items)ValidateCondition(i,depth+1);
        }
        public static bool? Condition(EffectCondition c,OopartEffectContext x)
        {
            if(c.kind=="always")return true;
            if(c.kind=="all" || c.kind=="any"){var values=c.items.Select(i=>Condition(i,x)).ToArray();return c.kind=="all"?values.Any(v=>v==false)?false:values.Any(v=>v==null)?(bool?)null:true:values.Any(v=>v==true)?true:values.Any(v=>v==null)?(bool?)null:false;}
            if(c.kind=="job")return x.jobId==c.targetId;
            if(c.kind=="formation-job")return x.formationJobs.Count(j=>j==c.targetId)>=c.value;
            if(!x.inBattle)return null;
            switch(c.kind){case "skill":return x.skillId==c.targetId || c.targetId=="slot."+x.skillSlot;case "action":return x.action==c.targetId;case "attribute":return x.attribute==c.targetId || x.attributes.Contains(c.targetId);case "hp-at-most":return (long)x.hp*100<=(long)x.maxHp*c.value;case "hp-at-least":return (long)x.hp*100>=(long)x.maxHp*c.value;case "hits-at-least":return x.hits>=c.value;case "turn-at-least":return x.clock/100>=c.value;case "turn-at-most":return x.clock/100<c.value;case "target-status":return x.targetStatus==c.targetId || x.targetStatuses.Contains(c.targetId);default:throw new ArgumentException("Unknown condition.");}
        }
        public static bool? Active(OopartEffectDef e,OopartEffectContext x){var values=e.conditions.Select(c=>Condition(c,x)).ToArray();return values.Any(v=>v==false)?false:values.Any(v=>v==null)?(bool?)null:true;}
        public static int Value(OopartEffectDef e,OopartEffectContext x)
        {
            if(Active(e,x)!=true || e.durationClock>0 && x.clock>=e.durationClock)return 0;
            long n=e.baseValue;
            if(e.scalingType=="turn-growth")n+=x.clock/100*e.scalingValue;
            else if(e.scalingType=="turn-decay")n-=x.clock/100*e.scalingValue;
            else if(e.scalingType=="hp-missing")n+=(long)Math.Max(0,x.maxHp-x.hp)*e.scalingValue/Math.Max(1,x.maxHp);
            else if(e.scalingType=="hits")n+=(long)x.hits*e.scalingValue;
            return (int)Math.Max(e.minValue,Math.Min(e.maxValue,n));
        }
        public static string ConditionsText(EffectCondition c)=>c.kind=="always"?"常時":c.kind=="all"?"("+string.Join(" かつ ",c.items.Select(ConditionsText))+")":c.kind=="any"?"("+string.Join(" または ",c.items.Select(ConditionsText))+")":c.kind=="job"?"ジョブ："+c.targetId.Replace("job.",""):c.kind=="skill"?"スキル："+c.targetId:c.kind=="attribute"?c.targetId+"属性":c.kind=="hp-at-most"?"HP "+c.value+"%以下":c.kind=="hp-at-least"?"HP "+c.value+"%以上":c.kind=="hits-at-least"?"被弾 "+c.value+"回以上":c.kind=="turn-at-least"?c.value+"ターン経過":c.kind=="turn-at-most"?"最初の"+c.value+"ターン":c.kind=="target-status"?"対象："+EnemyStatusState.Label(c.targetId):c.kind=="formation-job"?c.targetId+"を"+c.value+"人編成":"行動："+c.targetId;
        public static string Describe(OopartEffectDef e)=>e.effectType+" ／ "+e.targetId+" ＋"+e.baseValue+(e.effectType=="status" || e.effectType=="wt" || e.effectType=="tool-uses"?"":"%")+(e.scalingType=="constant"?"":" ／ "+e.scalingType+" "+e.scalingValue+"・最大"+e.maxValue)+(e.durationClock>0?" ／ "+e.durationClock/100+"ターン":"")+" ／ "+(e.conditions.Length==0?"常時":string.Join(" かつ ",e.conditions.Select(ConditionsText)));
    }
    public sealed class BuffInstance
    {
        public string sourceActorId,sourceId,kind;public int initialValue,value,decayAmount,decayPeriod=100;public long expiresAt,nextDecayAt;public bool extendable=true,expired;
        public bool Active(long now)=>!expired && expiresAt>now;
        public void Tick(long now){if(expired)return;while(decayAmount>0 && nextDecayAt<=now && nextDecayAt<expiresAt){value=Math.Max(0,value-decayAmount);nextDecayAt+=decayPeriod;}if(now>=expiresAt)expired=true;}
        public bool Extend(long now,int amount){Tick(now);if(!Active(now) || !extendable || amount<=0)return false;expiresAt=checked(expiresAt+amount);return true;}
    }
}
