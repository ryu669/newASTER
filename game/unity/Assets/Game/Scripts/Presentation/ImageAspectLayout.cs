using UnityEngine;
namespace NewAster.Presentation {
 public sealed partial class PrototypeBootstrap {
  private static Rect ContainImage(Rect box,float width,float height){var fit=NewAster.Core.AspectLayout.Contain(box.x,box.y,box.width,box.height,width,height);return new Rect(fit.X,fit.Y,fit.Width,fit.Height);}
  private static Rect LogicalCameraRect(Rect logical){var fit=NewAster.Core.AspectLayout.Contain(0,0,Screen.width,Screen.height,1600,900);return new Rect((fit.X+fit.Width*logical.x)/Screen.width,(Screen.height-fit.Y-fit.Height+fit.Height*logical.y)/Screen.height,fit.Width*logical.width/Screen.width,fit.Height*logical.height/Screen.height);}
  private static void BeginAspectCanvas(){
   GUI.matrix=Matrix4x4.identity;var fit=NewAster.Core.AspectLayout.Contain(0,0,Screen.width,Screen.height,1600,900);var prior=GUI.color;GUI.color=new Color(.025f,.045f,.065f);
   foreach(var rect in new[]{new Rect(0,0,Screen.width,fit.Y),new Rect(0,fit.Y+fit.Height,Screen.width,Screen.height-fit.Y-fit.Height),new Rect(0,fit.Y,fit.X,fit.Height),new Rect(fit.X+fit.Width,fit.Y,Screen.width-fit.X-fit.Width,fit.Height)})if(rect.width>0 && rect.height>0)GUI.DrawTexture(rect,Texture2D.whiteTexture);
   GUI.color=prior;GUI.matrix=Matrix4x4.TRS(new Vector3(fit.X,fit.Y,0),Quaternion.identity,new Vector3(fit.Scale,fit.Scale,1));
  }
 }
}
