using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    // Selection happens only after a completed enemy action; it never advances battle RNG.
    public static class TrialSingingSelector
    {
        public static string Select(CollectionCatalog catalog,string owner,IEnumerable<string> formation,IEnumerable<string> owned,IEnumerable<string> heard,Random random,bool preferMissing)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            var songs=catalog.owners.Single(o=>o.id==owner && o.kind=="colossus").poemIds;
            if(!preferMissing)return songs[random.Next(songs.Length)];
            var acquired=new HashSet<string>(owned);var current=new HashSet<string>(heard);var party=new HashSet<string>(formation);
            var needed=new HashSet<string>(songs.Where(id=>!acquired.Contains(id)));
            foreach(var link in catalog.links.Where(l=>!acquired.Contains(l.targetPoemId) && party.Contains(catalog.poems.Single(p=>p.id==l.targetPoemId).ownerId)))needed.Add(link.sourcePoemId);
            var candidates=songs.Where(id=>needed.Contains(id) && !current.Contains(id)).ToArray();
            return candidates.Length>0?candidates[random.Next(candidates.Length)]:songs[random.Next(songs.Length)];
        }
    }
}
