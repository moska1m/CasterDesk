using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CasterDesk.Core;

namespace CasterDesk.Wpf;
internal static class Ui
{
 public static Button Button(string text,Action action,bool primary=false)
 {var b=new Button{Content=L.T(text),Margin=new Thickness(0,0,0,8)};if(primary)b.SetResourceReference(FrameworkElement.StyleProperty,"PrimaryButton");b.Click+=(_,_)=>action();return b;}
 public static TextBlock Text(string text,bool heading=false)=>new(){Text=L.T(text),FontSize=heading?19:14,FontWeight=heading?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,heading?12:0,0,10)};
 public static Border Panel(UIElement child){var panel=new Border{Child=child,Padding=new Thickness(18),Margin=new Thickness(0,0,12,12)};panel.SetResourceReference(Border.BackgroundProperty,"PanelBrush");return panel;}
 public static Image Gear()
 {
  var group=new GeometryGroup{FillRule=FillRule.EvenOdd};
  void Add(double x,double y,double radius)
  {var outline=new StreamGeometry();using(var context=outline.Open()){for(int i=0;i<32;i++){double angle=(i+.5)*Math.PI*2/32,r=radius*(i%4 is 1 or 2?1:.73);var p=new Point(x+Math.Cos(angle)*r,y+Math.Sin(angle)*r);if(i==0)context.BeginFigure(p,true,true);else context.LineTo(p,true,false);}}group.Children.Add(outline);group.Children.Add(new EllipseGeometry(new Point(x,y),radius*.36,radius*.36));}
  Add(15,15,8.1);Add(32,31,12.5);var picture=new DrawingImage(new GeometryDrawing(Brushes.White,null,group));picture.Freeze();return new Image{Source=picture,Width=32,Height=32,Stretch=Stretch.Uniform};
 }
 public static ComboBox Combo(params string[] choices){var box=new ComboBox{Margin=new Thickness(0,0,0,10)};foreach(string c in choices)box.Items.Add(L.T(c));box.SelectedIndex=choices.Length>0?0:-1;return box;}
 public static BitmapSource Bitmap(System.Drawing.Image image)
 {
  double scale=Math.Min(1,1280.0/Math.Max(image.Width,image.Height));
  using var bitmap=new System.Drawing.Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)),System.Drawing.Imaging.PixelFormat.Format32bppArgb);
  using(var drawing=System.Drawing.Graphics.FromImage(bitmap)){drawing.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;drawing.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;drawing.DrawImage(image,0,0,bitmap.Width,bitmap.Height);}
  var data=bitmap.LockBits(new System.Drawing.Rectangle(0,0,bitmap.Width,bitmap.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
  try{var source=BitmapSource.Create(bitmap.Width,bitmap.Height,96,96,PixelFormats.Bgra32,null,data.Scan0,data.Stride*bitmap.Height,data.Stride);source.Freeze();return source;}finally{bitmap.UnlockBits(data);}
 }
 static readonly System.Collections.Generic.Dictionary<string,BitmapSource> images=new(StringComparer.OrdinalIgnoreCase);
 public static BitmapSource FileImage(string path)
 {string key=Path.GetFullPath(path)+File.GetLastWriteTimeUtc(path).Ticks;if(images.TryGetValue(key,out var cached))return cached;using var stream=File.OpenRead(path);var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.DecodePixelWidth=512;b.StreamSource=stream;b.EndInit();b.Freeze();if(images.Count>=128)images.Clear();images[key]=b;return b;}
 public static void OpenFolder(string path)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe",path){UseShellExecute=true});
}
