using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
namespace NewAster.Presentation
{
    [Serializable] public sealed class WatchModeSettings
    {
        public string heroineId;
        public int windowX,windowY,windowWidth=480,windowHeight=270;
        public bool alwaysOnTop;
        public float volumeMultiplier=.5f;
    }
    // All platform-dependent window operations stay outside the life simulation.
    public sealed class WatchWindowAdapter
    {
        [StructLayout(LayoutKind.Sequential)] private struct Rect {public int left,top,right,bottom;}
        [DllImport("kernel32.dll")] private static extern bool QueryUnbiasedInterruptTime(out ulong ticks);
        public static double RewardClock=>Supported && QueryUnbiasedInterruptTime(out ulong ticks)?ticks/10000000d:Time.realtimeSinceStartupAsDouble;
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window,IntPtr insert,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window,int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr window,int index,int value);
        [DllImport("user32.dll")] private static extern bool AdjustWindowRectEx(ref Rect rect,int style,bool menu,int extendedStyle);
        private static bool Supported=>Application.platform==RuntimePlatform.WindowsPlayer;
        private static IntPtr Handle=>Supported?Process.GetCurrentProcess().MainWindowHandle:IntPtr.Zero;
        public static bool IsVisible=>!Supported || !IsIconic(Handle);
        private Rect original;
        private int width,height;
        private FullScreenMode mode;
        private bool originalTopmost;
        private int originalStyle;
        public void Enter(WatchModeSettings settings)
        {
            width=Screen.width;height=Screen.height;mode=Screen.fullScreenMode;
            if(Supported){GetWindowRect(Handle,out original);originalTopmost=(GetWindowLong(Handle,-20)&8)!=0;originalStyle=GetWindowLong(Handle,-16);}
            settings.windowWidth=Math.Max(480,Math.Min(Math.Max(480,Screen.currentResolution.width),settings.windowWidth));settings.windowHeight=settings.windowWidth*9/16;
            Screen.SetResolution(settings.windowWidth,settings.windowHeight,FullScreenMode.Windowed);
        }
        public void Apply(WatchModeSettings settings)
        {
            if(!Supported)return;
            SetWindowLong(Handle,-16,GetWindowLong(Handle,-16)|0x00040000|0x00010000);
            int left=GetSystemMetrics(76),top=GetSystemMetrics(77),right=left+GetSystemMetrics(78),bottom=top+GetSystemMetrics(79);
            var rect=new Rect{right=settings.windowWidth,bottom=settings.windowHeight};AdjustWindowRectEx(ref rect,GetWindowLong(Handle,-16),false,GetWindowLong(Handle,-20));int outerWidth=rect.right-rect.left,outerHeight=rect.bottom-rect.top;
            settings.windowX=Math.Max(left,Math.Min(right-outerWidth,settings.windowX));settings.windowY=Math.Max(top,Math.Min(bottom-outerHeight,settings.windowY));
            SetWindowPos(Handle,new IntPtr(settings.alwaysOnTop?-1:-2),settings.windowX,settings.windowY,outerWidth,outerHeight,0x60);
            UnityEngine.Debug.Log("WATCH_WINDOW_APPLIED client="+settings.windowWidth+"x"+settings.windowHeight+" topmost="+((GetWindowLong(Handle,-20)&8)!=0)+" resizable="+((GetWindowLong(Handle,-16)&0x00040000)!=0));
        }
        public void Capture(WatchModeSettings settings)
        {if(Supported && GetWindowRect(Handle,out var r)){settings.windowX=r.left;settings.windowY=r.top;settings.windowWidth=Screen.width;settings.windowHeight=Screen.height;}}
        public void Exit()
        {Screen.SetResolution(width,height,mode);}
        public void RestorePosition(){if(Supported){SetWindowLong(Handle,-16,originalStyle);SetWindowPos(Handle,new IntPtr(originalTopmost?-1:-2),original.left,original.top,original.right-original.left,original.bottom-original.top,0x60);UnityEngine.Debug.Log("WATCH_WINDOW_RESTORED client="+Screen.width+"x"+Screen.height+" topmost="+((GetWindowLong(Handle,-20)&8)!=0));}}
    }
}
