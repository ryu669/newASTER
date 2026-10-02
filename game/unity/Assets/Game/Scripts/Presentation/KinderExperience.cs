using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private enum KinderScreen { Entrance, Draw, Exchange, Tickets, Rates, Confirmation, Result, Revealing }
        private KinderScreen kinderScreen,kinderOrigin;
        private KinderRequest kinderRequest;
        private FormalKinderBanner kinderBanner;
        private GrowthReceipt kinderReceipt;
        private string kinderSaveError;
        private int kinderSelection,kinderCount=1;
        private float kinderRevealStarted;
        private void InitializeKinder()
        {
            var source=Resources.Load<TextAsset>("Economy/kinder-trial");
            if(source==null)throw new ArgumentException("Kinder rules missing.");
            kinderBanner=JsonUtility.FromJson<FormalKinderBanner>(source.text);kinderBanner.Validate(combatDefinitions.FormationIds);
        }
        private void KinderBack()
        {
            if(formalProgression.HasPending)return;
            if(kinderScreen==KinderScreen.Revealing){kinderScreen=KinderScreen.Result;return;}
            if(kinderScreen==KinderScreen.Entrance){kinderGarden=false;return;}
            if(kinderScreen==KinderScreen.Confirmation){kinderRequest=null;kinderScreen=kinderOrigin;return;}
            kinderScreen=KinderScreen.Entrance;kinderRequest=null;kinderReceipt=null;
        }
        private void ConfirmKinder(KinderOperation operation,FormalGrowthSave state,string heroineId=null)
        {
            kinderRequest=new KinderRequest(operation==KinderOperation.IntroGrant?"grant.plan4-kinder-introduction":Guid.NewGuid().ToString("N"),state.revision,operation,operation==KinderOperation.StoneDraw?kinderCount:1,heroineId);
            formalProgression.PreviewKinder(kinderRequest,kinderBanner);kinderOrigin=kinderScreen;kinderScreen=KinderScreen.Confirmation;kinderSaveError=null;
        }
        private void DrawKinderExperience()
        {
            GrowthStyles();var state=formalProgression.Snapshot;
            GrowthFill(0,0,1600,900,ink);GrowthFill(30,103,1540,732,new Color(.16f,.14f,.12f));GrowthFill(38,95,1524,732,new Color(.27f,.23f,.18f));GrowthFrame(46,87,1508,732);
            GrowthLine(555,105,555,800,gold,2);GrowthLine(90,56,640,56,gold);GrowthLine(960,56,1510,56,gold);GrowthDiamond(800,56,17);
            Label(680,25,250,40,"万 物 の 書",growthTextStyle,gold);
            if(GrowthButton(80,118,180,48,"‹ 戻る",!formalProgression.HasPending))KinderBack();
            Label(92,214,420,72,"キンダーガーデン",growthTitleStyle);
            GrowthDiamond(300,455,100);GrowthDiamond(300,455,70);GrowthDiamond(300,455,28);
            Label(160,593,345,74,"新しい誓いが\nここから芽吹く。",growthTextStyle);
            Label(92,737,420,40,"育成素材のみの検証用テーブル",growthSmallStyle);
            string[] titles={"誓いの入口","石で誓女を迎える","ポイント交換","専用チケット","提供割合","選択の確認","新しい誓い","誓いが芽吹く"};
            Label(605,204,880,55,titles[(int)kinderScreen],growthTitleStyle);GrowthLine(605,270,1498,270,gold);
            if(kinderScreen==KinderScreen.Entrance){
                if(GrowthButton(605,315,885,70,"石を使って誓女を迎える"))kinderScreen=KinderScreen.Draw;
                if(GrowthButton(605,410,885,70,"100ポイントを専用チケットに交換"))kinderScreen=KinderScreen.Exchange;
                if(GrowthButton(605,505,885,70,"所持チケットを使う"))kinderScreen=KinderScreen.Tickets;
                if(GrowthButton(605,600,885,55,"提供割合・交換ルールを見る"))kinderScreen=KinderScreen.Rates;
                if(GrowthButton(605,700,885,62,"検証用・初回3000石を受け取る",formalProgression.KinderReceipt("grant.plan4-kinder-introduction")==null,true))ConfirmKinder(KinderOperation.IntroGrant,state);
            }else if(kinderScreen==KinderScreen.Draw){
                Label(605,317,880,48,$"所持石  {state.stones}",growthTitleStyle);
                if(GrowthButton(605,411,427,68,(kinderCount==1?"◆ ":"")+"1回 ／ 300石"))kinderCount=1;
                if(GrowthButton(1062,411,427,68,(kinderCount==10?"◆ ":"")+"10回 ／ 3000石"))kinderCount=10;
                Label(605,535,880,100,$"付与ポイント  {kinderCount}\n★6合計3% ／ 対象5人は均等 ／ 10回に確定枠はありません。",growthTextStyle);
                Label(605,657,880,38,state.stones>=300*kinderCount?"確認するだけでは抽選・消費しません。":"石が不足しています。",growthSmallStyle);
                if(GrowthButton(605,709,885,62,"費用を確認する",state.stones>=300*kinderCount,true))ConfirmKinder(KinderOperation.StoneDraw,state);
            }else if(kinderScreen==KinderScreen.Exchange || kinderScreen==KinderScreen.Tickets){
                bool exchange=kinderScreen==KinderScreen.Exchange;
                Label(605,305,880,50,exchange?$"共通ポイント  {state.kinderPoints} ／ 交換費用100":"使う専用チケットを選んでください",growthTextStyle);
                for(int i=0;i<5;i++){
                    string id=kinderBanner.heroineIds[i];int count=state.tickets.SingleOrDefault(t=>t.heroineId==id)?.count??0;
                    string name=combatDefinitions.Hero(id).name;
                    if(GrowthButton(605,373+i*60,885,48,(kinderSelection==i?"◆ ":"")+name+(exchange?"":" ／ 所持 "+count)))kinderSelection=i;
                }
                string selected=kinderBanner.heroineIds[kinderSelection];int owned=state.tickets.SingleOrDefault(t=>t.heroineId==selected)?.count??0;
                Label(605,675,885,30,exchange?"交換ではチケットのみを付与。人物はまだ付与しません。":"対象人物100%。石消費・乱数・ポイント付与はありません。",growthSmallStyle);
                if(GrowthButton(605,727,885,62,exchange?"交換内容を確認する":"チケット使用を確認する",exchange?state.kinderPoints>=100:owned>0,true))ConfirmKinder(exchange?KinderOperation.Exchange:KinderOperation.TicketDraw,state,selected);
            }else if(kinderScreen==KinderScreen.Rates){
                Label(605,310,885,84,"★6合計 3% ／ 1人あたり 3÷5 = 0.6%\nカテゴリー当選後に対象5人を均等抽選。",growthTextStyle);
                int total=kinderBanner.materials.Sum(m=>m.weight);
                for(int i=0;i<kinderBanner.materials.Length;i++){var m=kinderBanner.materials[i];Label(605,430+i*68,885,52,$"{(m.kind=="nectar"?"ネクタル":"覚醒結晶")} {m.amount}個    {(97m*m.weight/total):0.##}%",growthTextStyle);}
                Label(605,615,885,120,"共通ポイント・チケットは失効なし。\n交換対象は現在の5人。チケット使用ではポイントを付与しません。\nオーパーツは未実装のため、この検証用ガチャからは排出しません。",growthSmallStyle);
            }else if(kinderScreen==KinderScreen.Confirmation){
                string name=kinderRequest.HeroineId==null?"":combatDefinitions.Hero(kinderRequest.HeroineId).name;
                string description=kinderRequest.Operation==KinderOperation.StoneDraw?$"{kinderRequest.Count}回の抽選\n消費石  {300*kinderRequest.Count} ／ 所持 {state.stones}\n付与ポイント  {kinderRequest.Count}":kinderRequest.Operation==KinderOperation.Exchange?$"{name} 専用チケット1枚\n消費ポイント 100 ／ 所持 {state.kinderPoints}\n人物の付与はチケット使用時です。":kinderRequest.Operation==KinderOperation.TicketDraw?$"{name} を100%付与\n専用チケット1枚を消費\n所持済みなら専用欠片100、最大後なら汎用100。":"検証用の初回配布 3000石\n費用なし ／ この保存につき1回のみ。";
                Label(605,329,885,220,description,growthTextStyle);
                Label(605,605,885,86,kinderSaveError??"確定後に抽選し、費用と全報酬を一括保存します。\n保存失敗時は再抽選せず同じ結果を再保存します。",growthSmallStyle);
                if(GrowthButton(605,719,570,62,formalProgression.HasPending?"同じ結果で保存を再試行":"この内容で確定する",true,true)){
                    try{
                        if(formalProgression.CommitKinder(kinderRequest,kinderBanner,KinderRandom.NextBelow,formalGrowthStore.Save)!=GrowthCommitResult.SaveFailed){kinderReceipt=formalProgression.KinderReceipt(kinderRequest.Id);kinderScreen=kinderReceipt.kinderOutcomes.Length>0?KinderScreen.Revealing:KinderScreen.Result;kinderRevealStarted=Time.unscaledTime;kinderSaveError=null;}
                        else kinderSaveError="保存できませんでした。石・報酬はまだ変更していません。";
                    }catch(Exception e){kinderSaveError="保存できませんでした。内容を保持して再試行します。";Debug.LogException(e);}
                }
                if(GrowthButton(1190,719,300,62,"取消",!formalProgression.HasPending))KinderBack();
            }else if(kinderScreen==KinderScreen.Revealing){
                float progress=Mathf.Clamp01((Time.unscaledTime-kinderRevealStarted)/1.8f);
                float radius=30+90*progress;
                for(int i=0;i<8;i++){float angle=(i*45+Time.unscaledTime*12)*Mathf.Deg2Rad;GrowthDiamond(1050+Mathf.Cos(angle)*radius,460+Mathf.Sin(angle)*radius,12+18*progress);}
                GrowthDiamond(1050,460,45);Label(790,625,600,50,"新しい誓いが、記憶の庭へ。",growthTextStyle,gold);
                if(progress>=1 || GrowthButton(605,727,885,62,"演出をスキップ ／ 結果を見る"))kinderScreen=KinderScreen.Result;
            }else{
                if(kinderReceipt.kinderOutcomes.Length==0)Label(605,327,885,130,kinderRequest.Operation==KinderOperation.Exchange?"専用チケット1枚を保存しました。\nチケット使用画面で誓女を迎えられます。":"初回3000石を保存しました。",growthTextStyle,gold);
                else for(int i=0;i<kinderReceipt.kinderOutcomes.Length;i++){
                    var r=kinderReceipt.kinderOutcomes[i];string label=r.kind=="heroine"?combatDefinitions.Hero(r.heroineId).name+"\n"+(r.grantKind=="owned"?"初回入手":r.grantKind=="overflow"?"汎用超過素材100":"専用欠片100"):(r.kind=="nectar"?"ネクタル":"覚醒結晶")+" ＋"+r.amount;
                    Label(605+(i%2)*450,315+(i/2)*68,430,64,label,growthTextStyle,r.kind=="heroine"?gold:ivory);
                }
                Label(605,670,885,40,$"保存済み ／ 石 {state.stones} ／ 共通ポイント {state.kinderPoints}",growthSmallStyle);
                if(GrowthButton(605,727,885,62,"キンダーガーデンの入口へ",true,true))KinderBack();
            }
            Label(90,850,1420,32,formalProgression.HasPending?"保存待ち ／ 同じ結果を再保存してください":"キンダーガーデン  ／  "+titles[(int)kinderScreen]+"    •    Escで戻る",growthSmallStyle);
        }
        private void PrepareKinderCapture(string[] args)
        {
            var save=formalProgression.Snapshot;save.stones=3000;save.kinderPoints=200;save.tickets=new[]{new HeroineTicket {heroineId=kinderBanner.heroineIds[0],count=1}};
            formalProgression=new FormalProgression(save,combatDefinitions.FormationIds);kinderGarden=true;encounter=null;title=false;
            if(args.Contains("-captureKinderDraw"))kinderScreen=KinderScreen.Draw;
            if(args.Contains("-captureKinderExchange"))kinderScreen=KinderScreen.Exchange;
            if(args.Contains("-captureKinderTickets"))kinderScreen=KinderScreen.Tickets;
            if(args.Contains("-captureKinderRates"))kinderScreen=KinderScreen.Rates;
            if(args.Contains("-captureKinderConfirm")){kinderScreen=KinderScreen.Draw;kinderCount=10;ConfirmKinder(KinderOperation.StoneDraw,save);}
            if(args.Contains("-captureKinderResult")){
                kinderRequest=new KinderRequest("capture.kinder-result",save.revision,KinderOperation.StoneDraw,10);int draws=0;
                formalProgression.CommitKinder(kinderRequest,kinderBanner,max=>max==10000?(draws++==0?0:300):0,s=>true);
                kinderReceipt=formalProgression.KinderReceipt(kinderRequest.Id);kinderScreen=KinderScreen.Result;
            }
        }
    }
}
