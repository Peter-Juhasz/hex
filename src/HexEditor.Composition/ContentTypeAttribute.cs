namespace HexEditor.Composition;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ContentTypeAttribute(string type) : Attribute
{
	public string Type { get; } = type;
}