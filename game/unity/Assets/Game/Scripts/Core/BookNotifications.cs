using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    [Flags] public enum BookNotice {None=0,Unread=1,Upgrade=2,Develop=4}
    public sealed class BookNotificationService
    {
        private long revision=-1;
        private readonly Dictionary<string,BookNotice> notices=new Dictionary<string,BookNotice>();
        public BookNotice For(string id)=>id!=null && notices.TryGetValue(id,out var value)?value:BookNotice.None;
        public int Count(IEnumerable<string> ids)=>ids.Distinct().Count(id=>For(id)!=BookNotice.None);
        private void Add(string id,BookNotice value){notices[id]=For(id)|value;}
        public void Refresh(FormalCampaignSave s,HomeExperienceCatalog home,CollectionCatalog collection)
        {
            if(revision==s.revision)return;notices.Clear();revision=s.revision;
            foreach(var h in s.growth.heroines){
                var a=s.affection?.states.SingleOrDefault(p=>p.personId==home.PersonId(h.heroineId));
                if(a!=null && AffectionEventResolver.ForPerson(s,home,h.heroineId).Any(e=>a.level>=e.requiredAffectionLevel && !a.readEventIds.Contains(e.id)))Add(h.heroineId,BookNotice.Unread);
                if(s.home!=null && home.weaponNodes.Any(n=>n.heroineId==h.heroineId && !n.initial && (s.home.weaponNodeIds.Contains(n.id)?s.home.WeaponLevel(n.id)<7 && WeaponGrowthRules.Costs(n,s.home.WeaponLevel(n.id),home).All(c=>HomeRules.Balance(s,c.resourceId)>=c.amount):n.parentIds.All(s.home.weaponNodeIds.Contains) && n.costs.All(c=>HomeRules.Balance(s,c.resourceId)>=c.amount))))Add(h.heroineId,BookNotice.Upgrade);
                if(h.level<h.LevelCap && s.growth.nectar>=FormalProgression.LevelCost(h.level,h.level+1))Add(h.heroineId,BookNotice.Upgrade);
            }
            foreach(var chapter in collection.chapters.Where(c=>c.textId!=null && s.world.unlockedStoryIds.Contains(c.id) && !s.world.readStoryIds.Contains(c.id)))Add(chapter.ownerId,BookNotice.Unread);
            foreach(var p in s.collection?.ooparts?.progress??Array.Empty<OopartProgress>()){
                var d=collection.Oopart(p.oopartId);int cost=FormalRelicRules.UpgradeCost(new CollectionRelic{level=p.level},RelicOperation.LevelUp,collection.contentVersion);
                if(p.level<120 && collection.relics.Single(r=>r.id==p.oopartId).materialIds.All(id=>HomeRules.Balance(s,id)>=cost))Add("items.main",BookNotice.Upgrade);
                if(s.growth.nectar>=d.directNectarCost && HomeRules.Balance(s,d.directMaterialId)>=d.directMaterialCost && d.randomStats.Any(r=>OopartService.DirectEligible(p,r)))Add("items.main",BookNotice.Upgrade);
            }
            var t=s.world.terraform;if(t!=null)foreach(var d in t.domains){int next=d.maxReachedLevel+1;if(next>7)continue;var depths=next<6?new string[]{null}:TerraformRules.DeepFor(t,d.domainId);var fusions=next<7?new string[]{null}:TerraformRules.Fusions[Array.IndexOf(TerraformRules.DomainIds,d.domainId)];if(depths.Any(deep=>fusions.Any(f=>TerraformRules.BlockReason(t,d.domainId,next,deep,f,true)==null)))Add("terraform.domain."+d.domainId,BookNotice.Develop);}
        }
    }
}
