"""Integrate the four reviewed first-batch heroines; fail closed on missing assets."""
import copy,hashlib,json,shutil,uuid,re
from pathlib import Path
from importlib.machinery import SourceFileLoader
author=SourceFileLoader('first_batch_author',str(Path(__file__).with_name('author-plan12-first-batch.py'))).load_module()
ROOT=author.ROOT;SCRIPTS=ROOT/'game/unity/Assets/Game/Scripts';RES=ROOT/'game/unity/Assets/Game/Resources'
PEOPLE=author.PEOPLE
def change(p,old,new):
    p=SCRIPTS/p;t=p.read_text(encoding='utf-8');
    if new not in t:
        assert old in t,(p,old);p.write_text(t.replace(old,new),encoding='utf-8')
def metadata(p):
    if not Path(str(p)+'.meta').exists():Path(str(p)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
def code():
    names=[];personal=[];reactions=[];identity=[];traits=[]
    for p in PEOPLE:
        hero='heroine.'+p['key'];q=lambda x:json.dumps(x,ensure_ascii=False)
        names.append(f'                case {q(hero)}:return new HeroineAuthoredIdentity('+','.join(q(p[k]) for k in ['innate','personal','mastery','weapon','flavor'])+');')
        personal.append(f'                case {q(hero)}:return new HeroinePersonalTrait(HeroineAuthoredNames.For({q(hero)}).Personal,{q(p["icon"])},{q(p["personalText"])},{q(p["personalKind"])},{p["personalPercent"]});')
        reactions.append(f'                case {q(hero)}:return new ReactionStyleProfile{{main={q(p["styles"][0])},sub={q(p["styles"][1])}}};')
        label={'hp':'最大HP＋5%','physical-defense':'物理防御＋5%','critical':'会心率＋5%'}[p['bonus']]
        identity.append(f'                case {q(hero)}:icon={q(p["icon"])};bonus={q(label)};break;')
        traits.append(f'                case {q(hero)}:key={q(p["bonus"])};break;')
    for file,rows in [('Core/HeroineAuthoredNames.cs',names),('Core/HeroinePersonalAbility.cs',personal),('Core/DailyReactionProfiles.cs',reactions),('Data/HeroineIdentityCatalog.cs',identity),('Core/HeroineTraitRules.cs',traits)]:
        p=SCRIPTS/file;t=p.read_text(encoding='utf-8');anchor='                default:';at=t.index(anchor);rows=[r for r in rows if r not in t];p.write_text(t[:at]+'\n'.join(rows)+ ('\n' if rows else '')+t[at:],encoding='utf-8')
    change('Core/HeroinePersonalAbility.cs','p.Kind=="reduction" || p.Kind=="armor"','p.Kind=="reduction" || p.Kind=="wounded-defense" && (long)hero.HitPoints*2<=hero.MaxHitPoints || p.Kind=="armor"')
    p=SCRIPTS/'Data/ProductionEconomyCatalog.cs';t=p.read_text(encoding='utf-8')
    if '"heroine.iron-maiden"' not in t:
        t=t.replace('"heroine.aeon"};','"heroine.aeon",'+','.join(json.dumps('heroine.'+p['key']) for p in PEOPLE)+'};')
        t=re.sub(r'(int\[\] Attacks=\{[^}]+)(\};)',r'\1,8,6,14,8,6,14,8,6,14,8,6,14\2',t)
        t=re.sub(r'(float\[\] Powers=\{[^}]+)(\};)',r'\1,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f,1.18f,1.14f,1.38f\2',t)
        p.write_text(t,encoding='utf-8')
    source=(SCRIPTS/'Data/Plan12AeonHomeArt.cs').read_text(encoding='utf-8');body=source[source.index('            var display='):source.index('            home.assets=assets.ToArray();')+len('            home.assets=assets.ToArray();')]
    body=body.replace('"heroine.aeon"','"heroine."+key').replace('"art.plan12.aeon.standing"','"art.plan12."+key+".standing"').replace('"art.plan12.aeon.expression."','"art.plan12."+key+".expression."').replace('"art.plan12.aeon.pose.idle"','"art.plan12."+key+".pose.idle"').replace('"Illustrations/aeon-"','"Illustrations/"+key+"-"').replace('"Illustrations/aeon-standing-candidate-v1"','"Illustrations/"+key+"-standing-candidate-v1"')
    out='using System.Linq;\nusing NewAster.Core;\nnamespace NewAster.Data\n{\n    public static class Plan12FirstBatchHomeArt\n    {\n        public static void Apply(HomeExperienceCatalog home){foreach(var key in new[]{'+','.join(json.dumps(p['key']) for p in PEOPLE)+'})ApplyOne(home,key);}\n        private static void ApplyOne(HomeExperienceCatalog home,string key)\n        {\n'+body+'\n        }\n    }\n}\n'
    p=SCRIPTS/'Data/Plan12FirstBatchHomeArt.cs';p.write_text(out,encoding='utf-8');metadata(p)
    change('Data/ProductionStoryCatalog.cs','Plan12AeonHomeArt.Apply(home);','Plan12AeonHomeArt.Apply(home);\n            Plan12FirstBatchHomeArt.Apply(home);')
    for file,expr in [('Core/PlayableBattle.cs','formationIds[i]'),('Presentation/HeroineSanctuaryArt.cs','hero')]:
        old=expr+'=="heroine.aeon"';change(file,old,old+' || '+ ' || '.join(expr+'=="heroine.'+p['key']+'"' for p in PEOPLE))
    print('FIRST_BATCH_CODE_CONNECTED; catalogs remain staged')
def art():
    from runpy import run_path
    run_path(str(ROOT/'tools/validate-plan12-asset-reviews.py'))['validate']()
    keys=['standing','portrait','expression-joy','expression-puzzled','expression-determined','attack','hit','cutin']+['sd-'+a for a in ['idle','walk','talk','react','sit','work','look']]+['equipment-tree-v1']+['event-'+str(i)+'-cg' for i in range(5)]
    for p in PEOPLE:
        root=ROOT/'game/art-source'/(p['key']+'-v1')
        for key in keys:assert (root/(key+'.png')).exists(),(p['key'],key)
    hashes={};framing=author.read(RES/'UI/heroine-portrait-framing.json');trees=author.read(RES/'UI/heroine-weapon-trees.json')
    base=next(x for x in trees['entries'] if x['heroineId']=='heroine.aeon')
    for p in PEOPLE:
        key=p['key'];hero='heroine.'+key;root=ROOT/'game/art-source'/(key+'-v1');mapping={}
        for asset in keys:
            # Layered trees replace the rejected character-shaped tree image.
            # Keep old source for rejection history; never copy it back into an active layout.
            if asset=='equipment-tree-v1' and (RES/'UI/WeaponTrees'/(key+'.json')).exists():continue
            name='heroine-portrait-'+key+'-v1' if asset=='portrait' else 'heroine-weapon-tree-'+key+'-v1' if asset=='equipment-tree-v1' else key+'-'+asset+'-candidate-v1'
            dest=RES/'Illustrations'/(name+'.png');shutil.copyfile(root/(asset+'.png'),dest);metadata(dest);mapping[asset]='Illustrations/'+name;hashes[mapping[asset]]=hashlib.sha256(dest.read_bytes()).hexdigest()
        binding=dict(heroineId=hero,placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']);author.write(RES/'Illustrations'/(key+'-battle-binding.json'),binding)
        framing['entries']=[e for e in framing['entries'] if e['heroineId']!=hero]+[dict(heroineId=hero,resourcePath=mapping['portrait'],faceCenterX=.5,faceCenterY=.35,faceHeight=.25,legacyPortrait=False)]
        if not (RES/'UI/WeaponTrees'/(key+'.json')).exists():
            layout=author.remap(copy.deepcopy(base),'aeon',key)
            trees['entries']=[e for e in trees['entries'] if e['heroineId']!=hero]+[layout]
        manifest=author.read(root/'generation.json');
        for a in manifest['assets']:a['sha256']=hashlib.sha256(Path(a['path']).read_bytes()).hexdigest()
        author.write(root/'generation.json',manifest)
    author.write(RES/'UI/heroine-portrait-framing.json',framing);author.write(RES/'UI/heroine-weapon-trees.json',trees)
    path=SCRIPTS/'Data/ProductionAssetAcceptance.cs';t=path.read_text(encoding='utf-8')
    for resource,digest in hashes.items():
        row='            {"'+resource+'","'+digest+'"},'
        if '"'+resource+'"' not in t:t=t.replace('        });',row+'\n        });')
        else:t=re.sub(r'\{"'+re.escape(resource)+r'","[a-f0-9]+"\}', '{"'+resource+'","'+digest+'"}',t)
    path.write_text(t,encoding='utf-8')
    for folder,name in [('Combat','battle-plan12-first-batch'),('Story','plan12-first-batch-story-content')]:
        path=RES/folder/(name+'.json');shutil.copyfile(author.OUT/('combat.json' if folder=='Combat' else 'story.json'),path);metadata(path)
    print('FIRST_BATCH_ART_CONNECTED 84 dedicated images; ready for Unity validation')
if __name__=='__main__':
    import sys
    code()
    if '--art' in sys.argv:art()
