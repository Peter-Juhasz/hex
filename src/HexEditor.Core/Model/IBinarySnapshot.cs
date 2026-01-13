namespace HexEditor.Model;

public interface IBinarySnapshot
{
	ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	IBinarySnapshot Apply(BinaryChange change);
}
