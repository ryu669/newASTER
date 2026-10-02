"""Reproducible original Slayer production-base mesh. Run with Blender --background --python.

No reference pixels, models, or animation data are copied. This is a first art iteration,
not the approved final character. Blender coordinates: Z up, face toward -Y.
"""
import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'game/unity/Assets/Game/Resources/Characters/Slayer'
SOURCE = ROOT / 'game/art-source/slayer'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color, metal=0, rough=.6):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    return m

skin = material('Skin', (.94, .70, .60))
hair = material('Honey Hair', (.82, .57, .25))
hairlight = material('Hair Highlights', (.98, .79, .44))
pink = material('Rose Cloth', (.66, .16, .30))
rose = material('Rose Trim', (.96, .47, .60))
dark = material('Deep Rose', (.22, .04, .09))
ivory = material('Ivory Feathers', (.93, .92, .83))
gold = material('Champagne Metal', (.86, .66, .28), .8, .25)
steel = material('Silver Blade', (.66, .81, .86), .8, .22)
teal = material('Teal Iris', (.03, .43, .48))
ink = material('Lash and Pupil', (.045, .025, .025))
white = material('Eye White', (.99, .94, .89))
lips = material('Lip', (.58, .20, .23))
meshes = []
bodypieces = []
bindings = {}

def finish(obj, name, mat, bone=None, body=False):
    obj.name = name
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for p in obj.data.polygons:
        p.use_smooth = True
    if body:
        bodypieces.append(obj)
    else:
        meshes.append(obj)
        if bone:
            bindings[obj.name] = bone
    return obj

def ellipsoid(name, pos, scale, mat, bone=None, body=False, segments=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=12, location=pos)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat, bone, body)

def segment(name, a, b, width, depth, mat, bone=None, body=False):
    a, b = Vector(a), Vector(b)
    obj = ellipsoid(name, (a+b)/2, (width, depth, (b-a).length/2+width*.45), mat, bone, body)
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return obj

def surface(name, rings, mat, bone=None, sides=32, cap=False):
    # Each ring: center x,y,z and elliptical radii x,y.
    v, f = [], []
    for x,y,z,rx,ry in rings:
        v.extend([(x+rx*math.cos(2*math.pi*i/sides), y+ry*math.sin(2*math.pi*i/sides), z) for i in range(sides)])
    for j in range(len(rings)-1):
        for i in range(sides):
            n = (i+1)%sides
            f.append((j*sides+i,j*sides+n,(j+1)*sides+n,(j+1)*sides+i))
    if cap:
        f += [tuple(reversed(range(sides))), tuple((len(rings)-1)*sides+i for i in range(sides))]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(v, [], f)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, bone)

def ribbon(name, points, radii, mat, bone):
    verts, faces = [], []
    for p,r in zip(points,radii):
        for i in range(10):
            t = 2*math.pi*i/10
            verts.append((p[0]+r*math.cos(t),p[1]+r*.42*math.sin(t),p[2]))
    for j in range(len(points)-1):
        for i in range(10):
            faces.append((j*10+i,j*10+(i+1)%10,(j+1)*10+(i+1)%10,(j+1)*10+i))
    faces += [tuple(reversed(range(10))),tuple((len(points)-1)*10+i for i in range(10))]
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces)
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    return finish(obj,name,mat,bone)

