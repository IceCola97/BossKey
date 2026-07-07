using BossKey.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Utils
{
    public readonly struct Hotkey(ModifierKey modifierKey, Keys baseKey)
    {
        public static Hotkey None { get; } = new(ModifierKey.None, Keys.None);

        public ModifierKey ModifierKey { get; } = modifierKey;

        public Keys BaseKey { get; } = baseKey;

        /// <summary>
        /// 将所有右修饰键归为左修饰键
        /// </summary>
        /// <returns></returns>
        public Hotkey NormalizeLeft()
        {
            var normalizedModifierKey = ModifierKey;

            if (normalizedModifierKey.HasFlag(ModifierKey.RAlt))
            {
                normalizedModifierKey &= ~ModifierKey.RAlt;
                normalizedModifierKey |= ModifierKey.LAlt;
            }

            if (normalizedModifierKey.HasFlag(ModifierKey.RControl))
            {
                normalizedModifierKey &= ~ModifierKey.RControl;
                normalizedModifierKey |= ModifierKey.LControl;
            }

            if (normalizedModifierKey.HasFlag(ModifierKey.RShift))
            {
                normalizedModifierKey &= ~ModifierKey.RShift;
                normalizedModifierKey |= ModifierKey.LShift;
            }

            if (normalizedModifierKey.HasFlag(ModifierKey.RWindows))
            {
                normalizedModifierKey &= ~ModifierKey.RWindows;
                normalizedModifierKey |= ModifierKey.LWindows;
            }

            return new Hotkey(normalizedModifierKey, BaseKey);
        }

        public override bool Equals(object? obj)
        {
            return obj is Hotkey hotkey &&
                   ModifierKey == hotkey.ModifierKey &&
                   BaseKey == hotkey.BaseKey;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModifierKey, BaseKey);
        }

        public static bool operator ==(Hotkey left, Hotkey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Hotkey left, Hotkey right)
        {
            return !(left == right);
        }

        public void Deconstruct(out ModifierKey modifierKey, out Keys baseKey)
        {
            modifierKey = ModifierKey;
            baseKey = BaseKey;
        }
    }
}
