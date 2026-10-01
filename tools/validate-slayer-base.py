"""Read-only structural checks for the garment-independent Slayer authoring base.
Run with Blender --background --python tools/validate-slayer-base.py.
These checks do not certify anatomy, deformation quality, or final art quality.
"""
import bpy
import bmesh
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
    assert len(meshes)==30, 'Unexpected base mesh inventory'
    assert not any(o.name.startswith(('Outfit_','Hair_','Wing','Weapon_')) for o in meshes), 'Garment or accessory leaked into base'
    body=next(o for o in meshes if o.name=='Body_Common')
    rig=next(o for o in objects if o.type=='ARMATURE')
    assert len(rig.data.bones)==30
    assert all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes)
    assert all(o.data.uv_layers for o in meshes), 'Base must retain UV coordinates'
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
