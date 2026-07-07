using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BossKey.Components
{
    /// <summary>
    /// 快捷键释放前事件参数。当 Cancel 为 true 且 Message 不为 null 时，组件将向用户显示 Message 中的提示信息。
    /// </summary>
    public class BeforeKeyReleaseEventArgs : CancelEventArgs
    {
        /// <summary>
        /// 提示信息。当 Cancel 为 true 时，组件将显示此消息。
        /// </summary>
        public string? Message { get; set; }
    }

    [DefaultEvent("HotkeyChanged")]
    public partial class HotkeyBox : UserControl
    {
        // ---- 内部字段 ----
        private ModifierKey _modifierKeys;
        private Keys _key;
        private bool _exactlyModifier;
        private ModifierKey _pressedModifiers;        // 当前按下的修饰键
        private ModifierKey _snapshotModifiers;        // 非修饰键按下时的修饰键快照
        private bool _hasSnapshot;                      // 是否有有效的快照
        private bool _updatingDisplay;                   // 防止 TextChanged 重入

        // ---- 构造与事件绑定 ----

        public HotkeyBox()
        {
            InitializeComponent();
            SetStyle(ControlStyles.FixedHeight, true);
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            textBox.KeyDown += TextBox_KeyDown;
            textBox.KeyUp += TextBox_KeyUp;
            textBox.KeyPress += TextBox_KeyPress;
            textBox.TextChanged += TextBox_TextChanged;
            textBox.MouseUp += TextBox_MouseUp;
            textBox.Enter += TextBox_Enter;
            textBox.Leave += TextBox_Leave;
        }

        /// <summary>
        /// 仿照 TextBoxBase 做法：AutoSize 时强制使用 PreferredHeight，设计器仅显示左右锚点。
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            height = textBox.PreferredHeight;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        // ---- 公开事件 ----

        /// <summary>
        /// 在非修饰键释放前触发。设置 Cancel = true 可阻止快捷键录入，若同时设置 Message 组件将弹出提示。
        /// </summary>
        [Category("快捷键")]
        [Description("在非修饰键释放前触发，可取消录入并显示自定义提示。")]
        public event EventHandler<BeforeKeyReleaseEventArgs>? BeforeKeyRelease;

        /// <summary>
        /// 当快捷键（ModifierKeys 或 BaseKey）发生变化时触发。
        /// </summary>
        [Category("快捷键")]
        [Description("当快捷键（修饰键或基础按键）发生变化时触发。")]
        public event EventHandler? HotkeyChanged;

        /// <summary>
        /// 触发 <see cref="HotkeyChanged"/> 事件。
        /// </summary>
        protected virtual void OnHotkeyChanged(EventArgs e)
        {
            HotkeyChanged?.Invoke(this, e);
        }

        // ---- 公开属性 ----

        /// <summary>
        /// 是否精确区分左右修饰键（如 左Ctrl 与 右Ctrl）。
        /// </summary>
        [Category("快捷键")]
        [Description("是否精确区分左右修饰键。设为 true 时区分左右 Ctrl/Alt/Shift/Win。")]
        [DefaultValue(false)]
        public bool ExactlyModifier
        {
            get => _exactlyModifier;
            set
            {
                if (_exactlyModifier != value)
                {
                    _exactlyModifier = value;
                    UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// 是否允许单独按下退格键清空当前快捷键。
        /// </summary>
        [Category("快捷键")]
        [Description("设为 true 时，在输入框中单独按下退格键将清空快捷键。")]
        [DefaultValue(true)]
        public bool AllowBackspace { get; set; } = true;

        /// <summary>
        /// 当前快捷键的修饰键集合（设计器中隐藏，请使用 Hotkey 属性）。
        /// </summary>
        [Category("快捷键")]
        [Description("当前快捷键的修饰键。")]
        [DefaultValue(ModifierKey.None)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ModifierKey ModifierKeys
        {
            get => _modifierKeys;
            set
            {
                if (_modifierKeys == value)
                    return;

                _modifierKeys = value;
                UpdateDisplay();
                OnHotkeyChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// 当前快捷键的基础按键（设计器中隐藏，请使用 Hotkey 属性）。
        /// </summary>
        [Category("快捷键")]
        [Description("当前快捷键的基础按键。")]
        [DefaultValue(Keys.None)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Keys BaseKey
        {
            get => _key;
            set
            {
                if (_key == value)
                    return;

                _key = value;
                UpdateDisplay();
                OnHotkeyChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// 快捷键组合。可在设计器中直接选择如 Ctrl+A，自动拆分为修饰键和基础按键。
        /// </summary>
        [Category("快捷键")]
        [Description("快捷键组合。可直接选择带修饰键的组合如 Ctrl+A。")]
        [DefaultValue(Keys.None)]
        public Keys Hotkey
        {
            get
            {
                Keys result = _key;
                if ((_modifierKeys & (ModifierKey.LControl | ModifierKey.RControl)) != 0)
                    result |= Keys.Control;
                if ((_modifierKeys & (ModifierKey.LShift | ModifierKey.RShift)) != 0)
                    result |= Keys.Shift;
                if ((_modifierKeys & (ModifierKey.LAlt | ModifierKey.RAlt)) != 0)
                    result |= Keys.Alt;
                return result;
            }
            set
            {
                var oldModifiers = _modifierKeys;
                var oldKey = _key;

                _modifierKeys = ModifierKey.None;
                _key = Keys.None;

                if ((value & Keys.Control) != 0) _modifierKeys |= ModifierKey.LControl;
                if ((value & Keys.Shift) != 0) _modifierKeys |= ModifierKey.LShift;
                if ((value & Keys.Alt) != 0) _modifierKeys |= ModifierKey.LAlt;

                // 去除修饰键标志后即为基础按键
                _key = value & ~(Keys.Control | Keys.Shift | Keys.Alt);
                UpdateDisplay();

                if (_modifierKeys != oldModifiers || _key != oldKey)
                    OnHotkeyChanged(EventArgs.Empty);
            }
        }

        // ---- TextBox 样式同步 ----

        /// <summary>
        /// 同步字体到内部文本框。
        /// </summary>
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            textBox.Font = Font;
        }

        /// <summary>
        /// 同步前景色到内部文本框。
        /// </summary>
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            textBox.ForeColor = ForeColor;
        }

        /// <summary>
        /// 同步背景色到内部文本框。
        /// </summary>
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            textBox.BackColor = BackColor;
        }

        /// <summary>
        /// 文本对齐方式。
        /// </summary>
        [Category("外观")]
        [Description("文本框的文本对齐方式。")]
        [DefaultValue(HorizontalAlignment.Center)]
        public HorizontalAlignment TextAlign
        {
            get => textBox.TextAlign;
            set => textBox.TextAlign = value;
        }

        // ---- 重置方法 ----

        /// <summary>
        /// 重置文本对齐方式为默认值。
        /// </summary>
        public void ResetTextAlign() => textBox.TextAlign = HorizontalAlignment.Center;

        private bool ShouldSerializeTextAlign() => textBox.TextAlign != HorizontalAlignment.Center;

        // ======================== 事件处理 ========================

        private void TextBox_Enter(object? sender, EventArgs e)
        {
            // 获得焦点时重置跟踪状态
            _pressedModifiers = ModifierKey.None;
            _hasSnapshot = false;
            UpdateDisplay();
        }

        private void TextBox_Leave(object? sender, EventArgs e)
        {
            // 失去焦点时提交当前状态
            _pressedModifiers = ModifierKey.None;
            _hasSnapshot = false;
        }

        private void TextBox_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            // 阻止 Tab / 方向键等导致焦点切换，确保所有按键都能被 KeyDown / KeyUp 捕获
            switch (e.KeyCode)
            {
                case Keys.Tab:
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.End:
                case Keys.Home:
                case Keys.PageUp:
                case Keys.PageDown:
                    e.IsInputKey = true;
                    break;
            }
        }

        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // 阻止字符输入，我们完全自行管理显示文本
            e.Handled = true;
        }

        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            // 拦截外部文本更改（如右键粘贴），立即恢复正确显示
            if (!_updatingDisplay)
                UpdateDisplay();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // 处理修饰键按下
            ModifierKey mod = KeyCodeToModifier(e.KeyCode);
            if (mod != ModifierKey.None)
            {
                // 开始录入新快捷键时清除旧的主要按键
                if (_pressedModifiers == ModifierKey.None)
                    _key = Keys.None;

                // 加入当前按下集合
                if (!_exactlyModifier)
                {
                    mod = NormalizeModifier(mod);
                    // 移除另一侧（已规范化的相同键）
                    _pressedModifiers &= ~mod;
                }
                _pressedModifiers |= mod;
                _hasSnapshot = false;
                UpdateDisplay();
                e.SuppressKeyPress = true;
            }
            else
            {
                // 非修饰键按下 → 快照当前修饰键状态
                _snapshotModifiers = _pressedModifiers;
                _hasSnapshot = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_KeyUp(object? sender, KeyEventArgs e)
        {
            ModifierKey mod = KeyCodeToModifier(e.KeyCode);
            if (mod != ModifierKey.None)
            {
                // 修饰键释放 → 从当前集合移除
                if (!_exactlyModifier)
                {
                    mod = NormalizeModifier(mod);
                }
                _pressedModifiers &= ~mod;
                UpdateDisplay();
                return;
            }

            // AllowBackspace：无修饰键时单独按退格清空快捷键
            if (AllowBackspace && e.KeyCode == Keys.Back && _pressedModifiers == ModifierKey.None)
            {
                bool hadValue = _modifierKeys != ModifierKey.None || _key != Keys.None;
                _modifierKeys = ModifierKey.None;
                _key = Keys.None;
                _pressedModifiers = ModifierKey.None;
                _hasSnapshot = false;
                UpdateDisplay();

                if (hadValue)
                    OnHotkeyChanged(EventArgs.Empty);

                return;
            }

            // 非修饰键释放 → 尝试提交快捷键
            CommitKey(e.KeyCode);
        }

        private void TextBox_MouseUp(object? sender, MouseEventArgs e)
        {
            // 将鼠标按键映射为 Keys 并在当前修饰键下提交
            Keys mouseKey = MouseButtonToKeys(e.Button);
            if (mouseKey != Keys.None)
            {
                // 使用当前已跟踪的修饰键状态
                _snapshotModifiers = _pressedModifiers;
                _hasSnapshot = true;
                CommitKey(mouseKey);
            }
        }

        // ======================== 核心逻辑 ========================

        /// <summary>
        /// 提交主要按键。
        /// </summary>
        private void CommitKey(Keys key)
        {
            ModifierKey mods = _hasSnapshot ? _snapshotModifiers : _pressedModifiers;

            // 始终触发 BeforeKeyRelease 事件
            var args = new BeforeKeyReleaseEventArgs();
            BeforeKeyRelease?.Invoke(this, args);

            // 事件被取消且提供了消息 → 弹出提示
            if (args.Cancel && !string.IsNullOrEmpty(args.Message))
            {
                MessageBox.Show(args.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _hasSnapshot = false;
                return;
            }

            // 事件被取消（无消息）→ 静默放弃
            if (args.Cancel)
            {
                _hasSnapshot = false;
                return;
            }

            // 必须包含修饰键，否则静默忽略
            if (mods == ModifierKey.None)
            {
                _hasSnapshot = false;
                return;
            }

            var oldModifiers = _modifierKeys;
            var oldKey = _key;

            _modifierKeys = mods;
            _key = key;
            _hasSnapshot = false;
            _pressedModifiers = ModifierKey.None;
            UpdateDisplay();

            if (_modifierKeys != oldModifiers || _key != oldKey)
                OnHotkeyChanged(EventArgs.Empty);
        }

        /// <summary>
        /// 更新文本框的显示内容。
        /// </summary>
        private void UpdateDisplay()
        {
            var sb = new StringBuilder();

            // 正在录入 → 显示临时状态；已确认且有 Key → 显示确认值；Key 为空 → 不显示任何内容
            bool isEntering = _pressedModifiers != ModifierKey.None || _hasSnapshot;

            if (!isEntering && _key == Keys.None)
            {
                // Key 为空时不显示任何修饰键
            }
            else
            {
                ModifierKey displayMods = isEntering ? _pressedModifiers : _modifierKeys;

                AppendModifier(sb, displayMods, ModifierKey.LControl, ModifierKey.RControl, "Ctrl");
                AppendModifier(sb, displayMods, ModifierKey.LShift, ModifierKey.RShift, "Shift");
                AppendModifier(sb, displayMods, ModifierKey.LAlt, ModifierKey.RAlt, "Alt");
                AppendModifier(sb, displayMods, ModifierKey.LWindows, ModifierKey.RWindows, "Win");

                if (!isEntering && _key != Keys.None)
                {
                    sb.Append(GetKeyDisplayName(_key));
                }
            }

            _updatingDisplay = true;
            textBox.Text = sb.ToString();
            _updatingDisplay = false;
        }

        private void AppendModifier(StringBuilder sb, ModifierKey mods,
            ModifierKey left, ModifierKey right, string baseName)
        {
            if (_exactlyModifier)
            {
                if ((mods & left) != 0)
                    sb.Append("左").Append(baseName).Append('+');
                if ((mods & right) != 0)
                    sb.Append("右").Append(baseName).Append('+');
            }
            else
            {
                if ((mods & (left | right)) != 0)
                    sb.Append(baseName).Append('+');
            }
        }

        // ======================== 按键映射 ========================

        /// <summary>
        /// 将 Keys 转换为对应的 ModifierKey，利用 WndProc 捕获的扩展标志位和扫描码区分左右。
        /// </summary>
        private ModifierKey KeyCodeToModifier(Keys key)
        {
            bool isExtended = textBox.IsExtendedKey;
            int scanCode = textBox.ScanCode;

            return key switch
            {
                Keys.LControlKey => ModifierKey.LControl,
                Keys.RControlKey => ModifierKey.RControl,
                Keys.ControlKey  => isExtended ? ModifierKey.RControl : ModifierKey.LControl,
                Keys.LShiftKey   => ModifierKey.LShift,
                Keys.RShiftKey   => ModifierKey.RShift,
                Keys.ShiftKey    => scanCode == 0x36 ? ModifierKey.RShift : ModifierKey.LShift,
                Keys.LMenu       => ModifierKey.LAlt,
                Keys.RMenu       => ModifierKey.RAlt,
                Keys.Menu        => isExtended ? ModifierKey.RAlt : ModifierKey.LAlt,
                Keys.LWin        => ModifierKey.LWindows,
                Keys.RWin        => ModifierKey.RWindows,
                _                => ModifierKey.None,
            };
        }

        /// <summary>
        /// 将修饰键规范化为左侧（用于 ExactlyModifier = false 时）。
        /// </summary>
        private static ModifierKey NormalizeModifier(ModifierKey mod)
        {
            return mod switch
            {
                ModifierKey.RControl  => ModifierKey.LControl,
                ModifierKey.RShift    => ModifierKey.LShift,
                ModifierKey.RAlt      => ModifierKey.LAlt,
                ModifierKey.RWindows  => ModifierKey.LWindows,
                _                     => mod,
            };
        }

        /// <summary>
        /// 将鼠标按键映射为 Keys 枚举值。
        /// </summary>
        private static Keys MouseButtonToKeys(MouseButtons button)
        {
            return button switch
            {
                MouseButtons.Left   => Keys.LButton,
                MouseButtons.Right  => Keys.RButton,
                MouseButtons.Middle => Keys.MButton,
                MouseButtons.XButton1 => Keys.XButton1,
                MouseButtons.XButton2 => Keys.XButton2,
                _ => Keys.None,
            };
        }

        // ======================== 中文按键名称映射 ========================

        /// <summary>
        /// 获取按键的中文显示名称。
        /// </summary>
        private static string GetKeyDisplayName(Keys key)
        {
            if (_keyNameMap.TryGetValue(key, out string? name))
                return name;

            // D0-D9 → "0"-"9"
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((char)('0' + (key - Keys.D0))).ToString();

            // NumPad0-NumPad9 → "数字键盘0"-"数字键盘9"
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return "数字键盘" + (key - Keys.NumPad0).ToString();

            // A-Z → "A"-"Z"
            if (key >= Keys.A && key <= Keys.Z)
                return key.ToString();

            // Oem 键尝试转换
            string? oemName = GetOemKeyName(key);
            if (oemName != null)
                return oemName;

            return key.ToString();
        }

        private static readonly Dictionary<Keys, string> _keyNameMap = new()
        {
            // 功能键
            { Keys.Back,        "退格" },
            { Keys.Tab,         "Tab" },
            { Keys.Return,      "回车" },
            { Keys.Escape,      "Esc" },
            { Keys.Space,       "空格" },
            { Keys.PageUp,      "PageUp" },
            { Keys.PageDown,    "PageDown" },
            { Keys.End,         "End" },
            { Keys.Home,        "Home" },
            { Keys.Left,        "左方向键" },
            { Keys.Up,          "上方向键" },
            { Keys.Right,       "右方向键" },
            { Keys.Down,        "下方向键" },
            { Keys.Insert,      "插入" },
            { Keys.Delete,      "删除" },
            { Keys.CapsLock,    "大写锁定" },
            { Keys.NumLock,     "数字锁定" },
            { Keys.Scroll,      "滚动锁定" },
            { Keys.PrintScreen, "打印屏幕" },
            { Keys.Pause,       "暂停" },
            { Keys.Apps,        "菜单键" },
            { Keys.Sleep,       "休眠" },

            // 鼠标按键
            { Keys.LButton,     "鼠标左键" },
            { Keys.RButton,     "鼠标右键" },
            { Keys.MButton,     "鼠标中键" },
            { Keys.XButton1,    "鼠标侧键1" },
            { Keys.XButton2,    "鼠标侧键2" },

            // Multiply / Add / Separator / Subtract / Decimal / Divide（数字键盘）
            { Keys.Multiply,    "数字键盘*" },
            { Keys.Add,         "数字键盘+" },
            { Keys.Separator,   "数字键盘分隔" },
            { Keys.Subtract,    "数字键盘-" },
            { Keys.Decimal,     "数字键盘." },
            { Keys.Divide,      "数字键盘/" },

            // Oem 按键（注意：Oem1-Oem8 与具体 OemXxx 值相同，此处使用具体名称）
            { Keys.OemSemicolon,    ";" },
            { Keys.Oemplus,         "=" },
            { Keys.Oemcomma,        "," },
            { Keys.OemMinus,        "-" },
            { Keys.OemPeriod,       "." },
            { Keys.OemQuestion,     "/" },
            { Keys.Oemtilde,        "`" },
            { Keys.OemOpenBrackets, "[" },
            { Keys.OemCloseBrackets,"]" },
            { Keys.OemPipe,         "\\" },
            { Keys.OemQuotes,       "'" },
            { Keys.OemBackslash,    "\\" },

            // F1-F24
            { Keys.F1,  "F1" },  { Keys.F2,  "F2" },  { Keys.F3,  "F3" },  { Keys.F4,  "F4" },
            { Keys.F5,  "F5" },  { Keys.F6,  "F6" },  { Keys.F7,  "F7" },  { Keys.F8,  "F8" },
            { Keys.F9,  "F9" },  { Keys.F10, "F10" }, { Keys.F11, "F11" }, { Keys.F12, "F12" },
            { Keys.F13, "F13" }, { Keys.F14, "F14" }, { Keys.F15, "F15" }, { Keys.F16, "F16" },
            { Keys.F17, "F17" }, { Keys.F18, "F18" }, { Keys.F19, "F19" }, { Keys.F20, "F20" },
            { Keys.F21, "F21" }, { Keys.F22, "F22" }, { Keys.F23, "F23" }, { Keys.F24, "F24" },
        };

        private static string? GetOemKeyName(Keys key)
        {
            return key switch
            {
                Keys.OemSemicolon     => ";",
                Keys.Oemplus          => "=",
                Keys.Oemcomma         => ",",
                Keys.OemMinus         => "-",
                Keys.OemPeriod        => ".",
                Keys.OemQuestion      => "/",
                Keys.Oemtilde         => "`",
                Keys.OemOpenBrackets  => "[",
                Keys.OemCloseBrackets => "]",
                Keys.OemPipe          => "\\",
                Keys.OemQuotes        => "'",
                Keys.OemBackslash     => "\\",
                _                     => null,
            };
        }
    }

    /// <summary>
    /// 内部文本框，重写 WndProc 以捕获按键的扩展标志位和扫描码，用于区分左右修饰键。
    /// </summary>
    internal class HotkeyTextBox : TextBox
    {
        /// <summary>最近一次 WM_KEYDOWN 的扩展键标志位（bit 24 of lParam）。</summary>
        public bool IsExtendedKey { get; private set; }
        /// <summary>最近一次 WM_KEYDOWN 的硬件扫描码。</summary>
        public int ScanCode { get; private set; }

        protected override void WndProc(ref Message m)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;

            if (m.Msg is WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                IsExtendedKey = ((int)m.LParam & 0x01000000) != 0;
                ScanCode = ((int)m.LParam >> 16) & 0xFF;
            }
            base.WndProc(ref m);
        }
    }
}