# Complete adult body, including covered torso, five fingers per hand, and feet.
ellipsoid('Pelvis', (0,0,.91), (.185,.125,.17), skin, body=True)
ellipsoid('Waist', (0,0,1.095), (.125,.095,.17), skin, body=True)
ellipsoid('Ribcage', (0,0,1.265), (.175,.112,.18), skin, body=True)
for s in (-1,1):
    ellipsoid('Chest', (s*.082,-.068,1.27), (.086,.071,.077), skin, body=True)
    segment('Shoulder', (s*.12,0,1.36),(s*.23,0,1.35),.059,.061,skin,body=True)
    segment('UpperArm', (s*.21,0,1.35),(s*.36,0,1.12),.050,.052,skin,body=True)
    segment('Forearm', (s*.36,0,1.12),(s*.45,-.01,.93),.035,.036,skin,body=True)
    ellipsoid('Palm', (s*.466,-.01,.88),(.033,.023,.058),skin,body=True)
    for finger in range(4):
        x=s*(.442+finger*.014)
        length=[.063,.077,.073,.057][finger]
        segment('Finger',(x,-.01,.853),(x,-.015,.853-length),.009,.010,skin,body=True)
    segment('Thumb',(s*.444,-.012,.90),(s*.422,-.022,.855),.012,.012,skin,body=True)
    segment('Thigh',(s*.10,0,.91),(s*.103,-.025,.51),.084,.089,skin,body=True)
    segment('Calf',(s*.103,-.025,.51),(s*.105,.015,.105),.050,.051,skin,body=True)
    ellipsoid('Foot',(s*.105,-.065,.060),(.047,.106,.045),skin,body=True)
