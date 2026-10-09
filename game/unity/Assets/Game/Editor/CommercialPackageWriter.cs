using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NewAster.Core;
using UnityEditor;
using UnityEngine;

public static class CommercialPackageWriter
{
    [Serializable] private sealed class FileRecord { public string path,sha256; public long bytes; }
    [Serializable] private sealed class Inventory { public string version,kind; public FileRecord[] files; }
    private static string Hash(string path){using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    private static FileRecord Record(string root,string path)=>new FileRecord{path=path.Substring(root.Length+1).Replace('\\','/'),bytes=new FileInfo(path).Length,sha256=Hash(path)};
    public static void Write(string player)
    {
        string root=Path.GetDirectoryName(Path.GetFullPath(player));
        var source=Resources.Load<TextAsset>("Combat/battle-plan11-7");
        if(source==null)throw new InvalidOperationException("Missing production catalog for package.");
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(source.text);combat.Validate();
        string version=PlayerSettings.bundleVersion;
        int people=combat.heroines.Select(h=>combat.PersonId(h.id)).Distinct().Count();
        string content=people+"人物・"+combat.heroines.Length+"形態・"+combat.jobs.Length+"ジョブ";
        int.TryParse(version.Split('.')[0],out int major);
        string saveDirectory=major>=1?"release-saves-generation-1":"development-saves";
        string saveRoot="%USERPROFILE%/AppData/LocalLow/"+PlayerSettings.companyName+"/"+PlayerSettings.productName;
        var utf8=new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(root,"README.txt"),
            "newASTER / Version "+version+"\n収録："+content+"\n\n起動：同じフォルダのnewASTER.exeを実行してください。_DataやDLLを分離しないでください。\n対応：Windows x64。1280×720以上の画面で自動描画検証を実施しています。最低動作環境の保証値は未確定です。\n\n操作説明は各画面の？を参照してください。戻るボタンとEscで一段戻ります。\nセーブ管理は万物の書→システム→セーブ管理。2スロット、各2世代のバックアップを利用できます。\n削除はスロット単位です。復元すると進行が巻き戻ります。\n保存場所："+saveRoot+"/"+saveDirectory+"\n開発版と正式版は保存先が別です。開発版セーブの自動削除は行いません。\n\n不具合報告：https://github.com/ryu669/newASTER/issues\nバージョン、発生画面、再現手順、Player.logを添えてください。ログ："+saveRoot+"/Player.log\n個人情報を含むファイルやセーブを公開する前に内容を確認してください。\n\nCREDITS.txt、ThirdPartyNoticesに同梱物の説明とライセンスがあります。\nPACKAGE-MANIFEST.jsonは配布ファイルのSHA-256一覧、RESOURCE-INVENTORY.jsonは同梱元Resourcesの技術一覧です。\n",utf8);
        File.WriteAllText(Path.Combine(root,"CREDITS.txt"),
            "newASTER / Version "+version+"\n収録："+content+"\n\n企画・本文・ゲームルール・UI・実装：newASTER制作\n人物・巨神獣・背景・家具・CG：newASTER用に制作したAI生成美術\nBGM・SE：独自の手続き生成音\n日本語フォント：Noto Sans CJK JP / SIL Open Font License 1.1\nライセンス全文とNOTICE：ThirdPartyNotices/NotoSansCJKjp\n実行環境：Unity 6 / Unity Technologies\n\n参考映像は観察資料として使用。原作の画像・映像・音声は配布物へ転載しません。\nRESOURCE-INVENTORY.jsonはファイル照合用の技術一覧であり、素材の審査や利用許諾を代替するものではありません。\n",utf8);
        string resources=Path.GetFullPath("Assets/Game/Resources");
        var resourceInventory=new Inventory{version=version,kind="source-resources-technical-inventory",files=Directory.GetFiles(resources,"*",SearchOption.AllDirectories).Where(f=>!f.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)).OrderBy(f=>f,StringComparer.Ordinal).Select(f=>Record(resources,f)).ToArray()};
        File.WriteAllText(Path.Combine(root,"RESOURCE-INVENTORY.json"),JsonUtility.ToJson(resourceInventory,true),utf8);
        string manifestPath=Path.Combine(root,"PACKAGE-MANIFEST.json");
        var manifest=new Inventory{version=version,kind="distribution-sha256",files=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(f=>!string.Equals(f,manifestPath,StringComparison.OrdinalIgnoreCase)).OrderBy(f=>f,StringComparer.Ordinal).Select(f=>Record(root,f)).ToArray()};
        File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true),utf8);
        Debug.Log("COMMERCIAL_PACKAGE_PASS version="+version+" forms="+combat.heroines.Length+" files="+manifest.files.Length+" resources="+resourceInventory.files.Length);
    }
}
