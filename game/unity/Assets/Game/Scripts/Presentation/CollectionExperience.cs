using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool collectionOpen;
        private int collectionOwner,collectionRelicPage,collectionTab,resultTab;
        private Vector2 resultScroll;
        private FormalRelicRequest relicRequest;
        private string relicError;
        private string relicComparisonKey,relicComparisonText;
        private CollectionReceipt lastCollectionResult;
        private int AvailableCollectionMaterials=>formalCampaign?.Snapshot.collection?.materials.Sum(m=>m.amount)??campaign.Progress.Materials;
        private CollectionCatalog CollectionData()=>collectionCatalog??(collectionCatalog=SelectCollectionCatalog());
        private string CollectionOwnerName(CollectionOwnerDef owner)=>owner.kind=="colossus"?(campaign.ColossusUnlocks.IsUnlocked(owner.id)?WorldCatalog.Colossi.Single(c=>c.Id==owner.id).DisplayName:"未解放の巨神獣"):combatDefinitions.Hero(owner.id).name;
        private void CollectionBack(){if(formalCampaign.HasPending)return;if(terraformRequest!=null){terraformRequest=null;terraformWarning=false;return;}if(relicRequest!=null){relicRequest=null;relicError=null;return;}trialPoemChapter=null;collectionOpen=false;}
        private void RelicSelect(CollectionRelic relic,RelicOperation operation,string heroineId=null){relicRequest=new FormalRelicRequest("relic."+Guid.NewGuid().ToString("N"),relic.id,heroineId,formalCampaign.Snapshot.revision,operation,CollectionData().contentVersion);relicError=null;}
        private void RelicCommit()
        {
            try{
                var outcome=formalCampaign.CommitRelic(relicRequest,CollectionData(),formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign);
                if(outcome==GrowthCommitResult.SaveFailed){relicError="保存待ちです。同じ強化・装備内容で再試行します。";return;}
                formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);relicRequest=null;relicError="保存しました。能力は次の出撃から反映されます。";
            }catch(Exception e){relicError=e is ArgumentException?"素材数・80%条件・Lv上限・装備先を確認してください。":"保存できません。同じ内容で再試行してください。";Debug.LogException(e);}
        }
        private string RelicUnavailable(CollectionRelic relic,RelicOperation operation)
        {
            if(operation==RelicOperation.Equip || operation==RelicOperation.Unequip)return null;
            if(operation==RelicOperation.LevelUp && relic.level>=120)return "Lv上限120に到達しています。";
            int value=operation==RelicOperation.AttackUp?relic.attackRoll:relic.hpRoll,maximum=operation==RelicOperation.AttackUp?100:1000;
            if(operation!=RelicOperation.LevelUp && value>=maximum)return "抽選値が上限に到達しています。";
            if(operation!=RelicOperation.LevelUp && !FormalRelicRules.DirectEligible(value,maximum))return "直接強化には、この項目が上限の80%以上必要です。";
            int cost=FormalRelicRules.UpgradeCost(relic,operation,CollectionData().contentVersion);
            var def=CollectionData().relics.Single(d=>d.id==relic.id);
            return FormalRelicRules.MaterialBalance(formalCampaign.Snapshot.collection,def)<cost?"素材が不足しています。必要数：各 "+cost+"。":null;
        }
        private void DrawCollectionExperience()
        {
            GrowthStyles();PalaceBackdrop("crown");GrowthFrame(90,80,1420,745);
            DrawBookFooter();
            if(collectionTab!=0){DrawStandaloneItems();return;}
            if(plan8StoryTrial && trialPoemChapter!=null){DrawTrialPoemConditions();return;}
            if(ProductionStoryActive && trialPoemChapter!=null){DrawProductionPoemConditions();return;}
            if(relicRequest!=null){DrawRelicConfirmation();return;}
            if(collectionTab==0){
                var owners=CollectionData().owners;collectionOwner=Math.Max(0,Math.Min(collectionOwner,owners.Length-1));var o=owners[collectionOwner];
                Label(340,283,920,55,CollectionOwnerName(o),growthTitleStyle);
                var world=formalCampaign.Snapshot.world;
                for(int i=0;i<3;i++){
                    var c=CollectionData().chapters.Single(x=>x.id==o.chapterIds[i]);int count=c.poemIds.Count(world.poemIds.Contains);
                    GrowthLine(150,355+i*110,1430,355+i*110,gold);
                    bool authored=ProductionStoryActive || plan8StoryTrial && HasTrialText(c.id),open=world.unlockedStoryIds.Contains(c.id),read=world.readStoryIds.Contains(c.id);
                    bool ownerOpen=o.kind=="heroine" || campaign.ColossusUnlocks.IsUnlocked(o.id);
                    string chapterName=ProductionStoryActive?(ownerOpen?ProductionStoryTitle(c.id):"未解放の章"):"第"+(i+1)+"章";
                    Label(180,375+i*110,authored?810:1200,45,$"{chapterName}   詩 {count}/{c.poemIds.Length}   {(open?read?"読了":"章の条件達成":"未解放")}",growthTextStyle);
                    Label(180,420+i*110,authored?810:1200,36,ProductionStoryActive?"この章の詩をそろえると読めます。":authored?"条件：この章の詩をそろえる。オリジナル試遊本文。":o.kind=="colossus"?"条件：この巨神獣の詩を8つそろえる。本文は未制作です。":"条件：開始編成に参加し、対応する巨神獣の歌を聞く。本文未制作。",growthSmallStyle);
                    if(authored){
                        if(GrowthButton(1015,405+i*110,180,44,"条件・詩対応",ownerOpen)){trialPoemChapter=c.id;trialPoemScroll=Vector2.zero;}
                        if(GrowthButton(1230,405+i*110,200,44,read?"回想":"読む／再開",open))BeginAdv(c.id,read);
                    }
                }
                DrawBookTransition(true);
            }else if(collectionTab==1)DrawModernRelics();else DrawMaterialInventory();
        }
        private void DrawRelicConfirmation()
        {
            var r=formalCampaign.Snapshot.collection.relics.Single(x=>x.id==relicRequest.RelicId);Label(170,290,1260,65,"オーパーツの変更を確認",growthTitleStyle);string info;
            if(relicRequest.Operation==RelicOperation.LevelUp)info=$"Lv {r.level} → {Math.Min(120,r.level+1)}\n攻撃固定成長 +2 ／ HP固定成長 +5\n対応する巨神獣素材 {FormalRelicRules.UpgradeCost(r,RelicOperation.LevelUp,CollectionData().contentVersion)} を消費します。";
            else if(relicRequest.Operation==RelicOperation.AttackUp)info=$"攻撃抽選値 {r.attackRoll} → {Math.Min(100,r.attackRoll+1)} / 100\n80%以上の項目だけ直接強化できます。\n対応する巨神獣素材 {FormalRelicRules.UpgradeCost(r,relicRequest.Operation,CollectionData().contentVersion)} を消費します。";
            else if(relicRequest.Operation==RelicOperation.HpUp)info=$"HP抽選値 {r.hpRoll} → {Math.Min(1000,r.hpRoll+10)} / 1000\n80%以上の項目だけ直接強化できます。\n対応する巨神獣素材 {FormalRelicRules.UpgradeCost(r,relicRequest.Operation,CollectionData().contentVersion)} を消費します。";
            else info=RelicEquipmentComparison(r,relicRequest);
            Label(170,375,1260,175,info,growthTextStyle);
            var ownedHeroes=combatDefinitions.HeroineIds.Where(id=>formalProgression.Snapshot.heroines.Any(h=>h.heroineId==id)).ToArray();
            var previousButtonStyle=growthButtonStyle;growthButtonStyle=new GUIStyle(previousButtonStyle){fontSize=16,wordWrap=false};
            if(relicRequest.Operation==RelicOperation.Equip && !formalCampaign.HasPending)for(int i=0;i<ownedHeroes.Length;i++){
                var id=ownedHeroes[i];if(GrowthButton(170+(i%5)*245,553+(i/5)*45,230,40,combatDefinitions.Hero(id).name,true,id==relicRequest.HeroineId))relicRequest=new FormalRelicRequest(relicRequest.Id,relicRequest.RelicId,id,relicRequest.Revision,relicRequest.Operation,relicRequest.ContentVersion);
            }
            string unavailable=RelicUnavailable(r,relicRequest.Operation);
            if(relicError!=null || unavailable!=null)Label(170,689,1260,36,relicError??unavailable,growthSmallStyle);
            growthButtonStyle=previousButtonStyle;
            if(GrowthButton(170,740,580,48,"取消",!formalCampaign.HasPending))CollectionBack();
            if(GrowthButton(790,740,640,48,formalCampaign.HasPending?"同じ変更を保存する":"変更して保存する",formalCampaign.HasPending || unavailable==null,true))RelicCommit();
        }
        private string RelicEquipmentComparison(CollectionRelic relic,FormalRelicRequest request)
        {
            string key=request.Signature+"|"+request.Revision;
            if(key==relicComparisonKey)return relicComparisonText;
            var snapshot=formalCampaign.Snapshot;var current=FormalRelicRules.Equipped(snapshot.collection,request.HeroineId);
            var next=JsonUtility.FromJson<FormalCollectionLedger>(JsonUtility.ToJson(snapshot.collection));next.equipment=next.equipment.Where(e=>e.heroineId!=request.HeroineId && e.relicId!=request.RelicId).ToArray();
            if(request.Operation==RelicOperation.Equip)next.equipment=next.equipment.Concat(new[]{new CollectionEquipment{heroineId=request.HeroineId,relicId=request.RelicId}}).ToArray();
            var before=new PlayableBattle(1,campaign.Playable,1,combatDefinitions:combatDefinitions.WithFormation(PreviewFormation(request.HeroineId)),formalGrowth:snapshot.growth,collectionGrowth:snapshot.collection,homeProgress:snapshot.home,homeCatalog:HomeData(),relicCatalog:CollectionData());
            var after=new PlayableBattle(1,campaign.Playable,1,combatDefinitions:combatDefinitions.WithFormation(PreviewFormation(request.HeroineId)),formalGrowth:snapshot.growth,collectionGrowth:next,homeProgress:snapshot.home,homeCatalog:HomeData(),relicCatalog:CollectionData());
            int index=Array.IndexOf(PreviewFormation(request.HeroineId),request.HeroineId);var a=before.State.Heroes[index];var b=after.State.Heroes[index];
            string equipped=current==null?"未装備":$"Lv{current.level} ／ HP +{FormalRelicRules.Hp(current)}・攻撃 +{FormalRelicRules.Attack(current)}";
            relicComparisonKey=key;return relicComparisonText=$"装備先：{combatDefinitions.Hero(request.HeroineId).name} ／ 現在 {equipped}\n出撃時HP {a.MaxHitPoints} → {b.MaxHitPoints} ／ 攻撃 {a.Attack} → {b.Attack}\n両防御 {a.PhysicalDefense}/{a.MagicDefense} → {b.PhysicalDefense}/{b.MagicDefense} ／ 速度 {a.Speed} → {b.Speed}\n{(request.Operation==RelicOperation.Equip?NewAster.Data.ProductionEconomyCatalog.RelicAbility(CollectionData().relics.Single(d=>d.id==relic.id)):"オーパーツの能力を外します。")}\nチェイン率は変化しません。保存後の次の出撃から反映します。";
        }
        private string ResultDetail()
        {
            if(lastCollectionResult==null || resultTab==0)return result;var r=lastCollectionResult;
            if(resultTab==1 && ProductionStoryActive){
                var poems=ProductionStoryData().chapters.SelectMany(c=>c.poems).ToDictionary(p=>p.id);
                return $"聞いた巨神獣の詩 {r.battle.heardPoemIds.Length} ／ 新規 {r.acquiredPoemIds.Length}\n"+string.Join("\n",r.acquiredPoemIds.Select(id=>CollectionData().poems.Single(p=>p.id==id)).GroupBy(p=>p.ownerId).Select(g=>CollectionOwnerName(CollectionData().owners.Single(o=>o.id==g.Key))+"\n"+string.Join("\n",g.Select(p=>"　「"+poems[p.id].text+"」"))))+"\n新しい章 "+r.unlockedChapterIds.Length+"\n"+string.Join("\n",r.unlockedChapterIds.Select(ProductionStoryTitle));
            }
            if(resultTab==1)return $"聞いた巨神獣の詩 {r.battle.heardPoemIds.Length} ／ 新規 {r.acquiredPoemIds.Length}\n"+string.Join("\n",r.acquiredPoemIds.Select(id=>CollectionData().poems.Single(p=>p.id==id)).GroupBy(p=>p.ownerId).Select(g=>CollectionOwnerName(CollectionData().owners.Single(o=>o.id==g.Key))+"："+g.Count()+"詩"))+$"\n新しい章 {r.unlockedChapterIds.Length} ／ "+(plan8StoryTrial?"8章のオリジナル試遊本文":"本文未制作");
            if(resultTab==2){
                if(r.reason!=BattleEndReason.Victory)return "敗北・撤退ではTP・素材・初回解放は発生しません。\n聞いた詩と対応する人物詩・章を保存しました。";
                var source=WorldCatalog.Colossi.Single(c=>c.Id==r.battle.colossusId);var band=CollectionData().rewardBands.Single(b=>b.ownerId==source.Id && r.battle.level>=b.minLevel && r.battle.level<=b.maxLevel);
                var saved=formalCampaign.Snapshot;int amount=saved.collection.materials.Where(m=>m.sourceColossusId==source.Id).Sum(m=>m.amount);
                return $"記憶元：{source.WorldLineId??"世界統合"} ／ 環境：{string.Join("・",source.EnvironmentTags)}\nTP +{r.terraformingTp} ／ 保存後所持 {saved.world.terraform.totalTp}\n巨神獣別素材 +{10+r.battle.level} ／ 保存後所持 {amount}\nページ・環境・庭の初回解放は一度だけ。再戦でもTPと素材を得られます。";
            }
            return $"レリックハント 抽選 {r.relicDrawCount}回 ／ 獲得 {r.relicDrops.Length}個\n"+(r.relicDrops.Length==0?"今回の遺物獲得はありません。":string.Join("\n",r.relicDrops.Select(x=>$"攻撃 {x.attackRoll}/100 ／ HP {x.hpRoll}/1000")))+"\n同名は項目ごとの高値を保持。提供率 "+(r.battle.contentVersion==CollectionCatalog.ProductionVersion?(r.battle.relicHunt?"35%":"5%"):"75%")+"。";
        }
        private void PrepareCollectionCapture(string[] args)
        {
            if(!formalDiagnostic)throw new InvalidOperationException("Collection diagnostics must not access player saves.");
            selectedLevel=50;StartBattle(WorldCatalog.ColossusIds[0]);
            encounter=new PlayableBattle(50,campaign.Playable,8,combatDefinitions:combatDefinitions,formalGrowth:formalProgression.Snapshot,colossusDefinition:ColossusCombatCatalog.Get(activeColossus));StartCollection();
            // Diagnostic fixture only; normal play records completed enemy commands.
            foreach(var id in collectionCatalog.owners[0].poemIds.Take(8))collectionSession.RecordCompletedSinging(id);
            var reason=args.Contains("-collectionDefeat")?BattleEndReason.Defeat:args.Contains("-collectionRetreat")?BattleEndReason.Retreat:BattleEndReason.Victory;
            if(reason==BattleEndReason.Victory)encounter.State.ApplyBossDamage(int.MaxValue);
            else if(reason==BattleEndReason.Defeat)foreach(var hero in encounter.State.Heroes)hero.TakeDamage(int.MaxValue);
            formalVictoryDiagnosticFailure=args.Contains("-collectionPending") || args.Contains("-collectionRetry");string before=JsonUtility.ToJson(formalCampaign.Snapshot);
            PrepareFormalBattleEnd(reason);
            if(args.Contains("-collectionRetry")){
                if(!formalCampaign.HasPending || JsonUtility.ToJson(formalCampaign.Snapshot)!=before)throw new Exception("Collection pending was not atomic.");
                var request=formalBattleEndRequest;formalVictoryDiagnosticFailure=false;PersistFormalVictory();
                if(formalCampaign.HasPending || formalBattleEndRequest!=null || lastCollectionResult.acquiredPoemIds.Length!=48)throw new Exception("Collection retry failed.");
                if(formalCampaign.CommitBattleEnd(request,null,null,s=>throw new Exception("duplicate"))!=GrowthCommitResult.AlreadyCommitted)throw new Exception("Collection replay failed.");
                Debug.Log("COLLECTION_END_NAVIGATION_PASS 4 assertions reason="+reason);
            }
            resultTab=args.Contains("-collectionPoems")?1:args.Contains("-collectionWorld")?2:args.Contains("-collectionDrops")?3:0;
            if(args.Contains("-collectionMany")) {
                var many=formalCampaign.Snapshot;many.collection.relics=CollectionData().relics.Select((d,i)=>new CollectionRelic{id=d.id,level=1+i,attackRoll=i*7,hpRoll=i*70}).ToArray();
                many.collection.materials=CollectionData().owners.Where(o=>o.kind=="colossus").Select(o=>new CollectionMaterial{id=o.materialIds[0],sourceColossusId=o.id,amount=60}).ToArray();BindFormalCampaign(many);
                collectionRelicPage=4;Debug.Log("PLAN5_MANY_RELICS_PASS 15 items, five pages, last page bound");
            }
            if(args.Contains("-collectionChapters") || args.Contains("-collectionInventory") || args.Contains("-collectionConfirm")){
                result=null;encounter=null;collectionOpen=true;collectionTab=args.Contains("-collectionChapters")?0:1;
                if(args.Contains("-collectionConfirm")){
                    var relic=formalCampaign.Snapshot.collection.relics.First();string prior=JsonUtility.ToJson(formalCampaign.Snapshot);RelicSelect(relic,RelicOperation.LevelUp);CollectionBack();
                    if(relicRequest!=null || JsonUtility.ToJson(formalCampaign.Snapshot)!=prior)throw new Exception("Relic cancel changed saved state.");
                    RelicSelect(relic,RelicOperation.LevelUp);Debug.Log("COLLECTION_RELIC_CANCEL_PASS 2 assertions");
                }
            }
        }
    }
}
