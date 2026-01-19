using HexEditor.Core.Tagging;
using HexEditor.Model;
using HexEditor.Structure;
using HexEditor.ViewModel;
using HexEditor.WinUI.Scrolling;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Immutable;
using Windows.UI;

namespace HexEditor.WinUI.Outlining;

internal sealed class OutliningMargin : ContentControl
{
	public OutliningMargin(WinUIHexView view, ViewScroller viewScroller, VisualTheme visualTheme, ReflectionTaggerProvider taggerProvider, string contentType) : base()
	{
		_theme = visualTheme;
		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

		var taggers = taggerProvider.CreateTaggers<StructureTag>(contentType).ToImmutableArray();
		tagAggregator = new FullCachingTagAggregator<StructureTag>(new ParallelTagAggregator<StructureTag>(taggers));

		_canvas = new Canvas
		{
			Width = _width,
		};
		this.Content = _canvas;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		viewScroller.ScrollableHeightChanged += OnScrollableHeightChanged;
	}

	private readonly double _width = 14;
	private readonly VisualTheme _theme;

	private readonly Canvas _canvas;

	private readonly Brush _strokeBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly Brush _transparentBrush = new SolidColorBrush(Colors.Transparent);
	private readonly Brush _pointerOverBrush = new SolidColorBrush(Color.FromArgb(255, 235, 238, 244));
	private readonly WinUIHexView _view;

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly ITagAggregator<StructureTag> tagAggregator;

	public event EventHandler<OutliningRegionSelectionRequestedEventArgs>? OutliningRegionSelectionRequested;
	public event EventHandler<EventArgs>? OutliningRegionDismissRequested;

	private void AddRegion(TagSpan<StructureTag> span)
	{
		var startRowTop = _view.MapToVisualHex(span.Span.Start).Y;
		var endRowTop = _view.MapToVisualHex(span.Span.End).Y;
		if (startRowTop == endRowTop)
		{
			return;
		}

		var startOffset = startRowTop + _theme.RowHeight / 2;
		var endRowBottom = endRowTop + _theme.RowHeight;
		var height = Math.Round(endRowBottom - startRowTop);

		var canvas = new Canvas()
		{
			Width = _width,
			Height = height,
			Tag = span,
			Background = _transparentBrush,
			IsHitTestVisible = true,
		};
		Canvas.SetTop(canvas, Math.Round(startRowTop));
		Canvas.SetLeft(canvas, 0);
		if (span.Tag.Label != null)
		{
			ToolTipService.SetToolTip(canvas, span.Tag.Label);
		}

		var line = new Path()
		{
			Data = new PathGeometry()
			{
				Figures =
				[
					new PathFigure()
					{
						IsFilled = false,
						IsClosed = false,
						StartPoint = new(_canvas.XamlRoot.SnapToPixels(_width), 0),
						Segments =
						[
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(_width / 2), 0),
							},
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(_width / 2), height),
							},
							new LineSegment()
							{
								Point = new(_canvas.XamlRoot.SnapToPixels(_width), height),
							},
						],
					}
				],
			},
			Width = _width,
			Height = height,
			Stroke = _strokeBrush,
			StrokeThickness = 1,
		};
		canvas.Children.Add(line);

		canvas.PointerEntered += OnPointerEntered;
		canvas.PointerExited += OnPointerExited;

		// insert in order
		var insertionIndex = 0;
		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			var child = _canvas.Children[i];
			if (child is Canvas { Tag: TagSpan<StructureTag> existingSpan })
			{
				if (span.Span.Start.Position < existingSpan.Span.Start.Position)
				{
					break;
				}
			}
			insertionIndex++;
		}
		_canvas.Children.Insert(insertionIndex, canvas);
	}

	private void OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		var line = (Canvas)sender;
		line.Background = _transparentBrush;
		OutliningRegionDismissRequested?.Invoke(this, new());
	}

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		var line = (Canvas)sender;
		line.Background = _pointerOverBrush;
		OutliningRegionSelectionRequested?.Invoke(this, new OutliningRegionSelectionRequestedEventArgs((TagSpan<StructureTag>)line.Tag));
	}

	private void OnViewVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		_queue.Enqueue(async c =>
		{
			// remove regions that are no longer visible
			if (!e.RemovedRows.IsEmpty)
			{
				DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
				{
					for (int i = 0; i < _canvas.Children.Count; i++)
					{
						var child = _canvas.Children[i];
						if (child is Canvas { Tag: TagSpan<StructureTag> span })
						{
							bool toRemove = true;
							foreach (var row in e.RemovedRows)
							{
								if (span.Span.Snapshot == row.Extent.Snapshot &&
									span.Span.Span.IntersectsWith(row.Extent.Span)
								)
								{
									toRemove = false;
									break;
								}
							}
							if (toRemove)
							{
								_canvas.Children.RemoveAt(i);
								child.PointerEntered -= OnPointerEntered;
								child.PointerExited -= OnPointerExited;
								i--;
							}
						}
					}
				});
			}

			// recompute and add regions for newly visible rows
			if (!e.AddedRows.IsEmpty)
			{
				var newSpan = SnapshotSpan.Create(e.AddedRows[0].Extent.Start, e.AddedRows[^1].Extent.End);

				try
				{
					// get structure
					var structures = await tagAggregator.GetTagsAsync(newSpan, c).ConfigureAwait(false);
					DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
					{
						if (c.IsCancellationRequested)
						{
							return;
						}

						foreach (var newStructure in structures)
						{
							// check if we already have this region
							var exists = false;
							for (int i = 0; i < _canvas.Children.Count; i++)
							{
								var child = _canvas.Children[i];
								if (child is Canvas { Tag: TagSpan<StructureTag> span })
								{
									if (span.Span == newStructure.Span && span.Tag == newStructure.Tag)
									{
										exists = true;
										break;
									}
								}
							}

							if (exists)
							{
								continue;
							}

							// add region
							AddRegion(newStructure);
						}
					});
				}
				catch (OperationCanceledException) when (c.IsCancellationRequested)
				{
					// ignore
				}
				catch (Exception ex)
				{
					// TODO
				}
			}
		});
	}

	#region Scrolling
	private void OnScrollableHeightChanged(object? sender, ScrollableHeightChangedEventArgs e)
	{
		_canvas.Height = e.NewHeight;
	}
	#endregion
}

public class OutliningRegionSelectionRequestedEventArgs(TagSpan<StructureTag> span) : EventArgs
{
	public TagSpan<StructureTag> Span { get; } = span;
}
