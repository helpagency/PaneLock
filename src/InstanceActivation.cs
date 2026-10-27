using System;
using System.Threading;
using System.Windows.Forms;
sealed class InstanceActivation:IDisposable {
 public const string EventName="Local\\PaneLock.Activate.v1";
 readonly EventWaitHandle signal;RegisteredWaitHandle registration;
 public InstanceActivation(string name){signal=new EventWaitHandle(false,EventResetMode.AutoReset,name);}
 public void Attach(MainForm form){registration=ThreadPool.RegisterWaitForSingleObject(signal,delegate(object state,bool timedOut){try{form.BeginInvoke((Action)form.ShowWorkspace);}catch(InvalidOperationException){}},null,Timeout.Infinite,false);}
 public static bool Request(string name){for(int attempt=0;attempt<30;attempt++){try{using(var signal=EventWaitHandle.OpenExisting(name)){Win.AllowSetForegroundWindow(UInt32.MaxValue);signal.Set();return true;}}catch(WaitHandleCannotBeOpenedException){Thread.Sleep(50);}catch(UnauthorizedAccessException){return false;}}return false;}
 public static bool ShowOlderVersion(){IntPtr found=IntPtr.Zero;Win.EnumWindows(delegate(IntPtr h,IntPtr data){if(Win.Title(h).StartsWith("PaneLock 0.")&&Win.ProcessName(h).StartsWith("PaneLock",StringComparison.OrdinalIgnoreCase)){found=h;return false;}return true;},IntPtr.Zero);if(found==IntPtr.Zero)return false;uint pid;Win.GetWindowThreadProcessId(found,out pid);Win.AllowSetForegroundWindow(pid);Win.ShowWindowAsync(found,9);Win.SetForegroundWindow(found);return true;}
 public void Dispose(){if(registration!=null)registration.Unregister(null);signal.Dispose();}
}
