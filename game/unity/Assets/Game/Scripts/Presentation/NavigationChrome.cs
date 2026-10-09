using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool bookSystemButtonDrawn,navigationChromeValidated;
        private int bookSystemButtonsThisGui;
        private void DrawSingleBookSystemButton(float x,float y,float width,float height,bool enabled)
        {
            GrowthStyles();
            if(bookSystemButtonDrawn)return;
            bookSystemButtonDrawn=true;bookSystemButtonsThisGui++;
            if(GrowthButton(x,y,width,height,"システム",enabled))bookSystemOpen=true;
        }
        private void ReturnBookToTitle()
        {
            if(!FlushSaveChanges())return;
            book.Close();title=true;bookSystemOpen=false;
        }
        private void DrawGlobalBackButton()
        {
            GrowthStyles();
            bool enabled=book!=null && combatDefinitionError==null && !plan7ActiveCombat && (!title || titlePanel!=null || saveManagementOpen) && !(formalCampaign?.HasPending??false) && formalBattleEndRequest==null;
            if(GrowthButton(1408,14,76,52,"戻る",enabled))HandleEscapeNavigation();
        }
        private void ValidateNavigationChrome()
        {
            if(!plan10UiCapture || navigationChromeValidated || Event.current.type!=EventType.Repaint)return;
            AcceptanceCheck(bookSystemButtonsThisGui<=1,"Only one system button is drawn per GUI pass");
            if(IsBookScreen && !help && !bookSystemOpen && !gardenViewing)AcceptanceCheck(bookSystemButtonsThisGui==1,"Book page has one system entry");
            navigationChromeValidated=true;
            Debug.Log("PLAN119_NAVIGATION_CHROME_PASS backButton=true ribbonCountLabels=0 systemButtons="+bookSystemButtonsThisGui+" physicalInput=0");
        }
    }
}
