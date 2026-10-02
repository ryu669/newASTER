using System;
using System.IO;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    /// <summary>端末セーブの入出力。書込み途中で既存セーブを破壊しないよう一時ファイルを経由する。</summary>
    public static class CampaignSaveStore
    {
        // New-game world slot: never read or overwrite the legacy trial file.
        private const string FileName = "campaign-formal-world-v1.json";
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(CampaignState campaign)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            Directory.CreateDirectory(Application.persistentDataPath);
            var temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(campaign.CreateSave(), true));
            if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, SavePath + ".bak");
            else File.Move(temporaryPath, SavePath);
        }

        public static bool TryLoad(out CampaignSaveV2 save)
        {
            save = null;
            if (TryRead(SavePath, out save)) return true;
            return TryRead(SavePath + ".bak", out save);
        }

        private static bool TryRead(string path, out CampaignSaveV2 save)
        {
            save = null;
            if (!File.Exists(path)) return false;
            try
            {
                var parsed = JsonUtility.FromJson<CampaignSaveV2>(File.ReadAllText(path));
                if (parsed == null || parsed.version != CampaignSaveV2.Version || parsed.materials < 0 || parsed.terraformingExperience < 0) return false;
                save = parsed;
                return true;
            }
            catch (IOException) { return false; }
            catch (ArgumentException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
