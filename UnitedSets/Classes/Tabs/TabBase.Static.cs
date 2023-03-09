﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnitedSets.Services;
using WinWrapper;

namespace UnitedSets.Classes.Tabs;

partial class TabBase
{
    public readonly static List<Window> MainWindows = new();
    public static event Action? OnUpdateStatusLoopComplete;
    static readonly SynchronizedCollection<TabBase> AllTabs = new();
    //static readonly WindowClass UnitedSetsSwitcherWindowClass;

    static readonly SettingsService Settings
        = App.Current.Services.GetService<SettingsService>() ?? throw new InvalidOperationException("Settings Init Failed");

    static TabBase()
    {
        //UnitedSetsSwitcherWindowClass = new WindowClass(nameof(UnitedSetsSwitcherWindowClass),
        //    (hwnd, msg, wparam, lparam) =>
        //    {
        //        if (msg == PInvoke.WM_ACTIVATE)
        //        {
        //            var tab = AllTabs.FirstOrDefault(x => x.Windows.FirstOrDefault(x => x.Handle == hwnd, default) != default);
        //            tab?.SwitcherWindowFocusCallback();
        //        }
        //        return PInvoke.DefWindowProc(hwnd, msg, wparam, lparam);
        //    },
        //    ClassStyle: WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_HREDRAW,
        //    BackgroundBrush: new(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH).Value));
        Thread UpdateStatusLoop = new(StaticUpdateStatusThreadLoop)
        {
            Name = "United Sets Update Status Loop"
        };
        UpdateStatusLoop.Start();
    }
    

}
