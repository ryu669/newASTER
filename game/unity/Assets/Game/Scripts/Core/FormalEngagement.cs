using System;
using System.Linq;
namespace NewAster.Core
{
    [Serializable] public sealed class FormalEngagementState
    {
        public int version=1;
        public long activeSeconds,claimedPeriods;
        public int lastLoginDay;
        public void Validate(){if(version!=1 || activeSeconds<0 || claimedPeriods<0 || claimedPeriods>activeSeconds/1800 || lastLoginDay!=0 && !FormalEngagementRules.ValidDay(lastLoginDay))throw new ArgumentException("Invalid engagement ledger.");}
    }
    [Serializable] public sealed class FormalEngagementRules
    {
        public const string ProductionVersion="engagement-production-2026-10-05";
        public string version="engagement-trial-2026-10-03";
        public int loginStones=300,periodStones=100,periodSeconds=1800;
        public void Validate(){if(version!="engagement-trial-2026-10-03" && version!=ProductionVersion || loginStones<1 || periodStones<1 || periodSeconds!=1800)throw new ArgumentException("Invalid engagement rules.");}
        public static int Day(DateTime utc){if(utc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC clock required.");return int.Parse(utc.AddHours(9).ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture));}
        public static bool ValidDay(int day)=>DateTime.TryParseExact(day.ToString(),"yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _);
    }
    public sealed class FormalEngagementRequest
    {
        public bool Login {get;}
        public int Day {get;}
        public long Period {get;}
        public long Revision {get;}
        public string Id=>Login?"grant.login."+Day:"grant.active-time."+Period;
        public string Signature=>"engagement|engagement-trial-2026-10-03|"+(Login?"login|"+Day:"time|"+Period);
        public FormalEngagementRequest(bool login,int day,long period,long revision){if(login && !FormalEngagementRules.ValidDay(day) || !login && period<1 || revision<0)throw new ArgumentException("Invalid reward request.");Login=login;Day=day;Period=period;Revision=revision;}
    }
    public sealed partial class FormalCampaignJournal
    {
        private FormalEngagementRequest pendingEngagement;
        public int PreviewEngagement(FormalEngagementRequest request,FormalEngagementRules rules,DateTime nowUtc)
        {
            Ready();return EngagementAmount(request,rules,nowUtc);
        }
        private int EngagementAmount(FormalEngagementRequest r,FormalEngagementRules rules,DateTime nowUtc)
        {
            rules.Validate();if(r.Revision!=current.revision)throw new ArgumentException("Stale engagement request.");
            var ledger=current.engagement??new FormalEngagementState();
            if(r.Login){if(r.Day!=FormalEngagementRules.Day(nowUtc) || r.Day<=ledger.lastLoginDay)throw new ArgumentException("Login day unavailable.");return rules.loginStones;}
            if(r.Period<=ledger.claimedPeriods || r.Period!=ledger.activeSeconds/rules.periodSeconds)throw new ArgumentException("Playtime reward unavailable.");
            return checked((int)((r.Period-ledger.claimedPeriods)*rules.periodStones));
        }
        public bool CommitActiveSeconds(int seconds,Func<FormalCampaignSave,bool> save)
        {
            Ready();if(seconds<1 || seconds>3600)throw new ArgumentException("Invalid active checkpoint.");
            var next=Snapshot;if(next.engagement==null)next.engagement=new FormalEngagementState();next.engagement.activeSeconds=checked(next.engagement.activeSeconds+seconds);next.revision=checked(next.revision+1);return Persist(next,save);
        }
        public GrowthCommitResult CommitEngagement(FormalEngagementRequest r,FormalEngagementRules rules,DateTime nowUtc,Func<FormalCampaignSave,bool> save)
        {
            if(r==null || save==null)throw new ArgumentNullException();if(writing)throw new InvalidOperationException("Concurrent save.");
            var receipt=current.growth.receipts.SingleOrDefault(x=>x.transactionId==r.Id);
            if(receipt!=null){if(receipt.signature!=r.Signature)throw new ArgumentException("Reward ID reused.");return GrowthCommitResult.AlreadyCommitted;}
            if(HasPending){if(pendingEngagement==null || pendingEngagement.Id!=r.Id || pendingEngagement.Revision!=r.Revision || pendingEngagement.Signature!=r.Signature)throw new InvalidOperationException("Retry same reward first.");}
            else {
                int amount=EngagementAmount(r,rules,nowUtc);var next=Snapshot;if(next.engagement==null)next.engagement=new FormalEngagementState();
                next.growth.stones=checked(next.growth.stones+amount);next.growth.revision=checked(next.growth.revision+1);
                next.growth.receipts=next.growth.receipts.Concat(new[]{new GrowthReceipt {transactionId=r.Id,signature=r.Signature}}).ToArray();
                if(r.Login)next.engagement.lastLoginDay=r.Day;else next.engagement.claimedPeriods=r.Period;
                next.revision=checked(next.revision+1);next.Validate();pending=next;pendingEngagement=r;
            }
            if(!Persist(pending,save))return GrowthCommitResult.SaveFailed;
            pending=null;pendingEngagement=null;return GrowthCommitResult.Committed;
        }
    }
}
