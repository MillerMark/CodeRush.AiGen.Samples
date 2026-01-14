namespace CodeRush.AiGen.Main.ContextAcquisition;

public sealed class ValidationResult {
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public void Add(string error) {
        if (!string.IsNullOrWhiteSpace(error))
            Errors.Add(error);
    }

    public string? FirstErrorOrNull() => Errors.Count > 0 ? Errors[0] : null;
}
