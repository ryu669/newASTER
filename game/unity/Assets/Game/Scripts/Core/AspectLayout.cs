using System;
namespace NewAster.Core {
 public readonly struct AspectBox {
  public float X {get;} public float Y {get;} public float Width {get;} public float Height {get;} public float Scale {get;}
  public AspectBox(float x,float y,float width,float height,float scale){X=x;Y=y;Width=width;Height=height;Scale=scale;}
 }
 public static class AspectLayout {
  public static AspectBox Contain(float x,float y,float width,float height,float sourceWidth,float sourceHeight){
   if(float.IsNaN(width) || float.IsNaN(height) || float.IsNaN(sourceWidth) || float.IsNaN(sourceHeight) || float.IsInfinity(width) || float.IsInfinity(height) || float.IsInfinity(sourceWidth) || float.IsInfinity(sourceHeight) || width<=0 || height<=0 || sourceWidth<=0 || sourceHeight<=0)throw new ArgumentException("Aspect layout requires finite positive sizes.");
   float scale=Math.Min(width/sourceWidth,height/sourceHeight),w=sourceWidth*scale,h=sourceHeight*scale;return new AspectBox(x+(width-w)/2,y+(height-h)/2,w,h,scale);
  }
 }
}
