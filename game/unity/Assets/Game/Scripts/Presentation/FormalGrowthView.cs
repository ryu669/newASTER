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
            bool diagnostic=Environment.GetCommandLineArgs().Contains("-presentationCapture");
            FormalGrowthSave save=null;
            var load=diagnostic?FormalLoadResult.Missing:formalGrowthStore.Load(out save);
            if(load==FormalLoadResult.Blocked || load==FormalLoadResult.RecoveredBackup) throw new ArgumentException("正式育成の保存が破損または未対応です。元ファイルを保持し、上書きを停止しました。");
            if(load==FormalLoadResult.Missing) {
                save=new FormalGrowthSave {saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,
                    heroines=combatDefinitions.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
                // Diagnostic captures never create a real player save.
                if(!diagnostic) formalGrowthStore.Save(save);
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
        private void DrawFormalGrowth() => DrawGrowthExperience();
    }
}
