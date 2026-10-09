using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly DailyInteractionHistory dailyHistory=new DailyInteractionHistory();
        private readonly System.Random dailyRandom=new System.Random();
        private string dailyDateForm;
        private DailyInteractionContext DailyContext(GardenLifeAgent a)
        {
            var c=gardenLifeRuntime.Context(a);var save=LifeSnapshot();
            Func<string,DailyActorContext> actor=id=>{
                if(id==null)return null;var affection=AffectionService.State(save,HomeData(),id);
                return new DailyActorContext{form=combatDefinitions.Hero(id),affectionLevel=affection.level,lover=AffectionService.IsLover(affection)};
            };
            return new DailyInteractionContext{subject=actor(a.heroineId),partner=actor(a.partnerId),garden=c.gardenId,time=c.timePhase,weather=c.weather,interaction=c.interactionTag,participantCount=c.participantIds.Length,participantIds=c.participantIds,extremes=(c.domains??Array.Empty<TerraformDomainState>()).Where(d=>!string.IsNullOrEmpty(d.activeExtremeId)).Select(d=>d.activeExtremeId).ToArray(),terraformLevels=(c.domains??Array.Empty<TerraformDomainState>()).ToDictionary(d=>d.domainId,d=>d.currentLevel)};
        }
        private void BeginDailyInteraction(string form,string kind,string dateId=null)
        {
            if(affectionInteraction!=null || affectionRequest!=null || gardenLifeRuntime==null || gardenLifeRuntime.Paused || formalCampaign.HasPending || formalProgression.HasPending)return;
            var runtime=gardenLifeRuntime;var a=runtime.Agents.SingleOrDefault(x=>x.heroineId==form);if(a==null)return;
            var context=DailyContext(a);
            if(kind=="together" && (a.kind!="Furniture" && a.kind!="Scenery" || a.slotId==null)){gardenLifeMessage="家具や景観を利用している時に、一緒に過ごせます。";return;}
            DailyDateDef date=dateId==null?null:DailyDates(form).Single(d=>d.id==dateId);
            if(date!=null && context.subject.affectionLevel<date.minimumAffection){gardenLifeMessage="デートは好感度Lv10から楽しめます。";return;}
            // Player interactions have one heroine; autonomous Social keeps subject and partner separate.
            context.partner=null;context.participantCount=1;context.participantIds=new[]{form};
            var chosen=new List<DailyInteractionDef>();
            var definitions=DailyInteractionContent.Definitions.Concat(HomeData().dailyInteractions??Array.Empty<DailyInteractionDef>()).ToArray();
            for(int i=0;i<(date==null?1:2);i++){
                var pool=date!=null && i==0?definitions.Where(d=>d.topicTags.Contains(date.topic)).ToArray():definitions;
                var part=DailyInteractionSelector.Choose(pool,context,dailyHistory,n=>dailyRandom.Next(n),chosen.Select(d=>d.id));
                if(part==null && chosen.Count==0)part=DailyInteractionSelector.Choose(definitions,context,dailyHistory,n=>dailyRandom.Next(n));
                if(part!=null)chosen.Add(part);
            }
            if(chosen.Count==0)return;
            var hero=context.subject.form;var styleOverride=string.IsNullOrEmpty(hero.reactionStyleMain)?null:new ReactionStyleProfile{main=hero.reactionStyleMain,sub=string.IsNullOrEmpty(hero.reactionStyleSub)?null:hero.reactionStyleSub};string style=ReactionStyleRules.Choose(DailyReactionProfiles.For(combatDefinitions.PersonId(form)),styleOverride,n=>dailyRandom.Next(n),context);
            var common=DailyInteractionContent.Presentations;
            var sequence=chosen.Select(d=>DailyPresentationResolver.Resolve(d.presentationId,form,style,HomeData().dailyPresentations??Array.Empty<DailyPresentationDef>(),common.SingleOrDefault(p=>p.id==d.presentationId))).ToList();
            string[] styleText={"相手へ身を寄せ、はっきりした身振りを返す。","小さく頷き、相手の動きを静かに待つ。","落ち着いた姿勢を保ち、短く目を合わせる。","相手のそばで、親しげに顔を向ける。","姿勢を整え、ゆっくり会釈する。","大きく腕を動かし、軽やかに応じる。","周囲を不思議そうに眺め、ふと顔を見合わせる。","少し首を傾け、楽しそうに目を合わせる。"};
            foreach(var p in sequence)p.description+=" "+styleText[Array.IndexOf(ReactionStyleRules.Ids,style)];
            if(date!=null){sequence.Insert(0,new DailyPresentationDef{id=date.id+".start",title=date.name+"の始まり",description="隣に立ち、今日の時間を一緒に始める。",seconds=20,gesture="look"});sequence.Add(new DailyPresentationDef{id=date.id+".end",title=date.name+"の帰り道",description="余韻を分かち合い、穏やかに会釈して日常へ戻る。",seconds=20,gesture="look"});}
            string id=Guid.NewGuid().ToString("N");string slot=kind=="together"?a.slotId:null;
            if(!runtime.TryReserveDailySession(id,new[]{form},slot)){gardenLifeMessage="今は別の交流中です。少し待ってから声をかけてください。";return;}
            var state=new DailyPresentationState{x=a.x,y=a.y,facing=a.facing,furniture=slot,distance=.075f};
            foreach(var d in chosen)dailyHistory.Record(d);
            affectionInteraction=new DailyInteractionSession(id,runtime.GardenId,kind,dateId??chosen[0].id,new[]{form},sequence.ToArray(),state,()=>runtime.ReleaseDailySession(id),completion=>{
                if(completion.GivesAffection)ProposeAffection("interaction",completion.forms,completion.contentId,completion.sessionId);
                else affectionInteraction=null;
            });
            dailyDateForm=null;gardenLifePanel=null;gardenLifeMessage=sequence[0].description;
        }
        private void DrawDailyDateMenu()
        {
            if(dailyDateForm==null)return;
            GrowthFill(0,88,1600,812,new Color(0,0,0,.8f));GrowthFrame(390,185,820,560);
            Label(430,220,740,60,combatDefinitions.Hero(dailyDateForm).name+"と過ごす",growthTitleStyle);
            var affection=AffectionService.State(LifeSnapshot(),HomeData(),dailyDateForm);
            var dates=DailyDates(dailyDateForm);
            dailyDateScroll=GUI.BeginScrollView(new Rect(430,295,750,345),dailyDateScroll,new Rect(0,0,720,Math.Max(345,dates.Length*64)));
            for(int i=0;i<dates.Length;i++){var date=dates[i];if(GrowthButton(10,i*64,700,54,date.name+" ／ ひとときを一緒に",affection.level>=date.minimumAffection))BeginDailyInteraction(dailyDateForm,"date",date.id);}
            GUI.EndScrollView();
            if(GrowthButton(440,650,720,48,"戻る"))dailyDateForm=null;
        }
        private Vector2 dailyDateScroll;
        private DailyDateDef[] DailyDates(string form)=>DailyDateCatalog.Definitions.Concat((HomeData().dailyDates??Array.Empty<DailyDateDef>()).Where(d=>d.ownerId==form || d.ownerId==combatDefinitions.PersonId(form))).ToArray();
        private void DailyAutonomousSocial(GardenLifeContext source)
        {
            if(source.heroineId==null || source.partnerId==null || source.socialSeconds!=0 || affectionInteraction!=null)return;
            var a=gardenLifeRuntime.Agents.SingleOrDefault(x=>x.heroineId==source.heroineId);if(a==null)return;
            var part=DailyInteractionSelector.Choose(DailyInteractionContent.Definitions.Concat(HomeData().dailyInteractions??Array.Empty<DailyInteractionDef>()),DailyContext(a),dailyHistory,n=>dailyRandom.Next(n));
            if(part==null)return;dailyHistory.Record(part);var p=DailyInteractionContent.Presentations.SingleOrDefault(x=>x.id==part.presentationId);
            gardenLifeMessage=DailyPresentationResolver.Resolve(part.presentationId,a.heroineId,"reserved",HomeData().dailyPresentations??Array.Empty<DailyPresentationDef>(),p).description;
        }
    }
}
