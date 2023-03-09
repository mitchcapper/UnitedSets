using System.Drawing;
using WindowEx = WinWrapper.Window;
using WinWrapper;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI3HwndHostPlus;

partial class HwndHost
{
    int CountDown = 5;
	public async void ClearCrop() {
		CropLeft = CropRight = CropBottom = CropTop = 0;
		await _HostedWindow.SetRegionAsync(null);//could do initial region as well
	}
	public void FixSizeBug() => _HostedWindow.Restore();
	public bool MayBeSizeBug =>  _CacheWidth == 0 && _CacheHeight ==0 && ! IsDisposed && _HostedWindow.IsValid && _HostedWindow.IsNormalSize && Visibility == Microsoft.UI.Xaml.Visibility.Visible;
    async void OnWindowUpdate()
    {
        if (_CacheWidth == 0 || _CacheHeight == 0) return; // wait for update
        if (IsDisposed) return;

        var WindowToHost = this._HostedWindow;

        if (!IsWindowVisible)
        {
            WindowToHost.IsVisible = false;
            return;
        }

        bool Check = false;
        if (CountDown > 0)
        {
            CountDown--;
            if (CountDown == 0) WindowToHost.Redraw();
        }
        else Check = true;
        var windowbounds = _ParentWindow.Bounds;

        var scale = GetScale(_ParentWindow);
        var Pt = new System.Drawing.Point
        {
            X = (int)(windowbounds.X + _CacheXFromWindow * scale),
            Y = (int)(windowbounds.Y + _CacheYFromWindow * scale)
        };


        try
        {
			if (WindowToHost.IsResizable)
				WindowToHost.IsResizable = false;
        }
        catch
        {

        }
        if (!WindowToHost.IsValid)
        {
            Dispose();
            return;
        }
        Updating?.Invoke();
        var YShift = _ParentWindow.IsMaximized ? 8 : 0;
        if (!_NoMovingMode)
        {
            var oldBounds = WindowToHost.Bounds;
            var newBounds = new Rectangle(
            Pt.X + 8 - _CropLeft,
            Pt.Y + YShift - _CropTop,
            (int)(_CacheWidth * scale) + _CropLeft + _CropRight,
            (int)(_CacheHeight * scale) + _CropTop + _CropBottom
            );
            if (oldBounds != newBounds)
            {
                if (Check && WindowEx.ForegroundWindow == WindowToHost)
                {
                    await DetachAndDispose();
                    return;
                }
                else WindowToHost.Bounds = newBounds;
                if (ActivateCrop)
                    if (ForceInvalidateCrop || oldBounds.Size != newBounds.Size)
                    {
                        ForceInvalidateCrop = false;
                        _ = WindowToHost.SetRegionAsync(new(_CropLeft, _CropTop, WindowToHost.Bounds.Width - _CropLeft - _CropRight, WindowToHost.Bounds.Height - _CropTop - _CropBottom));
                    }
            }
        }
        if (!_IsOwnerSetSuccessful)
        {
            var oldBounds = WindowToHost.Bounds;
            if (new WindowRelative(WindowToHost).GetAboves().Take(10).Any(x => x == _ParentWindow))
            {
                await Task.Delay(500);
                if (oldBounds == WindowToHost.Bounds && IsWindowVisible)
                {
                    WindowToHost.ActivateTopMost();
                    WindowToHost.Focus();
                }
            }
        }
        WindowToHost.IsVisible = true;
    }
}
