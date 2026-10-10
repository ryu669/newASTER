using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private AdvSession adv;private FormalHomeRequest advRequest;private HomeReadLine advPendingLine;private int advSavedLines;private bool advBacklog,advHelp,advCgGallery;private Vector2 advScroll;private string advError;
        private AudioSource advBgm,advSe;private int advSoundRevision;private bool advAudioPaused;
        private BoundedCache<string,Texture2D> advImages;
        private Texture2D AdvTexture(string assetId)
        {
            var asset=HomeData().assets.SingleOrDefault(a=>a.id==assetId);
            if(asset==null || string.IsNullOrWhiteSpace(asset.resourcePath))return null;
            if(advImages==null)advImages=new BoundedCache<string,Texture2D>(24,ScheduleHeroineAssetCleanup);
            if(!advImages.TryGetValue(asset.resourcePath,out var texture)){texture=Resources.Load<Texture2D>(asset.resourcePath);advImages[asset.resourcePath]=texture;}
            return texture;
        }
        private void DrawAdvActor(AdvActorState actor)
        {
            var slot=HomeData().actorSlots.Single(s=>s.id==actor.SlotId);
            float w=slot.size01.x*1600,h=slot.size01.y*900,x=slot.anchor.x*1600-slot.pivot.x*w,y=slot.anchor.y*900-slot.pivot.y*h;
            var rect=new Rect(x,y,w,Math.Max(1,Math.Min(h,615-y)));var standing=AdvTexture(actor.StandingAsset);var expression=AdvTexture(actor.ExpressionAsset);
            var expressionDef=HomeData().assets.SingleOrDefault(a=>a.id==actor.ExpressionAsset);
            if(expression!=null && standing!=null && expressionDef?.fullFrame!=true && expressionDef?.mappedOverlay!=true && (expression.width!=standing.width || expression.height!=standing.height))expression=null;
            bool fullExpression=expression!=null && expressionDef.fullFrame;
            var display=HomeData().displays.Single(d=>d.heroineId==actor.HeroineId && d.outfitId==actor.OutfitId);
            var portraitCrop=display.portraitCrop01;
            bool mappedPortrait=display.usePortraitCrop && portraitCrop!=null && standing!=null;
            if(mappedPortrait){
                rect=new Rect(rect.center.x-325,rect.y,650,rect.height);
                if(actor.HeroineId=="heroine.nighthawk" || actor.HeroineId=="heroine.oriflamme" || actor.HeroineId=="heroine.shell" || actor.HeroineId=="heroine.r" || actor.HeroineId.StartsWith("heroine.annihilator",StringComparison.Ordinal))rect=new Rect(375,120,850,495);
                var face=expressionDef?.overlayRegion01;var source=expressionDef?.overlaySourceRegion01;
                DrawMappedExpression(rect,fullExpression?expression:standing,expressionDef?.mappedOverlay==true?expression:null,
                    face==null?new Rect():new Rect(face.x,face.y,face.width,face.height),source==null?new Rect():new Rect(source.x,source.y,source.width,source.height),new Rect(portraitCrop.x,portraitCrop.y,portraitCrop.width,portraitCrop.height));
            }
            else if(standing!=null && expressionDef?.mappedOverlay==true){
                var face=expressionDef.overlayRegion01;var source=expressionDef.overlaySourceRegion01;
                DrawMappedExpression(rect,standing,expression,new Rect(face.x,face.y,face.width,face.height),new Rect(source.x,source.y,source.width,source.height),new Rect(0,0,1,1));
            }
            else if(standing!=null)DrawExpressionLayer(rect,fullExpression?expression:standing,expression,expressionDef?.regionalOverlay==true?expressionDef.overlayRegion01:null);
            else GrowthFill(x,y,w,h,new Color(.22f,.38f,.42f));
            if(standing!=null && expression!=null && !fullExpression && expressionDef?.regionalOverlay!=true && expression.width==standing.width && expression.height==standing.height)GUI.DrawTexture(rect,expression,ScaleMode.ScaleToFit,true);
            if(standing==null){Label(x+15,y+70,w-30,110,combatDefinitions.Hero(actor.HeroineId).name,growthTextStyle);Label(x+15,y+200,w-30,110,actor.PosePlaceholder?"未対応pose\n同人物の仮表示":"静的検証用\n立ち絵",growthSmallStyle);}
        }
        // The source PNGs remain untouched. All pixels outside the authored patch use the standing image.
        private static void DrawExpressionLayer(Rect destination,Texture2D standing,Texture2D expression,HomeRect region,HomeRect crop=null)
        {
            if(standing==null)return;
            if(crop==null)crop=new HomeRect{x=0,y=0,width=1,height=1};
            destination=ContainImage(destination,standing.width*crop.width,standing.height*crop.height);
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
            if(advSoundRevision!=adv.SoundRevision){advSoundRevision=adv.SoundRevision;var sound=HomeData().assets.SingleOrDefault(a=>a.id==adv.SoundId);var clip=Resources.Load<AudioClip>(sound?.resourcePath??"");if(clip!=null){var channel=adv.SoundChannel=="bgm"?advBgm:advSe;channel.clip=clip;channel.loop=adv.SoundChannel=="bgm";channel.Play();TrialObserve("audio","adv-requested",adv.SoundId+";channel="+adv.SoundChannel);}}
            if(adv.Paused || !artHasFocus || !Application.isFocused){advBgm.Pause();advSe.Pause();advAudioPaused=true;}else if(advAudioPaused){advBgm.UnPause();advSe.UnPause();advAudioPaused=false;}
        }
        private void BeginAdv(string source,bool replay)
        {
            if(!HomeOperationsAllowed || !BookInputAllowed || !FlushSaveChanges())return;var c=HomeData();var snapshot=formalCampaign.Snapshot;var e=c.events.SingleOrDefault(x=>x.id==source);var ch=c.chapters.SingleOrDefault(x=>x.id==source);
            if(plan8StoryTrial && !HasTrialText(source))return;
            bool unlocked=e!=null?(snapshot.home?.unlockedEventIds.Contains(source)??false):ch!=null && snapshot.world.unlockedStoryIds.Contains(source);bool read=e!=null?(snapshot.home?.readEventIds.Contains(source)??false):snapshot.world.readStoryIds.Contains(source);
            if(!unlocked || replay && !read)return;
            adv=new AdvSession(c,e?.sceneId??ch.sceneId,source,replay,snapshot.home?.readLineKeys);adv.SetSpeed(PlayerPrefs.GetInt("plan6.text-speed",30));advSavedLines=0;advRequest=null;advPendingLine=null;advBacklog=false;advHelp=false;advCgGallery=false;advError=null;advScroll=Vector2.zero;
            if((plan8StoryTrial || ProductionStoryActive) && !replay)adv.ResumeAtFirstUnread();
            advSoundRevision=0;advAudioPaused=false;SyncAdvAudio();if(!ProductionStoryActive){advBgm.clip=Resources.Load<AudioClip>(ArtSampleSettings.AudioResource("bgm"));advBgm.loop=true;if(advBgm.clip!=null)advBgm.Play();}
            if(advBgm.clip!=null)TrialObserve("audio","adv-bgm-requested",advBgm.clip.name+";loop=true");
            TrialObserve("reading",replay?"replay-start":"start",source);
            if(ch!=null && book.Bookmark==BookBookmark.Stories)book.BeginReading(ch.id,3);
        }
        private void PersistAdvLine()
        {
            if(adv==null || adv.Replay || advSavedLines>=adv.NewlyRead.Count)return;
            if(advRequest==null){advPendingLine=adv.NewlyRead[advSavedLines];advRequest=new FormalHomeRequest(Guid.NewGuid().ToString("N"),"advRead",formalCampaign.Snapshot.revision,HomeData().contentVersion,advPendingLine.sceneId+"/"+advPendingLine.scriptVersion+"/"+advPendingLine.lineId);}
            try{if(formalCampaign.CommitAdvLine(advRequest,HomeData(),advPendingLine,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign)==GrowthCommitResult.SaveFailed){advError="行既読を保存できません。同じ内容で再試行してください。";adv.Pause();return;}advSavedLines++;advRequest=null;advPendingLine=null;advError=null;}
            catch(Exception e){advError="行既読の保存を停止しました："+e.Message;adv.Pause();}
        }
        private void CompleteAdv()
        {
            if(adv==null || !adv.EndReached || adv.Completed)return;
            if(adv.Replay){adv.MarkCommitted();return;}
            if(advSavedLines<adv.NewlyRead.Count){PersistAdvLine();return;}
            if(advRequest==null)advRequest=new FormalHomeRequest(Guid.NewGuid().ToString("N"),"sceneEnd",formalCampaign.Snapshot.revision,HomeData().contentVersion,adv.SourceId+"/"+adv.SceneId+"/"+adv.ScriptVersion);
            try{if(formalCampaign.CommitAdvEnd(advRequest,HomeData(),adv,formalDiagnostic?SaveDiagnosticCampaign:SaveTrialObservedCampaign)==GrowthCommitResult.SaveFailed){advError="読了を保存できません。同じ候補を再保存します。";return;}adv.MarkCommitted();advRequest=null;advError=null;campaign=new CampaignState(NewAster.Data.WorldCatalog.ColossusIds,formalCampaign.Snapshot.world);}
            catch(Exception e){advError="読了の保存を停止しました："+e.Message;}
        }
        private void AdvanceAdv(){if(adv==null || advCgGallery || formalCampaign.HasPending || advRequest!=null)return;adv.Advance();PersistAdvLine();SyncAdvAudio();}
        private void CloseAdv(){if(adv==null || formalCampaign.HasPending || advRequest!=null)return;TrialObserve("reading",adv.Completed?"completed":"interrupted",adv.SourceId);book.EndReading();adv=null;advBacklog=false;advHelp=false;advCgGallery=false;if(advBgm!=null){advBgm.Stop();advSe.Stop();}}
        private void UpdateAdv()
        {
            if(adv==null)return;if(!Application.isFocused || advBacklog || advHelp || advCgGallery || formalCampaign.HasPending || advRequest!=null){adv.Pause();SyncAdvAudio();return;}adv.Tick(Time.unscaledDeltaTime);PersistAdvLine();SyncAdvAudio();
        }
        private void DrawAdv()
        {
            bool previousAdvEnabled=GUI.enabled;GUI.enabled=previousAdvEnabled && !advBacklog && !advHelp;
            drawingModal=true;GrowthStyles();GrowthFill(0,0,1600,900,new Color(.09f,.17f,.22f));
            var background=AdvTexture(adv.BackgroundId);if(background!=null)GUI.DrawTexture(new Rect(0,100,1600,515),background,ScaleMode.ScaleAndCrop);
            bool hasCg=adv.CgId!=null;
            if(hasCg)
            {
                var cg=AdvTexture(adv.CgId);
                GrowthFill(0,0,1600,900,new Color(.035f,.035f,.045f));
                if(cg!=null)GUI.DrawTexture(new Rect(0,0,1600,900),cg,ScaleMode.ScaleToFit);
                else Label(420,300,760,60,"CG未制作・素材欠落",growthTitleStyle);
                if(advCgGallery)
                {
                    GUI.enabled=previousAdvEnabled;
                    var input=Event.current;
                    if(input.type==EventType.MouseDown || input.type==EventType.KeyDown && input.keyCode==KeyCode.Escape)
                    {advCgGallery=false;adv.Pause();input.Use();}
                    return;
                }
                if(GrowthButton(1330,14,225,42,"絵を全画面で見る",advRequest==null && !formalCampaign.HasPending))
                {advCgGallery=true;adv.SetAuto(false);adv.SetSkip(false);adv.Pause();SyncAdvAudio();GUI.enabled=previousAdvEnabled;return;}
            }
            else
            {
                Label(45,20,1490,45,ProductionStoryActive?ProductionStoryTitle(adv.SourceId):"物語",growthTitleStyle);
                if(!adv.HideActors)foreach(var actor in adv.Actors.OrderBy(a=>HomeData().actorSlots.Single(s=>s.id==a.SlotId).drawOrder))DrawAdvActor(actor);
            }
            var bodyStyle=new GUIStyle(growthTextStyle){fontSize=ArtSampleSettings.LargeText?27:23};
            if(hasCg)GrowthFill(40,615,1520,205,new Color(.035f,.055f,.08f,.86f));else GrowthFrame(40,615,1520,205);if(adv.SpeakerId!=null)Label(70,630,1440,40,combatDefinitions.Hero(adv.SpeakerId).name,growthTextStyle);Label(70,adv.SpeakerId==null?635:675,1440,adv.SpeakerId==null?165:125,adv.VisibleText,bodyStyle);
            if(adv.EndReached){if(advError!=null)Label(70,675,1440,80,advError,growthTextStyle);
                if(GrowthButton(55,840,730,48,adv.Completed?"本へ戻る":adv.Replay?"回想を終了する":"読了を保存する",true,true)){if(adv.Completed){CloseAdv();return;}else CompleteAdv();}}
            else if(GrowthButton(55,840,300,48,adv.Paused?"手動で再開":adv.FullyVisible?"次の行へ":"全文を表示",!formalCampaign.HasPending && advRequest==null,true)){if(adv.Paused)adv.Resume();else AdvanceAdv();}
            if(!adv.EndReached){if(GrowthButton(375,840,180,48,"バックログ")){advBacklog=true;adv.Pause();}if(GrowthButton(570,840,140,48,adv.Auto?"auto停止":"auto")){if(adv.Paused)adv.Resume();adv.SetAuto(!adv.Auto);}if(GrowthButton(725,840,150,48,"既読skip")){if(adv.Paused)adv.Resume();adv.SetSkip(!adv.Skip);}
                if(GrowthButton(890,840,180,48,"速度 "+adv.CharactersPerSecond)){int[] speeds={15,30,60,120};int speed=speeds[(Array.IndexOf(speeds,adv.CharactersPerSecond)+1)%4];adv.SetSpeed(speed);PlayerPrefs.SetInt("plan6.text-speed",speed);RequestSettingsSave();}
                if(GrowthButton(1085,840,160,48,"操作説明")){advHelp=true;adv.Pause();}if(GrowthButton(1260,840,285,48,"中断して本へ",!formalCampaign.HasPending && advRequest==null)){CloseAdv();return;}}
            if(advRequest!=null){GrowthFill(40,470,1520,55,navy);Label(55,480,1000,40,advError??"保存待ち",growthSmallStyle);if(GrowthButton(1100,475,440,45,"同じ内容で保存を再試行")){if(advPendingLine!=null)PersistAdvLine();else CompleteAdv();}}
            GUI.enabled=previousAdvEnabled;
            if(advBacklog || advHelp){GrowthFrame(180,140,1240,610);if(advHelp)Label(220,225,1150,340,"一回目は全文表示、次の入力で行を送ります。\n既読skipは最初の未読で停止します。\nバックログ・説明・非アクティブ中はタイマーを停止します。\n閉じた後は手動で再開してください。\n"+((plan8StoryTrial || ProductionStoryActive)?"中断後は保存済みの最初の未読行から再開します。回想は先頭からです。":"中断後の再開はsceneの先頭。保存済みの行既読だけを保持します。"),growthTextStyle);
                else{advScroll=GUI.BeginScrollView(new Rect(220,210,1150,430),advScroll,new Rect(0,0,1110,Math.Max(430,adv.Backlog.Count*180)));for(int i=0;i<adv.Backlog.Count;i++)Label(10,i*180,1080,175,adv.Backlog[i],bodyStyle);GUI.EndScrollView();}
                if(GrowthButton(220,675,1150,50,"閉じる ／ 本文は停止したまま")){advBacklog=false;advHelp=false;advCgGallery=false;}}
        }
    }
}
