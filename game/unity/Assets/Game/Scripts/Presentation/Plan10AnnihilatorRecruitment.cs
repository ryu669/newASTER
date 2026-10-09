using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool expansionRecruitmentOpen;
        private int expansionRecruitmentPage;
        private GrowthRequest expansionRecruitRequest;
        private string expansionRecruitError;
        private string[] UnownedExpansionForms()=>combatDefinitions.HeroineIds.Skip(5).Where(id=>combatDefinitions.HeroineIds.Contains(id) && !formalProgression.Snapshot.heroines.Any(h=>h.heroineId==id)).ToArray();
        private void DrawExpansionRecruitmentButton()
        {
            int count=UnownedExpansionForms().Length;if(count==0)return;
            if(GrowthButton(80,118,430,48,"デバッグ：新しい天使を迎える",BookInputAllowed && homeRequest==null && !formalCampaign.HasPending))expansionRecruitmentOpen=true;
        }
        private void CloseExpansionRecruitment()
        {
            // Keep the visible dialog and its retry request while a save is unresolved.
            if(expansionRecruitRequest!=null)return;
            expansionRecruitmentOpen=false;
            expansionRecruitError=null;
        }
        private GrowthCommitResult RecruitExpansionForm(string id)
        {
            if(!combatDefinitions.HeroineIds.Skip(5).Contains(id) || !combatDefinitions.HeroineIds.Contains(id))throw new ArgumentException("Unknown recruitable form.");
            if(formalProgression.Snapshot.heroines.Any(h=>h.heroineId==id))return GrowthCommitResult.AlreadyCommitted;
            if(expansionRecruitRequest!=null && expansionRecruitRequest.HeroineId!=id)throw new InvalidOperationException("加入の保存を再試行してください。");
            expansionRecruitRequest=expansionRecruitRequest??new GrowthRequest(Guid.NewGuid().ToString("N"),id,formalProgression.Snapshot.revision,GrowthOperation.ReceiveHeroine);
            var result=formalProgression.Commit(expansionRecruitRequest,SaveFormalGrowth);
            if(result==GrowthCommitResult.SaveFailed){expansionRecruitError="保存できません。同じ加入を再試行してください。";return result;}
            expansionRecruitRequest=null;expansionRecruitError=null;heroineRoster=null;status=combatDefinitions.Hero(id).name+"を迎えました。編成から出撃枠を選んでください。";PlayProductionUnlock();return result;
        }
        private void DrawAnnihilatorRecruitmentDialog()
        {
            if(!expansionRecruitmentOpen)return;drawingModal=true;
            GrowthFill(0,0,1600,900,new Color(0,0,0,.75f));GrowthFrame(285,65,1030,780);
            Label(330,95,940,48,"デバッグ：新しい天使を迎える",growthTitleStyle,gold);

            var allForms=UnownedExpansionForms();int pages=Math.Max(1,(allForms.Length+4)/5);expansionRecruitmentPage=Mathf.Clamp(expansionRecruitmentPage,0,pages-1);var forms=allForms.Skip(expansionRecruitmentPage*5).Take(5).ToArray();
            for(int i=0;i<forms.Length;i++){
                string id=forms[i];var hero=combatDefinitions.Hero(id);
                if(GrowthButton(330,229+i*70,940,58,hero.name+" ／ "+HeroineIdentityCatalog.JobName(hero.jobId)+"を迎える",expansionRecruitRequest==null || expansionRecruitRequest.HeroineId==id,true)){
                    try{RecruitExpansionForm(id);}catch(Exception e){expansionRecruitError=e.Message;}
                }
            }
            if(GrowthButton(330,610,260,48,"‹ 前の5形態",expansionRecruitmentPage>0 && expansionRecruitRequest==null))expansionRecruitmentPage--;
            Label(660,619,260,40,(expansionRecruitmentPage+1)+" / "+pages,growthSmallStyle);
            if(GrowthButton(1010,610,260,48,"次の5形態 ›",expansionRecruitmentPage+1<pages && expansionRecruitRequest==null))expansionRecruitmentPage++;
            if(expansionRecruitError!=null)Label(330,665,940,40,expansionRecruitError,growthSmallStyle);
            if(GrowthButton(970,765,300,48,book.Bookmark==BookBookmark.Summoning?"召喚へ戻る":"一覧へ戻る",expansionRecruitRequest==null,true))CloseExpansionRecruitment();
        }
    }
}
