using System;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    // Functional acceptance only. No text or numerical fixture is authored game content.
    public static class HomeExperienceFixture
    {
        public static HomeExperienceCatalog Create(CombatDefinitionCatalog combat)
        {
            var collection=CollectionContractFixture.Create(combat);var heroes=combat.FormationIds;
            var c=new HomeExperienceCatalog{
                schemaVersion=1,contentVersion=HomeExperienceCatalog.FixtureVersion,status="fixture",
                heroineIds=heroes.ToArray(),colossusIds=WorldCatalog.ColossusIds.ToArray(),poemIds=collection.poems.Select(p=>p.id).ToArray(),resourceIds=collection.resources.Where(r=>r.kind=="material").Select(r=>r.id).Concat(new[]{"resource.materials"}).ToArray(),
                abilityIds=new[]{"ability.home-fixture.attack"},skillIds=new[]{"skill.home-fixture.preview"},
                materials=collection.resources.Where(r=>r.kind=="material" && WorldCatalog.ColossusIds.Contains(r.ownerId)).Select(r=>new HomeMaterialDef{id=r.id,colossusId=r.ownerId}).ToArray(),
                interactions=heroes.Select(h=>new HomeTalkDef{id="interaction.fixture."+h,heroineId=h,affectionGain=1,costs=new[]{new HomeCost{resourceId=collection.owners[0].materialIds[0],amount=1}}}).ToArray(),
                gardens=GardenCatalog.Requirements.Select((g,i)=>i<2?Garden(g.GardenId):new HomeGardenLayout{id=g.GardenId,schemaVersion=1,unmade=true}).ToArray(),
                actorSlots=new[]{Slot("slot.left",.22f),Slot("slot.center",.5f),Slot("slot.right",.78f)},
                texts=new[]{new HomeTextDef{id="text.fixture.line",text="【動作検証用】この文章は正式な人物の物語ではありません。"}},
                scripts=new[]{new HomeAdvScript{schemaVersion=1,scriptVersion=1,id="scene.home-fixture",commands=new[]{new HomeAdvCommand{commandId="cmd.background",kind="background",assetId="asset.fixture.background",transition="instant"},new HomeAdvCommand{commandId="cmd.actor",kind="actor",heroineId=heroes[0],slotId="slot.left",outfitId="outfit.fixture",expressionId="expression.normal",poseId="pose.idle"},new HomeAdvCommand{commandId="cmd.line",kind="line",lineId="line.fixture",textId="text.fixture.line",speakerId=heroes[0]},new HomeAdvCommand{commandId="cmd.end",kind="end"}}}},
                assets=new[]{Asset("asset.fixture.background","background"),Asset("asset.fixture.furniture","furniture"),Asset("asset.fixture.standing","standing"),Asset("asset.fixture.expression","expression"),Asset("asset.fixture.pose","pose"),Asset("asset.fixture.cg","cg"),Asset("asset.fixture.audio","audio")},
                displays=heroes.Select(h=>new HomeDisplaySet{id="display.fixture."+h,heroineId=h,outfitId="outfit.fixture",standingAssetId="asset.fixture.standing",expressions=new[]{new HomeDisplayVariant{id="expression.normal",assetId="asset.fixture.expression"}},poses=new[]{new HomeDisplayVariant{id="pose.idle",assetId="asset.fixture.pose"}}}).ToArray(),
                furniture=Enumerable.Range(0,3).Select(i=>new HomeFurnitureLayout{id="furniture.fixture."+i,assetId="asset.fixture.furniture",size01=new HomePoint{x=.16f,y=.2f},drawAnchor=new HomePoint{x=.5f,y=1},footprint=new HomeRect{x=.1f,y=.75f,width=.8f,height=.25f},orientationIds=new[]{"orientation.default"},costs=new[]{new HomeCost{resourceId=collection.owners[0].materialIds[0],amount=2+i}},slots=new[]{new HomeFurnitureSlot{id="slot.use",offset=new HomePoint{x=.5f,y=.8f},actionIds=new[]{"action.sit"}}}}).ToArray(),
                weaponNodes=heroes.SelectMany(h=>new[]{new HomeWeaponNode{id=h+".weapon.root",heroineId=h,initial=true,abilityId="ability.home-fixture.attack",skillId="skill.home-fixture.preview"},new HomeWeaponNode{id=h+".weapon.alpha",heroineId=h,parentIds=new[]{h+".weapon.root"},costs=new[]{new HomeCost{resourceId=collection.owners[0].materialIds[0],amount=3}},abilityId="ability.home-fixture.attack",skillId="skill.home-fixture.preview"},new HomeWeaponNode{id=h+".weapon.beta",heroineId=h,parentIds=new[]{h+".weapon.root"},costs=new[]{new HomeCost{resourceId=collection.owners[0].materialIds[0],amount=4}},abilityId="ability.home-fixture.attack",skillId="skill.home-fixture.preview"},new HomeWeaponNode{id=h+".weapon.gamma",heroineId=h,parentIds=new[]{h+".weapon.alpha",h+".weapon.beta"},costs=new[]{new HomeCost{resourceId=collection.owners[0].materialIds[0],amount=5}},abilityId="ability.home-fixture.attack",skillId="skill.home-fixture.preview"}}).ToArray(),
                events=heroes.SelectMany(h=>Enumerable.Range(0,5).Select(i=>new HomeEventDef{id=h+".event."+i,heroineId=h,kind=i<3?"affinity":"lover",establishesLover=i==2,sceneId="scene.home-fixture",unlockCondition=i==0?Always():new HomeCondition{kind="flag",domain="eventRead",id=h+".event."+(i-1)}})).ToArray(),
                chapters=collection.chapters.Select(ch=>new HomeChapterDef{id=ch.id,ownerId=ch.ownerId,requiredPoemIds=ch.poemIds.ToArray(),sceneId="scene.home-fixture"}).ToArray()
            };
            c.subjects=c.colossusIds.Select((id,i)=>Subject("colossi",id,i)).Concat(heroes.Select((id,i)=>Subject("heroines",id,i))).Concat(c.gardens.Select((g,i)=>Subject("gardens",g.id,i))).Concat(c.colossusIds.Concat(heroes).Select((id,i)=>Subject("stories",id,i))).ToArray();
            foreach(var n in c.weaponNodes){int hero=Array.IndexOf(heroes,n.heroineId);int stage=n.initial?0:n.id.EndsWith("alpha")?1:n.id.EndsWith("beta")?2:3;n.attackBonus=stage*4;n.skillPower=1+stage*.15f;n.treePosition=new HomePoint{x=stage==1?.2f+hero*.015f:stage==2?.8f-hero*.015f:.5f,y=stage==0?.9f:stage==3?.1f:.5f};n.terminal=stage==0?null:stage==1?"α":stage==2?"β":"γ";}
            c.texts=c.texts.Concat(new[]{new HomeTextDef{id="text.fixture.long",text="【動作検証用の長文】日本語の段階表示、全文表示から次の行へ送る二段階入力、人物の表示枠移動、CGを閉じた後の復元を確認します。これは物語本文ではありません。鉢植えや家具と人物の進行、詩と章、読了と恋人状態は別々に保存します。"},new HomeTextDef{id="text.fixture.last",text="【動作検証用】endの保存に成功した時点だけで読了します。回想は保存しません。"}}).ToArray();
            c.assets=c.assets.Concat(new[]{Asset("asset.fixture.foreground","foreground")}).ToArray();foreach(var g in c.gardens.Where(g=>!g.unmade)){g.backgroundAssetId="asset.fixture.background";g.foregroundAssetIds=new[]{"asset.fixture.foreground"};}
            var rich=new[]{new HomeAdvCommand{commandId="cmd.bg",kind="background",assetId="asset.fixture.background",transition="fade",durationMs=200},new HomeAdvCommand{commandId="cmd.a",kind="actor",heroineId=heroes[0],slotId="slot.left",outfitId="outfit.fixture",expressionId="expression.normal",poseId="pose.idle"},new HomeAdvCommand{commandId="cmd.b",kind="actor",heroineId=heroes[1],slotId="slot.right",outfitId="outfit.fixture",expressionId="expression.missing",poseId="pose.missing"},new HomeAdvCommand{commandId="cmd.sound",kind="sound",audioId="asset.fixture.audio",channel="se"},new HomeAdvCommand{commandId="cmd.l1",kind="line",lineId="line.one",textId="text.fixture.line",speakerId=heroes[0]},new HomeAdvCommand{commandId="cmd.cg",kind="cg",assetId="asset.fixture.cg",hideActors=true},new HomeAdvCommand{commandId="cmd.l2",kind="line",lineId="line.long",textId="text.fixture.long"},new HomeAdvCommand{commandId="cmd.hidecg",kind="hideCg"},new HomeAdvCommand{commandId="cmd.move",kind="actor",heroineId=heroes[0],slotId="slot.center",outfitId="outfit.fixture",expressionId="expression.normal",poseId="pose.idle"},new HomeAdvCommand{commandId="cmd.hide",kind="hideActor",heroineId=heroes[1]},new HomeAdvCommand{commandId="cmd.l3",kind="line",lineId="line.last",textId="text.fixture.last",speakerId=heroes[0]},new HomeAdvCommand{commandId="cmd.end",kind="end"}};
            foreach(var e in c.events)e.sceneId="scene.fixture."+e.id;foreach(var ch in c.chapters)ch.sceneId="scene.fixture."+ch.id;
            foreach(var e in c.events.Where(e=>e.id.EndsWith(".event.0")))e.unlockCondition=new HomeCondition{kind="atLeast",domain="affection",ownerId=e.heroineId,value=1};
            c.scripts=c.scripts.Concat(c.events.Select(e=>new HomeAdvScript{id=e.sceneId,schemaVersion=1,scriptVersion=1,commands=rich})).Concat(c.chapters.Select(ch=>new HomeAdvScript{id=ch.sceneId,schemaVersion=1,scriptVersion=1,commands=rich})).ToArray();
            // Candidate art is explicitly bound to Slayer only; the fixture remains non-release content.
            var candidateAssets=new[]{new HomeAssetDef{id="art.candidate.slayer.standing.v1",kind="standing",placeholder=true,resourcePath="Illustrations/slayer-standing-candidate-v1",fullFrame=true}}
                .Concat(new[]{"joy","puzzled","determined"}.Select(expression=>new HomeAssetDef{id="art.candidate.slayer.expression."+expression+".v1",kind="expression",placeholder=true,resourcePath="Illustrations/slayer-expression-"+expression+"-candidate-v1",fullFrame=true})).ToArray();
            c.assets=c.assets.Concat(candidateAssets).ToArray();
            c.assets.Single(a=>a.id=="asset.fixture.background").resourcePath="Illustrations/forest-far-candidate-v1";
            c.assets.Single(a=>a.id=="asset.fixture.foreground").resourcePath="Illustrations/forest-front-candidate-v1";
            c.assets.Single(a=>a.id=="asset.fixture.cg").resourcePath="Illustrations/slayer-garden-cg-candidate-v1";
            c.assets.Single(a=>a.id=="asset.fixture.audio").resourcePath="Audio/candidate-heal";
            var furnitureArt=new[]{"bench","desk","fountain"};var actions=new[]{"sit","work","look"};
            for(int i=0;i<3;i++){
                var f=c.furniture[i];f.assetId="art.candidate.furniture."+furnitureArt[i]+".v1";f.supportedHeroineIds=new[]{"heroine.slayer"};
                f.slots[0].actionIds=actions[i]=="sit"?new[]{"action.sit"}:new[]{"action."+actions[i],"action.sit"};
                c.assets=c.assets.Concat(new[]{new HomeAssetDef{id=f.assetId,kind="furniture",placeholder=true,resourcePath="Illustrations/garden-"+furnitureArt[i]+"-candidate-v1"}}).ToArray();
            }
            var slayerDisplay=c.displays.Single(d=>d.heroineId=="heroine.slayer");slayerDisplay.standingAssetId=candidateAssets[0].id;
            slayerDisplay.expressions=new[]{new HomeDisplayVariant{id="expression.normal",assetId="asset.fixture.expression"}}
                .Concat(new[]{"joy","puzzled","determined"}.Select(expression=>new HomeDisplayVariant{id="expression."+expression,assetId="art.candidate.slayer.expression."+expression+".v1"})).ToArray();
            c.scripts=c.scripts.Concat(new[]{new HomeAdvScript{id="scene.art-candidate.slayer",schemaVersion=1,scriptVersion=1,
                commands=new[]{new HomeAdvCommand{commandId="art.background",kind="background",assetId="asset.fixture.background",transition="instant"}}.Concat(new[]{"normal","joy","puzzled","determined"}.SelectMany(expression=>new[]{
                    new HomeAdvCommand{commandId="art.actor."+expression,kind="actor",heroineId="heroine.slayer",slotId="slot.center",outfitId="outfit.fixture",expressionId="expression."+expression,poseId="pose.idle"},
                    new HomeAdvCommand{commandId="art.line."+expression,kind="line",lineId="art.line."+expression,textId="text.fixture.line",speakerId="heroine.slayer"}}))
                    .Concat(new[]{new HomeAdvCommand{commandId="art.end",kind="end"}}).ToArray()}}).ToArray();
            c.Validate();return c;
        }
        private static HomeCondition Always()=>new HomeCondition{kind="always"};
        private static HomeBookSubject Subject(string bookmark,string id,int order)=>new HomeBookSubject{id="book."+bookmark+"."+id,bookmarkId=bookmark,subjectId=id,pageOrder=order,unlockCondition=Always()};
        private static HomeAssetDef Asset(string id,string kind)=>new HomeAssetDef{id=id,kind=kind,placeholder=true};
        private static HomeActorSlot Slot(string id,float x)=>new HomeActorSlot{id=id,anchor=new HomePoint{x=x,y=.8f},pivot=new HomePoint{x=.5f,y=1},size01=new HomePoint{x=.25f,y=.65f},drawOrder=id=="slot.left"?0:id=="slot.center"?1:2};
        private static HomeGardenLayout Garden(string id)=>new HomeGardenLayout{id=id,schemaVersion=1,zones=new[]{new HomeGardenZone{id="zone.ground",order=0,bounds=new HomeRect{x=0,y=0,width=1,height=1}}}};
    }
}
