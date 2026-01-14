using CodeRush.AiGen.Main.Shared;

namespace CodeRush.AiGen.Main.InFlightEdits;

/// <summary>
/// Submits orders for processing.
/// </summary>
public sealed class OrderSubmissionService {
    /// <summary>
    /// Submits an order for processing.
    /// </summary>
    public OrderProcessingResult Submit(Order? order) {
        // Check 1 - valid input
        if (order is null)
            throw new ArgumentNullException(nameof(order));

        if (string.IsNullOrWhiteSpace(order.OrderId))
            throw new InvalidOperationException("OrderId is required.");

        if (order.Customer is null)
            throw new InvalidOperationException("Customer is required.");

        // Check 2 - operational prerequisites
        if (order.Customer.BillingAddress is null)
            throw new InvalidOperationException("Billing address is required.");

        if (string.IsNullOrWhiteSpace(order.Customer.BillingAddress.CountryCode))
            throw new InvalidOperationException("Billing address country is required.");

        // Check 3 - operational success
        bool externalAccepted = ExternalGatewayAccepts(order);
        if (!externalAccepted)
            throw new InvalidOperationException("External gateway rejected the order.");

        // All good.
        return OrderProcessingResult.Ok();
    }

    private static bool ExternalGatewayAccepts(Order order) {
        // Deterministic "failure" trigger for repeatability:
        // OrderIds ending with "X" are always rejected.
        string id = order.OrderId ?? string.Empty;
        return !id.EndsWith("X", StringComparison.OrdinalIgnoreCase);
    }
}
