using BossKey.Models;
using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using static BossKey.Models.WindowsAPI;

namespace BossKey.Components
{
    public class HotkeyFormBase : Form
    {
        private IHotkeyManager? _hotkeyManager;

        internal void BindHotkeyManager(IHotkeyManager hotkeyManager)
        {
            ArgumentNullException.ThrowIfNull(hotkeyManager);
            _hotkeyManager = hotkeyManager;
        }

        protected override void WndProc(ref Message m)
        {
            var hotkeyManager = _hotkeyManager;

            if (hotkeyManager != null
                && m.Msg == (int)WindowMessage.HotKey)
            {
                int id = m.WParam.ToInt32();
                int hotkeyValue = m.LParam.ToInt32();
                int modifiers = hotkeyValue & 0xFFFF;
                int vkey = hotkeyValue >> 16;

                var hotkey = new Hotkey(
                    ModifierKeyToApplication(modifiers),
                    VirtualKeyToApplication(vkey)
                );

                hotkeyManager.DispatchHotkey(id, hotkey);
            }

            base.WndProc(ref m);
        }
    }
}
