using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
static class Program {
 public static bool TestMode;
 [STAThread]static int Main(string[] args){
  try{Win.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){}
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string data=Argument(args,"--data");Storage.DataDir=data??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PaneLock");
  if(args.Length>1&&args[0]=="--watch")return Recovery.Watch(args[1]);
#if INTERACTIVE_TEST
 if(args.Length==0){TestMode=true;Tests.Root=Tests.DefaultRoot();return Tests.Interactive();}
#endif
#if UI_TEST
 if(args.Length==0){TestMode=true;Tests.Root=Tests.DefaultRoot();return Tests.UI();}
#endif
#if DESKTOP_TEST
  if(args.Length==0){TestMode=true;Tests.Root=Tests.DefaultRoot();Storage.DataDir=Path.Combine(Tests.Root,"data","desktop");return Tests.Desktop();}
#endif
  if(args.Contains("--demo-window")){TestMode=true;return Tests.Demo();}
  if(args.Contains("--signal-test"))return InstanceActivation.Request(Argument(args,"--name"))?0:1;
  if(args.Contains("--self-test")){TestMode=true;return Tests.Unit();}
  if(args.Contains("--desktop-test")){TestMode=true;return Tests.Desktop();}
  if(args.Contains("--ui-test")){TestMode=true;return Tests.UI();}
  if(args.Contains("--recovery-child")){TestMode=true;return Tests.RecoveryChild(args);}
  if(args.Contains("--install")){Installer.ShowInstall(null);return 0;}
  bool created;using(var mutex=new Mutex(true,"Local\\PaneLockPrototype",out created)){
   if(!created){if(!InstanceActivation.Request(InstanceActivation.EventName)&&!InstanceActivation.ShowOlderVersion())MessageBox.Show("PaneLock is already running but its window did not respond. Look for its tray icon.","PaneLock");return 0;}
   using(var activation=new InstanceActivation(InstanceActivation.EventName)){
   Directory.CreateDirectory(Storage.DataDir);int recovered=Recovery.RecoverOrphans(Storage.DataDir);string notice;var settings=Storage.Load(out notice);if(recovered>0)notice="Recovered Chrome windows after an interrupted PaneLock session.";
   using(var recovery=new Recovery()){
    try{recovery.StartWatcher();}catch(Exception ex){MessageBox.Show("Crash protection could not start: "+ex.Message+"\nPaneLock will not change your windows until its recovery helper can run.","PaneLock");return 1;}
    var engine=new Engine(settings,MonitorInfo.Read(),recovery);
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){try{engine.SetPaused(true);engine.ReleaseAll(false);}catch{}MessageBox.Show("PaneLock restored its windows after an error: "+e.Exception.Message,"PaneLock");};
    AppDomain.CurrentDomain.UnhandledException+=delegate{try{engine.ReleaseAll(false);}catch{}};
    using(var form=new MainForm(engine,settings,recovery,args.Contains("--background"),notice)){var handle=form.Handle;activation.Attach(form);Application.Run(form);}
   }
   }
  }return 0;
 }
 public static string Argument(string[] args,string name){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
}
