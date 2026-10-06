using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewAster.Presentation
{
    // Shared image skin. Text, hit rectangles and progression remain owned by each screen.
    public static class ImageUiSkin
    {
        private static readonly Dictionary<string,Texture2D> images=new Dictionary<string,Texture2D>();
        private static readonly Dictionary<GUIStyle,GUIStyle> textButtons=new Dictionary<GUIStyle,GUIStyle>();
        private static GUISkin appliedSkin;
        public static Texture2D Image(string name)
        {
            if(images.TryGetValue(name,out var image))return image;
            image=Resources.Load<Texture2D>("UiImages/"+name);
            if(image==null)throw new InvalidOperationException("UI画像が見つかりません："+name);
            images.Add(name,image);return image;
        }
        public static void Backdrop(Rect rect)
        {GUI.DrawTexture(rect,Image("library-v1"),ScaleMode.ScaleAndCrop);Flat(rect,new Color(0,0,0,.28f));}
        private static void Flat(Rect rect,Color color)
        {var before=GUI.color;GUI.color=before*color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=before;}
        public static void Surface(Rect rect,Color color)
        {
            // Dimming scrims, HP meters and thin separators are functional overlays, not panels.
            if(rect.width<96 || rect.height<28 || color.a<.72f && Mathf.Max(color.r,color.g,color.b)<.03f){Flat(rect,color);return;}
            if(rect.width>=1599 && rect.height>=899 && color.a>=.9f){Backdrop(rect);return;}
            bool light=(color.r+color.g+color.b)/3f>.55f;
            NineSlice(rect,Image(light?"parchment-v1":"panel-v1"),new Rect(0,0,1,1),.13f,.18f,
                Mathf.Min(28,rect.width*.075f),Mathf.Min(28,rect.height*.16f),new Color(1,1,1,color.a));
        }
        public static void Frame(Rect rect,bool light=false)
        {NineSlice(rect,Image(light?"parchment-v1":"panel-v1"),new Rect(0,0,1,1),.13f,.18f,32,32,Color.white);}
        public static void ButtonArt(Rect rect,bool enabled,bool primary=false)
        {
            bool hover=enabled && rect.Contains(Event.current.mousePosition);
            Color tint=!enabled?new Color(.53f,.57f,.64f):primary?new Color(.94f,1,.85f):hover?new Color(1.15f,1.15f,1.15f):Color.white;
            // Original whole PNG is retained. Sample only the central authored button band.
            NineSlice(rect,Image("button-v1"),new Rect(32f/1536f,370f/1024f,1472f/1536f,312f/1024f),.16f,.15f,
                Mathf.Min(34,rect.width*.14f),Mathf.Min(14,rect.height*.24f),tint);
        }
        private static GUIStyle TextButton(GUIStyle source)
        {
            if(textButtons.TryGetValue(source,out var result))return result;
            // Screens also pass short-lived styles; never retain an unbounded per-frame cache.
            if(textButtons.Count>=128)textButtons.Clear();
            result=new GUIStyle {font=source.font,fontSize=source.fontSize,fontStyle=source.fontStyle,alignment=source.alignment,wordWrap=source.wordWrap,richText=source.richText,clipping=source.clipping,contentOffset=source.contentOffset,padding=new RectOffset(source.padding.left,source.padding.right,source.padding.top,source.padding.bottom),margin=new RectOffset(source.margin.left,source.margin.right,source.margin.top,source.margin.bottom),fixedWidth=source.fixedWidth,fixedHeight=source.fixedHeight,stretchWidth=source.stretchWidth,stretchHeight=source.stretchHeight};
            result.normal.textColor=source.normal.textColor;result.hover.textColor=source.hover.textColor;result.active.textColor=source.active.textColor;result.focused.textColor=source.focused.textColor;result.onNormal.textColor=source.onNormal.textColor;result.onHover.textColor=source.onHover.textColor;result.onActive.textColor=source.onActive.textColor;result.onFocused.textColor=source.onFocused.textColor;
            foreach(var state in new[]{result.normal,result.hover,result.active,result.focused,result.onNormal,result.onHover,result.onActive,result.onFocused}){state.background=null;}
            textButtons.Add(source,result);return result;
        }
        public static bool Button(Rect rect,string caption,GUIStyle style=null,bool primary=false)
        {
            var source=style??GUI.skin.button;
            if(caption.Length==0 || source==GUIStyle.none)return GUI.Button(rect,caption,source);
            ButtonArt(rect,GUI.enabled,primary);
            var label=TextButton(source);
            // Caller-owned styles can change their text colour after the cache is created.
            label.normal.textColor=source.normal.textColor;label.hover.textColor=source.hover.textColor;
            return GUI.Button(rect,caption,label);
        }
        public static string TextField(Rect rect,string value,int maxLength,GUIStyle style)
        {
            NineSlice(rect,Image("parchment-v1"),new Rect(0,0,1,1),.13f,.18f,14,6,Color.white);
            return GUI.TextField(rect,value,maxLength,TextButton(style));
        }
        public static float HorizontalSlider(Rect rect,float value,float left,float right)
        {
            var track=TextButton(GUI.skin.horizontalSlider);var thumb=TextButton(GUI.skin.horizontalSliderThumb);
            thumb.fixedWidth=20;thumb.fixedHeight=24;
            NineSlice(new Rect(rect.x,rect.y+10,rect.width,8),Image("panel-v1"),new Rect(0,0,1,1),.13f,.18f,8,3,Color.white);
            float fraction=Mathf.InverseLerp(left,right,value);
            NineSlice(new Rect(rect.x+fraction*(rect.width-20),rect.y+2,20,24),Image("parchment-v1"),new Rect(0,0,1,1),.13f,.18f,6,6,Color.white);
            return GUI.HorizontalSlider(rect,value,left,right,track,thumb);
        }
        public static void ApplyControls(GUISkin skin)
        {
            if(appliedSkin==skin)return;appliedSkin=skin;
            var paper=Image("parchment-v1");var dark=Image("panel-v1");
            foreach(var state in new[]{skin.textField.normal,skin.textField.hover,skin.textField.focused,skin.textField.active}){
                state.background=paper;state.textColor=new Color(.12f,.15f,.20f);
            }
            skin.textField.border=new RectOffset(40,40,40,40);
            foreach(var track in new[]{skin.horizontalSlider,skin.horizontalScrollbar,skin.verticalScrollbar}){
                track.normal.background=dark;track.border=new RectOffset(10,10,10,10);
            }
            foreach(var thumb in new[]{skin.horizontalSliderThumb,skin.horizontalScrollbarThumb,skin.verticalScrollbarThumb}){
                thumb.normal.background=paper;thumb.hover.background=paper;thumb.active.background=paper;
                thumb.border=new RectOffset(8,8,8,8);
            }
        }
        // Source corners keep their shape at every resolution. No CPU pixel reads or per-frame textures.
        private static void NineSlice(Rect rect,Texture2D image,Rect uv,float sx,float sy,float dx,float dy,Color tint)
        {
            if(Event.current.type!=EventType.Repaint)return;
            dx=Mathf.Min(dx,rect.width*.5f);dy=Mathf.Min(dy,rect.height*.5f);
            // Every adjacent patch shares exact screen-pixel edges, including fractional GUI scaling.
            var matrix=GUI.matrix;
            float SnapX(float x)=>Mathf.Abs(matrix.m00)<.001f?x:(Mathf.Round(x*matrix.m00+matrix.m03)-matrix.m03)/matrix.m00;
            float SnapY(float y)=>Mathf.Abs(matrix.m11)<.001f?y:(Mathf.Round(y*matrix.m11+matrix.m13)-matrix.m13)/matrix.m11;
            var saved=GUI.color;GUI.color=saved*tint;
            for(int row=0;row<3;row++)for(int col=0;col<3;col++){
                float x=col==0?rect.x:col==1?rect.x+dx:rect.xMax-dx;
                float y=row==0?rect.y:row==1?rect.y+dy:rect.yMax-dy;
                float w=col==1?rect.width-2*dx:dx,h=row==1?rect.height-2*dy:dy;
                float u=uv.x+uv.width*(col==0?0:col==1?sx:1-sx);
                float v=uv.y+uv.height*(row==0?1-sy:row==1?sy:0);
                float uw=uv.width*(col==1?1-2*sx:sx),vh=uv.height*(row==1?1-2*sy:sy);
                float right=SnapX(x+w),bottom=SnapY(y+h);x=SnapX(x);y=SnapY(y);
                if(right>x && bottom>y)GUI.DrawTextureWithTexCoords(new Rect(x,y,right-x,bottom-y),image,new Rect(u,v,uw,vh),true);
            }
            GUI.color=saved;
        }
    }
}
