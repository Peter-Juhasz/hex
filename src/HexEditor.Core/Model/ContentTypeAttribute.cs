namespace HexEditor.Core.Model;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ContentTypeAttribute(string type) : Attribute
{
	public string Type { get; } = type;
}