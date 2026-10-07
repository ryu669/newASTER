using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private int terraformDomain,terraformDeep,terraformFusion;
        private bool terraformWarning,possibleWorlds;
        private string terraformMessage,worldNameInput="";
        private Vector2 terraformScroll;
        private void TerraformCommit(Action<TerraformSave> action)
        {
            try {
                var world=formalCampaign.Snapshot.world;var s=TerraformRules.Migrate(world);
                SeedTerraformHistory(s);TerraformRules.RefreshDeep(s,world.readStoryIds,CollectionData());action(s);
                if(!formalCampaign.CommitWorld(world,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign)){terraformMessage="保存できませんでした。操作を再試行してください。";return;}
                campaign=new CampaignState(WorldCatalog.ColossusIds,formalCampaign.Snapshot.world);terraformMessage="新天地を保存しました。";terraformWarning=false;
            }catch(Exception e){terraformMessage=e.Message;}
        }
        private void SeedTerraformHistory(TerraformSave s)
        {
            var receipts=formalCampaign.Snapshot.collection?.receipts;
            if(receipts==null)return;
            foreach(var group in receipts.Where(r=>r.reason==BattleEndReason.Victory).GroupBy(r=>r.battle.colossusId)){
                int highest=group.Max(r=>r.battle.level);var record=s.acquiredWorldRecords.SingleOrDefault(r=>r.colossusId==group.Key);
                if(record==null)s.acquiredWorldRecords=s.acquiredWorldRecords.Concat(new[]{new TerraformRecord {colossusId=group.Key,highestLevel=highest,tier=Math.Min(5,highest/10+1)}}).ToArray();
                else {record.highestLevel=Math.Max(record.highestLevel,highest);record.tier=Math.Max(record.tier,Math.Min(5,highest/10+1));}
                if(TerraformRules.Index(group.Key)==14)s.worldIntegrated=true;
            }
        }
        private void DrawTerraformExperience()
        {
            var world=formalCampaign.Snapshot.world;var s=TerraformRules.Migrate(world);SeedTerraformHistory(s);TerraformRules.RefreshDeep(s,world.readStoryIds,CollectionData());
            Label(150,195,850,50,(s.customWorldName??"新天地")+"　TP "+s.totalTp+(s.sevenExtremeGenesis?"　七極創世":""),growthTitleStyle);
            if(GrowthButton(1110,195,320,45,possibleWorlds?"新天地へ":"可能世界"))possibleWorlds=!possibleWorlds;
            if(possibleWorlds){DrawPossibleWorlds(s);return;}
            for(int i=0;i<7;i++){
                var d=s.domains.Single(x=>x.domainId==TerraformRules.DomainIds[i]);
                if(GrowthButton(150,260+i*64,330,56,TerraformRules.DomainNames[i]+" Lv"+d.currentLevel+" "+TerraformRules.LevelNames[d.currentLevel],true,terraformDomain==i)){terraformDomain=i;terraformDeep=terraformFusion=0;terraformWarning=false;}
            }
            string id=TerraformRules.DomainIds[terraformDomain];var selected=s.domains.Single(x=>x.domainId==id);
            var deeps=TerraformRules.DeepFor(s,id);terraformDeep=Math.Min(terraformDeep,Math.Max(0,deeps.Length-1));string deep=deeps.Length>0?deeps[terraformDeep]:null;
            string fusion=TerraformRules.Fusions[terraformDomain][terraformFusion];
            DrawTerraformLandscape(new Rect(530,255,900,70),s,true);
            Label(530,260,880,65,"現在の景観："+TerraformLandscape(s),growthSmallStyle);
            Label(530,330,880,65,"現在 Lv"+selected.currentLevel+" ／ 最大到達 Lv"+selected.maxReachedLevel+"\n文明圏は取得した記述を再編集して構築します。",growthSmallStyle);
            if(deep!=null && GrowthButton(530,405,420,44,"深層："+TerraformRules.DeepNames[Array.IndexOf(TerraformRules.DeepIds,deep)]))terraformDeep=(terraformDeep+1)%deeps.Length;
            if(GrowthButton(980,405,450,44,"融合："+TerraformRules.ExtremeNames[terraformDomain][terraformFusion]))terraformFusion=(terraformFusion+1)%4;
            int next=Math.Min(7,selected.maxReachedLevel+1);string reason=TerraformRules.BlockReason(s,id,next,deep,fusion,true);
            Label(530,470,900,100,"次 Lv"+next+" "+TerraformRules.LevelNames[next]+" ／ TP "+TerraformRules.Cost(s,selected,next,fusion)+"\n"+(reason??"記述条件を達成。景観・領域タグを解放します。")+"\n景観予測："+(next==7?TerraformRules.ExtremeNames[terraformDomain][terraformFusion]:TerraformRules.DomainNames[terraformDomain]+" "+TerraformRules.LevelNames[next]),growthSmallStyle);
            if(terraformWarning){
                Label(530,560,900,100,"過剰再生\nこれ以降は居住環境としての安全な再生ではありません。\n過去世界の深層記述を新天地へ適用します。",growthSmallStyle);
                if(GrowthButton(530,670,550,48,"警告を確認して適用"))TerraformCommit(t=>TerraformRules.SetLevel(t,id,6,deep,null,true));
                if(GrowthButton(1100,670,330,48,"取消"))terraformWarning=false;
            }else {
                if(GrowthButton(530,570,900,48,"領域を発展 / 極相を再構築",reason==null && !formalCampaign.HasPending)){
                    if(next==6)terraformWarning=true;else TerraformCommit(t=>TerraformRules.SetLevel(t,id,next,deep,fusion,true));
                }
                if(GrowthButton(530,635,430,44,"Lv5へ無料安定化",selected.maxReachedLevel>=5))TerraformCommit(t=>TerraformRules.SetLevel(t,id,5));
                if(GrowthButton(980,635,450,44,"Lv6へ無料再設定",selected.maxReachedLevel>=6 && deep!=null))terraformWarning=true;
            }
            Label(530,725,900,70,terraformMessage??"Lv6/7は居住環境の上位互換ではありません。到達記録を保ったまま安定化できます。",growthSmallStyle);
        }
        private string TerraformLandscape(TerraformSave s)=>string.Join(" ・ ",s.domains.Where(d=>d.currentLevel>0 && TerraformRules.DomainIds.Contains(d.domainId)).Select(d=>{
            int i=Array.IndexOf(TerraformRules.DomainIds,d.domainId);string name=d.activeExtremeId==null?TerraformRules.DomainNames[i]+" "+TerraformRules.LevelNames[d.currentLevel]:TerraformRules.Fusions[i].Select((f,j)=>new {Id=d.domainId+"_"+f,Name=TerraformRules.ExtremeNames[i][j]}).Where(e=>e.Id==d.activeExtremeId).Select(e=>e.Name).FirstOrDefault()??d.activeExtremeId;
            return name+(d.activeDeepRecordId==null?"":"＋"+(Array.IndexOf(TerraformRules.DeepIds,d.activeDeepRecordId)<0?d.activeDeepRecordId:TerraformRules.DeepNames[Array.IndexOf(TerraformRules.DeepIds,d.activeDeepRecordId)]));
        }));
        private void DrawPossibleWorlds(TerraformSave s)
        {
            terraformScroll=GUI.BeginScrollView(new Rect(150,260,1280,450),terraformScroll,new Rect(0,0,1220,1750));
            int y=0;for(int i=0;i<7;i++)for(int j=0;j<4;j++){
                string id=TerraformRules.DomainIds[i]+"_"+TerraformRules.Fusions[i][j];var e=s.discoveredExtremes.SingleOrDefault(x=>x.extremeId==id);
                Label(10,y,1200,40,TerraformRules.ExtremeNames[i][j]+(e==null?"　未発見":"　発見順 "+e.sequence+" ／ "+e.deepRecordId),growthSmallStyle);y+=40;
            }
            Label(10,y,1200,65,"使用済み深層："+string.Join("・",s.usedDeepRecords)+"\n最大到達："+string.Join(" / ",s.domains.Select(d=>d.domainId+" "+d.maxReachedLevel)),growthSmallStyle);y+=75;
            for(int i=0;i<TerraformRules.Phenomena.Length;i++){
                string id=TerraformRules.Phenomena[i];bool owned=s.unlockedWorldPhenomena.Contains(id);
                if(GrowthButton(10,y,1200,40,TerraformRules.PhenomenonNames[i]+(owned?(s.activeWorldPhenomenonId==id?"　演出中（解除）":"　解放済み（演出する）"):"　"+TerraformRules.PhenomenonCosts[i]+" TP"),owned || s.sevenExtremeGenesis && s.totalTp>=TerraformRules.PhenomenonCosts[i]))TerraformCommit(t=>{if(owned)t.activeWorldPhenomenonId=t.activeWorldPhenomenonId==id?null:id;else TerraformRules.UnlockPhenomenon(t,id);});y+=44;
            }
            GUI.EndScrollView();
            if(s.sevenExtremeGenesis){worldNameInput=GUI.TextField(new Rect(150,730,900,45),worldNameInput,40);if(GrowthButton(1080,730,350,45,"星名を保存"))TerraformCommit(t=>t.customWorldName=string.IsNullOrWhiteSpace(worldNameInput)?null:worldNameInput.Trim());}
            else Label(150,730,1200,55,"7領域すべての最大到達Lv7で七極創世。星名と世界現象を解放します。",growthSmallStyle);
        }
    }
}
