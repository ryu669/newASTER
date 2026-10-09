using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private int terraformDomain, terraformDeep, terraformFusion, terraformTargetLevel = 1, possibleWorldTab;
        private bool terraformWarning, possibleWorlds, terraformSelectionReady;
        private string terraformMessage, worldNameInput, worldNameOwner;
        private Vector2 terraformScroll;
        private FormalTerraformRequest terraformRequest;
        private readonly Rect[] terraformDomainCards = {
            new Rect(135,255,240,64), new Rect(135,410,240,64), new Rect(135,565,240,64),
            new Rect(395,225,240,64), new Rect(655,255,240,64), new Rect(655,410,240,64), new Rect(655,565,240,64)
        };
        private TerraformSave TerraformPreview()
        {
            var snapshot = formalCampaign.Snapshot;
            TerraformRules.Synchronize(snapshot, CollectionData());
            return snapshot.world.terraform;
        }
        private void DrawTerraformBookPage()
        {
            GrowthStyles(); PalaceBackdrop("crown"); GrowthFrame(90,80,1420,745);
            possibleWorlds = book.Bookmark==BookBookmark.PossibleWorlds;if(possibleWorlds)int.TryParse(book.PageState.sortMode,out possibleWorldTab);
            if(gardenLifeRecords)DrawGardenLifeRecords();else{DrawTerraformExperience();if(!possibleWorlds && Btn(130,820,380,44,"生活記録をひらく"))gardenLifeRecords=true;} DrawBookTransition(true);
        }
        private void SelectTerraformDomain(TerraformSave s,int index)
        {
            terraformDomain=index;if(book.Bookmark==BookBookmark.NewWorld && book.SubjectId!="terraform.domain."+TerraformRules.DomainIds[index]){book.RequestSubject(BookBookmark.NewWorld,"terraform.domain."+TerraformRules.DomainIds[index]);book.CompleteTransition();}var d=s.domains.Single(x=>x.domainId==TerraformRules.DomainIds[index]);
            terraformTargetLevel=Math.Min(7,d.maxReachedLevel+1);
            var deep=TerraformRules.DeepFor(s,d.domainId);terraformDeep=Math.Max(0,Array.IndexOf(deep,d.activeDeepRecordId));
            terraformFusion=Math.Max(0,Array.FindIndex(TerraformRules.Fusions[index],f=>d.activeExtremeId==d.domainId+"_"+f));
            terraformSelectionReady=true;
        }
        private void ProposeTerraform(TerraformOperation operation,string domain=null,int level=0,string deep=null,string fusion=null,string value=null)
        {
            if(formalCampaign.HasPending || terraformRequest!=null)return;
            var s=TerraformPreview();
            bool warning=operation==TerraformOperation.SetLevel && level==6 && s.domains.Single(d=>d.domainId==domain).currentLevel!=6;
            terraformRequest=new FormalTerraformRequest("terraform."+Guid.NewGuid().ToString("N"),formalCampaign.Snapshot.revision,operation,domain,level,deep,fusion,value,warning);
            terraformWarning=warning;
            if(!warning)TerraformCommit();
        }
        private void TerraformCommit()
        {
            if(terraformRequest==null)return;
            try {
                var result=formalCampaign.CommitTerraform(terraformRequest,CollectionData(),formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign);
                if(result==GrowthCommitResult.SaveFailed){terraformMessage="保存できませんでした。同じ変更の保存を再試行してください。";return;}
                campaign=new CampaignState(WorldCatalog.ColossusIds,formalCampaign.Snapshot.world);
                terraformMessage="新天地を保存しました。";terraformRequest=null;terraformWarning=false;terraformSelectionReady=false;
            }catch(Exception e){terraformMessage="保存できませんでした："+e.Message;Debug.LogException(e);if(!formalCampaign.HasPending){terraformRequest=null;terraformWarning=false;}}
        }
        private void DrawTerraformExperience()
        {
            var s=TerraformPreview();int restored=Array.IndexOf(TerraformRules.DomainIds,(book.SubjectId??"").Replace("terraform.domain.",""));if(book.Bookmark==BookBookmark.NewWorld && restored>=0 && restored!=terraformDomain){terraformSelectionReady=false;terraformDomain=restored;}if(!terraformSelectionReady)SelectTerraformDomain(s,terraformDomain);
            string achievement=s.sevenExtremeGenesis?"七極創世":s.worldIntegrated?"新天地成立":"新天地を形成中";
            Label(130,175,860,48,(s.customWorldName??"新天地")+"　／　"+achievement,growthTextStyle);
            Label(1100,175,340,48,"TP "+s.totalTp.ToString("N0"),growthTitleStyle,gold);
            if(possibleWorlds)DrawPossibleWorlds(s);else DrawTerraformDomains(s);
            DrawTerraformConfirmation(s);
        }
        private void DrawTerraformDomains(TerraformSave s)
        {
            bool ready=terraformRequest==null && !formalCampaign.HasPending;
            for(int i=0;i<7;i++){
                var d=s.domains.Single(x=>x.domainId==TerraformRules.DomainIds[i]);var r=terraformDomainCards[i];
                if(GrowthButton(r.x,r.y,r.width,r.height,TerraformRules.DomainNames[i]+"\nLv"+d.currentLevel+" "+TerraformRules.LevelNames[d.currentLevel],ready,terraformDomain==i))SelectTerraformDomain(s,i);
            }
            DrawTerraformLandscape(new Rect(385,325,260,270),s,true);
            Label(385,605,260,70,"7領域の記述が形作る新天地\n"+(s.worldIntegrated?"成立した世界を再編集":"全領域Lv3でアステリアを受入"),growthSmallStyle);
            string id=TerraformRules.DomainIds[terraformDomain];var selected=s.domains.Single(x=>x.domainId==id);
            Label(975,240,460,42,TerraformRules.DomainNames[terraformDomain],growthTitleStyle,gold);
            Label(975,292,460,64,"現在 Lv"+selected.currentLevel+" "+TerraformRules.LevelNames[selected.currentLevel]+" ／ 最大 Lv"+selected.maxReachedLevel+(selected.activeExtremeId==null?"":"\n"+TerraformCatalog.ExtremeName(selected.activeExtremeId)),growthSmallStyle);
            Label(975,362,460,32,"設定するLv（到達済みは無料）",growthSmallStyle);
            for(int level=0;level<=7;level++){
                int row=level/4,col=level%4;
                if(GrowthButton(975+col*114,402+row*45,106,38,"Lv"+level,ready && level<=selected.maxReachedLevel+1,terraformTargetLevel==level))terraformTargetLevel=level;
            }
            var deeps=TerraformRules.DeepFor(s,id);terraformDeep=Math.Min(terraformDeep,Math.Max(0,deeps.Length-1));string deep=deeps.Length>0?deeps[terraformDeep]:null;
            if(terraformTargetLevel>=6){
                if(GrowthButton(975,487,455,38,deep==null?"深層記述：未取得":"深層："+TerraformCatalog.DeepName(deep),ready && deep!=null))terraformDeep=(terraformDeep+1)%deeps.Length;
            }
            string fusion=TerraformRules.Fusions[terraformDomain][terraformFusion];
            if(terraformTargetLevel==7 && GrowthButton(975,532,455,38,"融合："+TerraformCatalog.ExtremeName(id+"_"+fusion),ready))terraformFusion=(terraformFusion+1)%4;
            int target=terraformTargetLevel;string reason=TerraformRules.BlockReason(s,id,target,deep,fusion,true);
            int cost=TerraformRules.Cost(s,selected,target,fusion);
            string requirement=target==0?"未形成へ再設定":TerraformCatalog.RecordRequirement(s,id,target);
            Label(975,580,455,74,"Lv"+target+" "+TerraformRules.LevelNames[target]+"　／　"+cost.ToString("N0")+" TP\n"+requirement,growthSmallStyle);
            string[] unlocks=TerraformCatalog.UnlockPreview(id,target);
            string unlockText=unlocks.Length==0?"領域景観・環境タグ":string.Join("・",unlocks.Select(TerraformUnlockName));
            Label(135,695,760,44,"解放："+unlockText,growthSmallStyle);
            string prediction=target==7?TerraformCatalog.ExtremeName(id+"_"+fusion):TerraformRules.DomainNames[terraformDomain]+" "+TerraformRules.LevelNames[target];
            Label(135,747,760,40,"景観予測："+prediction+(target>=6 && deep!=null?" ＋ "+TerraformCatalog.DeepName(deep):""),growthSmallStyle);
            Label(975,662,455,65,reason??(target>=6?"居住環境の安全限界を超える再編集です。":"必要条件を達成しています。"),growthSmallStyle);
            string verb=target>selected.maxReachedLevel?"領域を発展する":target==7 && cost>0?"新しい極相を発見する":"環境を再設定する";
            if(GrowthButton(975,740,455,52,verb+" ／ "+cost+" TP",ready && reason==null))ProposeTerraform(TerraformOperation.SetLevel,id,target,deep,fusion);
        }
        private static string TerraformUnlockName(string id)=>id.StartsWith("garden.")?ProductionGardenCatalog.GardenName(id):ProductionGardenCatalog.FurnitureName(id);
        private string TerraformLandscape(TerraformSave s)=>string.Join(" ・ ",s.domains.Where(d=>d.currentLevel>0 && TerraformRules.DomainIds.Contains(d.domainId)).Select(d=>(d.activeExtremeId==null?TerraformCatalog.DomainName(d.domainId)+" "+TerraformRules.LevelNames[d.currentLevel]:TerraformCatalog.ExtremeName(d.activeExtremeId))+(d.activeDeepRecordId==null?"":"＋"+TerraformCatalog.DeepName(d.activeDeepRecordId))));
        private void DrawTerraformConfirmation(TerraformSave s)
        {
            if(terraformRequest==null){if(terraformMessage!=null)Label(135,795,1290,28,terraformMessage,growthSmallStyle);return;}
            GrowthFill(95,160,1410,660,new Color(.02f,.04f,.09f,.97f));
            if(terraformWarning && !formalCampaign.HasPending){
                Label(230,275,1140,62,s.overgrowthWarningAccepted?"過剰再生を再設定":"過剰再生 ― 深層記述の適用",growthTitleStyle,gold);
                Label(230,370,1140,220,"これ以降は居住環境としての安全な再生ではありません。\n過去世界の深層記述を新天地へ適用します。\n\n適用："+TerraformCatalog.DomainName(terraformRequest.DomainId)+" ＋ "+TerraformCatalog.DeepName(terraformRequest.DeepRecordId)+"\nLv5への安定化と到達済み環境への再設定は無料です。",growthTextStyle);
                if(GrowthButton(230,640,420,60,"取消")){terraformRequest=null;terraformWarning=false;}
                if(GrowthButton(720,640,650,60,"警告を確認して適用する"))TerraformCommit();
            }else{
                Label(230,300,1140,60,"新天地の変更を保存待ち",growthTitleStyle);
                Label(230,405,1140,160,terraformMessage??"",growthTextStyle);
                if(GrowthButton(230,640,1140,60,"同じ変更の保存を再試行"))TerraformCommit();
            }
        }
        private void DrawPossibleWorlds(TerraformSave s)
        {
            bool ready=terraformRequest==null && !formalCampaign.HasPending;
            string[] tabs={"極相 "+s.discoveredExtremes.Length+"/28","深層記述 "+s.acquiredDeepRecords.Count(TerraformRules.DeepIds.Contains)+"/7","世界現象","星名"};
            for(int i=0;i<4;i++)if(GrowthButton(135+i*325,240,310,44,tabs[i],ready,possibleWorldTab==i)){possibleWorldTab=i;book.PageState.sortMode=i.ToString();bookNavigationDirty=true;terraformScroll=Vector2.zero;}
            if(possibleWorldTab==3){
                Label(180,330,1200,60,s.sevenExtremeGenesis?"七極創世":"星名",growthTitleStyle);
                Label(180,395,1200,54,s.sevenExtremeGenesis?"過去7世界のいずれにも該当しない世界が成立した":"全7領域の最大到達Lv7で、星名入力が解放されます。",growthSmallStyle);
                if(worldNameInput==null || worldNameOwner!=s.customWorldName){worldNameInput=s.customWorldName??"";worldNameOwner=s.customWorldName;}
                bool old=GUI.enabled;GUI.enabled=old && ready && s.sevenExtremeGenesis;worldNameInput=GUI.TextField(new Rect(180,465,1200,64),worldNameInput,40);GUI.enabled=old;
                Label(180,555,1200,60,"40文字以内。後から変更可能です。空欄で既定の「新天地」に戻ります。",growthSmallStyle);
                if(GrowthButton(180,650,1200,60,"星名を保存",ready && s.sevenExtremeGenesis))ProposeTerraform(TerraformOperation.RenameWorld,value:worldNameInput);
                return;
            }
            float contentHeight=possibleWorldTab==0?1120:possibleWorldTab==1?1010:720;
            terraformScroll=GUI.BeginScrollView(new Rect(135,305,1300,438),terraformScroll,new Rect(0,0,1260,contentHeight));
            try {
                int y=0;
                if(possibleWorldTab==0){
                    foreach(var definition in TerraformCatalog.Extremes){
                        var e=s.discoveredExtremes.FirstOrDefault(x=>x.extremeId==definition.id);
                        Label(12,y,855,34,definition.displayName+"　／　"+TerraformCatalog.DomainName(definition.mainDomainId)+" ＋ "+TerraformCatalog.DomainName(definition.fusionDomainId),growthSmallStyle);
                        Label(885,y,370,34,e==null?"未発見":"発見順 "+e.sequence+" ／ "+TerraformCatalog.DeepName(e.deepRecordId),growthSmallStyle);y+=39;
                    }
                }else if(possibleWorldTab==1){
                    var snapshot=formalCampaign.Snapshot;
                    foreach(var deep in TerraformCatalog.DeepRecords){
                        bool owned=s.acquiredDeepRecords.Contains(deep.id),used=s.usedDeepRecords.Contains(deep.id);
                        Label(12,y,1235,40,deep.worldLineId+"　"+deep.displayName+"　"+(owned?used?"取得・使用済み":"取得済み":"未取得"),growthTextStyle,gold);y+=46;
                        foreach(var colossus in WorldCatalog.Colossi.Where(c=>c.WorldLineId==deep.worldLineId)){
                            var record=s.acquiredWorldRecords.FirstOrDefault(r=>r.colossusId==colossus.Id);var owner=CollectionData().owners.Single(o=>o.id==colossus.Id);
                            int read=owner.chapterIds.Count(snapshot.world.readStoryIds.Contains);
                            Label(32,y,1200,33,colossus.DisplayName+"　最高討伐Lv "+(record?.highestLevel??0)+"/50　／　章読了 "+read+"/3",growthSmallStyle);y+=34;
                        }
                        y+=22;
                    }
                }else{
                    Label(12,0,1200,52,"七極創世後に永久解放。選択・解除は無料。ホーム・箱庭の演出だけを変更します。",growthSmallStyle);y=68;
                    for(int i=0;i<TerraformRules.Phenomena.Length;i++){
                        string id=TerraformRules.Phenomena[i];bool owned=s.unlockedWorldPhenomena.Contains(id),active=s.activeWorldPhenomenonId==id;
                        Label(12,y,650,46,TerraformRules.PhenomenonNames[i]+(owned?active?"　演出中":"　解放済み":"　"+TerraformRules.PhenomenonCosts[i].ToString("N0")+" TP"),growthTextStyle);
                        if(GrowthButton(735,y,485,45,owned?active?"演出を解除":"この演出を選ぶ":"永久解放する",ready && (owned || s.sevenExtremeGenesis && s.totalTp>=TerraformRules.PhenomenonCosts[i])))ProposeTerraform(owned?TerraformOperation.SelectPhenomenon:TerraformOperation.UnlockPhenomenon,value:active?null:id);y+=60;
                    }
                }
            }finally{GUI.EndScrollView();}
            Label(135,755,1300,42,"最大到達："+string.Join(" ／ ",TerraformRules.DomainIds.Select(id=>TerraformCatalog.DomainName(id)+" Lv"+s.domains.Single(d=>d.domainId==id).maxReachedLevel)),growthSmallStyle);
        }
    }
}
