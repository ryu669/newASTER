"""Build the close-up Slayer revision; --preview-only skips rig/export for art iteration."""
import bpy
import sys
import math
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parent))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
import slayer_beauty_geometry as geo
meshes,bindings,bones,body=geo.build()
PREVIEW='--preview-only' in sys.argv
NO_RENDERS='--no-renders' in sys.argv
OUT=ROOT/'game/unity/Assets/Game/Resources/Characters/Slayer'
SOURCE=ROOT/'game/art-source/slayer'
REVIEW=ROOT/'game/Builds/playable'
for d in (OUT,SOURCE,REVIEW): d.mkdir(parents=True,exist_ok=True)

def dist(p,a,b):
    v=b-a; t=max(0,min(1,(p-a).dot(v)/v.length_squared)); return (p-a-v*t).length

def hip_weights(point):
    amount=max(0,min(1,(1.08-point.z)/.26)); amount=amount*amount*(3-2*amount)
    right=max(0,min(1,(point.x+.055)/.11)); right=right*right*(3-2*right)
    spine=max(0,min(1,(point.z-.98)/.10)); spine=spine*spine*(3-2*spine)
    return {'Hips':(1-amount)*(1-spine),'Spine':(1-amount)*spine,'Thigh.R':amount*right,'Thigh.L':amount*(1-right)}

def body_candidates(point,segments):
    x,y,z=point; side='.R' if x>=0 else '.L'; lateral=abs(x)
    torso=('Hips','Spine','Chest','Neck','Head')
    arm=lateral>.19 and z>.76 or lateral>.15 and z>1.16
    if arm:
        names=[n for n in segments if n.endswith(side) and n.startswith(('UpperArm','Forearm','Hand','Finger','Thumb'))]
        if z>1.16: names+=['Chest','Spine']
    elif z<.98:
        names=['Hips']+[n for n in segments if n.endswith(side) and n.startswith(('Thigh','Shin','Foot'))]
        if z>.88: names+=['Spine']
    else:
        names=list(torso)
    return {n:segments[n] for n in names}

