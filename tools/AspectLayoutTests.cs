using System;
using NewAster.Core;
internal static class AspectLayoutTests {
 public static void Run(Action<bool,string> check){
  foreach(var size in new[]{new[]{1280f,720f},new[]{1600f,900f},new[]{1280f,960f},new[]{2560f,1080f},new[]{900f,1600f}}){var f=AspectLayout.Contain(0,0,size[0],size[1],1600,900);check(Math.Abs(f.Width/f.Height-1600f/900f)<.00001f,"Logical UI ratio preserved across window sizes");check(f.X>=0 && f.Y>=0 && f.X+f.Width<=size[0]+.001 && f.Y+f.Height<=size[1]+.001,"Canvas fits inside window");check(Math.Abs((f.X+f.Width/2)-size[0]/2)<.001 && Math.Abs((f.Y+f.Height/2)-size[1]/2)<.001,"Canvas centered");}
  var wide=AspectLayout.Contain(50,100,965,640,1024,1536);check(Math.Abs(wide.Width/wide.Height-2f/3)<.00001,"Tall equipment image ratio preserved");
  var cg=AspectLayout.Contain(0,120,1600,495,1536,768);check(cg.Y>=120 && cg.Y+cg.Height<=615,"CG remains entirely above dialogue box");
  foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity}){bool rejected=false;try{AspectLayout.Contain(0,0,invalid,900,1600,900);}catch(ArgumentException){rejected=true;}check(rejected,"Invalid layout size rejected");}
  Console.WriteLine("ASPECT_LAYOUT_PASS image ratio / crop canvas / non-16:9 / CG bounds");
 }
}
