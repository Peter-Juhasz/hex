using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace HexEditor.WinUI;

internal class AsciiContentView : ContentControl
{
	public AsciiContentView(IHexView view, ViewScroller viewScroller, VisualTheme theme) : base()
	{
		_theme = theme;
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
			VerticalAlignment = VerticalAlignment.Stretch,
			VerticalScrollMode = ScrollingScrollMode.Disabled,
			CornerRadius = new CornerRadius(0),
			Padding = new Thickness(0),
		};
		this.Content = _scrollView;

		_canvas = new Canvas
		{
			MinWidth = theme.Columns * theme.FontWidth,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		viewScroller.OffsetChanged += OnScrollOffsetChanged;
		viewScroller.ScrollableHeightChanged += OnScrollableHeightChanged;
	}

	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly FontFamily _editorFontFamily = new FontFamily("Cascadia Mono");
	private readonly Brush _editorForegroundBrush = new SolidColorBrush(Colors.Black);
	private readonly IHexView _view;
	private readonly VisualTheme _theme;

	private void OnViewVisibleRowsChanged(object sender, VisibleRowsChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
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
				Canvas.SetTop(rowCanvas, Math.Round(row.VisualBounds.Top));

				foreach (var run in row.AsciiRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _editorFontFamily,
						FontSize = _theme.FontSize,
						Foreground = _editorForegroundBrush,
						IsTextSelectionEnabled = false,
						IsHitTestVisible = false,
						TextWrapping = TextWrapping.NoWrap,
						TextTrimming = TextTrimming.None,
						TextAlignment = TextAlignment.Left,
						Tag = run,
					};
					Canvas.SetLeft(hexTextBlock, Math.Round(run.LeftPosition));
					rowCanvas.Children.Add(hexTextBlock);
				}

				_canvas.Children.Add(rowCanvas);
			}
		});
	}

	#region Scrolling
	private static readonly ScrollingScrollOptions scrollOptions = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled);

	private void OnScrollOffsetChanged(object? sender, ScrollChangedEventArgs e)
	{
		_scrollView.ScrollTo(0, e.VerticalOffset, scrollOptions);
	}

	private void OnScrollableHeightChanged(object sender, ScrollableHeightChangedEventArgs e)
	{
		_canvas.Height = e.NewHeight;
	}
	#endregion
}
