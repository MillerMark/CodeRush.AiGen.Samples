namespace CodeRush.AiGen.Main.ContextAcquisition;

/// <summary>
/// Base class for validators that can either fast-fail or collect all errors.
/// </summary>
public abstract class BaseValidator<T> {
    protected BaseValidator(bool stopOnFirstError = true) {
        StopOnFirstError = stopOnFirstError;
    }

    protected bool StopOnFirstError { get; }

    public ValidationResult Validate(T value) {
        var result = new ValidationResult();
        ValidateCore(value, result);

        // If fast-fail is enabled, keep only the first error.
        if (StopOnFirstError && result.Errors.Count > 1) {
            string first = result.Errors[0];
            result.Errors.Clear();
            result.Errors.Add(first);
        }

        return result;
    }

    protected abstract void ValidateCore(T value, ValidationResult result);

    // Shared helpers that derived validators can (and should) reuse.
    protected void RequireCustomer(Customer? customer, ValidationResult result) {
        if (customer is null) {
            result.Add("Customer is required.");
            if (StopOnFirstError)
                return;
        }

        if (customer?.BillingAddress is null) {
            result.Add("Billing address is required.");
            if (StopOnFirstError)
                return;
        }

        if (string.IsNullOrWhiteSpace(customer?.BillingAddress?.CountryCode)) {
            result.Add("Billing address country is required.");
        }
    }
}
