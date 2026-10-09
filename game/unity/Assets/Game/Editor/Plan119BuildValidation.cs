using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public sealed class Plan119BuildValidation:IPreprocessBuildWithReport
{
    public int callbackOrder=>-90;
    public void OnPreprocessBuild(BuildReport report)=>Validate();
    public static void Validate()
    {
        if(Directory.Exists(Plan119LoadFixtureImporter.Root))throw new BuildFailedException("Test image fixtures must be removed before building the product.");
        if(AssetDatabase.FindAssets("t:Model",new[]{"Assets/Game"}).Length!=0)throw new BuildFailedException("Retired 3D models must not be packaged in the 2D product.");
        Debug.Log("PRODUCTION_2D_ONLY_PASS modelAssets=0");
        T Load<T>(string path)=>JsonUtility.FromJson<T>((Resources.Load<TextAsset>(path)??throw new ArgumentException("Missing production catalog: "+path)).text);
        var combat=Load<CombatDefinitionCatalog>("Combat/battle-plan11-7");
        ProductionEconomyCatalog.Kinder(combat.HeroineIds).Validate(combat.HeroineIds);
        var story=Load<ProductionStoryContent>("Story/plan10-shangrila-story-content");
        var errors=HeroineProductionValidation.Errors(combat,story,Load<HeroinePortraitCatalog>("UI/heroine-portrait-framing"),Load<HeroineWeaponTreeCatalog>("UI/heroine-weapon-trees"));
        if(errors.Length>0)throw new BuildFailedException(string.Join("\n",errors));
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        foreach(var asset in home.assets)if(Resources.Load(asset.resourcePath)==null)throw new BuildFailedException(asset.id+" / image / Missing resource "+asset.resourcePath);
        ProductionStoryCatalog.Collection(combat,story).Validate();
        Debug.Log("PLAN11_9_PRODUCTION_BUILD_PASS forms="+combat.heroines.Length+" people="+combat.heroines.Select(h=>combat.PersonId(h.id)).Distinct().Count()+" jobs="+combat.heroines.Select(h=>h.jobId).Distinct().Count());
    }
    public static void Build(){Validate();Plan15BookWatchBuild.Build();}
    public static void BuildReleaseCandidate()
    {
        var args=Environment.GetCommandLineArgs();
        int at=Array.IndexOf(args,"-buildOutput");
        if(at<0 || at+1>=args.Length)throw new ArgumentException("Release candidate requires a separate explicit output.");
        if(string.Equals(Path.GetFullPath(args[at+1]),Path.GetFullPath("../Builds/plan11-9/newASTER.exe"),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Preserve the development build; choose a separate candidate directory.");
        string previous=PlayerSettings.bundleVersion;
        try{PlayerSettings.bundleVersion="1.0.0-rc.1";Build();}
        finally{PlayerSettings.bundleVersion=previous;AssetDatabase.SaveAssets();}
    }
}
