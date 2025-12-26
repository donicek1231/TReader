using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace TReader.Services
{
    public static class WindowHelper
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        // 使用 GetWindowLongPtr 以支持 x64
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public const int VK_CONTROL = 0x11;

        public static void HideFromAltTab(Window window)
        {
            var handle = window.TryGetPlatformHandle()?.Handle;
            if (handle.HasValue)
            {
                IntPtr exStyle = GetWindowLongPtr(handle.Value, GWL_EXSTYLE);
                SetWindowLongPtr(handle.Value, GWL_EXSTYLE, (IntPtr)(exStyle.ToInt64() | WS_EX_TOOLWINDOW));
            }
        }
    }
}
