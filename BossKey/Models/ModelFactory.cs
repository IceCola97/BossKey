using System;
using System.Collections.Generic;
using System.Text;

namespace BossKey.Models
{
    internal static class ModelFactory
    {
        private static readonly Lazy<IWindowScanner> _windowScanner = new(() => new WindowScanner());
        private static readonly Lazy<IWindowControllerManager> _windowControllerManager = new(() => new WindowControllerManager());
        private static readonly Lazy<IWindowController> _windowController = new(() => new WindowController());
        private static readonly Lazy<IHotkeyManager> _hotkeyManager = new(() => new HotkeyManager());
        private static readonly Lazy<ITrayModel> _trayModel = new(() => new TrayModel(WindowController));
        private static readonly Lazy<IWindowStateService> _windowStateService = new(() => new WindowStateService());

        public static IWindowScanner WindowScanner => _windowScanner.Value;

        public static IWindowControllerManager WindowControllerManager => _windowControllerManager.Value;

        public static IWindowController WindowController => _windowController.Value;

        public static IHotkeyManager HotkeyManager => _hotkeyManager.Value;

        public static ITrayModel TrayModel => _trayModel.Value;

        public static IWindowStateService WindowStateService => _windowStateService.Value;
    }
}
