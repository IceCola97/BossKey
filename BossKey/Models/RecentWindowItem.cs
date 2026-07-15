using BossKey.Utils;
using System.Diagnostics;

namespace BossKey.Models
{
    internal sealed class RecentWindowItem(ScannedWindow window)
    {
        private readonly ScannedWindow _window = window;

        public string Title => GetWindowTitle(_window);

        public nint Handle => _window.Handle;

        public ScannedWindow Window => _window;

        private static string GetWindowTitle(ScannedWindow window)
        {
            return window.Title ?? $"窗口 #{window.Handle:X08}";
        }

        public override bool Equals(object? obj)
        {
            return obj is RecentWindowItem item &&
                   EqualityComparer<ScannedWindow>.Default.Equals(_window, item._window);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_window);
        }
    }
}
