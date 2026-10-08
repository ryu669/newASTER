using System;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private float kinderTrainCapture=-1;
        private void DrawKinderTrain()
        {
            float elapsed=kinderTrainCapture>=0?kinderTrainCapture:Time.unscaledTime-kinderRevealStarted;
            if(kinderTrainCapture<0){if(ArtSampleSettings.ReducedMotion)elapsed+=4.5f;else if(ArtSampleSettings.Shortened)elapsed*=3;}
            var old=GUI.color;GUI.BeginGroup(new Rect(605,300,885,407));
            GrowthFill(0,0,885,407,new Color(.055f,.09f,.16f));
            string frame=elapsed<1.6f?"arrival":elapsed<2.4f?"doors":"disembark";
            var illustration=Resources.Load<Texture2D>("KinderTrain/"+frame);
            if(illustration==null)throw new InvalidOperationException("Train illustration missing: "+frame);GUI.color=Color.white;
            if(illustration!=null)GUI.DrawTexture(new Rect(0,0,885,407),illustration,ScaleMode.ScaleAndCrop,true);
            float transition=frame=="doors"?Mathf.Clamp01((elapsed-1.6f)/.35f):frame=="disembark"?Mathf.Clamp01((elapsed-2.4f)/.35f):1;
            if(transition<1){var prior=Resources.Load<Texture2D>("KinderTrain/"+(frame=="doors"?"arrival":"doors"));GUI.color=new Color(1,1,1,1-transition);if(prior!=null)GUI.DrawTexture(new Rect(0,0,885,407),prior,ScaleMode.ScaleAndCrop,true);GUI.color=old;}
            GrowthFill(0,0,885,47,new Color(.025f,.045f,.08f,.8f));
            Label(15,6,850,39,elapsed<1.6f?"星の夜行列車が、ホームへ到着します。":elapsed<2.4f?"扉の向こうに、新しい出会い。":"ようこそ、記憶の庭へ。",growthSmallStyle,gold);
            if(elapsed>=2.4f){
                int count=kinderReceipt.kinderOutcomes.Length;
                for(int i=0;i<count;i++){
                    float step=Mathf.Clamp01((elapsed-2.4f-i*.12f)/.75f);if(step<=0)continue;
                    var r=kinderReceipt.kinderOutcomes[i];float destination=8+i%5*173,dy=302+i/5*49;
                    float cx=Mathf.Lerp(390,destination,step),cy=Mathf.Lerp(186,dy,step);
                    GUI.color=new Color(1,1,1,step);GrowthFill(cx,cy,170,43,navy);
                    GrowthLine(cx,cy,cx+170,cy,gold);GrowthLine(cx,cy+43,cx+170,cy+43,gold);
                    GrowthDiamond(cx,cy,4);GrowthDiamond(cx+170,cy+43,4);
                    if(r.kind=="heroine"){
                        DrawHeroPortrait(new Rect(cx+3,cy+3,37,37),r.heroineId);
                        Label(cx+44,cy+5,123,36,combatDefinitions.Hero(r.heroineId).name,new GUIStyle(growthSmallStyle){fontSize=13},gold);
                    }else{
                        DrawSanctuaryIcon(new Rect(cx+7,cy+7,28,28),"star",ivory);
                        Label(cx+42,cy+5,115,35,r.kind=="nectar"?"ネクタル":"覚醒結晶",new GUIStyle(growthSmallStyle){fontSize=15});
                    }
                    GUI.color=old;
                }
            }
            GUI.EndGroup();GUI.color=old;
            if(GrowthButton(605,727,885,62,"演出をスキップ ／ 結果を見る") || kinderTrainCapture<0 && elapsed>=5.2f)kinderScreen=KinderScreen.Result;
        }
    }
}
