using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class ColossusActionCombatDef
    {
        public string name,targetRule="single",damageType="physical",requiredPartId;
        public EnemyStatusDef[] statusEffects;
        public int damagePercent=100,gaugeGain=1,drainAmount=1,waitPercent=100;
        public ColossusActionCombatDef Copy(){var c=(ColossusActionCombatDef)MemberwiseClone();c.statusEffects=statusEffects?.Select(e=>e.Copy()).ToArray();return c;}
    }
    [Serializable] public sealed class ColossusPartCombatDef
    {
        public string id,role,breakEffect;
        public int baseHp,hpPerLevel;
    }
    [Serializable] public sealed class ColossusCombatDef
    {
        public const string Version="colossus-trial-2026-10-03";
        public const string Plan8Version="colossus-plan8-2026-10-04";
        public const string Plan9Version="colossus-plan9-2026-10-04";
        public const string ProductionVersion="colossus-production-2026-10-05";
        public static bool SupportedVersion(string value)=>value==Version || value==Plan8Version || value==Plan9Version || value==ProductionVersion;
        public int schemaVersion=1;
        public string contentVersion=Version,status="trial",id;
        public int baseHp,hpPerLevel,gaugeMax,baseDamage,damagePerLevel,majorBonus,ultimateBonus;
        public string normalAction,enragedAction,majorAction,ultimateAction;
        public ColossusPartCombatDef[] parts;
        public ColossusActionCombatDef[] actionCycle;
        public int enemySpeed=90,majorWaitPercent=150,enrageHpPercent=50,enrageDamagePercent=125,attackBreakDamagePercent=75;
        public string majorDamageType="magic";
        public AttributeResistanceDef[] attributeResistances;
        public EnemyStatusResistanceDef[] statusResistances;
        public void Validate()
        {
            CombatAttributeRules.ValidateResistances(attributeResistances);new EnemyStatusState(statusResistances);foreach(var a in actionCycle??Array.Empty<ColossusActionCombatDef>())foreach(var e in a.statusEffects??Array.Empty<EnemyStatusDef>())e.Validate();
            if(schemaVersion!=1 || !SupportedVersion(contentVersion) || !(status=="trial" || contentVersion==ProductionVersion && status=="production-candidate") || !CollectionCatalog.ValidId(id) || baseHp<1 || hpPerLevel<0 || gaugeMax<2 || baseDamage<1 || damagePerLevel<0 || majorBonus<1 || ultimateBonus<majorBonus ||
               new[]{normalAction,enragedAction,majorAction,ultimateAction}.Any(string.IsNullOrWhiteSpace) ||
               parts==null || parts.Length<4 || parts.Length>6 || parts.Any(p=>p==null || !CollectionCatalog.ValidId(p.id) || p.baseHp<1 || p.hpPerLevel<0 ||
                   !new[]{"gauge","attack","armor","drain","auxiliary"}.Contains(p.role) || p.breakEffect!=(p.role=="gauge"?"gauge-down":"")) ||
               parts.Select(p=>p.id).Distinct().Count()!=parts.Length || new[]{"gauge","attack","armor","drain"}.Any(role=>parts.Count(p=>p.role==role)!=1))
                throw new ArgumentException("Invalid colossus battle definition.");
            if(enemySpeed<1 || enemySpeed>1000 || majorWaitPercent<1 || majorWaitPercent>500 || enrageHpPercent<1 || enrageHpPercent>99 || enrageDamagePercent<100 || enrageDamagePercent>300 || attackBreakDamagePercent<1 || attackBreakDamagePercent>100 || !new[]{"physical","magic"}.Contains(majorDamageType))throw new ArgumentException("Invalid colossus action tuning.");
            if(contentVersion==Plan9Version && (actionCycle==null || actionCycle.Length==0))throw new ArgumentException("Plan9 enemy requires an authored action cycle.");
            if(actionCycle!=null && actionCycle.Length>0 && (actionCycle.Length<2 || actionCycle.Length>6 || actionCycle.Any(a=>a==null || string.IsNullOrWhiteSpace(a.name) || !new[]{"single","all","lowest-hp","highest-resource"}.Contains(a.targetRule) || !new[]{"physical","magic"}.Contains(a.damageType) || a.damagePercent<1 || a.damagePercent>300 || a.gaugeGain<0 || a.gaugeGain>3 || a.drainAmount<0 || a.drainAmount>5 || a.waitPercent<1 || a.waitPercent>500 || !string.IsNullOrEmpty(a.requiredPartId) && !parts.Any(p=>p.id==a.requiredPartId)) || !actionCycle.Any(a=>string.IsNullOrEmpty(a.requiredPartId))))throw new ArgumentException("Invalid colossus action cycle or missing fallback.");
            checked{var hp=baseHp+50*hpPerLevel;var damage=baseDamage+50*damagePerLevel+ultimateBonus;foreach(var p in parts){var partHp=p.baseHp+50*p.hpPerLevel;}}
        }
        public ColossusCombatDef Copy()=>new ColossusCombatDef {
            statusResistances=statusResistances?.Select(r=>new EnemyStatusResistanceDef{kind=r.kind,resistanceBp=r.resistanceBp}).ToArray(),attributeResistances=attributeResistances?.Select(a=>a.Copy()).ToArray(),schemaVersion=schemaVersion,contentVersion=contentVersion,status=status,id=id,baseHp=baseHp,hpPerLevel=hpPerLevel,gaugeMax=gaugeMax,baseDamage=baseDamage,damagePerLevel=damagePerLevel,majorBonus=majorBonus,ultimateBonus=ultimateBonus,
            normalAction=normalAction,enragedAction=enragedAction,majorAction=majorAction,ultimateAction=ultimateAction,
            actionCycle=actionCycle?.Select(a=>a.Copy()).ToArray(),enemySpeed=enemySpeed,majorWaitPercent=majorWaitPercent,enrageHpPercent=enrageHpPercent,enrageDamagePercent=enrageDamagePercent,attackBreakDamagePercent=attackBreakDamagePercent,majorDamageType=majorDamageType,
            parts=parts.Select(p=>new ColossusPartCombatDef {id=p.id,role=p.role,breakEffect=p.breakEffect,baseHp=p.baseHp,hpPerLevel=p.hpPerLevel}).ToArray()
        };
    }
}
