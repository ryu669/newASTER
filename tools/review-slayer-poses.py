"""Render clothed deformation checks without changing the authoring source."""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Quaternion

root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'game/art-source/slayer/slayer-beauty-v2.blend'))
scene=bpy.context.scene; rig=bpy.data.objects['Slayer_Rig']; rig.animation_data_clear()
for obj in scene.objects:
    if obj.type=='MESH':
        obj.hide_render=obj.name.startswith(('Outfit_Training','Wing','Weapon_'))
scene.render.resolution_x=960; scene.render.resolution_y=1200
scene.render.resolution_percentage=100; scene.cycles.samples=16
cam=scene.camera; cam.location=(2.6,-5,2)
cam.rotation_euler=(Vector((0,0,.98))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.ortho_scale=2.1
poses={
    'rest':{},
    'raised-arms':{'UpperArm.R':(0,-65,0),'UpperArm.L':(0,65,0)},
    'bent-elbows':{'Forearm.R':(-90,0,0),'Forearm.L':(-90,0,0)},
    'raised-knee':{'Thigh.R':(-60,0,0),'Shin.R':(85,0,0)},
}
for name,pose in poses.items():
    for bone in rig.pose.bones:
        bone.rotation_mode='XYZ'; bone.rotation_euler=(0,0,0); bone.location=(0,0,0)
    for bone,angles in pose.items():
        pb=rig.pose.bones[bone]; pb.rotation_mode='QUATERNION'
        i=next(i for i,v in enumerate(angles) if v)
        axis=Vector(tuple(1 if k==i else 0 for k in range(3)))
        axis=pb.bone.matrix_local.to_3x3().inverted() @ axis
        pb.rotation_quaternion=Quaternion(axis,math.radians(angles[i]))
    bpy.context.view_layer.update()
    scene.render.filepath=str(root/'game/Builds/playable'/('slayer-pose-'+name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('POSE_RENDERED',name,flush=True)
