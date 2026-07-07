using BossKey.Components;
using BossKey.Models;
using BossKey.Utils;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BossKey
{
    public partial class FormMain : HotkeyFormBase
    {
        private readonly SortedListModel<ScannedWindow, ListViewItem> _listModel = [];

        public FormMain()
        {
            InitializeComponent();

            ModelFactory.WindowScanner.WindowCreated += WindowScanner_WindowCreated;
            ModelFactory.WindowScanner.WindowDestroyed += WindowScanner_WindowDestroyed;
        }

        #region Methods

        private ListViewItem GetScannedWindowListItem(ScannedWindow window)
        {
            string? imageKey = GetWindowIcon(window);
            string title = window.Title ?? string.Empty;

            var item = new ListViewItem
            {
                Text = title,
                Tag = window,
            };

            if (imageKey != null)
            {
                item.ImageKey = imageKey;
            }

            return item;
        }

        private IEnumerable<KeyValuePair<ScannedWindow, ListViewItem>> GetScannedWindowPairs(IEnumerable<ScannedWindow> windows)
        {
            foreach (var window in windows)
            {
                var item = GetScannedWindowListItem(window);
                yield return new KeyValuePair<ScannedWindow, ListViewItem>(window, item);
            }
        }

        private void PerformSearch(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = null;
            }

            ModelFactory.WindowScanner.Filter = text;

            var windows = ModelFactory.WindowScanner.Windows;

            imageWindow.Images.Clear();

            _listModel.Clear();
            _listModel.AddAll(GetScannedWindowPairs(windows));

            listWindows.BeginUpdate();
            listWindows.Items.Clear();

            foreach (var (_, item) in _listModel)
            {
                listWindows.Items.Add(item);
            }

            listWindows.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            listWindows.EndUpdate();
        }

        private void PerformWindowCreated(ScannedWindow window)
        {
            // 先把新窗口加入到列表模型中
            var item = GetScannedWindowListItem(window);
            int index = _listModel.Add(window, item);

            if (index < 0)
            {
                return;
            }

            // 再按索引把新窗口加入到 ListView 中
            listWindows.Items.Insert(index, item);
        }

        private void PerformWindowDestroyed(ScannedWindow window)
        {
            // 先把窗口从列表模型中移除
            int index = _listModel.Remove(window);

            if (index < 0)
            {
                return;
            }

            // 再按索引把窗口从 ListView 中移除
            var item = listWindows.Items[index];
            listWindows.Items.RemoveAt(index);
            imageWindow.Images.RemoveByKey(item.ImageKey);
        }

        private static string BuildIconKey(ScannedWindow window)
        {
            return window.Handle.ToString("X");
        }

        private string? GetWindowIcon(ScannedWindow window)
        {
            // 优先使用 GetClassLong 获取图标（不发送窗口消息，性能远优于 SendMessage）
            nint hIcon = WindowsAPI.GetClassLong(window.Handle, WindowsAPI.ClassLongIndex.HIconSm);

            if (hIcon == 0)
            {
                hIcon = WindowsAPI.GetClassLong(window.Handle, WindowsAPI.ClassLongIndex.HIcon);
            }

            // 仅当 GetClassLong 无法获取图标时才回退到 SendMessage
            if (hIcon == 0)
            {
                hIcon = WindowsAPI.SendMessage(window.Handle, WindowsAPI.WindowMessage.GetIcon, (nint)WindowsAPI.IconSize.Small2, 0);

                if (hIcon == 0)
                {
                    hIcon = WindowsAPI.SendMessage(window.Handle, WindowsAPI.WindowMessage.GetIcon, (nint)WindowsAPI.IconSize.Small, 0);
                }
            }

            string? key = null;

            if (hIcon != 0)
            {
                try
                {
                    using var icon = Icon.FromHandle(hIcon);
                    key = BuildIconKey(window);
                    imageWindow.Images.Add(key, icon);
                }
                catch
                {
                    // 图标句柄无效时跳过，imageKey 保持 null
                }
            }

            return key;
        }

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
            trackOpacity.Value = trackOpacity.Maximum;
            trackVolume.Value = trackVolume.Maximum;
            hotkeyAutoHide.Hotkey = Keys.None;
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
                trackOpacity.Value = controller.Opacity ?? trackOpacity.Maximum;
                trackVolume.Value = controller.Volume.HasValue
                    ? (int)(controller.Volume.Value * 100)
                    : trackVolume.Maximum;
                (hotkeyAutoHide.ModifierKeys, hotkeyAutoHide.BaseKey) =
                    controller.AutoHideHotkey?.NormalizeLeft() ?? Hotkey.None;
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
            try
            {
                ModelFactory.WindowController.Opacity = checkOpacity.Checked
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
            try
            {
                ModelFactory.WindowController.AutoHideHotkey =
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
            try
            {
                ModelFactory.WindowController.Volume = checkVolume.Checked
                    ? (float)trackVolume.Value / 100 : null;

            }
            catch (Exception ex)
            {
                SyncBackWindowControllerSelection();
                ReportError(ex.Message, "更改进程音量失败");
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
            try
            {
                ModelFactory.WindowController.TopMost = checkTopmost.Checked;
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

        #endregion Methods

        #region Events

        private void WindowScanner_WindowCreated(ScannedWindow window)
        {
            Invoke(() => PerformWindowCreated(window));
        }

        private void WindowScanner_WindowDestroyed(ScannedWindow window)
        {
            Invoke(() => PerformWindowDestroyed(window));
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            ModelFactory.HotkeyManager.BindWindow(this);
            SwitchControlPanelEnabled(false);
            PerformSearch(string.Empty);
        }

        private void TextSearch_TextChanged(object sender, EventArgs e)
        {
            LazyCall.Debounce("TextSearch_TextChanged", 300, () =>
            {
                Invoke(() =>
                {
                    PerformSearch(textSearch.Text);
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

        #endregion Events

        private void FormMain_Click(object sender, EventArgs e)
        {
            Debugger.Break();
        }
    }
}
