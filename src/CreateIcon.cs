using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
static class CreateIcon {
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr h);
 static void Main(string[] args){using(var b=new Bitmap(64,64)){using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(Color.FromArgb(26,61,130)))g.FillRectangle(fill,2,2,60,60);using(var fill=new SolidBrush(Color.FromArgb(98,163,255)))g.FillRectangle(fill,9,11,12,42);using(var fill=new SolidBrush(Color.White))g.FillRectangle(fill,25,11,16,42);using(var fill=new SolidBrush(Color.FromArgb(98,163,255)))g.FillRectangle(fill,45,11,10,42);}var h=b.GetHicon();try{using(var icon=Icon.FromHandle(h))using(var output=File.Create(args[0]))icon.Save(output);}finally{DestroyIcon(h);}}}
}
