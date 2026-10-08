using System;
using System.Globalization;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class PlayRewardState
    {
        public long totalPlaySeconds;
        public double rewardRemainderSeconds;
        public string lastDailyRewardDate;
        public void Validate(){if(totalPlaySeconds<0 || double.IsNaN(rewardRemainderSeconds) || double.IsInfinity(rewardRemainderSeconds) || rewardRemainderSeconds<0 || rewardRemainderSeconds>=1800 || !string.IsNullOrEmpty(lastDailyRewardDate) && !DateTime.TryParseExact(lastDailyRewardDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new ArgumentException("Invalid play reward state.");}
    }
    public static class DailyRewardService
    {
        public static string Day(DateTime utc) {if(utc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC required.");return utc.AddHours(4).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
        public static int Apply(PlayRewardState state,DateTime utc)
        {string day=Day(utc);if(string.CompareOrdinal(day,state.lastDailyRewardDate)<=0)return 0;state.lastDailyRewardDate=day;return 300;}
    }
    public static class PlayTimeRewardService
    {
        public static int Apply(PlayRewardState state,double seconds)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||seconds>3600)throw new ArgumentException("Invalid elapsed time.");
            double elapsed=state.rewardRemainderSeconds+seconds;long periods=(long)(elapsed/1800);
            state.totalPlaySeconds=checked(state.totalPlaySeconds+(long)Math.Floor(elapsed)-(long)Math.Floor(state.rewardRemainderSeconds));
            state.rewardRemainderSeconds=elapsed-periods*1800;return checked((int)(periods*100));
        }
    }
    public static class MaterialExchangeService
    {
        public static int UnitCost(CollectionResourceDef resource)=>resource!=null && resource.kind=="material" && resource.rarity<=2 && resource.rarity>=1 && resource.minDropLevel<=20 && !resource.partBreakOnly?resource.rarity==1?500:2000:0;
        public static bool Unlocked(FormalCampaignSave save,string id)=>save.collection!=null && (save.collection.materials.Any(m=>m.id==id) || (save.materialExchangeUnlockedIds??Array.Empty<string>()).Contains(id));
        public static int Maximum(FormalCampaignSave save,CollectionResourceDef resource)=>UnitCost(resource)==0 || !Unlocked(save,resource.id)?0:save.growth.nectar/UnitCost(resource);
        public static void Apply(FormalCampaignSave save,CollectionResourceDef resource,int quantity)
        {
            if(quantity<1 || quantity>Maximum(save,resource))throw new ArgumentException("交換条件・残高を確認してください。");
            save.growth.nectar=checked(save.growth.nectar-UnitCost(resource)*quantity);
            var material=save.collection.materials.SingleOrDefault(m=>m.id==resource.id);
            if(material==null){material=new CollectionMaterial{id=resource.id,sourceColossusId=resource.ownerId};save.collection.materials=save.collection.materials.Concat(new[]{material}).ToArray();}
            material.amount=checked(material.amount+quantity);save.materialExchangeUnlockedIds=(save.materialExchangeUnlockedIds??Array.Empty<string>()).Union(new[]{resource.id}).ToArray();
        }
    }
    public sealed partial class FormalCampaignJournal
    {
        public bool CommitPlayRewards(double seconds,DateTime utc,Func<FormalCampaignSave,bool> save)
        {
            Ready();var next=Snapshot;
            if(next.playRewards==null){var old=next.engagement;next.playRewards=new PlayRewardState{totalPlaySeconds=old?.activeSeconds??0,rewardRemainderSeconds=(old?.activeSeconds??0)-1800*(old?.claimedPeriods??0),lastDailyRewardDate=old?.lastLoginDay>0?DateTime.ParseExact(old.lastLoginDay.ToString(),"yyyyMMdd",CultureInfo.InvariantCulture).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture):null};}
            int stones=checked(DailyRewardService.Apply(next.playRewards,utc)+PlayTimeRewardService.Apply(next.playRewards,seconds));
            next.growth.stones=checked(next.growth.stones+stones);if(stones>0)next.growth.revision=checked(next.growth.revision+1);
            next.revision=checked(next.revision+1);return Persist(next,save);
        }
        public bool CommitMaterialExchange(string id,int quantity,long revision,CollectionCatalog catalog,Func<FormalCampaignSave,bool> save)
        {Ready();if(revision!=Revision)throw new ArgumentException("Stale exchange.");var next=Snapshot;MaterialExchangeService.Apply(next,catalog.resources.Single(r=>r.id==id),quantity);next.growth.revision=checked(next.growth.revision+1);next.revision=checked(next.revision+1);return Persist(next,save);}
    }
}
