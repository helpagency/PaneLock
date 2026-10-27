using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
static class Win {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern bool SetWindowText(IntPtr h,string title);
 [DllImport("user32.dll")]public static extern IntPtr WindowFromPoint(Point point);
 [DllImport("user32.dll")]public static extern IntPtr GetAncestor(IntPtr window,uint flags);
 public delegate void EventProc(IntPtr hook,uint kind,IntPtr h,int obj,int child,uint thread,uint time);
 [DllImport("user32.dll")]public static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,EventProc callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")]public static extern bool UnhookWinEvent(IntPtr hook);
 [DllImport("user32.dll")]public static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]public static extern IntPtr GetWindowLongPtr(IntPtr h,int index);
 [DllImport("user32.dll")]public static extern IntPtr SendMessageTimeout(IntPtr h,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
 [DllImport("user32.dll")]public static extern bool AllowSetForegroundWindow(uint process);
 public static string ProcessName(IntPtr h){uint pid;GetWindowThreadProcessId(h,out pid);try{using(var p=System.Diagnostics.Process.GetProcessById((int)pid))return p.ProcessName;}catch{return "";}}
 public static bool IsAssignable(IntPtr h,bool allowSelf=false){
  if(!IsWindow(h)||!IsWindowVisible(h)||GetWindow(h,4)!=IntPtr.Zero||Title(h).Length==0)return false;
  uint pid;GetWindowThreadProcessId(h,out pid);if(!allowSelf&&pid==(uint)System.Diagnostics.Process.GetCurrentProcess().Id)return false;
  string c=Class(h);if(c=="Shell_TrayWnd"||c=="Shell_SecondaryTrayWnd"||c=="Progman"||c=="WorkerW"||c=="DV2ControlHost")return false;
  long style=GetWindowLongPtr(h,-20).ToInt64();if((style&0x08000080)!=0)return false;
  return StartTime(pid)>0;
 }

 [DllImport("user32.dll")]public static extern IntPtr MonitorFromPoint(Point point,uint flags);
 [DllImport("shcore.dll")]public static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
 public static uint MonitorDpi(Rectangle bounds){try{uint x,y;return GetDpiForMonitor(MonitorFromPoint(new Point{X=bounds.Left+bounds.Width/2,Y=bounds.Top+bounds.Height/2},2),0,out x,out y)==0?x:96;}catch{return 96;}}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct DisplayDevice {public int cb;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string DeviceName;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceString;public int StateFlags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceID;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceKey;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern bool EnumDisplayDevices(string name,uint i,ref DisplayDevice d,uint flags);
 [DllImport("user32.dll")]public static extern bool EnumWindows(EnumProc callback,IntPtr data);
 [DllImport("gdi32.dll")]public static extern uint GetRegionData(IntPtr region,uint size,byte[] data);
 [DllImport("gdi32.dll")]public static extern IntPtr ExtCreateRegion(IntPtr xform,uint size,byte[] data);
 [DllImport("user32.dll")]public static extern uint GetDpiForWindow(IntPtr h);
 [DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]public static extern IntPtr GetWindow(IntPtr h,uint command);
 [DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr h);
 public static string Title(IntPtr h){var s=new StringBuilder(512);GetWindowText(h,s,s.Capacity);return s.ToString();}
 public static string Class(IntPtr h){var s=new StringBuilder(128);GetClassName(h,s,s.Capacity);return s.ToString();}
 public static byte[] RegionData(IntPtr h){var r=CreateRectRgn(0,0,0,0);try{if(GetWindowRgn(h,r)<=0)return null;uint n=GetRegionData(r,0,null);if(n==0||n>1048576)throw new InvalidOperationException("Could not save the original window region.");var data=new byte[n];if(GetRegionData(r,n,data)!=n)throw new InvalidOperationException("Could not read window region.");return data;}finally{DeleteObject(r);}}
 public static bool RestoreRegion(IntPtr h,byte[] data){IntPtr region=IntPtr.Zero;if(data!=null&&data.Length>0){region=ExtCreateRegion(IntPtr.Zero,(uint)data.Length,data);if(region==IntPtr.Zero)return false;}if(SetWindowRgn(h,region,true)!=0)return true;if(region!=IntPtr.Zero)DeleteObject(region);return false;}
 public static long StartTime(uint pid){try{using(var p=System.Diagnostics.Process.GetProcessById((int)pid))return p.StartTime.ToUniversalTime().Ticks;}catch{return 0;}}
 public static bool SameWindow(IntPtr h,uint pid,long start){uint actual;GetWindowThreadProcessId(h,out actual);return IsWindow(h)&&actual==pid&&start>0&&StartTime(pid)==start;}
 public static bool IsChrome(IntPtr h){uint pid;GetWindowThreadProcessId(h,out pid);if(!Class(h).StartsWith("Chrome_WidgetWin_"))return false;try{using(var p=System.Diagnostics.Process.GetProcessById((int)pid))return p.ProcessName.Equals("chrome",StringComparison.OrdinalIgnoreCase);}catch{return false;}}

 public delegate bool EnumProc(IntPtr h,IntPtr data);
 public delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h,EnumProc callback,IntPtr data);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,StringBuilder text,int max);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr h,IntPtr region,bool redraw);
 [DllImport("user32.dll")] public static extern int GetWindowRgn(IntPtr h,IntPtr region);
 [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int l,int t,int r,int b);
 [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
 [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr SetWindowsHookEx(int type,HookProc callback,IntPtr module,uint thread);
 [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string module);
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls,string title);

 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L,T,R,B; }
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] public struct Placement { public int length,flags,show; public Point min,max; public Rect normal; }
 [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr h,ref Placement p);
 [DllImport("user32.dll")] public static extern bool SetWindowPlacement(IntPtr h,ref Placement p);
 [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h,int c);
 [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
 [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h,int a,out Rect r,int size);
}
