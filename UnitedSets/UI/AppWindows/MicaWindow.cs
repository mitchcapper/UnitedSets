﻿// Reference: winui3gallery://item/SystemBackdrops
using WinRT;
using Microsoft.UI.Xaml;
using Microsoft.UI.Composition.SystemBackdrops;
using WinUIEx;
using UnitedSets.Helpers;
using Windows.Foundation;
using Get.EasyCSharp;

namespace UnitedSets.UI.AppWindows;

public partial class MicaWindow : WindowEx
{
    readonly bool IsMicaInfinite;
    MicaController? m_micaController;
    SystemBackdropConfiguration? m_configurationSource;
    
    public MicaWindow(bool IsMicaInfinite)
    {
        this.IsMicaInfinite = IsMicaInfinite;
        TrySetMicaBackdrop();
    }
    bool TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            WindowsSystemDispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            m_configurationSource = new SystemBackdropConfiguration();
            

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;

            m_micaController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);

            Activated += OnActivatedChange;
            Closed += OnWindowClosed;
            return true; // succeeded
        }

        return false; // Mica is not supported on this system
    }

    [Event(typeof(TypedEventHandler<object, WindowActivatedEventArgs>))]
    private void OnActivatedChange(WindowActivatedEventArgs args)
    {
        if (m_configurationSource == null) return;
        bool IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        if (IsInputActive)
            m_configurationSource.IsInputActive = true;
        else if (!IsMicaInfinite)
            m_configurationSource.IsInputActive = false;
    }
    
    [Event(typeof(TypedEventHandler<object, WindowEventArgs>))]
    private void OnWindowClosed()
    {
        // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
        // use this closed window.
        if (m_micaController != null)
        {
            m_micaController.Dispose();
            m_micaController = null;
        }
        Activated -= OnActivatedChange;
        m_configurationSource = null;
    }
}