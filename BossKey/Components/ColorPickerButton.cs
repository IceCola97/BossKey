using BossKey.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BossKey.Components
{
    /// <summary>
    /// 颜色选择按钮组件。左侧显示当前颜色的色块，右侧提供"选择颜色"和"屏幕取色"两个按钮。
    /// </summary>
    [DefaultEvent("SelectedColorChanged")]
    [DefaultProperty("SelectedColor")]
    public partial class ColorPickerButton : UserControl
    {
        private const int BUTTON_WIDTH = 20;
        private const int FIXED_HEIGHT = 28;
        private const int MIN_WIDTH = 90;

        private Color _selectedColor = Color.Black;
        private Panel _swatchPanel = null!;
        private Button _btnDialog = null!;
        private Button _btnPick = null!;
        private ToolTip _toolTip = null!;
        private bool _showCancelOverlay;

        public ColorPickerButton()
        {
            InitializeComponent();
            _toolTip = new ToolTip();
            _toolTip.SetToolTip(_btnDialog, "选择颜色...");
            _toolTip.SetToolTip(_btnPick, "从屏幕取色");
            UpdateSwatch();
        }

        /// <summary>
        /// 限制高度固定、设置最小宽度，防止设计器拖出畸形尺寸。
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            height = FIXED_HEIGHT;
            if (width < MIN_WIDTH)
                width = MIN_WIDTH;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        /// <summary>
        /// 控件启用/禁用时同步子控件状态并刷新外观。
        /// </summary>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _btnDialog.Enabled = Enabled;
            _btnPick.Enabled = Enabled;
            _swatchPanel.Invalidate();
        }

        /// <summary>
        /// 控件尺寸变化时重新布局子控件：按钮靠右，色块填充剩余空间。
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_swatchPanel != null && _btnDialog != null && _btnPick != null)
            {
                LayoutChildren();
            }
        }

        private void LayoutChildren()
        {
            int h = Height;
            int twoBtn = BUTTON_WIDTH * 2;

            _btnPick.Location = new Point(Width - BUTTON_WIDTH, 0);
            _btnPick.Size = new Size(BUTTON_WIDTH, h);

            _btnDialog.Location = new Point(Width - twoBtn, 0);
            _btnDialog.Size = new Size(BUTTON_WIDTH, h);

            _swatchPanel.Size = new Size(Width - twoBtn, h);
        }

        /// <summary>
        /// 当前选中的颜色。
        /// </summary>
        [Category("外观")]
        [Description("当前选中的颜色。")]
        [DefaultValue(typeof(Color), "Black")]
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    UpdateSwatch();
                    OnSelectedColorChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 当选中的颜色发生变化时触发。
        /// </summary>
        [Category("行为")]
        [Description("当选中的颜色发生变化时触发。")]
        public event EventHandler? SelectedColorChanged;

        /// <summary>
        /// 触发 <see cref="SelectedColorChanged"/> 事件。
        /// </summary>
        protected virtual void OnSelectedColorChanged(EventArgs e)
        {
            SelectedColorChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 将当前颜色转换为 COLORREF 格式 (0x00BBGGRR)。
        /// </summary>
        public int ToCOLORREF()
        {
            return _selectedColor.R | (_selectedColor.G << 8) | (_selectedColor.B << 16);
        }

        /// <summary>
        /// 从 COLORREF 格式 (0x00BBGGRR) 转换为 Color。
        /// </summary>
        public static Color FromCOLORREF(int colorRef)
        {
            return Color.FromArgb(
                colorRef & 0xFF,
                (colorRef >> 8) & 0xFF,
                (colorRef >> 16) & 0xFF);
        }

        private void UpdateSwatch()
        {
            _swatchPanel.Invalidate();
        }

        private void SwatchPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = _swatchPanel.ClientRectangle;

            if (Enabled)
            {
                // 正常状态：填充选中颜色 + 标准边框
                using var fillBrush = new SolidBrush(_selectedColor);
                g.FillRectangle(fillBrush, rect);
                ControlPaint.DrawBorder(g, rect, SystemColors.WindowFrame, ButtonBorderStyle.Solid);
            }
            else
            {
                // 禁用状态：灰色"透明"填充 + 灰色边框
                using var fillBrush = new SolidBrush(SystemColors.Control);
                g.FillRectangle(fillBrush, rect);
                ControlPaint.DrawBorder(g, rect, SystemColors.InactiveBorder, ButtonBorderStyle.Solid);
            }

            // 取色取消半透明遮罩
            if (_showCancelOverlay)
            {
                using var overlayBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
                g.FillRectangle(overlayBrush, rect);
                TextRenderer.DrawText(g, "取消取色", Font, rect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void SwatchPanel_Click(object? sender, EventArgs e)
        {
            ShowColorDialog();
        }

        private void BtnDialog_Click(object? sender, EventArgs e)
        {
            ShowColorDialog();
        }

        private void BtnPick_Click(object? sender, EventArgs e)
        {
            // 延迟执行，避免当前按钮的点击消息干扰覆盖窗
            BeginInvoke(new Action(BeginScreenPick));
        }

        private void ShowColorDialog()
        {
            using var dialog = new ColorDialog
            {
                Color = _selectedColor,
                FullOpen = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedColor = dialog.Color;
            }
        }

        private void BeginScreenPick()
        {
            var originalColor = _selectedColor;
            var ctrlRect = RectangleToScreen(ClientRectangle);

            using var overlay = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                TopMost = true,
                ShowInTaskbar = false,
                Cursor = Cursors.Cross,
                Opacity = 0.005,
                BackColor = Color.Black
            };

            overlay.MouseMove += (s, e) =>
            {
                var pos = Cursor.Position;

                if (ctrlRect.Contains(pos))
                {
                    overlay.Cursor = Cursors.Hand;
                    if (!_showCancelOverlay)
                    {
                        _showCancelOverlay = true;
                        _swatchPanel.Invalidate();
                    }
                }
                else
                {
                    overlay.Cursor = Cursors.Cross;
                    if (_showCancelOverlay)
                    {
                        _showCancelOverlay = false;
                        _swatchPanel.Invalidate();
                    }
                    // 实时预览鼠标指向的颜色
                    _selectedColor = GetPixelAt(pos);
                    _swatchPanel.Invalidate();
                }
            };

            overlay.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var pos = Cursor.Position;
                    if (ctrlRect.Contains(pos))
                    {
                        // 点击了组件区域：取消取色，恢复原色
                        _selectedColor = originalColor;
                        UpdateSwatch();
                    }
                    else
                    {
                        // 正常取色
                        _selectedColor = GetPixelAt(pos);
                        UpdateSwatch();
                        OnSelectedColorChanged(EventArgs.Empty);
                    }
                    overlay.Close();
                }
            };

            overlay.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    // ESC 取消取色，恢复原色
                    _selectedColor = originalColor;
                    UpdateSwatch();
                    overlay.Close();
                }
            };

            overlay.ShowDialog();

            // 取色结束后清除遮罩状态
            _showCancelOverlay = false;
            _swatchPanel.Invalidate();
        }

        /// <summary>
        /// 获取屏幕上指定坐标点的像素颜色。
        /// </summary>
        private static Color GetPixelAt(Point screenPoint)
        {
            nint hdc = WindowsAPI.GetDC(0);
            uint pixel = WindowsAPI.GetPixel(hdc, screenPoint.X, screenPoint.Y);
            WindowsAPI.ReleaseDC(0, hdc);
            return Color.FromArgb(
                (int)(pixel & 0xFF),
                (int)((pixel >> 8) & 0xFF),
                (int)((pixel >> 16) & 0xFF));
        }
    }
}
