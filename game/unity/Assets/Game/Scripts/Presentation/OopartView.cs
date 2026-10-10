using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool oopartPanel;private string selectedOopart,oopartError,oopartMessage;private int oopartSlot,oopartPresetIndex;private Vector2 oopartScroll;private OopartRequest oopartRequest;
        private bool OopartModalVisible=>oopartPanel || oopartRequest!=null;
        private string StatName(string key)=>key=="hp"?"HP":key=="attack"?"攻撃":key=="physical-defense"?"物理防御":key=="magic-defense"?"魔法防御":"速度";
        private string StatText(StatValues v)=>string.Join(" ／ ",StatValues.Names.Select(k=>StatName(k)+" +"+v.Get(k)));
        private FormalCampaignSave OopartSnapshot(){var s=LifeSnapshot();OopartSaveAdapter.Migrate(s.collection,CollectionData(),CurrentFormation());s.collection.ooparts?.SyncFormation(CurrentFormation());return s;}
        private void OpenOoparts(int slot,string id=null){if(!BookInputAllowed || encounter!=null)return;oopartSlot=slot;oopartPanel=true;oopartError=null;oopartMessage=null;oopartScroll=Vector2.zero;selectedOopart=id??OopartSnapshot().collection.ooparts.slots[slot].equippedOopartId??OopartSnapshot().collection.ooparts.progress.FirstOrDefault()?.oopartId;}
        private void ProposeOopart(string kind,string target=null,int count=1,string stat=null){if(oopartRequest!=null || formalCampaign.HasPending || encounter!=null)return;oopartRequest=new OopartRequest(Guid.NewGuid().ToString("N"),formalCampaign.Revision,kind,target,oopartSlot,count,stat);oopartError=null;}
        private void CommitOopart()
        {
            try{var request=oopartRequest;var result=formalCampaign.CommitOopart(request,CollectionData(),combatDefinitions.FormationIds,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign,HomeData());if(result==GrowthCommitResult.SaveFailed){oopartError="保存できませんでした。同じ内容で再試行してください。";return;}oopartRequest=null;oopartError=null;oopartMessage="保存済み";if(book.Bookmark==BookBookmark.Formation && (request.Kind=="equip" || request.Kind=="clear-slot")){oopartPanel=false;formationLayer=0;}formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);lifeSnapshotCached=null;}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is System.IO.IOException){oopartError=e.Message;if(!formalCampaign.HasPending)oopartRequest=null;}
        }
        private bool CloseOoparts(){if(oopartRequest!=null){if(!formalCampaign.HasPending)oopartRequest=null;return true;}if(!oopartPanel)return false;oopartPanel=false;return true;}
        private void DrawOopartInventoryPage()
        {
            var s=OopartSnapshot();var o=s.collection.ooparts;int pages=Math.Max(1,(o.progress.Length+3)/4);collectionRelicPage=Mathf.Clamp(collectionRelicPage,0,pages-1);
            if(o.progress.Length==0)Label(190,300,1200,90,"未所持",growthTextStyle);
            foreach(var pair in o.progress.Skip(collectionRelicPage*4).Take(4).Select((p,i)=>new{p,i})){
                var d=CollectionData().Oopart(pair.p.oopartId);float y=220+pair.i*130;GrowthFrame(150,y,1280,120);DrawBookEmblem(new Rect(168,y+15,65,65),6);Label(249,y+10,750,35,d.name+" ／ Lv"+pair.p.level,growthTextStyle);Label(249,y+46,750,30,StatText(OopartService.Fixed(pair.p,d)),new GUIStyle(growthSmallStyle){fontSize=16});
                int place=Array.FindIndex(o.slots,x=>x.equippedOopartId==d.id);Label(249,y+82,750,27,place<0?"未装備":"第"+(place+1)+"枠",growthSmallStyle);
                if(GrowthButton(1025,y+28,380,60,"詳細",BookInputAllowed))OpenOoparts(place<0?formationSlot:place,d.id);
            }
            if(GrowthButton(150,753,190,40,"‹",collectionRelicPage>0))collectionRelicPage--;Label(365,757,160,32,(collectionRelicPage+1)+" / "+pages,growthSmallStyle);if(GrowthButton(555,753,190,40,"›",collectionRelicPage+1<pages))collectionRelicPage++;
        }
        private string EffectText(OopartEffectDef e)
        {
            string target=e.targetId=="all"?"全能力":e.targetId=="defense"?"両防御":StatValues.Names.Contains(e.targetId)?StatName(e.targetId):e.targetId=="slot.1"?"スキル2":e.targetId;
            string type=e.effectType=="stat-percent"?target:e.effectType=="attribute"?target+"属性ダメージ":e.effectType=="skill-power"?target+"威力":e.effectType=="cast-percent"?"詠唱WT短縮":e.effectType=="wt"?"行動WT短縮":e.effectType=="gauge"?"ゲージ獲得":e.effectType=="part"?"部位ダメージ":e.effectType=="status"?EnemyStatusState.Label(target)+"付与値":e.effectType=="buff-power"?"自身が発動したバフ":"ツール最大回数";
            string scale=e.scalingType=="turn-growth"?" ／ 毎ターン +"+e.scalingValue+"%、最大"+e.maxValue+"%":e.scalingType=="turn-decay"?" ／ 毎ターン −"+e.scalingValue+"ポイント":e.scalingType=="hp-missing"?" ／ 減少HPに応じ最大 +"+e.maxValue+"%":e.scalingType=="hits"?" ／ 被弾1回 +"+e.scalingValue+"%、最大"+e.maxValue+"%":"";
            return type+" +"+e.baseValue+(e.effectType=="tool-uses"?"回":e.effectType=="status" || e.effectType=="wt"?"":"%")+scale+(e.durationClock>0?" ／ 持続 "+e.durationClock/100+"ターン":"");
        }
        private string oopartCompareKey,oopartCompareText;
        private string OopartComparison(FormalCampaignSave save,OopartProgress selected)
        {
            var slots=save.collection.ooparts.slots;string form=slots[oopartSlot].heroineFormId;
            if(form==null)return "—";
            string key=save.revision+"|"+oopartSlot+"|"+selected.oopartId+"|"+slots[oopartSlot].equippedOopartId;
            if(oopartCompareKey==key)return oopartCompareText;
            var ids=PreviewFormation(form);int actor=Array.IndexOf(ids,form);
            var before=new PlayableBattle(1,campaign.Playable,1,combatDefinitions:combatDefinitions.WithFormation(ids),formalGrowth:save.growth,collectionGrowth:save.collection,homeProgress:save.home,homeCatalog:HomeData(),relicCatalog:CollectionData(),useJobRulesV2:true);
            var afterSave=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(save));
            foreach(var slot in afterSave.collection.ooparts.slots)if(slot.equippedOopartId==selected.oopartId)slot.equippedOopartId=null;
            afterSave.collection.ooparts.slots[oopartSlot].equippedOopartId=selected.oopartId;
            var after=new PlayableBattle(1,campaign.Playable,1,combatDefinitions:combatDefinitions.WithFormation(ids),formalGrowth:afterSave.growth,collectionGrowth:afterSave.collection,homeProgress:afterSave.home,homeCatalog:HomeData(),relicCatalog:CollectionData(),useJobRulesV2:true);
            var a=before.State.Heroes[actor];var b=after.State.Heroes[actor];
            var old=new[]{a.MaxHitPoints,a.Attack,a.PhysicalDefense,a.MagicDefense,a.Speed};var next=new[]{b.MaxHitPoints,b.Attack,b.PhysicalDefense,b.MagicDefense,b.Speed};
            oopartCompareKey=key;return oopartCompareText="出撃時能力　"+string.Join(" ／ ",StatValues.Names.Select((k,i)=>StatName(k)+" "+old[i]+"→"+next[i]));
        }
        private string OopartConditionText(EffectCondition c)
        {
            if(c.kind=="all" || c.kind=="any")return "（"+string.Join(c.kind=="all"?" かつ ":" または ",c.items.Select(OopartConditionText))+"）";
            if(c.kind=="job" || c.kind=="formation-job"){string name=HeroineIdentityCatalog.JobName(c.targetId);return c.kind=="job"?name+"装備時":name+"を"+c.value+"人編成";}
            return OopartEffectEngine.ConditionsText(c);
        }
        private string OopartOperationSummary(FormalCampaignSave save)
        {
            if(oopartRequest.Kind!="level" && oopartRequest.Kind!="direct")return oopartRequest.Kind=="equip"?(oopartRequest.Target==null?"未装備":CollectionData().Oopart(oopartRequest.Target).name):oopartRequest.Kind=="preset-load" || oopartRequest.Kind=="preset-save"?"編成 "+(oopartPresetIndex+1):"第"+(oopartRequest.Slot+1)+"枠";
            var next=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(save));
            try{
                var old=save.collection.ooparts.progress.Single(p=>p.oopartId==oopartRequest.Target);oopartRequest.Apply(next,CollectionData(),CurrentFormation());var changed=next.collection.ooparts.progress.Single(p=>p.oopartId==oopartRequest.Target);
                string outcome=oopartRequest.Kind=="level"?"Lv "+old.level+" → "+changed.level:StatName(oopartRequest.Stat)+"累積 "+old.accumulatedRandomStats.Get(oopartRequest.Stat)+" → "+changed.accumulatedRandomStats.Get(oopartRequest.Stat);
                var costs=save.collection.materials.Select(m=>new{id=m.id,amount=m.amount-HomeRules.Balance(next,m.id)}).Where(m=>m.amount>0).Select(m=>CollectionData().resources.Single(r=>r.id==m.id).name+" "+m.amount+"個").ToList();
                int nectar=save.growth.nectar-next.growth.nectar;if(nectar>0)costs.Insert(0,"ネクタル "+nectar.ToString("N0"));
                string delta=oopartRequest.Kind=="level"?"\n強化後："+StatText(OopartService.Fixed(changed,CollectionData().Oopart(old.oopartId))):"";
                return outcome+"\n消費："+string.Join(" ／ ",costs)+delta;
            }catch(ArgumentException e){return e.Message;}
        }
        private void DrawOopartOverlay()
        {
            if(!OopartModalVisible)return;GrowthStyles();GrowthFill(0,88,1600,812,new Color(0,0,0,.88f));GrowthFrame(65,130,1470,705);var s=OopartSnapshot();var o=s.collection.ooparts;
            Label(95,145,1180,55,"編成 › 第"+(oopartSlot+1)+"枠 › オーパーツ",growthTitleStyle);
            if(oopartRequest!=null){
                string cost=OopartOperationSummary(s);
                Label(160,275,1280,160,"変更内容："+(oopartRequest.Kind=="direct"?StatName(oopartRequest.Stat)+"の直接強化":oopartRequest.Kind=="level"?"レベル強化":oopartRequest.Kind=="equip"?"装備変更":oopartRequest.Kind=="clear-slot"?"隊員を外す":oopartRequest.Kind=="preset-save"?"編成プリセット保存":"編成プリセット読込")+"\n"+cost,new GUIStyle(growthTextStyle){fontSize=20});
                if(oopartError!=null)Label(160,445,1280,90,oopartError,growthSmallStyle);
                if(GrowthButton(160,650,620, sixty,formalCampaign.HasPending?"再試行":"確定して保存",true,true))CommitOopart();if(GrowthButton(820,650,620, sixty,"取消",!formalCampaign.HasPending))oopartRequest=null;return;
            }
            string equipped=o.slots[oopartSlot].equippedOopartId;Label(95,188,1170,23,"現在装備："+(equipped==null?"なし":CollectionData().Oopart(equipped).name),new GUIStyle(growthSmallStyle){fontSize=16});
            if(GrowthButton(1290,150,210,42,"閉じる")){oopartPanel=false;return;}
            for(int i=0;i<5;i++){
                string caption="第"+(i+1)+"枠 ／ "+(o.slots[i].heroineFormId==null?"空枠":combatDefinitions.Hero(o.slots[i].heroineFormId).name);int originalFont=growthButtonStyle.fontSize;
                try{while(growthButtonStyle.fontSize>14 && growthButtonStyle.CalcSize(new GUIContent(caption)).x>239)growthButtonStyle.fontSize--;if(GrowthButton(95+i*282,212,265,45,caption,true,i==oopartSlot))oopartSlot=i;}finally{growthButtonStyle.fontSize=originalFont;}
            }
            oopartScroll=GUI.BeginScrollView(new Rect(95,277,440,465),oopartScroll,new Rect(0,0,415,Math.Max(465,o.progress.Length*66)));
            for(int i=0;i<o.progress.Length;i++){var p=o.progress[i];var d=CollectionData().Oopart(p.oopartId);int owner=Array.FindIndex(o.slots,x=>x.equippedOopartId==p.oopartId);if(GrowthButton(0,i*66,410,60,d.name+" ／ Lv"+p.level+"\n"+(owner<0?"未装備":"第"+(owner+1)+"枠に装備"),true,selectedOopart==p.oopartId))selectedOopart=p.oopartId;}GUI.EndScrollView();
            string currentForm=o.slots[oopartSlot].heroineFormId;
            if(GrowthButton(95,768,210,40,"装備を外す",o.slots[oopartSlot].equippedOopartId!=null))ProposeOopart("equip");if(GrowthButton(320,768,215,40,"隊員を外す",currentForm!=null))ProposeOopart("clear-slot");
            var selected=o.progress.SingleOrDefault(p=>p.oopartId==selectedOopart);if(selected==null){Label(570,300,875,80,"未所持",growthTextStyle);return;}var def=CollectionData().Oopart(selected.oopartId);var fixedStats=def.levelStats.At(selected.level);
            Label(570,275,920,50,def.name+" ／ Lv"+selected.level+" / 120",new GUIStyle(growthTitleStyle){fontSize=28});
            for(int i=0;i<5;i++){string stat=StatValues.Names[i];var r=def.randomStats.Single(v=>v.stat==stat);float y=327+i*38;int max=OopartService.Maximum(selected,r);Label(570,y,655,34,StatName(stat)+"　Lv固定 +"+fixedStats.Get(stat)+" ／ 累積 +"+selected.accumulatedRandomStats.Get(stat)+" / "+max,new GUIStyle(growthSmallStyle){fontSize=18});bool possible=OopartService.DirectEligible(selected,r) && s.growth.nectar>=def.directNectarCost && HomeRules.Balance(s,def.directMaterialId)>=def.directMaterialCost;if(GrowthButton(1250,y,230,32,"直接強化",possible))ProposeOopart("direct",selected.oopartId,stat:stat);}
            string form=o.slots[oopartSlot].heroineFormId;var context=new OopartEffectContext{inBattle=false,jobId=form==null?null:combatDefinitions.Hero(form).jobId,formationJobs=o.slots.Where(i=>i.heroineFormId!=null).Select(i=>combatDefinitions.Hero(i.heroineFormId).jobId).ToArray()};
            for(int i=0;i<def.effects.Length;i++){var e=def.effects[i];bool? active=OopartEffectEngine.Active(e,context);if(active==true && (e.scalingType!="constant" || e.durationClock>0))active=null;Label(570,527+i*58,915,54,EffectText(e)+"\n"+(form==null?"空枠":active==null?"戦闘中に判定":active.Value?"有効":"無効")+" ／ "+(e.conditions.Length==0?"常時":string.Join(" かつ ",e.conditions.Select(OopartConditionText))),new GUIStyle(growthSmallStyle){fontSize=16});}
            Label(570,652,915,44,OopartComparison(s,selected),new GUIStyle(growthSmallStyle){fontSize=17});
            bool elsewhere=o.slots.Where((v,i)=>i!=oopartSlot).Any(v=>v.equippedOopartId==selected.oopartId);
            if(GrowthButton(570,707,330,45,elsewhere?"他の編成枠で装備中":"装備",!elsewhere && o.slots[oopartSlot].equippedOopartId!=selected.oopartId,true))ProposeOopart("equip",selected.oopartId);
            int[] amounts={1,10,120};for(int i=0;i<3;i++){
                int reachable=OopartService.ReachableLevel(s,CollectionData(),selected.oopartId,amounts[i]);
                string caption=selected.level==120?"強化完了":reachable==selected.level?"素材不足":(amounts[i]==120?"最大：":"")+"Lv "+reachable;
                if(GrowthButton(920+i*190,707,175,45,caption,reachable>selected.level))ProposeOopart("level",selected.oopartId,amounts[i]);
            }
            string guidance=selected.level==120?"レベル強化は完了しています。":string.Join(" ／ ",CollectionData().relics.Single(r=>r.id==selected.oopartId).materialIds.Select(id=>{
                var material=CollectionData().resources.Single(r=>r.id==id);int owned=HomeRules.Balance(s,id),needed=FormalRelicRules.UpgradeCost(new CollectionRelic{level=selected.level},RelicOperation.LevelUp,CollectionData().contentVersion);
                string source=WorldCatalog.Colossi.FirstOrDefault(c=>c.Id==material.ownerId)?.DisplayName??material.ownerId;
                return material.name+" "+owned+" / "+needed+"個"+(owned<needed?"（不足 "+(needed-owned)+"）":"")+"　入手："+source;
            }));
            Label(570,760,915,65,(oopartError??oopartMessage)!=null?(oopartError??oopartMessage)+"\n"+guidance:guidance,new GUIStyle(growthSmallStyle){fontSize=15});
        }
        private void DrawOopartPresets(bool enabled)
        {
            var o=OopartSnapshot().collection.ooparts;string id="preset."+(oopartPresetIndex+1);
            if(GrowthButton(95,660,350,45,"編成プリセット "+(oopartPresetIndex+1),enabled))oopartPresetIndex=(oopartPresetIndex+1)%3;
            if(GrowthButton(480,660,430,45,"現在の5枠を保存",enabled)){oopartPanel=true;ProposeOopart("preset-save",id);}
            if(GrowthButton(945,660,490,45,"プリセットを読み込む",enabled && o.presets.Any(p=>p.id==id))){oopartPanel=true;ProposeOopart("preset-load",id);}
        }
    }
}
