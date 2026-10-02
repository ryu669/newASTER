"""Original close-up character geometry, authored in metres, Z up, facing -Y.
The art sheet is a visual reference, not projected onto the mesh.
"""
import bpy
import bmesh
import math
from mathutils import Vector
from math import sin, cos, pi, exp, sqrt

objects=[]
bindings={}
body_parts=[]

def mat(name,color,metal=0,rough=.55):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=rough
    vertex=m.node_tree.nodes.new('ShaderNodeVertexColor'); vertex.layer_name='Tint'
    mix=m.node_tree.nodes.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1; mix.inputs[1].default_value=(*color,1)
    m.node_tree.links.new(vertex.outputs['Color'],mix.inputs[2]); m.node_tree.links.new(mix.outputs[0],p.inputs['Base Color'])
    return m

skin=mat('Beauty_Skin',(.96,.76,.68),rough=.68)
hair=mat('Beauty_Hair',(.73,.45,.235),rough=.57)
hair_hi=mat('Beauty_HairLight',(.84,.56,.32),rough=.54)
hair_lo=mat('Beauty_HairShade',(.62,.355,.17),rough=.46)
cloth=mat('Beauty_RoseFabric',(.79,.30,.405),rough=.85)
trim=mat('Beauty_RoseTrim',(.94,.50,.60),rough=.64)
deep=mat('Beauty_TrainingFabric',(.24,.08,.145),rough=.8)
white=mat('Beauty_EyeWhite',(.98,.945,.915),rough=.3)
iris=mat('Beauty_Iris',(1,1,1),rough=.2)
ink=mat('Beauty_Lash',(.105,.032,.035),rough=.7)
lip=mat('Beauty_Lip',(.65,.245,.265),rough=.5)
mouth_dark=mat('Beauty_Mouth',(.12,.022,.038),rough=.8)
gold=mat('Beauty_Gold',(.82,.57,.245),.68,.29)
ivory=mat('Beauty_Feather',(.95,.925,.865),rough=.7)
steel=mat('Beauty_Silver',(.65,.79,.87),.75,.22)
gem=mat('Beauty_Jewel',(.07,.60,.61),.3,.17)

def mesh(name,verts,faces,material,bone=None,colors=None):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    data.materials.append(material)
    for p in data.polygons: p.use_smooth=True
    tint=data.color_attributes.new(name='Tint',type='FLOAT_COLOR',domain='POINT')
    for i,c in enumerate(tint.data): c.color=colors[i] if colors else (1,1,1,1)
    objects.append(obj)
    if bone: bindings[obj.name]=bone
    return obj

def sphere(name,pos,scale,material,bone=None,seg=32,rings=20,body=False):
    v=[]; f=[]
    for j in range(rings+1):
        ph=pi*j/rings
        for i in range(seg):
            a=2*pi*i/seg
            v.append((pos[0]+scale[0]*sin(ph)*cos(a),pos[1]+scale[1]*sin(ph)*sin(a),pos[2]+scale[2]*cos(ph)))
    for j in range(rings):
        for i in range(seg): f.append((j*seg+i,(j+1)*seg+i,(j+1)*seg+(i+1)%seg,j*seg+(i+1)%seg))
    obj=mesh(name,v,f,material,bone)
    if body: body_parts.append(obj)
    return obj

def catmull(points,t):
    n=len(points)-1; x=min(n-1e-7,max(0,t*n)); i=int(x); u=x-i
    a=points[max(0,i-1)]; b=points[i]; c=points[min(n,i+1)]; d=points[min(n,i+2)]
    value=[.5*(2*b[k]+(-a[k]+c[k])*u+(2*a[k]-5*b[k]+4*c[k]-d[k])*u*u+(-a[k]+3*b[k]-3*c[k]+d[k])*u*u*u) for k in range(len(a))]
    return Vector(value) if len(value)<=4 else value

def tube(name,points,radii,material,bone=None,steps=40,sides=10,flatten=1,axis=None):
    verts=[]; faces=[]
    for j in range(steps+1):
        t=j/steps; c=catmull(points,t); tangent=(catmull(points,min(1,t+.001))-catmull(points,max(0,t-.001))).normalized()
        guide=Vector(axis if axis else (0,1,0))
        if abs(guide.dot(tangent))>.97: guide=Vector((1,0,0))
        u=tangent.cross(guide).normalized(); v=tangent.cross(u).normalized()
        fi=t*(len(radii)-1); ix=min(len(radii)-2,int(fi)); r=radii[ix]*(1-(fi-ix))+radii[ix+1]*(fi-ix)
        for i in range(sides):
            a=2*pi*i/sides; p=c+u*(r*cos(a))+v*(r*flatten*sin(a)); verts.append(tuple(p))
    for j in range(steps):
        for i in range(sides): faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    faces.extend([tuple(reversed(range(sides))),tuple(steps*sides+i for i in range(sides))])
    return mesh(name,verts,faces,material,bone)

