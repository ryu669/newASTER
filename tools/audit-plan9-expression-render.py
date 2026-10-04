"""Read-only comparison of rendered artwork, independent of GUI labels/controls."""
import argparse
import json
import math
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('gallery')
parser.add_argument('--output', required=True)
args = parser.parse_args()
gallery = Path(args.gallery)
face = (466/1024, 280/1536, 110/1024, 83/1536)
boxes = []
for destination, crop in [((65,180,540,560),(0,0,1,1)),((650,260,460,300),(.39,.155,.235,.12))]:
    x,y,w,h = destination
    cx,cy,cw,ch = crop
    scale = min(w/(1024*cw),h/(1536*ch))
    aw,ah = 1024*cw*scale,1536*ch*scale
    dx,dy = x+(w-aw)/2,y+(h-ah)/2
    fx,fy,fw,fh = face
    boxes.append((dx+(fx-cx)/cw*aw,dy+(fy-cy)/ch*ah,fw/cw*aw,fh/ch*ah))
rows=[]
for height in (720,1080):
    baseline_path=gallery/f'iconoclast-normal-{height}.png'
    if not baseline_path.exists():
        continue
    with Image.open(baseline_path) as base:
        bounds=[(math.floor(x*base.width/1600)-2,math.floor(y*base.height/900)-2,
                 math.ceil((x+w)*base.width/1600)+2,math.ceil((y+h)*base.height/900)+2) for x,y,w,h in boxes]
        for expression in ('joy','puzzled','determined'):
            with Image.open(gallery/f'iconoclast-{expression}-{height}.png') as candidate:
                if base.size != candidate.size:
                    raise ValueError('Capture size mismatch')
                inside=outside=0
                for y in range(math.ceil(160*height/900),math.floor(760*height/900)):
                    for x in range(math.ceil(40*base.width/1600),math.floor(1170*base.width/1600)):
                        if base.getpixel((x,y)) == candidate.getpixel((x,y)):
                            continue
                        if any(left<=x<=right and top<=y<=bottom for left,top,right,bottom in bounds):
                            inside+=1
                        else:
                            outside+=1
                if outside or not inside:
                    raise ValueError(f'{expression}-{height}: inside={inside}, outside={outside}')
                rows.append(dict(expression=expression,height=height,changedInside=inside,changedOutside=outside))
                print(f'PLAN9_EXPRESSION_RENDER_PASS {expression}-{height} inside={inside} outside={outside}')
Path(args.output).write_text(json.dumps(dict(schemaVersion=1,performanceMeasured=False,
    scope='art-stage-comparison; visual seam acceptance separate',cases=rows),indent=2)+'\n',encoding='utf-8')
