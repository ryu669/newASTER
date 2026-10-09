using System;
using System.Collections.Generic;
using System.Linq;
namespace NewAster.Core
{
    public sealed partial class GardenLifeRuntime
    {
        private readonly Dictionary<string,string> dailyOwners=new Dictionary<string,string>();
        private readonly Dictionary<string,string> dailySlotOwners=new Dictionary<string,string>();
        public bool OwnsDailySession(string session)=>dailyOwners.Values.Contains(session);
        public bool TryReserveDailySession(string session,string[] forms,string slotId=null)
        {
            if(Paused || !HomeExperienceCatalog.Id(session) || forms==null || forms.Length==0 || forms.Length>8 || forms.Distinct().Count()!=forms.Length || forms.Any(id=>dailyOwners.ContainsKey(id) || !Agents.Any(a=>a.heroineId==id)))return false;
            if(slotId!=null && (!slots.Any(s=>s.id==slotId && s.capacity>=forms.Length) || dailySlotOwners.ContainsKey(slotId) || reservations.TryGetValue(slotId,out var users) && users.Any(id=>!forms.Contains(id))))return false;
            foreach(var a in Agents.Where(a=>forms.Contains(a.heroineId))){if(a.partnerId!=null)EndSocial(a);Release(a);dailyOwners.Add(a.heroineId,session);a.kind="DailyInteraction";a.tag="look";}
            if(slotId!=null){dailySlotOwners.Add(slotId,session);reservations[slotId]=forms.ToList();foreach(var a in Agents.Where(a=>forms.Contains(a.heroineId)))a.slotId=slotId;}
            return true;
        }
        public void ReleaseDailySession(string session)
        {
            foreach(var id in dailyOwners.Where(p=>p.Value==session).Select(p=>p.Key).ToArray()){
                dailyOwners.Remove(id);var a=Agents.Single(x=>x.heroineId==id);Idle(a);
            }
            foreach(var id in dailySlotOwners.Where(p=>p.Value==session).Select(p=>p.Key).ToArray()){dailySlotOwners.Remove(id);reservations.Remove(id);}
        }
    }
}
