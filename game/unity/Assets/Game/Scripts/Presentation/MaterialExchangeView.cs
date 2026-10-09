using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string exchangeMaterial,exchangeError;
        private int exchangeQuantity;
        private long exchangeRevision;
        private void ProposeExchange(CollectionResourceDef resource,int quantity)
        {var s=formalCampaign.Snapshot;if(quantity<1 || quantity>MaterialExchangeService.Maximum(s,resource))return;exchangeMaterial=resource.id;exchangeQuantity=quantity;exchangeRevision=formalCampaign.Revision;exchangeError=null;}
        private void DrawExchange()
        {
            GrowthFill(0,0,1600,900,new Color(0,0,0,.8f));GrowthFrame(360,230,880,450);
            var resource=CollectionData().resources.Single(r=>r.id==exchangeMaterial);var s=formalCampaign.Snapshot;int cost=checked(exchangeQuantity*MaterialExchangeService.UnitCost(resource));
            Label(420,280,760,200,resource.name+" ×"+exchangeQuantity+"\n費用 "+cost+"ネクタル\n残高 "+s.growth.nectar+" → "+(s.growth.nectar-cost),growthTextStyle);
            Label(420,475,760,60,exchangeError??"数量と残高を確認してください。",growthSmallStyle);
            if(GrowthButton(420,570,350,60,"交換する",!formalCampaign.HasPending && exchangeQuantity<=MaterialExchangeService.Maximum(s,resource))){
                exchangeRevision=formalCampaign.Revision;
                try{if(formalCampaign.CommitMaterialExchange(exchangeMaterial,exchangeQuantity,exchangeRevision,CollectionData(),formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign)){formalProgression=new FormalProgression(formalCampaign.Snapshot.growth,combatDefinitions.HeroineIds);exchangeMaterial=null;}else exchangeError="保存できませんでした。費用・素材は変更していません。";}catch(Exception e){exchangeError=e.Message;}
            }
            if(GrowthButton(800,570,350,60,"戻る")){exchangeMaterial=null;exchangeError=null;}
        }
    }
}
