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
        private Texture2D AdvTexture(string assetId)
        {
            var asset=HomeData().assets.SingleOrDefault(a=>a.id==assetId);
            return asset!=null && !string.IsNullOrWhiteSpace(asset.resourcePath)?Resources.Load<Texture2D>(asset.resourcePath):null;
        }
        private void DrawAdvActor(AdvActorState actor)
        {
            var slot=HomeData().actorSlots.Single(s=>s.id==actor.SlotId);
            float w=slot.size01.x*1600,h=slot.size01.y*900,x=slot.anchor.x*1600-slot.pivot.x*w,y=slot.anchor.y*900-slot.pivot.y*h;
            var rect=new Rect(x,y,w,Math.Max(1,Math.Min(h,525-y)));var standing=AdvTexture(actor.StandingAsset);var expression=AdvTexture(actor.ExpressionAsset);
            var expressionDef=HomeData().assets.SingleOrDefault(a=>a.id==actor.ExpressionAsset);
            if(expression!=null && standing!=null && (expression.width!=standing.width || expression.height!=standing.height))expression=null;
            bool fullExpression=expression!=null && expressionDef.fullFrame;
            if(standing!=null)DrawExpressionLayer(rect,fullExpression?expression:standing,expression,expressionDef?.regionalOverlay==true?expressionDef.overlayRegion01:null);
            else GrowthFill(x,y,w,h,new Color(.22f,.38f,.42f));
            if(standing!=null && expression!=null && !fullExpression && expressionDef?.regionalOverlay!=true && expression.width==standing.width && expression.height==standing.height)GUI.DrawTexture(rect,expression,ScaleMode.ScaleToFit,true);
            if(standing==null){Label(x+15,y+70,w-30,110,combatDefinitions.Hero(actor.HeroineId).name,growthTextStyle);Label(x+15,y+200,w-30,110,actor.PosePlaceholder?"未対応pose\n同人物の仮表示":"静的検証用\n立ち絵",growthSmallStyle);}
        }
        // The source PNGs remain untouched. All pixels outside the authored patch use the standing image.
        private static void DrawExpressionLayer(Rect destination,Texture2D standing,Texture2D expression,HomeRect region,HomeRect crop=null)
        {
            if(standing==null)return;
            if(crop==null){
                float scale=Math.Min(destination.width/standing.width,destination.height/standing.height);
                destination=new Rect(destination.x+(destination.width-standing.width*scale)/2,destination.y+(destination.height-standing.height*scale)/2,standing.width*scale,standing.height*scale);
                crop=new HomeRect{x=0,y=0,width=1,height=1};
            }
            GUI.BeginGroup(destination);
            try{
                GUI.DrawTextureWithTexCoords(new Rect(0,0,destination.width,destination.height),standing,new Rect(crop.x,1-crop.y-crop.height,crop.width,crop.height),true);
                if(expression!=null && region!=null && expression.width==standing.width && expression.height==standing.height){
                    var patch=new Rect((region.x-crop.x)/crop.width*destination.width,(region.y-crop.y)/crop.height*destination.height,region.width/crop.width*destination.width,region.height/crop.height*destination.height);
                    GUI.DrawTextureWithTexCoords(patch,expression,new Rect(region.x,1-region.y-region.height,region.width,region.height),true);
                }
            }finally{GUI.EndGroup();}
        }
        private void SyncAdvAudio()
        {
            if(adv==null)return;
            if(advBgm==null){advBgm=gameObject.AddComponent<AudioSource>();advSe=gameObject.AddComponent<AudioSource>();advBgm.playOnAwake=false;advSe.playOnAwake=false;}
            advBgm.volume=ArtSampleSettings.Bgm;advSe.volume=ArtSampleSettings.Se;
            if(advSoundRevision!=adv.SoundRevision){advSoundRevision=adv.SoundRevision;var sound=HomeData().assets.SingleOrDefault(a=>a.id==adv.SoundId);var clip=Resources.Load<AudioClip>(sound?.resourcePath??"");if(clip!=null){var channel=adv.SoundChannel=="bgm"?advBgm:advSe;channel.clip=clip;channel.loop=adv.SoundChannel=="bgm";channel.Play();}}
            if(adv.Paused && !advAudioPaused){advBgm.Pause();advSe.Pause();advAudioPaused=true;}else if(!adv.Paused && advAudioPaused){advBgm.UnPause();advSe.UnPause();advAudioPaused=false;}
        }
        private void BeginAdv(string source,bool replay)
        {
            if(!formalDiagnostic || !BookInputAllowed)return;var c=HomeData();var snapshot=formalCampaign.Snapshot;var e=c.events.SingleOrDefault(x=>x.id==source);var ch=c.chapters.SingleOrDefault(x=>x.id==source);
            bool unlocked=e!=null?(snapshot.home?.unlockedEventIds.Contains(source)??false):ch!=null && snapshot.world.unlockedStoryIds.Contains(source);bool read=e!=null?(snapshot.home?.readEventIds.Contains(source)??false):snapshot.world.readStoryIds.Contains(source);
            if(!unlocked || replay && !read)return;
            adv=new AdvSession(c,e?.sceneId??ch.sceneId,source,replay,snapshot.home?.readLineKeys);adv.SetSpeed(PlayerPrefs.GetInt("plan6.text-speed",30));advSavedLines=0;advRequest=null;advPendingLine=null;advBacklog=false;advHelp=false;advError=null;advScroll=Vector2.zero;
            advSoundRevision=0;advAudioPaused=false;SyncAdvAudio();advBgm.clip=Resources.Load<AudioClip>("Audio/candidate-bgm");advBgm.loop=true;if(advBgm.clip!=null)advBgm.Play();
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
            var background=AdvTexture(adv.BackgroundId);if(background!=null)GUI.DrawTexture(new Rect(0,100,1600,425),background,ScaleMode.ScaleAndCrop);
            Label(45,25,1490,50,"機能検証用ADV ／ 正式本文未制作・美術候補"+(adv.Replay?" ／ 回想・読み取り専用":""),growthTitleStyle);
            GrowthFill(0,80,1600,40,navy);
            Label(45,85,1490,35,"背景："+adv.BackgroundId+" ／ BGM・SE：見本用の合成音候補",growthSmallStyle);
            if(adv.CgId!=null){var cg=AdvTexture(adv.CgId);if(cg!=null)GUI.DrawTexture(new Rect(280,115,1040,410),cg,ScaleMode.ScaleToFit);else{GrowthFill(280,160,1040,340,new Color(.31f,.24f,.35f));Label(420,300,760,60,"CG未制作・素材欠落",growthTitleStyle);}}
            if(!adv.HideActors)foreach(var actor in adv.Actors.OrderBy(a=>HomeData().actorSlots.Single(s=>s.id==a.SlotId).drawOrder))DrawAdvActor(actor);
            var bodyStyle=new GUIStyle(growthTextStyle){fontSize=ArtSampleSettings.LargeText?27:23};
            GrowthFrame(40,525,1520,245);Label(70,540,1440,45,adv.SpeakerId==null?"地の文":combatDefinitions.Hero(adv.SpeakerId).name,growthTextStyle);Label(70,600,1440,150,adv.VisibleText,bodyStyle);
            if(adv.EndReached){Label(70,605,1440,80,adv.Completed?(adv.Replay?"回想が終了しました。進行は変更していません。":"読了の保存が完了しました。"):advError??"endに到達しました。読了は保存成功後に確定します。",growthTextStyle);
                if(GrowthButton(55,790,730,58,adv.Completed?"本へ戻る":adv.Replay?"回想を終了する":"読了を保存する",true,true)){if(adv.Completed){CloseAdv();return;}else CompleteAdv();}}
            else if(GrowthButton(55,790,300,58,adv.Paused?"手動で再開":adv.FullyVisible?"次の行へ":"全文を表示",!formalCampaign.HasPending && advRequest==null,true)){if(adv.Paused)adv.Resume();else AdvanceAdv();}
            if(!adv.EndReached){if(GrowthButton(375,790,180,58,"バックログ")){advBacklog=true;adv.Pause();}if(GrowthButton(570,790,140,58,adv.Auto?"auto停止":"auto")){if(adv.Paused)adv.Resume();adv.SetAuto(!adv.Auto);}if(GrowthButton(725,790,150,58,"既読skip")){if(adv.Paused)adv.Resume();adv.SetSkip(!adv.Skip);}
                if(GrowthButton(890,790,180,58,"速度 "+adv.CharactersPerSecond)){int[] speeds={15,30,60,120};int speed=speeds[(Array.IndexOf(speeds,adv.CharactersPerSecond)+1)%4];adv.SetSpeed(speed);PlayerPrefs.SetInt("plan6.text-speed",speed);}
                if(GrowthButton(1085,790,160,58,"操作説明")){advHelp=true;adv.Pause();}if(GrowthButton(1260,790,285,58,"中断して本へ",!formalCampaign.HasPending && advRequest==null)){CloseAdv();return;}}
            if(advRequest!=null){GrowthFill(40,470,1520,55,navy);Label(55,480,1000,40,advError??"保存待ち",growthSmallStyle);if(GrowthButton(1100,475,440,45,"同じ内容で保存を再試行")){if(advPendingLine!=null)PersistAdvLine();else CompleteAdv();}}
            GUI.enabled=previousAdvEnabled;
            if(advBacklog || advHelp){GrowthFrame(180,140,1240,610);if(advHelp)Label(220,225,1150,340,"一回目は全文表示、次の入力で行を送ります。\n既読skipは最初の未読で停止します。\nバックログ・説明・非アクティブ中はタイマーを停止します。\n閉じた後は手動で再開してください。\n中断後の再開はsceneの先頭。保存済みの行既読だけを保持します。",growthTextStyle);
                else{advScroll=GUI.BeginScrollView(new Rect(220,210,1150,430),advScroll,new Rect(0,0,1110,Math.Max(430,adv.Backlog.Count*180)));for(int i=0;i<adv.Backlog.Count;i++)Label(10,i*180,1080,175,adv.Backlog[i],bodyStyle);GUI.EndScrollView();}
                if(GrowthButton(220,675,1150,50,"閉じる ／ 本文は停止したまま")){advBacklog=false;advHelp=false;}}
        }
    }
}
