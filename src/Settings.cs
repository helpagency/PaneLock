using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

sealed class MonitorInfo {
 public string Id,Device,Name;public Rectangle Bounds,Work;public bool Primary;public uint Dpi=96;
 public override string ToString(){return Name+"   "+Bounds.Width+" × "+Bounds.Height+(Primary?"   · Primary":"");}
 public static List<MonitorInfo> Read(){var result=new List<MonitorInfo>();int n=0;foreach(var screen in Screen.AllScreens.OrderBy(s=>s.Bounds.Left).ThenBy(s=>s.Bounds.Top)){
  var d=new Win.DisplayDevice();d.cb=System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win.DisplayDevice));
  bool found=Win.EnumDisplayDevices(screen.DeviceName,0,ref d,1);
  string id=found&&!String.IsNullOrEmpty(d.DeviceID)?d.DeviceID:screen.DeviceName;
  // Mirrored outputs can share an identifier. Never alias two independent screens.
  if(result.Any(m=>m.Id==id))id+="|"+screen.DeviceName;
  result.Add(new MonitorInfo{Id=id,Device=screen.DeviceName,Name="Monitor "+(++n)+(found&&!String.IsNullOrWhiteSpace(d.DeviceString)?" · "+d.DeviceString:""),Bounds=screen.Bounds,Work=screen.WorkingArea,Primary=screen.Primary,Dpi=Win.MonitorDpi(screen.Bounds)});
 }return result;}
 public static string Fingerprint(List<MonitorInfo> monitors){return String.Join(";",monitors.Select(m=>m.Id+":"+m.Bounds+":"+m.Work+":"+m.Dpi));}
}
sealed class LayoutSettings {
 public string MonitorId {get;set;}public int Count {get;set;}public bool WorkArea {get;set;}public double[] Edges {get;set;}public bool EqualSides {get;set;}
 [ScriptIgnore]public double MinimumFraction=.15;
 public int MaxPanes(MonitorInfo monitor){int width=WorkArea?monitor.Work.Width:monitor.Bounds.Width;return Math.Max(1,Math.Min(3,width/(int)Math.Ceiling(510*monitor.Dpi/96.0)));}
 public void Constrain(MonitorInfo monitor){int width=WorkArea?monitor.Work.Width:monitor.Bounds.Width;int max=MaxPanes(monitor);if(Count>max)SetCount(max);MinimumFraction=Math.Min(1.0/Count,Math.Max(.15,510*monitor.Dpi/96.0/Math.Max(1,width)));for(int i=1;i<Count;i++)Edges[i]=Math.Max(Edges[i-1]+MinimumFraction,Math.Min(1-MinimumFraction*(Count-i),Edges[i]));}
 public LayoutSettings(){EqualSides=true;Count=3;WorkArea=true;Edges=new double[]{0,1.0/3,2.0/3,1};}
 public void Normalize(){if(Count<1||Count>3)Count=3;if(Edges==null||Edges.Length!=Count+1||Edges.Any(x=>Double.IsNaN(x)||Double.IsInfinity(x))){SetCount(Count);return;}Edges[0]=0;Edges[Count]=1;for(int i=1;i<Count;i++)if(Edges[i]-Edges[i-1]<.15||1-Edges[i]<.15*(Count-i)){SetCount(Count);return;}}
 public void SetCount(int count){Count=Math.Max(1,Math.Min(3,count));Edges=Enumerable.Range(0,Count+1).Select(i=>(double)i/Count).ToArray();}
 public void Split(int index,double fraction){if(index<1||index>=Count)return;if(Count==3&&EqualSides){double side=index==1?fraction:1-fraction;side=Math.Max(MinimumFraction,Math.Min((1-MinimumFraction)/2,side));Edges[1]=side;Edges[2]=1-side;}else Edges[index]=Math.Max(Edges[index-1]+MinimumFraction,Math.Min(Edges[index+1]-MinimumFraction,fraction));}
 public bool SetPixels(int width,int middle,int left,out string error){error="";if(Count!=3){error="Choose three panes to set a middle width.";return false;}int min=(int)Math.Ceiling(MinimumFraction*width-0.00001);if(EqualSides)left=(width-middle)/2;int right=width-left-middle;if(left<min||middle<min||right<min){error="Each pane needs at least "+min+" px at this display's scaling. Adjust the middle or left width.";return false;}Edges=new double[]{0,(double)left/width,(double)(left+middle)/width,1};return true;}
 public Rectangle Zone(Rectangle area,int pane){pane=Math.Max(0,Math.Min(Count-1,pane));int l=area.Left+(int)Math.Round(area.Width*Edges[pane]),r=area.Left+(int)Math.Round(area.Width*Edges[pane+1]);return new Rectangle(l,area.Top,r-l,area.Height);}
}
sealed class SavedWindow {
 public long Handle {get;set;}public uint Pid {get;set;}public long Start {get;set;}public string Title {get;set;}public string ProcessName {get;set;}public string MonitorId {get;set;}public int Pane {get;set;}
}
sealed class AppSettings {
 public int Version {get;set;}public bool RestoreWindows {get;set;}public List<LayoutSettings> Layouts {get;set;}public List<SavedWindow> Windows {get;set;}
 public AppSettings(){Version=5;RestoreWindows=true;Layouts=new List<LayoutSettings>();Windows=new List<SavedWindow>();}
 public LayoutSettings Layout(string id){var layout=Layouts.FirstOrDefault(l=>l.MonitorId==id);if(layout==null){layout=new LayoutSettings{MonitorId=id};Layouts.Add(layout);}layout.Normalize();return layout;}
}
static class Storage {
 static readonly JavaScriptSerializer serializer=new JavaScriptSerializer{MaxJsonLength=8*1024*1024};
 public static string DataDir;
 public static string SettingsPath {get{return Path.Combine(DataDir,"settings.json");}}
 public static T Read<T>(string path) where T:new(){for(int attempt=0;;attempt++){try{using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream))return serializer.Deserialize<T>(reader.ReadToEnd());}catch(IOException){if(attempt>=4)throw;System.Threading.Thread.Sleep(25);}}}
 public static void Write(string path,object value){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,serializer.Serialize(value));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 public static AppSettings Load(out string notice){notice="";if(!File.Exists(SettingsPath))return new AppSettings();try{return Validate(Read<AppSettings>(SettingsPath));}catch{try{notice="Settings restored from backup.";return Validate(Read<AppSettings>(SettingsPath+".bak"));}catch{notice="Settings could not be read. Using defaults; the original file is preserved.";return new AppSettings();}}}
 static AppSettings Validate(AppSettings s){if(s==null)throw new InvalidDataException();if(s.Layouts==null)s.Layouts=new List<LayoutSettings>();if(s.Windows==null)s.Windows=new List<SavedWindow>();s.Layouts=s.Layouts.Where(l=>l!=null&&l.MonitorId!=null).GroupBy(l=>l.MonitorId).Select(g=>g.First()).ToList();foreach(var l in s.Layouts){l.Normalize();if(s.Version<5)l.EqualSides=l.Count==3&&Math.Abs(l.Edges[1]-(1-l.Edges[2]))<.001;}s.Version=5;s.Windows=s.Windows.Where(w=>w!=null).ToList();return s;}
}
