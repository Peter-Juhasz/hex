using HexEditor.Core.Caret;
using HexEditor.Core.Model;
using HexEditor.Core.Scrolling;
using HexEditor.Core.Selection;
using HexEditor.Core.SelectionMatchHighlight;
using HexEditor.Core.ViewModel;
using HexEditor.Model;

namespace HexEditor.Core.Tests.SelectionMatchHighlight;

[TestClass]
public class SelectionMatchHighlightTaggerTests
{
	private class ByteArrayDataSource(byte[] data) : IBinaryDataSource
	{
		public long Length => data.Length;

		public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
		{
			data.AsMemory((int)offset, destination.Length).CopyTo(destination);
			return ValueTask.CompletedTask;
		}
	}

	private class MockSelection : ISelection
	{
		public SelectionSpan? Span { get; set; }

		public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

		public void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint) { }
		public void Select(SnapshotSpan span, bool isReversed = false) { }
		public void MoveActivePointLeft() { }
		public void MoveActivePointRight() { }
		public void MoveActivePointUpByRow() { }
		public void MoveActivePointDownByRow() { }
		public void MoveActivePointToHome() { }
		public void MoveActivePointToEnd() { }
		public void MoveActivePointToRowStart() { }
		public void MoveActivePointToRowEnd() { }
		public void SelectAll() { }
		public void Clear() { }
		public void SelectTo(SnapshotPoint anchorPoint) { }
	}

	private class MockView : IGraphicalHexView
	{
		public required IBinarySnapshot Snapshot { get; init; }
		public required ISelection Selection { get; init; }
		public ICaret Caret => throw new NotImplementedException();
		public SnapshotSpan VisibleSpan => throw new NotImplementedException();
		public System.Collections.Immutable.ImmutableArray<IHexViewRow> VisibleRows => throw new NotImplementedException();
		public IViewport Viewport => throw new NotImplementedException();
		public double ScrollableHeight => throw new NotImplementedException();

		public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;
		public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

		public SnapshotSpan GetContainingRow(SnapshotPoint point) => throw new NotImplementedException();
		public System.Collections.Immutable.ImmutableArray<SnapshotSpan> GetRowSegments(SnapshotSpan span) => throw new NotImplementedException();
		public SnapshotPoint MapFromVisualAscii(System.Numerics.Vector2 point) => throw new NotImplementedException();
		public SnapshotPoint MapFromVisualHex(System.Numerics.Vector2 point) => throw new NotImplementedException();
		public SnapshotSpan MapRowFromVisual(double verticalOffset) => throw new NotImplementedException();
		public long MapRowIndexFromVerticalOffset(double verticalOffset) => throw new NotImplementedException();
		public double MapRowIndexToVerticalOffset(long rowIndex) => throw new NotImplementedException();
		public ViewportBounds MapToVisualAscii(SnapshotPoint point) => throw new NotImplementedException();
		public System.Numerics.Vector2[] MapToVisualAscii(SnapshotSpan span) => throw new NotImplementedException();
		public ViewportBounds MapToVisualHex(SnapshotPoint point) => throw new NotImplementedException();
		public System.Numerics.Vector2[] MapToVisualHex(SnapshotSpan span) => throw new NotImplementedException();
	}

	private class MockViewAccessor : IViewAccessor
	{
		public required IGraphicalHexView View { get; init; }
	}

	[TestMethod]
	public async Task GetTagsAsync_NoSelection_ReturnsEmpty()
	{
		// Arrange
		var dataSource = new ByteArrayDataSource([1, 2, 3, 1, 2, 3]);
		var manager = new SnapshotManager(dataSource);
		var snapshot = manager.CurrentSnapshot;

		var selection = new MockSelection { Span = null };
		var view = new MockView { Snapshot = snapshot, Selection = selection };
		var viewAccessor = new MockViewAccessor { View = view };

		var tagger = new SelectionMatchHighlightTagger(viewAccessor);

		var span = new SnapshotSpan(snapshot, new LongSpan(0, snapshot.Length));

		// Act
		var tags = await tagger.GetTagsAsync(span, CancellationToken.None);

		// Assert
		Assert.AreEqual(0, tags.Length);
	}

	[TestMethod]
	public async Task GetTagsAsync_WithSelection_FindsMatches()
	{
		// Arrange
		var dataSource = new ByteArrayDataSource([1, 2, 3, 1, 2, 3, 4, 5, 1, 2, 3]);
		var manager = new SnapshotManager(dataSource);
		var snapshot = manager.CurrentSnapshot;

		var selection = new MockSelection();
		var view = new MockView { Snapshot = snapshot, Selection = selection };
		
		var selectionManager = new SelectionManager(view);
		var selectionSpan = new SelectionSpan(
			selectionManager,
			new SnapshotPoint(snapshot, 0),
			new SnapshotPoint(snapshot, 3)
		);
		selection.Span = selectionSpan;

		var viewAccessor = new MockViewAccessor { View = view };

		var tagger = new SelectionMatchHighlightTagger(viewAccessor);

		var span = new SnapshotSpan(snapshot, new LongSpan(0, snapshot.Length));

		// Act
		var tags = await tagger.GetTagsAsync(span, CancellationToken.None);

		// Assert
		Assert.AreEqual(3, tags.Length); // Should find 3 matches of [1, 2, 3]
		Assert.AreEqual(0, tags[0].Span.Span.StartOffset);
		Assert.AreEqual(3, tags[1].Span.Span.StartOffset);
		Assert.AreEqual(8, tags[2].Span.Span.StartOffset);
	}

	[TestMethod]
	public async Task GetTagsAsync_EmptySelection_ReturnsEmpty()
	{
		// Arrange
		var dataSource = new ByteArrayDataSource([1, 2, 3]);
		var manager = new SnapshotManager(dataSource);
		var snapshot = manager.CurrentSnapshot;

		var selection = new MockSelection();
		var view = new MockView { Snapshot = snapshot, Selection = selection };
		
		var selectionManager = new SelectionManager(view);
		var selectionSpan = new SelectionSpan(
			selectionManager,
			new SnapshotPoint(snapshot, 0),
			new SnapshotPoint(snapshot, 0)
		);
		selection.Span = selectionSpan;

		var viewAccessor = new MockViewAccessor { View = view };

		var tagger = new SelectionMatchHighlightTagger(viewAccessor);

		var span = new SnapshotSpan(snapshot, new LongSpan(0, snapshot.Length));

		// Act
		var tags = await tagger.GetTagsAsync(span, CancellationToken.None);

		// Assert
		Assert.AreEqual(0, tags.Length);
	}

	[TestMethod]
	public async Task GetTagsAsync_MatchAtBufferBoundary_FindsMatch()
	{
		// Arrange - Create data that will span across buffer boundaries
		// Buffer size is 64KB, so create data larger than that with a pattern at the boundary
		var bufferSize = 64 * 1024;
		var pattern = new byte[] { 0xAA, 0xBB, 0xCC };
		var data = new byte[bufferSize + 100];
		
		// Fill with zeros
		Array.Fill(data, (byte)0);
		
		// Place pattern at buffer boundary (just before, at, and after 64KB mark)
		Array.Copy(pattern, 0, data, bufferSize - 2, pattern.Length); // Pattern spans boundary
		Array.Copy(pattern, 0, data, bufferSize + 10, pattern.Length); // Pattern after boundary
		
		var dataSource = new ByteArrayDataSource(data);
		var manager = new SnapshotManager(dataSource);
		var snapshot = manager.CurrentSnapshot;

		var selection = new MockSelection();
		var view = new MockView { Snapshot = snapshot, Selection = selection };
		
		var selectionManager = new SelectionManager(view);
		var selectionSpan = new SelectionSpan(
			selectionManager,
			new SnapshotPoint(snapshot, bufferSize - 2),
			new SnapshotPoint(snapshot, bufferSize - 2 + pattern.Length)
		);
		selection.Span = selectionSpan;

		var viewAccessor = new MockViewAccessor { View = view };
		var tagger = new SelectionMatchHighlightTagger(viewAccessor);

		var span = new SnapshotSpan(snapshot, new LongSpan(0, snapshot.Length));

		// Act
		var tags = await tagger.GetTagsAsync(span, CancellationToken.None);

		// Assert - Should find both matches
		Assert.AreEqual(2, tags.Length);
		Assert.AreEqual(bufferSize - 2, tags[0].Span.Span.StartOffset);
		Assert.AreEqual(bufferSize + 10, tags[1].Span.Span.StartOffset);
	}
}
