using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

// A file/hash check proves identity, not that an illustration depicts its intended subject.
public static class Plan12AssetReviewGate
{
    [Serializable] private sealed class Review { public Form[] forms; }
    [Serializable] private sealed class Form { public string key; public Asset[] assets; }
    [Serializable] private sealed class Asset { public string key,path,sha256; public Purpose purposeReview; public RuntimeReview runtimeReview; }
    [Serializable] private sealed class RuntimeReview {public string status,reviewedSha256;public Capture[] captureFiles;}
    [Serializable] private sealed class Capture {public string path,sha256;}
    [Serializable] private sealed class Purpose { public string status,subject; }
    [Serializable] private sealed class Bundle {public string heroineId,layoutPath; public Component[] components;}
    [Serializable] private sealed class Component {public string path,sha256,status;}
    [Serializable] private sealed class TreeLayout {public string heroineId,foliageAsset,flowerAsset;public Decoration[] decorations;}
    [Serializable] private sealed class Decoration {public string asset;}
    private static string Digest(string path){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    private static void ValidateBundle(string root,string hero,string path)
    {
        var bundle=JsonUtility.FromJson<Bundle>(File.ReadAllText(path));
        if(bundle.heroineId!="heroine."+hero)throw new InvalidOperationException("Tree bundle identity mismatch: "+hero);
        var layout=JsonUtility.FromJson<TreeLayout>(File.ReadAllText(Path.Combine(root,bundle.layoutPath)));
        if(layout.heroineId!=bundle.heroineId)throw new InvalidOperationException("Tree layout identity mismatch: "+hero);
        var required=new[]{"common-tree",layout.foliageAsset,layout.flowerAsset??"crimson-flower"}.Concat(layout.decorations.Select(p=>p.asset)).Select(k=>"game/unity/Assets/Game/Resources/UI/WeaponTrees/"+k+".png").Append(bundle.layoutPath).Distinct().OrderBy(p=>p).ToArray();
        if(!required.SequenceEqual(bundle.components.Select(c=>c.path).OrderBy(p=>p)))throw new InvalidOperationException("Incomplete layered tree bundle: "+hero);
        foreach(var component in bundle.components)if(component.status!="approved" || Digest(Path.Combine(root,component.path))!=component.sha256)throw new InvalidOperationException("Unreviewed or changed tree component: "+component.path);
    }
    public static void Validate()
    {
        string root=Path.GetFullPath("../..");
        var review=JsonUtility.FromJson<Review>(File.ReadAllText(Path.Combine(root,"docs/production/plan12-first-batch-asset-review.json")));
        foreach(string hero in new[]{"iron-maiden","avalanche","anathema","shiva"}){
            var form=review.forms.Single(f=>f.key==hero);
            if(form.assets.Length!=21 || form.assets.Select(a=>a.key).Distinct().Count()!=21)throw new InvalidOperationException("Incomplete asset review: "+hero);
            foreach(var asset in form.assets){
                if(asset.purposeReview?.status!="approved")throw new InvalidOperationException("Asset purpose review required: "+hero+"/"+asset.key);
                if(asset.key=="equipment-tree-v1"){
                    if(asset.purposeReview.subject=="layered-branching-tree-without-people")ValidateBundle(root,hero,Path.Combine(root,asset.path));
                    else if(asset.purposeReview.subject!="branching-tree-without-people")throw new InvalidOperationException("Equipment tree must depict branches without people: "+hero);
                }
                using(var hash=SHA256.Create()){
                    string digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Path.Combine(root,asset.path)))).Replace("-","").ToLowerInvariant();
                    if(digest!=asset.sha256)throw new InvalidOperationException("Asset changed after visual review: "+hero+"/"+asset.key);
                }
            }
        }
    }
    public static void ValidateRelease()
    {
        Validate();
        string root=Path.GetFullPath("../..");
        var review=JsonUtility.FromJson<Review>(File.ReadAllText(Path.Combine(root,"docs/production/plan12-first-batch-asset-review.json")));
        foreach(string hero in new[]{"aeon","iron-maiden","avalanche","anathema","shiva"}){
            var form=review.forms.Single(f=>f.key==hero);
            if(form.assets.Length!=21)throw new InvalidOperationException("Incomplete release art: "+hero);
            foreach(var asset in form.assets){
                var runtime=asset.runtimeReview;
                if(asset.purposeReview?.status!="approved" || Digest(Path.Combine(root,asset.path))!=asset.sha256 || runtime?.status!="approved" || runtime.reviewedSha256!=asset.sha256 || runtime.captureFiles==null || runtime.captureFiles.Length==0)
                    throw new InvalidOperationException("Runtime visual review required: "+hero+"/"+asset.key);
                // Captures are local QA outputs; their recorded digests remain in the review ledger.
                foreach(var capture in runtime.captureFiles)if(capture.sha256==null || capture.sha256.Length!=64 || (File.Exists(Path.Combine(root,capture.path)) && Digest(Path.Combine(root,capture.path))!=capture.sha256))throw new InvalidOperationException("Runtime evidence changed: "+capture.path);
            }
        }
        Debug.Log("PLAN12_RELEASE_ART_PASS 5 forms / 105 source and runtime reviews");
    }
}
