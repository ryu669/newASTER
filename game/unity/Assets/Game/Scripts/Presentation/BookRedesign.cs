using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool bookSystemOpen, activeRelicHunt;
        private static readonly BookBookmark[] RibbonOrder={BookBookmark.Colossi,BookBookmark.Heroines,BookBookmark.Gardens,BookBookmark.Stories,BookBookmark.Summoning,BookBookmark.Items,BookBookmark.RelicHunt,BookBookmark.NewWorld,BookBookmark.PossibleWorlds};
        private static readonly string[] RibbonNames={"巨神獣","誓女","庭","物語","召喚","アイテム","レリックハント","新天地","可能世界"};
        private Texture2D bookEmblemAtlas;
        private void DrawBookEmblem(Rect rect,int index)
        {
            if(bookEmblemAtlas==null)bookEmblemAtlas=Resources.Load<Texture2D>("UI/book-emblems-v1");
            if(bookEmblemAtlas==null)throw new InvalidOperationException("Missing book emblem image atlas.");
            GUI.DrawTextureWithTexCoords(rect,bookEmblemAtlas,new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f),true);
        }
        private bool CanOpenBookSystem=>BookInputAllowed && relicRequest==null && growthRequest==null && kinderRequest==null && terraformRequest==null && homeRequest==null && !engagementOpen;
        private bool IsBookScreen=>!title && encounter==null && adv==null && !modelViewer && !recoveryActive && combatDefinitionError==null && !artSample && plan9EnemyPreview==null && plan9Expression==null && plan9Cg==null;
        private void DrawBookRibbon()
        {
            GrowthStyles();GrowthFill(0,0,1600,88,ink);
            int current=Array.IndexOf(RibbonOrder,book.Bookmark);if(current<0)current=0;
            DrawRibbon(current,new Rect(4,0,46,Math.Max(180,68+RibbonNames[current].Length*27)),true);
            for(int i=0;i<RibbonOrder.Length;i++)if(i!=current)DrawRibbon(i,new Rect(552+i*100,0,48,104),false);
            bool prior=GUI.enabled;GUI.enabled=prior && !AffectionModalVisible && !OopartModalVisible && !help && !bookSystemOpen && !engagementOpen && relicRequest==null && growthRequest==null && kinderRequest==null && terraformRequest==null && homeRequest==null && !expansionRecruitmentOpen && !gardenDiscardConfirm && gardenPresetShortage==null && gardenLifeError==null && gardenLifeRequest==null;
            if(GrowthButton(1490,14,64,52,"？",true)){help=true;PlayProductionUiSound("決定");}
            GUI.enabled=prior;
            if(!help && !bookSystemOpen && book.Bookmark!=BookBookmark.Colossi && book.Bookmark!=BookBookmark.Stories && book.Bookmark!=BookBookmark.RelicHunt && book.Bookmark!=BookBookmark.Gardens && !formationOpen && !(book.Bookmark==BookBookmark.Heroines && growthScreen==GrowthScreen.Weapons)){if(GrowthButton(1060,842,215,40,"システム",CanOpenBookSystem))bookSystemOpen=true;}
            if(help){drawingModal=true;DrawHelp();}
            else if(bookSystemOpen)DrawBookSystem();
        }
        private void DrawRibbon(int index,Rect rect,bool selected)
        {
            var color=selected?new Color(.36f,.26f,.16f):new Color(.13f+.025f*(index%3),.20f,.25f);
            GrowthFill(rect.x,rect.y,rect.width,rect.height-14,color);
            for(int row=0;row<14;row++){float inset=row*rect.width/28;GrowthFill(rect.x+inset,rect.y+rect.height-14+row,rect.width-inset*2,1,color);}
            DrawBookEmblem(new Rect(rect.x+5,8,rect.width-10,rect.width-10),index);
            if(selected){var verticalStyle=new GUIStyle(growthTitleStyle){fontSize=21,alignment=TextAnchor.MiddleCenter};for(int i=0;i<RibbonNames[index].Length;i++)Label(rect.x,52+i*27,rect.width,27,RibbonNames[index][i].ToString(),verticalStyle,gold);}
            bool enabled=BookInputAllowed && !book.IsTransitioning && !help && !bookSystemOpen && !engagementOpen && relicRequest==null && growthRequest==null && kinderRequest==null && terraformRequest==null && homeRequest==null && !expansionRecruitmentOpen;
            bool prior=GUI.enabled;GUI.enabled=prior&&enabled;
            if(ImageUiSkin.Button(rect,"",GUIStyle.none) && !selected){collectionOpen=false;kinderGarden=false;trialPoemChapter=null;RequestBookBookmark(RibbonOrder[index]);}
            GUI.enabled=prior;
        }
        private void DrawBookFooter()
        {
            GrowthFill(0,812,1600,88,ink);
            if(Btn(40,836,190,44,"‹",BookInputAllowed && book.CanTurnPrevious))RequestBookTurn(-1);
            Label(247,840,140,36,(book.SubjectIndex+1)+" / "+Math.Max(1,book.SubjectCount),small,gold);
            if(Btn(400,836,190,44,"›",BookInputAllowed && book.CanTurnNext))RequestBookTurn(1);
            if(Btn(610,836,230,44,book.Face==BookFace.Overview?"詳細":"概要",BookInputAllowed && book.CanFlip))RequestBookFlip();
            if(Btn(1310,836,245,44,"システム",CanOpenBookSystem))bookSystemOpen=true;
        }
        private void DrawBookSystem()
        {
            drawingModal=true;if(titlePanel!=null){DrawTitlePanel();return;}GrowthFill(0,0,1600,900,new Color(0,0,0,.75f));GrowthFrame(430,220,740,450);
            Label(470,255,660,55,"システム",growthTitleStyle,gold);
            if(GrowthButton(480,340,640,55,"保存",BookInputAllowed,true)){Save();}
            if(GrowthButton(480,415,300,55,"設定")){OpenTitlePanel("settings");}
            if(GrowthButton(800,415,320,55,"表紙へ",BookInputAllowed)){book.Close();title=true;bookSystemOpen=false;}
            if(GrowthButton(480,490,640,55,"閉じる"))bookSystemOpen=false;
            if(status.Contains("保存"))Label(480,570,640,55,status,growthSmallStyle);
        }
        private void DrawStandaloneItems()
        {
            GrowthStyles();PalaceBackdrop("crown");GrowthFrame(90,100,1420,702);
            if(GrowthButton(150,130,590,58,"オーパーツ",relicRequest==null,collectionTab!=2))collectionTab=1;
            if(GrowthButton(790,130,640,58,"素材",relicRequest==null,collectionTab==2))collectionTab=2;
            if(relicRequest!=null)DrawRelicConfirmation();else if(collectionTab==2)DrawMaterialInventory();else DrawModernRelics();
        }
        private string BookHelpText()
        {
            string common="上部の画像しおりでページを切り替えます。開いているしおりは左側に移動し、名前が縦に表示されます。ほかのしおりの位置は固定です。\n下部の ‹ › で対象を変更、詳細／概要で同じ対象の情報を切り替えます。\n保存・表紙への移動は下部のシステムから。操作確定・討伐後は自動保存します。\n\n";
            if(encounter!=null)return "対象の部位を選び、READYの誓女のスキルを使います。\nジョブの固有資源は操作メニューで確認できます。\n部位破壊で敵を弱体化。チェイン率は装備では変わりません。\nEscで一時停止。撤退は確認してから実行します。";
            switch(book.Bookmark){
                case BookBookmark.Heroines:return common+"顔画像から誓女を選択。レベル・覚醒・誓いで育成します。\nスキル・特性の画像は詳細を開きます。神器の木は枝上のノードを選び、素材と効果を確認して解放・強化・装備します。\n装備は神器1つ、オーパーツ1つ。Escで前の画面へ。";
                case BookBookmark.Items:return common+"オーパーツ／素材を切り替えます。装備先は所持している全誓女から選べます。\n同名の抽選値は各項目の高値を保持。攻撃80／100、HP800／1000以上から直接強化できます。Lv上限120。\nジョブ適性は一致したジョブのみ。時計は敵の行動完了を1ターンと数え、戦闘ごとに戻ります。";
                case BookBookmark.Summoning:return common+"石・チケットで召喚し、ポイントで交換できます。費用と提供割合は召喚前に確認できます。保存成功後に結果が確定します。";
                case BookBookmark.Stories:return common+"章ごとに詩を集めると物語が解放されます。読む／再開で本文へ、回想で読了した章を読み直します。";
                case BookBookmark.Gardens:return common+"家具・人物・催事・環境を下部メニューで選びます。人物の「話す」は短い反応、「観察」は現在の暮らしを説明します。\n模様替えは家具を選んで庭をクリックし、編集パネルから確定します。戻す／やり直すは50操作まで。取消では保存しません。\n生活記録は新天地、鑑賞ではフレームと動作の停止を選べます。";
                case BookBookmark.NewWorld:case BookBookmark.PossibleWorlds:return common+"領域を選び、到達Lv・深度記録・極みを設定します。Lv6への変更は確認を表示します。可能世界では記述が生む現象を確認できます。";
                default:return common+"挑戦レベルを選んで5人で出撃します。通常討伐の遺物は5%で1回抽選。レリックハントは各35%、レベル帯に応じ1〜5回抽選。\n部位破壊後に本体を攻めると安全です。Lv45以上は極大技に注意。アステリアは全7領域Lv3が必要です。";
            }
        }
        private void DrawEncounterBookPage()
        {
            GrowthStyles();PalaceBackdrop("crown");GrowthFill(55,115,920,676,new Color(.95f,.90f,.79f));GrowthFrame(1004,115,541,676);
            DrawBookFooter();if(!book.HasSubject)return;
            bool stories=book.Bookmark==BookBookmark.Stories;
            if(stories){DrawStoryBookContent();DrawBookSubjectArt();return;}
            SyncBookSelectedLevel();var c=NewAster.Data.WorldCatalog.Colossi.Single(x=>x.Id==book.SubjectId);bool unlocked=campaign.ColossusUnlocks.IsUnlocked(c.Id);
            Label(87,150,850,65,unlocked?c.DisplayName:"未解放",heading,new Color(.12f,.17f,.20f));
            if(!unlocked){Label(87,250,850,100,"前の巨神獣の初回討伐で解放されます。",text);return;}
            DrawBookSubjectArt();
            if(book.Face==BookFace.Details){
                var d=NewAster.Data.ColossusCombatCatalog.Get(c.Id);Label(87,255,845,95,"世界の記憶\n"+string.Join(" ・ ",c.EnvironmentTags),text);
                Label(87,385,845,180,"部位\n"+string.Join(" ・ ",d.parts.Select((p,i)=>NewAster.Data.ColossusCombatCatalog.PartName(new BattlePart(p.id,p.baseHp,p.breakEffect,role:p.role),i))),text);
                Label(87,595,845,110,"大技　"+d.majorAction+"\n極大技　"+d.ultimateAction,text);return;
            }
            Label(87,255,845,100,"失われた世界の呪歌\n"+string.Join(" ・ ",c.EnvironmentTags),text);
            Label(87,390,845,55,"挑戦  Lv."+selectedLevel+"  /  "+campaign.Playable.HighestLevel,heading);
            int[] steps={-5,-1,1,5};for(int i=0;i<4;i++)if(Btn(87+i*148,480,133,52,(steps[i]>0?"＋":"−")+Math.Abs(steps[i]),BookInputAllowed))selectedLevel=Mathf.Clamp(selectedLevel+steps[i],1,campaign.Playable.HighestLevel);
            if(Btn(690,480,240,52,"最高レベル",BookInputAllowed))selectedLevel=campaign.Playable.HighestLevel;
            var band=CollectionData().rewardBands.Single(x=>x.ownerId==c.Id && selectedLevel>=x.minLevel && selectedLevel<=x.maxLevel);
            Label(87,555,845,80,book.Bookmark==BookBookmark.RelicHunt?"遺物 35% × "+band.draws+"回抽選 ／ 候補 "+band.relicIds.Length+"種類":"遺物 5% × 1回抽選 ／ TP・素材・詩",small);
            var missing=TerraformRules.Index(c.Id)==14?TerraformRules.MissingIntegration(campaign.Terraform):Array.Empty<string>();
            if(missing.Length>0)Label(87,637,845,55,"必要領域Lv3："+string.Join("・",missing.Select(TerraformCatalog.DomainName)),small);
            if(Btn(87,708,845,60,book.Bookmark==BookBookmark.RelicHunt?"レリックハントに出撃":"5人の誓女と出撃",BookInputAllowed && !book.IsTransitioning && missing.Length==0))StartBattle(c.Id);
        }
        private void DrawStoryBookContent()
        {
            var owner=CollectionData().owners.Single(o=>o.id==book.SubjectId);Label(87,150,845,60,CollectionOwnerName(owner),heading);
            bool ownerOpen=owner.kind=="heroine" || campaign.ColossusUnlocks.IsUnlocked(owner.id);var world=formalCampaign.Snapshot.world;
            if(!ownerOpen){Label(87,260,840,90,"巨神獣の解放後に記憶を読めます。",text);return;}
            for(int i=0;i<owner.chapterIds.Length;i++){
                var c=CollectionData().chapters.Single(x=>x.id==owner.chapterIds[i]);bool open=world.unlockedStoryIds.Contains(c.id),read=world.readStoryIds.Contains(c.id);float y=250+i*165;
                Label(87,y,820,43,ProductionStoryTitle(c.id),text);
                Label(87,y+52,350,36,"詩 "+c.poemIds.Count(world.poemIds.Contains)+" / "+c.poemIds.Length+"  "+(read?"読了":open?"解放済み":"未解放"),small);
                if(Btn(450,y+50,180,43,"詩の記録",BookInputAllowed)){collectionOwner=Array.IndexOf(CollectionData().owners,owner);collectionOpen=true;collectionTab=0;trialPoemChapter=c.id;}
                if(Btn(645,y+50,280,43,read?"回想":"読む／再開",BookInputAllowed && open))BeginAdv(c.id,read);
            }
        }
        private void DrawModernRelics()
        {
            if(CollectionData().oopartDefs.Length>0){DrawOopartInventoryPage();return;}
            var ledger=formalCampaign.Snapshot.collection;var items=ledger.relics;int pages=Math.Max(1,(items.Length+3)/4);collectionRelicPage=Mathf.Clamp(collectionRelicPage,0,pages-1);
            if(items.Length==0)Label(170,310,1200,95,"所持しているオーパーツはありません。",growthTitleStyle);
            foreach(var pair in items.Skip(collectionRelicPage*4).Take(4).Select((r,i)=>new{r,i})){
                var r=pair.r;var d=CollectionData().relics.Single(x=>x.id==r.id);float y=220+pair.i*130;
                GrowthFill(145,y,1285,124,new Color(.10f,.17f,.21f));if(d.jobId!=null)DrawSanctuaryIcon(new Rect(164,y+20,66,66),"resource."+d.jobId.Replace("job.",""),Color.white);else DrawBookEmblem(new Rect(164,y+20,66,66),d.turnEffect=="ramp"?11:d.turnEffect=="wane"?13:d.attackPercent>=40?9:d.defensePercent>0?12:d.speedPercent>0?15:6);
                Label(249,y+9,720,34,(d.name??r.id)+"  Lv."+r.level,growthTextStyle,gold);
                Label(249,y+44,735,44,NewAster.Data.ProductionEconomyCatalog.RelicAbility(d),new GUIStyle(growthSmallStyle){fontSize=16});
                int balance=FormalRelicRules.MaterialBalance(ledger,d);Label(249,y+88,735,27,"攻撃抽選 "+r.attackRoll+" / 100　HP抽選 "+r.hpRoll+" / 1000　強化素材 "+balance+"（Lv強化 "+r.level+"）",new GUIStyle(growthSmallStyle){fontSize=14});
                var e=ledger.equipment.SingleOrDefault(x=>x.relicId==r.id);
                if(GrowthButton(1015,y+12,385,43,e==null?"装備する":combatDefinitions.Hero(e.heroineId).name+" ／ 外す"))RelicSelect(r,e==null?RelicOperation.Equip:RelicOperation.Unequip,e?.heroineId??combatDefinitions.FormationIds[0]);
                if(GrowthButton(1015,y+64,118,40,"Lv ＋",RelicUnavailable(r,RelicOperation.LevelUp)==null))RelicSelect(r,RelicOperation.LevelUp);
                if(GrowthButton(1148,y+64,118,40,"攻撃 ＋",RelicUnavailable(r,RelicOperation.AttackUp)==null))RelicSelect(r,RelicOperation.AttackUp);
                if(GrowthButton(1281,y+64,118,40,"HP ＋",RelicUnavailable(r,RelicOperation.HpUp)==null))RelicSelect(r,RelicOperation.HpUp);
            }
            if(GrowthButton(150,753,190,40,"‹",collectionRelicPage>0))collectionRelicPage--;Label(365,757,160,32,(collectionRelicPage+1)+" / "+pages,growthSmallStyle);
            if(GrowthButton(555,753,190,40,"›",collectionRelicPage+1<pages))collectionRelicPage++;
            if(relicError!=null)Label(780,749,650,55,relicError,growthSmallStyle);
        }
    }
}
