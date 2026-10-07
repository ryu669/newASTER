using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private PanzerLoadout SavedPanzerLoadout(){var e=HomeState.panzerEquipment?.SingleOrDefault(p=>p.heroineId=="heroine.shell");return e==null?new PanzerLoadout():new PanzerLoadout(e.resistance,e.firstTool,e.secondTool);}
        private bool panzerSetupOpen;
        private int panzerResistance,panzerTool0,panzerTool1=1;
        private string BattleResourceLine(int actor,BattleHero hero,BattlePresentationEvent visual)=>hero.IsPanzer?(hero.ArmorActive?"装甲は通常回復不可・ツールで修復":"生身は通常回復可・防御のみ"):encounter.ResourceName(actor)+" "+(visual?.Resources[actor]??hero.JobResource)+"/"+hero.JobResourceMax;
        private void DrawPanzerSetup()
        {
            if(!panzerSetupOpen)return;drawingModal=true;
            GrowthFill(0,0,1600,900,new Color(0,0,0,.8f));GrowthFrame(300,160,1000,580);
            Label(340,190,920,55,"シェルの出撃装甲・ツール",growthTitleStyle,gold);
            Label(340,260,920,80,"装甲耐性と有限ツール2枠を、出撃前に設定します。\nこの設定は次の戦闘から適用。各ツールは戦闘ごとに2回、再召喚では補充されません。",growthSmallStyle);
            string[] values={"physical","magic","fire"},labels={"物理ダメージ20%軽減","魔法ダメージ20%軽減","火20%・火傷蓄積半減"};
            for(int i=0;i<3;i++)if(GrowthButton(340+i*307,355,298,52,labels[i],homeRequest==null,panzerResistance==i))panzerResistance=i;
            int[] slots={panzerTool0,panzerTool1};
            for(int slot=0;slot<2;slot++){
                Label(340,430+slot*82,200,48,"ツール枠 "+(slot+1),growthTextStyle);
                for(int i=0;i<4;i++)if(GrowthButton(555+i*174,430+slot*82,164,48,PlayableBattle.PanzerToolName(PlayableBattle.PanzerTools[i]),homeRequest==null && i!=slots[1-slot],slots[slot]==i)){if(slot==0)panzerTool0=i;else panzerTool1=i;}
            }
            Label(340,604,920,40,"修復：装甲40% ／ 防護：物理35%・3T ／ 鼓舞：味方攻撃25%・3T ／ 延長：全員の効果＋1T",new GUIStyle(growthSmallStyle){fontSize=17});
            if(homeError!=null)Label(340,638,920,28,homeError,new GUIStyle(growthSmallStyle){fontSize=16});
            if(GrowthButton(340,674,430,48,homeRequest==null?"設定を保存して戻る":"同じ設定で保存を再試行",true,true)){
                if(homeRequest==null)ProposeHome(new HomeOperation("panzer-loadout","heroine.shell",values[panzerResistance],PlayableBattle.PanzerTools[panzerTool0],PlayableBattle.PanzerTools[panzerTool1]));ConfirmHome();if(homeRequest==null)panzerSetupOpen=false;
            }
            if(GrowthButton(810,674,450,48,"変更せず戻る",homeRequest==null))panzerSetupOpen=false;
        }
        private void DrawPanzerBattleTools(int actor,bool enabled,GUIStyle style)
        {
            var j=encounter.JobState(actor);
            if(encounter.RequiresPanzerDefense(actor)){
                Label(34,484,820,32,"装甲喪失中：スキル・ツール・パス不可。下の防御で再召喚を待つ。",new GUIStyle(small){fontSize=17},gold);return;
            }
            if(Btn(34,484,154,32,"支援対象 "+(lifeTarget+1)+" ↻",enabled,style))lifeTarget=(lifeTarget+1)%5;
            for(int slot=0;slot<2;slot++)if(Btn(200+slot*330,484,320,32,PlayableBattle.PanzerToolName(j.PanzerTools[slot])+" ／ 残り"+j.ToolUses[slot]+"回",enabled && encounter.CanUsePanzerTool(actor,slot,lifeTarget),style)){
                encounter.UsePanzerTool(actor,slot,lifeTarget);ResetBattleMenu();QueueBattleEvents();
            }
        }
    }
}
