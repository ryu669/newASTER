using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private float bookTransitionElapsed;
        private readonly System.Collections.Generic.Dictionary<string,int> bookLevelByColossus=new System.Collections.Generic.Dictionary<string,int>();
        private string bookLevelOwner;
        private const float BookTransitionSeconds=.16f; // Presentation trial setting, no progression rule.
        private BookNavigationState CreateFormalBook()
        {
            var data=HomeData();
            var navigation=new BookNavigationState(data.subjects.Select(s=>new BookOrderedSubject(
                s.bookmarkId=="colossi"?BookBookmark.Colossi:s.bookmarkId=="heroines"?BookBookmark.Heroines:s.bookmarkId=="gardens"?BookBookmark.Gardens:BookBookmark.Stories,s.subjectId,s.pageOrder)).Concat(new[]{new BookOrderedSubject(BookBookmark.NewWorld,"terraform.new-world",0),new BookOrderedSubject(BookBookmark.PossibleWorlds,"terraform.possible-worlds",0),new BookOrderedSubject(BookBookmark.Summoning,"summoning.main",0),new BookOrderedSubject(BookBookmark.Items,"items.main",0)}).Concat(NewAster.Data.WorldCatalog.ColossusIds.Select((id,i)=>new BookOrderedSubject(BookBookmark.RelicHunt,id,i))));
            return navigation;
        }
        private void SyncBookSelectedLevel()
        {
            if((book.Bookmark!=BookBookmark.Colossi && book.Bookmark!=BookBookmark.RelicHunt) || !book.HasSubject || bookLevelOwner==book.SubjectId)return;
            if(bookLevelOwner!=null)bookLevelByColossus[bookLevelOwner]=selectedLevel;
            bookLevelOwner=book.SubjectId;selectedLevel=bookLevelByColossus.TryGetValue(bookLevelOwner,out var level)?Math.Max(1,Math.Min(campaign.Playable.HighestLevel,level)):1;
        }
        private bool BookInputAllowed=>formalCampaign==null || !formalCampaign.HasPending && !formalProgression.HasPending && homeRequest==null && !placing && gardenLifeEditor==null && gardenLifeRequest==null && !gardenDiscardConfirm && !gardenLifeResidentPlace && gardenPresetShortage==null;
        private void RequestBookBookmark(BookBookmark bookmark){if(BookInputAllowed && (bookmark==BookBookmark.Gardens && formalCampaign!=null?book.RequestSubject(bookmark,LifeSnapshot().gardenLife.favoriteGardenId??LifeSnapshot().gardenLife.lastGardenId??lastLifeGarden??NewAster.Core.GardenLifeCatalog.GardenIds[0]):book.RequestBookmark(bookmark))){bookTransitionElapsed=0;scroll=Vector2.zero;formationOpen=false;returnToFormationFromWeapon=false;gardenPanel=GardenPanel.None;gardenMenuExpanded=false;if(bookmark==BookBookmark.Heroines){heroineRosterOpen=true;growthScreen=GrowthScreen.Overview;}}}
        private void RequestBookTurn(int direction){if(BookInputAllowed && book.RequestTurn(direction)){bookTransitionElapsed=0;scroll=Vector2.zero;}}
        private void RequestBookFlip(){if(BookInputAllowed && book.RequestFlip())bookTransitionElapsed=0;}
        private void UpdateBookTransition()
        {if(book==null || !book.IsTransitioning)return;bookTransitionElapsed+=Time.unscaledDeltaTime;if(bookTransitionElapsed>=BookTransitionSeconds)book.CompleteTransition();}
        private void DrawBookTransition(bool growth=false)
        {
            if(!book.IsTransitioning)return;
            if(growth){Label(1020,760,245,40,"ページをめくっています",growthSmallStyle);if(GrowthButton(1280,756,190,40,"演出をスキップ"))book.CompleteTransition();}
            else{Label(32,820,700,35,"ページをめくっています",small,Color.white);if(Btn(760,814,210,42,"演出をスキップ"))book.CompleteTransition();}
        }
        private void OpenCollectionForBook()
        {
            if(!BookInputAllowed || !book.HasSubject || book.IsTransitioning)return;
            var owners=CollectionData().owners;int index=Array.FindIndex(owners,o=>o.id==book.SubjectId);
            if(index>=0)collectionOwner=index;collectionOpen=true;collectionTab=0;
        }
        private void CollectionTurnOwner(int direction)
        {
            if(!BookInputAllowed || book.IsTransitioning)return;var owners=CollectionData().owners;int next=collectionOwner+Math.Sign(direction);if(next<0 || next>=owners.Length)return;
            collectionOwner=next;if(book.RequestSubject(BookBookmark.Stories,owners[next].id))bookTransitionElapsed=0;
        }
        private void PrepareBookCapture(string[] args)
        {
            encounter=null;result=null;collectionOpen=false;title=false;int checks=0;var saveBefore=UnityFormalCampaignJson.Encode(formalCampaign.Snapshot);
            Action<bool> check=ok=>{checks++;if(!ok)throw new InvalidOperationException("Plan6 book acceptance failed at "+checks);};
            var initial=CreateFormalBook();check(initial.SubjectCount==15 && !initial.CanTurnPrevious);
            initial.ChangeBookmark(BookBookmark.Gardens);check(initial.SubjectCount==9);initial.ChangeBookmark(BookBookmark.Colossi);
            int changes=0;initial.DestinationChanged+=()=>changes++;check(initial.RequestTurn(1));for(int i=0;i<30;i++)check(!initial.RequestTurn(1));check(changes==1 && initial.SubjectIndex==1);initial.CompleteTransition();
            initial.ChangeBookmark(BookBookmark.Stories);check(initial.SubjectCount==20 && initial.BeginReading("chapter.fixture",3));check(initial.Reading.Move(1) && initial.SubjectId==initial.Reading.OwnerId);initial.EndReading();
            initial.Close();initial.Reenter();check(initial.SubjectIndex==0 && initial.Bookmark==BookBookmark.Colossi);check(UnityFormalCampaignJson.Encode(formalCampaign.Snapshot)==saveBefore);
            book=CreateFormalBook();
            if(args.Contains("-bookLast")){for(int i=0;i<14;i++)book.TurnPage(1);check(!book.CanTurnNext);}
            else if(args.Contains("-bookEmpty")){book=new BookNavigationState(Array.Empty<BookOrderedSubject>(),BookBookmark.Gardens);check(book.SubjectId==null && !book.CanFlip);}
            else if(args.Contains("-bookEmptyHero")){book=new BookNavigationState(Array.Empty<BookOrderedSubject>(),BookBookmark.Heroines);check(book.SubjectId==null && !book.CanFlip);}
            else if(args.Contains("-bookStories")){book.ChangeBookmark(BookBookmark.Stories);for(int i=0;i<15;i++)book.TurnPage(1);check(book.SubjectId==combatDefinitions.FormationIds[0]);}
            else if(args.Contains("-bookStoryDetails")){book.ChangeBookmark(BookBookmark.Stories);for(int i=0;i<15;i++)book.TurnPage(1);book.FlipPage();check(book.Face==BookFace.Details && book.SubjectId==combatDefinitions.FormationIds[0]);}
            else if(args.Contains("-bookGarden")){book.ChangeBookmark(BookBookmark.Gardens);book.TurnPage(1);check(book.SubjectId=="garden.crystal-highland");}
            else if(args.Contains("-bookGardenLast")){book.ChangeBookmark(BookBookmark.Gardens);for(int i=0;i<8;i++)book.TurnPage(1);check(book.SubjectId=="garden.integrated-world" && !book.CanTurnNext);}
            else if(args.Contains("-bookHero")){book.ChangeBookmark(BookBookmark.Heroines);check(!book.CanTurnPrevious);}
            Debug.Log("PLAN6_BOOK_PLAYER_PASS "+checks+" checks / subject="+(book.SubjectId??"empty"));
        }
    }
}
