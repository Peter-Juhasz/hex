using HexEditor.Formats;
using HexEditor.Model;
using HexEditor.Structure;
using HexEditor.ViewModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.UI;

namespace HexEditor.WinUI;

internal class OutliningMargin : ContentControl
{
	public OutliningMargin(WinUIHexView view, HexContentView editorScrollView, VisualTheme visualTheme) : base()
	{
		_theme = visualTheme;
		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

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
			Width = 16,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		_view.ScrollableHeightChanged += OnViewHeightChanged;

		var scrollOptions = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore);
		editorScrollView.ViewChanged += (s, e) =>
		{
			_scrollView.ScrollTo(0, e.VerticalOffset, scrollOptions);
		};
	}

	private readonly VisualTheme _theme;

	private void AddRegion(StructureSpan span)
	{
		var startRowTop = _view.MapToVisual(span.FullExtent.Start).Y;
		var endRowTop = _view.MapToVisual(span.FullExtent.End).Y;
		if (startRowTop == endRowTop)
		{
			return;
		}

		var startOffset = startRowTop + _theme.RowHeight / 2;
		var endRowBottom = endRowTop + _theme.RowHeight;
		var height = endRowBottom - startRowTop;

		var line = new Path()
		{
			Data = new PathGeometry()
			{
				Figures = 
				[
					new PathFigure()
					{
						StartPoint = new(_canvas.XamlRoot.SnapToPixels(16), _canvas.XamlRoot.SnapToPixels(0)),
						Segments =
						[
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(8), _canvas.XamlRoot.SnapToPixels(0)),
							},
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(8), _canvas.XamlRoot.SnapToPixels(height)),
							},
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(16), _canvas.XamlRoot.SnapToPixels(height)),
							},
						],
					}
				],
			},
			Stroke = _strokeBrush,
			StrokeThickness = 1,
			Width = 16,
			Height = height,
			Tag = span,
		};
		Canvas.SetTop(line, startRowTop);
		Canvas.SetLeft(line, 0);
		if (span.Label != null)
		{
			ToolTipService.SetToolTip(line, span.Label);
		}
		_canvas.Children.Add(line);
	}

	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly Brush _strokeBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly WinUIHexView _view;

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly IStructureProvider structureProvider = new MidiStructureProvider();

	private void OnViewVisibleRowsChanged(object sender, VisibleRowsChangedEventArgs e)
	{
		_queue.Enqueue(async c =>
		{
			// remove regions that are no longer visible
			if (!e.RemovedRows.IsEmpty)
			{
				for (int i = 0; i < _canvas.Children.Count; i++)
				{
					var child = _canvas.Children[i];
					if (child is Path { Tag: StructureSpan span } path)
					{
						bool toRemove = true;
						foreach (var row in e.RemovedRows)
						{
							if (span.FullExtent.Snapshot == row.Extent.Snapshot &&
								span.FullExtent.Span.IntersectsWith(row.Extent.Span)
							)
							{
								toRemove = false;
								break;
							}
						}
						if (toRemove)
						{
							_canvas.Children.RemoveAt(i);
							i--;
						}
					}
				}
			}

			// recompute and add regions for newly visible rows
			if (!e.AddedRows.IsEmpty)
			{
				var newSpan = SnapshotSpan.Create(e.AddedRows[0].Extent.Start, e.AddedRows[^1].Extent.End);

				try
				{
					// get structure
					var structures = await structureProvider.GetStructureSpansAsync(newSpan, c);
					foreach (var newStructure in structures)
					{
						// check if we already have this region
						var exists = false;
						for (int i = 0; i < _canvas.Children.Count; i++)
						{
							var child = _canvas.Children[i];
							if (child is Path { Tag: StructureSpan span } path)
							{
								if (span.FullExtent == newStructure.FullExtent && span.Label == newStructure.Label)
								{
									exists = true;
									break;
								}
							}
						}

						if (exists)
						{
							break;
						}

						// add region
						AddRegion(newStructure);
					}
				}
				catch (Exception ex)
				{

				}
			}
		});
	}

	private void OnViewHeightChanged(object sender, HeightChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			_canvas.Height = e.NewHeight;
		});
	}
}
