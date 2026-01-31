using HexEditor.Core.Caret;
using HexEditor.Core.ReferenceHighlight;
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

internal sealed class HexReferenceHighlightLayer : Canvas
{
	public HexReferenceHighlightLayer(IGraphicalHexView view, ITagAggregator<ReferenceTag> tagAggregator, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = theme.Columns * 2 * theme.FontWidth;
		this.IsHitTestVisible = false;

		_tagAggregator = tagAggregator;

		_canvas = this;
		_view.VisibleRowsChanged += OnVisibleRowsChanged;
		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;
	private readonly Brush _errorBrush = new SolidColorBrush(Color.FromArgb(255, 219, 224, 204));

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly ITagAggregator<ReferenceTag> _tagAggregator;

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

	private void Invalidate(ImmutableArray<TagSpan<ReferenceTag>> tags, CancellationToken cancellationToken)
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
			if (_canvas.Children[i] is Path { Tag: TagSpan<ReferenceTag> renderTag } path)
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

	private Path AddHighlight(TagSpan<ReferenceTag> tagSpan)
	{
		for (int i = 0; i < _canvas.Children.Count; i++)
		{
			if (_canvas.Children[i] is Path { Tag: TagSpan<ReferenceTag> renderTag } existingPath)
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
			Fill = _errorBrush,
			IsHitTestVisible = false,
			Tag = tagSpan,
		};
		Canvas.SetZIndex(path, -1);
		_canvas.Children.Add(path);

		var points = _view.MapToVisualHex(tagSpan.Span);
		var figure = ((PathGeometry)path.Data).Figures[0];
		figure.Fill(points);
		return path;
	}
}
