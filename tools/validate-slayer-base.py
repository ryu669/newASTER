"""Read-only structural checks for the garment-independent Slayer authoring base.
Run with Blender --background --python tools/validate-slayer-base.py.
These checks do not certify anatomy, deformation quality, or final art quality.
"""
import bpy
import bmesh
import math
from pathlib import Path

source=Path(__file__).resolve().parents[1]/'game/art-source/slayer'
for suffix in ('blend','fbx'):
    path=source/('slayer-base-body-v2.'+suffix)
    if suffix=='blend':
        bpy.ops.wm.open_mainfile(filepath=str(path))
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path))
    objects=list(bpy.context.scene.objects)
    meshes=[o for o in objects if o.type=='MESH']
    assert len(meshes)==31, 'Unexpected base mesh inventory'
    assert not any(o.name.startswith(('Outfit_','Hair_','Wing','Weapon_')) for o in meshes), 'Garment or accessory leaked into base'
    body=next(o for o in meshes if o.name=='Body_Common')
    rig=next(o for o in objects if o.type=='ARMATURE')
    assert len(rig.data.bones)==30
    assert all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes)
    assert all(o.data.uv_layers for o in meshes), 'Base must retain UV coordinates'
    for expression in ('Smile','Talk','Sad','Angry','Surprise'):
        assert any(o.data.shape_keys and any(k.name.endswith(expression) for k in o.data.shape_keys.key_blocks) for o in meshes), 'Missing expression: '+expression
        deltas=[(v.co-o.data.shape_keys.key_blocks[0].data[i].co).length
                for o in meshes if o.data.shape_keys
                for key in o.data.shape_keys.key_blocks if key.name.endswith(expression)
                for i,v in enumerate(key.data)]
        assert all(math.isfinite(d) for d in deltas) and max(deltas)>.001, 'Expression must actually deform: '+expression
    for eye in [o for o in meshes if o.name.startswith('Eye_Iris')]:
        keys=eye.data.shape_keys.key_blocks
        blink=next(k for k in keys if k.name.endswith('Blink'))
        assert max(abs(a.co.z-b.co.z) for a,b in zip(keys[0].data,blink.data))<.000001, 'Blink must occlude, not squash, the iris'
    print('SLAYER_EXPRESSION_VALIDATION_PASS',suffix,'5 nonzero expressions; iris retains shape during blink',flush=True)
    groups={g.index:g.name for g in body.vertex_groups}
    for vertex in body.data.vertices:
        weights=[g for g in vertex.groups if g.weight>0]
        assert 1<=len(weights)<=4 and abs(sum(g.weight for g in weights)-1)<.0001
        x,y,z=vertex.co
        if abs(x)<.10 and 1.04<z<1.25:
            assert not any(groups[g.group].startswith(('UpperArm','Forearm','Hand','Finger','Thumb')) for g in weights), 'Arm weights leaked into central torso'
        if z<.80 and abs(x)>.03:
            opposite='.L' if x>0 else '.R'
            assert not any(groups[g.group].endswith(opposite) for g in weights), 'Opposite leg weights leaked into skin'
        if abs(x)>.25 and .80<z<.98:
            assert not any(groups[g.group].startswith(('Hips','Spine','Thigh','Shin','Foot')) for g in weights), 'Finger tip attached to pelvis or leg'
    # Actual deformation check, not just bone/shape-name presence.
    rig.animation_data_clear()
    for bone in rig.pose.bones:
        bone.rotation_mode='XYZ'; bone.rotation_euler=(0,0,0); bone.location=(0,0,0)
    bpy.context.view_layer.update()
    graph=bpy.context.evaluated_depsgraph_get()
    def evaluated_positions():
        evaluated=body.evaluated_get(graph); mesh=evaluated.to_mesh()
        positions=[v.co.copy() for v in mesh.vertices]; evaluated.to_mesh_clear()
        return positions
    rest=evaluated_positions()
    for joint,angle,axis in [(part+side,angle,axis) for side in ('.R','.L') for part,angle,axis in [('UpperArm',70,2),('Forearm',90,0),('Thigh',65,0),('Shin',90,0)]]:
        rig.pose.bones[joint].rotation_euler[axis]=math.radians(angle)
        bpy.context.view_layer.update(); moved=evaluated_positions()
        assert all(all(math.isfinite(c) for c in p) for p in moved)
        assert max((a-b).length for a,b in zip(rest,moved))>.01, 'Pose did not deform the skin'
        opposite=-1 if joint.endswith('.R') else 1
        safe=[i for i,v in enumerate(body.data.vertices) if v.co.x*opposite>.04 and v.co.z<.80]
        assert max((rest[i]-moved[i]).length for i in safe)<.00001, 'Joint pose displaced opposite leg'
        if joint.startswith(('UpperArm','Forearm')):
            safe=[i for i,v in enumerate(body.data.vertices) if abs(v.co.x)<.10 and 1.04<v.co.z<1.25]
            assert max((rest[i]-moved[i]).length for i in safe)<.00001, 'Arm pose displaced torso centre'
        rig.pose.bones[joint].rotation_euler=(0,0,0); bpy.context.view_layer.update()
    print('SLAYER_POSE_VALIDATION_PASS',suffix,'8 isolated joint poses',flush=True)
    for prefix,expected in [('Eye_White',2),('Eye_Iris',2),('Eye_Lid',4)]:
        parts=[o for o in meshes if o.name.startswith(prefix)]
        assert len(parts)==expected
        assert all(o.data.shape_keys and any(k.name.endswith('Blink') for k in o.data.shape_keys.key_blocks) for o in parts)
    bm=bmesh.new(); bm.from_mesh(body.data)
    assert all(e.is_manifold for e in bm.edges), 'Body skin must not contain open clothing-cut boundaries'
    remaining=set(bm.verts); components=0
    while remaining:
        components+=1; pending=[remaining.pop()]
        while pending:
            for edge in pending.pop().link_edges:
                for vertex in edge.verts:
                    if vertex in remaining:
                        remaining.remove(vertex); pending.append(vertex)
    assert components==1, 'Common skin should be a connected surface'
    bm.free()
    print('SLAYER_BASE_VALIDATION_PASS',suffix,len(meshes),'meshes',len(body.data.vertices),'body vertices',len(rig.data.bones),'bones',flush=True)

# Repeated object names must not overwrite the left/right or head binding.
bpy.ops.wm.open_mainfile(filepath=str(source/'slayer-beauty-v2.blend'))
hair=bpy.data.objects['Hair_Styled']
names={g.index:g.name for g in hair.vertex_groups}
assert all(all(names[g.group]=='Head' for g in v.groups if g.weight>0) for v in hair.data.vertices), 'Hair attached to limbs'
wings=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Wing')]
assert len(wings)==2
for wing in wings:
    names={g.index:g.name for g in wing.vertex_groups}
    for v in wing.data.vertices:
        expected='Wing.R' if v.co.x>0 else 'Wing.L'
        assert all(names[g.group]==expected for g in v.groups if g.weight>0), 'Wing side binding overwritten'
for outfit_name in ('Outfit_Rose','Outfit_Training'):
    outfit=bpy.data.objects[outfit_name]; names={g.index:g.name for g in outfit.vertex_groups}
    for v in outfit.data.vertices:
        if v.co.z<.50 and abs(v.co.x)>.02:
            opposite='.L' if v.co.x>0 else '.R'
            assert not any(names[g.group].endswith(opposite) for g in v.groups if g.weight>0), 'Boot/legging attached to opposite leg'
print('SLAYER_ATTACHMENT_VALIDATION_PASS hair, wings, boots and leggings',flush=True)
