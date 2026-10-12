"""Migrate every playable form to one wooden tree construction, without changing growth data."""
import json,re,uuid
from pathlib import Path
R=Path(__file__).resolve().parents[1]
RES=R/'game/unity/Assets/Game/Resources'
if (RES/'UI/WeaponTrees/common-layout.json').exists():
 print('Common tree migration already applied; edit the shared layout and color catalog directly.')
 raise SystemExit(0)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
template=read(RES/'UI/WeaponTrees/viper.json')
template['heroineId']='common';template['decorations']=[]
for n in template['nodes']:n['nodeId']=n['nodeId'].split('.weapon.',1)[1]
write(RES/'UI/WeaponTrees/common-layout.json',template)
meta=RES/'UI/WeaponTrees/common-layout.json.meta'
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf8')
catalog=read(RES/'UI/heroine-weapon-trees.json')
palette={'slayer':(.95,.60,.70),'iconoclast':(1,.70,.35),'undermine':(.68,.48,.88),'echidna':(.50,.62,.95),'excalipan':(.95,.58,.35),'aurora':(.65,.82,1),'aion':(.40,.72,.95)}
colors=[]
for entry in catalog['entries']:
 hero=entry['heroineId'];key=hero[8:];old=RES/('UI/WeaponTrees/'+key+'.json')
 rgb=palette.get(key.split('-swimsuit')[0],(.63,.76,.90))
 tint=dict(zip(('r','g','b','a'),(*rgb,1)))
 source=read(old) if old.exists() else {}
 colors.append(dict(heroineId=hero,foliageColor=source.get('foliageColor',tint),flowerColor=source.get('flowerColor',tint)))
 entry['resourcePath']='UI/WeaponTrees/common-tree'
 entry['nodes']=[dict(nodeId=hero+'.weapon.'+n['nodeId'],position=n['position']) for n in template['nodes']]
write(RES/'UI/heroine-weapon-trees.json',catalog)
write(RES/'UI/WeaponTrees/heroine-colors.json',dict(entries=colors))
meta=RES/'UI/WeaponTrees/heroine-colors.json.meta'
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf8')
p=R/'game/unity/Assets/Game/Scripts/Presentation/LayeredWeaponTree.cs';s=p.read_text(encoding='utf8')
s=re.sub(r'private static bool HasLayeredWeaponTree\(string hero\)=>[^;]+;', 'private static bool HasLayeredWeaponTree(string hero)=>!string.IsNullOrEmpty(hero) && hero.StartsWith("heroine.",StringComparison.Ordinal);',s)
start=s.index('        // Attachment points on the shared wooden branches')
end=s.index('        private void DrawRoundWeaponNode',start)
s=s[:start]+s[end:]
s=s.replace('public TreePiece[] canopy,flowers,decorations;', 'public TreePiece[] canopy,flowers;')
s=s.replace('        private readonly Dictionary<string,LayeredTree> layeredTrees', '        [Serializable] private sealed class TreeColors { public string heroineId; public Color foliageColor,flowerColor; }\n        [Serializable] private sealed class TreeColorCatalog { public TreeColors[] entries; }\n        private readonly Dictionary<string,LayeredTree> layeredTrees')
start=s.index('                var source=Resources.Load<TextAsset>("UI/WeaponTrees/"+hero.Substring(8));')
end=s.index('                layeredTrees.Add(hero,art);',start)
s=s[:start]+'''                var source=Resources.Load<TextAsset>("UI/WeaponTrees/common-layout");
                if(source==null)throw new InvalidOperationException("Missing shared weapon tree layout.");
                art=JsonUtility.FromJson<LayeredTree>(source.text);
                art.heroineId=hero;
                foreach(var anchor in art.nodes)anchor.nodeId=hero+".weapon."+anchor.nodeId;
                var colors=JsonUtility.FromJson<TreeColorCatalog>(Resources.Load<TextAsset>("UI/WeaponTrees/heroine-colors").text);
                var tint=colors.entries.Single(c=>c.heroineId==hero);
                art.foliageColor=tint.foliageColor;art.flowerColor=tint.flowerColor;
''' +s[end:]
s=s.replace('            // Gate each decoration by the exact persisted node ID, never by equipment or Lv.', '            // Only acquired non-root node IDs grow leaves and flowers.')
s=s.replace('                var hanging=HangingTreePositions(paintArea,art);\n                for(int i=0;i<art.decorations.Length;i++)DrawHangingTreeDecoration(paintArea,art.decorations[i],i,hanging[i]);\n','')
p.write_text(s,encoding='utf8')
print('COMMON_WEAPON_TREE_MIGRATED forms='+str(len(colors)))
