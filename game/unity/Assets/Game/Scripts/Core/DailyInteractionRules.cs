using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class ReactionStyleProfile
    {
        public string main,sub;
        public ReactionStyleWeightRule[] weightRules=Array.Empty<ReactionStyleWeightRule>();
        public void Validate(){if(!ReactionStyleRules.Ids.Contains(main) || sub!=null && (!ReactionStyleRules.Ids.Contains(sub) || sub==main))throw new ArgumentException("Invalid reaction style.");foreach(var rule in weightRules??Array.Empty<ReactionStyleWeightRule>()){if(rule==null || rule.mainWeight<0 || rule.mainWeight>100 || rule.condition==null)throw new ArgumentException("Invalid style weight rule.");rule.condition.Validate();}}
    }
    [Serializable] public sealed class ReactionStyleWeightRule {public int mainWeight=70;public InteractionCondition condition;}
    public static class ReactionStyleRules
    {
        public static readonly string[] Ids={"active","reserved","cool","affectionate","elegant","bold","innocent","teasing"};
        public static readonly string[] Names={"積極的","控えめ","クール","甘えん坊","上品","豪快","天然","小悪魔"};
        public static string Choose(ReactionStyleProfile person,ReactionStyleProfile form,Func<int,int> draw,DailyInteractionContext context=null)
        {
            var p=form??person??throw new ArgumentException("A main reaction style is required.");p.Validate();
            if(p.sub==null)return p.main;
            int weight=context==null?70:(p.weightRules??Array.Empty<ReactionStyleWeightRule>()).FirstOrDefault(r=>r.condition.Matches(context))?.mainWeight??70;
            int n=draw(100);if(n<0 || n>=100)throw new ArgumentException("Invalid reaction roll.");return n<weight?p.main:p.sub;
        }
    }
    public sealed class DailyActorContext
    {
        public HeroineCombatDef form;
        public int affectionLevel;
        public bool lover;
    }
    public sealed class DailyInteractionContext
    {
        public DailyActorContext subject,partner;
        public string garden,time,weather,interaction;
        public int participantCount=1;
        public string[] participantIds=Array.Empty<string>(),extremes=Array.Empty<string>();
        public Dictionary<string,int> terraformLevels=new Dictionary<string,int>();
        internal DailyInteractionContext Swap()=>new DailyInteractionContext{subject=partner,partner=subject,garden=garden,time=time,weather=weather,interaction=interaction,participantCount=participantCount,participantIds=participantIds,extremes=extremes,terraformLevels=terraformLevels};
    }
    [Serializable] public sealed class InteractionCondition
    {
        public string kind,actor="subject",value;
        public int minimum,maximum=99;
        public InteractionCondition[] items=Array.Empty<InteractionCondition>();
        public void Validate()
        {
            if(!new[]{"and","or","trait","visible-trait","affection","lover","form","garden","time","weather","terraform","extreme","interaction","participant","count"}.Contains(kind) || actor!="subject" && actor!="partner" || minimum<0 || maximum<minimum)throw new ArgumentException("Invalid daily condition.");
            if(kind=="and" || kind=="or"){if(items==null || items.Length==0 || items.Any(c=>c==null))throw new ArgumentException("Empty condition expression.");foreach(var c in items)c.Validate();}
            else if(kind!="affection" && kind!="count" && kind!="lover" && string.IsNullOrEmpty(value))throw new ArgumentException("Missing condition value.");
            if(kind=="trait")InteractionTraitCatalog.Get(value);
        }
        public bool Matches(DailyInteractionContext c)
        {
            if(kind=="and")return items.All(x=>x.Matches(c));if(kind=="or")return items.Any(x=>x.Matches(c));
            var a=actor=="partner"?c.partner:c.subject;
            switch(kind){
                case "trait":return a!=null && InteractionTraitCatalog.Has(a.form,value,true);
                case "visible-trait":return a!=null && InteractionTraitCatalog.VisibleIds(a.form).Contains(value);
                case "affection":return a!=null && a.affectionLevel>=minimum && a.affectionLevel<=maximum;
                case "lover":return a!=null && a.lover;
                case "form":return a?.form.id==value;
                case "garden":return c.garden==value;
                case "time":return c.time==value;
                case "weather":return c.weather==value;
                case "terraform":return c.terraformLevels.TryGetValue(value,out var level) && level>=minimum && level<=maximum;
                case "extreme":return c.extremes.Contains(value);
                case "interaction":return c.interaction==value;
                case "participant":return c.participantIds.Contains(value);
                case "count":return c.participantCount>=minimum && c.participantCount<=maximum;
                default:throw new ArgumentException("Unknown daily condition.");
            }
        }
    }
    [Serializable] public sealed class DailyInteractionDef
    {
        public string id,category,presentationId,scriptId;
        public string[] topicTags=Array.Empty<string>();
        public InteractionCondition[] conditions=Array.Empty<InteractionCondition>();
        public int weight;
        public bool replayable=true,symmetric;
        public void Validate()
        {
            if(!HomeExperienceCatalog.Id(id) || !HomeExperienceCatalog.Id(presentationId) || !new[]{"personal","pair","trait","common"}.Contains(category) || weight<0 || weight>10000 || topicTags==null || topicTags.Length==0 || topicTags.Any(string.IsNullOrEmpty) || topicTags.Distinct().Count()!=topicTags.Length || conditions==null || conditions.Any(c=>c==null))throw new ArgumentException("Invalid daily interaction definition.");
            foreach(var c in conditions)c.Validate();
        }
        public bool Matches(DailyInteractionContext c)=>conditions.All(x=>x.Matches(c)) || symmetric && c.partner!=null && conditions.All(x=>x.Matches(c.Swap()));
    }
    public sealed class DailyInteractionHistory
    {
        private readonly Queue<string> recent=new Queue<string>();
        private string[] lastTopics=Array.Empty<string>();
        public decimal Multiplier(DailyInteractionDef d)=>(recent.Contains(d.id)?.2m:1m)*(d.topicTags.Intersect(lastTopics).Any()?.5m:1m);
        public void Record(DailyInteractionDef d){recent.Enqueue(d.id);while(recent.Count>5)recent.Dequeue();lastTopics=(string[])d.topicTags.Clone();}
    }
    public static class DailyInteractionSelector
    {
        public static int BaseWeight(string category)=>category=="personal"?100:category=="pair"?60:category=="trait"?35:category=="common"?20:throw new ArgumentException("Unknown daily category.");
        public static DailyInteractionDef Choose(IEnumerable<DailyInteractionDef> definitions,DailyInteractionContext context,DailyInteractionHistory history,Func<int,int> draw,IEnumerable<string> excluded=null)
        {
            var all=definitions.ToArray();if(all.Any(d=>d==null) || all.Select(d=>d.id).Distinct().Count()!=all.Length)throw new ArgumentException("Duplicate daily interaction ID.");
            foreach(var d in all)d.Validate();
            var skip=new HashSet<string>(excluded??Array.Empty<string>());
            var candidates=all.Where(d=>d.Matches(context) && !skip.Contains(d.id)).ToArray();
            if(candidates.Length==0)return null;
            var weights=candidates.Select(d=>checked((int)((d.weight==0?BaseWeight(d.category):d.weight)*10*(history?.Multiplier(d)??1m)))).ToArray();
            int total=weights.Sum(),roll=draw(total);if(roll<0 || roll>=total)throw new ArgumentException("Invalid daily selection roll.");
            for(int i=0;i<candidates.Length;i++){roll-=weights[i];if(roll<0)return candidates[i];}throw new InvalidOperationException();
        }
    }
    public sealed class DailyPresentationState
    {
        public float x,y,facing=1,distance;
        public string expression="neutral",furniture,gesture="idle";
    }
    [Serializable] public sealed class DailyPresentationDef
    {
        public string id,title,description,gesture="look",expression,requiredFurniture;
        public string[] capabilities=Array.Empty<string>();
        public int seconds=10;
    }
    public sealed partial class HomeExperienceCatalog
    {
        public DailyInteractionDef[] dailyInteractions=Array.Empty<DailyInteractionDef>();
        public DailyPresentationDef[] dailyPresentations=Array.Empty<DailyPresentationDef>();
        public DailyDateDef[] dailyDates=Array.Empty<DailyDateDef>();
        public void ValidateDailyContent()
        {
            var parts=DailyInteractionContent.Definitions.Concat(dailyInteractions??Array.Empty<DailyInteractionDef>()).ToArray();
            if(parts.Any(d=>d==null) || parts.Select(d=>d.id).Distinct().Count()!=parts.Length)throw new ArgumentException("Duplicate daily content definition.");
            foreach(var p in parts)p.Validate();
            var presentations=dailyPresentations??Array.Empty<DailyPresentationDef>();
            if(presentations.Any(p=>p==null || string.IsNullOrWhiteSpace(p.id) || string.IsNullOrWhiteSpace(p.description) || p.seconds<5 || p.seconds>20) || presentations.Select(p=>p.id).Distinct().Count()!=presentations.Length)throw new ArgumentException("Invalid daily presentation definition.");
            var dates=DailyDateCatalog.Definitions.Concat(dailyDates??Array.Empty<DailyDateDef>()).ToArray();
            if(dates.Any(d=>d==null || !Id(d.id) || string.IsNullOrWhiteSpace(d.name) || string.IsNullOrWhiteSpace(d.topic) || d.minimumAffection<0 || d.minimumAffection>99 || d.ownerId!=null && !heroineIds.Contains(d.ownerId)) || dates.Select(d=>d.id).Distinct().Count()!=dates.Length)throw new ArgumentException("Invalid daily date definition.");
        }
    }
    public static class DailyPresentationResolver
    {
        public static DailyPresentationDef Resolve(string id,string form,string style,IEnumerable<DailyPresentationDef> available,DailyPresentationDef common)
        {
            var all=available.ToArray();var p=all.FirstOrDefault(x=>x.id==form+"/"+id)??all.FirstOrDefault(x=>x.id==style+"/"+id)??all.FirstOrDefault(x=>x.id==id)??common??new DailyPresentationDef{id=id,title="一緒の時間",description="顔を向け、同じ時間を穏やかに過ごしている。"};
            return new DailyPresentationDef{id=p.id,title=p.title,description=p.description,gesture=p.gesture,expression=p.expression,requiredFurniture=p.requiredFurniture,seconds=p.seconds,capabilities=(p.capabilities??Array.Empty<string>()).ToArray()};
        }
    }
}
