using HexEditor.Model;

namespace HexEditor.ViewModel;

public interface IViewBuffer
{
	IBinaryBuffer DataBuffer { get; }

	bool TryRead(long offset, int length, out ReadOnlyMemory<byte> data);

	Task LoadChunkAsync(long offset, int length, CancellationToken cancellationToken);
}

public static partial class Extensions
{
	extension(IViewBuffer @this)
	{
		public bool TryReadByte(long offset, out byte value)
		{
			if (@this.TryRead(offset, 1, out var data))
			{
				value = data.Span[0];
				return true;
			}

			value = 0;
			return false;
		}

		public bool TryCopyTo(long offset, Span<byte> destination)
		{
			if (@this.TryRead(offset, destination.Length, out var data))
			{
				data.Span.CopyTo(destination);
				return true;
			}

			return false;
		}

		public bool TryRead(Range range, out ReadOnlyMemory<byte> data)
		{
			var (offset, length) = range.GetOffsetAndLength((int)@this.DataBuffer.Length);
			return @this.TryRead(offset, length, out data);
		}


		public Task LoadChunkAsync(Range range, CancellationToken cancellationToken)
		{
			var (offset, length) = range.GetOffsetAndLength((int)@this.DataBuffer.Length);
			return @this.LoadChunkAsync(offset, length, cancellationToken);
		}
	}
}