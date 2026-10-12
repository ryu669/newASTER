"""Check the unchanged default framebuffer; this does not replace visual review."""
import sys
from PIL import Image,ImageStat
im=Image.open(sys.argv[1])
assert im.size==(1920,1080),im.size
assert max(ImageStat.Stat(im.convert('RGB')).stddev)>20
