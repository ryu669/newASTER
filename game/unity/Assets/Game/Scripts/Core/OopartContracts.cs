using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class StatValues
    {
        public int hp,attack,physicalDefense,magicDefense,speed;
        public static readonly string[] Names={"hp","attack","physical-defense","magic-defense","speed"};
        public int Get(string stat)=>stat=="hp"?hp:stat=="attack"?attack:stat=="physical-defense"?physicalDefense:stat=="magic-defense"?magicDefense:stat=="speed"?speed:throw new ArgumentException("Unknown stat.");
        public void Set(string stat,int value){switch(stat){case "hp":hp=value;break;case "attack":attack=value;break;case "physical-defense":physicalDefense=value;break;case "magic-defense":magicDefense=value;break;case "speed":speed=value;break;default:throw new ArgumentException("Unknown stat.");}}
        public StatValues Copy()=>new StatValues{hp=hp,attack=attack,physicalDefense=physicalDefense,magicDefense=magicDefense,speed=speed};
        public static StatValues Add(StatValues a,StatValues b){var n=new StatValues();foreach(var key in Names)n.Set(key,checked(a.Get(key)+b.Get(key)));return n;}
    }
    [Serializable] public sealed class StatGrowthDef
    {
        public StatValues initial=new StatValues{hp=50,attack=10,physicalDefense=5,magicDefense=5,speed=1},perLevel=new StatValues{hp=5,attack=2,physicalDefense=1,magicDefense=1,speed=1};
        public StatValues At(int level){if(level<1 || level>120)throw new ArgumentException("Lv1..120 required.");var n=new StatValues();foreach(var key in StatValues.Names)n.Set(key,checked(initial.Get(key)+perLevel.Get(key)*(level-1)));return n;}
    }
    [Serializable] public sealed class RandomStatDef {public string stat;public int maximum;public int minimumPercent=20,maximumPercent=100;}
    [Serializable] public sealed class EffectCondition
    {
        public string kind="always",targetId;public int value;public EffectCondition[] items=Array.Empty<EffectCondition>();
        public EffectCondition Copy()=>new EffectCondition{kind=kind,targetId=targetId,value=value,items=items.Select(i=>i.Copy()).ToArray()};
    }
    [Serializable] public sealed class OopartEffectDef
    {
        public string id,effectType,targetId,scalingType="constant";public int baseValue,scalingValue,minValue,maxValue=1000,durationClock;public bool extendable=true;
        public EffectCondition[] conditions=Array.Empty<EffectCondition>();
        public OopartEffectDef Copy()=>new OopartEffectDef{id=id,effectType=effectType,targetId=targetId,scalingType=scalingType,baseValue=baseValue,scalingValue=scalingValue,minValue=minValue,maxValue=maxValue,durationClock=durationClock,extendable=extendable,conditions=conditions.Select(c=>c.Copy()).ToArray()};
    }
    [Serializable] public sealed class OopartDef
    {
        public string id,name,directMaterialId;public StatGrowthDef levelStats=new StatGrowthDef();public RandomStatDef[] randomStats=Array.Empty<RandomStatDef>();public OopartEffectDef[] effects=Array.Empty<OopartEffectDef>();
        public int directNectarCost=50000,directMaterialCost=20;
        public OopartDef Copy()=>new OopartDef{id=id,name=name,directMaterialId=directMaterialId,directNectarCost=directNectarCost,directMaterialCost=directMaterialCost,levelStats=new StatGrowthDef{initial=levelStats.initial.Copy(),perLevel=levelStats.perLevel.Copy()},randomStats=randomStats.Select(r=>new RandomStatDef{stat=r.stat,maximum=r.maximum,minimumPercent=r.minimumPercent,maximumPercent=r.maximumPercent}).ToArray(),effects=effects.Select(e=>e.Copy()).ToArray()};
    }
    [Serializable] public sealed class OopartProgress
    {
        public string oopartId;public int level=1,legacyHpMaximum;public StatValues accumulatedRandomStats=new StatValues();
        public OopartProgress Copy()=>new OopartProgress{oopartId=oopartId,level=level,legacyHpMaximum=legacyHpMaximum,accumulatedRandomStats=accumulatedRandomStats.Copy()};
    }
    [Serializable] public sealed class FormationSlot
    {
        public string heroineFormId,equippedOopartId;
        public FormationSlot Copy()=>new FormationSlot{heroineFormId=heroineFormId,equippedOopartId=equippedOopartId};
    }
    [Serializable] public sealed class OopartFormationPreset {public string id,name;public FormationSlot[] slots=Array.Empty<FormationSlot>();}
    [Serializable] public sealed class OopartInventorySave
    {
        public int version=1;public OopartProgress[] progress=Array.Empty<OopartProgress>();public FormationSlot[] slots=Enumerable.Range(0,5).Select(i=>new FormationSlot()).ToArray();public OopartFormationPreset[] presets=Array.Empty<OopartFormationPreset>();
        public OopartInventorySave Copy()=>new OopartInventorySave{version=version,progress=progress.Select(p=>p.Copy()).ToArray(),slots=slots.Select(s=>s.Copy()).ToArray(),presets=presets.Select(p=>new OopartFormationPreset{id=p.id,name=p.name,slots=p.slots.Select(s=>s.Copy()).ToArray()}).ToArray()};
        public void SyncFormation(string[] forms){if(forms==null || forms.Length!=5)return;for(int i=0;i<5;i++)slots[i].heroineFormId=forms[i];}
        public void Validate()
        {
            if(version!=1 || progress==null || progress.Any(p=>p==null || !CollectionCatalog.ValidId(p.oopartId) || p.level<1 || p.level>120 || p.accumulatedRandomStats==null || StatValues.Names.Any(k=>p.accumulatedRandomStats.Get(k)<0) || p.legacyHpMaximum!=0 && p.legacyHpMaximum!=1000) || progress.Select(p=>p.oopartId).Distinct().Count()!=progress.Length || presets==null || presets.Any(p=>p==null || !CollectionCatalog.ValidId(p.id) || string.IsNullOrWhiteSpace(p.name)) || presets.Select(p=>p.id).Distinct().Count()!=presets.Length)throw new ArgumentException("Invalid oopart inventory.");
            foreach(var list in new[]{slots}.Concat(presets.Select(p=>p.slots))){if(list==null || list.Length!=5 || list.Any(s=>s==null || s.heroineFormId!=null && !CollectionCatalog.ValidId(s.heroineFormId) || s.equippedOopartId!=null && !progress.Any(p=>p.oopartId==s.equippedOopartId)) || list.Where(s=>s.equippedOopartId!=null).Select(s=>s.equippedOopartId).Distinct().Count()!=list.Count(s=>s.equippedOopartId!=null) || list.Where(s=>s.heroineFormId!=null).Select(s=>s.heroineFormId).Distinct().Count()!=list.Count(s=>s.heroineFormId!=null))throw new ArgumentException("Invalid formation oopart slots.");}
        }
        public void ValidateContent(CollectionCatalog catalog){Validate();foreach(var p in progress){var d=catalog.Oopart(p.oopartId);foreach(var r in d.randomStats)if(p.accumulatedRandomStats.Get(r.stat)>Math.Max(r.maximum,r.stat=="hp"?p.legacyHpMaximum:0))throw new ArgumentException("Oopart random stat over cap.");}}
    }
    public sealed partial class CollectionCatalog
    {
        public OopartDef[] oopartDefs=Array.Empty<OopartDef>();
        public OopartDef Oopart(string id)=>oopartDefs.Single(d=>d.id==id);
        public void ValidateOopartDefinitions()
        {
            if(oopartDefs==null || oopartDefs.Any(d=>d==null) || oopartDefs.Select(d=>d.id).Distinct().Count()!=oopartDefs.Length)throw new ArgumentException("Invalid oopart definitions.");
            foreach(var d in oopartDefs){if(!CollectionCatalog.ValidId(d.id) || string.IsNullOrWhiteSpace(d.name) || relics==null || !relics.Any(r=>r.id==d.id) || d.levelStats==null || d.levelStats.initial==null || d.levelStats.perLevel==null || StatValues.Names.Any(k=>d.levelStats.initial.Get(k)<0 || d.levelStats.perLevel.Get(k)<0) || d.randomStats==null || d.randomStats.Length!=5 || d.randomStats.Any(r=>r==null) || !d.randomStats.Select(r=>r.stat).OrderBy(s=>s).SequenceEqual(StatValues.Names.OrderBy(s=>s)) || d.randomStats.Any(r=>r.maximum<1 || r.minimumPercent<20 || r.maximumPercent>100 || r.minimumPercent>r.maximumPercent) || d.effects==null || d.effects.Length>2 || d.effects.Any(e=>e==null) || d.effects.Select(e=>e.id).Distinct().Count()!=d.effects.Length || d.directNectarCost<0 || d.directMaterialCost<0 || resources==null || !resources.Any(r=>r.id==d.directMaterialId && r.kind=="material" && r.minDropLevel>=15))throw new ArgumentException("Invalid oopart growth or rescue costs.");foreach(var e in d.effects)OopartEffectEngine.Validate(e);d.levelStats.At(120);}
            foreach(string kind in new[]{"stun","absent"})if(oopartDefs.Count(d=>d.effects.Any(e=>e.effectType=="status" && e.targetId==kind))>1)throw new ArgumentException("Only one control-status oopart per status is allowed.");
        }
    }
}
