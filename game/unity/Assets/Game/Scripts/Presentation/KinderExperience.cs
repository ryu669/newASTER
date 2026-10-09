using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private enum KinderScreen { Entrance, Draw, Exchange, Tickets, Rates, Confirmation, Result, Revealing, Targets }
        private KinderScreen kinderScreen,kinderOrigin;
        private KinderRequest kinderRequest;
        private FormalKinderBanner kinderBanner;
        private GrowthReceipt kinderReceipt;
        private string kinderSaveError;
        private int kinderSelection,kinderCount=1,kinderPage;
        private float kinderRevealStarted;
        private void InitializeKinder()
        {
            if(ProductionStoryActive){kinderBanner=NewAster.Data.ProductionEconomyCatalog.Kinder(combatDefinitions.HeroineIds);kinderBanner.Validate(combatDefinitions.HeroineIds);return;}
            var source=Resources.Load<TextAsset>("Economy/kinder-trial");
            if(source==null)throw new ArgumentException("Kinder rules missing.");
            kinderBanner=JsonUtility.FromJson<FormalKinderBanner>(source.text);kinderBanner.Validate(combatDefinitions.HeroineIds);
        }
        private void KinderBack()
        {
            if(formalProgression.HasPending)return;
            if(kinderScreen==KinderScreen.Revealing){kinderScreen=KinderScreen.Result;return;}
            if(kinderScreen==KinderScreen.Entrance){kinderGarden=false;BackBookPage();return;}
            if(kinderScreen==KinderScreen.Confirmation){kinderRequest=null;kinderScreen=kinderOrigin;return;}
            kinderScreen=KinderScreen.Entrance;kinderRequest=null;kinderReceipt=null;
        }
        private void ConfirmKinder(KinderOperation operation,FormalGrowthSave state,string heroineId=null)
        {
            kinderRequest=new KinderRequest(operation==KinderOperation.IntroGrant?"grant.plan4-kinder-introduction":Guid.NewGuid().ToString("N"),state.revision,operation,operation==KinderOperation.StoneDraw?kinderCount:1,heroineId,kinderBanner.contentVersion);
            formalProgression.PreviewKinder(kinderRequest,kinderBanner);kinderOrigin=kinderScreen;kinderScreen=KinderScreen.Confirmation;kinderSaveError=null;
        }
        private bool BeginKinderStoneDraw(int count)
        {
            if(!BookInputAllowed || kinderScreen==KinderScreen.Confirmation || (count!=1 && count!=10) || formalProgression.Snapshot.stones<300*count)return false;
            kinderCount=count;ConfirmKinder(KinderOperation.StoneDraw,formalProgression.Snapshot);return true;
        }
        private void DrawKinderLanding(FormalGrowthSave state)
        {
            if(GrowthButton(605,315,425,55,"召喚対象",BookInputAllowed))kinderScreen=KinderScreen.Targets;
            if(GrowthButton(1060,315,430,55,"提供割合",BookInputAllowed))kinderScreen=KinderScreen.Rates;
            if(GrowthButton(605,395,425,55,"ポイント交換",BookInputAllowed))kinderScreen=KinderScreen.Exchange;
            if(GrowthButton(1060,395,430,55,"チケット使用",BookInputAllowed))kinderScreen=KinderScreen.Tickets;
            Label(605,493,885,38,"★6 3% ／ 全"+kinderBanner.heroineIds.Length+"形態",growthTextStyle,gold);
            if(GrowthButton(605,555,425,100,"1回召喚\n石 300",BookInputAllowed && state.stones>=300,true))BeginKinderStoneDraw(1);
            if(GrowthButton(1060,555,430,100,"10回召喚\n石 3,000",BookInputAllowed && state.stones>=3000,true))BeginKinderStoneDraw(10);
            if(GrowthButton(605,685,425,45,"特別交換",BookInputAllowed)){affectionShop=true;affectionPanel=false;}
            if(GrowthButton(1060,685,430,45,"星の恵み",BookInputAllowed))OpenEngagement();
            if(formalProgression.KinderReceipt("grant.plan4-kinder-introduction")==null && GrowthButton(605,750,885,45,"初回の贈り物 ／ 石 3,000",BookInputAllowed))ConfirmKinder(KinderOperation.IntroGrant,state);
        }
        private void DrawKinderConfirmation(FormalGrowthSave state)
        {
            drawingModal=true;GrowthFill(0,0,1600,900,new Color(0,0,0,.78f));GrowthFill(345,190,910,560,ink);GrowthFrame(345,190,910,560);
            var request=kinderRequest;string name=request.HeroineId==null?"":combatDefinitions.Hero(request.HeroineId).name;
            string title=request.Operation==KinderOperation.StoneDraw?request.Count+"回召喚":request.Operation==KinderOperation.Exchange?"専用チケット交換":request.Operation==KinderOperation.TicketDraw?"チケット召喚":"初回の贈り物";
            Label(390,222,760,50,title,growthTitleStyle,gold);
            if(GrowthButton(1160,217,55,48,"×",!formalProgression.HasPending)){KinderBack();return;}
            int tickets=state.tickets.SingleOrDefault(t=>t.heroineId==request.HeroineId)?.count??0;
            string summary=request.Operation==KinderOperation.StoneDraw?"石 "+(300*request.Count).ToString("N0")+" 消費":request.Operation==KinderOperation.IntroGrant?"石 3,000 受取":name+" ／ 専用チケット1枚";
            Label(390,300,820,72,summary,growthTextStyle);
            string rows=request.Operation==KinderOperation.StoneDraw?"石　"+state.stones.ToString("N0")+" → "+(state.stones-300*request.Count).ToString("N0")+"\nポイント　"+state.kinderPoints+" → "+(state.kinderPoints+request.Count):request.Operation==KinderOperation.Exchange?"ポイント　"+state.kinderPoints+" → "+(state.kinderPoints-100)+"\nチケット　"+tickets+" → "+(tickets+1):request.Operation==KinderOperation.TicketDraw?"チケット　"+tickets+" → "+(tickets-1)+"\n"+(state.heroines.Any(h=>h.heroineId==request.HeroineId)?"所持済み ／ "+(state.heroines.Single(h=>h.heroineId==request.HeroineId).duplicateRank>=5?"汎用超過素材 +100":"専用欠片 +100"):"初回入手"):"石　"+state.stones.ToString("N0")+" → "+(state.stones+3000).ToString("N0");
            GrowthFill(390,395,820,125,new Color(.06f,.13f,.18f));Label(415,414,770,95,rows,growthTextStyle);
            if(kinderSaveError!=null)Label(390,535,820,74,kinderSaveError,growthSmallStyle);
            if(GrowthButton(390,650,330,60,"キャンセル",!formalProgression.HasPending))KinderBack();
            if(GrowthButton(750,650,460,60,formalProgression.HasPending?"保存を再試行":request.Operation==KinderOperation.StoneDraw || request.Operation==KinderOperation.TicketDraw?"召喚する":request.Operation==KinderOperation.Exchange?"交換する":"受け取る",true,true)){
                try{
                    if(formalProgression.CommitKinder(request,kinderBanner,KinderRandom.NextBelow,SaveFormalGrowth)!=GrowthCommitResult.SaveFailed){kinderReceipt=formalProgression.KinderReceipt(request.Id);kinderScreen=kinderReceipt.kinderOutcomes.Length>0?KinderScreen.Revealing:KinderScreen.Result;kinderRevealStarted=Time.unscaledTime;kinderSaveError=null;}
                    else kinderSaveError="保存できませんでした。再試行してください。";
                }catch(Exception e){kinderSaveError="保存できませんでした。再試行してください。";Debug.LogException(e);}
            }
        }
        private void DrawKinderExperience()
        {
            GrowthStyles();var state=formalProgression.Snapshot;
            bool priorEnabled=GUI.enabled;bool confirming=kinderScreen==KinderScreen.Confirmation;var visible=confirming?kinderOrigin:kinderScreen;if(expansionRecruitmentOpen || confirming)GUI.enabled=false;
            PalaceBackdrop("leaf");GrowthFill(38,95,1524,732,ink);GrowthFrame(46,87,1508,732);GrowthLine(555,105,555,800,gold,2);
            if(visible!=KinderScreen.Entrance && GrowthButton(80,118,180,48,"‹ 戻る",!formalProgression.HasPending))KinderBack();
            Label(92,214,420,72,"キンダーガーデン",growthTitleStyle);
            DrawHeroPortrait(new Rect(92,300,420,340),kinderBanner.heroineIds[kinderSelection]);
            Label(92,650,420,65,combatDefinitions.Hero(kinderBanner.heroineIds[kinderSelection]).name,growthTextStyle,gold);
            Label(92,737,420,40,"実装済み全"+kinderBanner.heroineIds.Length+"形態 ／ 育成素材",growthSmallStyle);
            Label(605,125,885,48,"石 "+state.stones.ToString("N0")+"　／　ポイント "+state.kinderPoints+"　／　チケット "+state.tickets.Sum(t=>t.count),growthTextStyle);
            string[] titles={"召喚","召喚","ポイント交換","専用チケット","提供割合","確認","召喚結果","召喚","召喚対象"};
            Label(605,204,880,55,titles[(int)visible],growthTitleStyle);GrowthLine(605,270,1498,270,gold);
            if(visible==KinderScreen.Entrance || visible==KinderScreen.Draw){
                if(visible==KinderScreen.Entrance)DrawRRecruitment();
                DrawKinderLanding(state);
            }else if(visible==KinderScreen.Exchange || visible==KinderScreen.Tickets || visible==KinderScreen.Targets){
                bool exchange=visible==KinderScreen.Exchange,pool=visible==KinderScreen.Targets;
                Label(605,305,880,50,pool?"全"+kinderBanner.heroineIds.Length+"形態":exchange?$"ポイント {state.kinderPoints} ／ チケット1枚 100":"専用チケット",growthTextStyle);
                int pages=(kinderBanner.heroineIds.Length+4)/5;kinderPage=Mathf.Clamp(kinderPage,0,pages-1);
                for(int i=kinderPage*5;i<Math.Min(kinderBanner.heroineIds.Length,(kinderPage+1)*5);i++){
                    string id=kinderBanner.heroineIds[i];int count=state.tickets.SingleOrDefault(t=>t.heroineId==id)?.count??0;
                    string name=combatDefinitions.Hero(id).name;
                    if(GrowthButton(605,359+(i%5)*53,885,48,(kinderSelection==i?"◆ ":"")+name+(pool?(state.heroines.Any(h=>h.heroineId==id)?" ／ 所持":" ／ 未所持"):exchange?"":" ／ 所持 "+count)))kinderSelection=i;
                }
                if(GrowthButton(605,632,250,36,"‹ 前の5形態",kinderPage>0)){kinderPage--;kinderSelection=kinderPage*5;}
                Label(900,632,280,36,$"{kinderPage+1} / {pages}",growthSmallStyle);
                if(GrowthButton(1240,632,250,36,"次の5形態 ›",kinderPage+1<pages)){kinderPage++;kinderSelection=kinderPage*5;}
                string selected=kinderBanner.heroineIds[kinderSelection];int owned=state.tickets.SingleOrDefault(t=>t.heroineId==selected)?.count??0;
                if(!pool)Label(605,675,885,30,combatDefinitions.Hero(selected).name+" ／ 専用チケット "+(exchange?"1枚":owned+"枚"),growthSmallStyle);
                if(!pool && GrowthButton(605,727,885,62,exchange?"交換内容を確認する":"チケット使用を確認する",exchange?state.kinderPoints>=100:owned>0,true))ConfirmKinder(exchange?KinderOperation.Exchange:KinderOperation.TicketDraw,state,selected);
            }else if(visible==KinderScreen.Rates){
                Label(605,310,885,84,$"★6合計 3% ／ 1形態あたり {(3m/kinderBanner.heroineIds.Length):0.####}%\nカテゴリー当選後に全{kinderBanner.heroineIds.Length}形態を均等抽選。",growthTextStyle);
                int total=kinderBanner.materials.Sum(m=>m.weight);
                for(int i=0;i<kinderBanner.materials.Length;i++){var m=kinderBanner.materials[i];Label(605,430+i*68,885,52,$"{(m.kind=="nectar"?"ネクタル":"覚醒結晶")} {m.amount}個    {(97m*m.weight/total):0.##}%",growthTextStyle);}
            }else if(visible==KinderScreen.Revealing){
                DrawKinderTrain();
            }else{
                if(kinderReceipt.kinderOutcomes.Length==0)Label(605,327,885,130,kinderRequest.Operation==KinderOperation.Exchange?"専用チケット1枚を保存しました。\nチケット使用画面で誓女を迎えられます。":"初回3000石を保存しました。",growthTextStyle,gold);
                else for(int i=0;i<kinderReceipt.kinderOutcomes.Length;i++){
                    var r=kinderReceipt.kinderOutcomes[i];string label=r.kind=="heroine"?combatDefinitions.Hero(r.heroineId).name+"\n"+(r.grantKind=="owned"?"初回入手":r.grantKind=="overflow"?"汎用超過素材100":"専用欠片100"):(r.kind=="nectar"?"ネクタル":"覚醒結晶")+" ＋"+r.amount;
                    Label(605+(i%2)*450,315+(i/2)*68,430,64,label,growthTextStyle,r.kind=="heroine"?gold:ivory);
                }
                Label(605,670,885,40,$"保存済み ／ 石 {state.stones} ／ 共通ポイント {state.kinderPoints}",growthSmallStyle);
                if(GrowthButton(605,727,885,62,"キンダーガーデンの入口へ",true,true))KinderBack();
            }
            GUI.enabled=priorEnabled;if(confirming && kinderScreen==KinderScreen.Confirmation)DrawKinderConfirmation(state);DrawAnnihilatorRecruitmentDialog();
        }
        private void PrepareKinderCapture(string[] args)
        {
            int heroAt=Array.IndexOf(args,"-heroineId");
            int requested=heroAt>=0 && heroAt+1<args.Length?Array.IndexOf(kinderBanner.heroineIds,args[heroAt+1]):0;
            kinderSelection=Math.Max(0,requested);kinderPage=kinderSelection/5;
            if(ProductionStoryActive)AcceptanceCheck(kinderBanner.heroineIds.SequenceEqual(combatDefinitions.HeroineIds),"Summon pool includes every implemented form");
            var save=formalProgression.Snapshot;save.stones=3000;save.kinderPoints=200;save.tickets=new[]{new HeroineTicket {heroineId=kinderBanner.heroineIds[kinderSelection],count=1}};
            formalProgression=new FormalProgression(save,combatDefinitions.HeroineIds);kinderGarden=true;encounter=null;title=false;
            if(args.Contains("-captureKinderDraw"))kinderScreen=KinderScreen.Draw;
            if(args.Contains("-captureKinderExchange"))kinderScreen=KinderScreen.Exchange;
            if(args.Contains("-captureKinderTickets"))kinderScreen=KinderScreen.Tickets;
            if(args.Contains("-captureKinderRates"))kinderScreen=KinderScreen.Rates;
            if(args.Contains("-captureKinderConfirm")){kinderScreen=KinderScreen.Draw;kinderCount=10;ConfirmKinder(KinderOperation.StoneDraw,save);}
            if(args.Contains("-captureKinderResult")){
                kinderRequest=new KinderRequest("capture.kinder-result",save.revision,KinderOperation.StoneDraw,10,bannerVersion:kinderBanner.contentVersion);int draws=0;
                int selected=Array.IndexOf(args,"-heroineId");int actor=selected>=0 && selected+1<args.Length?Array.IndexOf(kinderBanner.heroineIds,args[selected+1]):0;actor=Math.Max(0,actor);kinderSelection=actor;
                formalProgression.CommitKinder(kinderRequest,kinderBanner,max=>max==10000?(draws++==0?0:300):max==kinderBanner.heroineIds.Length?actor:0,s=>true);
                kinderReceipt=formalProgression.KinderReceipt(kinderRequest.Id);kinderScreen=KinderScreen.Result;
            }
            Debug.Log("KINDER_ALL_IMPLEMENTED_CAPTURE_PASS forms="+kinderBanner.heroineIds.Length+" selection="+kinderBanner.heroineIds[kinderSelection]+" page="+(kinderPage+1)+" physicalInput=0");
        }
    }
}
