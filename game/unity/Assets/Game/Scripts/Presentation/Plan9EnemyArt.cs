using System;
using System.Linq;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private BattleIllustrationView plan9EnemyPreview;
        private string plan9EnemyName;
        private int plan9EnemyBrokenMask;
        private bool plan9EnemyMajor;
        private void InitializePlan9EnemyReview(string[] args)
        {
            int index=Array.IndexOf(args,"-inspectPlan9Enemy");
            if(index<0)return;
            if(index+1>=args.Length)throw new ArgumentException("Missing enemy art review ID");
            string id="colossus."+args[index+1];var definition=WorldCatalog.Colossi.SingleOrDefault(c=>c.Id==id);
            if(definition==null)throw new ArgumentException("Unknown enemy art review");
            plan9EnemyName=definition.DisplayName;
            plan9EnemyPreview=new BattleIllustrationView("Illustrations/battle-"+args[index+1]+"-candidate-v1");
            if(!plan9EnemyPreview.Ready)throw new InvalidOperationException("Enemy art manifest unavailable");
            int mask=Array.IndexOf(args,"-inspectPlan9EnemyBroken");
            if(mask>=0 && (mask+1>=args.Length || !int.TryParse(args[mask+1],out plan9EnemyBrokenMask) || plan9EnemyBrokenMask<0 || plan9EnemyBrokenMask>((1<<plan9EnemyPreview.PartCount)-1)))throw new ArgumentException("Invalid enemy break review mask");
            plan9EnemyMajor=args.Contains("-inspectPlan9EnemyMajor");
            Debug.Log("PLAN9_ENEMY_ART_CAPTURE id="+id+" mask="+plan9EnemyBrokenMask+" major="+plan9EnemyMajor);
        }
        private void DrawPlan9EnemyArt()
        {
            GrowthStyles();TitleFill(new Rect(0,0,1600,900),titleInk);
            Label(40,20,1520,55,plan9EnemyName+" ／ 部位合成の制作審査",heading,titleGold);
            var canvas=new Rect(350,95,720,720);
            if(plan9EnemyMajor && plan9EnemyBrokenMask==0)plan9EnemyPreview.DrawEnemyMajorPreview(canvas);
            else plan9EnemyPreview.DrawEnemyPreview(canvas,plan9EnemyBrokenMask);
            Label(40,830,1520,45,"候補：独立部位を合成 ／ 破壊マスク "+plan9EnemyBrokenMask,small,Color.white);
        }
        private void PreparePlan9ColossusCapture(string[] args)
        {
            int index=Array.IndexOf(args,"-inspectPlan9Colossus");if(index<0)return;
            if(!formalDiagnostic || capturePath==null || index+1>=args.Length)throw new InvalidOperationException("Colossus capture requires isolated diagnostic save");
            int levelIndex=Array.IndexOf(args,"-inspectPlan9ColossusLevel");
            if(levelIndex>=0 && (levelIndex+1>=args.Length || !int.TryParse(args[levelIndex+1],out selectedLevel) || selectedLevel<1 || selectedLevel>50))throw new ArgumentException("Invalid colossus capture level");
            StartBattle(args[index+1]);encounter.State.AdvanceBossGauge(encounter.State.BossGaugeMax-1);
            int breakIndex=Array.IndexOf(args,"-inspectPlan9ColossusBreak");
            if(breakIndex>=0){if(breakIndex+1>=args.Length || !encounter.State.Parts.Any(p=>p.Id==args[breakIndex+1]))throw new ArgumentException("Unknown capture part");encounter.State.BreakPart(args[breakIndex+1],int.MaxValue);}
            encounter.DrainPresentationEvents();
            Debug.Log("PLAN9_COLOSSUS_CAPTURE id="+activeColossus+" level="+selectedLevel+" parts="+encounter.State.Parts.Count+" action="+encounter.NextEnemyAction);
        }
    }
}
