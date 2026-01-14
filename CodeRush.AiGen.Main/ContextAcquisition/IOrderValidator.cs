using CodeRush.AiGen.Main.Shared;

namespace CodeRush.AiGen.Main.ContextAcquisition;

/// <summary>
/// Validates orders before submission.
/// </summary>
public interface IOrderValidator {
    /// <summary>
    /// Validates the order and returns a failure reason if invalid.
    /// </summary>
    ValidationResult Validate(Order order);
}
