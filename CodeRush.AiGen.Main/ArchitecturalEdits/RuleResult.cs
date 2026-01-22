namespace CodeRush.AiGen.Main.ArchitecturalEdits;

public sealed class RuleResult {
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }

    public static RuleResult Success(string? message = null) =>
        new() { IsSuccess = true, Message = message };

    public static RuleResult Failure(string? message = null) =>
        new() { IsSuccess = false, Message = message };
}
