using System;
using System.IO;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private void PrepareSaveManagementCapture(string view)
        {
            // A capture always owns a fresh directory, never the player's normal save slots.
            string root=Path.Combine(Path.GetDirectoryName(capturePath),"save-acceptance-"+Guid.NewGuid().ToString("N"));
            InitializeSaveSlots(root,0);var original=formalCampaign.Snapshot;
            for(int i=0;i<4;i++){var next=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(original));next.revision+=i;next.growth.nectar+=i*100;saveSlots.Save(1,next,DateTime.UtcNow);}
            saveSlots.Save(2,original,DateTime.UtcNow);ResetForSlotLoad(saveSlots.Load(1));formalDiagnostic=false;
            saveSelectedSlot=2;SaveSelectedSlot();AcceptanceCheck(saveSlots.ActiveSlot==2,"SV-18 manual save selects slot2");
            saveSelectedSlot=1;LoadSelectedSlot();AcceptanceCheck(saveSlots.ActiveSlot==1,"SV-18 load selects slot1");
            long before=formalCampaign.Snapshot.revision;restoreChoice=saveSlots.Inspect(1,2);RestoreChosenBackup();
            AcceptanceCheck(formalCampaign.Snapshot.revision<before && saveSlots.Inspect(1,1).Status==SaveSlotStatus.Ready,"SV-18 restore reloads progress and preserves current");
            deleteChoice=saveSlots.RequestDelete(2);saveSlots.ConfirmDelete(deleteChoice);DeleteChosenSlot();
            AcceptanceCheck(saveSlots.Inspect(2).Status==SaveSlotStatus.Empty && saveSlots.Inspect(1).Status==SaveSlotStatus.Ready,"SV-18 two-confirmation delete isolates other slot");
            saveSelectedSlot=2;NewGameInSelectedSlot();AcceptanceCheck(saveSlots.Inspect(2).Status==SaveSlotStatus.Ready,"SV-18 deleted slot reusable");
            deleteChoice=saveSlots.RequestDelete(2);saveSlots.ConfirmDelete(deleteChoice);DeleteChosenSlot();AcceptanceCheck(title && saveSlotDeleted,"SV-18 active slot deletion returns title and stops autosave");
            saveSelectedSlot=1;LoadSelectedSlot();OpenSaveManagement();saveSelectedSlot=1;saveManagementMessage="保存・ロード・復元・個別削除を確認しました。";
            if(view=="save-restore")restoreChoice=saveSlots.Inspect(1,1);
            if(view=="save-delete-first" || view=="save-delete-second"){deleteChoice=saveSlots.RequestDelete(1);if(view=="save-delete-second")saveSlots.ConfirmDelete(deleteChoice);}
            if(view=="save-empty")saveSelectedSlot=2;
            Debug.Log("SV18_WINDOWS_PLAYER_PASS save/load/restore/delete/reuse/active-delete physicalInput=0 root="+root);
        }
    }
}
