using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void BackBookPage()
        {
            if(!BookInputAllowed || !FlushSaveChanges())return;
            if(book.IsTransitioning){book.CompleteTransition();return;}
            if(!book.GoBack()){ReturnBookToTitle();return;}
            bookTransitionElapsed=0;
            formationOpen=book.Bookmark==BookBookmark.Formation;
            if(formationOpen)formationLayer=0;
            heroineRosterOpen=book.Bookmark==BookBookmark.Heroines;
            growthScreen=book.Face==BookFace.Details?GrowthScreen.Information:GrowthScreen.Overview;
            if(book.Bookmark==BookBookmark.Heroines && weaponMaterialReturnHero==book.SubjectId){heroineRosterOpen=false;growthScreen=GrowthScreen.Weapons;weaponMaterialReturnHero=null;}
            else returnToFormationFromWeapon=false;
        }
        private void HandleEscapeNavigation()
        {
            if(plan7ActiveCombat)return;
            if(plan9EnemyPreview!=null)plan9EnemyPreview=null;
            else if(plan9Expression!=null)plan9Expression=null;
            else if(plan9Cg!=null)plan9Cg=null;
            else if(artSample){artSample=false;artBgm?.Stop();artSe?.Stop();}
            else if(adv!=null){if(advBacklog || advHelp){advBacklog=false;advHelp=false;}else CloseAdv();}
            else if(saveManagementOpen)BackSaveManagement();
            else if(modelViewer)modelViewer=false;
            else if(help)help=false;
            else if(bookSystemOpen && titlePanel!=null)CloseTitlePanel();
            else if(bookSystemOpen)bookSystemOpen=false;
            else if(expansionRecruitmentOpen)CloseExpansionRecruitment();
            else if(exchangeMaterial!=null){exchangeMaterial=null;exchangeError=null;}
            else if(CloseAffectionLayer()){}
            else if(CloseOoparts()){}
            else if(terraformRequest!=null){if(!formalCampaign.HasPending){terraformRequest=null;terraformWarning=false;}}
            else if(homeRequest!=null){if(!formalCampaign.HasPending){homeRequest=null;homeOperation=null;}}
            else if(panzerSetupOpen)panzerSetupOpen=false;
            else if(placing){placing=false;selectedFurniture=null;}
            else if(CloseGardenLifeLayer()){}
            else if(CloseGardenMenuLayer()){}
            else if(recoveryActive){if(recoveryConfirm)recoveryConfirm=false;else{recoveryActive=false;title=true;}}
            else if(relicRequest!=null || collectionOpen)CollectionBack();
            else if(engagementOpen)EngagementBack();
            else if(book.IsTransitioning && encounter==null && !title)book.CompleteTransition();
            else if(!title && encounter==null && book.Bookmark==BookBookmark.Formation)BackFormationLayer();
            else if(!title && encounter==null && (kinderGarden || book.Bookmark==BookBookmark.Summoning) && formalProgression!=null) KinderBack();
            else if(!title && encounter==null && book.Bookmark==BookBookmark.Heroines && formalProgression!=null) GrowthBack();
            else if(selectingAlly) { selectingAlly=false; selectedAllies.Clear(); }
            else if(storyText!=null) CloseStory();
            else if(kinderGarden) kinderGarden=false;
            else if(retreat) { retreat=false; paused=false; }
            else if(CloseBattleMenuLayer()){}
            else if(result!=null && formalBattleEndRequest==null && !formalCampaign.HasPending){result=null;encounter=null;}
            else if(title && titlePanel!=null)CloseTitlePanel();
            else if(title && FormalEntranceVisible)formalEntranceComplete=true;
            else if(title)OpenTitlePanel("exit");
            else if(encounter!=null && result==null) paused=!paused;
            else if(result==null)BackBookPage();
        }
    }
}
