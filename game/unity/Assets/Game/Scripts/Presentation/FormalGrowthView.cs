using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private FormalProgression formalProgression;
        private FormalGrowthStore formalGrowthStore;
        private GrowthRequest growthRequest;
        private void InitializeFormalGrowth()
        {
            formalGrowthStore=new FormalGrowthStore(Path.Combine(Application.persistentDataPath,"formal-growth-v1.json"),"newaster.formal-growth",
                s=>JsonUtility.ToJson(s,true),text=>JsonUtility.FromJson<FormalGrowthSave>(text));
            var load=formalGrowthStore.Load(out var save);
            if(load==FormalLoadResult.Blocked || load==FormalLoadResult.RecoveredBackup) throw new ArgumentException("正式育成の保存が破損または未対応です。元ファイルを保持し、上書きを停止しました。");
            if(load==FormalLoadResult.Missing) {
                save=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,
                    heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
                // Diagnostic captures never create a real player save.
                if(!Environment.GetCommandLineArgs().Contains("-presentationCapture")) formalGrowthStore.Save(save);
            }
            formalProgression=new FormalProgression(save,combatDefinitions.FormationIds);
            book=new BookNavigationState(new System.Collections.Generic.Dictionary<BookBookmark,System.Collections.Generic.IReadOnlyList<string>> {
                [BookBookmark.Colossi]=NewAster.Data.WorldCatalog.ColossusIds,
                [BookBookmark.Heroines]=combatDefinitions.FormationIds,
                [BookBookmark.Gardens]=new[]{"garden.grassland-forest"},
                [BookBookmark.Stories]=new[]{"story.green-return-dragon"}
            });
            Debug.Log("FORMAL_GROWTH_READY revision="+save.revision);
        }
        private void DrawFormalGrowth()
        {
            string id=combatDefinitions.FormationIds[book.SubjectIndex];var definition=combatDefinitions.Hero(id);
            var snapshot=formalProgression.Snapshot;var g=snapshot.heroines.Single(h=>h.heroineId==id);
            var previewBattle=new PlayableBattle(1,campaign.Playable,combatDefinitions:combatDefinitions,formalGrowth:snapshot);
            var actor=previewBattle.State.Heroes[book.SubjectIndex];
            Label(32,275,930,55,$"{definition.name} / Lv.{g.level}/{g.LevelCap} / 覚醒{g.awakeningStage}",heading);
            Label(32,338,930,65,$"HP {actor.MaxHitPoints}  攻撃 {actor.Attack}  防御 {actor.PhysicalDefense}  魔法防御 {actor.MagicDefense}  速度 {actor.Speed}\nネクタル {snapshot.nectar} / 結晶 {snapshot.awakeningCrystals} / 専用欠片 {g.fragments} / 汎用 {snapshot.overflow}",small);
            if(book.Face==BookFace.Details) {
                Label(32,420,930,160,"スキル："+string.Join(" / ",Enumerable.Range(0,3).Select(slot=>combatDefinitions.Skill(id,slot).name))+"\n重複強化 "+g.duplicateRank+"/5：基礎HP・攻撃・防御＋"+(2*g.duplicateRank)+"%。速度・チェイン率は変わりません。",text);
                Label(32,610,930,130,"装備の樹・好感度は後続で接続します。\n旧試遊の枝・支援・素材を正式人物へ流用しません。",text);return;
            }
            if(growthRequest!=null) {
                if(growthRequest.HeroineId!=id) {
                    if(formalProgression.HasPending) Label(32,430,920,100,"保存待ちの人物ページへ戻って再試行してください。",text);
                    else growthRequest=null;
                    return;
                }
                if(formalProgression.HasPending) {
                    Label(32,430,920,80,"保存に失敗しました。所持量は変わっていません。\n同じ操作を再保存します。",text);
                } else {
                    var p=formalProgression.Preview(growthRequest);
                    Label(32,430,920,120,$"確認：Lv.{p.HeroineAfter.level} / 覚醒{p.HeroineAfter.awakeningStage} / 強化{p.HeroineAfter.duplicateRank}\n消費：ネクタル {p.NectarCost}・結晶 {p.CrystalCost}・専用 {p.FragmentCost}・汎用 {p.OverflowCost}\n汎用への変換：{p.OverflowGrant}",text);
                }
                if(Btn(32,590,445,55,formalProgression.HasPending?"同じ操作で保存を再試行":"確認して確定")) {
                    try {
                        var result=formalProgression.Commit(growthRequest,formalGrowthStore.Save);
                        if(result!=GrowthCommitResult.SaveFailed) {growthRequest=null;status="育成を保存しました。次の出撃から反映されます。";}
                    } catch(Exception e) {status="保存できませんでした。所持量は変更せず再試行できます。";Debug.LogException(e);}
                }
                if(Btn(492,590,445,55,"取り消す",!formalProgression.HasPending)) growthRequest=null;
                return;
            }
            if(formalProgression.HasPending) {Label(32,430,920,100,"保存待ちの人物ページへ戻って再試行してください。",text);return;}
            int[] steps={1,5,10};
            for(int i=0;i<3;i++) {
                int targetLevel=Math.Min(g.LevelCap,g.level+steps[i]);int cost=targetLevel>g.level?FormalProgression.LevelCost(g.level,targetLevel):0;
                if(Btn(32+i*308,430,294,55,$"Lv.{targetLevel}へ / ネクタル{cost}",targetLevel>g.level && snapshot.nectar>=cost)) growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,snapshot.revision,GrowthOperation.Level,targetLevel);
            }
            int crystals=g.awakeningStage==0?20:60;
            if(Btn(32,515,910,55,g.awakeningStage==2?"覚醒2達成":$"覚醒{g.awakeningStage+1} / 結晶{crystals} / Lv.{g.LevelCap}で解放",g.awakeningStage<2 && g.level==g.LevelCap && snapshot.awakeningCrystals>=crystals)) growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,snapshot.revision,GrowthOperation.Awaken);
            if(Btn(32,600,910,55,$"重複強化 {g.duplicateRank}/5 / 専用を優先して合計100個",g.duplicateRank<5 && (long)g.fragments+snapshot.overflow>=100)) growthRequest=new GrowthRequest(Guid.NewGuid().ToString("N"),id,snapshot.revision,GrowthOperation.Strengthen);
            Label(32,695,930,85,"育成の初期配布は検証用です。\n討伐でのネクタル・結晶獲得と正式ガチャは次に接続します。",small);
        }
    }
}
