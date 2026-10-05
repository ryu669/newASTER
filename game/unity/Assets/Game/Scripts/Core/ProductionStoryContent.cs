using System;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class ProductionStoryPoem
    { public string id,text,body,sourcePoemId,reason; }
    [Serializable] public sealed class ProductionStoryChapter
    { public string id,ownerId,title,introduction,conclusion,backgroundResourcePath; public ProductionStoryPoem[] poems; }
    [Serializable] public sealed class ProductionStoryEvent
    { public string id,ownerId,title,cgResourcePath,backgroundResourcePath; public int affectionRequired; public bool establishesLover; public string[] paragraphs,expressions; }
    [Serializable] public sealed class ProductionStoryContent
    {
        public const string Version="production-story-2026-10-04";
        public int schemaVersion;
        public string contentVersion,provenance;
        public ProductionStoryChapter[] chapters;
        public ProductionStoryEvent[] events;
        private static void Require(bool valid,string message){if(!valid)throw new ArgumentException(message);}
        private static bool Text(string text)=>!string.IsNullOrWhiteSpace(text) && !text.Contains("【動作検証用") && !text.Contains("未制作");
        public void Validate(string[] heroines,string[] colossi)
        {
            Require(schemaVersion==1 && contentVersion==Version && provenance=="newaster-original","Unknown production story identity.");
            Require(heroines!=null && heroines.Length==5 && heroines.All(Text) && heroines.Distinct().Count()==5 && colossi!=null && colossi.Length==15 && colossi.All(Text) && colossi.Distinct().Count()==15 && !heroines.Intersect(colossi).Any(),"Production story requires five heroines and fifteen enemies.");
            Require(chapters!=null && chapters.Length==60 && chapters.All(c=>c!=null) && chapters.Select(c=>c.id).Distinct().Count()==60,"Production story requires sixty distinct chapters.");
            var all=chapters.SelectMany(c=>c.poems??Array.Empty<ProductionStoryPoem>()).ToArray();
            Require(all.Length==450 && all.All(p=>p!=null) && all.Select(p=>p.id).Distinct().Count()==450,"Production story requires 450 distinct poems.");
            Require(all.All(p=>Text(p.text) && Text(p.body) && p.body.Contains(p.text)) && all.Select(p=>p.text).Distinct().Count()==450,"Every original poem must appear in its unique authored body.");
            Require(all.Select(p=>p.body).Distinct().Count()==450,"Poem paragraphs must not be duplicated.");
            foreach(var owner in colossi.Concat(heroines)){
                bool enemy=colossi.Contains(owner);int per=enemy?8:6;
                var owned=chapters.Where(c=>c.ownerId==owner).ToArray();
                Require(owned.Length==3,"Every production owner requires three chapters.");
                for(int index=1;index<=3;index++){
                    string id=owner+(enemy?".collection.chapter.":".poem-chapter.")+index;
                    var chapter=owned.SingleOrDefault(c=>c.id==id);
                    Require(chapter!=null && Text(chapter.title) && Text(chapter.introduction) && Text(chapter.conclusion) && !string.IsNullOrWhiteSpace(chapter.backgroundResourcePath),"Missing authored chapter identity or narrative.");
                    Require(chapter.poems!=null && chapter.poems.Length==per,"Wrong owner poem count.");
                    for(int i=0;i<per;i++){
                        var poem=chapter.poems[i];
                        Require(poem.id==owner+".collection.poem."+((index-1)*per+i+1).ToString("D2"),"Poem identity does not match chapter.");
                        if(enemy)Require(string.IsNullOrEmpty(poem.sourcePoemId) && string.IsNullOrEmpty(poem.reason),"Enemy poems cannot have heroine correspondence.");
                        else Require(Text(poem.reason) && all.Any(p=>p.id==poem.sourcePoemId && colossi.Any(c=>p.id.StartsWith(c+".collection.poem.",StringComparison.Ordinal))),"Heroine poem requires an authored enemy source and reason.");
                    }
                }
                if(!enemy)Require(owned.SelectMany(c=>c.poems).Select(p=>p.sourcePoemId).Distinct().Count()==18,"Heroine sources must be individually selected.");
            }
            Require(events!=null && events.Length==25 && events.All(e=>e!=null) && events.Select(e=>e.id).Distinct().Count()==25,"Production story requires twenty-five distinct events.");
            Require(events.All(e=>e.paragraphs!=null) && events.SelectMany(e=>e.paragraphs).Distinct().Count()==events.Sum(e=>e.paragraphs.Length),"Event narratives must be individually authored.");
            Require(events.Select(e=>e.cgResourcePath).Distinct().Count()==25,"Events require twenty-five separate CG bindings.");
            foreach(string hero in heroines)for(int i=0;i<5;i++){
                var e=events.SingleOrDefault(v=>v.id==hero+".event."+i);
                Require(e!=null && e.ownerId==hero && Text(e.title) && e.affectionRequired==new[]{1,5,10,15,20}[i] && e.establishesLover==(i==2),"Invalid production event identity or progression.");
                Require(e.paragraphs!=null && e.paragraphs.Length>=8 && e.paragraphs.All(Text) && e.paragraphs.Distinct().Count()==e.paragraphs.Length,"Event requires eight original narrative lines.");
                Require(e.expressions!=null && e.expressions.Length==e.paragraphs.Length && e.expressions.All(x=>new[]{"normal","joy","puzzled","determined"}.Contains(x)),"Every event line requires a known expression.");
                Require(!string.IsNullOrWhiteSpace(e.cgResourcePath) && !string.IsNullOrWhiteSpace(e.backgroundResourcePath),"Event art binding missing.");
            }
        }
    }
}
