using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private AdvSession adv;private FormalHomeRequest advRequest;private HomeReadLine advPendingLine;private int advSavedLines;private bool advBacklog,advHelp;private Vector2 advScroll;private string advError;
        private AudioSource advBgm,advSe;private int advSoundRevision;private bool advAudioPaused;
        private void SyncAdvAudio()
        {
            if(adv==null)return;
            if(advBgm==null){advBgm=gameObject.AddComponent<AudioSource>();advSe=gameObject.AddComponent<AudioSource>();advBgm.playOnAwake=false;advSe.playOnAwake=false;}
            if(advSoundRevision!=adv.SoundRevision){advSoundRevision=adv.SoundRevision;var clip=Resources.Load<AudioClip>(adv.SoundId??"");if(clip!=null){var channel=adv.SoundChannel=="bgm"?advBgm:advSe;channel.clip=clip;channel.loop=adv.SoundChannel=="bgm";channel.Play();}}
            if(adv.Paused && !advAudioPaused){advBgm.Pause();advSe.Pause();advAudioPaused=true;}else if(!adv.Paused && advAudioPaused){advBgm.UnPause();advSe.UnPause();advAudioPaused=false;}
        }
        private void BeginAdv(string source,bool replay)
        {
            if(!formalDiagnostic || !BookInputAllowed)return;var c=HomeData();var snapshot=formalCampaign.Snapshot;var e=c.events.SingleOrDefault(x=>x.id==source);var ch=c.chapters.SingleOrDefault(x=>x.id==source);
            bool unlocked=e!=null?(snapshot.home?.unlockedEventIds.Contains(source)??false):ch!=null && snapshot.world.unlockedStoryIds.Contains(source);bool read=e!=null?(snapshot.home?.readEventIds.Contains(source)??false):snapshot.world.readStoryIds.Contains(source);
            if(!unlocked || replay && !read)return;
            adv=new AdvSession(c,e?.sceneId??ch.sceneId,source,replay,snapshot.home?.readLineKeys);adv.SetSpeed(PlayerPrefs.GetInt("plan6.text-speed",30));advSavedLines=0;advRequest=null;advPendingLine=null;advBacklog=false;advHelp=false;advError=null;advScroll=Vector2.zero;
            advSoundRevision=0;advAudioPaused=false;SyncAdvAudio();
            if(ch!=null && book.Bookmark==BookBookmark.Stories)book.BeginReading(ch.id,3);
        }
        private void PersistAdvLine()
        {
            if(adv==null || adv.Replay || advSavedLines>=adv.NewlyRead.Count)return;
            if(advRequest==null){advPendingLine=adv.NewlyRead[advSavedLines];advRequest=new FormalHomeRequest(Guid.NewGuid().ToString("N"),"advRead",formalCampaign.Snapshot.revision,HomeData().contentVersion,advPendingLine.sceneId+"/"+advPendingLine.scriptVersion+"/"+advPendingLine.lineId);}
            try{if(formalCampaign.CommitAdvLine(advRequest,HomeData(),advPendingLine,formalDiagnostic?SaveDiagnosticCampaign:formalCampaignStore.Save)==GrowthCommitResult.SaveFailed){advError="行既読を保存できません。同じ内容で再試行してください。";adv.Pause();return;}advSavedLines++;advRequest=null;advPendingLine=null;advError=null;}
            catch(Exception e){advError="行既読の保存を停止しました："+e.Message;adv.Pause();}
        }
        private void CompleteAdv()
        {
            if(adv==null || !adv.EndReached || adv.Completed)return;
            if(adv.Replay){adv.MarkCommitted();return;}
            if(advSavedLines<adv.NewlyRead.Count){PersistAdvLine();return;}
            if(advRequest==null)advRequest=new FormalHomeRequest(Guid.NewGuid().ToString("N"),"sceneEnd",formalCampaign.Snapshot.revision,HomeData().contentVersion,adv.SourceId+"/"+adv.SceneId+"/"+adv.ScriptVersion);
            try{if(formalCampaign.CommitAdvEnd(advRequest,HomeData(),adv,formalDiagnostic?SaveDiagnosticCampaign:formalCampaignStore.Save)==GrowthCommitResult.SaveFailed){advError="読了を保存できません。同じ候補を再保存します。";return;}adv.MarkCommitted();advRequest=null;advError=null;campaign=new CampaignState(NewAster.Data.WorldCatalog.ColossusIds,formalCampaign.Snapshot.world);}
            catch(Exception e){advError="読了の保存を停止しました："+e.Message;}
        }
        private void AdvanceAdv(){if(adv==null || formalCampaign.HasPending || advRequest!=null)return;adv.Advance();PersistAdvLine();SyncAdvAudio();}
        private void CloseAdv(){if(adv==null || formalCampaign.HasPending || advRequest!=null)return;book.EndReading();adv=null;advBacklog=false;advHelp=false;if(advBgm!=null){advBgm.Stop();advSe.Stop();}}
        private void UpdateAdv()
        {
            if(adv==null)return;if(!Application.isFocused || advBacklog || advHelp || formalCampaign.HasPending || advRequest!=null){adv.Pause();SyncAdvAudio();return;}adv.Tick(Time.unscaledDeltaTime);PersistAdvLine();SyncAdvAudio();
        }
        private void DrawAdv()
        {
            bool previousAdvEnabled=GUI.enabled;GUI.enabled=previousAdvEnabled && !advBacklog && !advHelp;
            drawingModal=true;GrowthStyles();GrowthFill(0,0,1600,900,new Color(.09f,.17f,.22f));
            Label(45,25,1490,50,"機能検証用ADV ／ 正式本文・美術は未制作"+(adv.Replay?" ／ 回想・読み取り専用":""),growthTitleStyle);
            Label(45,85,1490,35,"背景："+adv.BackgroundId+" ／ 音：未制作・継続",growthSmallStyle);
            if(adv.CgId!=null){GrowthFill(280,160,1040,340,new Color(.31f,.24f,.35f));Label(420,300,760,60,"CG動作確認用の仮表示",growthTitleStyle);}
            if(!adv.HideActors)foreach(var actor in adv.Actors.OrderBy(a=>HomeData().actorSlots.Single(s=>s.id==a.SlotId).drawOrder)){var slot=HomeData().actorSlots.Single(s=>s.id==actor.SlotId);float w=slot.size01.x*1600,h=slot.size01.y*900,x=slot.anchor.x*1600-slot.pivot.x*w,y=slot.anchor.y*900-slot.pivot.y*h;var rect=new Rect(x,y,w,h);var standing=Resources.Load<Texture2D>(actor.StandingAsset);if(standing!=null)GUI.DrawTexture(rect,standing,ScaleMode.ScaleToFit,true);else GrowthFill(x,y,w,h,new Color(.22f,.38f,.42f));Label(x+15,y+70,w-30,110,combatDefinitions.Hero(actor.HeroineId).name,growthTextStyle);Label(x+15,y+200,w-30,110,actor.PosePlaceholder?"未対応pose\n同人物の仮表示":"静的検証用\n立ち絵",growthSmallStyle);}
            GrowthFrame(40,525,1520,245);Label(70,540,1440,45,adv.SpeakerId==null?"地の文":combatDefinitions.Hero(adv.SpeakerId).name,growthTextStyle);Label(70,600,1440,150,adv.VisibleText,growthTextStyle);
            if(adv.EndReached){Label(70,605,1440,80,adv.Completed?(adv.Replay?"回想が終了しました。進行は変更していません。":"読了の保存が完了しました。"):advError??"endに到達しました。読了は保存成功後に確定します。",growthTextStyle);
                if(GrowthButton(55,790,730,58,adv.Completed?"本へ戻る":adv.Replay?"回想を終了する":"読了を保存する",true,true)){if(adv.Completed){CloseAdv();return;}else CompleteAdv();}}
            else if(GrowthButton(55,790,300,58,adv.Paused?"手動で再開":adv.FullyVisible?"次の行へ":"全文を表示",!formalCampaign.HasPending && advRequest==null,true)){if(adv.Paused)adv.Resume();else AdvanceAdv();}
            if(!adv.EndReached){if(GrowthButton(375,790,180,58,"バックログ")){advBacklog=true;adv.Pause();}if(GrowthButton(570,790,140,58,adv.Auto?"auto停止":"auto")){if(adv.Paused)adv.Resume();adv.SetAuto(!adv.Auto);}if(GrowthButton(725,790,150,58,"既読skip")){if(adv.Paused)adv.Resume();adv.SetSkip(!adv.Skip);}
                if(GrowthButton(890,790,180,58,"速度 "+adv.CharactersPerSecond)){int[] speeds={15,30,60,120};int speed=speeds[(Array.IndexOf(speeds,adv.CharactersPerSecond)+1)%4];adv.SetSpeed(speed);PlayerPrefs.SetInt("plan6.text-speed",speed);}
                if(GrowthButton(1085,790,160,58,"操作説明")){advHelp=true;adv.Pause();}if(GrowthButton(1260,790,285,58,"中断して本へ",!formalCampaign.HasPending && advRequest==null)){CloseAdv();return;}}
            if(advRequest!=null){GrowthFill(40,470,1520,55,navy);Label(55,480,1000,40,advError??"保存待ち",growthSmallStyle);if(GrowthButton(1100,475,440,45,"同じ内容で保存を再試行")){if(advPendingLine!=null)PersistAdvLine();else CompleteAdv();}}
            GUI.enabled=previousAdvEnabled;
            if(advBacklog || advHelp){GrowthFrame(180,140,1240,610);if(advHelp)Label(220,225,1150,340,"一回目は全文表示、次の入力で行を送ります。\n既読skipは最初の未読で停止します。\nバックログ・説明・非アクティブ中はタイマーを停止します。\n閉じた後は手動で再開してください。\n中断後の再開はsceneの先頭。保存済みの行既読だけを保持します。",growthTextStyle);
                else{advScroll=GUI.BeginScrollView(new Rect(220,210,1150,430),advScroll,new Rect(0,0,1110,Math.Max(430,adv.Backlog.Count*180)));for(int i=0;i<adv.Backlog.Count;i++)Label(10,i*180,1080,175,adv.Backlog[i],growthTextStyle);GUI.EndScrollView();}
                if(GrowthButton(220,675,1150,50,"閉じる ／ 本文は停止したまま")){advBacklog=false;advHelp=false;}}
        }
    }
}
