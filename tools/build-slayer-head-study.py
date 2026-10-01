"""Independent art-direction study. Does not replace the playable Slayer.

Blender 4.3: --background --python tools/build-slayer-head-study.py
Outputs an editable head and four actual geometry renders, not concept images.
No purchased mesh, texture, or screenshot pixels are embedded.
"""
import bpy
import sys
import math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parent))
bpy.ops.wm.read_factory_settings(use_empty=True)
import slayer_beauty_geometry as g
SOURCE=ROOT/'game/art-source/slayer'
REVIEW=ROOT/'game/Builds/head-study'
REVIEW.mkdir(parents=True,exist_ok=True)

# Separate landmarks: fuller cheek, shorter lower face, stronger nose/lip profile.
g.HEAD=[(1.487,.004,.056,.030),(1.502,.027,.069,.046),
        (1.524,.052,.078,.065),(1.550,.077,.085,.081),
        (1.58,.096,.091,.096),(1.615,.103,.087,.101),
        (1.660,.104,.081,.099),(1.705,.084,.065,.079),
        (1.746,.002,.002,.002)]
def face_y(x,z):
    width,front,_=g.head_profile(z)
    c=math.sqrt(max(0,1-(x/max(width,.001))**2))
    y=-front*c**.48
    y-=.012*math.exp(-(x/.008)**2-((z-1.562)/.012)**2)
    y-=.0025*math.exp(-(x/.012)**2-((z-1.581)/.021)**2)
    y-=.005*math.exp(-(x/.019)**2-((z-1.537)/.007)**2)
    return y
def eye_bounds(x,side):
    u=(side*x-.020)/.061
    if u<-.000001 or u>1.000001: return None
    u=max(0,min(1,u))
    center=1.607+.003*u
    return center+.0105*math.sin(math.pi*u)**.62,center-.013*math.sin(math.pi*u)**.80,center,u
g.face_y=face_y
g.eye_bounds=eye_bounds
g.create_face()
for obj in g.objects:
    if obj.name.startswith('Ear'):
        for vertex in obj.data.vertices: vertex.co.x*=.91
g.loft('Study_Neck',[(0,.008,1.425,.031,.033),(0,.008,1.49,.032,.033),(0,.008,1.525,.040,.035)],g.skin,'Head')
g.loft('Study_Collar',[(0,.008,1.41,.045,.042),(0,.008,1.44,.037,.037),(0,.008,1.466,.036,.036)],g.cloth,'Head')

# Hair consists of individually authored tapered ribbons, with broad surfaces
# and narrow edges. This replaces the repeated cylindrical lock cross-section.
hair=g.mat('Study Champagne Hair',(.79,.58,.36),rough=.85)
line=g.mat('Study Hair Edge',(.27,.13,.08),rough=.9)
# Continuous crown underneath the designed panels closes their root seams.
cap_vertices=[]; cap_faces=[]
for j in range(33):
    for i in range(96):
        a=2*math.pi*i/96; front=max(0,-math.sin(a))
        ph=.002+j/32*(1.90-.89*front-.002)
        cap_vertices.append((.118*math.sin(ph)*math.cos(a),.009+.107*math.sin(ph)*math.sin(a),1.623+.130*math.cos(ph)))
for j in range(32):
    for i in range(96):
        cap_faces.append((j*96+i,j*96+(i+1)%96,(j+1)*96+(i+1)%96,(j+1)*96+i))
