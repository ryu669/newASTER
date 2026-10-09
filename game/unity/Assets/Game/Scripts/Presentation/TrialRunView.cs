using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private TrialRunTelemetry trialTelemetry;
        private StreamWriter trialLog;
        private long trialOperation;
        private bool trialInactive;
        private string trialScreen;
        private bool trialReference;
        private string trialTimingPhase;

        private bool SaveTrialObservedCampaign(FormalCampaignSave next)
        {
            TrialObserve("save","start","revision="+next.revision);
            try {
                bool success=saveSlots==null?formalCampaignStore.Save(next):!saveSlotBlocked && !saveSlotDeleted && delayedSaves.SaveImmediate(next,DateTime.UtcNow);
                ObserveTrialSave(next,success);return success;
            } catch { TrialObserve("save","failed");throw; }
        }

        private void ObserveTrialSave(FormalCampaignSave next,bool success)
        {
            if(trialTelemetry==null)return;
            try {
                TrialObserve("save",success?"committed":"failed","revision="+next.revision);
                if(!success)return;
                TrialObserve("economy","balance",$"revision={next.revision};nectar={next.growth.nectar};crystals={next.growth.awakeningCrystals};stones={next.growth.stones};materials={string.Join(",",(next.collection?.materials??Array.Empty<CollectionMaterial>()).Select(m=>m.id+":"+m.amount))}");
                foreach(var receipt in next.growth.receipts)
                    TrialObserve("economy","transaction",receipt.signature,"growth/"+receipt.transactionId,operationId:receipt.transactionId);
                foreach(var receipt in next.home?.receipts??Array.Empty<HomeReceipt>())
                    TrialObserve("garden",receipt.kind,receipt.signature,"home/"+receipt.transactionId,operationId:receipt.transactionId);
                foreach(var line in next.home?.readLineKeys??Array.Empty<HomeReadLine>())
                    TrialObserve("reading","line-committed",line.sceneId,"line/"+line.sceneId+"/"+line.scriptVersion+"/"+line.lineId);
            } catch { trialTelemetry.MarkIncomplete(); }
        }

        private void InitializeTrialTelemetry()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-plan8Telemetry")) return;
            try {
                Func<string,string> arg = key => {
                    var positions = Enumerable.Range(0,args.Length).Where(i=>args[i]==key).ToArray();
                    if (positions.Length!=1 || positions[0]+1>=args.Length) throw new ArgumentException("Missing telemetry option: "+key);
                    return args[positions[0]+1];
                };
                var boundary = TrialDiagnosticBoundary.Create(arg("-plan8RepositoryRoot"),Application.persistentDataPath,arg("-plan8RunId"));
                Directory.CreateDirectory(boundary.DirectoryPath);
                // Exclusive creation also prevents concurrent writers from sharing a run log.
                trialLog = new StreamWriter(new FileStream(Path.Combine(boundary.DirectoryPath,"events.jsonl"),FileMode.CreateNew,FileAccess.Write,FileShare.Read));
                var timer = Stopwatch.StartNew();
                string assembly = Path.Combine(Application.dataPath,"Managed","Assembly-CSharp.dll");
                string hash;
                using (var sha=SHA256.Create()) using(var stream=File.OpenRead(assembly))
                    hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
                string resourceHash,definitionHash;
                using(var sha=SHA256.Create()) using(var stream=File.OpenRead(Path.Combine(Application.dataPath,"resources.assets")))
                    resourceHash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
                var baseline=TrialBaselineResources.ReadAndValidate();
                string source=string.Join("\n",baseline.definitions.Select(d=>d.resourcePath+"\n"+Resources.Load<TextAsset>(d.resourcePath).text));
                using(var sha=SHA256.Create())definitionHash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source))).Replace("-","");
                trialTelemetry = new TrialRunTelemetry(arg("-plan8RunId"),Guid.NewGuid().ToString("N"),baseline.contentVersion,
                    hash,args.Contains("-presentationCapture")?"diagnostic":"mouse-keyboard",
                    ()=>timer.Elapsed.TotalSeconds,()=>DateTime.UtcNow,row=>{trialLog.WriteLine(JsonUtility.ToJson(row));trialLog.Flush();},resourceHash,definitionHash);
                TrialObserve("timing","startup");
            } catch (Exception e) {
                try { trialLog?.Dispose(); } catch { }
                trialLog=null;
                UnityEngine.Debug.LogWarning("PLAN8_TELEMETRY_INCOMPLETE startup "+e.GetType().Name);
            }
        }

        private void TrialObserve(string category,string action,string detail="",string eventId=null,long tick=0,double duration=0,string operationId=null)
        {
            if(trialTelemetry==null || trialReference)return;
            string op="op-"+(++trialOperation);
            trialTelemetry.Record(eventId??op,category,action,detail,encounter==null?"":battleId??"",encounter==null?0:encounter.Seed,operationId??op,tick,duration);
        }

        private void UpdateTrialTelemetry()
        {
            if(trialTelemetry==null)return;
            bool inactive=!Application.isFocused || paused || retreat || help || (adv!=null && adv.Paused);
            if(inactive!=trialInactive){trialTelemetry.SetInactive(inactive);trialInactive=inactive;TrialObserve("timing",inactive?"pause":"resume");}
            string screen=recoveryActive?"recovery":title?"title":adv!=null?"reading":storyText!=null?"story":result!=null?"result":encounter!=null?"battle":kinderGarden?"kinder":"book";
            if(screen!=trialScreen){trialScreen=screen;TrialObserve("navigation","screen",screen);}
            string phase=inactive?"inactive":encounter==null || result!=null?"outside-battle":playback.Busy?"presentation":"decision";
            if(phase!=trialTimingPhase){
                if(trialTimingPhase!=null)TrialObserve("timing",trialTimingPhase+"-end");
                trialTimingPhase=phase;TrialObserve("timing",phase+"-start");
            }
        }

        private void FinishTrialTelemetry()
        {
            if(trialTelemetry==null)return;
            TrialObserve("timing","shutdown",trialTelemetry.Incomplete?"incomplete":"complete");
            try { trialLog?.Dispose(); } catch { trialTelemetry.MarkIncomplete();UnityEngine.Debug.LogWarning("PLAN8_TELEMETRY_INCOMPLETE close"); }
            UnityEngine.Debug.Log(trialTelemetry.Incomplete?"PLAN8_TELEMETRY_INCOMPLETE":"PLAN8_TELEMETRY_COMPLETE events="+trialTelemetry.RecordedCount);
            trialTelemetry=null;trialLog=null;
        }
    }
}
