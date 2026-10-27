using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
sealed class RecoveryWindow {
 public long Handle {get;set;}public uint Pid {get;set;}public long Start {get;set;}public Win.Placement Placement {get;set;}public byte[] Region {get;set;}
}
sealed class RecoveryFile {
 public int ParentPid {get;set;}public long ParentStart {get;set;}public List<RecoveryWindow> Windows {get;set;}
}
sealed class Recovery:IDisposable {
 public readonly string PathName;readonly int parentPid;readonly long parentStart;Process watcher;
 public Recovery(){var p=Process.GetCurrentProcess();parentPid=p.Id;parentStart=p.StartTime.ToUniversalTime().Ticks;PathName=Path.Combine(Storage.DataDir,"recovery-"+Guid.NewGuid().ToString("N")+".json");Write(new List<Entry>());}
 public void StartWatcher(){var start=new ProcessStartInfo(Application.ExecutablePath,"--watch \""+PathName+"\"");start.UseShellExecute=false;start.CreateNoWindow=true;start.WindowStyle=ProcessWindowStyle.Hidden;watcher=Process.Start(start);for(int wait=0;wait<60&&!File.Exists(PathName+".watch-ready");wait++){if(watcher.HasExited)throw new InvalidOperationException("Recovery helper exited before becoming ready.");Thread.Sleep(25);}if(!File.Exists(PathName+".watch-ready"))throw new InvalidOperationException("Recovery helper did not become ready.");}
 public bool WatcherRunning {get{try{return watcher!=null&&!watcher.HasExited;}catch{return false;}}}public string WatchStatus {get{try{return watcher==null?"not started":watcher.HasExited?"exited "+watcher.ExitCode:"running";}catch{return "unknown";}}}
 public void Write(IEnumerable<Entry> entries){Storage.Write(PathName,new RecoveryFile{ParentPid=parentPid,ParentStart=parentStart,Windows=entries.Select(e=>new RecoveryWindow{Handle=e.H.ToInt64(),Pid=e.Pid,Start=e.Start,Placement=e.Original,Region=e.OriginalRegion}).ToList()});}
 public static bool ParentAlive(RecoveryFile file){try{var p=Process.GetProcessById(file.ParentPid);return p.StartTime.ToUniversalTime().Ticks==file.ParentStart&&!p.HasExited;}catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch{return true;}}
 public static bool Restore(RecoveryWindow saved){var h=new IntPtr(saved.Handle);if(!Win.SameWindow(h,saved.Pid,saved.Start))return true;if(!Win.RestoreRegion(h,saved.Region))return false;var p=saved.Placement;if(!Win.SetWindowPlacement(h,ref p))return false;EnsureVisible(h);return true;}
 public static void EnsureVisible(IntPtr h){Win.Rect r;if(!Win.GetWindowRect(h,out r))return;var bounds=Rectangle.FromLTRB(r.L,r.T,r.R,r.B);if(Screen.AllScreens.Any(s=>Rectangle.Intersect(s.WorkingArea,bounds).Width>=80&&Rectangle.Intersect(s.WorkingArea,bounds).Height>=60))return;var area=Screen.PrimaryScreen.WorkingArea;Win.ShowWindowAsync(h,9);Win.SetWindowPos(h,IntPtr.Zero,area.Left+30,area.Top+30,Math.Min(Math.Max(500,bounds.Width),Math.Max(100,area.Width-60)),Math.Min(Math.Max(300,bounds.Height),Math.Max(100,area.Height-60)),0x14);}
 public static int Apply(string path){RecoveryFile file;try{file=Storage.Read<RecoveryFile>(path);}catch{try{file=Storage.Read<RecoveryFile>(path+".bak");}catch{return -1;}}if(file==null||file.Windows==null||ParentAlive(file))return -1;int failed=0;foreach(var w in file.Windows)if(!Restore(w))failed++;if(failed==0)DeleteJournal(path);return failed;}
 static void DeleteJournal(string path){try{File.Delete(path);File.Delete(path+".bak");File.Delete(path+".watch-ready");File.Delete(path+".watch-error.txt");}catch{}}
 public static int RecoverOrphans(string directory){if(!Directory.Exists(directory))return 0;int count=0;foreach(var path in Directory.GetFiles(directory,"recovery-*.json")){RecoveryFile f;try{f=Storage.Read<RecoveryFile>(path);}catch{continue;}if(f!=null&&!ParentAlive(f)){int result=Apply(path);if(result==0)count++;}}return count;}
 public static int Watch(string path){try{RecoveryFile file=Storage.Read<RecoveryFile>(path);File.WriteAllText(path+".watch-ready", "ready");while(ParentAlive(file))Thread.Sleep(500);if(!File.Exists(path)&&!File.Exists(path+".bak"))return 0;int result=Apply(path);if(result!=0)File.WriteAllText(path+".watch-error.txt","Recovery result: "+result);return result==0?0:1;}catch(Exception ex){try{File.WriteAllText(path+".watch-error.txt",ex.ToString());}catch{}return 1;}}
 public void Dispose(){try{if(File.Exists(PathName)){var file=Storage.Read<RecoveryFile>(PathName);if(file.Windows!=null&&file.Windows.Count==0)DeleteJournal(PathName);}}catch{}if(watcher!=null)watcher.Dispose();}
}
sealed class Entry {
 public IntPtr H;public uint Pid;public long Start;public int Pane;public string Title,ProcessName,MonitorId,ActualMonitorId;public Win.Placement Original;public byte[] OriginalRegion;public bool Chrome,PaneFullscreen,VideoActive,VideoPending,VideoOwnsCrop;public DateTime VideoDeadline;public int CropLeft,CropTop,CropRight,CropBottom;public Rectangle LastRegion;public uint Dpi;public DateTime ReenterAt;
}
sealed class AppWindow {
 public IntPtr H;public uint Pid;public long Start;public string Title,ProcessName;
 public override string ToString(){return Title;}
 public static List<AppWindow> Read(){var list=new List<AppWindow>();Win.EnumWindows(delegate(IntPtr h,IntPtr data){if(Win.IsAssignable(h)){string title=VideoFullscreenBridge.CleanTitle(Win.Title(h));if(title.Length>0){uint pid;Win.GetWindowThreadProcessId(h,out pid);long start=Win.StartTime(pid);if(start>0)list.Add(new AppWindow{H=h,Pid=pid,Start=start,Title=title,ProcessName=Win.ProcessName(h)});}}return true;},IntPtr.Zero);return list;}
}
sealed class Engine {
 public readonly List<SavedWindow> Pending=new List<SavedWindow>();public readonly Dictionary<IntPtr,Entry> Items=new Dictionary<IntPtr,Entry>();public List<MonitorInfo> Monitors;public AppSettings Settings;public Recovery Recovery;public bool Paused;public IntPtr DraggingWindow;public string Error="";public Action Changed;bool notifying;
 public Engine(AppSettings settings,List<MonitorInfo> monitors,Recovery recovery){Settings=settings;Monitors=monitors;Recovery=recovery;Pending.AddRange(settings.Windows);}
 void Journal(){if(Recovery!=null)Recovery.Write(Items.Values);}
 public void Notify(){if(notifying)return;try{notifying=true;if(Changed!=null)Changed();}finally{notifying=false;}}
 public MonitorInfo Resolve(string id){return Monitors.FirstOrDefault(m=>m.Id==id)??Monitors.FirstOrDefault(m=>m.Primary)??Monitors.First();}
 public MonitorInfo MonitorFor(IntPtr h){string device=Screen.FromHandle(h).DeviceName;return Monitors.FirstOrDefault(m=>m.Device==device)??Resolve(null);}
 public LayoutSettings LayoutFor(MonitorInfo monitor){var l=Settings.Layout(monitor.Id);l.Constrain(monitor);return l;}
 public Rectangle Zone(Entry e){var m=Resolve(e.MonitorId);var l=LayoutFor(m);return l.Zone(l.WorkArea?m.Work:m.Bounds,e.Pane);}
 public bool Assign(IntPtr h,int pane,string monitorId,bool allowTest=false){
  if(!Win.IsWindow(h)||(!allowTest&&!Win.IsAssignable(h))){Error="Choose an application window to assign.";return false;}
  uint pid;Win.GetWindowThreadProcessId(h,out pid);long start=Win.StartTime(pid);if(start==0){Error="Could not identify this application process safely.";return false;}
  Entry e;bool added=!Items.TryGetValue(h,out e);if(added){
   var placement=new Win.Placement();placement.length=System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win.Placement));if(!Win.GetWindowPlacement(h,ref placement))return false;
   e=new Entry{H=h,Pid=pid,Start=start,Title=VideoFullscreenBridge.CleanTitle(Win.Title(h)),Chrome=Win.IsChrome(h),ProcessName=Win.ProcessName(h),Original=placement,OriginalRegion=Win.RegionData(h)};Items.Add(h,e);
  }
  bool moving=e.ActualMonitorId!=null&&e.ActualMonitorId!=Resolve(monitorId).Id;
  if(moving)PrepareMove(e);
  e.MonitorId=monitorId??MonitorFor(h).Id;e.Pane=Math.Max(0,Math.Min(LayoutFor(Resolve(e.MonitorId)).Count-1,pane));
  // Commit recovery state before changing another process's window.
  try{Journal();}catch(Exception ex){if(added)Items.Remove(h);Error="Could not save recovery state: "+ex.Message;return false;}
  Pending.RemoveAll(w=>(w.Handle==h.ToInt64()&&w.Pid==pid&&w.Start==start)||w.Title==e.Title);Error="";Tick();Notify();return true;
 }
 public void Move(IntPtr h,string monitorId,int pane){Entry e;if(Items.TryGetValue(h,out e))Assign(h,pane,monitorId);else Assign(h,pane,monitorId);}
 public void MoveAdjacent(IntPtr h,int delta){Entry e;if(!Items.TryGetValue(h,out e)){Error="Assign this window first.";return;}int i=Monitors.FindIndex(m=>m.Id==Resolve(e.MonitorId).Id);Move(h,Monitors[(i+delta+Monitors.Count)%Monitors.Count].Id,e.Pane);}
 public void PrepareMove(Entry e){if(e.PaneFullscreen){if(RestoreCrop(e))e.ReenterAt=DateTime.UtcNow.AddMilliseconds(550);}}
 public void UpdateMonitors(List<MonitorInfo> monitors){foreach(var e in Items.Values)PrepareMove(e);Monitors=monitors;Tick();Notify();}
 public void LayoutChanged(string id){foreach(var e in Items.Values.Where(e=>Resolve(e.MonitorId).Id==id))e.Pane=Math.Min(LayoutFor(Resolve(id)).Count-1,e.Pane);Tick();Notify();}
 public static bool FindContent(IntPtr h,out Win.Rect found){Win.Rect outer;Win.GetWindowRect(h,out outer);Win.Rect best=new Win.Rect();long area=0;Win.EnumChildWindows(h,delegate(IntPtr child,IntPtr data){if(Win.Class(child)=="Chrome_RenderWidgetHostHWND"&&Win.IsWindowVisible(child)){Win.Rect r;if(Win.GetWindowRect(child,out r)&&r.L>=outer.L&&r.T>=outer.T&&r.R<=outer.R&&r.B<=outer.B){long a=(long)(r.R-r.L)*(r.B-r.T);if(a>area){area=a;best=r;}}}return true;},IntPtr.Zero);found=best;return area>0;}
 public bool ToggleChromeFullscreen(IntPtr h){Entry e;if(Paused||!Items.TryGetValue(h,out e)||!e.Chrome||!Win.SameWindow(h,e.Pid,e.Start))return false;e.ReenterAt=DateTime.MinValue;if(e.PaneFullscreen){bool ok=RestoreCrop(e);Tick();return ok;}if(Win.IsZoomed(h)){Win.ShowWindowAsync(h,9);Error="Chrome is restoring. Press F11 again.";return false;}
  Win.Rect raw,content;Win.GetWindowRect(h,out raw);if(!FindContent(h,out content)||content.T-raw.T<20){Error="Exit Chrome's native fullscreen first, then press F11.";return false;}
  e.CropLeft=Math.Max(0,content.L-raw.L);e.CropTop=content.T-raw.T;e.CropRight=Math.Max(0,raw.R-content.R);e.CropBottom=Math.Max(0,raw.B-content.B);e.Dpi=Win.GetDpiForWindow(h);
  try{Journal();}catch(Exception ex){Error="Fullscreen cancelled: recovery journal could not be saved. "+ex.Message;return false;}
  e.PaneFullscreen=true;e.LastRegion=Rectangle.Empty;Error="";Tick();return true;
 }
 public bool RestoreCrop(Entry e){if(!e.PaneFullscreen)return true;if(!Win.RestoreRegion(e.H,e.OriginalRegion)){Error="Chrome could not be restored. Try Unlock all again.";return false;}e.PaneFullscreen=false;e.LastRegion=Rectangle.Empty;return true;}
 public void SetPaused(bool paused){if(paused){var resume=new HashSet<IntPtr>(Items.Values.Where(e=>e.VideoActive&&(e.VideoPending||e.VideoOwnsCrop)).Select(e=>e.H));foreach(var e in Items.Values){e.VideoPending=false;RestoreCrop(e);e.ReenterAt=DateTime.MinValue;}Paused=false;Tick();Paused=true;foreach(var e in Items.Values){e.VideoPending=e.VideoActive&&resume.Contains(e.H);e.VideoOwnsCrop=false;}}else{Paused=false;foreach(var e in Items.Values)if(e.VideoPending)e.VideoDeadline=DateTime.UtcNow.AddSeconds(4);Tick();}}

 bool ticking;
 public void Tick(){if(Paused||ticking||Monitors.Count==0)return;ticking=true;var gone=new List<IntPtr>();var reenter=new List<IntPtr>();var videoEnter=new List<IntPtr>();try{
  foreach(var e in Items.Values){if(!Win.SameWindow(e.H,e.Pid,e.Start)){gone.Add(e.H);continue;}string rawTitle=Win.Title(e.H);e.Title=VideoFullscreenBridge.CleanTitle(rawTitle);VideoFullscreenBridge.Observe(this,e,rawTitle);if(Win.IsIconic(e.H)||e.H==DraggingWindow)continue;
   var monitor=Resolve(e.MonitorId);if(e.ActualMonitorId!=null&&e.ActualMonitorId!=monitor.Id)PrepareMove(e);e.ActualMonitorId=monitor.Id;
   if(e.PaneFullscreen&&Win.GetDpiForWindow(e.H)!=e.Dpi)PrepareMove(e);
   var z=Zone(e);if(Win.IsZoomed(e.H)){Win.ShowWindowAsync(e.H,9);continue;}
   Win.Rect raw,frame;if(!Win.GetWindowRect(e.H,out raw))continue;int dl=0,dt=0,dr=0,db=0;
   if(Win.DwmGetWindowAttribute(e.H,9,out frame,16)==0){dl=frame.L-raw.L;dt=frame.T-raw.T;dr=raw.R-frame.R;db=raw.B-frame.B;}
   int x=z.X-dl,y=z.Y-dt,w=z.Width+dl+dr,h=z.Height+dt+db;
   if(e.PaneFullscreen){x=z.X-e.CropLeft;y=z.Y-e.CropTop;w=z.Width+e.CropLeft+e.CropRight;h=z.Height+e.CropTop+e.CropBottom;var region=new Rectangle(e.CropLeft,e.CropTop,z.Width,z.Height);if(region!=e.LastRegion){var hr=Win.CreateRectRgn(region.Left,region.Top,region.Right,region.Bottom);if(Win.SetWindowRgn(e.H,hr,true)==0){Win.DeleteObject(hr);Error="Chrome rejected pane clipping.";}else e.LastRegion=region;}}
   if(raw.L!=x||raw.T!=y||raw.R-raw.L!=w||raw.B-raw.T!=h){if(!Win.SetWindowPos(e.H,IntPtr.Zero,x,y,w,h,0x4014))Error="The application rejected resizing. Run it and PaneLock with the same permissions.";}
   if(e.VideoPending&&e.VideoActive&&!e.PaneFullscreen)videoEnter.Add(e.H);
   if(!e.PaneFullscreen&&e.ReenterAt!=DateTime.MinValue&&DateTime.UtcNow>=e.ReenterAt){e.ReenterAt=DateTime.MinValue;reenter.Add(e.H);}
  }
  foreach(var h in gone)Items.Remove(h);if(gone.Count>0){Journal();Notify();}
 }finally{ticking=false;}
 foreach(var h in reenter)ToggleChromeFullscreen(h);
 foreach(var h in videoEnter){Entry e;if(Items.TryGetValue(h,out e))VideoFullscreenBridge.Enter(this,e);}
 }
 public bool Release(IntPtr h,bool notify=true){Entry e;if(!Items.TryGetValue(h,out e))return true;if(!global::Recovery.Restore(new RecoveryWindow{Handle=h.ToInt64(),Pid=e.Pid,Start=e.Start,Placement=e.Original,Region=e.OriginalRegion})){Error="Could not restore a window. Its recovery record was kept.";return false;}Items.Remove(h);Journal();if(notify)Notify();return true;}
 public void ReleaseAll(bool notify=true){if(notify)Pending.Clear();foreach(var h in new List<IntPtr>(Items.Keys))Release(h,false);if(notify)Notify();}
 public void SaveAssignments(){Settings.Windows=Pending.Concat(Items.Values.Select(e=>new SavedWindow{Handle=e.H.ToInt64(),Pid=e.Pid,Start=e.Start,Title=e.Title,ProcessName=e.ProcessName,MonitorId=e.MonitorId,Pane=e.Pane})).ToList();}
 public int RestoreAssignments(List<AppWindow> windows){int skipped=0;var remembered=Pending.ToList();var claimed=new HashSet<IntPtr>(Items.Keys);foreach(var saved in remembered){AppWindow match=windows.FirstOrDefault(w=>w.H.ToInt64()==saved.Handle&&w.Pid==saved.Pid&&w.Start==saved.Start);if(match==null){var candidates=windows.Where(w=>!claimed.Contains(w.H)&&w.Title==saved.Title&&String.Equals(w.ProcessName,saved.ProcessName??"chrome",StringComparison.OrdinalIgnoreCase)).ToList();if(candidates.Count==1)match=candidates[0];}if(match!=null&&!claimed.Contains(match.H)){claimed.Add(match.H);Assign(match.H,saved.Pane,saved.MonitorId);}else skipped++;}return skipped;}
}
