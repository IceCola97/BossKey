using BossKey.Components;
using BossKey.Models;
using BossKey.Utils;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BossKey
{
    public partial class FormMain : HotkeyFormBase
    {
        private readonly UpdatingListModel _updatingListModel;

        public FormMain()
        {
            InitializeComponent();

            _updatingListModel = new(listWindows, imageWindow);
        }

        #region Methods

        private void SyncBackWindowControllerSelection()
        {
            if (ModelFactory.WindowController.Current == 0)
            {
                listWindows.SelectedItems.Clear();
            }
        }

        private void SwitchToWindow(nint hWnd)
        {
            nint current = ModelFactory.WindowController.Current;

            if (current != hWnd)
            {
                if (current != 0)
                {
                    ModelFactory.WindowController.Close();
                }

                if (hWnd == 0)
                {
                    SwitchControlPanelEnabled(false);
                    return;
                }

                ModelFactory.WindowController.Open(hWnd);
                SwitchControlPanelEnabled(true);
            }
        }

        private void SwitchControlPanelEnabled(bool enabled)
        {
            groupConf.Enabled = enabled;
            checkOpacity.Enabled = enabled;
            checkAutoHide.Enabled = enabled;
            checkVolume.Enabled = enabled;
            checkTopmost.Enabled = enabled;
            checkTransparentColor.Enabled = enabled;

            if (enabled)
            {
                LoadControlPanelState();
            }
            else
            {
                ClearControlPanelState();
            }
        }

        private void ClearControlPanelState()
        {
            checkOpacity.Checked = false;
            checkAutoHide.Checked = false;
            checkVolume.Checked = false;
            checkTopmost.Checked = false;
            checkTransparentColor.Checked = false;
            trackOpacity.Value = trackOpacity.Maximum;
            trackVolume.Value = trackVolume.Maximum;
            hotkeyAutoHide.Hotkey = Keys.None;
            colorPickerButton.SelectedColor = Color.White;
            labelOpacity.Text = "100%";
            labelVolume.Text = "100%";
        }

        private void LoadControlPanelState()
        {
            try
            {
                var controller = ModelFactory.WindowController;
                checkOpacity.Checked = controller.Opacity.HasValue;
                checkAutoHide.Checked = controller.AutoHideHotkey.HasValue;
                checkVolume.Checked = controller.Volume.HasValue;
                checkTopmost.Checked = controller.TopMost;
                checkTransparentColor.Checked = controller.TransparentColor.HasValue;
                trackOpacity.Value = controller.Opacity ?? trackOpacity.Maximum;
                trackVolume.Value = controller.Volume.HasValue
                    ? (int)(controller.Volume.Value * 100)
                    : trackVolume.Maximum;
                (hotkeyAutoHide.ModifierKeys, hotkeyAutoHide.BaseKey) =
                    controller.AutoHideHotkey?.NormalizeLeft() ?? Hotkey.None;
                colorPickerButton.SelectedColor = controller.TransparentColor.HasValue
                    ? FromColorBGR(controller.TransparentColor.Value) : Color.White;
                labelOpacity.Text = $"{trackOpacity.Value / 255.0 * 100:F0}%";
                labelVolume.Text = $"{trackVolume.Value}%";
            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "载入窗口数据失败");
            }
        }

        private void SwitchOpacityPanelEnabled(bool enabled)
        {
            trackOpacity.Enabled = enabled;
            labelOpacity.Enabled = enabled;

            LazySaveOpacityPanelState();
        }

        private void LazySaveOpacityPanelState(int duration = 300)
        {
            LazyCall.Debounce("SaveOpacityPanelState", duration, () => Invoke(SaveOpacityPanelState));
        }

        private void SaveOpacityPanelState()
        {
            var windowController = ModelFactory.WindowController;

            if (windowController.Current == 0)
            {
                return;
            }

            try
            {
                windowController.Opacity = checkOpacity.Checked
                    ? (byte)trackOpacity.Value : null;
            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "更改透明度失败");
            }
        }

        private void SwitchAutoHidePanelEnabled(bool enabled)
        {
            hotkeyAutoHide.Enabled = enabled;
            LazySaveAutoHidePanelState();
        }

        private void LazySaveAutoHidePanelState(int duration = 300)
        {
            LazyCall.Debounce("SaveAutoHidePanelState", duration, () => Invoke(SaveAutoHidePanelState));
        }

        private void LoadAutoHidePanelState()
        {
            (hotkeyAutoHide.ModifierKeys, hotkeyAutoHide.BaseKey) =
                ModelFactory.WindowController.AutoHideHotkey?.NormalizeLeft() ?? Hotkey.None;
        }

        private void SaveAutoHidePanelState()
        {
            var windowController = ModelFactory.WindowController;

            if (windowController.Current == 0)
            {
                return;
            }

            try
            {
                windowController.AutoHideHotkey =
                    checkAutoHide.Checked
                    && hotkeyAutoHide.ModifierKeys != ModifierKey.None
                    && hotkeyAutoHide.BaseKey != Keys.None
                        ? new(hotkeyAutoHide.ModifierKeys, hotkeyAutoHide.BaseKey) : null;
            }
            catch (Exception ex)
            {
                LoadAutoHidePanelState();
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "添加热键失败");
            }
        }

        private void SwitchVolumePanelEnabled(bool enabled)
        {
            trackVolume.Enabled = enabled;
            labelVolume.Enabled = enabled;
            LazySaveVolumePanelState();
        }

        private void LazySaveVolumePanelState(int duration = 300)
        {
            LazyCall.Debounce("SaveVolumePanelState", duration, () => Invoke(SaveVolumePanelState));
        }

        private void SaveVolumePanelState()
        {
            var windowController = ModelFactory.WindowController;

            if (windowController.Current == 0)
            {
                return;
            }

            try
            {
                windowController.Volume = checkVolume.Checked
                    ? (float)trackVolume.Value / 100 : null;

            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "更改进程音量失败");
            }
        }

        private void SwitchTransparentColorPanelEnabled(bool enabled)
        {
            colorPickerButton.Enabled = enabled;

            LazySaveTransparentColorPanelState();
        }

        private void LazySaveTransparentColorPanelState(int duration = 300)
        {
            LazyCall.Debounce("SaveTransparentColorPanelState", duration, () => Invoke(SaveTransparentColorPanelState));
        }

        private void SaveTransparentColorPanelState()
        {
            var windowController = ModelFactory.WindowController;

            if (windowController.Current == 0)
            {
                return;
            }

            try
            {
                windowController.TransparentColor = checkTransparentColor.Checked
                    ? ToColorBGR(colorPickerButton.SelectedColor) : null;
            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "更改透明颜色失败");
            }
        }

        private void SwitchTopMostPanelEnabled(bool enabled)
        {
            LazySaveTopMostPanelState();
        }

        private void LazySaveTopMostPanelState(int duration = 300)
        {
            LazyCall.Debounce("SaveTopMostPanelState", duration, () => Invoke(SaveTopMostPanelState));
        }

        private void SaveTopMostPanelState()
        {
            var windowController = ModelFactory.WindowController;

            if (windowController.Current == 0)
            {
                return;
            }

            try
            {
                windowController.TopMost = checkTopmost.Checked;
            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "更改置顶状态失败");
            }
        }

        private static void ReportError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error, default, MessageBoxOptions.ServiceNotification);
        }

        private static int ToColorBGR(Color color)
        {
            return (color.B << 16) | (color.G << 8) | color.R;
        }

        private static Color FromColorBGR(int bgr)
        {
            int r = bgr & 0xFF;
            int g = (bgr >> 8) & 0xFF;
            int b = (bgr >> 16) & 0xFF;
            return Color.FromArgb(r, g, b);
        }

        #endregion Methods

        #region Events

        private void FormMain_Load(object sender, EventArgs e)
        {
            ModelFactory.HotkeyManager.BindWindow(this);
            SwitchControlPanelEnabled(false);
            _updatingListModel.Invalidate();
        }

        private void TextSearch_TextChanged(object sender, EventArgs e)
        {
            LazyCall.Debounce("TextSearch_TextChanged", 300, () =>
            {
                Invoke(() =>
                {
                    _updatingListModel.Filter = textSearch.Text;
                });
            });
        }

        private void ListWindows_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listWindows.SelectedItems.Count <= 0)
            {
                SwitchToWindow(0);
                return;
            }

            var item = listWindows.SelectedItems[0];

            if (item.Tag is not ScannedWindow scannedWindow)
            {
                SwitchToWindow(0);
                return;
            }

            SwitchToWindow(scannedWindow.Handle);
        }

        private void CheckOpacity_CheckedChanged(object sender, EventArgs e)
        {
            SwitchOpacityPanelEnabled(checkOpacity.Checked);
        }

        private void CheckAutoHide_CheckedChanged(object sender, EventArgs e)
        {
            SwitchAutoHidePanelEnabled(checkAutoHide.Checked);
        }

        private void CheckVolume_CheckedChanged(object sender, EventArgs e)
        {
            SwitchVolumePanelEnabled(checkVolume.Checked);
        }

        private void CheckTopmost_CheckedChanged(object sender, EventArgs e)
        {
            SwitchTopMostPanelEnabled(checkTopmost.Checked);
        }

        private void CheckTransparentColor_CheckedChanged(object sender, EventArgs e)
        {
            SwitchTransparentColorPanelEnabled(checkTransparentColor.Checked);
        }

        private void TrackOpacity_Scroll(object sender, EventArgs e)
        {
            labelOpacity.Text = $"{trackOpacity.Value / 255.0 * 100:F0}%";
            LazySaveOpacityPanelState();
        }

        private void HotkeyAutoHide_HotkeyChanged(object sender, EventArgs e)
        {
            LazySaveAutoHidePanelState();
        }

        private void TrackVolume_Scroll(object sender, EventArgs e)
        {
            labelVolume.Text = $"{trackVolume.Value}%";
            LazySaveVolumePanelState();
        }

        private void ColorPickerButton_SelectedColorChanged(object sender, EventArgs e)
        {
            LazySaveTransparentColorPanelState();
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                // 切换开发模式
                GlobalConfigs.DevelopMode = !GlobalConfigs.DevelopMode;
                _updatingListModel.Invalidate();
            }
        }

        private void TimerLock_Tick(object sender, EventArgs e)
        {
            // 对于一些窗口可能会反复清空属性，所以为了优化体验，对于当前窗口将使用定时器定期刷新属性
            if (ModelFactory.WindowController.Current != 0)
            {
                ModelFactory.WindowController.ReapplyProperties();
            }
        }

        #endregion Events
    }
}
