using HexEditor.Core.Actions;
using HexEditor.Core.Caret;
using HexEditor.Core.ContentType;
using HexEditor.Core.Model;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Windows.UI;

namespace HexEditor.WinUI.Actions;

internal sealed class ActionsMargin : Canvas
{
	public ActionsMargin(IGraphicalHexView view, VisualTheme visualTheme, IBinaryActionProvider actionProvider, IContentTypeRegistry contentTypeRegistry) : base()
	{
		_theme = visualTheme;
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
		this.MinWidth = _width;

		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(view.Snapshot.Source.ContentType).Select(t => t.Type).ToImmutableArray();
		_actionProvider = actionProvider;

		_canvas = this;

		_view = view;
		_view.Caret.PositionChanged += OnCaretPositionChanged;
	}

	private readonly double _width = 24;
	private readonly VisualTheme _theme;

	private readonly Canvas _canvas;
	private Button? _button;
	private MenuFlyout? _menuFlyout;

	private readonly IGraphicalHexView _view;

	private readonly BackgroundTaskQueue _queue = new(default);
	private readonly IBinaryActionProvider _actionProvider;

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		var caret = _view.Caret.Position.Point;
		var span = _view.Selection.Span?.Span ?? SnapshotSpan.Create(_view.Caret.Position.Point, 0);
		_queue.Enqueue(async ct =>
		{
			var actions = await _actionProvider.GetActionsAsync(span, ct).ConfigureAwait(false);
			if (ct.IsCancellationRequested)
			{
				return;
			}

			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
			{
				HandleActionsChanged(span, caret, actions, ct);
			});
		});
	}

	private void HandleActionsChanged(SnapshotSpan span, SnapshotPoint caret, ImmutableArray<BinaryAction> actions, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		if (_view.Caret.Position.Point != caret)
		{
			return;
		}

		if (actions.IsEmpty)
		{
			this.Visibility = Visibility.Collapsed;
			return;
		}

		if (_button == null)
		{
			_button = new Button()
			{
				Width = _width,
				Height = _width,
				Padding = new Thickness(2),
				Background = null,
				BorderBrush = null,
				Content = new FontIcon()
				{
					Glyph = "\uEA80",
					FontSize = 16,
					Foreground = new SolidColorBrush(Color.FromArgb(255, 173, 139, 0)),
				},
			};
			_menuFlyout = new MenuFlyout()
			{
				Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
			};
			_button.Flyout = _menuFlyout;

			_canvas.Children.Add(_button);
		}

		var visual = _view.MapToVisualHex(caret);
		Canvas.SetTop(_button, visual.Top);

		_menuFlyout!.Items.Clear();

		foreach (var action in actions)
		{
			_menuFlyout.Items.Add(new MenuFlyoutItem()
			{
				Text = action.Title,
				IsEnabled = true,
			});
		}

		this.Visibility = Visibility.Visible;
	}
}
