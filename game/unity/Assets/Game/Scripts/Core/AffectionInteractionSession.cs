using System;
namespace NewAster.Core
{
    public sealed class AffectionInteractionSession
    {
        public string Id{get;} public string FormId{get;} public string ContentId{get;} public string GardenId{get;}
        public bool Completed{get;private set;} public bool Cancelled{get;private set;}
        private float remaining;
        public AffectionInteractionSession(string id,string form,string content,string garden,float seconds)
        {if(!HomeExperienceCatalog.Id(id) || !HomeExperienceCatalog.Id(form) || !HomeExperienceCatalog.Id(content) || !HomeExperienceCatalog.Id(garden) || seconds<=0 || float.IsNaN(seconds) || float.IsInfinity(seconds))throw new ArgumentException("Invalid player interaction.");Id=id;FormId=form;ContentId=content;GardenId=garden;remaining=seconds;}
        public void Tick(float seconds,bool active)
        {if(Completed || Cancelled || !active || seconds<=0 || seconds>2)return;remaining-=seconds;if(remaining<=0)Completed=true;}
        public void Cancel(){if(!Completed)Cancelled=true;}
    }
}