segment('Neck',(0,0,1.39),(0,0,1.48),.046,.045,skin,body=True)
ellipsoid('Head',(0,-.015,1.59),(.108,.093,.137),skin,body=True,segments=32)
ellipsoid('Chin',(0,-.035,1.50),(.061,.056,.043),skin,body=True)
ellipsoid('Nose',(0,-.109,1.565),(.017,.023,.029),skin,body=True)
for s in (-1,1): ellipsoid('Ear',(s*.105,-.006,1.576),(.019,.015,.033),skin,body=True)
bpy.ops.object.select_all(action='DESELECT')
for obj in bodypieces: obj.select_set(True)
bpy.context.view_layer.objects.active=bodypieces[0]
bpy.ops.object.join()
body=bpy.context.object; body.name='Body_Common'
# Union removes interior intersections and retains skin beneath every garment.
remesh=body.modifiers.new('Connected Body','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.008
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=body.modifiers.new('Surface Relax','SMOOTH'); smooth.factor=.65; smooth.iterations=5
bpy.ops.object.modifier_apply(modifier=smooth.name)
dec=body.modifiers.new('Game Topology Base','DECIMATE'); dec.ratio=.5
bpy.ops.object.modifier_apply(modifier=dec.name)
for p in body.data.polygons: p.use_smooth=True
meshes.append(body)

# Facial features are individually authored geometry; texture/shape-key polish remains.
for s in (-1,1):
    ellipsoid('EyeWhite', (s*.045,-.101,1.606),(.034,.012,.021),white,'Head')
    ellipsoid('Iris', (s*.044,-.112,1.606),(.016,.004,.018),teal,'Head')
    ellipsoid('Pupil', (s*.043,-.116,1.606),(.006,.002,.012),ink,'Head')
    ellipsoid('EyeLight', (s*.038,-.119,1.615),(.005,.002,.006),white,'Head')
    segment('UpperLash',(s*.018,-.111,1.620),(s*.073,-.105,1.619),.004,.003,ink,'Head')
    segment('Brow',(s*.021,-.099,1.644),(s*.076,-.090,1.639),.005,.004,hair,'Head')
segment('Mouth',(-.026,-.098,1.532),(.026,-.098,1.532),.003,.003,lips,'Head')

# Sculptural bob cap and curved, tapered locks, not a helmet sphere.
capverts,capfaces=[],[]
for j in range(13):
    for i in range(40):
        theta=2*math.pi*i/40
        front=math.sin(theta)<-.35
        phi=(j/12)*(1.38 if front else 2.3)
        capverts.append((.116*math.sin(phi)*math.cos(theta),-.004+.102*math.sin(phi)*math.sin(theta),1.61+.133*math.cos(phi)))
for j in range(12):
    for i in range(40): capfaces.append((j*40+i,(j+1)*40+i,(j+1)*40+(i+1)%40,j*40+(i+1)%40))
mesh=bpy.data.meshes.new('BobCap'); mesh.from_pydata(capverts,[],capfaces)
obj=bpy.data.objects.new('Hair_Bob',mesh); bpy.context.collection.objects.link(obj); finish(obj,'Hair_Bob',hair,'Head')
for s in (-1,1):
    for i in range(5):
        y=-.055+i*.030
        ribbon('Bob_Lock',[(s*.09,y,1.68),(s*.128,y-.005,1.59),(s*.133,y,1.49),(s*.092,y-.008,1.455)],[.025,.029,.024,.003],hair if i%2 else hairlight,'Head')
for i in range(7):
    x=(i-3)*.029
    ribbon('Fringe',[(x*.8,-.065,1.711),(x,-.11,1.673),(x+.009,-.118,1.645)],[.021,.019,.002],hairlight if i%2 else hair,'Head')

# Outfit groups are separate skinned meshes sharing the same bind pose.
dress=surface('Outfit_Rose_Dress',[(0,0,.735,.238,.158),(0,0,.82,.209,.143),(0,0,.94,.196,.138),(0,0,1.04,.155,.114),(0,0,1.13,.147,.119),(0,-.018,1.25,.195,.185),(0,-.012,1.33,.191,.163),(0,0,1.39,.067,.066)],pink)
solid=dress.modifiers.new('Tailored Thickness','SOLIDIFY'); solid.thickness=.004
bpy.context.view_layer.objects.active=dress; bpy.ops.object.modifier_apply(modifier=solid.name)
surface('Outfit_Rose_Hem',[(0,0,.735,.243,.162),(0,0,.769,.235,.158)],rose)
surface('Outfit_Rose_Belt',[(0,0,1.03,.161,.120),(0,0,1.06,.157,.117)],gold)
surface('Outfit_Rose_Collar',[(0,0,1.385,.063,.058),(0,0,1.432,.055,.052)],rose,'Neck')
ellipsoid('Outfit_Rose_Brooch',(0,-.159,1.287),(.025,.012,.027),gold,'Chest')
for s in (-1,1):
    for k in range(5):
        a=2*math.pi*k/5
        ellipsoid('Hair_Rose_Petal',(s*.112+.016*math.cos(a),-.058,1.66+.016*math.sin(a)),(.013,.008,.010),rose,'Head')
    ellipsoid('Hair_Rose_Core',(s*.112,-.067,1.66),(.007,.004,.007),gold,'Head')
    surface('Outfit_Rose_Boot',[(s*.105,0,.09,.057,.066),(s*.105,0,.18,.057,.061),(s*.103,-.012,.36,.061,.060),(s*.103,-.018,.46,.065,.066)],ivory,'Shin'+('.L' if s<0 else '.R'))
    ellipsoid('Outfit_Rose_Shoe',(s*.105,-.068,.057),(.060,.116,.052),ivory,'Foot'+('.L' if s<0 else '.R'))
    surface('Outfit_Rose_BootTrim',[(s*.103,-.018,.43,.067,.068),(s*.103,-.018,.46,.068,.069)],gold,'Shin'+('.L' if s<0 else '.R'))

# A second modest training outfit proves mesh switching without changing the body.
surface('Outfit_Training_Tunic',[(0,0,.81,.22,.151),(0,0,1.0,.17,.121),(0,0,1.14,.152,.128),(0,-.014,1.27,.199,.185),(0,-.008,1.34,.192,.165),(0,0,1.38,.175,.132)],dark)
for s in (-1,1):
    surface('Outfit_Training_Legging',[(s*.105,0,.13,.057,.061),(s*.103,-.022,.49,.068,.072),(s*.10,0,.79,.105,.114),(s*.10,0,.89,.105,.127)],dark)
    ellipsoid('Outfit_Training_Shoe',(s*.105,-.065,.054),(.06,.115,.05),dark,'Foot'+('.L' if s<0 else '.R'))

# Separate articulated wings. Each feather is a new, low-poly authored form.
for s in (-1,1):
    wingbone='Wing'+('.L' if s<0 else '.R')
    for i in range(9):
        a=(s*.12,.095,1.32)
        b=(s*(.34+i*.045),.12+i*.021,1.44-i*.047)
        segment('Wing_Feather',a,b,.043-i*.002,.022,ivory,wingbone)
    ellipsoid('Wing_Root',(s*.12,.10,1.30),(.055,.033,.055),gold,wingbone)

# Straight double-edge sword with a distinct star guard.
blade=surface('Weapon_Blade',[(.50,-.028,.82,.024,.008),(.50,-.028,.40,.019,.007),(.50,-.028,.28,.001,.001)],steel,'Hand.R',sides=4,cap=True)
segment('Weapon_Grip',(.50,-.028,.82),(.50,-.028,.94),.015,.015,dark,'Hand.R')
segment('Weapon_Guard',(.405,-.028,.81),(.595,-.028,.81),.014,.015,gold,'Hand.R')
ellipsoid('Weapon_Gem',(.50,-.040,.82),(.021,.012,.026),teal,'Hand.R')

# Common skeleton. Finger bones and equipment/wing sockets are preserved in FBX.
bones={
 'Root':((0,0,0),(0,0,.15),None),
 'Hips':((0,0,.89),(0,0,1.03),'Root'),
 'Spine':((0,0,1.03),(0,0,1.21),'Hips'),
 'Chest':((0,0,1.21),(0,0,1.39),'Spine'),
 'Neck':((0,0,1.39),(0,0,1.47),'Chest'),
 'Head':((0,0,1.47),(0,0,1.72),'Neck')}
for s,suf in [(-1,'.L'),(1,'.R')]:
    bones['UpperArm'+suf]=((s*.19,0,1.35),(s*.36,0,1.12),'Chest')
    bones['Forearm'+suf]=((s*.36,0,1.12),(s*.45,-.01,.93),'UpperArm'+suf)
    bones['Hand'+suf]=((s*.45,-.01,.93),(s*.468,-.01,.84),'Forearm'+suf)
    for f in range(4):
        x=s*(.442+f*.014); length=[.063,.077,.073,.057][f]
        bones[f'Finger{f}'+suf]=((x,-.01,.85),(x,-.015,.853-length),'Hand'+suf)
    bones['Thumb'+suf]=((s*.444,-.012,.90),(s*.422,-.022,.855),'Hand'+suf)
    bones['Thigh'+suf]=((s*.10,0,.91),(s*.103,-.025,.51),'Hips')
    bones['Shin'+suf]=((s*.103,-.025,.51),(s*.105,.015,.105),'Thigh'+suf)
    bones['Foot'+suf]=((s*.105,.015,.105),(s*.105,-.15,.055),'Shin'+suf)
    bones['Wing'+suf]=((s*.12,.095,1.32),(s*.65,.24,1.24),'Chest')
rigdata=bpy.data.armatures.new('Slayer_Common_Skeleton')
rig=bpy.data.objects.new('Slayer_Rig',rigdata); bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name,(a,b,parent) in bones.items():
    bone=rigdata.edit_bones.new(name); bone.head=a; bone.tail=b
    if parent: bone.parent=rigdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')

def distance(point,a,b):
    delta=b-a; t=max(0,min(1,(point-a).dot(delta)/delta.length_squared))
    return (point-(a+delta*t)).length

for obj in meshes:
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free()
    mod=obj.modifiers.new('Common Skeleton','ARMATURE'); mod.object=rig
    obj.parent=rig
    fixed=bindings.get(obj.name)
    if fixed:
        g=obj.vertex_groups.new(name=fixed); g.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    else:
        candidates=[n for n in bones if n!='Root' and not n.startswith('Wing')]
        groups={n:obj.vertex_groups.new(name=n) for n in candidates}
        for v in obj.data.vertices:
            closest=sorted([(distance(v.co,Vector(bones[n][0]),Vector(bones[n][1])),n) for n in candidates])[:4]
            weights=[1/(d+.012)**4 for d,n in closest]; total=sum(weights)
            for (d,n),w in zip(closest,weights): groups[n].add([v.index],w/total,'REPLACE')
    # UVs ready for future hand-authored textures; colors here are materials only.
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(island_margin=.015); bpy.ops.object.mode_set(mode='OBJECT')

def clip(name, poses, end):
    action=bpy.data.actions.new(name); rig.animation_data_create(); rig.animation_data.action=action
    for frame,values in poses:
        for p in rig.pose.bones:
            p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0); p.location=(0,0,0)
        for bone,angles in values.items(): rig.pose.bones[bone].rotation_euler=tuple(math.radians(v) for v in angles)
        for p in rig.pose.bones:
            p.keyframe_insert('rotation_euler',frame=frame); p.keyframe_insert('location',frame=frame)
    action.use_fake_user=True
    for f in action.fcurves:
        for k in f.keyframe_points: k.interpolation='BEZIER'
    return action

