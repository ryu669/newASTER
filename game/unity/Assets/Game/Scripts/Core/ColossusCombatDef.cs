using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class ColossusPartCombatDef
    {
        public string id,role,breakEffect;
        public int baseHp,hpPerLevel;
    }
    [Serializable] public sealed class ColossusCombatDef
    {
        public const string Version="colossus-trial-2026-10-03";
        public const string Plan8Version="colossus-plan8-2026-10-04";
        public static bool SupportedVersion(string value)=>value==Version || value==Plan8Version;
        public int schemaVersion=1;
        public string contentVersion=Version,status="trial",id;
        public int baseHp,hpPerLevel,gaugeMax,baseDamage,damagePerLevel,majorBonus,ultimateBonus;
        public string normalAction,enragedAction,majorAction,ultimateAction;
        public ColossusPartCombatDef[] parts;
        public void Validate()
        {
            if(schemaVersion!=1 || !SupportedVersion(contentVersion) || status!="trial" || !CollectionCatalog.ValidId(id) || baseHp<1 || hpPerLevel<0 || gaugeMax<2 || baseDamage<1 || damagePerLevel<0 || majorBonus<1 || ultimateBonus<majorBonus ||
               new[]{normalAction,enragedAction,majorAction,ultimateAction}.Any(string.IsNullOrWhiteSpace) ||
               parts==null || parts.Length<4 || parts.Length>6 || parts.Any(p=>p==null || !CollectionCatalog.ValidId(p.id) || p.baseHp<1 || p.hpPerLevel<0 ||
                   !new[]{"gauge","attack","armor","drain","auxiliary"}.Contains(p.role) || p.breakEffect!=(p.role=="gauge"?"gauge-down":"")) ||
               parts.Select(p=>p.id).Distinct().Count()!=parts.Length || new[]{"gauge","attack","armor","drain"}.Any(role=>parts.Count(p=>p.role==role)!=1))
                throw new ArgumentException("Invalid colossus battle definition.");
            checked{var hp=baseHp+50*hpPerLevel;var damage=baseDamage+50*damagePerLevel+ultimateBonus;foreach(var p in parts){var partHp=p.baseHp+50*p.hpPerLevel;}}
        }
        public ColossusCombatDef Copy()=>new ColossusCombatDef {
            schemaVersion=schemaVersion,contentVersion=contentVersion,status=status,id=id,baseHp=baseHp,hpPerLevel=hpPerLevel,gaugeMax=gaugeMax,baseDamage=baseDamage,damagePerLevel=damagePerLevel,majorBonus=majorBonus,ultimateBonus=ultimateBonus,
            normalAction=normalAction,enragedAction=enragedAction,majorAction=majorAction,ultimateAction=ultimateAction,
            parts=parts.Select(p=>new ColossusPartCombatDef {id=p.id,role=p.role,breakEffect=p.breakEffect,baseHp=p.baseHp,hpPerLevel=p.hpPerLevel}).ToArray()
        };
    }
}
