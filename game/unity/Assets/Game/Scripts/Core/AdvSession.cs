using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace NewAster.Core
{
    public sealed class AdvActorState
    {
        public string HeroineId{get;} public string SlotId{get;} public string OutfitId{get;} public string ExpressionAsset{get;} public string PoseAsset{get;} public bool PosePlaceholder{get;}
        internal AdvActorState(HomeAdvCommand cmd,HomeDisplaySet d){HeroineId=cmd.heroineId;SlotId=cmd.slotId;OutfitId=cmd.outfitId;ExpressionAsset=(d.expressions.FirstOrDefault(e=>e.id==cmd.expressionId)??d.expressions.Single(e=>e.id=="expression.normal")).assetId;var pose=d.poses.FirstOrDefault(p=>p.id==cmd.poseId);PoseAsset=pose?.assetId;PosePlaceholder=pose==null;}
    }
    public sealed class AdvSession
    {
        private readonly HomeExperienceCatalog catalog;private readonly HomeAdvScript script;private readonly HashSet<string> read;private readonly List<AdvActorState> actors=new List<AdvActorState>();private readonly List<string> backlog=new List<string>();
        private int index,visible;private double reveal,autoElapsed,transitionRemaining;private int[] elements=Array.Empty<int>();
        public string SourceId{get;} public string SceneId=>script.id;public int ScriptVersion=>script.scriptVersion;public bool Replay{get;}
        public bool EndReached{get;private set;} public bool Completed{get;private set;} public bool Paused{get;private set;} public bool Auto{get;private set;} public bool Skip{get;private set;}
        public string BackgroundId{get;private set;} public string CgId{get;private set;} public bool HideActors{get;private set;} public string SoundId{get;private set;} public string SoundChannel{get;private set;}
        public string LineId{get;private set;} public string Text{get;private set;}=""; public string SpeakerId{get;private set;} public int CharactersPerSecond{get;private set;}=30;
        public string VisibleText=>visible>=elements.Length?Text:Text.Substring(0,elements[visible]);public bool FullyVisible=>visible>=elements.Length;public bool InTransition=>transitionRemaining>0;
        public IReadOnlyList<AdvActorState> Actors=>actors.AsReadOnly();public IReadOnlyList<string> Backlog=>backlog.AsReadOnly();public IReadOnlyList<HomeReadLine> NewlyRead=>newlyRead.AsReadOnly();private readonly List<HomeReadLine> newlyRead=new List<HomeReadLine>();
        public AdvSession(HomeExperienceCatalog c,string scene,string source,bool replay,IEnumerable<HomeReadLine> existing)
        {
            c.Validate();catalog=c;script=c.scripts.Single(s=>s.id==scene);SourceId=source;Replay=replay;read=new HashSet<string>((existing??Array.Empty<HomeReadLine>()).Where(l=>l.sceneId==scene && l.scriptVersion==script.scriptVersion).Select(l=>l.lineId));RunCommands();
        }
        private void RunCommands()
        {
            LineId=null;Text="";visible=0;reveal=0;autoElapsed=0;
            while(index<script.commands.Length){var cmd=script.commands[index++];switch(cmd.kind){
                case "background":BackgroundId=cmd.assetId;transitionRemaining=cmd.durationMs/1000d;break;
                case "actor":actors.RemoveAll(a=>a.HeroineId==cmd.heroineId);actors.Add(new AdvActorState(cmd,catalog.displays.Single(d=>d.heroineId==cmd.heroineId && d.outfitId==cmd.outfitId)));break;
                case "hideActor":actors.RemoveAll(a=>a.HeroineId==cmd.heroineId);break;
                case "cg":CgId=cmd.assetId;HideActors=cmd.hideActors;transitionRemaining=cmd.durationMs/1000d;break;
                case "hideCg":CgId=null;HideActors=false;break;
                case "sound":SoundId=cmd.audioId;SoundChannel=cmd.channel;break;
                case "line":LineId=cmd.lineId;Text=catalog.texts.Single(t=>t.id==cmd.textId).text;SpeakerId=cmd.speakerId;elements=StringInfo.ParseCombiningCharacters(Text);backlog.Add((SpeakerId??"地の文")+"："+Text);if(Skip && !read.Contains(LineId))Skip=false;return;
                case "end":EndReached=true;Auto=false;Skip=false;return;
                default:throw new ArgumentException("Unsupported ADV command.");
            }}
        }
        public bool Advance()
        {
            if(Paused || InTransition || EndReached)return false;
            if(!FullyVisible){visible=elements.Length;return false;}
            if(read.Add(LineId) && !Replay)newlyRead.Add(new HomeReadLine{sceneId=SceneId,scriptVersion=ScriptVersion,lineId=LineId});RunCommands();return true;
        }
        public void Tick(double seconds)
        {
            if(Paused || EndReached || seconds<=0 || seconds>2)return;
            if(transitionRemaining>0){transitionRemaining=Math.Max(0,transitionRemaining-seconds);return;}
            if(Skip){if(!read.Contains(LineId)){Skip=false;return;}visible=elements.Length;Advance();return;}
            if(!FullyVisible){reveal+=seconds*CharactersPerSecond;visible=Math.Min(elements.Length,(int)reveal);return;}
            if(Auto){autoElapsed+=seconds;if(autoElapsed>=1){autoElapsed=0;Advance();}}
        }
        public void SetSpeed(int speed){if(!new[]{15,30,60,120}.Contains(speed))throw new ArgumentException("Unsupported text speed.");CharactersPerSecond=speed;}
        public void SetAuto(bool enabled){if(!Paused && !EndReached){Auto=enabled;autoElapsed=0;Skip=false;}}
        public void SetSkip(bool enabled){if(!Paused && !EndReached){Skip=enabled && read.Contains(LineId);Auto=false;}}
        public void Pause(){Paused=true;Auto=false;Skip=false;autoElapsed=0;}
        public void Resume(){Paused=false;autoElapsed=0;}
        public void MarkCommitted(){if(!EndReached)throw new InvalidOperationException("Scene not at end.");Completed=true;}
    }
    public sealed partial class FormalCampaignJournal
    {
        public GrowthCommitResult CommitAdvLine(FormalHomeRequest request,HomeExperienceCatalog c,HomeReadLine line,Func<FormalCampaignSave,bool> save)
        {
            if(request.Kind!="advRead" || request.OperationKey!=line.sceneId+"/"+line.scriptVersion+"/"+line.lineId)throw new ArgumentException("Read signature mismatch.");
            return CommitHome(request,c,h=>{if(!h.readLineKeys.Any(l=>l.sceneId==line.sceneId && l.scriptVersion==line.scriptVersion && l.lineId==line.lineId))h.readLineKeys=h.readLineKeys.Concat(new[]{new HomeReadLine{sceneId=line.sceneId,scriptVersion=line.scriptVersion,lineId=line.lineId}}).ToArray();return h;},save);
        }
        public GrowthCommitResult CommitAdvEnd(FormalHomeRequest request,HomeExperienceCatalog c,AdvSession session,Func<FormalCampaignSave,bool> save)
        {
            if(session.Replay || !session.EndReached || request.Kind!="sceneEnd" || request.OperationKey!=session.SourceId+"/"+session.SceneId+"/"+session.ScriptVersion)throw new ArgumentException("Invalid scene completion.");
            return CommitHomeCandidate(request,c,s=>{
                var ev=c.events.SingleOrDefault(e=>e.id==session.SourceId);var ch=c.chapters.SingleOrDefault(ch0=>ch0.id==session.SourceId);HomeCost[] rewards;
                if(ev!=null){if(ev.sceneId!=session.SceneId || !s.home.unlockedEventIds.Contains(ev.id))throw new ArgumentException("Event is not unlocked.");s.home.readEventIds=s.home.readEventIds.Union(new[]{ev.id}).ToArray();if(ev.establishesLover)s.home.loverHeroineIds=s.home.loverHeroineIds.Union(new[]{ev.heroineId}).ToArray();rewards=ev.rewards;}
                else if(ch!=null){if(ch.sceneId!=session.SceneId || !s.world.unlockedStoryIds.Contains(ch.id))throw new ArgumentException("Chapter is not unlocked.");s.world.readStoryIds=s.world.readStoryIds.Union(new[]{ch.id}).ToArray();rewards=ch.rewards;}
                else throw new ArgumentException("Unknown scene source.");
                if(rewards.Length>0 && !s.home.claimedRewardIds.Contains(session.SourceId)){HomeRules.Grant(s,c,rewards);s.home.claimedRewardIds=s.home.claimedRewardIds.Union(new[]{session.SourceId}).ToArray();}
                HomeConditions.Refresh(s,c);return s;
            },save);
        }
    }
}
