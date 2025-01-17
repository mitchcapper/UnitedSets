using Get.EasyCSharp;
using Cursor = WinWrapper.Input.Cursor;
using Keyboard = WinWrapper.Input.Keyboard;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Windows.Foundation;

namespace UnitedSets.UI.AppWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : INotifyPropertyChanged
{
    private partial void SetupUIThreadLoopTimer(out DispatcherQueueTimer timer);

    [Event(typeof(TypedEventHandler<DispatcherQueueTimer, object>))]
    private void OnUIThreadTimerLoop()
    {
        CacheValue();
    }
    private async void OnDifferentThreadLoop()
    {
        UpdateWindowIcon();

        ThreadLoopDetectAndUpdateHasOwnerChange();

        await RemoveDisposedTab();

        if (Cursor.IsLeftButtonDown && Keyboard.IsControlDown)
            WindowDragLogic();
    }

}