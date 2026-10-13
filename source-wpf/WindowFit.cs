using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace CasterDesk.Wpf;
internal static class WindowFit
{
 [StructLayout(LayoutKind.Sequential)]struct Area{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct Monitor{public uint Size;public Area Bounds,Work;public uint Flags;}
 [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="GetMonitorInfoW")]static extern bool GetMonitorInfo(IntPtr monitor,ref Monitor info);
 public static Size Available(Window? owner)
 {
  if(owner!=null){var handle=new WindowInteropHelper(owner).Handle;if(handle!=IntPtr.Zero){var info=new Monitor{Size=(uint)Marshal.SizeOf<Monitor>()};if(GetMonitorInfo(MonitorFromWindow(handle,2),ref info)){var dpi=VisualTreeHelper.GetDpi(owner);return new Size((info.Work.Right-info.Work.Left)/dpi.DpiScaleX,(info.Work.Bottom-info.Work.Top)/dpi.DpiScaleY);}}}
  return SystemParameters.WorkArea.Size;
 }
 public static void Clamp(Window window){var area=Available(window.Owner??window);window.MinWidth=Math.Min(window.MinWidth,area.Width);window.MinHeight=Math.Min(window.MinHeight,area.Height);window.Width=Math.Min(window.Width,area.Width);window.Height=Math.Min(window.Height,area.Height);}
}
