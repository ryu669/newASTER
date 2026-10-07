using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly Dictionary<string,BattleIllustrationView> bookIllustrations=new Dictionary<string,BattleIllustrationView>();
        private void DrawBookSubjectArt()
        {
            var area=new Rect(1024,198,576,459);
            if(!book.HasSubject){Label(1050,275,510,100,"表示する対象はありません。",text,Color.white);return;}
            string owner=book.SubjectId;
            if(WorldCatalog.ColossusIds.Contains(owner)){
                var chapter=ProductionStoryData().chapters.First(c=>c.ownerId==owner);
                var background=Resources.Load<Texture2D>(chapter.backgroundResourcePath);if(background!=null)GUI.DrawTexture(area,background,ScaleMode.ScaleAndCrop);
                if(!bookIllustrations.TryGetValue(owner,out var art)){art=new BattleIllustrationView(ColossusCombatCatalog.IllustrationResource(owner));bookIllustrations.Add(owner,art);}
                art.DrawEnemyPreview(new Rect(1035,205,550,445),0);
            }else if(combatDefinitions.HeroineIds.Contains(owner)){
                var image=Resources.Load<Texture2D>("Illustrations/"+owner.Substring("heroine.".Length)+"-standing-candidate-v1");
                if(image!=null)GUI.DrawTexture(area,image,ScaleMode.ScaleToFit,true);
            }
        }
    }
}
