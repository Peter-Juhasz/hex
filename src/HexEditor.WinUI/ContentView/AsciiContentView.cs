using HexEditor.Model;
using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.UI;

namespace HexEditor.WinUI.ContentView;

internal sealed class AsciiContentView : ContentControl
{
	public AsciiContentView(WinUIHexView view, ViewScroller viewScroller, VisualTheme theme) : base()
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

		_view.SelectionManager.SelectionChanged += OnSelectionChanged;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
	}

	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly Brush _editorForegroundBrush = new SolidColorBrush(Colors.Black);
	private readonly WinUIHexView _view;
	private readonly VisualTheme _theme;

	private Path? _selectionPath;
	private readonly Brush _selectionBackground = new SolidColorBrush(Color.FromArgb(255, 153, 201, 239));
	private SnapshotPoint? _anchorPoint;

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
					IsHitTestVisible = false,
				};
				Canvas.SetTop(rowCanvas, Math.Round(row.VisualBounds.Top));

				foreach (var run in row.AsciiRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _theme.FontFamily,
						FontSize = _theme.FontSize,
						Foreground = _editorForegroundBrush,
						IsTextSelectionEnabled = false,
						IsHitTestVisible = false,
						TextWrapping = TextWrapping.NoWrap,
						TextTrimming = TextTrimming.None,
						TextAlignment = TextAlignment.Left,
						Tag = run,
					};
					if (run.Style is WinUITextRunStyle style)
					{
						if (style.Background is not null)
						{
							var rectangle = new Rectangle()
							{
								Width = run.RenderedWidth,
								Height = _theme.RowHeight,
								Fill = style.Background,
								IsHitTestVisible = false,
							};
							if (style.Opacity is not null)
							{
								rectangle.Opacity = style.Opacity.Value;
							}
							Canvas.SetZIndex(rectangle, -2);
							Canvas.SetLeft(rectangle, Math.Round(run.LeftPosition));
							rowCanvas.Children.Add(rectangle);
						}
						if (style.Foreground is not null)
						{
							hexTextBlock.Foreground = style.Foreground;
						}
						if (style.FontWeight is not null)
						{
							hexTextBlock.FontWeight = style.FontWeight.Value;
						}
						if (style.Opacity is not null)
						{
							hexTextBlock.Opacity = style.Opacity.Value;
						}
					}
					Canvas.SetLeft(hexTextBlock, Math.Round(run.LeftPosition));
					rowCanvas.Children.Add(hexTextBlock);
				}

				_canvas.Children.Add(rowCanvas);
			}
		});
	}

	#region Selection
	private void OnSelectionChanged(object? sender, Selection.SelectionChangedEventArgs e)
	{
		if (e.Selection == null)
		{
			_selectionPath?.Visibility = Visibility.Collapsed;
			return;
		}

		if (_selectionPath == null)
		{
			_selectionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				Fill = _selectionBackground,
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_selectionPath, -1);
			_canvas.Children.Add(_selectionPath);
		}

		var points = _view.MapToVisualAscii(e.Selection.Span);
		var figure = ((PathGeometry)_selectionPath.Data).Figures[0];
		figure.Fill(points);
		_selectionPath.Visibility = Visibility.Visible;
	}

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_anchorPoint = _view.MapFromVisualAscii(_view.MapViewportToVisual(e.GetCurrentPoint(this).Position));
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_anchorPoint is SnapshotPoint anchorPoint)
		{
			var activePoint = _view.MapFromVisualAscii(_view.MapViewportToVisual(e.GetCurrentPoint(this).Position));
			_view.SelectionManager.Select(anchorPoint, activePoint);
		}
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_anchorPoint = null;
	}
	#endregion

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
