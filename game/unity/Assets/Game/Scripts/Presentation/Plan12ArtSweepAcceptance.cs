using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private sealed class ArtSweepItem { public string hero,view,source; }
        [Serializable] private sealed class ArtSweepRecord { public string heroineId,view,source,image; }
        [Serializable] private sealed class ArtSweepReport { public ArtSweepRecord[] records; }
        private List<ArtSweepItem> artSweep;
        private readonly List<ArtSweepRecord> artSweepRecords=new List<ArtSweepRecord>();
        private int artSweepIndex=-1,artSweepFrame;
        private float artSweepReadyAt;
        private bool artSweepScreenshot,artSweepGalleryOnly;
        private string artSweepDirectory,artSweepImage,artSweepSd;
        private void PrepareArtSweep(string[] args)
        {
            if(!args.Contains("-plan12ArtSweep"))return;
            if(capturePath==null || !plan10UiCapture || !formalDiagnostic)throw new ArgumentException("Art sweep requires isolated Plan10 UI capture.");
            artSweepDirectory=Path.GetDirectoryName(Path.GetFullPath(capturePath));
            File.WriteAllText(Path.Combine(artSweepDirectory,"home-catalog.json"),JsonUtility.ToJson(HomeData(),true));
            var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
            save.growth.heroines=save.growth.heroines.Concat(combatDefinitions.HeroineIds.Except(save.growth.heroines.Select(h=>h.heroineId)).Select(id=>new FormalHeroineGrowth{heroineId=id,level=20})).ToArray();
            save.world.poemIds=ProductionStoryData().chapters.SelectMany(c=>c.poems).Select(p=>p.id).ToArray();
            save.home.weaponNodeIds=HomeData().weaponNodes.Select(n=>n.id).ToArray();
            save.home.weaponLevels=HomeData().weaponNodes.Select(n=>new HomeWeaponLevel{nodeId=n.id,level=7}).ToArray();
            HomeConditions.Refresh(save,HomeData());save.Validate();BindFormalCampaign(save);
            artSweepGalleryOnly=args.Contains("-plan12CgGallerySweep");
            artSweep=new List<ArtSweepItem>();
            foreach(var hero in combatDefinitions.HeroineIds){
                if(!artSweepGalleryOnly)foreach(var view in new[]{"detail","tree","expression.normal","expression.joy","expression.puzzled","expression.determined","sd.idle","sd.walk","sd.talk","sd.react","sd.sit","sd.work","sd.look","battle"})artSweep.Add(new ArtSweepItem{hero=hero,view=view});
                foreach(var e in ProductionStoryData().events.Where(e=>e.ownerId==hero))artSweep.Add(new ArtSweepItem{hero=hero,view="cg."+e.id.Split('.').Last(),source=e.id});
            }
            int heroFilter=Array.IndexOf(args,"-plan12ArtHero"),viewFilter=Array.IndexOf(args,"-plan12ArtViews");
            if(heroFilter>=0)artSweep=artSweep.Where(item=>item.hero=="heroine."+args[heroFilter+1]).ToList();
            if(viewFilter>=0){var views=args[viewFilter+1].Split(',');artSweep=artSweep.Where(item=>views.Contains(item.view)).ToList();}
            if(artSweep.Count==0)throw new ArgumentException("Art sweep filter matched no pending views.");
            artSweepReadyAt=Time.realtimeSinceStartup+8;
            Debug.Log("PLAN12_ART_SWEEP_READY forms="+combatDefinitions.HeroineIds.Length+" captures="+artSweep.Count+" isolated=true expressionVisibility=diagnostic sdPose=forced");
        }
        private bool UpdateArtSweep()
        {
            if(artSweep==null)return false;
            if(Time.realtimeSinceStartup<artSweepReadyAt)return true;
            if(artSweepScreenshot){
                if(++artSweepFrame<3 || !File.Exists(artSweepImage))return true;
                var done=artSweep[artSweepIndex];
                artSweepRecords.Add(new ArtSweepRecord{heroineId=done.hero,view=done.view,source=done.source,image=Path.GetFileName(artSweepImage)});
                artSweepScreenshot=false;
            }else if(artSweepIndex>=0){
                if(++artSweepFrame<3)return true;
                ScreenCapture.CaptureScreenshot(artSweepImage);artSweepScreenshot=true;artSweepFrame=0;return true;
            }
            if(++artSweepIndex>=artSweep.Count){
                File.WriteAllText(Path.Combine(artSweepDirectory,"sweep.json"),JsonUtility.ToJson(new ArtSweepReport{records=artSweepRecords.ToArray()},true));
                Debug.Log("PLAN12_ART_SWEEP_PASS captures="+artSweepRecords.Count+" isolated=true");Application.Quit();return true;
            }
            var item=artSweep[artSweepIndex];
            adv=null;advCgGallery=false;encounter=null;title=false;help=false;bookSystemOpen=false;artSweepSd=null;heroineRosterOpen=false;growthScreen=GrowthScreen.Overview;selectedNode=null;
            book.RequestSubject(BookBookmark.Heroines,item.hero);book.CompleteTransition();
            if(item.view=="tree")growthScreen=GrowthScreen.Weapons;
            else if(item.view.StartsWith("expression.") || item.view.StartsWith("cg.")){
                var home=HomeData();var e=home.events.First(e=>e.heroineId==item.hero && (item.source==null || e.id==item.source));
                item.source=e.id;
                var catalog=home;
                if(item.view.StartsWith("expression.")){
                    // Diagnostic visibility only: force the requested expression on the first actor.
                    catalog=JsonUtility.FromJson<HomeExperienceCatalog>(JsonUtility.ToJson(home));
                    var script=catalog.scripts.Single(s=>s.id==e.sceneId);script.commands=script.commands.Where(c=>c.kind!="cg" && c.kind!="hideCg").ToArray();
                    foreach(var command in script.commands)if(string.IsNullOrEmpty(command.speakerId))command.speakerId=null;
                    script.commands.First(c=>c.kind=="actor").expressionId=item.view;
                }
                adv=new AdvSession(catalog,e.sceneId,e.id,true,Array.Empty<HomeReadLine>());
                string wanted=item.view.StartsWith("expression.")?home.displays.Single(d=>d.heroineId==item.hero && d.outfitId=="outfit.fixture").expressions.Single(x=>x.id==item.view).assetId:null;
                int guard=0;
                while(!adv.EndReached && (wanted!=null?!adv.Actors.Any(a=>a.ExpressionAsset==wanted):adv.CgId==null) && guard++<100){adv.Tick(2);adv.Advance();}
                AcceptanceCheck(!adv.EndReached && guard<100,"Art sweep finds requested expression or CG: "+item.hero+" / "+item.view);
                adv.Tick(2);if(!adv.FullyVisible)adv.Advance();adv.Pause();
                if(artSweepGalleryOnly){advCgGallery=true;adv.SetAuto(false);adv.SetSkip(false);var text=adv.VisibleText;AdvanceAdv();UpdateAdv();AcceptanceCheck(adv.VisibleText==text && adv.Paused,"CG gallery blocks advance and timers");HandleEscapeNavigation();AcceptanceCheck(adv!=null && !advCgGallery && adv.VisibleText==text && adv.Paused,"CG gallery Escape returns to same paused line");advCgGallery=true;}
            }
            else if(item.view.StartsWith("sd."))artSweepSd=item.view.Substring(3);
            else if(item.view=="battle"){
                var save=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot));
                save.home.formationIds=new[]{item.hero}.Concat(combatDefinitions.HeroineIds.Where(id=>combatDefinitions.PersonId(id)!=combatDefinitions.PersonId(item.hero)).GroupBy(combatDefinitions.PersonId).Select(g=>g.First()).Take(4)).ToArray();
                BindFormalCampaign(save);StartBattle(WorldCatalog.ColossusIds[0],1137);ReadyArcane();selectedHero=0;battlePanel=BattlePanel.None;paused=true;encounter.DrainPresentationEvents();playback.Reset();
            }
            artSweepImage=Path.Combine(artSweepDirectory,item.hero.Substring("heroine.".Length)+"-"+item.view+".png");
            artSweepFrame=0;artSweepReadyAt=Time.realtimeSinceStartup+.6f;
            if(item.view=="detail")Debug.Log("PLAN12_ART_SWEEP_FORM "+item.hero);
            return true;
        }
        private bool DrawArtSweepSd()
        {
            if(artSweep==null || artSweepSd==null)return false;
            Styles();BeginAspectCanvas();GrowthStyles();GrowthFill(0,0,1600,900,new Color(.12f,.18f,.2f));
            var hero=artSweep[artSweepIndex].hero;
            Label(50,30,1500,60,combatDefinitions.Hero(hero).name+" ／ 家具用SD固定姿勢確認 ／ "+artSweepSd,heading,Color.white);
            string resource="Illustrations/"+hero.Substring("heroine.".Length)+"-sd-"+artSweepSd+"-candidate-v1";
            bool fallback=Resources.Load<Texture2D>(resource)==null;
            if(fallback)resource="Illustrations/"+hero.Substring("heroine.".Length)+"-sd-idle-candidate-v1";
            AcceptanceCheck(Resources.Load<Texture2D>(resource)!=null,"Art sweep SD or runtime idle fallback exists: "+resource);
            DrawGardenResident(new Rect(100,110,1400,650),new HomeOccupant{heroineId=hero,actionId="action."+artSweepSd,x=.5f,y=.7f});
            GUI.DrawTexture(new Rect(1000,180,480,480),SampleImage(hero.Substring("heroine.".Length)+"-sd-"+(fallback?"idle":artSweepSd)),ScaleMode.ScaleToFit,true);
            if(fallback)Label(1000,690,550,70,"専用姿勢なし：実装どおりidleを代用",growthTextStyle,Color.yellow);
            Label(50,810,1500,50,"左：通常の庭描画と同じ縮尺 ／ 右：原画の拡大確認（家具の遮蔽や操作の合格を意味しない）",small,Color.white);
            return true;
        }
    }
}
