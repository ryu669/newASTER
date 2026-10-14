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
        string nested="{\"growth\":{\"receipts\":["+string.Join(",",Enumerable.Repeat("{\"text\":\"brace } [ \\\" home\",\"home\":true}",10000))+"]},\"ho\\u006de\":null,\"tail\":7}";
        // Missing root fields must not allocate one token for every nested receipt.
        FormalCampaignJsonShape.HasRootMember(nested,"absent");
        long allocated=GC.GetAllocatedBytesForCurrentThread();
        bool absent=!FormalCampaignJsonShape.HasRootMember(nested,"absent");
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        check(absent && allocated<16384,"Large nested history root scan has bounded temporary allocation");
        check(FormalCampaignJsonShape.RootMemberIsNull(nested,"home") && FormalCampaignJsonShape.RootInt32Member(nested,"tail")==7,"Large history skips nested keys and escaped brackets but retains escaped root keys");
        var shape=FormalCampaignJsonShape.Inspect(nested);
        check(shape.Contains("growth") && shape.IsNull("home") && !shape.Contains("absent") && shape.Json("tail")=="7","Single-pass root index matches nested and escaped key semantics");
        string nulled=FormalCampaignJsonShape.WithNullRootMembers(nested,"growth","tail","growth","absent");
        check(nulled=="{\"growth\":null,\"ho\\u006de\":null,\"tail\":null}","Batch null normalization replaces disjoint members once and preserves escaped keys");
        check(FormalCampaignJsonShape.WithRootMemberJson(nested,"home","{}").EndsWith("\"ho\\u006de\":{},\"tail\":7}"),"Large history root replacement preserves following fields");
        Console.WriteLine("SAVE_SHAPE_HISTORY_PASS receipts=10000 temporaryBytes="+allocated);
    }
}