g.mesh('Study_Crown',cap_vertices,cap_faces,hair,'Head')
def panel(name,points,widths,normal):
    verts=[]; colors=[]; faces=[]; rows=48; cols=12
    guide=Vector(normal).normalized()
    for j in range(rows+1):
        t=j/rows; center=g.catmull(points,t)
        tangent=(g.catmull(points,min(1,t+.001))-g.catmull(points,max(0,t-.001))).normalized()
        lateral=tangent.cross(guide).normalized()
        pos=t*(len(widths)-1); k=min(len(widths)-2,int(pos)); width=widths[k]*(1-(pos-k))+widths[k+1]*(pos-k)
        for i in range(cols+1):
            u=2*i/cols-1
            p=center+lateral*(width*u)+guide*(.0035*(1-u*u)*math.sin(math.pi*t)**.5)
            verts.append(tuple(p))
            band=math.exp(-((p.z-1.704-.0017*math.sin(u*15+t*8))/.0045)**2)
            edge=abs(u)**7
            shade=.92-.15*edge-.15*t*t+.18*band
            colors.append((shade,min(1.15,shade+.02*band),min(1.2,shade+.06*band),1))
    for j in range(rows):
        for i in range(cols):
            a=j*(cols+1)+i; faces.append((a,a+1,a+cols+2,a+cols+1))
    obj=g.mesh(name,verts,faces,hair,'Head',colors)
    uv=obj.data.uv_layers.new(name='HairFlow')
    for loop in obj.data.loops:
        v=loop.vertex_index; uv.data[loop.index].uv=((v%(cols+1))/cols,(v//(cols+1))/rows)
    solid=obj.modifiers.new('Thin ribbon edge','SOLIDIFY'); solid.thickness=.0005
    # Only silhouette edge strokes, not dense wire-like lines through the lock.
    for side in (0,cols):
        path=[verts[j*(cols+1)+side] for j in range(rows+1)]
        g.tube(name+'_edge',path,[.00018,.00022,.00012,.00002],line,'Head',steps=48,sides=4)
    return obj

# Back volume fans outward at the temples, then closes into the nape.
for i in range(13):
    a=-.10+(math.pi+.20)*i/12; sx=math.cos(a); sy=math.sin(a)
    panel('Study_Back_%02d'%i,
        [(0,.008,1.752),(.071*sx,.01+.070*sy,1.72),
         (.122*sx,.01+.111*sy,1.655),(.139*sx,.008+.118*sy,1.58),
         (.115*sx,.005+.102*sy,1.526),(.070*sx,.028+.066*sy,1.484)],
        [.009,.029,.030,.026,.018,.0001],(sx,sy,0))

# Swept asymmetric bangs: a few readable major shapes instead of equal teeth.
bangs=[
    ([(-.008,-.023,1.753),(-.055,-.088,1.710),(-.085,-.111,1.655),(-.079,-.119,1.613)], [.008,.027,.029,.0001]),
    ([(.008,-.018,1.757),(-.022,-.093,1.712),(-.044,-.117,1.66),(-.027,-.124,1.608)], [.009,.035,.028,.0001]),
    ([(.020,-.019,1.754),(.031,-.090,1.717),(.024,-.121,1.66),(-.003,-.125,1.606)], [.008,.034,.027,.0001]),
    ([(.031,-.019,1.750),(.066,-.077,1.711),(.065,-.112,1.651),(.040,-.122,1.604)], [.007,.027,.024,.0001]),
    ([(.042,-.014,1.744),(.094,-.067,1.701),(.100,-.100,1.638),(.077,-.117,1.58)], [.006,.025,.022,.0001]),
]
for i,(points,widths) in enumerate(bangs): panel('Study_Fringe_%02d'%i,points,widths,(0,-1,0))
for side in (-1,1):
    for i in range(3):
        panel('Study_Temple_%s_%s'%(side,i),
            [(side*.040,.005,1.744),(side*.112,-.033+i*.02,1.69),
             (side*(.142+i*.005),-.046+i*.020,1.615),
             (side*.126,-.075+i*.012,1.557),(side*(.073+i*.010),-.093+i*.010,1.526-i*.011)],
            [.006,.025,.028,.022,.0001],(side*.6,-.8,0))
    g.flower('Study_Blossom',(side*.111,-.061,1.704),.021,'Head')

# The nape braid is part of the reference silhouette, not a bob ending at the neck.
for i in range(7):
    z=1.505-i*.019
    for side in (-1,1):
        panel('Study_Braid_%s_%s'%(i,side),
              [(side*.013,.085,z+.018),(side*.019,.087,z),(0,.090,z-.017)],
              [.009,.012,.001],(0,1,0))

# Preserve actual mesh colours in a restrained unlit/toon study material. This
# is not the Unity production shader and is labelled as an offline art study.
for material in bpy.data.materials:
    material.use_nodes=True; nodes=material.node_tree.nodes; nodes.clear()
    out=nodes.new('ShaderNodeOutputMaterial'); emission=nodes.new('ShaderNodeEmission')
    tint=nodes.new('ShaderNodeVertexColor'); tint.layer_name='Tint'
    mix=nodes.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1
    mix.inputs[1].default_value=material.diffuse_color
    material.node_tree.links.new(tint.outputs['Color'],mix.inputs[2])
    material.node_tree.links.new(mix.outputs[0],emission.inputs[0])
    diffuse=nodes.new('ShaderNodeBsdfDiffuse')
    material.node_tree.links.new(mix.outputs[0],diffuse.inputs[0])
    blend=nodes.new('ShaderNodeMixShader'); blend.inputs[0].default_value=.22
    material.node_tree.links.new(emission.outputs[0],blend.inputs[1])
    material.node_tree.links.new(diffuse.outputs[0],blend.inputs[2])
    material.node_tree.links.new(blend.outputs[0],out.inputs[0])

scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=16
scene.view_settings.view_transform='Standard'
scene.world=bpy.data.worlds.new('Study background'); scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.14,.17,1)
light_data=bpy.data.lights.new('Study key','AREA'); light_data.energy=120; light_data.size=3
light=bpy.data.objects.new('Study key',light_data); scene.collection.objects.link(light)
light.location=(-2,-3,4); light.rotation_euler=(Vector((0,0,1.6))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
camera_data=bpy.data.cameras.new('Study camera'); camera=bpy.data.objects.new('Study camera',camera_data)
scene.collection.objects.link(camera); scene.camera=camera; camera_data.type='ORTHO'; camera_data.ortho_scale=.40
scene['status']='HEAD STUDY ONLY: not final art, not rigged, not in runtime; compare geometry before committing to full 3D production'
def aim(pos):
    camera.location=pos; camera.rotation_euler=(Vector((0,0,1.595))-camera.location).to_track_quat('-Z','Y').to_euler()
aim((0,-3,1.595))
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'slayer-head-study-v3.blend'),compress=True)
for name,pos in [('front',(0,-3,1.595)),('threequarter',(1.25,-3,1.595)),('profile',(3,0,1.595)),('back',(0,3,1.595))]:
    aim(pos); scene.render.filepath=str(REVIEW/('slayer-head-v3-'+name+'.png'))
    bpy.ops.render.render(write_still=True)
meshes=[o for o in scene.objects if o.type=='MESH']
assert all(all(math.isfinite(c) for c in v.co) for o in meshes for v in o.data.vertices)
print('HEAD_STUDY_PASS',len(meshes),'meshes; four orthographic renders; playable assets unchanged',flush=True)