idle=clip('Idle',[(1,{}),(31,{'Chest':(1,0,0),'Wing.L':(5,0,0),'Wing.R':(-5,0,0)}),(61,{})],61)
clip('Attack',[(1,{}),(7,{'Chest':(0,0,-15),'UpperArm.R':(-65,5,-25),'Forearm.R':(0,0,-35)}),(13,{'Chest':(15,0,20),'UpperArm.R':(35,0,25)}),(25,{})],25)
clip('Cast',[(1,{}),(10,{'UpperArm.L':(-30,0,25),'UpperArm.R':(-30,0,-25),'Forearm.L':(-35,0,0),'Forearm.R':(-35,0,0),'Head':(-5,0,0)}),(31,{'UpperArm.L':(-30,0,25),'UpperArm.R':(-30,0,-25),'Forearm.L':(-35,0,0),'Forearm.R':(-35,0,0),'Head':(-5,0,0)})],31)
clip('Hit',[(1,{}),(5,{'Chest':(-14,0,0),'Head':(8,0,0)}),(19,{})],19)
clip('Victory',[(1,{}),(20,{'UpperArm.R':(-130,0,-15),'Forearm.R':(0,0,-15),'Head':(0,0,-7)}),(61,{'UpperArm.R':(-130,0,-15),'Forearm.R':(0,0,-15),'Head':(0,0,-7)})],61)
rig.animation_data.action=idle
bpy.context.scene.render.fps=30
bpy.context.scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for obj in meshes: obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'slayer-production-v1.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,use_mesh_modifiers=True)

