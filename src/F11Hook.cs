using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
sealed class F11Hook:IDisposable {
 readonly Engine engine;readonly Control owner;readonly Win.HookProc callback;IntPtr hook;bool swallowed;
 public bool Installed { get { return hook!=IntPtr.Zero; } }
 public F11Hook(Engine e,Control c){engine=e;owner=c;callback=Handle;hook=Win.SetWindowsHookEx(13,callback,Win.GetModuleHandle(null),0);if(hook==IntPtr.Zero)engine.Error="F11 interception unavailable. Restart PaneLock.";}
 IntPtr Handle(int code,IntPtr message,IntPtr data){
  if(code>=0 && Marshal.ReadInt32(data)==0x7A){
   int msg=message.ToInt32();bool down=msg==0x100||msg==0x104,up=msg==0x101||msg==0x105;
   bool modified=(Win.GetAsyncKeyState(0x11)&0x8000)!=0 || (Win.GetAsyncKeyState(0x12)&0x8000)!=0 || (Win.GetAsyncKeyState(0x10)&0x8000)!=0;
   if(RouteF11(down,up,Win.GetForegroundWindow(),modified))return new IntPtr(1);
  }
  return Win.CallNextHookEx(hook,code,message,data);
 }
 public bool RouteF11(bool down,bool up,IntPtr foreground,bool modified){
  if(up && swallowed){swallowed=false;return true;}
  if(down){
   if(swallowed)return true;
   Entry e;
   if(!engine.Paused && !modified && engine.Items.TryGetValue(foreground,out e) && e.Chrome){
    swallowed=true;try{owner.BeginInvoke((Action)delegate{engine.ToggleChromeFullscreen(foreground);});}catch(InvalidOperationException){}return true;
   }
  }
  return false;
 }
 public void Dispose(){if(hook!=IntPtr.Zero){Win.UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}}
}
