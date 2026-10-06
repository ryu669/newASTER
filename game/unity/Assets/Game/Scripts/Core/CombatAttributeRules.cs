using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class AttributeResistanceDef
    {
        public string attribute;
        // Positive means resistance, negative means weakness. 10000 bp = 100%.
        public int resistanceBp;
        public AttributeResistanceDef Copy() => (AttributeResistanceDef)MemberwiseClone();
    }
    public static class CombatAttributeRules
    {
        public static readonly string[] Kinds={"斬撃","刺突","打撃","銃弾","ビーム","火","水","氷","風","雷","光","闇"};
        public static void Validate(string[] attributes)
        {
            var a=attributes??Array.Empty<string>();
            if(a.Length>5 || a.Distinct().Count()!=a.Length || a.Any(x=>!Kinds.Contains(x)))throw new ArgumentException("Skills require zero to five distinct known attributes.");
        }
        public static void ValidateResistances(AttributeResistanceDef[] resistances)
        {
            var r=resistances??Array.Empty<AttributeResistanceDef>();
            if(r.Any(x=>x==null || !Kinds.Contains(x.attribute) || x.resistanceBp < -10000 || x.resistanceBp>10000) || r.Select(x=>x.attribute).Distinct().Count()!=r.Length)throw new ArgumentException("Invalid attribute resistance.");
        }
        // Average each attribute's multiplier: adding attributes cannot multiply weaknesses.
        public static decimal Multiplier(string[] attributes,AttributeResistanceDef[] resistances)
        {
            Validate(attributes);ValidateResistances(resistances);
            if(attributes==null || attributes.Length==0)return 1m;
            return attributes.Average(a=>1m-(resistances??Array.Empty<AttributeResistanceDef>()).Where(r=>r.attribute==a).Select(r=>r.resistanceBp/10000m).DefaultIfEmpty(0).Single());
        }
        public static string Labels(string[] attributes)=>attributes==null || attributes.Length==0?"無属性":string.Join("・",attributes);
        public static string Describe(AttributeResistanceDef[] resistances)=>string.Join(" ／ ",(resistances??Array.Empty<AttributeResistanceDef>()).Where(r=>r.resistanceBp!=0).Select(r=>r.attribute+(r.resistanceBp<0?"弱点 ":"耐性 ")+Math.Abs(r.resistanceBp/100m)+"%"));
    }
}
