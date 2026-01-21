using System;
using CodeRush.AiGen.Main.Shared;

namespace CodeRush.AiGen.Main.FineGrainedDeltas;

/// <summary>
/// Calculates tax for orders. This file is intentionally written to support AiGen examples.
/// </summary>
public sealed class OrderTaxCalculator {
    private const decimal DefaultTaxRate = 0.0825m;

    /// <summary>
    /// Logs the timestamp, total orders, and the inspected/skipped counts.
    /// </summary>
    private void LogCounts(IReadOnlyList<Order> orders, int inspected, int skipped) {
        int total = orders?.Count ?? 0;
        string timestamp = DateTime.UtcNow.ToString("o");
        string message = $"[{timestamp}] OrdersTotal={total}, Inspected={inspected}, Skipped={skipped}";
        Console.WriteLine(message);
    }

    /// <summary>
    /// Computes tax for each order and returns the number of orders updated.
    /// </summary>
    public int ComputeTaxes(IReadOnlyList<Order> orders) {
        if (orders is null)
            throw new ArgumentNullException(nameof(orders));

        int updated = 0;

        decimal taxRate = DefaultTaxRate;
        int inspected = 0;
        int skipped = 0;

        for (int i = 0; i < orders.Count; i++) {
            inspected++;

            Order? order = orders[i];
            if (order is null) {
                skipped++;
                continue;
            }

            Customer? customer = order.Customer;
            if (customer is null) {
                skipped++;
                continue;
            }

            if (order.Subtotal <= 0m) {
                order.TaxAmount = 0m;
                skipped++;
                continue;
            }

            if (customer.IsTaxExempt) {
                order.TaxAmount = 0m;
                updated++;
                continue;
            }

            // TODO: Promotional discounts are normally non-taxable, but some customers are override-eligible => tax must be computed.

            decimal taxableBase = order.Subtotal - order.DiscountAmount;
            if (taxableBase < 0m) {
                taxableBase = 0m;
            }

            decimal computed = Math.Round(taxableBase * taxRate, 2, MidpointRounding.AwayFromZero);

            // Avoid negative tax and avoid churn if unchanged.
            if (computed < 0m)
                computed = 0m;

            if (order.TaxAmount != computed) {
                order.TaxAmount = computed;
                updated++;
            }
        }

        LogCounts(orders, inspected, skipped);

        return updated;
    }
}
