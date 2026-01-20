namespace HexEditor.Model;

public interface ISnapshotManager
{
    IBinarySnapshot CurrentSnapshot { get; }

    void ApplyChange(BinaryChange change);

    void Undo();

    void Save();
}
