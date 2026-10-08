using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class AffectionState
    {
        public string personId;
        public int level,exp,levelCap=20;
        public string[] readEventIds=Array.Empty<string>();

    }
    [Serializable] public sealed class AffectionStreak { public string personId,contentId; public int count; }
    [Serializable] public sealed class AffectionReceipt { public string id,signature; }
    [Serializable] public sealed class AffectionProgress
    {
        public int version=1;
        public AffectionState[] states=Array.Empty<AffectionState>();
        public AffectionStreak[] streaks=Array.Empty<AffectionStreak>();
        public AffectionReceipt[] receipts=Array.Empty<AffectionReceipt>();
        public string[] pendingMaxLevelPersonIds=Array.Empty<string>(),shownMaxLevelPersonIds=Array.Empty<string>();
        // Read-only migration archive: retain original values even if they exceeded the new cap.
        public HomeAffection[] legacyValues=Array.Empty<HomeAffection>();
        public void Validate()
        {
            if(version!=1 || states==null || states.Any(a=>a==null || !HomeExperienceCatalog.Id(a.personId) || a.levelCap!=20 && a.levelCap!=99 || a.level<0 || a.level>a.levelCap || a.exp<0 || a.exp>99 || a.level==a.levelCap && a.exp!=0 || a.readEventIds==null || a.readEventIds.Any(id=>!HomeExperienceCatalog.Id(id)) || a.readEventIds.Distinct().Count()!=a.readEventIds.Length) || states.Select(a=>a.personId).Distinct().Count()!=states.Length)throw new ArgumentException("Invalid affection state.");
            if(streaks==null || streaks.Any(s=>s==null || !states.Any(a=>a.personId==s.personId) || !HomeExperienceCatalog.Id(s.contentId) || s.count<1 || s.count>3) || streaks.Select(s=>s.personId).Distinct().Count()!=streaks.Length)throw new ArgumentException("Invalid affection streak.");
            if(receipts==null || receipts.Any(r=>r==null || !HomeExperienceCatalog.Id(r.id) || string.IsNullOrWhiteSpace(r.signature)) || receipts.Select(r=>r.id).Distinct().Count()!=receipts.Length)throw new ArgumentException("Invalid affection receipt.");
            foreach(var ids in new[]{pendingMaxLevelPersonIds,shownMaxLevelPersonIds})if(ids==null || ids.Distinct().Count()!=ids.Length || ids.Any(id=>!states.Any(a=>a.personId==id && a.level==99)))throw new ArgumentException("Invalid affection milestone.");
            if(pendingMaxLevelPersonIds.Intersect(shownMaxLevelPersonIds).Any() || legacyValues==null || legacyValues.Any(v=>v==null || v.value<0 || !HomeExperienceCatalog.Id(v.heroineId)))throw new ArgumentException("Invalid affection archive.");
        }
    }
    public static class AffectionSaveAdapter
    {
        public static void Migrate(FormalCampaignSave s,HomeExperienceCatalog c)
        {
            bool legacy=s.affection==null;
            if(legacy)s.affection=new AffectionProgress{legacyValues=(s.home?.affections??Array.Empty<HomeAffection>()).Select(a=>new HomeAffection{heroineId=a.heroineId,value=a.value}).ToArray()};
            foreach(var group in s.growth.heroines.Select(h=>h.heroineId).GroupBy(c.PersonId)){
                if(s.affection.states.Any(a=>a.personId==group.Key))continue;
                int value=legacy?(s.home?.affections??Array.Empty<HomeAffection>()).Where(a=>group.Contains(a.heroineId)).Select(a=>a.value).DefaultIfEmpty(c.InitialAffection(group.Key)).Max():c.InitialAffection(group.Key);
                if(legacy && group.Any(id=>s.home?.loverHeroineIds.Contains(id)??false))value=Math.Max(10,value);
                s.affection.states=s.affection.states.Concat(new[]{new AffectionState{personId=group.Key,level=Math.Min(99,value),levelCap=value>20?99:20,readEventIds=(s.home?.readEventIds??Array.Empty<string>()).Where(id=>c.events.Any(e=>e.id==id && c.PersonId(e.heroineId)==group.Key)).ToArray()}}).ToArray();
            }
            s.affection.Validate();
        }
        public static void ValidateContent(FormalCampaignSave s,HomeExperienceCatalog c)
        {
            s.affection?.Validate();if(s.affection==null)return;
            var people=s.growth.heroines.Select(h=>c.PersonId(h.heroineId)).Distinct().ToArray();
            if(s.affection.states.Any(a=>!people.Contains(a.personId) || a.readEventIds.Any(id=>!c.events.Any(e=>e.id==id && c.PersonId(e.heroineId)==a.personId))))throw new ArgumentException("Unknown affection owner or event.");
        }
        public static void MarkRead(FormalCampaignSave s,HomeExperienceCatalog c,string eventId)
        {
            if(s.affection==null)return;var ev=c.events.Single(e=>e.id==eventId);var state=AffectionService.State(s,c,ev.heroineId);state.readEventIds=state.readEventIds.Union(new[]{eventId}).ToArray();
        }
    }
    [Serializable] public sealed class InitialAffectionDef { public string personId; public int level; }
    public sealed partial class HomeExperienceCatalog
    {
        public InitialAffectionDef[] initialAffections=Array.Empty<InitialAffectionDef>();
        public HeroineEventDef[] affectionEvents=Array.Empty<HeroineEventDef>();
        public int InitialAffection(string id)=>initialAffections?.SingleOrDefault(a=>a.personId==id)?.level??0;
    }
    public static class AffectionService
    {
        public static bool IsLover(AffectionState a)=>a.level>=10;
        public static AffectionState State(FormalCampaignSave s,HomeExperienceCatalog c,string formId)
        { AffectionSaveAdapter.Migrate(s,c);return s.affection.states.Single(a=>a.personId==c.PersonId(formId)); }
        public static void Add(AffectionState a,int amount,AffectionProgress progress)
        {
            if(amount<0)throw new ArgumentException("Affection cannot decrease.");if(a.level==a.levelCap)return;
            int total=checked(a.level*100+a.exp+amount);a.level=Math.Min(a.levelCap,total/100);a.exp=a.level==a.levelCap?0:total%100;
            if(a.level==99 && !progress.shownMaxLevelPersonIds.Contains(a.personId))progress.pendingMaxLevelPersonIds=progress.pendingMaxLevelPersonIds.Union(new[]{a.personId}).ToArray();
        }
    }
    public static class AffectionRewardService
    {
        public static int Interaction(FormalCampaignSave s,HomeExperienceCatalog c,string formId,string contentId)
        {
            if(!HomeExperienceCatalog.Id(contentId))throw new ArgumentException("Interaction identity required.");
            var a=AffectionService.State(s,c,formId);var streak=s.affection.streaks.SingleOrDefault(t=>t.personId==a.personId);
            if(streak==null){streak=new AffectionStreak{personId=a.personId};s.affection.streaks=s.affection.streaks.Concat(new[]{streak}).ToArray();}
            streak.count=streak.contentId==contentId?Math.Min(3,streak.count+1):1;streak.contentId=contentId;int amount=streak.count==1?5:streak.count==2?3:1;AffectionService.Add(a,amount,s.affection);return amount;
        }
        public static void Discovery(FormalCampaignSave s,HomeExperienceCatalog c,params string[] forms)
        {foreach(var id in forms.Where(id=>!string.IsNullOrEmpty(id)).GroupBy(c.PersonId).Select(g=>g.First()))AffectionService.Add(AffectionService.State(s,c,id),3,s.affection);}
        public static void Battle(FormalCampaignSave s,HomeExperienceCatalog c,string[] forms,BattleEndReason reason)
        {int amount=reason==BattleEndReason.Victory?2:reason==BattleEndReason.Defeat?1:0;if(amount==0)return;foreach(var id in forms.Distinct().GroupBy(c.PersonId).Select(g=>g.First()))AffectionService.Add(AffectionService.State(s,c,id),amount,s.affection);}
    }
    public static class AffectionRingService
    {
        public const int Price=10000;
        public static void Purchase(FormalCampaignSave s)
        {if(s.growth.stones<Price)throw new ArgumentException("召喚石が不足しています。");s.growth.stones-=Price;s.growth.eternalRings=checked(s.growth.eternalRings+1);}
        public static void Use(FormalCampaignSave s,HomeExperienceCatalog c,string formId)
        {var a=AffectionService.State(s,c,formId);if(a.level!=20 || a.levelCap!=20 || s.growth.eternalRings<1)throw new ArgumentException("Lv20・未使用の人物と指輪1個が必要です。");s.growth.eternalRings--;a.levelCap=99;}
    }
}
