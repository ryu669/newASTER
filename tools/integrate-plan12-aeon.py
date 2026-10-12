"""Connect reviewed Aeon art and append-only production catalogs."""
import hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'game/unity/Assets/Game/Resources'
SRC=ROOT/'game/art-source/aeon-v1'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,v): p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def edit(p,old,new):
    p=ROOT/p;t=p.read_text(encoding='utf-8');
    if old in t and new not in t:p.write_text(t.replace(old,new),encoding='utf-8')
def assets():
    keys=['standing','portrait','expression-joy','expression-puzzled','expression-determined','attack','hit','cutin']+['sd-'+p for p in ['idle','walk','talk','react','sit','work','look']]+['equipment-tree-v1']+['event-'+str(i)+'-cg' for i in range(5)]
    assert len(keys)==21
    for k in keys:assert (SRC/(k+'.png')).exists(),k
    return {k:('heroine-portrait-aeon-v1' if k=='portrait' else 'heroine-weapon-tree-aeon-v1' if k=='equipment-tree-v1' else 'aeon-'+k+'-candidate-v1') for k in keys}
def integrate():
    mapping=assets()
    for key,name in mapping.items():shutil.copyfile(SRC/(key+'.png'),RES/'Illustrations'/(name+'.png'))
    write(RES/'Illustrations/aeon-battle-binding.json',dict(heroineId='heroine.aeon',placeholder=False,fullCanvas=True,**{k:'Illustrations/'+mapping[v] for k,v in [('resourcePath','standing'),('attackResourcePath','attack'),('hitResourcePath','hit'),('cutinResourcePath','cutin')]}))
    for key,source in [('Combat/battle-plan12-aeon.json','combat.json'),('Story/plan12-aeon-story-content.json','story.json')]:shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/Aeon'/source,RES/key)
    path=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';t=path.read_text(encoding='utf-8');anchor='        });'
    assert anchor in t
    additions=[]
    for name in mapping.values():
        resource='Illustrations/'+name;digest=hashlib.sha256((RES/(resource+'.png')).read_bytes()).hexdigest()
        if '"'+resource+'"' not in t:additions.append('            {"'+resource+'","'+digest+'"},')
    path.write_text(t.replace(anchor,'\n'.join(additions)+'\n'+anchor),encoding='utf-8')
    p=RES/'UI/heroine-portrait-framing.json';v=read(p)
    v['entries']=[e for e in v['entries'] if e['heroineId']!='heroine.aeon']+[dict(heroineId='heroine.aeon',resourcePath='Illustrations/'+mapping['portrait'],faceCenterX=.5,faceCenterY=.31,faceHeight=.19,legacyPortrait=False)];write(p,v)
    p=RES/'UI/heroine-weapon-trees.json';v=read(p);nodes=[dict(nodeId='heroine.aeon.weapon.root',position=dict(x=.5,y=.85))]
    for tier in range(1,5):
        for branch,x in [('alpha',[.36,.275,.185,.10][tier-1]),('beta',.50),('gamma',[.64,.725,.815,.90][tier-1])]:nodes.append(dict(nodeId='heroine.aeon.weapon.'+branch+('' if tier==1 else '.tier'+str(tier)),position=dict(x=x,y=([.58,.43,.28,.13] if branch=='beta' else [.52,.36,.25,.15])[tier-1])))
    v['entries']=[e for e in v['entries'] if e['heroineId']!='heroine.aeon']+[dict(heroineId='heroine.aeon',resourcePath='Illustrations/'+mapping['equipment-tree-v1'],nodes=nodes)];write(p,v)
    for p in [ROOT/'game/unity/Assets/Game/Scripts/Data/Plan12AeonHomeArt.cs',*list(RES.rglob('*aeon*'))]:
        if p.is_file() and not p.name.endswith('.meta') and not p.with_suffix(p.suffix+'.meta').exists():p.with_suffix(p.suffix+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
    print('AEON_RESOURCES_CONNECTED 21 images / 13 tree nodes / appended production catalogs')
    script='game/unity/Assets/Game/Scripts/'
    edit(script+'Data/ProductionEconomyCatalog.cs','11,6,16};','11,6,16,8,6,14};')
    edit(script+'Data/ProductionEconomyCatalog.cs','1.22f,1.14f,1.4f};','1.22f,1.14f,1.4f,1.18f,1.14f,1.38f};')
    edit(script+'Core/PlayableBattle.cs','formationIds[i]=="heroine.shangrila"','formationIds[i]=="heroine.shangrila" || formationIds[i]=="heroine.aeon"')
    edit(script+'Presentation/HeroineSanctuaryArt.cs','hero=="heroine.shangrila"','hero=="heroine.shangrila" || hero=="heroine.aeon"')
    edit(script+'Presentation/PrototypeBootstrap.cs','"Combat/battle-plan11-7"','"Combat/battle-plan12-aeon"')
    edit(script+'Presentation/PrototypeBootstrap.cs','Environment.GetCommandLineArgs().Contains("-capturePlan10Ui")?','(Environment.GetCommandLineArgs().Contains("-capturePlan10Ui") || Environment.GetCommandLineArgs().Contains("-captureAeon"))?')
    edit(script+'Presentation/PrototypeBootstrap.cs','PreparePlan10ShangrilaCapture(args);','PreparePlan10ShangrilaCapture(args);\n            PreparePlan12AeonCapture(args);')
    edit(script+'Presentation/ProductionStoryExperience.cs','Environment.GetCommandLineArgs().Contains("-capturePlan10Ui")','Environment.GetCommandLineArgs().Contains("-captureAeon") || Environment.GetCommandLineArgs().Contains("-capturePlan10Ui")')
    edit(script+'Presentation/ProductionStoryExperience.cs','combatDefinitions.HeroineIds.Contains("heroine.shangrila")?','combatDefinitions.HeroineIds.Contains("heroine.aeon")?"Story/plan12-aeon-story-content":combatDefinitions.HeroineIds.Contains("heroine.shangrila")?')
if __name__=='__main__':integrate()
