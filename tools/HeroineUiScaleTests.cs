using System;
using System.Linq;
using NewAster.Core;
public static class HeroineUiScaleTests
{
    public static void Run(Action<bool,string> check)
    {
        var heroes=Enumerable.Range(0,203).Select(i=>new HeroineCombatDef{id="heroine.scale."+i.ToString("D3"),name="検証"+i.ToString("D3"),personId="heroine.scale."+(i>=200?i-200:i).ToString("D3"),jobId=i%2==0?"job.fighter":"job.healer"}).ToArray();
        var growth=new FormalGrowthSave{heroines=heroes.Select((h,i)=>new FormalHeroineGrowth{heroineId=h.id,level=i%120+1}).ToArray()};
        var index=new HeroineUiIndex(new CombatDefinitionCatalog{heroines=heroes},growth);
        check(index.PersonCount==200,"200 persons remain distinct from 203 forms");
        var all=index.Search("",ownedOnly:true);check(all.Length==203 && all.Distinct().Count()==203,"Every form is selectable");
        check(Enumerable.Range(0,17).SelectMany(p=>all.Skip(p*12).Take(12)).SequenceEqual(all),"Roster pages cover all forms including final partial page");
        check(Enumerable.Range(0,26).SelectMany(p=>all.Skip(p*8).Take(8)).SequenceEqual(all),"Formation pages cover all forms");
        check(index.Search("  検証２０２ ").Single()==heroes[202].id,"Normalized name finds last form");
        check(index.Search("not found").Length==0,"Empty filter result has no fallback selection");
        check(index.Search("検証","job.healer",true).Length==101,"Name and job filter intersect");
        check(index.Search("",ownedOnly:true,order:1).First()==heroes[119].id,"Descending level order is deterministic");
        check(index.Search("検証",source:new[]{heroes[202].id}).Single()==heroes[202].id,"Restricted ticket pool preserves source identity");
        int first=HeroineUiIndex.FirstVisible(203*55,55,203);check(HeroineUiIndex.VisibleEnd(first,55,390,203)<=203,"Outdated scroll clamps to current result count");
        for(int i=0;i<203;i++){first=HeroineUiIndex.FirstVisible(i*55,55,203);int end=HeroineUiIndex.VisibleEnd(first,55,390,203);check(first==i && end-first<=9 && end>first,"Virtual rows stay bounded and include requested row "+i);}
        Console.WriteLine("HEROINE_UI_SCALE_PASS persons=200 forms=203 pagination / normalized search / jobs / level / tickets / bounded rows");
    }
}
