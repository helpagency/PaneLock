using System;
using System.Windows.Forms;
static class SetupProgram {
 [STAThread]static void Main(){try{Win.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Installer.ShowInstall(null);}
}
