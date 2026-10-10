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
        private bool jobPanelValidationPending,jobPanelProbe,jobPanelProbeFound,jobPanelProbeEnabled,jobPanelProbeClick;
        private int jobPanelProbeColumn,jobPanelProbeRow;
        // The job and skill panels share the original action area.
        private bool JobCommand(int column,int row,string caption,bool enabled,int columns=2,int span=1,int rows=3)
        {
            if(jobPanelProbe){
                if(column!=jobPanelProbeColumn || row!=jobPanelProbeRow)return false;
                jobPanelProbeFound=true;jobPanelProbeEnabled=enabled;return enabled && jobPanelProbeClick;
            }
            float gap=6,width=(332-gap*(columns-1))/columns,height=rows==4?26:32;
            return Btn(34+column*(width+gap),536+row*(rows==4?28:36),width*span+gap*(span-1),height,caption,enabled,
                new GUIStyle(button){fontSize=16,wordWrap=true,padding=new RectOffset(3,3,0,0),alignment=TextAnchor.MiddleCenter});
        }
        private string JobPanelResource(int actor)
        {
            var j=encounter.JobState(actor);var h=encounter.State.Heroes[actor];
            string gauge=h.JobResource+"/"+h.JobResourceMax;
            if(j.Id=="job.fighter")return "珠 "+gauge+" ／ BOOST "+j.Gauge+"/100\n"+(j.Reckless?"捨て身":"通常")+(encounter.JobState(actor).Empowered(encounter.Clock)?" ／ 全能力強化中":"");
            if(j.Id=="job.berserker")return "捕食 "+j.Predation+"/10 ／ ゲージ "+gauge+"\n"+(encounter.JobState(actor).Empowered(encounter.Clock)?"二重発動中":"通常");
            if(j.Id=="job.defender")return "ゲージ "+gauge+"\n"+(encounter.JobState(actor).Empowered(encounter.Clock)?"全員護衛・反撃中":"護衛："+encounter.JobDescription(actor).Split('/')[0].Replace("護衛：",""));
            if(j.Id=="job.blaster")return encounter.ResourceName(actor)+" "+gauge+"\n設定：詠唱 "+j.CastPercent+"% ／ ×"+j.Repeat;
            if(j.Id=="job.gunner")return "ゲージ "+gauge+"\n弾倉1："+j.Magazines[0]+"/6 ／ 弾倉2："+j.Magazines[1]+"/6";
            if(j.Id=="job.artist")return "歌唱ゲージ "+gauge+"\n"+(j.Singing?"歌唱中・共鳴 "+j.SongStage+"/5 ／ 毎T2消費":"通常行動で＋2 ／ 開始に3必要");
            if(j.Id=="job.alchemist")return "錬成 "+gauge+" ／ 投入 "+System.Linq.Enumerable.Sum(alchemyUnits)+" ／ 選択 "+encounter.AlchemyAttributes(actor)[alchemyAttribute]+"\n火弱点：火2＋光1 ／ 行動消費なし";
            if(j.Id=="job.chaser")return "駆動 "+gauge+" ／ NITRO "+j.NitroCount+"/10\n"+(j.NitroSelected?"次スキルWT0":"GEAR "+j.Gear+" ／ 消費 "+PlayableBattle.ChaserGearCost(j.Gear));
            if(j.Id=="job.general")return "指揮 "+gauge+"\n"+(actor!=encounter.CommanderActor?"非指揮官・通常スキルのみ":encounter.JobState(actor).Empowered(encounter.Clock)?"指揮官・5枠強化中":"指揮官・5枠の固有効果");
            if(j.Id=="job.sniper")return "狙撃 "+gauge+"\n"+(encounter.IsSniping(actor)?"詠唱中・全員支援":"支援対象："+encounter.HeroineName(encounter.SniperSupportTarget(actor)));
            if(j.Id=="job.panzer")return (h.ArmorActive?"ARMOR ":"生身 ")+h.HitPoints+"/"+h.MaxHitPoints+"\n"+(h.ArmorActive?"耐性："+(j.ArmorResistance=="physical"?"物理":j.ArmorResistance=="magic"?"魔法":"火"):"CALLまで "+System.Math.Max(0,h.ArmorCallAt-encounter.Clock));
            return encounter.JobDescription(actor);
        }
        private void DrawJobControls(int actor,bool enabled)
        {
            var j=encounter.JobState(actor);var h=encounter.State.Heroes[actor];
            string resource=JobPanelResource(actor);
            if(j.Id=="job.healer")resource="生命 "+h.JobResource+"/"+h.JobResourceMax+" ／ 対象："+encounter.HeroineName(lifeTarget);

            if(j.Id=="job.gambler")resource="資源消費なし ／ 3×3・5ライン";
            var resourceStyle=new GUIStyle(small){fontSize=16,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            if(plan10UiCapture && Event.current.type==EventType.Repaint && resourceStyle.CalcHeight(new GUIContent(resource),332)>49.1f)throw new System.InvalidOperationException("Job resource text overflow: "+j.Id+" / "+resource);
            Label(34,482,332,49,resource,resourceStyle,gold);
            if(j.Id!="job.gambler"){
                int value=j.Id=="job.fighter"?j.Gauge:h.JobResource,max=j.Id=="job.fighter"?100:h.JobResourceMax;
                if(j.Id=="job.panzer"){value=h.HitPoints;max=h.MaxHitPoints;}
                Meter(34,531,332,3,value,max,j.Id=="job.panzer" && !h.ArmorActive?new Color(.8f,.24f,.17f):gold);
            }
            if(j.Id=="job.fighter"){
                if(JobCommand(0,0,"−珠",enabled && j.BoostUnits>0,3))encounter.SelectBoostUnits(actor,Mathf.Max(0,j.BoostUnits-1));
                Label(146,539,108,26,"投入 "+j.BoostUnits+"個",small,gold);
                if(JobCommand(2,0,"＋珠",enabled && j.BoostUnits<h.JobResource,3))encounter.SelectBoostUnits(actor,j.BoostUnits+1);
                if(JobCommand(0,1,j.Reckless?"設定：捨て身 → 通常":"設定：通常 → 捨て身",enabled,2,2))encounter.ToggleReckless(actor);
                if(JobCommand(0,2,j.Gauge==100?"BOOST解放・3T ／ READY維持":"BOOST解放 ／ ゲージ100で使用",enabled && j.Gauge==100,2,2)){encounter.ActivateJobGauge(actor);QueueBattleEvents();}
            }else if(j.Id=="job.gambler"){
                if(JobCommand(0,0,"SLOT実行 ／ 全外れWT0",enabled,2,2) && encounter.SpinGamblerSlot(actor,target)){ResetBattleMenu();QueueBattleEvents();}
            }else if(j.Id=="job.sniper"){
                if(JobCommand(0,0,h.JobResource==h.JobResourceMax?"狙撃開始 ／ MAX消費":"狙撃開始 ／ 資源MAXで使用",enabled && h.JobResource==h.JobResourceMax,2,2) && encounter.StartSniperMode(actor,target)){ResetBattleMenu();QueueBattleEvents();}
                Label(34,577,332,58,"詠唱中は味方全員を支援",small,ivory);
            }else if(j.Id=="job.general"){
                bool commander=actor==encounter.CommanderActor;
                if(JobCommand(0,0,!commander?"非指揮官：固有コマンドなし":h.JobResource<h.JobResourceMax?"指揮解放 ／ 資源MAXで使用":"指揮解放 ／ READY維持",enabled && commander && h.JobResource==h.JobResourceMax,2,2)){encounter.ActivateGeneralCommand(actor);QueueBattleEvents();}
                Label(34,577,332,58,"指揮官は5枠の固有効果を3T強化",small,ivory);
            }else if(j.Id=="job.berserker" || j.Id=="job.defender"){
                if(JobCommand(0,0,h.JobResource<h.JobResourceMax?"ゲージ解放 ／ MAXで使用":"ゲージ解放 ／ READY維持",enabled && h.JobResource==h.JobResourceMax,2,2)){encounter.ActivateJobGauge(actor);QueueBattleEvents();}
                Label(34,577,332,58,j.Id=="job.berserker"?"3T：スキル二重発動":"3T：全員護衛・反撃",small,ivory);
            }else if(j.Id=="job.blaster"){
                int[] casts={0,50,100,200};
                for(int i=0;i<4;i++)if(JobCommand(i%2,i/2,(j.CastPercent==casts[i]?"◆ ":"")+"詠唱 "+casts[i]+"%",enabled && PlayableBattle.BlasterCost(casts[i],j.Repeat)<=h.JobResource))encounter.SelectBlaster(actor,casts[i],j.Repeat);
                for(int i=1;i<=3;i++)if(JobCommand(i-1,2,(j.Repeat==i?"◆ ":"")+"×"+i,enabled && PlayableBattle.BlasterCost(j.CastPercent,i)<=h.JobResource,3))encounter.SelectBlaster(actor,j.CastPercent,i);
            }else if(j.Id=="job.artist"){
                if(j.Singing){
                    if(JobCommand(0,0,"歌唱継続 ／ 行動消費",enabled,2,2)){encounter.ContinueSong(actor);ResetBattleMenu();QueueBattleEvents();}
                    if(JobCommand(0,1,"歌唱解除 ／ READY維持",enabled,2,2)){encounter.StopSong(actor);QueueBattleEvents();}
                }else if(JobCommand(0,0,h.JobResource>=3?"歌唱開始 ／ 行動消費":"歌唱開始 ／ ゲージ3以上",enabled && h.JobResource>=3,2,2)){encounter.StartSong(actor);ResetBattleMenu();QueueBattleEvents();}
            }else if(j.Id=="job.healer"){
                if(JobCommand(0,0,"対象変更 ↻",enabled,2,2,4))lifeTarget=(lifeTarget+1)%5;
                string[] tools={"heal","overheal","maxhp","revive","invest"},labels={"回復 −2","超過回復 −4","最大HP −4","蘇生 −8","生命投資 −3"};
                for(int i=0;i<tools.Length;i++)if(JobCommand(i%2,1+i/2,labels[i],enabled && encounter.CanUseLifeTool(actor,tools[i],lifeTarget),2,1,4)){encounter.UseLifeTool(actor,tools[i],lifeTarget);ResetBattleMenu();QueueBattleEvents();}
            }else if(j.Id=="job.chaser")DrawChaserBattleControls(actor,enabled,null);
            else if(j.Id=="job.alchemist")DrawAlchemyBattleControls(actor,enabled,null);
            else if(j.Id=="job.panzer")DrawPanzerBattleTools(actor,enabled,null);
            else if(j.Id=="job.gunner"){
                for(int i=0;i<2;i++)if(JobCommand(i,0,(j.SelectedMagazine==i?"◆ ":"")+"弾倉"+(i+1)+"："+j.Magazines[i]+"/6",enabled))encounter.SelectMagazine(actor,i);
                if(JobCommand(0,1,"全弾倉RELOAD ／ 行動消費",enabled,2,2)){ResetBattleMenu();encounter.Reload(actor);QueueBattleEvents();}
                string volley=j.Magazines[j.SelectedMagazine]==0?"全弾射撃 ／ 選択弾倉が空":h.JobResource<h.JobResourceMax?"全弾射撃 ／ ゲージMAXで使用":"全弾射撃 ／ 行動消費";
                if(JobCommand(0,2,volley,enabled && h.JobResource==h.JobResourceMax && j.Magazines[j.SelectedMagazine]>0,2,2) && encounter.FullVolley(actor,target)){ResetBattleMenu();QueueBattleEvents();}
            }
        }
    }
}
