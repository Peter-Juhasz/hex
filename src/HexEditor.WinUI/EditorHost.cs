using HexEditor.Model;
using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace HexEditor.WinUI;

public partial class EditorHost : Grid
{
	public EditorHost(IBinarySnapshot snapshot)
	{
		this.RequestedTheme = ElementTheme.Light;
		this.Background = new SolidColorBrush(Colors.White);
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(80, GridUnitType.Pixel),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(8, GridUnitType.Pixel),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(16, GridUnitType.Pixel),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(2, GridUnitType.Star),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(1, GridUnitType.Star),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(16, GridUnitType.Pixel),
		});

		this.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Star),
		});

		_view = new WinUIHexView(snapshot, _visualTheme);

		_verticalScrollBar = new ScrollBar()
		{
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Stretch,
			RequestedTheme = ElementTheme.Light,
			IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
		};
		_verticalScrollBar.SmallChange = _visualTheme.RowHeight;
		Grid.SetColumn(_verticalScrollBar, 5);
		this.Children.Add(_verticalScrollBar);

		_hexContentView = new HexContentView(_view);
		Grid.SetColumn(_hexContentView, 3);
		this.Children.Add(_hexContentView);

		_asciiContentView = new AsciiContentView(_view, _hexContentView);
		Grid.SetColumn(_asciiContentView, 4);
		this.Children.Add(_asciiContentView);

		_addressBarMargin = new AddressBarMargin(_view, _hexContentView);
		Grid.SetColumn(_addressBarMargin, 0);
		this.Children.Add(_addressBarMargin);

		_outliningMargin = new OutliningMargin(_view, _hexContentView);
		Grid.SetColumn(_outliningMargin, 2);
		this.Children.Add(_outliningMargin);

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		_hexContentView.ViewChanged += OnEditorViewChanged;
		_view.ScrollableHeightChanged += OnScrollableHeightChanged;
		_verticalScrollBar.ValueChanged += OnScrollBarValueChanged;

		_workerThreadQueue = Channel.CreateUnbounded<Task>();
		_workerThread = Worker(CancellationToken.None);
	}

	private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		_hexContentView.ScrollTo(e.NewValue);
		EnqueueWorkerTask(c => _view.ScrollToAsync(e.NewValue, c));
	}

	private void OnScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		_verticalScrollBar.Maximum = e.NewHeight;
	}

	private void OnEditorViewChanged(ScrollView sender, ViewportChangedEventArgs args)
	{
		_verticalScrollBar.ViewportSize = args.Height;
	}

	private readonly WinUIHexView _view;
	private readonly Channel<Task> _workerThreadQueue;
	private readonly Task _workerThread;

	private VisualTheme _visualTheme = new(
		Columns: 16,
		FontWidth: 8,
		RowHeight: 24
	);

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			EnqueueWorkerTask(c => _view.ResizeWindowAsync(_hexContentView.ActualWidth, _hexContentView.ActualHeight, c));
		});
	}

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;
	private readonly HexContentView _hexContentView;
	private readonly AsciiContentView _asciiContentView;
	private readonly ScrollBar _verticalScrollBar;

	private async Task Worker(CancellationToken cancellationToken)
	{
		await foreach (var task in _workerThreadQueue.Reader.ReadAllAsync(cancellationToken))
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				await new MessageDialog(ex.Message).ShowAsync();
			}
		}
	}

	private void EnqueueWorkerTask(Func<CancellationToken, Task> factory)
	{
		var task = factory(CancellationToken.None);
		_workerThreadQueue.Writer.TryWrite(task);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
	}
}
