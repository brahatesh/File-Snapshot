using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

namespace FileSnapshotUI.Helpers;

/// <summary>
/// A helper class used to enforce minimum and maximum window size constraints 
/// by hooking into the Win32 window procedure.
/// </summary>
/// <remarks>
/// This implementation was adapted from the WinUI-Gallery repository:
/// <list type="bullet">
/// <item><description><a href="https://github.com/microsoft/WinUI-Gallery/blob/be0c565cd3a4b9ba40375622904297e1548875fc/WinUIGallery/Helpers/Win32WindowHelper.cs">Win32WindowHelper.cs</a></description></item>
/// <item><description><a href="https://github.com/microsoft/WinUI-Gallery/blob/be0c565cd3a4b9ba40375622904297e1548875fc/WinUIGallery/Helpers/Win32.cs">Win32.cs</a></description></item>
/// </list>
/// </remarks>
internal class WindowHelper(Window window) {
    private POINT? minWindowSize = null;
    private POINT? maxWindowSize = null;
    private readonly Window window = window;
    private static WinProc? newWndProc = null;
    private static nint oldWndProc = nint.Zero;

    internal delegate IntPtr WinProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

    internal enum WindowMessage : int {
        WM_GETMINMAXINFO = 0x0024
    }

    internal enum WindowLongIndexFlags : int {
        GWL_WNDPROC = -4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    internal struct POINT {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    internal static extern int GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

    private static nint GetWindowHandleForCurrentWindow(object target) => WinRT.Interop.WindowNative.GetWindowHandle(target);

    /// <summary>
    /// Configures the minimum and maximum trackable size for the window.
    /// </summary>
    /// <param name="minWindowSize">Optional minimum size (<see cref="POINT"/>) for the window.</param>
    /// <param name="maxWindowSize">Optional maximum size (<see cref="POINT"/>) for the window.</param>
    public void SetWindowMinMaxSize(POINT? minWindowSize = null, POINT? maxWindowSize = null) {
        this.minWindowSize = minWindowSize;
        this.maxWindowSize = maxWindowSize;

        var hwnd = GetWindowHandleForCurrentWindow(window);

        newWndProc = new WinProc(WndProc);
        oldWndProc = SetWindowLongPtr64(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
    }

    private nint WndProc(nint hWnd, WindowMessage Msg, nint wParam, IntPtr lParam) {
        switch (Msg) {
            case WindowMessage.WM_GETMINMAXINFO:
                var dpi = GetDpiForWindow(hWnd);
                var scalingFactor = (float)dpi / 96;

                var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                if (minWindowSize != null) {
                    minMaxInfo.ptMinTrackSize.x = (int)(minWindowSize.Value.x * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                }
                if (maxWindowSize != null) {
                    minMaxInfo.ptMaxTrackSize.x = (int)(maxWindowSize.Value.x * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.y = (int)(maxWindowSize.Value.y * scalingFactor);
                }

                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;
        }
        return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
    }
}