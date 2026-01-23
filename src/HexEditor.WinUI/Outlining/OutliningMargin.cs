using HexEditor.Core.ContentType;
using HexEditor.Core.Structure;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Immutable;
using System.Linq;
using Windows.UI;

namespace HexEditor.WinUI.Outlining;

internal sealed class OutliningMargin : Canvas
{
	public OutliningMargin(WinUIHexView view, VisualTheme visualTheme, ITaggerProvider taggerProvider, string contentType, IContentTypeRegistry contentTypeRegistry) : base()
	{
		_theme = visualTheme;
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
		this.MinWidth = _width;

		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(contentType).Select(t => t.Type).ToImmutableArray();
		var taggers = taggerProvider.CreateTaggers<StructureTag>(interestedContentTypes);
		tagAggregator = new FullCachingTagAggregator<StructureTag>(new ParallelTagAggregator<StructureTag>(taggers));

		_canvas = this;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
	}

	private readonly double _width = 10;
	private readonly VisualTheme _theme;

	private readonly Canvas _canvas;

	private readonly Brush _strokeBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly Brush _transparentBrush = new SolidColorBrush(Colors.Transparent);
	private readonly Brush _pointerOverBrush = new SolidColorBrush(Color.FromArgb(255, 235, 238, 244));
	private readonly Brush _chevronBrush = new SolidColorBrush(Colors.Black);
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

		var startOffset = startRowTop + _theme.RowHeight / 2d;
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
			ToolTipService.SetPlacement(canvas, PlacementMode.Right);
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
						StartPoint = new(_width - 2, 0),
						Segments =
						[
							new LineSegment()
							{
								Point = new(_width / 2, 0),
							},
							new LineSegment()
							{
								Point = new(_width / 2, height - 1),
							},
							new LineSegment()
							{
								Point = new(_width - 2, height - 1),
							},
						],
					}
				],
				Transform = null,
			},
			Stretch = Stretch.None,
			Width = _width,
			Height = height,
			Stroke = _strokeBrush,
			Margin = new Thickness(0),
			StrokeThickness = 1,
			IsHitTestVisible = false,
		};
		Canvas.SetTop(line, _theme.RowHeight / 2d);
		Canvas.SetLeft(line, 0);
		canvas.Children.Add(line);

		var chevron = new Path()
		{
			Data = new PathGeometry()
			{
				Figures =
				[
					new PathFigure()
					{
						IsFilled = false,
						IsClosed = false,
						StartPoint = new(0, _theme.RowHeight / 2 - _width / 2),
						Segments =
						[
							new LineSegment()
							{
								Point = new(_width / 2, _theme.RowHeight / 2),
							},
							new LineSegment()
							{
								Point = new(_width, _theme.RowHeight / 2 - _width / 2),
							},
						],
					}
				],
				Transform = null,
			},
			Stretch = Stretch.None,
			Width = _width,
			Height = _theme.RowHeight,
			Stroke = _chevronBrush,
			Margin = new Thickness(0),
			StrokeThickness = 1,
			IsHitTestVisible = false,
		};
		Canvas.SetTop(chevron, 0);
		Canvas.SetLeft(chevron, 0);
		canvas.Children.Add(chevron);

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
		OutliningRegionDismissRequested?.Invoke(this, EventArgs.Empty);
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
}

public class OutliningRegionSelectionRequestedEventArgs(TagSpan<StructureTag> span) : EventArgs
{
	public TagSpan<StructureTag> Span { get; } = span;
}
