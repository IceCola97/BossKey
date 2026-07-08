using BossKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using ListModel = BossKey.Models.SortedListModel<BossKey.Models.ScannedWindow, System.Windows.Forms.ListViewItem>;

namespace BossKey.Models
{
    internal sealed class UpdatingListModel
    {
        private readonly ListView _list;
        private readonly ImageList _images;
        private readonly ListModel _listModel = [];

        private readonly MessageMergingQueue<UpdateCommand> _updateQueue = new(50);

        public UpdatingListModel(
            ListView list,
            ImageList images
        )
        {
            _list = list;
            _images = images;

            ModelFactory.WindowScanner.WindowCreated += WindowScanner_WindowCreated;
            ModelFactory.WindowScanner.WindowDestroyed += WindowScanner_WindowDestroyed;
            ModelFactory.WindowScanner.WindowShown += WindowScanner_WindowShown;
            ModelFactory.WindowScanner.WindowHidden += WindowScanner_WindowHidden;

            _updateQueue.OnDequeue += UpdateQueue_OnDequeue;
        }

        #region Async Event Handlers

        private void PostUpdateCommand(UpdateCommandType type, ScannedWindow window)
        {
            _updateQueue.Enqueue(new UpdateCommand
            {
                Type = type,
                Window = window,
            });
        }

        private void WindowScanner_WindowCreated(ScannedWindow window)
        {
            PostUpdateCommand(UpdateCommandType.WindowCreated, window);
        }

        private void WindowScanner_WindowDestroyed(ScannedWindow window)
        {
            PostUpdateCommand(UpdateCommandType.WindowDestroyed, window);
        }

        private void WindowScanner_WindowShown(ScannedWindow window)
        {
            PostUpdateCommand(UpdateCommandType.WindowShown, window);
        }

        private void WindowScanner_WindowHidden(ScannedWindow window)
        {
            PostUpdateCommand(UpdateCommandType.WindowHidden, window);
        }

        private void UpdateQueue_OnDequeue(UpdateCommand[] items)
        {
            _list.Invoke(() => PerformWindowChanged(items));
        }

        #endregion Async Event Handlers

        #region Sync Partial Update

        private void PerformWindowChanged(UpdateCommand[] items)
        {
            if (items.Length == 0)
                return;

            _list.BeginUpdate();

            foreach (var item in items)
            {
                switch (item.Type)
                {
                    case UpdateCommandType.WindowCreated:
                        PerformWindowCreated(item.Window);
                        break;
                    case UpdateCommandType.WindowDestroyed:
                        PerformWindowDestroyed(item.Window);
                        break;
                    case UpdateCommandType.WindowShown:
                        PerformWindowShown(item.Window);
                        break;
                    case UpdateCommandType.WindowHidden:
                        PerformWindowHidden(item.Window);
                        break;
                }
            }

            _list.EndUpdate();
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
            _list.Items.Insert(index, item);
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
            var item = _list.Items[index];
            _list.Items.RemoveAt(index);
            _images.Images.RemoveByKey(item.ImageKey);
        }

        private void PerformWindowShown(ScannedWindow window)
        {
            PerformWindowUpdateState(window);
        }

        private void PerformWindowHidden(ScannedWindow window)
        {
            PerformWindowUpdateState(window);
        }

        private void PerformWindowUpdateState(ScannedWindow window)
        {
            if (_listModel.TryGetValue(window, out var item))
            {
                UpdateScannedWindowListItem(window, item);
            }
        }

        #endregion Sync Partial Update

        #region Full Update

        public string Filter
        {
            get => ModelFactory.WindowScanner.Filter ?? string.Empty;
            set
            {
                var windowScanner = ModelFactory.WindowScanner;
                string? prevValue = windowScanner.Filter;
                string? nextValue = value;

                if (string.IsNullOrEmpty(prevValue))
                    prevValue = null;
                if (string.IsNullOrEmpty(nextValue))
                    nextValue = null;

                if (prevValue != nextValue)
                    PerformSearch(value);
            }
        }

        public void Invalidate()
        {
            PerformSearch(ModelFactory.WindowScanner.Filter);
        }

        private void PerformSearch(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = null;
            }

            ModelFactory.WindowScanner.Filter = text;

            var windows = ModelFactory.WindowScanner.Windows;

            _images.Images.Clear();

            _listModel.Clear();
            _listModel.AddAll(GetScannedWindowPairs(windows));

            _list.BeginUpdate();
            _list.Items.Clear();

            foreach (var (_, item) in _listModel)
            {
                _list.Items.Add(item);
            }

            _list.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            _list.EndUpdate();
        }

        #endregion

        #region Helpers

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
                    _images.Images.Add(key, icon);
                }
                catch
                {
                    // 图标句柄无效时跳过，imageKey 保持 null
                }
            }

            return key;
        }

        private ListViewItem GetScannedWindowListItem(ScannedWindow window)
        {
            var item = new ListViewItem();
            UpdateScannedWindowListItem(window, item);
            return item;
        }

        private void UpdateScannedWindowListItem(
            ScannedWindow window,
            ListViewItem item
        )
        {
            string? imageKey = GetWindowIcon(window);
            string title = window.Title ?? string.Empty;

            item.Text = GlobalConfigs.DevelopMode
                 ? string.Format("({0:X08}) {1}", window.Handle, title)
                 : title;
            item.Tag = window;

            if (!window.Visible)
            {
                item.ForeColor = SystemColors.GrayText;
                item.Text = "[隐藏] " + item.Text;
            }
            else
            {
                item.ForeColor = SystemColors.WindowText;
            }

            if (imageKey != null)
            {
                item.ImageKey = imageKey;
            }
        }

        private IEnumerable<KeyValuePair<ScannedWindow, ListViewItem>> GetScannedWindowPairs(IEnumerable<ScannedWindow> windows)
        {
            foreach (var window in windows)
            {
                var item = GetScannedWindowListItem(window);
                yield return new KeyValuePair<ScannedWindow, ListViewItem>(window, item);
            }
        }

        #endregion Helpers

        #region UpdateCommand

        private readonly struct UpdateCommand
        {
            public UpdateCommandType Type { get; init; }

            public ScannedWindow Window { get; init; }
        }

        private enum UpdateCommandType
        {
            WindowCreated,
            WindowDestroyed,
            WindowShown,
            WindowHidden,
        }

        #endregion UpdateCommand
    }
}
