using EasyCSharp;
using Microsoft.UI.Windowing;
using System.Collections.ObjectModel;
using WinRT.Interop;
using Microsoft.UI.Xaml;
using System;
using WindowEx = WinWrapper.Window;
using Windows.Win32;
using UnitedSets.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using WinUIEx.Messaging;
using Microsoft.UI.Dispatching;
using UnitedSets.Classes.Tabs;
using UnitedSets.Windows.Flyout;
using UnitedSets.Classes;
using System.Linq;
using System.Collections.Generic;
using Microsoft.UI;
using TransparentWinUIWindowLib;
using System.Threading.Tasks;
using IT = Windows.Foundation;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UnitedSets.Windows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : INotifyPropertyChanged
{
    public const string UnitedSetsTabWindowDragProperty = "UnitedSetsTabWindow";
    public static readonly uint UnitedSetCommunicationChangeWindowOwnership
        = PInvoke.RegisterWindowMessage(nameof(UnitedSetCommunicationChangeWindowOwnership));

    // Implement INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    // Singleton
    public SettingsService Settings = App.Current.Services.GetService<SettingsService>() ?? throw new InvalidOperationException();
    
    // Readonly
    public ObservableCollection<TabBase> Tabs { get; } = new();
    public ObservableCollection<TabGroup> HiddenTabs { get; } = new();

    public readonly WindowEx WindowEx;
    
    // Property
    [Property(CustomGetExpression = $"{nameof(_HasOwner)} = {nameof(WindowEx)}.Owner.IsValid", SetVisibility = GeneratorVisibility.DoNotGenerate)]
    bool _HasOwner = false;
    
    Visibility SettingsButtonVisibility => HasOwner ? Visibility.Collapsed : Visibility.Visible;
    readonly DispatcherQueueTimer timer;
    readonly WindowMessageMonitor WindowMessageMonitor;
    public bool IsAltTabVisible;

	//protected volatile bool TabCollectionChanged=true;
	//private TabBase[]? GetUpdatedTabCollection() {
	//	if (!TabCollectionChanged)
	//		return null;

	//	TabCollectionChanged = false;
	//	return Tabs.ToArray();
	//}
	public void AddTab(TabBase tab, int? index=null) {
		WireTabEvents(tab);
		if (index != null)
			Tabs.Insert(index.Value, tab);
		else
			Tabs.Add(tab);
		//TabCollectionChanged = true;
	}
	public void RemoveTab(TabBase tab) {
		Tabs.Remove(tab);
		UnwireTabEvents(tab);
		//TabCollectionChanged = true;
	}
	public IEnumerable<TabBase> GetTabsAndClear() {
		var ret = Tabs.ToArray();
		foreach (var tab in ret)
			RemoveTab(tab);
		return ret;
	}
	
	public TabBase? FindTabByWindow(WinWrapper.Window window) {
		return Tabs.ToArray().FirstOrDefault(tab=>tab.Windows.Contains(window));
	}
	public (TabGroup? group, TabBase? tab) FindHiddenTabByWindow(WinWrapper.Window window) {
		foreach (var tabg in HiddenTabs.ToArray()) {
			var tab = tabg.Tabs.ToArray().FirstOrDefault(tab => tab.Windows.Contains(window));
			if (tab != null)
				return (tabg, tab);
		}

		return (null, null);
	}
	//private Func<TabBase?> GetUpdatedTabCollectionDelegate() {
	//	var info = typeof(MainWindow).GetMethod(nameof(GetUpdatedTabCollection), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
	//	var args = new ParameterExpression[0];
	//	var lambdaExpression = Expression.Call(Expression.Constant(this), info!, args);
	//	return Expression.Lambda<Func<TabBase?>>(lambdaExpression, args).Compile();//compiled expressions have a one time setup cost but should be near equivalent of bare metal code for each call
	//}
	TransparentWindowManager trans_mgr;
	    public MainWindow()
    {
       
        Title = "UnitedSets";
		InitializeComponent();
		if (FeatureFlags.USE_TRANSPARENT_WINDOW)
			TransparentSetup();

		MinWidth = 100;
        
        WindowEx = WindowEx.FromWindowHandle(WindowNative.GetWindowHandle(this));
        WindowMessageMonitor = new WindowMessageMonitor(WindowEx);
		ExtendsContentIntoTitleBar = true;
        SetTitleBar(CustomDragRegion);

        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500);

        TabBase.MainWindows.Add(WindowEx);

        AppWindow.Closing += OnWindowClosing;
        Activated += FirstRun;
        SizeChanged += OnMainWindowResize;
        CustomDragRegionUpdator.EffectiveViewportChanged += OnCustomDragRegionUpdatorCalled;
        WindowMessageMonitor.WindowMessageReceived += OnWindowMessageReceived;
        TabBase.OnUpdateStatusLoopComplete += OnLoopCalled;
        timer.Tick += OnTimerLoopTick;
        timer.Start();
		// --add-window-by-exe
		var toAdd = CLI.GetArrVal("add-window-by-exe");
		var editLastAddedWindow = CLI.GetFlag("edit-last-added");
		LeftFlyout.NoAutoClose = CLI.GetFlag("edit-no-autoclose");
		foreach (var itm in toAdd) {
			var procs = System.Diagnostics.Process.GetProcesses().Where(p=>p.ProcessName.Equals(itm, StringComparison.OrdinalIgnoreCase)).ToList();
			foreach (var proc in procs)
				if (!proc.HasExited)
					AddTab( WindowEx.FromWindowHandle(proc.MainWindowHandle));
				
			
		}
		if (editLastAddedWindow && Tabs.Count > 0)
			Tabs.Last().TabDoubleTapped(this, new Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs());
		
	}
	private bool firstActivation;
	private void TransparentSetup() {
		var border = new Border {BorderThickness=new(10), Background=new SolidColorBrush(Color.FromArgb(0xdd,0xff,0xff,0xff)), CornerRadius=new(15,5,15,5), HorizontalAlignment=HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
		border.BorderBrush = new LinearGradientBrush(new GradientStopCollection { new GradientStop { Color = Color.FromArgb(0x99, 0x87, 0xC7, 0xFF), Offset = 1 }, new GradientStop { Color = Color.FromArgb(0x99, 0x00, 0x00, 0x8b), Offset = 0 } }, 45);
		Grid.SetColumnSpan(border, 50);
		Grid.SetRowSpan(border, 50);
		Canvas.SetZIndex(border, -5);
		MainAreaBorder.Margin = new(8, 0, 8, 8);
		RootGrid.Children.Insert(0, border);
		trans_mgr = new(this, swapChainPanel, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"Assets\NearTransparentBG.png"), FeatureFlags.ENTIRE_WINDOW_DRAGGABLE);
		trans_mgr.AfterInitialize();

	}

	private async void TransparentFinalize() {
		var width = this.Width;
		var height = this.Height;
		trans_mgr.RemoveBorderSetTransparentMap();
		Width = width;
		Height = height;

	}

	
	private void WireTabEvents(TabBase tab) {
		tab.RemoveTab += TabRemoveRequest;
		tab.ShowFlyout += TabShowFlyoutRequest;
		tab.ShowTab += TabShowRequest;
	}



	private void UnwireTabEvents(TabBase tab) {
		tab.RemoveTab -= TabRemoveRequest;
		tab.ShowFlyout -= TabShowFlyoutRequest;
		tab.ShowTab -= TabShowRequest;
	}
	
}
