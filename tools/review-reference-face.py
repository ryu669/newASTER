"""Local-only reference inspection. Never exports purchased mesh/texture data."""
import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
folder=Path(r'D:\M01_model\13_isana_ver3\13_isana')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(folder/'Mesh/mgcm_13_isana_v1.0.fbx'))
texture=bpy.data.images.load(str(folder/'Texture/mgcm_13_isana.png'))
for obj in bpy.data.objects:
    if obj.type!='MESH': continue
    if obj.data.shape_keys:
        for k in obj.data.shape_keys.key_blocks: k.value=0
    for material in obj.data.materials:
        material.use_nodes=True; nodes=material.node_tree.nodes; nodes.clear()
        out=nodes.new('ShaderNodeOutputMaterial'); shader=nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Roughness'].default_value=.8
        tex=nodes.new('ShaderNodeTexImage'); tex.image=texture
        material.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
        material.node_tree.links.new(tex.outputs['Alpha'],shader.inputs['Alpha'])
        material.node_tree.links.new(shader.outputs[0],out.inputs[0])
face=bpy.data.objects['face']
points=[face.matrix_world@v.co for v in face.data.vertices]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3))); hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(lo+hi)*.5; size=max(hi-lo)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.world=bpy.data.worlds.new('Reference studio'); scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.65,.65,1)
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
data=bpy.data.cameras.new('Reference camera'); cam=bpy.data.objects.new('Reference camera',data); scene.collection.objects.link(cam); scene.camera=cam
cam.location=center+Vector((size*.7,-size*5,size*.2)); cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler(); data.type='ORTHO'; data.ortho_scale=size*1.8
output=root/'.reference-analysis'; output.mkdir(exist_ok=True)
scene.render.filepath=str(output/'isana-face-study.png'); bpy.ops.render.render(write_still=True)
print('REFERENCE_FACE_BOUNDS',tuple(lo),tuple(hi),flush=True)
