using HexEditor.Core.Diagnostics;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Theming;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Immutable;

namespace HexEditor.WinUI.Squiggles;

internal sealed class AsciiSquigglesLayer : Canvas
{
	public AsciiSquigglesLayer(IGraphicalHexView view, ITagAggregator<DiagnosticTag> tagAggregator, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = theme.Columns * theme.FontWidth;

		_diagnosticTagAggregator = tagAggregator;

		_canvas = this;
		_view.VisibleRowsChanged += OnVisibleRowsChanged;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;
	private readonly Brush _errorBrush = new SolidColorBrush(Colors.Red);

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly ITagAggregator<DiagnosticTag> _diagnosticTagAggregator;

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
		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			if (_canvas.Children[i] is Path { Tag: TagSpan<DiagnosticTag> tagSpan } path)
			{
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
					i--;
				}
			}
		}

		foreach (var tagSpan in tags)
		{
			// TODO: split across multiple rows
			var diagnosticTag = tagSpan.Tag;
			var row = _view.GetContainingRow(tagSpan.Span.Start);
			var startColumn = tagSpan.Span.Start - row.Start;
			var endColumn = tagSpan.Span.End - row.Start;
			var asciiPrimaryGrouping = _theme.AsciiViewStyle?.PrimaryGrouping ?? 0;
			var asciiSecondaryGrouping = _theme.AsciiViewStyle?.SecondaryGrouping ?? 0;
			var x1 = IHexViewRow.CalculateAsciiPosition((int)startColumn, _theme.FontWidth, asciiPrimaryGrouping, asciiSecondaryGrouping);
			var x2 = IHexViewRow.CalculateAsciiPosition((int)endColumn, _theme.FontWidth, asciiPrimaryGrouping, asciiSecondaryGrouping);
			var width = x2 - x1;
			var baseline = _view.MapToVisualAscii(row.Start).Bottom - 3d;

			var underline = new Path()
			{
				Data = SquiggleUnderline.BuildGeometry(
					width: width,
					height: 3d,
					strokeThickness: 1d,
					wavelength: 6d
				),
				Stroke = _errorBrush,
				StrokeThickness = 1,
				IsHitTestVisible = false,
				UseLayoutRounding = true,
				Tag = tagSpan,
			};
			Canvas.SetLeft(underline, Math.Round(x1));
			Canvas.SetTop(underline, Math.Round(baseline));
			Canvas.SetZIndex(underline, -1);
			_canvas.Children.Add(underline);
		}
	}
}
