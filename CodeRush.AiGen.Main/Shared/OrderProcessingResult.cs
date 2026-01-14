namespace CodeRush.AiGen.Main.Shared;
public sealed class OrderProcessingResult {
    public bool Success { get; init; }
    public string? FailureReason { get; init; }

    public static OrderProcessingResult Ok() => new() { Success = true };
    public static OrderProcessingResult Fail(string reason) => new() { Success = false, FailureReason = reason };
}