# Training outfit starts hidden in the source preview. Unity controls both groups.
for obj in meshes:
    if obj.name.startswith('Outfit_Training'): obj.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'slayer-production-v1.blend'))

# Front three-quarter review image is local-only, not part of the Unity runtime.
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=900; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.world.color=(.12,.12,.12)
def area(name,pos,energy,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.location=pos; obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
area('Key',(2,-3,3.7),500,3); area('Fill',(-2,-1,2),250,3); area('Rim',(0,2,3),600,2)
camdata=bpy.data.cameras.new('ReviewCamera'); cam=bpy.data.objects.new('ReviewCamera',camdata); bpy.context.collection.objects.link(cam)
cam.location=(2.3,-5,2.1); cam.rotation_euler=(Vector((0,0,.93))-cam.location).to_track_quat('-Z','Y').to_euler(); camdata.type='ORTHO'; camdata.ortho_scale=2.10; scene.camera=cam
review=ROOT/'game/Builds/playable/slayer-model-review.png'; review.parent.mkdir(parents=True,exist_ok=True); scene.render.filepath=str(review)
print('SLAYER_MODEL_BASE',len(body.data.vertices),'body vertices',len(rigdata.bones),'bones',len(meshes),'mesh pieces')
bpy.ops.render.render(write_still=True)