if not PREVIEW:
    data=bpy.data.armatures.new('Slayer_Common_Skeleton')
    rig=bpy.data.objects.new('Slayer_Rig',data); bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    for name,(a,b,parent) in bones.items():
        bone=data.edit_bones.new(name); bone.head=a; bone.tail=b
        if parent: bone.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    segments={n:(Vector(a),Vector(b)) for n,(a,b,p) in bones.items() if n!='Root' and not n.startswith('Wing')}
    skin_weights={}; skin_tree=KDTree(len(body.data.vertices))
    for obj in sorted(meshes,key=lambda o:0 if o==body else 1):
        fixed=bindings.get(obj.name)
        if obj.name.startswith('Outfit_Rose_Embroidery'): fixed=None
        if obj.name.startswith(('Hair_','Eye_','Brow','Ear','Mouth','Lower_Lip','Head_')):
            assert fixed=='Head', 'Facial/hair part lost its explicit head binding: '+obj.name
        modifier=obj.modifiers.new('Common Skeleton','ARMATURE'); modifier.object=rig; obj.parent=rig
        if fixed:
            group=obj.vertex_groups.new(name=fixed); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        else:
            groups={n:obj.vertex_groups.new(name=n) for n in segments}
            for vertex in obj.data.vertices:
                point=vertex.co
                # Garments around the hips follow the torso rather than stretching to the hands.
                torso_garment=obj.name.startswith(('Outfit_Rose_Dress','Outfit_Rose_Seam','Outfit_Rose_Diagonal','Outfit_Rose_Belt','Outfit_Rose_Hem','Outfit_Rose_Embroidery','Outfit_Training_Tunic'))
                if (torso_garment and point.z<1.08) or (obj==body and abs(point.x)<.20 and .82<point.z<1.08):
                    # One continuous hip deformation field for skin AND fabric.
                    # Independent thresholds create a visible fold at the waist.
                    for name,weight in hip_weights(point).items():
                        if weight>0: groups[name].add([vertex.index],weight,'REPLACE')
                    continue
                if obj.name.startswith('Outfit_') and not torso_garment:
                    _,index,_=skin_tree.find(point)
                    for name,weight in skin_weights[index]: groups[name].add([vertex.index],weight,'REPLACE')
                    continue
                if obj==body and point.z>1.16 and .10<abs(point.x)<.215:
                    # Blend the shoulder over a surface band, not a hard region
                    # boundary that leaves folded flaps when the arm is raised.
                    amount=max(0,min(1,(abs(point.x)-.10)/.105)); amount=amount*amount*(3-2*amount)
                    side='.R' if point.x>0 else '.L'
                    mixed=[]
                    for candidates_names,share in [(('Chest','Spine'),1-amount),(('UpperArm'+side,'Forearm'+side),amount)]:
                        distances=[(dist(point,*segments[n]),n) for n in candidates_names]
                        raw=[1/(d+.015)**4 for d,n in distances]; total=sum(raw)
                        mixed.extend((n,share*w/total) for (d,n),w in zip(distances,raw))
                    for n,w in mixed:
                        if w>0: groups[n].add([vertex.index],w,'REPLACE')
                    continue
                candidates=body_candidates(point,segments) if not torso_garment else {n:v for n,v in segments.items() if n in ('Hips','Spine','Chest','Neck')}
                nearest=sorted((dist(point,a,b),n) for n,(a,b) in candidates.items())[:4]
                weights=[1/(d+.015)**4 for d,n in nearest]; total=sum(weights)
                for (d,n),weight in zip(nearest,weights): groups[n].add([vertex.index],weight/total,'REPLACE')
        if obj==body:
            names={g.index:g.name for g in obj.vertex_groups}
            for v in obj.data.vertices:
                skin_weights[v.index]=[(names[g.group],g.weight) for g in v.groups if g.weight>0]
                skin_tree.insert(v.co,v.index)
            skin_tree.balance()
        # Proper UVs are preserved alongside vertex colours for future texture painting.
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
        if not obj.data.uv_layers:
            bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(island_margin=.01); bpy.ops.object.mode_set(mode='OBJECT')
    # Consolidate fixed accessories and garments to avoid hundreds of draw objects.
    groups={}
    for obj in list(meshes):
        if obj.data.shape_keys: continue
        name=obj.name
        group='Hair_Styled' if name.startswith('Hair_') else 'Outfit_Rose' if name.startswith('Outfit_Rose') else 'Outfit_Training' if name.startswith('Outfit_Training') else 'Weapon_Complete' if name.startswith('Weapon_') else 'Wing'+bindings.get(name,'') if name.startswith('Wing_') else None
        if group: groups.setdefault(group,[]).append(obj)
    for name,parts in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for p in parts: p.select_set(True)
        bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
        joined=bpy.context.object; joined.name=name
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    def clip(name,poses):
        a=bpy.data.actions.new(name); a.use_fake_user=True; rig.animation_data_create(); rig.animation_data.action=a
        for frame,values in poses:
            for bone in rig.pose.bones: bone.rotation_mode='XYZ'; bone.rotation_euler=(0,0,0); bone.location=(0,0,0)
            for name,angles in values.items(): rig.pose.bones[name].rotation_euler=tuple(math.radians(v) for v in angles)
            for bone in rig.pose.bones: bone.keyframe_insert('rotation_euler',frame=frame); bone.keyframe_insert('location',frame=frame)
        return a
    idle=clip('Idle',[(1,{}),(31,{'Chest':(1,0,0),'Head':(0,0,1),'Wing.L':(3,0,0),'Wing.R':(-3,0,0)}),(61,{})])
    clip('Attack',[(1,{}),(7,{'Chest':(0,0,-12),'UpperArm.R':(-55,5,-20),'Forearm.R':(0,0,-28)}),(13,{'Chest':(12,0,16),'UpperArm.R':(30,0,20)}),(25,{})])
    clip('Cast',[(1,{}),(10,{'UpperArm.L':(-25,0,20),'UpperArm.R':(-25,0,-20),'Forearm.L':(-28,0,0),'Forearm.R':(-28,0,0),'Head':(-3,0,0)}),(31,{'UpperArm.L':(-25,0,20),'UpperArm.R':(-25,0,-20),'Forearm.L':(-28,0,0),'Forearm.R':(-28,0,0),'Head':(-3,0,0)})])
    clip('Hit',[(1,{}),(5,{'Chest':(-12,0,0),'Head':(6,0,0)}),(19,{})])
    clip('Victory',[(1,{}),(20,{'UpperArm.R':(-115,0,-15),'Forearm.R':(0,0,-10),'Head':(0,0,-5)}),(61,{'UpperArm.R':(-115,0,-15),'Forearm.R':(0,0,-10),'Head':(0,0,-5)})])
    rig.animation_data.action=idle; bpy.context.scene.frame_set(1); bpy.context.scene.render.fps=30
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(OUT/'slayer-beauty-v2.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,use_mesh_modifiers=True)
    # Clothing is fitted over this complete, independently editable base. Never
    # derive a body by deleting clothes from a purchased character model.
    base_meshes=[o for o in meshes if o.name=='Body_Common' or o.name.startswith(('Head_','Ear','Eye_','Brow','Mouth','Lower_Lip'))]
    assert body in base_meshes and any(o.name=='Head_Face' for o in base_meshes)
    assert not any(o.name.startswith(('Outfit_','Hair_','Wing','Weapon_')) for o in base_meshes)
    body['authoring_role']='Complete adult base body; do not remove skin under clothing'
    body['outfit_independent']=True
    base_signature=[tuple(v.co) for v in body.data.vertices]
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    for obj in base_meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(SOURCE/'slayer-base-body-v2.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,use_mesh_modifiers=True)
    base_scene=bpy.data.scenes.new('Slayer Base Body - Authoring')
    base_scene.collection.objects.link(rig)
    for obj in base_meshes: base_scene.collection.objects.link(obj)
    base_scene['purpose']='Clothing-independent adult mannequin for garment fitting; not a final anatomy or deformation approval'
    bpy.data.libraries.write(str(SOURCE/'slayer-base-body-v2.blend'),{base_scene},fake_user=True,compress=True)
    bpy.data.scenes.remove(base_scene)
    assert base_signature==[tuple(v.co) for v in body.data.vertices], 'Base export must not alter body geometry'
    print('SLAYER_BASE_BODY_OUTPUT',len(base_meshes),'meshes',len(body.data.vertices),'body vertices; clothing, hair, wings and weapon excluded',flush=True)

for obj in meshes:
    if obj.name.startswith('Outfit_Training'): obj.hide_render=True
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard'; scene.view_settings.look='Medium High Contrast' if 'Medium High Contrast' in [i.name for i in scene.bl_rna.properties] else 'None'
scene.view_settings.exposure=-.75
world=bpy.data.worlds.new('Beauty Studio'); scene.world=world; world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND'); bg.inputs['Color'].default_value=(.57,.61,.67,1); bg.inputs['Strength'].default_value=.6
def area(name,pos,power,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.location=pos; obj.rotation_euler=(Vector((0,0,1.4))-obj.location).to_track_quat('-Z','Y').to_euler()
area('Portrait Key',(-2,-4,4),220,4); area('Portrait Fill',(3,-2,2),140,4); area('Hair Rim',(0,2,3),230,3)
data=bpy.data.cameras.new('Beauty Review Camera'); cam=bpy.data.objects.new('Beauty Review Camera',data); bpy.context.collection.objects.link(cam); scene.camera=cam; data.type='ORTHO'
cam.location=(2.1,-4,1.63); cam.rotation_euler=(Vector((0,0,1.61))-cam.location).to_track_quat('-Z','Y').to_euler(); data.ortho_scale=.43
if not PREVIEW: bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'slayer-beauty-v2.blend'),compress=True)
def render(name,pos,target,scale,width,height):
    cam.location=pos; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler(); data.ortho_scale=scale
    scene.render.resolution_x=width; scene.render.resolution_y=height; scene.render.filepath=str(REVIEW/name)
    bpy.ops.render.render(write_still=True)
if not NO_RENDERS:
    render('slayer-v2-face-front.png',(0,-4,1.61),(0,0,1.61),.43,1200,1200)
    render('slayer-v2-face-threequarter.png',(2.1,-4,1.63),(0,0,1.61),.43,1200,1200)
if not PREVIEW and not NO_RENDERS:
    render('slayer-v2-full.png',(1.2,-4,1.40),(0,0,.92),2.02,1440,1920)
    render('slayer-v2-profile.png',(4,-.02,1.61),(0,0,1.61),.43,1200,1200)
print('SLAYER_BEAUTY_OUTPUT',len(meshes),'meshes',sum(len(o.data.vertices) for o in meshes),'vertices',len(bones),'bones',flush=True)
