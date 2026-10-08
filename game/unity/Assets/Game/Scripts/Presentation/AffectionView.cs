using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool affectionPanel,affectionShop,affectionConfirm;
        private bool AffectionMaxPending=>formalCampaign!=null && formalCampaign.HasAffection && IsBookScreen && LifeSnapshot().affection.pendingMaxLevelPersonIds.Length>0;
        private bool AffectionModalVisible=>affectionPanel || affectionShop || affectionConfirm || affectionError!=null || affectionRequest!=null || affectionInteraction!=null || AffectionMaxPending;
        private string affectionForm,affectionMessage,affectionError,affectionConfirmKind;
        private AffectionRequest affectionRequest;
        private AffectionInteractionSession affectionInteraction;
        private Vector2 affectionScroll;
        private int affectionRosterFilter;
        private Texture2D eternalRingArt;
        private void OpenAffection(string form)
        {if(!BookInputAllowed)return;affectionForm=form;affectionPanel=true;affectionShop=false;affectionScroll=Vector2.zero;}
        private string AffectionSummary(string form)
        {
            if(!formalCampaign.HasAffection)return "好感度・恋愛";
            var a=AffectionService.State(LifeSnapshot(),HomeData(),form);
            return "好感度 Lv"+a.level+" ／ EXP "+a.exp+"/100 ／ 上限"+a.levelCap+(AffectionService.IsLover(a)?" ／ 恋人":"");
        }
        private bool AffectionRosterMatch(string form)
        {
            if(affectionRosterFilter==0)return true;var s=LifeSnapshot();var a=AffectionService.State(s,HomeData(),form);
            return affectionRosterFilter==1?AffectionService.IsLover(a):affectionRosterFilter==2?AffectionEventResolver.ForPerson(s,HomeData(),form).Any(e=>a.level>=e.requiredAffectionLevel && !a.readEventIds.Contains(e.id)):affectionRosterFilter==3?a.level>=10:a.level>=20;
        }
        private void ProposeAffection(string kind,string[] forms,string content,string id=null)
        {
            if(affectionRequest!=null || formalCampaign.HasPending || formalProgression.HasPending)return;
            affectionRequest=new AffectionRequest(id??Guid.NewGuid().ToString("N"),formalCampaign.Revision,kind,forms,content);RetryAffection();
        }
        private void RetryAffection()
        {
            try{
                var request=affectionRequest;var result=formalCampaign.CommitAffection(request,HomeData(),formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign);
                if(result==GrowthCommitResult.SaveFailed){affectionError="保存できませんでした。同じ内容で再試行してください。";return;}
                affectionRequest=null;affectionError=null;affectionConfirm=false;lifeSnapshotCached=null;
                formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);
                affectionMessage=request.Kind=="ring-purchase"?"永遠の誓環を1個購入しました。":request.Kind=="ring-use"?"好感度上限が99になりました。":request.Kind=="max-display"?"長く重ねた日々の証 — 好感度Lv99に到達しました。":"交流を記録しました。";
                if(gardenLifeRuntime!=null && affectionInteraction!=null){affectionInteraction=null;gardenLifeRuntime.Paused=false;gardenLifeMessage=affectionMessage;}
            }catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is System.IO.IOException){affectionError=e.Message;if(!formalCampaign.HasPending)affectionRequest=null;}
        }
        private void BeginPlayerAffection(string form,bool together)
        {
            if(affectionInteraction!=null || affectionRequest!=null || gardenLifeRuntime==null || gardenLifeRuntime.Paused || formalCampaign.HasPending)return;
            var actor=gardenLifeRuntime.Agents.SingleOrDefault(a=>a.heroineId==form);if(actor==null)return;
            var context=gardenLifeRuntime.Context(actor);
            if(together && (actor.kind!="Furniture" && actor.kind!="Scenery" || string.IsNullOrEmpty(context.furnitureId))){gardenLifeMessage="家具や景観を利用している時に、一緒に過ごせます。";return;}
            string content=together?"together/"+context.furnitureId+"/"+context.interactionTag:"conversation.generic.greeting";
            affectionInteraction=new AffectionInteractionSession(Guid.NewGuid().ToString("N"),form,content,book.SubjectId,together?8:4);
            gardenLifeRuntime.Greet(form);gardenLifeRuntime.Paused=true;gardenLifeMessage=together?"同じ場所で一緒に過ごしています。":"こちらに顔を向け、言葉を交わしています。";
        }
        private void CancelPlayerAffection()
        {if(affectionInteraction==null || affectionRequest!=null)return;affectionInteraction.Cancel();affectionInteraction=null;if(gardenLifeRuntime!=null)gardenLifeRuntime.Paused=false;gardenLifeMessage="交流を取り消しました。";}
        private void FinishPlayerActivity()
        {
            if(gardenLifeRuntime?.ActivityId==null || formalCampaign.HasPending)return;
            string activity=gardenLifeRuntime.ActivityId;var ids=gardenLifeRuntime.Agents.Where(a=>a.activityId==activity).Select(a=>a.heroineId).ToArray();
            gardenLifeRuntime.EndActivity();ProposeAffection("activity",ids,"activity/"+activity);
        }
        private void UpdateAffection()
        {
            if(affectionInteraction==null || affectionRequest!=null)return;
            bool visible=!title && encounter==null && adv==null && book.Bookmark==BookBookmark.Gardens && book.SubjectId==affectionInteraction.GardenId && gardenLifeRuntime!=null;
            if(!visible){CancelPlayerAffection();return;}
            affectionInteraction.Tick(Time.unscaledDeltaTime,Application.isFocused && !help && !bookSystemOpen && !affectionPanel);
            if(affectionInteraction.Completed)ProposeAffection("interaction",new[]{affectionInteraction.FormId},affectionInteraction.ContentId,affectionInteraction.Id);
        }
        private bool CloseAffectionLayer()
        {
            if(affectionRequest!=null)return true;
            if(affectionInteraction!=null){CancelPlayerAffection();return true;}
            if(affectionConfirm){affectionConfirm=false;return true;}
            if(affectionPanel || affectionShop){affectionPanel=false;affectionShop=false;return true;}return false;
        }
        private void DrawAffectionOverlay()
        {
            if(formalCampaign==null || !formalCampaign.HasAffection)return;
            GrowthStyles();
            if(affectionInteraction!=null){GrowthFrame(70,185,825,85);Label(90,200,585,50,gardenLifeMessage,growthTextStyle);if(GrowthButton(695,205,170,45,"交流を取消",affectionRequest==null))CancelPlayerAffection();}
            var save=LifeSnapshot();
            if(!affectionPanel && !affectionShop && affectionError==null){
                var person=save.affection.pendingMaxLevelPersonIds.FirstOrDefault();
                if(person!=null && IsBookScreen && !formalCampaign.HasPending && !help && !bookSystemOpen){string form=save.growth.heroines.First(h=>HomeData().PersonId(h.heroineId)==person).heroineId;GrowthFill(0,88,1600,812,new Color(0,0,0,.8f));GrowthFrame(350,300,900,220);Label(390,330,820,110,combatDefinitions.Hero(form).name+"\n長く重ねた日々の証 — 好感度Lv99",growthTitleStyle);if(GrowthButton(590,450,420,50,"日々の証を心に留める"))ProposeAffection("max-display",new[]{form},"max-level");}return;
            }
            GrowthFill(0,88,1600,812,new Color(0,0,0,.8f));GrowthFrame(235,130,1130,670);
            if(affectionError!=null){Label(275,225,1050,130,affectionError,growthTextStyle);if(affectionRequest!=null && GrowthButton(425,475,750,60,"同じ内容で保存を再試行"))RetryAffection();else if(affectionRequest==null && GrowthButton(425,475,750,60,"戻る"))affectionError=null;return;}
            if(affectionConfirm){
                Label(285,185,1030,60,"永遠の誓環",growthTitleStyle);DrawEternalRing(new Rect(365,290,185,185));
                Label(600,285,655,170,affectionConfirmKind=="ring-purchase"?"召喚石10,000個で指輪1個を購入します。\n共通在庫へ保存し、抽選には入りません。":"この人物の好感度上限を20から99にします。\n指輪1個を消費します。返品・付け替えはできません。",growthTextStyle);
                if(GrowthButton(285,630,650, sixty,"確定して保存する",true,true))ProposeAffection(affectionConfirmKind,affectionConfirmKind=="ring-purchase"?Array.Empty<string>():new[]{affectionForm},"eternal-vow-ring");
                if(GrowthButton(965,630,350, sixty,"取消"))affectionConfirm=false;return;
            }
            if(GrowthButton(1145,155,170,45,"閉じる")){affectionPanel=false;affectionShop=false;affectionMessage=null;return;}
            if(affectionShop){
                Label(285,180,810,60,"召喚・交換 ／ 特別交換",growthTitleStyle);DrawEternalRing(new Rect(365,290,230,230));
                Label(685,295,590,200,"永遠の誓環\n召喚石 10,000個\n所持指輪 "+save.growth.eternalRings+"個 ／ 召喚石 "+save.growth.stones,growthTextStyle);
                Label(285,560,1030,75,affectionMessage??"Lv20の人物に使うと上限が99になります。\nガチャ排出・返品・付け替えはありません。",growthSmallStyle);
                if(GrowthButton(425,680,750, sixty,"購入内容を確認する",save.growth.stones>=AffectionRingService.Price,true)){affectionConfirmKind="ring-purchase";affectionConfirm=true;}return;
            }
            var a=AffectionService.State(save,HomeData(),affectionForm);
            Label(285,175,810,55,combatDefinitions.Hero(affectionForm).name+" ／ 好感度・恋愛",growthTitleStyle);
            Label(285,245,850,50,"Lv "+a.level+" ／ EXP "+a.exp+" / 100"+(AffectionService.IsLover(a)?" ／ 恋人":""),growthTextStyle);
            GrowthFill(285,300,650,12,new Color(.08f,.12f,.18f));GrowthFill(285,300,650*a.exp/100f,12,gold);
            if(a.levelCap==99)DrawEternalRing(new Rect(975,250,75,75));Label(1065,257,250,60,"好感度上限 "+a.levelCap,growthTitleStyle);
            var events=AffectionEventResolver.ForPerson(save,HomeData(),affectionForm);int unread=events.Count(e=>a.level>=e.requiredAffectionLevel && !a.readEventIds.Contains(e.id));
            Label(285,335,1000,35,"物語・回想 ／ 閲覧可能 "+events.Count(e=>a.level>=e.requiredAffectionLevel)+"件 ／ 未読 "+unread+"件",growthSmallStyle);
            affectionScroll=GUI.BeginScrollView(new Rect(285,380,1030,225),affectionScroll,new Rect(0,0,1000,Math.Max(225,events.Length*54)));
            for(int i=0;i<events.Length;i++){
                var ev=events[i];bool open=a.level>=ev.requiredAffectionLevel,read=a.readEventIds.Contains(ev.id);
                string title=!open && ev.visibility=="hint"?"これから紡ぐ物語":ProductionStoryActive?ProductionStoryTitle(ev.id):ev.id;
                if(GrowthButton(0,i*54,995,48,(read?"回想":open?"未読":"Lv"+ev.requiredAffectionLevel+"で解放")+" ／ "+title,open)){affectionPanel=false;gardenLifePanel=null;BeginAdv(ev.id,read);}
            }GUI.EndScrollView();
            Label(285,625,1030,40,affectionMessage??"",growthSmallStyle);
            if(GrowthButton(285,700,650,55,"永遠の誓環を使用する ／ 所持 "+save.growth.eternalRings+"個",a.level==20 && a.levelCap==20 && save.growth.eternalRings>0,true)){affectionConfirmKind="ring-use";affectionConfirm=true;}
            if(GrowthButton(965,700,350,55,"特別交換へ")){affectionPanel=false;affectionShop=true;}
        }
        private void DrawEternalRing(Rect rect)
        {if(eternalRingArt==null)eternalRingArt=Resources.Load<Texture2D>("UI/eternal-vow-ring-v1");if(eternalRingArt==null)throw new InvalidOperationException("Missing original eternal-vow ring image.");GUI.DrawTexture(rect,eternalRingArt,ScaleMode.ScaleToFit,true);}
    }
}