def loft(name,profiles,material,bone=None,sides=64,substeps=4,body=False):
    # Profiles x,y,z, lateral radius, depth. Interpolation yields continuous taper.
    v=[]; f=[]; count=(len(profiles)-1)*substeps+1
    for j in range(count):
        p=catmull(profiles,j/(count-1))
        x,y,z,rx,ry=p
        for i in range(sides):
            a=2*pi*i/sides; v.append((x+rx*cos(a),y+ry*sin(a),z))
    for j in range(count-1):
        for i in range(sides): f.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    f.extend([tuple(reversed(range(sides))),tuple((count-1)*sides+i for i in range(sides))])
    obj=mesh(name,v,f,material,bone)
    if body: body_parts.append(obj)
    return obj

def limb(name,points,radii,depths,material,body=False):
    # A profile following a limb axis; no separate bulging joint spheres.
    obj=tube(name,points,radii,material,steps=56,sides=24,flatten=depths)
    if body: body_parts.append(obj)
    return obj

def flower(name,pos,size,bone,material=trim):
    p=Vector(pos)
    for k in range(5):
        a=2*pi*k/5
        direction=Vector((sin(a),-.12,cos(a)))
        points=[p,p+direction*size*.47+Vector((0,-size*.17,0)),p+direction*size]
        tube(name+'_Petal',points,[size*.16,size*.31,.0002],material,bone,steps=16,sides=10,flatten=.20,axis=(0,1,0))
    sphere(name+'_Core',(p.x,p.y-size*.14,p.z),(size*.15,size*.10,size*.15),gold,bone,seg=20,rings=12)

