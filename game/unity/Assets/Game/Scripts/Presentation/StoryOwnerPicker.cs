using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private bool storyPickerOpen;
        private string storyPickerQuery="";
        private int storyPickerPage;
        private void DrawStoryOwnerPicker()
        {
            if(GrowthButton(80,108,230,48,"‹ 物語へ戻る",BookInputAllowed)){storyPickerOpen=false;return;}
            Label(350,114,130,38,"名前検索",growthSmallStyle);
            string next=ImageUiSkin.TextField(new Rect(485,108,790,48),storyPickerQuery,64,new GUIStyle(GUI.skin.textField){font=font,fontSize=22});
            if(next!=storyPickerQuery){storyPickerQuery=next;storyPickerPage=0;}
            if(GrowthButton(1300,108,220,48,"クリア",storyPickerQuery.Length>0)){storyPickerQuery="";storyPickerPage=0;}
            string query=HeroineUiIndex.Normalize(storyPickerQuery);
            var owners=CollectionData().owners.Where(o=>HeroineUiIndex.Normalize(CollectionOwnerName(o)).Contains(query)).OrderBy(CollectionOwnerName,StringComparer.Ordinal).ThenBy(o=>o.id,StringComparer.Ordinal).ToArray();
            int pages=Math.Max(1,(owners.Length+9)/10);storyPickerPage=Mathf.Clamp(storyPickerPage,0,pages-1);
            Label(80,175,1440,40,owners.Length+"件 ／ 人物と巨神獣の物語",growthSmallStyle);
            var rows=owners.Skip(storyPickerPage*10).Take(10).ToArray();
            for(int i=0;i<rows.Length;i++){var owner=rows[i];if(GrowthButton(80+i%2*730,235+i/2*105,705,88,CollectionOwnerName(owner)+" ／ "+owner.chapterIds.Length+"章",BookInputAllowed)){book.RequestSubject(BookBookmark.Stories,owner.id);storyPickerOpen=false;bookTransitionElapsed=0;}}
            if(rows.Length==0)Label(80,370,1440,60,"該当する物語がありません",growthTextStyle);
            DrawPageEnds(80,815,1440,ref storyPickerPage,pages,BookInputAllowed);
            if(GrowthButton(175,815,200,40,"‹ 前へ",storyPickerPage>0 && BookInputAllowed))storyPickerPage--;
            Label(660,819,300,40,(storyPickerPage+1)+" / "+pages,growthTextStyle);
            if(GrowthButton(1225,815,200,40,"次へ ›",storyPickerPage<pages-1 && BookInputAllowed))storyPickerPage++;
        }
    }
}
