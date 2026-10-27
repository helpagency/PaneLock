using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
static class Installer {
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 public static bool IsStartup(){using(var key=Registry.CurrentUser.OpenSubKey(RunKey)){return key!=null&&key.GetValue("PaneLock")!=null;}}
 public static void Startup(bool enabled,string executable){using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){if(enabled)key.SetValue("PaneLock","\""+executable+"\" --background");else key.DeleteValue("PaneLock",false);}}
 public static void ShowInstall(IWin32Window owner){using(var form=new Form{AutoScaleDimensions=new SizeF(96,96),AutoScaleMode=AutoScaleMode.Dpi,Text="Install PaneLock 0.6.1",ClientSize=new Size(520,270),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=new Font("Segoe UI",10)}){
  string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PaneLock","PaneLock.exe");
  form.Controls.Add(new Label{Text="Install for your Windows account",Font=new Font("Segoe UI",15,FontStyle.Bold),AutoSize=true,Location=new Point(20,20)});
  form.Controls.Add(new Label{Text="No administrator access needed. Creates a Start menu shortcut.\n"+Path.GetDirectoryName(target),Location=new Point(22,62),Size=new Size(480,60)});
  var desktop=new CheckBox{Text="Create a desktop shortcut",Checked=true,AutoSize=true,Location=new Point(22,132)};form.Controls.Add(desktop);
  var startup=new CheckBox{Text="Start PaneLock with Windows",Checked=IsStartup(),AutoSize=true,Location=new Point(22,165)};form.Controls.Add(startup);
  var install=new Button{Text="Install / update",Location=new Point(325,215),Size=new Size(170,34)};form.Controls.Add(install);install.Click+=delegate{try{Install(target,desktop.Checked,startup.Checked);MessageBox.Show(form,"Installed. Exit any running PaneLock, then open PaneLock from the Start menu. Your saved settings are retained.","PaneLock");form.Close();}catch(Exception ex){MessageBox.Show(form,"Installation did not finish: "+ex.Message+"\nIf updating, exit PaneLock from its tray menu and try again.","PaneLock");}};form.ShowDialog(owner);
 }}
 static void Install(string target,bool desktop,bool startup){Stage(target);string startFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"PaneLock");Directory.CreateDirectory(startFolder);Shortcut(Path.Combine(startFolder,"PaneLock.lnk"),target);if(desktop)Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"PaneLock.lnk"),target);Startup(startup,target);}
 public static void Stage(string target){Directory.CreateDirectory(Path.GetDirectoryName(target));using(var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("PaneLockPayload")){
  if(payload!=null){using(var output=File.Create(target))payload.CopyTo(output);}else if(!String.Equals(Path.GetFullPath(Application.ExecutablePath),Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))File.Copy(Application.ExecutablePath,target,true);
 }
 using(var config=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppConfig")){if(config!=null)using(var output=File.Create(target+".config"))config.CopyTo(output);}
 using(var license=Assembly.GetExecutingAssembly().GetManifestResourceStream("MITLicense")){if(license!=null)using(var output=File.Create(Path.Combine(Path.GetDirectoryName(target),"LICENSE")))license.CopyTo(output);}
 // The companion remains optional. Existing loaded copies continue working.
 using(var packed=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeCompanion")){if(packed!=null){string folder=Path.Combine(Path.GetDirectoryName(target),"YouTubeCompanion");Directory.CreateDirectory(folder);using(var archive=new ZipArchive(packed,ZipArchiveMode.Read)){foreach(var entry in archive.Entries){string destination=Path.GetFullPath(Path.Combine(folder,entry.FullName));if(!destination.StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid companion path.");if(String.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(destination);continue;}Directory.CreateDirectory(Path.GetDirectoryName(destination));using(var input=entry.Open())using(var output=File.Create(destination))input.CopyTo(output);}}}}

 }
 static void Shortcut(string path,string target){var type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type);object shortcut=null;try{shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});var t=shortcut.GetType();t.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{target});t.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{Path.GetDirectoryName(target)});t.InvokeMember("Description",BindingFlags.SetProperty,null,shortcut,new object[]{"PaneLock Chrome workspaces"});t.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);}finally{if(shortcut!=null)System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}}
}
