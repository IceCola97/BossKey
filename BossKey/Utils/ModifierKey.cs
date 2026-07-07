using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Utils
{
    [Flags]
    public enum ModifierKey : int
    {
        None = 0,
        LControl = 1,
        LShift = 2,
        LAlt = 4,
        LWindows = 8,
        RControl = 16,
        RShift = 32,
        RAlt = 64,
        RWindows = 128,
    }
}
