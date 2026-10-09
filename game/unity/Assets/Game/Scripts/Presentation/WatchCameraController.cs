using UnityEngine;
namespace NewAster.Presentation
{
    public static class WatchCameraController
    {
        public static Rect Follow(float x,float y)=>new Rect(Mathf.Clamp(800-x*2000,-400,0),Mathf.Clamp(400-y*1125,-225,0),2000,1125);
    }
    public static class WatchModePresentation
    {
        public const int TargetFrameRate=30;
        public static float Volume(float normal,float multiplier)=>normal*Mathf.Clamp01(multiplier);
    }
}
