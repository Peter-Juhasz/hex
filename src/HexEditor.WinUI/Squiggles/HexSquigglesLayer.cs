using HexEditor.Core.Diagnostics;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HexEditor.WinUI.Squiggles;

internal sealed class HexSquigglesLayer : Canvas
{
	public HexSquigglesLayer(IGraphicalHexView view, ITagAggregator<DiagnosticTag> tagAggregator, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = (view.Columns * 2) * theme.FontWidth;

		_diagnosticTagAggregator = tagAggregator;

		_canvas = this;
		_view.VisibleRowsChanged += OnVisibleRowsChanged;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly ITagAggregator<DiagnosticTag> _diagnosticTagAggregator;

	private readonly Dictionary<TagSpan<DiagnosticTag>, Path[]> _renderedSegments = new();

	private void OnVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		var visibleSpan = _view.VisibleSpan;
		_queue.Enqueue(async cancellationToken =>
		{
			var tags = await _diagnosticTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => Invalidate(tags));
		});
	}

	private void Invalidate(ImmutableArray<TagSpan<DiagnosticTag>> tags)
	{
		if (tags.IsEmpty)
		{
			_canvas.Children.Clear();
			_renderedSegments.Clear();
			this.Visibility = Visibility.Collapsed;
			return;
		}

		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			if (_canvas.Children[i] is Path { Tag: RenderTag renderTag } path)
			{
				var tagSpan = renderTag.TagSpan;
				bool toRemove = true;
				foreach (var tag in tags)
				{
					if (tagSpan.Equals(tag))
					{
						toRemove = false;
						break;
					}
				}
				if (toRemove)
				{
					_canvas.Children.RemoveAt(i);
					_renderedSegments.Remove(tagSpan);
					i--;
				}
			}
		}

		foreach (var tagSpan in tags)
		{
			using var builder = new PooledArrayBuilder<Path>();

			foreach (var segment in _view.GetRowSegments(tagSpan.Span))
			{
				builder.Add(AddSquiggle(tagSpan, segment));
			}

			_renderedSegments[tagSpan] = builder.ToArray();
		}
		this.Visibility = Visibility.Visible;
	}

	private Path AddSquiggle(TagSpan<DiagnosticTag> tagSpan, SnapshotSpan segment)
	{
		if (_renderedSegments.TryGetValue(tagSpan, out var paths))
		{
			foreach (var path in paths)
			{
				if (path.Tag is RenderTag renderTag &&
					renderTag.TagSpan == tagSpan &&
					renderTag.Segment == segment
				)
				{
					return path;
				}
			}
		}

		var diagnosticTag = tagSpan.Tag;
		var style = _theme.SquigglesMap?.GetValueOrDefault(diagnosticTag.Descriptor.Severity);

		var row = _view.GetContainingRow(segment.Start);
		var startColumn = segment.Start - row.Start;
		var endColumn = segment.End - row.Start;
		if (startColumn == endColumn)
		{
			endColumn++;
		}

		var primaryGrouping = _theme.HexView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.HexView?.SecondaryGrouping ?? 0;
		var x1 = IHexViewRow.GetVisualLeftOfHexColumn((int)startColumn, _theme.FontWidth, primaryGrouping, secondaryGrouping);
		var x2 = IHexViewRow.GetVisualRightOfHexColumn((int)endColumn - 1, _theme.FontWidth, primaryGrouping, secondaryGrouping);
		var width = x2 - x1;
		var baseline = _view.MapToVisualHex(row.Start).Bottom - 3d;

		var underline = new Path()
		{
			Data = SquiggleUnderline.BuildGeometry(
				width: width,
				height: 3d,
				strokeThickness: style?.StrokeThickness ?? 1d,
				wavelength: 6d
			),
			Stroke = style?.Stroke,
			StrokeThickness = style?.StrokeThickness ?? 1d,
			IsHitTestVisible = false,
			UseLayoutRounding = true,
			Tag = new RenderTag(tagSpan, segment),
		};
		Canvas.SetLeft(underline, Math.Round(x1));
		Canvas.SetTop(underline, Math.Round(baseline));
		Canvas.SetZIndex(underline, -1);
		_canvas.Children.Add(underline);
		return underline;
	}

	private sealed record class RenderTag(TagSpan<DiagnosticTag> TagSpan, SnapshotSpan Segment);
}
