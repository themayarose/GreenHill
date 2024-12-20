using System.Drawing;
using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;

using Windows.Graphics;
using Point = Windows.Foundation.Point;

using WinRT.Interop;

namespace GreenHill.Helpers;

public static class RectInt32Extensions {
    public static RectInt32 Scale(this RectInt32 self, double factor) {
        return new RectInt32() {
            X = (int) (self.X * factor),
            Y = (int) (self.Y * factor),
            Width = (int) (self.Width * factor),
            Height = (int) (self.Height * factor)
        };
    }

    public static RectInt32 Intersect(this RectInt32 self, RectInt32 other) {
        var thisRect = new Rectangle(self.X, self.Y, self.Width, self.Height);
        var otherRect = new Rectangle(other.X, other.Y, other.Width, other.Height);

        thisRect.Intersect(otherRect);

        return new RectInt32() {
            X = thisRect.X,
            Y = thisRect.Y,
            Width = thisRect.Width,
            Height = thisRect.Height
        };
    }

    public static IEnumerable<RectInt32> Subtract(this RectInt32 minuend, RectInt32 subtrahend) {
        subtrahend = subtrahend.Intersect(minuend);

        if (subtrahend.Width == 0 || subtrahend.Height == 0) {
            yield return minuend;
            yield break;
        }

        var heightA = subtrahend.Y - minuend.Y;
        if (heightA > 0) yield return new RectInt32(
            minuend.X,
            minuend.Y,
            minuend.Width,
            heightA
        );

        var widthB = subtrahend.X - minuend.X;
        if (widthB > 0) yield return new RectInt32(
            minuend.X,
            subtrahend.Y,
            widthB,
            subtrahend.Height
        );

        var widthC = minuend.X + minuend.Width - subtrahend.X - subtrahend.Width;
        if (widthC > 0) yield return new RectInt32(
            subtrahend.X + subtrahend.Width,
            subtrahend.Y,
            widthC,
            subtrahend.Height
        );

        var heightD = minuend.Y + minuend.Height - subtrahend.Y - subtrahend.Height;
        if (heightD > 0) yield return new RectInt32(
            minuend.X,
            subtrahend.Y + subtrahend.Height,
            minuend.Width,
            heightD
        );
    }

    public static IEnumerable<RectInt32> Subtract(this IEnumerable<RectInt32> minuends, RectInt32 subtrahend) {
        foreach (var m in minuends) {
            foreach (var r in m.Subtract(subtrahend)) {
                yield return r;
            }
        }
    }
}

public static class WindowExtensions {
    public static RectInt32 GetControlAsRectInt32(this Window window, FrameworkElement control) {
        var startPoint = control
            .TransformToVisual(window.Content)
            .TransformPoint(new Point(0, 0));

        return new RectInt32() {
            X = (int) startPoint.X,
            Y = (int) startPoint.Y,
            Width = (int) control.ActualWidth,
            Height = (int) control.ActualHeight
        };
    }

    public static void SetDragRectangles(this Window self, FrameworkElement appTitleBar, params FrameworkElement[] controls) {
        var window = self.GetAppWindowForCurrentWindow();

        if (!AppWindowTitleBar.IsCustomizationSupported() || !window.TitleBar.ExtendsContentIntoTitleBar) return;

        var scale = self.GetScaleAdjustment();

        var subtrahends = controls.Select(c => self.GetControlAsRectInt32(c));
        var minuend = self.GetControlAsRectInt32(appTitleBar);
        IEnumerable<RectInt32> rects = [minuend];

        foreach (var s in subtrahends) {
            rects = rects.Subtract(s);
        }

        rects = rects.Select(r => r.Scale(scale));

        window.TitleBar.SetDragRectangles(rects.ToArray());
    }

    public static AppWindow GetAppWindowForCurrentWindow(this Window self) {
        IntPtr hWnd = WindowNative.GetWindowHandle(self);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);

        return AppWindow.GetFromWindowId(wndId);
    }

    [DllImport("Shcore.dll", SetLastError = true)]
    internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    internal enum Monitor_DPI_Type : int {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    public static double GetScaleAdjustment(this Window self) {
        IntPtr hWnd = WindowNative.GetWindowHandle(self);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);

        if (result != 0) {
            throw new Exception("Could not get DPI for monitor.");
        }

        uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
        return scaleFactorPercent / 100.0;
    }

}
