using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class HomePoint { public float x,y; }
    [Serializable] public sealed class HomeRect { public float x,y,width,height; }
    [Serializable] public sealed class HomeCost { public string resourceId; public int amount; }
    [Serializable] public sealed class HomeMaterialDef { public string id,colossusId; }
    [Serializable] public sealed class HomeTalkDef { public string id,heroineId; public int affectionGain; public HomeCost[] costs; }
    [Serializable] public sealed class HomeCondition
    { public string kind,domain,id,ownerId; public int value; public HomeCondition[] items=Array.Empty<HomeCondition>(); }
    [Serializable] public sealed class HomeBookSubject
    { public string id,bookmarkId,subjectId; public int pageOrder; public HomeCondition unlockCondition; }
    [Serializable] public sealed class HomeWeaponNode
    { public string id,heroineId,abilityId,skillId; public bool initial; public int attackBonus; public float skillPower=1; public HomePoint treePosition; public string terminal; public string[] parentIds=Array.Empty<string>(); public HomeCost[] costs=Array.Empty<HomeCost>(); }
    [Serializable] public sealed class HomeGardenZone { public string id; public int order; public HomeRect bounds; }
    [Serializable] public sealed class HomeGardenLayout
    { public string id,backgroundAssetId; public string[] foregroundAssetIds=Array.Empty<string>(); public int schemaVersion; public bool unmade; public HomeGardenZone[] zones=Array.Empty<HomeGardenZone>(); }
    [Serializable] public sealed class HomeFurnitureSlot
    { public string id; public HomePoint offset; public string[] actionIds=Array.Empty<string>(); }
    [Serializable] public sealed class HomeFurnitureLayout
    {
        public string id,assetId; public HomePoint size01,drawAnchor; public HomeRect footprint;
        public string[] orientationIds=Array.Empty<string>(); public HomeFurnitureSlot[] slots=Array.Empty<HomeFurnitureSlot>();
        public HomeCost[] costs=Array.Empty<HomeCost>();
    }
    [Serializable] public sealed class HomeEventDef
    { public string id,heroineId,kind,sceneId; public bool establishesLover; public HomeCondition unlockCondition; public HomeCost[] rewards=Array.Empty<HomeCost>(); }
    [Serializable] public sealed class HomeChapterDef
    { public string id,ownerId,sceneId; public string[] requiredPoemIds=Array.Empty<string>(); public HomeCost[] rewards=Array.Empty<HomeCost>(); }
    [Serializable] public sealed class HomeTextDef { public string id,text; }
    [Serializable] public sealed class HomeAssetDef { public string id,kind; public bool placeholder; }
    [Serializable] public sealed class HomeDisplayVariant { public string id,assetId; }
    [Serializable] public sealed class HomeDisplaySet
    {
        public string id,heroineId,outfitId,standingAssetId;
        public HomeDisplayVariant[] expressions=Array.Empty<HomeDisplayVariant>(),poses=Array.Empty<HomeDisplayVariant>();
    }
    [Serializable] public sealed class HomeActorSlot { public string id; public HomePoint anchor,pivot,size01; public int drawOrder; }
    [Serializable] public sealed class HomeAdvCommand
    {
        public string commandId,kind,assetId,heroineId,slotId,outfitId,expressionId,poseId,lineId,textId,speakerId,audioId,channel,transition;
        public int durationMs; public bool hideActors;
    }
    [Serializable] public sealed class HomeAdvScript
    { public int schemaVersion,scriptVersion; public string id; public HomeAdvCommand[] commands=Array.Empty<HomeAdvCommand>(); }
    public sealed class HomeDiagnostic
    {
        public string Severity {get;}="error"; public string Code {get;} public string DocumentId {get;} public string JsonPointer {get;} public string ReferencedId {get;} public string Message {get;}
        public HomeDiagnostic(string code,string document,string pointer,string reference,string message)
        {Code=code;DocumentId=document;JsonPointer=pointer;ReferencedId=reference;Message=message;}
        public override string ToString()=>Code+" "+DocumentId+JsonPointer+": "+Message;
    }
    public sealed class HomeDefinitionException : ArgumentException
    {
        public HomeDiagnostic Diagnostic {get;}
        public HomeDefinitionException(HomeDiagnostic diagnostic):base(diagnostic.ToString()){Diagnostic=diagnostic;}
    }
    // Entire content pack is accepted before any references are published to the UI.
    [Serializable] public sealed partial class HomeExperienceCatalog
    {
        public const string FixtureVersion="home-fixture-2026-10-03";
        public int schemaVersion; public string contentVersion,status;
        public string[] heroineIds=Array.Empty<string>(),colossusIds=Array.Empty<string>(),poemIds=Array.Empty<string>(),resourceIds=Array.Empty<string>(),abilityIds=Array.Empty<string>(),skillIds=Array.Empty<string>(),speakerIds=Array.Empty<string>();
        public HomeMaterialDef[] materials=Array.Empty<HomeMaterialDef>();
        public HomeTalkDef[] interactions=Array.Empty<HomeTalkDef>();
        public HomeBookSubject[] subjects=Array.Empty<HomeBookSubject>(); public HomeWeaponNode[] weaponNodes=Array.Empty<HomeWeaponNode>();
        public HomeGardenLayout[] gardens=Array.Empty<HomeGardenLayout>(); public HomeFurnitureLayout[] furniture=Array.Empty<HomeFurnitureLayout>();
        public HomeEventDef[] events=Array.Empty<HomeEventDef>(); public HomeChapterDef[] chapters=Array.Empty<HomeChapterDef>();
        public HomeTextDef[] texts=Array.Empty<HomeTextDef>(); public HomeAssetDef[] assets=Array.Empty<HomeAssetDef>(); public HomeDisplaySet[] displays=Array.Empty<HomeDisplaySet>();
        public HomeActorSlot[] actorSlots=Array.Empty<HomeActorSlot>(); public HomeAdvScript[] scripts=Array.Empty<HomeAdvScript>();
        internal static bool Id(string id)=>CollectionCatalog.ValidId(id);
        private void Fail(string code,string pointer,string reference,string message)=>throw new HomeDefinitionException(new HomeDiagnostic(code,contentVersion??"home-pack",pointer,reference,message));
        private Dictionary<string,T> Index<T>(T[] values,Func<T,string> key,string pointer) where T:class
        {
            if(values==null)Fail("MISSING_REFERENCE",pointer,null,"必須一覧がありません。");
            var result=new Dictionary<string,T>(StringComparer.Ordinal);
            for(int i=0;i<values.Length;i++){
                var at=pointer+"/"+i;if(values[i]==null || !Id(key(values[i])))Fail("INVALID_RANGE",at,null,"有効なIDが必要です。");
                var id=key(values[i]);if(result.ContainsKey(id))Fail("DUPLICATE_ID",at,id,"IDが重複しています。");result.Add(id,values[i]);
            }return result;
        }
        private HashSet<string> Set(string[] ids,string pointer)
        {if(ids==null || ids.Any(x=>!Id(x)))Fail("INVALID_RANGE",pointer,null,"ID一覧が不正です。");if(ids.Distinct().Count()!=ids.Length)Fail("DUPLICATE_ID",pointer,null,"ID一覧が重複しています。");return new HashSet<string>(ids,StringComparer.Ordinal);}
        private void Ref(bool valid,string pointer,string id){if(!valid)Fail("MISSING_REFERENCE",pointer,id,"参照先がありません。");}
        private static bool Unit(float v)=>!float.IsNaN(v) && !float.IsInfinity(v) && v>=0 && v<=1;
        private static bool Finite(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        private static bool Point(HomePoint p)=>p!=null && Unit(p.x) && Unit(p.y);
        private static bool Size(HomePoint p)=>Point(p) && p.x>0 && p.y>0;
        private static bool Rect(HomeRect r)=>r!=null && Unit(r.x) && Unit(r.y) && Unit(r.width) && Unit(r.height) && r.width>0 && r.height>0 && (double)r.x+r.width<=1.00000001 && (double)r.y+r.height<=1.00000001;
        private void Costs(HomeCost[] costs,HashSet<string> resources,string pointer,bool positive)
        {
            if(costs==null || positive && costs.Length==0)Fail("UNRESOLVED_RULE",pointer,null,"費用の明示が必要です。");
            var totals=new Dictionary<string,long>();for(int i=0;i<costs.Length;i++){
                var c=costs[i];if(c==null || c.amount<0 || positive && c.amount==0)Fail("INVALID_RANGE",pointer+"/"+i,null,"費用数量が不正です。");
                Ref(Id(c.resourceId) && resources.Contains(c.resourceId),pointer+"/"+i+"/resourceId",c.resourceId);
                totals[c.resourceId]=(totals.TryGetValue(c.resourceId,out var n)?n:0)+c.amount;
                if(totals[c.resourceId]>int.MaxValue)Fail("INVALID_RANGE",pointer,c.resourceId,"費用合計が整数上限を超えています。");
            }
        }
        public void Validate(bool release=false)
        {
            if(schemaVersion!=1)Fail("UNKNOWN_SCHEMA","/schemaVersion",null,"未対応の定義版です。");
            if(!Id(contentVersion) || status!="fixture" && status!="release")Fail("UNRESOLVED_RULE","/contentVersion",contentVersion,"内容版と状態を指定してください。");
            if(release && status!="release")Fail("PLACEHOLDER_IN_RELEASE","/status",null,"検証パックを正式版へ採用できません。");
            if(status=="release" && contentVersion==FixtureVersion)Fail("PLACEHOLDER_IN_RELEASE","/contentVersion",contentVersion,"検証用内容版を正式版へ改名できません。");
            var hs=Set(heroineIds,"/heroineIds");var cs=Set(colossusIds,"/colossusIds");var ps=Set(poemIds,"/poemIds");var rs=Set(resourceIds,"/resourceIds");var abs=Set(abilityIds,"/abilityIds");var sks=Set(skillIds,"/skillIds");Set(speakerIds,"/speakerIds");
            if(hs.Overlaps(cs))Fail("DUPLICATE_ID","/heroineIds",null,"人物と巨神獣のIDが重複しています。");
            var ns=Index(weaponNodes,n=>n.id,"/weaponNodes");var gs=Index(gardens,g=>g.id,"/gardens");var fs=Index(furniture,f=>f.id,"/furniture");var es=Index(events,e=>e.id,"/events");var chs=Index(chapters,c=>c.id,"/chapters");
            var materialIndex=Index(materials,m=>m.id,"/materials");foreach(var material in materials){Ref(rs.Contains(material.id),"/materials/id",material.id);Ref(cs.Contains(material.colossusId),"/materials/colossusId",material.colossusId);}
            if(es.Keys.Intersect(chs.Keys).Any())Fail("DUPLICATE_ID","/events",null,"章とイベントの報酬IDが重複しています。");
            var tx=Index(texts,t=>t.id,"/texts");var ast=Index(assets,a=>a.id,"/assets");Index(displays,d=>d.id,"/displays");var slots=Index(actorSlots,s=>s.id,"/actorSlots");var scs=Index(scripts,s=>s.id,"/scripts");Index(subjects,s=>s.id,"/subjects");
            foreach(var t in texts)if(string.IsNullOrWhiteSpace(t.text))Fail("INVALID_RANGE","/texts",t.id,"本文が空です。");
            foreach(var a in assets){if(!new[]{"background","standing","expression","pose","cg","audio","furniture","foreground"}.Contains(a.kind))Fail("INVALID_RANGE","/assets",a.id,"素材種別が不正です。");if((release || status=="release") && a.placeholder)Fail("PLACEHOLDER_IN_RELEASE","/assets",a.id,"正式必須素材が仮素材です。");}
            foreach(var n in weaponNodes){Ref(hs.Contains(n.heroineId),"/weaponNodes/heroineId",n.heroineId);Set(n.parentIds,"/weaponNodes/parentIds");if(n.initial?n.parentIds.Length!=0:n.parentIds.Length==0)Fail("MISSING_REFERENCE","/weaponNodes/parentIds",n.id,"初期以外には親が必要です。");foreach(var id in n.parentIds)Ref(ns.TryGetValue(id,out var parent) && parent.heroineId==n.heroineId,"/weaponNodes/parentIds",id);Costs(n.costs,rs,"/weaponNodes/costs",!n.initial);Ref(abs.Contains(n.abilityId),"/weaponNodes/abilityId",n.abilityId);Ref(sks.Contains(n.skillId),"/weaponNodes/skillId",n.skillId);}
            foreach(var n in weaponNodes)foreach(var cost in n.costs)Ref(materialIndex.ContainsKey(cost.resourceId),"/weaponNodes/costs/resourceId",cost.resourceId);
            foreach(var group in weaponNodes.GroupBy(n=>n.heroineId))if(group.Count(n=>n.initial)!=1)Fail("INVALID_RANGE","/weaponNodes",group.Key,"人物ごとの初期ノードは一つです。");
            foreach(var g in gardens){Set(g.foregroundAssetIds,"/gardens/foregroundAssetIds");if(!g.unmade)Ref(ast.TryGetValue(g.backgroundAssetId,out var background) && background.kind=="background","/gardens/backgroundAssetId",g.backgroundAssetId);foreach(var id in g.foregroundAssetIds)Ref(ast.TryGetValue(id,out var foreground) && foreground.kind=="foreground","/gardens/foregroundAssetIds",id);}
            Cycle(ns.Keys,id=>ns[id].parentIds,"/weaponNodes");
            Index(interactions,t=>t.id,"/interactions");if(interactions.Select(t=>t.heroineId).Distinct().Count()!=interactions.Length)Fail("DUPLICATE_ID","/interactions",null,"人物ごとの交流定義が重複しています。");foreach(var t in interactions){Ref(hs.Contains(t.heroineId),"/interactions/heroineId",t.heroineId);if(t.affectionGain<=0)Fail("UNRESOLVED_RULE","/interactions",t.id,"交流の増分が未定です。");Costs(t.costs,rs,"/interactions/costs",true);foreach(var cost in t.costs)Ref(materialIndex.ContainsKey(cost.resourceId),"/interactions/costs",cost.resourceId);}
            foreach(var n in weaponNodes)if(n.attackBonus<0 || !Finite(n.skillPower) || n.skillPower<=0 || n.skillPower>10 || n.treePosition==null || !Finite(n.treePosition.x) || !Finite(n.treePosition.y) || n.treePosition.x<0 || n.treePosition.x>1 || n.treePosition.y<0 || n.treePosition.y>1)Fail("INVALID_RANGE","/weaponNodes/effects",n.id,"武器効果と樹の座標が不正です。");
            foreach(var g in gardens){if(g.schemaVersion!=1)Fail("UNKNOWN_SCHEMA","/gardens",g.id,"未対応の箱庭定義版です。");Index(g.zones,z=>z.id,"/gardens/zones");if(g.unmade){if(release || status=="release")Fail("PLACEHOLDER_IN_RELEASE","/gardens",g.id,"正式区画の配置定義が未制作です。");if(g.zones.Length!=0)Fail("INVALID_RANGE","/gardens/zones",g.id,"未制作区画に配置範囲を代用できません。");continue;}if(g.zones.Length==0 || g.zones.Any(z=>z.order<0 || !Rect(z.bounds)) || g.zones.Select(z=>z.order).Distinct().Count()!=g.zones.Length)Fail("INVALID_RANGE","/gardens/zones",g.id,"ゾーン境界と順序が不正です。");}
            foreach(var f in furniture){if(!Size(f.size01) || !Point(f.drawAnchor) || !Rect(f.footprint))Fail("INVALID_RANGE","/furniture",f.id,"家具の寸法・接地・占有範囲が不正です。");Set(f.orientationIds,"/furniture/orientationIds");if(f.orientationIds.Length!=1 || f.orientationIds[0]!="orientation.default")Fail("UNRESOLVED_RULE","/furniture/orientationIds",f.id,"初回は既定の向きだけに対応します。");Ref(ast.TryGetValue(f.assetId,out var a) && a.kind=="furniture","/furniture/assetId",f.assetId);Costs(f.costs,rs,"/furniture/costs",false);Index(f.slots,s=>s.id,"/furniture/slots");foreach(var s in f.slots){if(!Point(s.offset))Fail("INVALID_RANGE","/furniture/slots",s.id,"使用位置が不正です。");Set(s.actionIds,"/furniture/slots/actionIds");if(s.actionIds.Length==0)Fail("UNRESOLVED_RULE","/furniture/slots",s.id,"使用動作がありません。");}}
            foreach(var s in actorSlots)if(!Point(s.anchor) || !Point(s.pivot) || !Size(s.size01) || s.drawOrder<0)Fail("INVALID_RANGE","/actorSlots",s.id,"人物表示位置が不正です。");
            if(displays.Select(d=>d.heroineId+"|"+d.outfitId).Distinct().Count()!=displays.Length)Fail("DUPLICATE_ID","/displays",null,"人物と衣装の組が重複しています。");
            foreach(var d in displays){Ref(hs.Contains(d.heroineId) && Id(d.outfitId),"/displays/heroineId",d.heroineId);Ref(ast.TryGetValue(d.standingAssetId,out var a) && a.kind=="standing","/displays/standingAssetId",d.standingAssetId);var expressions=Index(d.expressions,v=>v.id,"/displays/expressions");Index(d.poses,v=>v.id,"/displays/poses");Ref(expressions.ContainsKey("expression.normal"),"/displays/expressions","expression.normal");foreach(var v in d.expressions)Ref(ast.TryGetValue(v.assetId,out var asset) && asset.kind=="expression","/displays/expressions",v.assetId);foreach(var v in d.poses)Ref(ast.TryGetValue(v.assetId,out var asset) && asset.kind=="pose","/displays/poses",v.assetId);}
            foreach(var e in events){Ref(hs.Contains(e.heroineId),"/events/heroineId",e.heroineId);if(e.kind!="affinity" && e.kind!="lover" || e.establishesLover && e.kind!="affinity")Fail("INVALID_RANGE","/events/kind",e.id,"イベント種別または恋人移行指定が不正です。");Ref(scs.ContainsKey(e.sceneId),"/events/sceneId",e.sceneId);Condition(e.unlockCondition,hs,cs,ps,gs.Keys,chs.Keys,es.Keys,"/events/unlockCondition");}
            foreach(var c in chapters){Ref(hs.Contains(c.ownerId) || cs.Contains(c.ownerId),"/chapters/ownerId",c.ownerId);Set(c.requiredPoemIds,"/chapters/requiredPoemIds");if(c.requiredPoemIds.Length==0)Fail("UNRESOLVED_RULE","/chapters/requiredPoemIds",c.id,"章に必要な詩を指定してください。");foreach(var id in c.requiredPoemIds)Ref(ps.Contains(id),"/chapters/requiredPoemIds",id);Ref(scs.ContainsKey(c.sceneId),"/chapters/sceneId",c.sceneId);}
            Cycle(es.Keys,id=>Dependencies(es[id].unlockCondition).Concat(LoverDependencies(es[id].unlockCondition)),"/events");
            foreach(var e in events)Costs(e.rewards,rs,"/events/rewards",false);foreach(var c in chapters)Costs(c.rewards,rs,"/chapters/rewards",false);
            foreach(var s in subjects){if(!new[]{"colossi","heroines","gardens","stories"}.Contains(s.bookmarkId) || s.pageOrder<0)Fail("INVALID_RANGE","/subjects",s.id,"分類またはページ順が不正です。");bool valid=s.bookmarkId=="colossi"?cs.Contains(s.subjectId):s.bookmarkId=="heroines"?hs.Contains(s.subjectId):s.bookmarkId=="gardens"?gs.ContainsKey(s.subjectId):hs.Contains(s.subjectId)||cs.Contains(s.subjectId);Ref(valid,"/subjects/subjectId",s.subjectId);Condition(s.unlockCondition,hs,cs,ps,gs.Keys,chs.Keys,es.Keys,"/subjects/unlockCondition");}
            if(subjects.GroupBy(s=>s.bookmarkId).Any(g=>g.Select(s=>s.pageOrder).Distinct().Count()!=g.Count() || g.Select(s=>s.subjectId).Distinct().Count()!=g.Count()))Fail("DUPLICATE_ID","/subjects",null,"分類内の対象またはページ順が重複しています。");
            foreach(var s in scripts)ValidateScript(s,hs,tx,ast,slots);
        }
        private IEnumerable<string> LoverDependencies(HomeCondition c)
        {if(c.kind=="flag" && c.domain=="lover")return events.Where(e=>e.heroineId==c.id && e.establishesLover).Select(e=>e.id);return c.items.SelectMany(LoverDependencies);}
        private static IEnumerable<string> Dependencies(HomeCondition c)
        {if(c.kind=="flag" && c.domain=="eventRead")return new[]{c.id};return c.items.SelectMany(Dependencies);}
        private void Cycle(IEnumerable<string> ids,Func<string,IEnumerable<string>> parents,string pointer)
        {
            var active=new HashSet<string>();var done=new HashSet<string>();Action<string> visit=null;
            visit=id=>{if(done.Contains(id))return;if(!active.Add(id))Fail("DEPENDENCY_CYCLE",pointer,id,"依存関係が循環しています。");foreach(var p in parents(id))visit(p);active.Remove(id);done.Add(id);};foreach(var id in ids)visit(id);
        }
        private void Condition(HomeCondition c,HashSet<string> hs,HashSet<string> cs,HashSet<string> ps,IEnumerable<string> gs,IEnumerable<string> chapters,IEnumerable<string> events,string pointer,int depth=0)
        {
            if(c==null || c.items==null)Fail("UNRESOLVED_RULE",pointer,null,"条件の明示が必要です。");
            if(depth>64)Fail("INVALID_RANGE",pointer,null,"条件の階層が深すぎます。");
            if(c.kind=="all" || c.kind=="any"){if(c.items.Length==0)Fail("INVALID_RANGE",pointer,null,"条件の集合が空です。");for(int i=0;i<c.items.Length;i++)Condition(c.items[i],hs,cs,ps,gs,chapters,events,pointer+"/items/"+i,depth+1);return;}
            if(c.items.Length!=0)Fail("INVALID_RANGE",pointer,null,"単一条件に子条件は指定できません。");
            if(c.kind=="always")return;
            if(c.kind=="flag"){
                IEnumerable<string> allowed=c.domain=="poemOwned"?ps:c.domain=="storyUnlocked" || c.domain=="storyRead"?chapters:c.domain=="eventRead"?events:c.domain=="heroineOwned" || c.domain=="lover"?hs:null;
                if(allowed==null)Fail("UNRESOLVED_RULE",pointer,c.domain,"未知の条件領域です。");Ref(allowed.Contains(c.id),pointer+"/id",c.id);
                if(c.domain=="lover")Ref(this.events.Any(e=>e.establishesLover && e.heroineId==c.id),pointer+"/id",c.id);return;
            }
            if(c.kind=="atLeast"){if(c.value<0)Fail("INVALID_RANGE",pointer,null,"条件値が負です。");if(c.domain=="affection")Ref(hs.Contains(c.ownerId),pointer+"/ownerId",c.ownerId);else if(c.domain=="highestClearedLevel")Ref(cs.Contains(c.ownerId),pointer+"/ownerId",c.ownerId);else if(c.domain!="terraformingXp")Fail("UNRESOLVED_RULE",pointer,c.domain,"未知の数量条件です。");return;}
            Fail("UNRESOLVED_RULE",pointer,c.kind,"未知の条件形式です。");
        }
        private void ValidateScript(HomeAdvScript s,HashSet<string> hs,Dictionary<string,HomeTextDef> texts,Dictionary<string,HomeAssetDef> assets,Dictionary<string,HomeActorSlot> slots)
        {
            string p="/scripts/"+s.id;if(s.schemaVersion!=1)Fail("UNKNOWN_SCHEMA",p,s.id,"未対応のADV定義版です。");
            if(s.scriptVersion<1 || s.commands==null || s.commands.Length<3)Fail("INVALID_RANGE",p,s.id,"本文と終了命令を持つADVが必要です。");
            Index(s.commands,c=>c.commandId,p+"/commands");
            if(s.commands[0].kind!="background" || s.commands.Last().kind!="end" || s.commands.Count(c=>c.kind=="end")!=1 || !s.commands.Any(c=>c.kind=="line"))Fail("INVALID_RANGE",p,s.id,"背景で開始し本文を経て末尾のendで終了してください。");
            Set(s.commands.Where(c=>c.kind=="line").Select(c=>c.lineId).ToArray(),p+"/lineIds");
            var occupancy=new Dictionary<string,string>();
            foreach(var c in s.commands){var at=p+"/commands/"+c.commandId;
                if(c.kind=="background" || c.kind=="cg"){
                    Ref(assets.TryGetValue(c.assetId,out var a) && a.kind==c.kind,at+"/assetId",c.assetId);
                    if(c.durationMs<0 || !string.IsNullOrEmpty(c.transition) && c.transition!="instant" && c.transition!="fade")Fail("INVALID_RANGE",at,null,"遷移指定が不正です。");
                }else if(c.kind=="actor"){
                    Ref(hs.Contains(c.heroineId) && slots.ContainsKey(c.slotId),at,c.heroineId);Ref(displays.Any(d=>d.heroineId==c.heroineId && d.outfitId==c.outfitId),at+"/outfitId",c.outfitId);
                    if(!Id(c.expressionId) || !Id(c.poseId))Fail("MISSING_REFERENCE",at,null,"表情とポーズの指定が必要です。");
                    if(occupancy.TryGetValue(c.slotId,out var hero) && hero!=c.heroineId)Fail("INVALID_RANGE",at,c.slotId,"人物表示枠が使用中です。");foreach(var key in occupancy.Where(x=>x.Value==c.heroineId).Select(x=>x.Key).ToArray())occupancy.Remove(key);occupancy[c.slotId]=c.heroineId;
                }else if(c.kind=="hideActor"){Ref(hs.Contains(c.heroineId),at,c.heroineId);foreach(var key in occupancy.Where(x=>x.Value==c.heroineId).Select(x=>x.Key).ToArray())occupancy.Remove(key);
                }else if(c.kind=="line"){Ref(texts.ContainsKey(c.textId),at+"/textId",c.textId);if(!string.IsNullOrEmpty(c.speakerId))Ref(hs.Contains(c.speakerId) || speakerIds.Contains(c.speakerId),at+"/speakerId",c.speakerId);
                }else if(c.kind=="sound"){Ref(assets.TryGetValue(c.audioId,out var a) && a.kind=="audio",at+"/audioId",c.audioId);if(c.channel!="bgm" && c.channel!="se")Fail("INVALID_RANGE",at,c.channel,"音声チャンネルが不正です。");
                }else if(c.kind!="hideCg" && c.kind!="end")Fail("UNRESOLVED_RULE",at,c.kind,"未対応のADV命令です。");
            }
        }
    }
}
