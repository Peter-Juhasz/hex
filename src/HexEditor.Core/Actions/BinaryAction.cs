using HexEditor.Model;

namespace HexEditor.Core.Actions;

public record class BinaryAction(
	string Title,
	BinaryEdit Edit
);
