using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BossKey.Models
{
    [DebuggerDisplay("Handle = {Handle}, Title = {Title}, ProcessId = {ProcessId}")]
    internal sealed class ScannedWindow : IEquatable<ScannedWindow>, IComparable<ScannedWindow>
    {
        public nint Handle { get; internal set; }

        public string? Title { get; internal set; }

        public int ProcessId { get; internal set; }

        public int CompareTo(ScannedWindow? other)
        {
            if (other == null) return 1;
            if (Equals(other)) return 0;
            if (other.Title == null) return 1;
            if (Title == null) return -1;

            int result = string.Compare(Title, other.Title, StringComparison.CurrentCultureIgnoreCase);

            if (result == 0)
                result = Handle.CompareTo(other.Handle);

            return result;
        }

        public override bool Equals(object? obj)
        {
            return obj is ScannedWindow window &&
                   Handle.Equals(window.Handle);
        }

        public bool Equals(ScannedWindow? other)
        {
            return other != null && Handle.Equals(other.Handle);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Handle);
        }

        public static bool operator ==(ScannedWindow? left, ScannedWindow? right)
        {
            return EqualityComparer<ScannedWindow>.Default.Equals(left, right);
        }

        public static bool operator !=(ScannedWindow? left, ScannedWindow? right)
        {
            return !(left == right);
        }
    }
}
