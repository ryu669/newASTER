using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private int protectedFormationSlot;
        private int lifeTarget;
        private void PrepareBattleJobCapture(string[] args)
        {
            int index=System.Array.IndexOf(args,"-captureBattleJob");
            if(index<0 || index+1>=args.Length || capturePath==null)return;
            if(!formalDiagnostic)throw new System.InvalidOperationException("Job capture requires isolated production fixture.");
            int actor=int.Parse(args[index+1]);if(actor<0 || actor>=5)throw new System.ArgumentOutOfRangeException();
            StartBattle(NewAster.Data.WorldCatalog.ColossusIds[0],715);
            int guard=0;while(encounter.AvailableHero!=actor && !encounter.Ended && guard++<200)encounter.Pass();
            if(encounter.AvailableHero!=actor)throw new System.InvalidOperationException("Job fixture never became READY.");
            encounter.State.Heroes[actor].GainResource(15);
            if(actor==0){encounter.SelectBoostUnits(actor,3);encounter.ToggleReckless(actor);}
            if(actor==1 || actor==2)encounter.ActivateJobGauge(actor);
            if(actor==3)encounter.SelectBlaster(actor,200,2);
            if(actor==4)encounter.SelectMagazine(actor,1);
            encounter.DrainPresentationEvents();SelectNextHero();ResetBattleMenu();battlePanel=BattlePanel.Actions;
            Debug.Log("BATTLE_JOB_V2_CAPTURE_PASS actor="+actor+" isolated=true");
        }
        private void DrawJobControls(int actor,bool enabled)
        {
            var j=encounter.JobState(actor);var h=encounter.State.Heroes[actor];
            var jobButton=new GUIStyle(button){fontSize=16,wordWrap=false,padding=new RectOffset(2,2,0,0),alignment=TextAnchor.MiddleCenter};
            var jobLabel=new GUIStyle(small){fontSize=17,padding=new RectOffset(0,0,0,0),wordWrap=false};
            if(j.Id=="job.fighter"){
                if(Btn(34,484,60,32,"−珠",enabled,jobButton))encounter.SelectBoostUnits(actor,Mathf.Max(0,j.BoostUnits-1));
                Label(100,487,120,28,"投入 "+j.BoostUnits+"個",jobLabel,ivory);
                if(Btn(220,484,60,32,"＋珠",enabled && j.BoostUnits<h.JobResource,jobButton))encounter.SelectBoostUnits(actor,j.BoostUnits+1);
                if(Btn(295,484,250,32,j.Reckless?"捨て身 → 通常":"通常 → 捨て身",enabled,jobButton))encounter.ToggleReckless(actor);
                if(Btn(560,484,295,32,"BOOST解放・3ターン",enabled && j.Gauge==100,jobButton))encounter.ActivateJobGauge(actor);
            }else if(j.Id=="job.berserker" || j.Id=="job.defender"){
                if(Btn(34,484,480,32,j.Id=="job.berserker"?"ゲージ解放 ／ スキル二重発動・3ターン":"ゲージ解放 ／ 全員護衛と反撃・3ターン",enabled && h.JobResource==h.JobResourceMax,jobButton))encounter.ActivateJobGauge(actor);
            }else if(j.Id=="job.blaster"){
                int[] casts={0,50,100,200};
                for(int i=0;i<4;i++)if(Btn(34+i*112,484,105,32,(j.CastPercent==casts[i]?"◆":"")+"詠唱"+casts[i]+"%",enabled && PlayableBattle.BlasterCost(casts[i],j.Repeat)<=h.JobResource,jobButton))encounter.SelectBlaster(actor,casts[i],j.Repeat);
                for(int i=1;i<=3;i++)if(Btn(490+(i-1)*120,484,112,32,(j.Repeat==i?"◆":"")+"×"+i,enabled && PlayableBattle.BlasterCost(j.CastPercent,i)<=h.JobResource,jobButton))encounter.SelectBlaster(actor,j.CastPercent,i);
            }else if(j.Id=="job.artist"){
                if(j.Singing){
                    if(Btn(34,484,380,32,"歌唱継続 ／ スキル選択なし",enabled,jobButton)){encounter.ContinueSong(actor);ResetBattleMenu();QueueBattleEvents();}
                    if(Btn(435,484,420,32,"歌唱解除 ／ 同じ行動順でスキルへ",enabled,jobButton)){encounter.StopSong(actor);QueueBattleEvents();}
                }else if(Btn(34,484,500,32,"歌唱開始 ／ ゲージ3以上・毎ターン2消費",enabled && h.JobResource>=3,jobButton)){encounter.StartSong(actor);ResetBattleMenu();QueueBattleEvents();}
            }else if(j.Id=="job.healer"){
                if(Btn(34,484,160,32,"対象 "+(lifeTarget+1)+" ↻",enabled,jobButton))lifeTarget=(lifeTarget+1)%5;
                string[] tools={"heal","overheal","maxhp","revive","invest"},labels={"回復 2","超過 4","最大HP 4","蘇生 8","生命投資 3"};
                for(int i=0;i<tools.Length;i++)if(Btn(204+i*132,484,124,32,labels[i],enabled && encounter.CanUseLifeTool(actor,tools[i],lifeTarget),jobButton)){encounter.UseLifeTool(actor,tools[i],lifeTarget);ResetBattleMenu();QueueBattleEvents();}
            }else if(j.Id=="job.chaser"){
                DrawChaserBattleControls(actor,enabled,jobButton);
            }else if(j.Id=="job.alchemist"){
                DrawAlchemyBattleControls(actor,enabled,jobButton);
            }else if(j.Id=="job.panzer"){
                DrawPanzerBattleTools(actor,enabled,jobButton);
            }else if(j.Id=="job.gunner"){
                for(int i=0;i<2;i++)if(Btn(34+i*165,484,155,32,(j.SelectedMagazine==i?"◆":"")+"弾倉"+(i+1)+"："+j.Magazines[i],enabled,jobButton))encounter.SelectMagazine(actor,i);
                if(Btn(375,484,200,32,"全弾倉RELOAD",enabled,jobButton)){ResetBattleMenu();encounter.Reload(actor);QueueBattleEvents();}
                if(Btn(590,484,265,32,"選択弾倉の全弾射撃",enabled && h.JobResource==h.JobResourceMax && j.Magazines[j.SelectedMagazine]>0,jobButton)){
                    if(encounter.FullVolley(actor,target)){ResetBattleMenu();QueueBattleEvents();}
                }
            }
            if(j.Id=="job.healer" || j.Id=="job.alchemist" || j.Id=="job.chaser")jobLabel.fontSize=14;Label(34,517,820,25,encounter.JobDescription(actor)+(j.Id=="job.healer"?" ／ "+encounter.HeroineName(lifeTarget):j.Id=="job.alchemist"?" ／ "+encounter.AlchemyPreview(actor,alchemyUnits):""),jobLabel,gold);
        }
    }
}
