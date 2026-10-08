using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private GrowthRequest rRecruitRequest;
        private string rRecruitError;
        private string[] PreviewFormation(string heroId=null)
        {
            var ids=CurrentFormation();for(int i=0;i<5;i++)if(ids[i]==null){string replacement=formalProgression.Snapshot.heroines.Select(h=>h.heroineId).First(id=>!ids.Where(x=>x!=null).Any(x=>combatDefinitions.PersonId(x)==combatDefinitions.PersonId(id)));ids[i]=replacement;}
            if(heroId!=null && !ids.Contains(heroId)){int same=Array.FindIndex(ids,id=>combatDefinitions.PersonId(id)==combatDefinitions.PersonId(heroId));ids[same<0?0:same]=heroId;}
            return ids;
        }
        private void DrawRRecruitment()
        {
            if(combatDefinitions.HeroineIds.Contains("heroine.annihilator")){DrawExpansionRecruitmentButton();return;}
            if(!combatDefinitions.HeroineIds.Contains("heroine.r") || formalProgression.Snapshot.heroines.Any(h=>h.heroineId=="heroine.r"))return;
            bool enabled=BookInputAllowed && homeRequest==null && !formalCampaign.HasPending && (!formalProgression.HasPending || rRecruitRequest!=null);
            if(GrowthButton(975,170,570,48,rRecruitError??"Rを迎える ／ アーティスト・無償",enabled)){
                try{
                    rRecruitRequest=rRecruitRequest??new GrowthRequest(Guid.NewGuid().ToString("N"),"heroine.r",formalProgression.Snapshot.revision,GrowthOperation.ReceiveHeroine);
                    var committed=formalProgression.Commit(rRecruitRequest,SaveFormalGrowth);
                    if(committed==GrowthCommitResult.SaveFailed){rRecruitError="加入の保存を再試行する";return;}
                    rRecruitRequest=null;rRecruitError=null;heroineRoster=null;status="Rが仲間になりました。編成から出撃枠へ追加できます。";PlayProductionUnlock();
                }catch(Exception e){rRecruitError=e.Message;}
            }
        }
    }
}
