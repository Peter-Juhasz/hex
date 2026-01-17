using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace HexEditor.WinUI;

internal class HexContentView : ContentControl
{
	public HexContentView(IHexView view, VisualTheme theme) : base()
	{
		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);

		_scrollView = new ScrollView
		{
			VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Top,
			VerticalAlignment = VerticalAlignment.Stretch,
			VerticalScrollMode = ScrollingScrollMode.Disabled,
			CornerRadius = new CornerRadius(0),
			Padding = new Thickness(0),
		};
		_scrollView.ViewChanged += (s, e) =>
		{
			RaiseViewChanged();
		};
		this.Content = _scrollView;

		_canvas = new Canvas
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MinWidth = (theme.Columns * 2) * theme.FontWidth,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_theme = theme;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		_view.ScrollableHeightChanged += OnViewHeightChanged;

		this.Loaded += OnLoaded;
	}

	public double VerticalOffset => _scrollView.VerticalOffset;

	public void ScrollTo(double offset)
	{
		_scrollView.ScrollTo(0, offset);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
	}

	internal void RaiseViewChanged()
	{
		ViewChanged?.Invoke(_scrollView, new ViewportChangedEventArgs(this.ActualWidth, this.ActualHeight, _scrollView.VerticalOffset));
	}

	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly double _fontSize = 14;
	private readonly FontFamily _editorFontFamily = new FontFamily("Cascadia Mono");
	private readonly Brush _editorForegroundBrush = new SolidColorBrush(Colors.Black);
	private readonly IHexView _view;
	private readonly VisualTheme _theme;

	public event TypedEventHandler<ScrollView, ViewportChangedEventArgs>? ViewChanged;

	public void SetScrollController(IScrollController controller)
	{
		_scrollView.ScrollPresenter.VerticalScrollController = controller;
	}

	private void OnViewVisibleRowsChanged(object sender, VisibleRowsChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
		{
			// remove old rows
			foreach (var row in e.RemovedRows)
			{
				for (var i = 0; i < _canvas.Children.Count; i++)
				{
					if (_canvas.Children[i] is FrameworkElement { Tag: IHexViewRow rowTag } fe && rowTag.Extent == row.Extent)
					{
						_canvas.Children.RemoveAt(i);
						break;
					}
				}
			}

			// add new rows
			foreach (var row in e.AddedRows)
			{
				var rowCanvas = new Canvas()
				{
					Height = row.VisualBounds.Height,
					Width = row.VisualBounds.Width,
					Tag = row,
				};
				Canvas.SetTop(rowCanvas, _canvas.XamlRoot.SnapToPixels(row.VisualBounds.Top));

				foreach (var run in row.HexRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _editorFontFamily,
						FontSize = _fontSize,
						Foreground = _editorForegroundBrush,
						IsTextSelectionEnabled = false,
						IsHitTestVisible = false,
						TextWrapping = TextWrapping.NoWrap,
						TextTrimming = TextTrimming.None,
						TextAlignment = TextAlignment.Left,
						Tag = run,
					};
					Canvas.SetLeft(hexTextBlock, _canvas.XamlRoot.SnapToPixels(run.LeftPosition));
					rowCanvas.Children.Add(hexTextBlock);
				}

				_canvas.Children.Add(rowCanvas);
			}
		});
	}

	private void OnViewHeightChanged(object sender, HeightChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			_canvas.Width = this.ActualWidth;
			_canvas.Height = e.NewHeight;
		});
	}
}

public class ViewportChangedEventArgs(double width, double height, double verticalOffset) : EventArgs
{
	public double Width { get; } = width;
	public double Height { get; } = height;
	public double VerticalOffset { get; } = verticalOffset;
}
