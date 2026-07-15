#define IDLE_UPDATE

using BossKey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using static BossKey.Models.WindowsAPI;
using ListModel = BossKey.Models.SortedListModel<BossKey.Models.ScannedWindow, System.Windows.Forms.ListViewItem>;

namespace BossKey.Models
{
    internal sealed class UpdatingListModel
    {
        private const int MAX_IDLE_TICKS = 10;

        private readonly ListView _list;
        private readonly ImageList _images;
        private readonly ListModel _listModel = [];

#if IDLE_UPDATE
        private readonly MessageMergingQueue<UpdateCommand> _updateQueue = new(0);
#else
        private readonly MessageMergingQueue<UpdateCommand> _updateQueue = new(50);
#endif

        public UpdatingListModel(
            ListView list,
            ImageList images
        )
        {
            _list = list;
            _images = images;

            EnableDoubleBuffered(list);

            ModelFactory.WindowScanner.WindowCreated += WindowScanner_WindowCreated;
            ModelFactory.WindowScanner.WindowDestroyed += WindowScanner_WindowDestroyed;
            ModelFactory.WindowScanner.WindowShown += WindowScanner_WindowShown;
            ModelFactory.WindowScanner.WindowHidden += WindowScanner_WindowHidden;
            IconCache.WindowIconUpdated += IconCache_WindowIconUpdated;

            _updateQueue.OnDequeue += UpdateQueue_OnDequeue;

#if IDLE_UPDATE
            Application.Idle += Application_Idle;
#endif
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

        private void PostUpdateCommand(UpdateCommandType type, nint hWnd, nint hIcon)
        {
            _updateQueue.Enqueue(new UpdateCommand
            {
                Type = type,
                Window = new ScannedWindow
                {
                    Handle = hWnd,
                },
                IconHandle = hIcon,
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

        private void IconCache_WindowIconUpdated(nint hWnd, nint hIcon)
        {
            PostUpdateCommand(UpdateCommandType.WindowIconUpdated, hWnd, hIcon);
        }

        private void UpdateQueue_OnDequeue(IEnumerable<UpdateCommand> items)
        {
            _list.Invoke(() => PerformWindowChanged(items));
        }

        #endregion Async Event Handlers

        #region Idle Update

#if IDLE_UPDATE
        private void Application_Idle(object? sender, EventArgs e)
        {
            // 在应用程序空闲时处理更新队列中的所有命令
            _updateQueue.TriggerDequeue();
        }
#endif

        #endregion Idle Update

        #region Sync Partial Update

        private void PerformWindowChanged(IEnumerable<UpdateCommand> items)
        {
            if (MessageMergingQueue<UpdateCommand>.IsEmptyItems(items))
                return;

            int start = Environment.TickCount;

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
                    case UpdateCommandType.WindowIconUpdated:
                        PerformWindowIconUpdated(item.Window, item.IconHandle);
                        break;
                }

                if (Environment.TickCount - start > MAX_IDLE_TICKS)
                {
                    // 如果处理时间超过最大空闲时间，则中断处理，等待下一次空闲时继续处理
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

        private void PerformWindowIconUpdated(ScannedWindow window, nint iconHandle)
        {
            UpdateWindowIcon(window.Handle, iconHandle);
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

        private static string BuildIconKey(nint hWnd)
        {
            return hWnd.ToString("X");
        }

        private void UpdateWindowIcon(nint hWnd, nint hIcon)
        {
            if (hIcon != 0)
            {
                try
                {
                    using var icon = Icon.FromHandle(hIcon);
                    string key = BuildIconKey(hWnd);
                    _images.Images.RemoveByKey(key);
                    _images.Images.Add(key, icon);
                }
                catch
                {
                    // 图标句柄无效时跳过，imageKey 保持 null
                }
            }
        }

        private string? GetWindowIcon(ScannedWindow window)
        {
            nint hIcon = IconCache.GetWindowIcon(window.Handle);

            string? key = null;

            if (hIcon != 0)
            {
                try
                {
                    using var icon = Icon.FromHandle(hIcon);
                    key = BuildIconKey(window.Handle);
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

            item.Text = GlobalConfigs.Instance.DevelopMode
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

        private static void EnableDoubleBuffered(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }

        #endregion Helpers

        #region UpdateCommand

        private readonly struct UpdateCommand
        {
            public UpdateCommandType Type { get; init; }

            public ScannedWindow Window { get; init; }

            public nint IconHandle { get; init; }
        }

        private enum UpdateCommandType
        {
            WindowCreated,
            WindowDestroyed,
            WindowShown,
            WindowHidden,
            WindowIconUpdated,
        }

        #endregion UpdateCommand
    }
}
