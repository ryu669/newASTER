using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private enum BattlePanel { None,Actions,Targets,Status,Timeline }
        private BattlePanel battlePanel;
        private bool battleMenuExpanded;
        private Vector2 battleDetailsScroll;
        private static readonly Rect battleDrawer=new Rect(1040,112,530,596);
        private bool BattleInputAllowed=>encounter!=null && !encounter.Ended && !paused && !playback.Busy && !retreat && !help && result==null && !selectingAlly;
        private string BattleTargetName(string id)
        {int index=encounter.State.Parts.ToList().FindIndex(p=>p.Id==id);return index<0?"本体":ColossusCombatCatalog.PartName(encounter.State.Parts[index],index);}
        private void ResetBattleMenu(){battlePanel=BattlePanel.None;battleMenuExpanded=false;battleDetailsScroll=Vector2.zero;}
        private void OpenBattlePanel(BattlePanel panel)
        {
            if(encounter==null || retreat || help || result!=null || selectingAlly)return;
            if((panel==BattlePanel.Actions || panel==BattlePanel.Targets) && !BattleInputAllowed)return;
            battlePanel=battlePanel==panel?BattlePanel.None:panel;battleDetailsScroll=Vector2.zero;
        }
        private bool CloseBattleMenuLayer()
        {
            if(encounter==null || retreat || help || result!=null || selectingAlly)return false;
            if(battlePanel!=BattlePanel.None){battlePanel=BattlePanel.None;battleDetailsScroll=Vector2.zero;return true;}
            if(battleMenuExpanded){battleMenuExpanded=false;return true;}return false;
        }
        private void DrawBattle()
        {
            RecordPlan7RenderedEvent();
            var Names=Enumerable.Range(0,5).Select(encounter.HeroineName).ToArray();
            var s=encounter.State;var visual=playback.Current;
            if(playback.Busy && (battlePanel==BattlePanel.Actions || battlePanel==BattlePanel.Targets))battlePanel=BattlePanel.None;
            target=illustrationView.Draw(encounter,visual,illustrationElapsed,target,BattleInputAllowed && battlePanel==BattlePanel.None,text,small,Names,s.Parts.Select(ColossusCombatCatalog.PartName).ToArray(),battlePanel==BattlePanel.None);
            GrowthFill(0,0,1600,86,new Color(.025f,.05f,.065f,.86f));
            Label(24,6,600,44,$"{WorldCatalog.Colossi.First(c=>c.Id==activeColossus).DisplayName}  Lv.{s.SelectedLevel}",heading,Color.white);
            int bossHp=visual?.BossHp??s.BossHitPoints,gauge=visual?.BossGauge??s.BossGauge;
            Label(24,48,595,27,$"HP {bossHp}/{s.BossMaxHitPoints}　大技 {gauge}/{s.BossGaugeMax}",small,Color.white);
            Meter(24,77,595,5,bossHp,s.BossMaxHitPoints,new Color(.7f,.2f,.33f));
            Label(655,12,400,62,"次の敵行動\n"+encounter.NextEnemyAction+(encounter.IsEnraged?" ／ 怒り":""),small,new Color(1,.87f,.59f));
            if(visual!=null && (visual.FullChain || visual.Chain>1))Label(1080,15,250,50,visual.FullChain?"FULL CHAIN\n追加 "+(visual.ChainActionCount-visual.Chain):"CHAIN "+visual.Chain,text,new Color(1,.85f,.5f));
            if(Btn(1380,18,190,48,paused?"手動で再開":"一時停止",result==null && !retreat && !help && !selectingAlly))paused=!paused;

            string message=breakNoticeRemaining>0?breakNotice:visual!=null?(visual.Actor<0?"巨神獣":Names[visual.Actor])+" ／ "+visual.Message:paused?"一時停止中。手動で再開できます。":"行動と対象を選べます。";
            if(breakNoticeRemaining>0 || visual!=null || paused){
                GrowthFill(20,708,1560,42,new Color(.025f,.05f,.065f,.82f));
                var noticeStyle=new GUIStyle(small){padding=new RectOffset(0,0,0,0)};
                while(noticeStyle.fontSize>13 && noticeStyle.CalcHeight(new GUIContent(message),1528)>35)noticeStyle.fontSize--;
                Label(36,713,1528,35,message,noticeStyle,breakNoticeRemaining>0?new Color(1,.85f,.5f):Color.white);
            }
            if(BattleInputAllowed && encounter.AvailableHero>=0 && battlePanel==BattlePanel.None)battlePanel=BattlePanel.Actions;
            if(battlePanel==BattlePanel.Actions)DrawBattleActions();else if(battlePanel!=BattlePanel.None){var matrix=GUI.matrix;if(battlePanel==BattlePanel.Targets)GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(-1020,0,0));DrawBattleDrawer();GUI.matrix=matrix;}
            if(Btn(20,762,190,48,"対象・部位",BattleInputAllowed))OpenBattlePanel(BattlePanel.Targets);
            if(Btn(220,762,190,48,"人物・状態",!selectingAlly))OpenBattlePanel(BattlePanel.Status);
            if(Btn(420,762,190,48,battleMenuExpanded?"操作を畳む ‹":"操作を開く ›",!selectingAlly)){
                battleMenuExpanded=!battleMenuExpanded;if(!battleMenuExpanded)battlePanel=BattlePanel.None;
            }
            if(battleMenuExpanded){
                if(Btn(620,762,190,48,"行動順",!selectingAlly))OpenBattlePanel(BattlePanel.Timeline);
                if(Btn(820,762,165,48,"ヘルプ",!selectingAlly)){ResetBattleMenu();help=true;paused=true;}
                if(Btn(995,762,165,48,"撤退",!selectingAlly)){ResetBattleMenu();retreat=true;paused=true;}
            }
            if(playback.Busy){
                if(Btn(1180,762,390,48,"演出をスキップ",!paused && !retreat && !help && !selectingAlly)){
                    playback.Skip();if(stage!=null)stage.ClearActionEffects();shownEvent=0;SelectNextHero();FinishCheck();
                }
            }else if(Btn(1180,762,390,48,"行動 ／ "+Names[encounter.AvailableHero>=0?encounter.AvailableHero:selectedHero],BattleInputAllowed)){
                SelectNextHero();OpenBattlePanel(BattlePanel.Actions);
            }
            GrowthFill(0,820,1600,80,new Color(.025f,.05f,.065f,.88f));
            var cardStyle=new GUIStyle(button){fontSize=16,padding=new RectOffset(8,8,3,3)};
            for(int i=0;i<5;i++){
                float x=20+i*315;var hero=s.Heroes[i];int hp=visual?.HeroHp[i]??hero.HitPoints;
                bool casting=visual?.Casting[i]??encounter.IsCasting(i),healed=visual!=null && visual.Kind==BattlePresentationKind.Healing && visual.HealingTargets.Contains(i);
                string state=hp==0?"戦闘不能":healed?"回復対象":casting?"詠唱中":playback.Busy?(visual?.Actor==i?"行動中":""):encounter.AvailableHero==i?"行動可能":"";
                if(healed)GrowthFill(x-2,830,307,63,new Color(.18f,.6f,.42f));
                if(Btn(x,830,303,53,Names[i]+"　"+state+"\n"+(hero.IsPanzer && hero.ArmorActive?"ARMOR ":"HP ")+hp+"/"+hero.MaxHitPoints,!selectingAlly,cardStyle)){
                    selectedHero=i;battlePanel=BattlePanel.Status;battleDetailsScroll=Vector2.zero;
                }
                Meter(x,886,303,5,hp,hero.MaxHitPoints,hp*3<hero.MaxHitPoints?new Color(.8f,.24f,.17f):new Color(.15f,.55f,.35f));
            }
            status=visual?.Message??message;
            if(selectingAlly)DrawAllySelection();
        }
        private void DrawBattleDrawer()
        {
            var Names=Enumerable.Range(0,5).Select(encounter.HeroineName).ToArray();
            var Jobs=combatDefinitions.IsFormal?encounter.State.Heroes.Select(h=>HeroineIdentityCatalog.JobName(combatDefinitions.Hero(h.Id).jobId)).ToArray():new[]{"ファイター","バーサーカー","ディフェンダー","ブラスター","ガンナー"};
            Panel(battleDrawer.x,battleDrawer.y,battleDrawer.width,battleDrawer.height,dark);
            string caption=battlePanel==BattlePanel.Actions?"行動を選ぶ":battlePanel==BattlePanel.Targets?"対象・部位":battlePanel==BattlePanel.Status?"人物・状態":"行動順";
            Label(1060,126,430,44,caption,heading,Color.white);
            if(Btn(1505,126,45,44,"×")){battlePanel=BattlePanel.None;return;}
            if(battlePanel==BattlePanel.Actions){DrawBattleActions();return;}
            if(battlePanel==BattlePanel.Targets){DrawBattleTargets();return;}
            string details;
            if(battlePanel==BattlePanel.Timeline){
                details="戦闘時刻 "+(playback.Current?.Clock??encounter.Clock)+" ／ Battle Turn "+encounter.BattleTurn+"\n\n"+string.Join("\n\n",encounter.UpcomingOrder().Select((e,i)=>(i==0?"▶ ":"")+(e.Actor<0?"巨神獣":Names[e.Actor])+(e.IsCast?" ／ 発動":"")+"　T "+e.At));
            }else{
                int actor=selectedHero;var hero=encounter.State.Heroes[actor];var visual=playback.Current;
                var effects=visual?.HeroEffects[actor]??hero.TimedEffects;
                details=Names[actor]+" ／ "+Jobs[actor]+"\nHP "+(visual?.HeroHp[actor]??hero.HitPoints)+"/"+hero.MaxHitPoints+"\n"+BattleResourceLine(actor,hero,visual)+"　速度 "+hero.Speed+"\n\n次の行動 T "+encounter.NextAt(actor)+"\n敵からの予測ダメージ "+encounter.PreviewEnemyDamage(actor)+"\n\nチェイン基本50% ／ 最大70%\n+5%累積対象："+string.Join("・",Enumerable.Range(0,5).Where(encounter.HasCumulativeChainBonus).Select(i=>Names[i]))+"\n固定行動："+encounter.ChainActionDescription(actor)+"\n\n"+string.Join("\n",effects.Select(e=>TimedSelfEffectDef.Label(e.Kind)+(e.Kind=="forced-target"?"":e.Percent+"%")+"（残り"+e.RemainingCommands+(encounter.UsesJobRulesV2?"ターン）":"行動）")))+"\n"+encounter.JobDescription(actor)+"\n\n状態異常："+(visual?.HeroStatuses[actor]??hero.Status.Description)+"\n"+string.Join("\n",EnemyStatusState.Kinds.Where(k=>hero.Status.Active(k)).Select(k=>EnemyStatusState.Label(k)+"："+EnemyStatusState.EffectDescription(k)))+"\n\n能力・神器・スキルの効果は出撃時の育成を反映します。";
            }
            var style=new GUIStyle(text);style.normal.textColor=Color.white;
            float height=Mathf.Max(465,style.CalcHeight(new GUIContent(details),460)+20);
            battleDetailsScroll=GUI.BeginScrollView(new Rect(1060,184,490,480),battleDetailsScroll,new Rect(0,0,460,height));
            GUI.Label(new Rect(4,4,452,height-8),details,style);GUI.EndScrollView();
        }
        private void DrawBattleActions()
        {
            int actor=encounter.AvailableHero;if(actor<0)return;var hero=encounter.State.Heroes[actor];
            GrowthFill(20,437,850,263,new Color(.025f,.05f,.065f,.92f));
            Label(34,446,570,35,encounter.HeroineName(actor)+" ／ スキルを選択",text,gold);
            bool enabled=BattleInputAllowed && !encounter.Acted[actor] && hero.IsAlive;
            if(encounter.UsesJobRulesV2)DrawJobControls(actor,enabled);
            else if(encounter.UsesOptionalResourceBoost){
                bool previous=GUI.enabled;GUI.enabled=enabled && hero.JobResource>0;
                var toggleStyle=new GUIStyle(GUI.skin.toggle){font=small.font,fontSize=18,wordWrap=false};
                toggleStyle.normal.textColor=ivory;toggleStyle.onNormal.textColor=ivory;toggleStyle.hover.textColor=gold;toggleStyle.onHover.textColor=gold;
                encounter.ResourceBoostSelected=GUI.Toggle(new Rect(35,484,815,32),encounter.ResourceBoostSelected,(encounter.ResourceBoostSelected?"［強化 ON］ ":"［強化 OFF］ ")+encounter.ResourceName(actor)+"を使って強化　所持 "+hero.JobResource+" / "+hero.JobResourceMax+"　1個につき+10%（任意）",toggleStyle);
                GUI.enabled=previous;
            }
            for(int slot=0;slot<3;slot++){
                int cost=encounter.SkillResourceCost(actor,slot);var healing=encounter.HealingSkill(actor,slot);
                string description=encounter.IsSelfBuff(actor,slot)?encounter.SelfBuffDescription(actor,slot):healing!=null?encounter.HealingDescription(actor,slot):encounter.AttackTargetDescription(actor,slot)+"予測 "+encounter.PreviewDamage(actor,slot,target);
                string caption=encounter.SkillName(actor,slot)+"\n"+encounter.SkillAttributes(actor,slot)+"\n"+description+"\n"+(cost>0?encounter.ResourceName(actor)+" "+cost+"消費・強化":"リソース消費なし")+"\n"+encounter.TimingDescription(actor,slot);
                var style=new GUIStyle(skillButton){fontSize=16};while(style.fontSize>11 && style.CalcHeight(new GUIContent(caption),253)>(encounter.UsesJobRulesV2?100:120))style.fontSize--;
                if(Btn(34+slot*280,encounter.UsesJobRulesV2?543:524,270,encounter.UsesJobRulesV2?104:122,caption,enabled && encounter.ConditionsSatisfied(actor,slot),style))ChooseBattleSkill(slot);
            }
            if(Btn(34,653,540,37,"対象："+BattleTargetName(target)+" ／ 対象を変更",enabled))battlePanel=BattlePanel.Targets;
            if(Btn(590,653,274,37,encounter.RequiresPanzerDefense(actor)?"防御・装甲を待つ":"パス",enabled && (encounter.RequiresPanzerDefense(actor) || encounter.CanPass(actor)))){ResetBattleMenu();if(encounter.RequiresPanzerDefense(actor))encounter.DefendPanzer(actor);else encounter.Pass();QueueBattleEvents();}
        }
        private bool ChooseBattleSkill(int slot)
        {
            if(!BattleInputAllowed || slot<0 || slot>2)return false;
            int actor=encounter.AvailableHero;if(actor<0)return false;var hero=encounter.State.Heroes[actor];
            if(!hero.IsAlive || encounter.Acted[actor] || hero.JobResource<encounter.SkillResourceCost(actor,slot) || !encounter.ConditionsSatisfied(actor,slot))return false;
            battlePanel=BattlePanel.None;
            if(encounter.HealingSkill(actor,slot)!=null && !hero.Status.Active("jamming")){healingActor=actor;healingSlot=slot;selectingAlly=true;selectedAllies.Clear();}
            else Act(actor,slot);
            return true;
        }
        private void DrawBattleTargets()
        {
            var parts=encounter.State.Parts;var visual=playback.Current;
            if(Btn(1060,184,490,49,(target=="body"?"◆ ":"")+"本体",BattleInputAllowed)){target="body";battlePanel=BattlePanel.None;}
            float row=Mathf.Min(62,375f/parts.Count);
            for(int i=0;i<parts.Count;i++){
                var part=parts[i];int hp=visual?.PartHp[i]??part.HitPoints;
                string caption=(target==part.Id?"◆ ":"")+ColossusCombatCatalog.PartName(part,i)+" ／ "+(hp==0?"破壊済み":"HP "+hp);
                if(Btn(1060,242+i*row,490,row-5,caption,BattleInputAllowed && hp>0)){target=part.Id;battlePanel=BattlePanel.None;}
            }
            int index=parts.ToList().FindIndex(p=>p.Id==target);
            string effect=index<0?"防御部位を壊すと本体ダメージが増加":ColossusCombatCatalog.PartEffect(parts[index]);
            string details=effect+"\n"+CombatAttributeRules.Describe(encounter.State.AttributeResistances)+"\n"+(visual?.EnemyStatuses[index+1]??encounter.EnemyStatusDescription(target));
            var style=new GUIStyle(small);while(style.fontSize>13 && style.CalcHeight(new GUIContent(details),490)>72)style.fontSize--;
            Label(1060,624,490,76,details,style,Color.white);
        }
        private void PrepareBattleMenuCapture(string scenario)
        {
            if(!formalDiagnostic || capturePath==null || encounter==null)throw new InvalidOperationException("Battle menu capture requires isolated battle");
            string before=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot),state=PlaybackState(encounter);
            ResetBattleMenu();OpenBattlePanel(BattlePanel.Actions);FocusCheck(battlePanel==BattlePanel.Actions,"commands open on demand");
            OpenBattlePanel(BattlePanel.Targets);FocusCheck(battlePanel==BattlePanel.Targets,"exclusive battle drawer");
            FocusCheck(CloseBattleMenuLayer() && battlePanel==BattlePanel.None,"Escape closes battle drawer");
            battleMenuExpanded=true;FocusCheck(CloseBattleMenuLayer() && !battleMenuExpanded,"Escape collapses battle toolbar");
            help=true;OpenBattlePanel(BattlePanel.Actions);FocusCheck(battlePanel==BattlePanel.None && !CloseBattleMenuLayer(),"help blocks underlying battle menus");help=false;
            paused=true;OpenBattlePanel(BattlePanel.Targets);FocusCheck(battlePanel==BattlePanel.None,"paused battle rejects target input");paused=false;
            selectingAlly=true;OpenBattlePanel(BattlePanel.Status);FocusCheck(battlePanel==BattlePanel.None,"ally selection blocks battle menus");selectingAlly=false;
            paused=true;FocusCheck(!ChooseBattleSkill(0),"paused command is rejected without consuming resources");paused=false;
            FocusCheck(before==UnityFormalCampaignJson.Encode(formalCampaign.Snapshot) && state==PlaybackState(encounter),"menu open/close leaves HP, resources and save unchanged");
            while(encounter.AvailableHero!=0 && !encounter.Ended)encounter.Pass();encounter.DrainPresentationEvents();SelectNextHero();
            ResetBattleMenu();
            if(scenario=="expanded")battleMenuExpanded=true;
            else if(scenario=="actions")OpenBattlePanel(BattlePanel.Actions);
            else if(scenario=="targets")OpenBattlePanel(BattlePanel.Targets);
            else if(scenario=="status")OpenBattlePanel(BattlePanel.Status);
            else if(scenario=="timeline")OpenBattlePanel(BattlePanel.Timeline);
            else if(scenario=="paused")paused=true;
            else if(scenario=="broken"){encounter.State.BreakPart(encounter.State.Parts[0].Id,int.MaxValue);target="body";OpenBattlePanel(BattlePanel.Targets);}
            else if(scenario=="playing"){
                OpenBattlePanel(BattlePanel.Actions);FocusCheck(ChooseBattleSkill(0) && playback.Busy && battlePanel==BattlePanel.None,"command uses real combat queue and closes drawer");
                FocusCheck(!ChooseBattleSkill(0),"playback rejects repeated command");paused=true;
            }else if(scenario=="healing"){
                // The current formal roster has no healing-confirmation skill. Test the existing
                // selection contract with an explicit isolated fixture, never add a formal skill.
                encounter=new PlayableBattle(1,campaign.Playable,73491,new[]{new HealingSkillDefinition(0,0,"回復対象の確認（操作検証）",HealingTargetRule.SelectedAllies,2,3,35,1m)});
                selectedHero=0;target="body";
                string beforeHeal=PlaybackState(encounter);FocusCheck(ChooseBattleSkill(0) && selectingAlly && battlePanel==BattlePanel.None && beforeHeal==PlaybackState(encounter),"healing fixture opens confirmation without consuming resources");
                Debug.Log("BATTLE_MENU_HEALING_FIXTURE selected-allies=2 / formal roster has no healing-confirmation skill");
            }
            else if(scenario!="closed")throw new ArgumentException("Unknown battle menu scenario: "+scenario);
            Debug.Log("BATTLE_MENU_CAPTURE_PASS "+scenario+" / isolated / art-first");
        }
    }
}
