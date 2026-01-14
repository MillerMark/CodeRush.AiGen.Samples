namespace CodeRush.AiGen.Main.Shared;

public sealed class Order {
    public string? OrderId { get; init; }
    public Customer? Customer { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }  // used in the delta example
    public bool HasDiscount => DiscountAmount > 0m;
    public decimal TaxAmount { get; set; }        // computed/assigned during processing
}
