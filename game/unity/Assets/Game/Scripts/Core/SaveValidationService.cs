using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace NewAster.Core
{
    [Serializable] public sealed class SaveHeader
    {
        public int saveGeneration,schemaVersion;
        public string gameVersion,saveId,savedAtUtc;
    }
    // The existing unified progress payload remains the sole representation of game systems.
    // It is stored as JSON text so its exact bytes can be authenticated before decoding/migrating.
    [Serializable] public sealed class GameSave
    {
        public SaveHeader header;
        public string payload,checksum;
    }
    public sealed class UnsupportedSaveException:ArgumentException
    {public UnsupportedSaveException(string message):base(message){} }
    public sealed class SaveMigrationService
    {
        public int CurrentSchema {get;}
        private readonly Dictionary<int,Func<FormalCampaignSave,FormalCampaignSave>> steps=new Dictionary<int,Func<FormalCampaignSave,FormalCampaignSave>>();
        public SaveMigrationService(int currentSchema=1){if(currentSchema<1)throw new ArgumentOutOfRangeException(nameof(currentSchema));CurrentSchema=currentSchema;}
        public void Register(int from,Func<FormalCampaignSave,FormalCampaignSave> migration)
        {if(from<1 || from>=CurrentSchema || migration==null || steps.ContainsKey(from))throw new ArgumentException("Invalid migration registration.");steps.Add(from,migration);}
        public FormalCampaignSave Migrate(int schema,FormalCampaignSave save)
        {
            if(schema<1 || schema>CurrentSchema)throw new UnsupportedSaveException("Unsupported save schema.");
            for(int version=schema;version<CurrentSchema;version++){
                if(!steps.TryGetValue(version,out var step))throw new UnsupportedSaveException("Migration is not available.");
                save=step(save)??throw new ArgumentException("Migration returned empty progress.");save.Validate();
            }
            return save;
        }
    }
    public sealed class SaveValidationService
    {
        public int Generation {get;}
        public string GameVersion {get;}
        public SaveMigrationService Migrations {get;}
        private readonly Func<FormalCampaignSave,string> encodeProgress;
        private readonly Func<string,FormalCampaignSave> decodeProgress;
        private readonly Func<GameSave,string> encodeDocument;
        private readonly Func<string,GameSave> decodeDocument;
        private readonly Action<FormalCampaignSave> validateContent;
        public SaveValidationService(int generation,string gameVersion,Func<FormalCampaignSave,string> encodeProgress,Func<string,FormalCampaignSave> decodeProgress,Func<GameSave,string> encodeDocument,Func<string,GameSave> decodeDocument,Action<FormalCampaignSave> validateContent=null,SaveMigrationService migrations=null)
        {
            if(generation<0 || generation>1 || string.IsNullOrWhiteSpace(gameVersion))throw new ArgumentException("Invalid save generation.");
            Generation=generation;GameVersion=gameVersion;this.encodeProgress=encodeProgress??throw new ArgumentNullException(nameof(encodeProgress));this.decodeProgress=decodeProgress??throw new ArgumentNullException(nameof(decodeProgress));this.encodeDocument=encodeDocument??throw new ArgumentNullException(nameof(encodeDocument));this.decodeDocument=decodeDocument??throw new ArgumentNullException(nameof(decodeDocument));this.validateContent=validateContent;Migrations=migrations??new SaveMigrationService();
        }
        public FormalCampaignSave Copy(FormalCampaignSave save)=>decodeProgress(encodeProgress(save));
        public GameSave Create(FormalCampaignSave progress,string saveId,DateTime now)
        {
            progress.Validate();validateContent?.Invoke(progress);
            var doc=new GameSave{header=new SaveHeader{saveGeneration=Generation,schemaVersion=Migrations.CurrentSchema,gameVersion=GameVersion,saveId=saveId,savedAtUtc=now.ToUniversalTime().ToString("O",CultureInfo.InvariantCulture)},payload=encodeProgress(progress)};
            doc.checksum=Checksum(doc);Validate(doc);return doc;
        }
        public string Encode(GameSave save)=>encodeDocument(save);
        public GameSave Read(string path)
        {
            // A malformed document is rejected before any game state is published.
            var doc=decodeDocument(File.ReadAllText(path,Encoding.UTF8));Validate(doc);return doc;
        }
        public FormalCampaignSave Validate(GameSave doc)
        {
            var h=doc?.header;
            if(h==null || h.schemaVersion<1 || !Guid.TryParseExact(h.saveId,"N",out _) || string.IsNullOrWhiteSpace(h.gameVersion) || !DateTime.TryParseExact(h.savedAtUtc,"O",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var date) || date.Kind!=DateTimeKind.Utc || doc.payload==null || doc.payload.Length>64*1024*1024 || doc.checksum!=Checksum(doc))throw new ArgumentException("Save integrity check failed.");
            if(h.saveGeneration!=Generation || h.schemaVersion>Migrations.CurrentSchema)throw new UnsupportedSaveException("Save belongs to a different generation or a newer game.");
            var save=decodeProgress(doc.payload)??throw new ArgumentException("Empty progress.");
            save=Migrations.Migrate(h.schemaVersion,save);save.Validate();validateContent?.Invoke(save);return save;
        }
        public static string Checksum(GameSave doc)
        {
            var h=doc.header;string canonical=h.saveGeneration+"\n"+h.schemaVersion+"\n"+h.gameVersion.Length+":"+h.gameVersion+"\n"+h.saveId+"\n"+h.savedAtUtc+"\n"+doc.payload;
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-","").ToLowerInvariant();
        }
    }
}
