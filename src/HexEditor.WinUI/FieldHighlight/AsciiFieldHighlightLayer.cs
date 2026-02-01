using HexEditor.Core.Caret;
using HexEditor.Core.Fields;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Immutable;
using System.Threading;
using Windows.UI;

namespace HexEditor.WinUI.Squiggles;

internal sealed class AsciiFieldHighlightLayer : Canvas
{
	public AsciiFieldHighlightLayer(IGraphicalHexView view, ITagAggregator<FieldTag> tagAggregator, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = view.Columns * 2 * theme.FontWidth;
		this.IsHitTestVisible = false;

		_tagAggregator = tagAggregator;

		_canvas = this;
		_view.VisibleRowsChanged += OnVisibleRowsChanged;
		_view.Caret.PositionChanged += OnCaretPositionChanged;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;
	private readonly Brush _errorBrush = new SolidColorBrush(Color.FromArgb(255, 219, 224, 204));

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly ITagAggregator<FieldTag> _tagAggregator;

	private void OnVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		Invalidate();
	}

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		Invalidate();
	}

	private void Invalidate()
	{
		var visibleSpan = _view.VisibleSpan;
		_queue.Enqueue(async cancellationToken =>
		{
			var tags = await _tagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => Invalidate(tags, cancellationToken));
		});
	}	

	private void Invalidate(ImmutableArray<TagSpan<FieldTag>> tags, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		if (tags.IsEmpty)
		{
			_canvas.Children.Clear();
			this.Visibility = Visibility.Collapsed;
			return;
		}

		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			if (_canvas.Children[i] is Path { Tag: TagSpan<FieldTag> renderTag } path)
			{
				var tagSpan = renderTag;
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
			AddHighlight(tagSpan);
		}
		this.Visibility = Visibility.Visible;
	}

	private Path AddHighlight(TagSpan<FieldTag> tagSpan)
	{
		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			if (_canvas.Children[i] is Path { Tag: TagSpan<FieldTag> renderTag } existingPath)
			{
				if (renderTag.Equals(tagSpan))
				{
					return existingPath;
				}
			}
		}

		var tag = tagSpan.Tag;

		var path = new Path()
		{
			Data = new PathGeometry()
			{
				Figures = [new PathFigure()
				{
					IsFilled = true,
					IsClosed = true,
				}],
			},
			Stroke = _errorBrush,
			StrokeThickness = 1d,
			StrokeLineJoin = PenLineJoin.Miter,
			IsHitTestVisible = false,
			Tag = tagSpan,
		};
		Canvas.SetZIndex(path, -1);
		_canvas.Children.Add(path);

		var points = _view.MapToVisualAscii(tagSpan.Span);
		var figure = ((PathGeometry)path.Data).Figures[0];
		figure.Fill(points);
		return path;
	}
}
