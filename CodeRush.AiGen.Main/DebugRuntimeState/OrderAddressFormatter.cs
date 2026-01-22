namespace CodeRush.AiGen.Main.DebugRuntimeState;

/// <summary>
/// Formats address strings for display and downstream integrations.
/// Intentionally contains a bug to support AiGen debug-time examples.
/// </summary>
public sealed class OrderAddressFormatter {
    /// <summary>
    /// Builds a single-line label, e.g. "Ada Smith — Seattle, WA 98101".
    /// </summary>
    public string BuildShippingLabel(Order order) {
        if (order is null)
            throw new ArgumentNullException(nameof(order));

        if (order.Customer is null)
            throw new InvalidOperationException("Customer is required.");

        if (order.Customer.BillingAddress is null)
            throw new InvalidOperationException("Billing address is required.");

        string name = order.Customer.DisplayName ?? "(unknown)";
        Address a = order.Customer.BillingAddress;


        // BUG: If Region is null/empty, this produces ugly output like:
        // "Ada Smith — Seattle,  98101" (double spaces / dangling comma)
        //
        // We'll break here with runtime values that trigger the bug, then ask AiGen:
        // "Create a test case for this method based on these debug time parameter values.
        // Add asserts to make sure the label has no double spaces and no dangling comma when region is blank."
        string cityRegionPostal = $"{a.City}, {a.Region} {a.PostalCode}".Trim();

        return $"{name} — {cityRegionPostal}";
    }
}
