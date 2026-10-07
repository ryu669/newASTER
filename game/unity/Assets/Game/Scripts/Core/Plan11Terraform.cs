using System;
using System.Linq;


namespace NewAster.Core
{
    [Serializable] public sealed class TerraformMigration { public bool plan11TerraformCompleted; }
    [Serializable] public sealed class TerraformDomainState
    {
        public string domainId; public int currentLevel,maxReachedLevel;
        public string activeDeepRecordId,activeExtremeId;
    }
    [Serializable] public sealed class TerraformRecord { public string colossusId; public int tier,highestLevel; }
    [Serializable] public sealed class TerraformDiscovery { public string extremeId,deepRecordId; public int sequence; }
    [Serializable] public sealed class TerraformSave
    {
        public int totalTp;
        public TerraformDomainState[] domains=TerraformRules.DomainIds.Select(id=>new TerraformDomainState {domainId=id}).ToArray();
        public TerraformRecord[] acquiredWorldRecords=Array.Empty<TerraformRecord>();
        public string[] acquiredDeepRecords=Array.Empty<string>(),usedDeepRecords=Array.Empty<string>(),unlockedWorldPhenomena=Array.Empty<string>();
        public TerraformDiscovery[] discoveredExtremes=Array.Empty<TerraformDiscovery>();
        public bool worldIntegrated,sevenExtremeGenesis,overgrowthWarningAccepted;
        public string customWorldName;
        public string activeWorldPhenomenonId;
    }
    public static class TerraformRules
    {
        public static readonly string[] ColossusIds={"colossus.green-return-dragon","colossus.red-crystal-tyrant","colossus.memory-crystal-dragon","colossus.sky-tower-machine","colossus.crystal-rose-princess","colossus.silver-sea-whale","colossus.heaven-tree-orochi","colossus.reenactment-yimir","colossus.emerald-star-astal","colossus.amber-king-serpent","colossus.white-divine-dragon-mother","colossus.black-smoke-citadel","colossus.dead-king-megadeath","colossus.final-flame-ice-phoenix","colossus.newborn-asteria"};
        public static readonly int[] Worlds={1,1,2,2,3,3,3,4,4,5,5,6,6,7,0};
        public static readonly string[] DomainIds={"life","water","earth","sky","light","night","civilization"};
        public static readonly string[] DomainNames={"生命圏","水圏","地圏","天圏","光圏","夜圏","文明圏"};
        public static readonly string[] LevelNames={"未形成","萌芽","形成","定着","繁栄","調和","過剰再生","極相"};
        public static readonly int[] Costs={0,100,300,600,1200,2000,4000,8000};
        public static readonly string[] DeepIds={"endless_genesis","informational_life","arcane_nature","eternal_reenactment","immutable_eternity","self_rewriting_world","frozen_cycle"};
        public static readonly string[] DeepNames={"無窮生誕","情報生命","魔生自然","永劫再演","不変永世","自己改界","凍結輪廻"};
        public static readonly string[][] ColossusDomains={new[]{"life","earth"},new[]{"earth","life"},new[]{"earth","civilization"},new[]{"sky","civilization"},new[]{"life","light"},new[]{"water","sky"},new[]{"life","water","earth"},new[]{"sky","life","light"},new[]{"night","sky","light"},new[]{"light","earth","water"},new[]{"life","light"},new[]{"earth","civilization","water"},new[]{"night","life","water"},new[]{"water","sky","earth"},DomainIds};
        public static readonly string[][] Fusions={new[]{"night","light","water","civilization"},new[]{"life","night","sky","civilization"},new[]{"life","light","sky","civilization"},new[]{"water","earth","night","civilization"},new[]{"life","earth","night","civilization"},new[]{"life","water","sky","civilization"},new[]{"life","water","sky","night"}};
        public static readonly string[][] ExtremeNames={new[]{"星幽樹海","光晶花園","原生湿林","機樹都市"},new[]{"翠海世界","星海深淵","天海界","蒼海機都"},new[]{"巨生大陸","光晶火山界","浮岳世界","地底機都"},new[]{"天瀑世界","浮遊大陸","星空浮遊界","天空機都"},new[]{"陽華楽園","晶光世界","永久黄昏界","光子都市"},new[]{"幽森星界","月海世界","星天界","永夜電脳都"},new[]{"生体機都","海洋機都","軌道天空都市","電脳夜都"}};
        public static readonly string[] Phenomena={"rain","snow","petals","fireflies","meteors","aurora","thunderclouds","sea_fireworks","crystal_rain","neon_festival"};
        public static readonly string[] PhenomenonNames={"雨","雪","花吹雪","蛍","流星群","オーロラ","雷雲","海上花火","光晶雨","ネオン祭"};
        public static readonly int[] PhenomenonCosts={1000,1000,1500,1500,2000,2000,2000,2500,3000,3000};
        public static TerraformSave Migrate(CampaignSaveV2 save)
        {
            if(save.migration==null)save.migration=new TerraformMigration();
            if(save.terraform==null)save.terraform=new TerraformSave();
            foreach(string id in DomainIds)if(!save.terraform.domains.Any(d=>d.domainId==id))save.terraform.domains=save.terraform.domains.Concat(new[]{new TerraformDomainState {domainId=id}}).ToArray();
            if(!save.migration.plan11TerraformCompleted){save.terraform.totalTp=checked(save.terraform.totalTp+save.terraformingExperience);save.migration.plan11TerraformCompleted=true;}
            return save.terraform;
        }
        public static void Validate(TerraformSave s)
        {
            if(s==null)return;
            if(s.totalTp<0 || s.domains==null || s.domains.Any(d=>d==null || string.IsNullOrWhiteSpace(d.domainId) || d.currentLevel<0 || d.currentLevel>7 || d.maxReachedLevel<d.currentLevel || d.maxReachedLevel>7) || s.domains.Select(d=>d.domainId).Distinct().Count()!=s.domains.Length)throw new ArgumentException("Invalid terraform domains.");
            if(s.acquiredWorldRecords==null || s.acquiredWorldRecords.Any(r=>r==null || string.IsNullOrWhiteSpace(r.colossusId) || r.tier<1 || r.tier>5 || r.highestLevel<1 || r.highestLevel>50) || s.acquiredWorldRecords.Select(r=>r.colossusId).Distinct().Count()!=s.acquiredWorldRecords.Length)throw new ArgumentException("Invalid terraform records.");
            foreach(var ids in new[]{s.acquiredDeepRecords,s.usedDeepRecords,s.unlockedWorldPhenomena})if(ids==null || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count()!=ids.Length)throw new ArgumentException("Invalid terraform ID set.");
            if(s.discoveredExtremes==null || s.discoveredExtremes.Any(e=>e==null || string.IsNullOrWhiteSpace(e.extremeId) || e.sequence<1) || s.discoveredExtremes.Select(e=>e.extremeId).Distinct().Count()!=s.discoveredExtremes.Length || s.discoveredExtremes.Select(e=>e.sequence).Distinct().Count()!=s.discoveredExtremes.Length)throw new ArgumentException("Invalid terraform discoveries.");
        }
        public static int Index(string id)=>ColossusIds.ToList().IndexOf(id);
        public static int BaseTp(int level)=>level==50?600:level>=40?400:level>=30?250:level>=20?150:level>=10?100:50;
        public static int Victory(TerraformSave s,string id,int level)
        {
            int index=Index(id);if(index<0 || level<1 || level>50)throw new ArgumentException("Unknown victory.");
            var r=s.acquiredWorldRecords.SingleOrDefault(x=>x.colossusId==id);
            if(r==null){r=new TerraformRecord {colossusId=id};s.acquiredWorldRecords=s.acquiredWorldRecords.Concat(new[]{r}).ToArray();}
            int[] thresholds={1,10,20,30,40,50},bonuses={100,200,300,500,700,1000};
            int tp=BaseTp(level)+thresholds.Select((t,i)=>r.highestLevel<t && level>=t?bonuses[i]:0).Sum();
            r.highestLevel=Math.Max(r.highestLevel,level);r.tier=Math.Max(r.tier,Math.Min(5,level/10+1));s.totalTp=checked(s.totalTp+tp);
            if(index==14)s.worldIntegrated=true;return tp;
        }
        public static bool HasRecord(TerraformSave s,string domain,int tier)=>s.acquiredWorldRecords.Any(r=>Index(r.colossusId)>=0 && Index(r.colossusId)<14 && r.tier>=tier && ColossusDomains[Index(r.colossusId)].Contains(domain));
        public static string[] MissingIntegration(TerraformSave s)=>DomainIds.Where(id=>s.domains.Single(d=>d.domainId==id).currentLevel<3).ToArray();
        public static string[] DeepFor(TerraformSave s,string domain)=>s.acquiredDeepRecords.Where(id=>{int i=Array.IndexOf(DeepIds,id);return i>=0 && ColossusIds.Any(c=>Worlds[Index(c)]==i+1 && ColossusDomains[Index(c)].Contains(domain));}).ToArray();
        public static void RefreshDeep(TerraformSave s,string[] readIds,CollectionCatalog catalog)
        {
            for(int i=0;i<7;i++)if(ColossusIds.Where(c=>Worlds[Index(c)]==i+1).All(c=>s.acquiredWorldRecords.Any(r=>r.colossusId==c && r.highestLevel==50) && catalog.owners.Single(o=>o.id==c).chapterIds.All(readIds.Contains)))
                s.acquiredDeepRecords=s.acquiredDeepRecords.Concat(new[]{DeepIds[i]}).Distinct().ToArray();
        }
        public static string BlockReason(TerraformSave s,string domain,int level,string deep=null,string fusion=null,bool confirm=false)
        {
            var d=s.domains.Single(x=>x.domainId==domain);
            if(level<1 || level>7 || level>d.maxReachedLevel+1)return "順番に領域を発展させてください。";
            if(level<=5 && level>d.maxReachedLevel){if(!HasRecord(s,domain,level))return "対応する記述Tier "+level+" が必要です。";if(level>=4 && s.acquiredWorldRecords.Count(r=>Index(r.colossusId)>=0 && Index(r.colossusId)<14 && ColossusDomains[Index(r.colossusId)].Contains(domain))<2)return "異なる2体以上の記述が必要です。";}
            if(level>=6){if(!s.worldIntegrated)return "アステリア初討伐が必要です。";if(!DeepFor(s,domain).Contains(deep))return "使用可能な深層記述を選択してください。";if(level==6 && !confirm)return "過剰再生の警告確認が必要です。";}
            if(level==7){if(!s.acquiredWorldRecords.Any(r=>Index(r.colossusId)==14 && r.highestLevel==50))return "アステリアLv50討伐が必要です。";int i=Array.IndexOf(DomainIds,domain);if(!Fusions[i].Contains(fusion) || !HasRecord(s,fusion,1))return "融合先の領域記述が必要です。";}
            return s.totalTp<Cost(s,d,level,fusion)?"TPが不足しています。":null;
        }
        public static int Cost(TerraformSave s,TerraformDomainState d,int level,string fusion)=>level>d.maxReachedLevel?Costs[level]:level==7 && !s.discoveredExtremes.Any(e=>e.extremeId==d.domainId+"_"+fusion)?1000:0;
        public static void SetLevel(TerraformSave s,string domain,int level,string deep=null,string fusion=null,bool confirm=false)
        {
            string reason=BlockReason(s,domain,level,deep,fusion,confirm);if(reason!=null)throw new InvalidOperationException(reason);
            var d=s.domains.Single(x=>x.domainId==domain);s.totalTp-=Cost(s,d,level,fusion);d.currentLevel=level;d.maxReachedLevel=Math.Max(d.maxReachedLevel,level);
            d.activeDeepRecordId=level>=6?deep:null;d.activeExtremeId=level==7?domain+"_"+fusion:null;
            if(level>=6){s.usedDeepRecords=s.usedDeepRecords.Concat(new[]{deep}).Distinct().ToArray();s.overgrowthWarningAccepted=true;}
            if(level==7 && !s.discoveredExtremes.Any(e=>e.extremeId==d.activeExtremeId))s.discoveredExtremes=s.discoveredExtremes.Concat(new[]{new TerraformDiscovery {extremeId=d.activeExtremeId,deepRecordId=deep,sequence=s.discoveredExtremes.Select(e=>e.sequence).DefaultIfEmpty(0).Max()+1}}).ToArray();
            s.sevenExtremeGenesis=DomainIds.All(id=>s.domains.Single(x=>x.domainId==id).maxReachedLevel==7);
        }
        public static void UnlockPhenomenon(TerraformSave s,string id)
        {int i=Array.IndexOf(Phenomena,id);if(i<0 || !s.sevenExtremeGenesis || s.unlockedWorldPhenomena.Contains(id) || s.totalTp<PhenomenonCosts[i])throw new InvalidOperationException("世界現象の解放条件が不足しています。");s.totalTp-=PhenomenonCosts[i];s.unlockedWorldPhenomena=s.unlockedWorldPhenomena.Concat(new[]{id}).ToArray();}
        public static string[] EnvironmentTags(TerraformSave s)=>s.domains.Select(d=>d.domainId+".level."+d.currentLevel).Concat(s.domains.Where(d=>d.activeDeepRecordId!=null).Select(d=>d.activeDeepRecordId)).ToArray();
        public static string[] ExtremeTags(TerraformSave s)=>s.domains.Where(d=>d.activeExtremeId!=null).Select(d=>d.activeExtremeId).ToArray();
    }
}