def create_body():
    loft('Torso',[(0,0,.865,.133,.104),(0,.006,.96,.158,.117),(0,0,1.065,.115,.086),(0,0,1.14,.108,.078),(0,-.006,1.25,.149,.104),(0,0,1.33,.160,.093),(0,0,1.365,.131,.073),(0,0,1.399,.047,.042)],skin,body=True)
    for s in (-1,1):
        sphere('Chest',(s*.065,-.069,1.269),(.067,.065,.076),skin,body=True)
        limb('Shoulder',[(s*.126,0,1.337),(s*.177,0,1.330),(s*.198,0,1.313)],[.043,.041,.039],1.05,skin,True)
        limb('Arm',[(s*.179,0,1.328),(s*.222,0,1.256),(s*.270,-.004,1.145),(s*.307,-.018,1.054),(s*.335,-.025,.967)],[.042,.039,.029,.033,.020],1.02,skin,True)
        sphere('Palm',(s*.345,-.024,.927),(.025,.018,.046),skin,body=True)
        for i,length in enumerate([.065,.077,.072,.058]):
            x=s*(.322+i*.016)
            limb('Finger',[(x,-.024,.909),(x+s*.004,-.026,.878),(x+s*.006,-.030,.907-length)],[.0068,.0064,.0043],.92,skin,True)
        limb('Thumb',[(s*.324,-.025,.948),(s*.306,-.034,.920),(s*.306,-.046,.897)],[.011,.008,.0055],1,skin,True)
        loft('Leg',[(s*.083,0,.918,.080,.091),(s*.088,-.001,.83,.079,.087),(s*.091,-.006,.69,.067,.075),(s*.096,-.021,.525,.043,.045),(s*.098,-.007,.447,.046,.049),(s*.10,.012,.32,.044,.049),(s*.101,.014,.16,.025,.028),(s*.101,.010,.105,.023,.027)],skin,body=True)
        sphere('Foot',(s*.101,-.049,.072),(.040,.086,.040),skin,body=True)
        for i in range(5): sphere('Toe',(s*(.075+i*.013),-.113+(i*.003),.065),(.009,.026,.012),skin,seg=16,rings=10,body=True)
    loft('Neck',[(0,0,1.36,.052,.045),(0,.004,1.415,.034,.036),(0,.008,1.479,.032,.034),(0,.008,1.535,.039,.037)],skin,body=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in body_parts: o.select_set(True)
    bpy.context.view_layer.objects.active=body_parts[0]; bpy.ops.object.join()
    body=bpy.context.object; body.name='Body_Common'
    for o in body_parts:
        if o in objects: objects.remove(o)
    objects.append(body)
    remesh=body.modifiers.new('Continuous skin','REMESH'); remesh.mode='VOXEL'; remesh.voxel_size=.0038
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    sm=body.modifiers.new('Anatomical surface relax','SMOOTH'); sm.factor=.35; sm.iterations=3; bpy.ops.object.modifier_apply(modifier=sm.name)
    shoulders=body.vertex_groups.new(name='ShoulderSurfaceRelax')
    for v in body.data.vertices:
        x,y,z=v.co
        weight=exp(-((abs(x)-.166)/.052)**2-((z-1.339)/.056)**2)
        if weight>.001: shoulders.add([v.index],weight,'REPLACE')
    sm=body.modifiers.new('Shoulder transition relax','SMOOTH'); sm.vertex_group=shoulders.name; sm.factor=.65; sm.iterations=12
    bpy.ops.object.modifier_apply(modifier=sm.name)
    group=body.vertex_groups.get('ShoulderSurfaceRelax')
    if group: body.vertex_groups.remove(group)
    dec=body.modifiers.new('Surface density','DECIMATE'); dec.ratio=.52; bpy.ops.object.modifier_apply(modifier=dec.name)
    for p in body.data.polygons: p.use_smooth=True
    # Remesh reconstructs attributes: explicitly restore a neutral tint.
    for attr in list(body.data.color_attributes): body.data.color_attributes.remove(attr)
    attr=body.data.color_attributes.new(name='Tint',type='FLOAT_COLOR',domain='POINT')
    for d in attr.data: d.color=(1,1,1,1)
    return body

HEAD=[(1.487,.005,.041,.032),(1.502,.032,.064,.049),(1.524,.060,.079,.069),(1.555,.088,.091,.088),(1.590,.102,.094,.100),(1.63,.108,.089,.104),(1.678,.103,.079,.099),(1.72,.076,.062,.074),(1.746,.003,.003,.003)]
def head_profile(z):
    for i in range(len(HEAD)-1):
        a,b=HEAD[i],HEAD[i+1]
        if z<=b[0]:
            t=max(0,(z-a[0])/(b[0]-a[0])); span=b[0]-a[0]
            prev=HEAD[max(0,i-1)]; nxt=HEAD[min(len(HEAD)-1,i+2)]
            result=[]
            for k in range(1,4):
                ma=(b[k]-prev[k])/(b[0]-prev[0]); mb=(nxt[k]-a[k])/(nxt[0]-a[0])
                result.append((2*t**3-3*t*t+1)*a[k]+(t**3-2*t*t+t)*span*ma+(-2*t**3+3*t*t)*b[k]+(t**3-t*t)*span*mb)
            return result
    return HEAD[-1][1:]

def face_y(x,z):
    width,front,back=head_profile(z)
    c=sqrt(max(0,1-(x/max(width,.001))**2))
    y=-front*c**.38
    y-=.007*exp(-(x/.007)**2-((z-1.564)/.011)**2)
    y-=.0012*exp(-(x/.007)**2-((z-1.588)/.020)**2)
    y-=.002*exp(-(x/.020)**2-((z-1.537)/.010)**2)
    return y

def eye_bounds(x,side):
    u=(side*x-.020)/.061
    if u<-.000001 or u>1.000001: return None
    u=max(0,min(1,u))
    center=1.605+.004*u
    return center+.021*sin(pi*u)**.73,center-.015*sin(pi*u)**.8,center,u

def eye_y(x,z):
    return face_y(x,z)-.0018-.0025*exp(-((abs(x)-.049)/.030)**2-((z-1.607)/.025)**2)

def add_blink(obj,side,kind):
    obj.shape_key_add(name='Basis'); key=obj.shape_key_add(name='Blink')
    for v,k in zip(obj.data.vertices,key.data):
        x,y,z=v.co; bounds=eye_bounds(x,side)
        if not bounds: continue
        top,bottom,center,u=bounds
        # Eyelids occlude the intact iris: scaling it vertically makes partial
        # blinks look like flattened stickers. Keep a tiny depth delta for FBX.
        if kind=='eye':
            k.co.y+=.00001
            continue
        center+=.003*sin(pi*u)
        if kind=='upper': k.co.z=z-(top-center)
        elif kind=='lower': k.co.z=z+(center-bottom)
        k.co.y=eye_y(x,k.co.z)-.0048

def create_face():
    verts=[]; faces=[]; colors=[]; rows=100; sides=128
    for j in range(rows+1):
        z=1.487+(1.746-1.487)*j/rows; w,f,b=head_profile(z)
        for i in range(sides):
            a=2*pi*i/sides; x=w*sin(a); c=cos(a)
            y=face_y(x,z) if c>=0 else -b*c
            verts.append((x,y,z))
            blush=exp(-((abs(x)-.064)/.026)**2-((z-1.563)/.020)**2)*max(0,c)**3
            colors.append((1,1-.15*blush,1-.12*blush,1))
    for j in range(rows):
        for i in range(sides):
            ids=(j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i)
            points=[verts[k] for k in ids]; x=sum(p[0] for p in points)/4; y=sum(p[1] for p in points)/4; z=sum(p[2] for p in points)/4
            bound=eye_bounds(x,1 if x>0 else -1)
            faces.append(ids)
    head=mesh('Head_Face',verts,faces,skin,'Head',colors)
    for s in (-1,1):
        sphere('Ear'+str(s),(s*.104,.002,1.575),(.015,.014,.028),skin,'Head',seg=32,rings=20)
        # Eye whites conform to the face; the almond outline controls the visible socket.
        ev=[]; ef=[]
        for j in range(13):
            t=j/12
            for i in range(65):
                x=s*(.020+.061*i/64); top,bottom,center,u=eye_bounds(x,s)
                z=bottom+(top-bottom)*t; ev.append((x,eye_y(x,z),z))
        for j in range(12):
            for i in range(64): ef.append((j*65+i,j*65+i+1,(j+1)*65+i+1,(j+1)*65+i))
        eye=mesh('Eye_White'+str(s),ev,ef,white,'Head'); add_blink(eye,s,'eye')
        # Radial iris detail is real vertex colour, with dark limbal ring and bright lower fibres.
        iv=[]; ic=[]; iff=[]; seg=96; nr=16
        for j in range(nr+1):
            r=j/nr
            for i in range(seg):
                a=2*pi*i/seg; x=s*.0505+.0155*r*cos(a); z=1.607+.023*r*sin(a)
                bound=eye_bounds(x,s); z=max(bound[1]+.0003,min(bound[0]-.0003,z))
                iv.append((x,eye_y(x,z)-.0012,z))
                fiber=(.5+.5*sin(a*37+r*18))*.055
                rim=max(0,(r-.80)/.20); lower=.5-.5*sin(a)
                c=(.055+.18*lower+fiber*.3,.26+.39*lower+fiber,.30+.35*lower+fiber)
                if r<.34: c=(.018,.045,.06)
                else: c=tuple(v*(1-.74*rim) for v in c)
                ic.append((*c,1))
        for j in range(nr):
            for i in range(seg): iff.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
        iris_obj=mesh('Eye_Iris'+str(s),iv,iff,iris,'Head',ic); add_blink(iris_obj,s,'eye')
        for dx,dz,r in [(-.004,.006,.0043),(.006,-.005,.0017)]:
            x=s*.0505+dx; z=1.607+dz
            glint=sphere('Eye_Glint'+str(s)+str(dx),(x,eye_y(x,z)-.0025,z),(r,.00055,r*1.15),white,'Head',seg=24,rings=12)
            add_blink(glint,s,'eye')
        for upper in (True,False):
            pts=[]
            for i in range(25):
                x=s*(.020+.061*i/24); top,bottom,center,u=eye_bounds(x,s); z=top if upper else bottom
                pts.append((x,eye_y(x,z)-.002,z))
            lash=tube('Eye_Lash'+str(s)+str(upper),pts,[.0003 if upper else .00005,.0018 if upper else .00015,.0022 if upper else .0004,.00015],ink,'Head',steps=64,sides=8,flatten=.5)
            add_blink(lash,s,'upper' if upper else 'lower')
            # Closed lids fill the socket, while the open pose is folded at its boundary.
            lv=[]; lf=[]; closed=[]
            for j in range(7):
                t=j/6
                for i in range(65):
                    x=s*(.020+.061*i/64); top,bottom,center,u=eye_bounds(x,s); boundary=top+.002 if upper else bottom-.002
                    z=boundary+(.0006*t if upper else -.0006*t)
                    lv.append((x,eye_y(x,z)-.0004,z)); nz=boundary+(center-boundary)*t
                    nz=boundary+(center+.003*sin(pi*u)-boundary)*t
                    closed.append((x,eye_y(x,nz)-.004,nz))
            for j in range(6):
                for i in range(64): lf.append((j*65+i,j*65+i+1,(j+1)*65+i+1,(j+1)*65+i))
            lid=mesh('Eye_Lid'+str(s)+str(upper),lv,lf,skin,'Head'); lid.shape_key_add(name='Basis'); key=lid.shape_key_add(name='Blink')
            for k,co in zip(key.data,closed): k.co=co
        for i in range(3):
            x=s*(.075+i*.002); z=1.613+i*.001
            tube('Eye_LashTip',[(x,eye_y(x,z)-.002,z),(x+s*.005,-.080,z+.004),(x+s*(.008+i*.001),-.078,z+.006)],[.0011,.0008,.00005],ink,'Head',steps=14,sides=6)
        brow=[]
        for i in range(9):
            x=s*(.020+.058*i/8); z=1.637+.005*sin(pi*i/8)+.003*i/8; brow.append((x,face_y(x,z)-.002,z))
        tube('Brow',brow,[.0006,.0014,.0012,.0001],hair_lo,'Head',steps=32,sides=6,flatten=.45)
    # Mouth is a shaped seam on the face, with a small lower-lip highlight.
    pts=[]
    for i in range(17):
        x=-.016+.032*i/16; z=1.537+.0018*(abs(x)/.016)**1.7
        pts.append((x,face_y(x,z)-.0009,z))
    mouth=tube('Mouth',pts,[.00012,.00055,.0006,.00012],lip,'Head',steps=40,sides=8,flatten=.5)
    mouth.shape_key_add(name='Basis'); smile=mouth.shape_key_add(name='Smile')
    for v,k in zip(mouth.data.vertices,smile.data): k.co.z+=.004*(abs(v.co.x)/.016)**1.6
    lower=tube('Lower_Lip',[(-.009,face_y(-.009,1.534)-.0006,1.534),(0,face_y(0,1.533)-.0008,1.533),(.009,face_y(.009,1.534)-.0006,1.534)],[.0001,.0003,.0001],skin,'Head',steps=24,sides=6)
    # Closed at rest; opening is a shallow anime mouth cavity, not a pasted image.
    mv=[(0,face_y(0,1.536)-.0014,1.536)]; mf=[]
    for i in range(64):
        a=2*pi*i/64; x=.012*cos(a); z=1.536+.00005*sin(a)
        mv.append((x,face_y(x,z)-.0014,z))
    for i in range(64): mf.append((0,i+1,(i+1)%64+1))
    inner=mesh('Mouth_Interior',mv,mf,mouth_dark,'Head'); inner.shape_key_add(name='Basis'); key=inner.shape_key_add(name='Talk')
    for i,k in enumerate(key.data):
        if i:
            a=2*pi*(i-1)/64; k.co.z=1.536+.007*sin(a); k.co.y=face_y(k.co.x,k.co.z)-.0014
    for obj,direction in ((mouth,1),(lower,-1)):
        if not obj.data.shape_keys: obj.shape_key_add(name='Basis')
        key=obj.shape_key_add(name='Talk')
        for v,k in zip(obj.data.vertices,key.data):
            k.co.z+=direction*.006*max(0,1-(v.co.x/.016)**2)
            k.co.y=face_y(k.co.x,k.co.z)-.002
    for obj in list(objects):
        if obj.name.startswith('Brow') or obj==mouth:
            if not obj.data.shape_keys: obj.shape_key_add(name='Basis')
            for emotion in ('Sad','Angry','Surprise'):
                key=obj.shape_key_add(name=emotion)
                for v,k in zip(obj.data.vertices,key.data):
                    if obj.name.startswith('Brow'):
                        inner_weight=max(0,min(1,(.079-abs(v.co.x))/.06))
                        k.co.z+=(.009*inner_weight if emotion=='Sad' else -.009*inner_weight if emotion=='Angry' else .009)
                    elif emotion=='Sad': k.co.z-=.005*(abs(v.co.x)/.016)**1.5
                    elif emotion=='Angry': k.co.z-=.002*(abs(v.co.x)/.016)**1.5
                    k.co.y=face_y(k.co.x,k.co.z)-.002
    return head

def hair_lock(name,points,width,material,depth=.18):
    obj=tube(name,points,[width*.35,width*.85,width*.78,width*.58,width*.25,.0001],material,'Head',steps=64,sides=14,flatten=depth,axis=(0,1,0))
    # Authored flow coordinates and strand colour survive FBX. The highlight
    # follows each curved lock instead of treating hair like a shiny cylinder.
    uv=obj.data.uv_layers.new(name='HairFlow')
    for loop in obj.data.loops:
        index=loop.vertex_index; uv.data[loop.index].uv=((index%14)/13,(index//14)/64)
    colors=obj.data.color_attributes['Tint']
    for vertex in obj.data.vertices:
        t=(vertex.index//14)/64; u=(vertex.index%14)/14
        band=exp(-((vertex.co.z-1.697-.002*sin(u*12*pi))/.013)**2)
        root=.09*exp(-(t/.14)**2); tip=.11*t**3
        strands=.022*(.5+.5*sin(u*28*pi+t*9))
        shade=.87-root-tip-strands+.13*band
        colors.data[vertex.index].color=(min(1,shade+.04*band),min(1,shade+.025*band),shade,1)
    return obj

def create_hair():
    # Under-cap smoothly covers the cranium and the nape; the fringe hides its front edge.
    verts=[]; faces=[]
    for j in range(49):
        for i in range(96):
            a=2*pi*i/96; front=max(0,-sin(a)); end=2.33-1.10*front**3; ph=.003+j/48*(end-.003)
            depth=-front**.38 if sin(a)<0 else sin(a)
            verts.append((.122*sin(ph)*cos(a),.010+.119*sin(ph)*depth,1.626+.135*cos(ph)))
    for j in range(48):
        for i in range(96): faces.append((j*96+i,(j+1)*96+i,(j+1)*96+(i+1)%96,j*96+(i+1)%96))
    mesh('Hair_Underlayer',verts,faces,hair,'Head')
    # A continuous inner bob prevents isolated hanging locks and background gaps
    # when the face fills the screen. Front remains open around the jaw.
    verts=[]; faces=[]; rows=32; segments=80
    for j in range(rows+1):
        t=j/rows
        radius=.113+.025*sin(pi*t)-.007*t**4
        for i in range(segments+1):
            a=-.25+(pi+.50)*i/segments
            z=1.655-.174*t+.004*sin(i*.7)*t**6
            verts.append((radius*cos(a),.005+radius*.89*sin(a),z))
    for j in range(rows):
        for i in range(segments):
            k=j*(segments+1)+i
            faces.append((k,k+segments+1,k+segments+2,k+1))
    bob=mesh('Hair_InnerBob',verts,faces,hair_lo,'Head')
    solid=bob.modifiers.new('Bob shell thickness','SOLIDIFY'); solid.thickness=.003
    bpy.context.view_layer.objects.active=bob; bpy.ops.object.modifier_apply(modifier=solid.name)
    # Curved panels sweep from the parting to curled tips; multiple layers keep volume.
    for i in range(15):
        a=-.25+3.64*i/14; sx=cos(a); sy=sin(a)
        points=[(.014*sx,.012,1.752),(.083*sx,.011+.082*sy,1.717),(.124*sx,.013+.115*sy,1.64),(.137*sx,.012+.124*sy,1.55),(.128*sx,-.003+.108*sy,1.493),(.114*sx,-.010+.098*sy,1.469+.009*sin(i*2.3))]
        hair_lock('Hair_BackLock',points,.025+(i%3)*.002,hair_hi if i%4==0 else hair)
    for s in (-1,1):
        for i in range(3):
            y=-.078+i*.030
            points=[(s*.025,-.001,1.750),(s*.081,y*.55,1.714),(s*.116,y,1.65),(s*(.131+i*.002),y-.004,1.565),(s*.122,y-.027,1.497),(s*(.088+i*.006),y-.030,1.484)]
            hair_lock('Hair_SideLock',points,.020+i*.002,hair_hi if i==0 else hair,depth=.16)
    fringe=[(-.089,1.585),(-.067,1.616),(-.043,1.629),(-.019,1.642),(.004,1.649),(.029,1.645),(.053,1.634),(.083,1.603)]
    for i,(x,z) in enumerate(fringe):
        points=[(.022+x*.19,-.003,1.752),(.020+x*.52,-.077,1.726),(.010+x*.85,-.106,1.687),(x+.008,-.115,(1.665+z)*.5),(x,-.113,z)]
        hair_lock('Hair_SweptFringe',points,.013 if i in (0,7) else .018,hair_hi if i%3==0 else hair,depth=.12)
    # Thin flyaway locks break the solid silhouette.
    for s in (-1,1):
        hair_lock('Hair_Flyaway',[(s*.09,-.018,1.70),(s*.146,-.056,1.63),(s*.159,-.08,1.558),(s*.132,-.12,1.520)],.0032,hair_hi)
    tube('Hair_Ahoge',[(-.015,0,1.748),(-.04,.002,1.815),(.028,.004,1.833),(.073,-.006,1.794),(.063,-.020,1.772)],[.0035,.003,.002,.0001],hair_hi,'Head',steps=80,sides=10,flatten=.3)
    for s in (-1,1):
        flower('Hair_Blossom',(s*.11,-.051,1.686),.025,'Head')
        flower('Hair_SmallBlossom',(s*.124,-.038,1.658),.014,'Head')
        for i in range(4): sphere('Hair_Pearl',(s*(.12+.007*sin(i)), -.047,1.64-i*.009),(.003,.003,.003),ivory,'Head',seg=16,rings=10)

def create_outfits():
    profiles=[(0,0,.765,.191,.126),(0,0,.82,.186,.126),(0,.003,.94,.166,.127),(0,0,1.05,.129,.101),(0,0,1.13,.122,.093),(0,-.020,1.24,.167,.144),(0,-.027,1.28,.170,.143),(0,-.009,1.335,.161,.116),(0,0,1.40,.049,.049)]
    dress=loft('Outfit_Rose_Dress',profiles,cloth,sides=96,substeps=6)
    # Open garment shells retain thin material thickness and edge definition.
    solid=dress.modifiers.new('Garment thickness','SOLIDIFY'); solid.thickness=.0025
    bpy.context.view_layer.objects.active=dress; bpy.ops.object.modifier_apply(modifier=solid.name)
    loft('Outfit_Rose_Hem',[(0,0,.764,.194,.129),(0,0,.786,.193,.130)],trim,sides=96,substeps=2)
    loft('Outfit_Rose_Collar',[(0,0,1.397,.052,.052),(0,0,1.43,.043,.045)],trim,'Neck',sides=64,substeps=4)
    loft('Outfit_Rose_Belt',[(0,0,1.04,.136,.108),(0,0,1.058,.132,.106)],trim,sides=96,substeps=2)
    # Fine piping follows the tailored seams rather than painting the whole body.
    for angle in [-2.20,-1.87,-1.27,-.94]:
        pts=[]
        for row in profiles[1:8]:
            x,y,z,rx,ry=row; pts.append(((rx+.0015)*cos(angle),y+(ry+.0015)*sin(angle),z))
        tube('Outfit_Rose_Seam',pts,[.0012,.0012],trim,steps=64,sides=6)
    tube('Outfit_Rose_Diagonal',[(-.039,-.042,1.404),(.02,-.14,1.33),(.081,-.146,1.20),(.106,-.104,1.08),(.147,-.096,.84)],[.006,.005,.004],trim,steps=90,sides=10,flatten=.24)
    flower('Outfit_Rose_ThroatFlower',(0,-.065,1.405),.025,'Neck')
    for x,y,z,size in [(-.127,-.087,.837,.022),(-.15,-.075,.864,.016),(-.121,-.103,.896,.016)]: flower('Outfit_Rose_Embroidery',(x,y,z),size,'Hips')
    for s,suf in [(-1,'.L'),(1,'.R')]:
        # Detached sleeves follow the arm contours, leaving shoulder and fingers visible.
        limb('Outfit_Rose_Sleeve',[(s*.226,0,1.257),(s*.27,-.005,1.145),(s*.31,-.017,1.053),(s*.335,-.025,.975)],[.042,.033,.036,.023],1.03,cloth)
        flower('Outfit_Rose_Cuff',(s*.338,-.050,.986),.014,'Forearm'+suf)
        boot=loft('Outfit_Rose_Boot',[(s*.101,.011,.09,.034,.037),(s*.101,.010,.17,.030,.032),(s*.100,.011,.31,.050,.055),(s*.098,-.006,.43,.052,.055),(s*.096,-.015,.505,.048,.052)],ivory,'Shin'+suf,sides=48,substeps=5)
        sphere('Outfit_Rose_Shoe',(s*.101,-.063,.074),(.045,.104,.040),ivory,'Foot'+suf,seg=48,rings=24)
        loft('Outfit_Rose_Heel',[(s*.101,.013,.030,.015,.017),(s*.101,.016,.084,.018,.024)],gold,'Foot'+suf,sides=24,substeps=3)
        flower('Outfit_Rose_BootFlower',(s*.105,-.062,.455),.019,'Shin'+suf)
        for offset in (-.039,.039): tube('Outfit_Rose_BootPiping',[(s*.101+offset,-.030,.48),(s*.101+offset*.8,-.020,.32),(s*.101+offset*.7,-.017,.17)],[.0015,.0012],gold,'Shin'+suf,steps=50,sides=6)
    loft('Outfit_Training_Tunic',[(0,0,.78,.198,.133),(0,.003,.94,.173,.134),(0,0,1.13,.130,.103),(0,-.020,1.24,.177,.154),(0,-.021,1.30,.177,.151),(0,0,1.37,.168,.102)],deep,sides=80,substeps=6)
    for s,suf in [(-1,'.L'),(1,'.R')]:
        loft('Outfit_Training_Legging',[(s*.101,.01,.11,.029,.032),(s*.1,.014,.32,.05,.055),(s*.096,-.02,.525,.05,.053),(s*.09,-.006,.69,.075,.083),(s*.088,0,.83,.087,.095),(s*.083,0,.91,.088,.104)],deep,sides=48,substeps=5)
        sphere('Outfit_Training_Shoe',(s*.101,-.061,.071),(.046,.104,.043),deep,'Foot'+suf,seg=32,rings=20)

def feather(name,root,tip,width,bone):
    root,tip=Vector(root),Vector(tip); direction=(tip-root).normalized(); lateral=Vector((direction.z,0,-direction.x)).normalized()
    verts=[]; faces=[]; colors=[]
    for j in range(33):
        t=j/32; center=root.lerp(tip,t)+Vector((0,-.017*sin(pi*t),.014*sin(pi*t)))
        w=width*sin(pi*t)**.67*(1-.3*t)+.0002
        for i in range(13):
            u=-1+2*i/12; pos=center+lateral*(w*u)+Vector((0,-.004*(1-u*u),0)); verts.append(tuple(pos))
            shade=.965+.035*cos(i*pi/12); colors.append((shade,shade,shade,1))
    for j in range(32):
        for i in range(12): faces.append((j*13+i,j*13+i+1,(j+1)*13+i+1,(j+1)*13+i))
    obj=mesh(name,verts,faces,ivory,bone,colors)
    solid=obj.modifiers.new('Feather thickness','SOLIDIFY'); solid.thickness=.0007; bpy.context.view_layer.objects.active=obj; bpy.ops.object.modifier_apply(modifier=solid.name)
    tube(name+'_Quill',[root,root.lerp(tip,.5)+Vector((0,-.021,.014)),tip],[.0014,.001,.0001],ivory,bone,steps=30,sides=6)

def create_wings_and_sword():
    for s,suf in [(-1,'.L'),(1,'.R')]:
        for i in range(16):
            root=(s*(.123+.005*i),.102,1.308-.006*i)
            tip=(s*(.44+.023*i),.125,1.66-.042*i)
            feather('Wing_Primary',root,tip,.026+min(i,9)*.0017,'Wing'+suf)
        for i in range(10):
            root=(s*(.12+.010*i),.077,1.308-.006*i)
            tip=(s*(.31+.028*i),.081,1.45-.028*i)
            feather('Wing_Covert',root,tip,.026,'Wing'+suf)
        flower('Wing_Blossom',(s*.162,.052,1.312),.021,'Wing'+suf)
    x=.361; y=-.038
    blade=loft('Weapon_Blade',[(x,y,.833,.021,.006),(x,y,.39,.015,.005),(x,y,.285,.0001,.0001)],steel,'Hand.R',sides=4,substeps=1)
    tube('Weapon_Grip',[(x,y,.830),(x,y,.927)],[.011,.010],trim,'Hand.R',steps=16,sides=12)
    tube('Weapon_Guard',[(x-.070,y,.824),(x-.037,y,.833),(x,y,.822),(x+.037,y,.833),(x+.07,y,.824)],[.004,.004,.004],gold,'Hand.R',steps=50,sides=10)
    flower('Weapon_Blossom',(x,y-.012,.828),.024,'Hand.R')
    for s in (-1,1): tube('Weapon_Filigree',[(x+s*.013,y-.006,.803),(x+s*.013,y-.006,.66),(x+s*.008,y-.006,.48)],[.0012,.0009],gold,'Hand.R',steps=40,sides=6)
    sphere('Weapon_Pommel',(x,y,.936),(.013,.012,.017),gold,'Hand.R',seg=24,rings=16)

def skeleton():
    bones={'Root':((0,0,0),(0,0,.15),None),'Hips':((0,0,.90),(0,0,1.04),'Root'),'Spine':((0,0,1.04),(0,0,1.21),'Hips'),'Chest':((0,0,1.21),(0,0,1.39),'Spine'),'Neck':((0,0,1.39),(0,0,1.479),'Chest'),'Head':((0,0,1.479),(0,0,1.745),'Neck')}
    for s,suf in [(-1,'.L'),(1,'.R')]:
        bones['UpperArm'+suf]=((s*.179,0,1.348),(s*.270,-.004,1.145),'Chest')
        bones['Forearm'+suf]=((s*.270,-.004,1.145),(s*.335,-.025,.967),'UpperArm'+suf)
        bones['Hand'+suf]=((s*.335,-.025,.967),(s*.345,-.024,.913),'Forearm'+suf)
        for i,length in enumerate([.065,.077,.072,.058]):
            x=s*(.322+i*.016); bones[f'Finger{i}'+suf]=((x,-.024,.909),(x+s*.006,-.030,.907-length),'Hand'+suf)
        bones['Thumb'+suf]=((s*.324,-.025,.948),(s*.306,-.046,.897),'Hand'+suf)
        bones['Thigh'+suf]=((s*.083,0,.918),(s*.096,-.021,.525),'Hips')
        bones['Shin'+suf]=((s*.096,-.021,.525),(s*.101,.010,.105),'Thigh'+suf)
        bones['Foot'+suf]=((s*.101,.010,.105),(s*.101,-.14,.060),'Shin'+suf)
        bones['Wing'+suf]=((s*.123,.102,1.308),(s*.65,.125,1.35),'Chest')
    return bones

def build():
    body=create_body(); create_face(); create_hair(); create_outfits(); create_wings_and_sword()
    dress=next(o for o in objects if o.name=='Outfit_Rose_Dress')
    for obj in objects:
        if obj.name.startswith(('Outfit_Rose_Seam','Outfit_Rose_Diagonal')):
            mod=obj.modifiers.new('Fitted garment detail','SHRINKWRAP'); mod.target=dress; mod.offset=.0013
            bpy.context.view_layer.objects.active=obj; bpy.ops.object.modifier_apply(modifier=mod.name)
        if obj==body or obj.name.startswith(('Outfit_','Weapon_')):
            for v in obj.data.vertices:
                x,y,z=v.co
                if obj.name.startswith('Outfit_') and .78<z<1.05 and abs(x)<.23:
                    ease=exp(-((z-.94)/.085)**2)
                    v.co.x*=1+.04*ease; v.co.y*=1+.06*ease
                if z>.78:
                    q=max(0,min(1,(abs(x)-.14)/.055)); q=q*q*(3-2*q)
                    v.co.x-=math.copysign(.018*q,x)
                    if obj==body and z>1.29:
                        h=min(1,(z-1.29)/.10); v.co.z-=.020*q*h*h*(3-2*h)
    # Normalize winding for Unity back-face culling, including all shape-key topology.
    for obj in objects:
        bm=bmesh.new(); bm.from_mesh(obj.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        if obj.name.startswith(('Eye_White','Eye_Iris','Eye_Lid')) and sum(f.normal.y for f in bm.faces)>0:
            bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
        bm.to_mesh(obj.data); bm.free()
        for p in obj.data.polygons: p.use_smooth=True
    bones=skeleton()
    for name,(a,b,p) in list(bones.items()):
        if name.startswith(('UpperArm','Forearm','Hand','Finger','Thumb')):
            a=list(a); b=list(b); a[0]-=math.copysign(.018,a[0]); b[0]-=math.copysign(.018,b[0])
            bones[name]=(a,b,p)
    return objects,bindings,bones,body
