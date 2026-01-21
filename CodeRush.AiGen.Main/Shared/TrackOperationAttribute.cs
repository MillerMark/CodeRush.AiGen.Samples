namespace CodeRush.AiGen.Main.Shared;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TrackOperationAttribute : Attribute
{
    public TrackOperationAttribute(string name) => Name = name;
    public string Name { get; }
    public string? Category { get; init; }
}