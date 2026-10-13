using System;
using System.Linq;
using System.Diagnostics;
using NewAster.Core;
public static class SaveHistoryPerformanceTests
{
    private static bool PreviousCheck(GrowthReceipt[] before,FormalGrowthSave next)=>before.Any(r=>!next.receipts.Any(n=>n.transactionId==r.transactionId && n.signature==r.signature && Same(r,n)));
    private static bool Same(GrowthReceipt a,GrowthReceipt b){var x=a.kinderOutcomes??Array.Empty<KinderOutcome>();var y=b.kinderOutcomes??Array.Empty<KinderOutcome>();return x.Length==y.Length && x.Zip(y,(l,r)=>l.kind==r.kind && l.heroineId==r.heroineId && l.amount==r.amount && l.grantKind==r.grantKind).All(v=>v);}
    public static void Run(Action<bool,string> check)
    {
        foreach(int count in new[]{0,1,1000,10000}){
            var before=Enumerable.Range(0,count).Select(i=>new GrowthReceipt{transactionId="history."+i,signature="history|"+i,kinderOutcomes=new[]{new KinderOutcome{kind="heroine",heroineId="heroine.slayer",amount=1,grantKind="fragments"}}}).ToArray();
            Func<FormalGrowthSave> next=()=>new FormalGrowthSave{saveId="history",receipts=before.Select(r=>r.Copy()).Reverse().Concat(new[]{new GrowthReceipt{transactionId="new",signature="new"}}).ToArray()};
            var good=next();good.Validate();check(!before.AnyMissingFrom(good),"Reordered intact history accepted "+count);
            if(count==0)continue;
            foreach(Action<GrowthReceipt> change in new Action<GrowthReceipt>[] {r=>r.signature="changed",r=>r.kinderOutcomes[0].kind="crystal",r=>r.kinderOutcomes[0].heroineId="heroine.r",r=>r.kinderOutcomes[0].amount=2,r=>r.kinderOutcomes[0].grantKind="overflow",r=>r.kinderOutcomes=Array.Empty<KinderOutcome>()}){
                var bad=next();change(bad.receipts[0]);check(before.AnyMissingFrom(bad)==PreviousCheck(before,bad) && before.AnyMissingFrom(bad),"History modification still rejected "+count);
            }
            var missing=next();missing.receipts=missing.receipts.Skip(1).ToArray();check(before.AnyMissingFrom(missing),"Missing history rejected "+count);
            var duplicate=next();duplicate.receipts=duplicate.receipts.Concat(new[]{duplicate.receipts[0].Copy()}).ToArray();bool rejected=false;try{duplicate.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Duplicate history ID rejected "+count);
            if(count==10000){var clock=Stopwatch.StartNew();for(int i=0;i<10;i++)before.AnyMissingFrom(good);clock.Stop();Console.WriteLine("SAVE_HISTORY_MEASUREMENT receipts=10000 linearMeanMs="+(clock.Elapsed.TotalMilliseconds/10).ToString("F3"));}
        }
        Console.WriteLine("SAVE_HISTORY_INTEGRITY_PASS reordered / missing / signature / all reward fields / duplicate IDs");
    }
}
