using System.Linq;
using NewAster.Data;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        // Legacy 3D preview and model review. Ordinary gameplay uses illustration views.
        private Camera viewCamera;
        private VerticalSliceBlockout stage;
        private bool slayerReview;
        private bool modelViewer, portraitFace=true;
        private float portraitYaw=-20, portraitZoom=1;
        private static readonly string[] ModelExpressions={"Neutral","Smile","Joy","Sad","Angry","Surprise","Talk"};
        private static readonly string[] ModelExpressionLabels={"通常","微笑み","喜び","悲しみ","怒り","驚き","口の動き"};

        private void UpdateSceneCamera(bool battleView,bool formalHeroView,bool gardenView)
        {
            viewCamera.cullingMask=modelViewer?~0:recoveryActive || engagementOpen || battleView || formalHeroView?0:~0;
            viewCamera.orthographic=modelViewer;
            viewCamera.backgroundColor=modelViewer?new Color(.42f,.44f,.48f):new Color(.045f,.10f,.11f);
            viewCamera.rect=LogicalCameraRect(battleView?new Rect(0f,.22f,.72f,.60f):new Rect(.64f,.27f,.36f,.51f));
            viewCamera.aspect=Screen.width*viewCamera.rect.width/(Screen.height*viewCamera.rect.height);
            viewCamera.fieldOfView=battleView?35f:60f;
            viewCamera.transform.position=gardenView?new Vector3(-4,5,-8):battleView?new Vector3(-.5f,4.5f,-10):new Vector3(-1,7,-15);
            viewCamera.transform.LookAt(gardenView?new Vector3(-3,1,3):new Vector3(-.5f,battleView?2.8f:1.8f,1.2f));
            if(modelViewer) {
                var canvas=NewAster.Core.AspectLayout.Contain(0,0,Screen.width,Screen.height,1600,900);
                var mouse=new Vector2((Input.mousePosition.x-canvas.X)/canvas.Scale,(Screen.height-Input.mousePosition.y-canvas.Y)/canvas.Scale);
                bool overPortrait=new Rect(0,125,1600,680).Contains(mouse);
                if(Input.GetMouseButton(0) && overPortrait)portraitYaw+=Input.GetAxis("Mouse X")*4;
                if(overPortrait)portraitZoom=Mathf.Clamp(portraitZoom-Input.mouseScrollDelta.y*.07f,.65f,1.5f);
                viewCamera.rect=LogicalCameraRect(new Rect(0,0,1,1));viewCamera.aspect=1600f/900f;
                var focus=new Vector3(-4.8f,portraitFace?1.49f:1.01f,-1.5f);
                float angle=portraitYaw*Mathf.Deg2Rad;
                viewCamera.transform.position=focus+new Vector3(Mathf.Cos(angle)*3,.025f,Mathf.Sin(angle)*3);
                viewCamera.transform.LookAt(focus); viewCamera.orthographicSize=(portraitFace?.19f:.73f)*portraitZoom;
            }
        }

        private void SynchronizeSceneStage(bool battleView,bool formalHeroView,bool gardenView)
        {
            if(stage!=null) stage.Synchronize(gardenView,campaign.Gardens.UnlockedGardenIds.Count>0,campaign.Playable,encounter,target,paused || retreat || help || result!=null,playback.Current);
            if(stage!=null) stage.SetPortraitView(modelViewer);
            bool bookPreviewVisible=title || modelViewer || encounter!=null && book.HasSubject && (book.Bookmark==BookBookmark.Colossi && book.SubjectId==WorldCatalog.ColossusIds[0] || book.Bookmark==BookBookmark.Gardens && book.SubjectId=="garden.grassland-forest" && campaign.Gardens.UnlockedGardenIds.Contains(book.SubjectId));
            if(!bookPreviewVisible)viewCamera.cullingMask=0;
            if(!modelViewer && !title && encounter==null && book.Bookmark==BookBookmark.Gardens)viewCamera.cullingMask=0;
            if(stage!=null) stage.gameObject.SetActive(!recoveryActive && !battleView && !formalHeroView && bookPreviewVisible);
        }

        private void DrawModelViewer()
        {
            Label(32,24,700,45,"スレイヤー  /  人物鑑賞",heading,Color.white);
            for(int i=0;i<ModelExpressions.Length;i++) if(Btn(32+i*117,82,108,36,ModelExpressionLabels[i]) && stage!=null) stage.SetSlayerExpression(ModelExpressions[i]);
            Panel(20,815,1560,66,dark);
            if(Btn(32,827,145,42,"全身")) { portraitFace=false; portraitZoom=1; }
            if(Btn(187,827,145,42,"顔")) { portraitFace=true; portraitZoom=1; }
            if(Btn(342,827,145,42,"正面")) portraitYaw=0;
            if(Btn(497,827,145,42,"斜め")) portraitYaw=-25;
            if(Btn(652,827,145,42,"横顔")) portraitYaw=90;
            if(Btn(807,827,210,42,"衣装を替える") && stage!=null) stage.SetSlayerOutfit(stage.SlayerOutfitId=="rose"?"training":"rose");
            Label(1040,835,500,32,"ドラッグで回転・ホイールで拡大",small,Color.white);
        }
    }
}
