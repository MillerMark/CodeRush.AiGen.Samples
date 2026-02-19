namespace CodeRush.AiGen.Main.Shared {
    public sealed class Customer {
        public enum Discounts {
            ReduceTaxableBase,   // discounts are non-taxable (common)
            AreFullyTaxable      // discounts do not reduce taxable base
        }
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public bool IsTaxExempt { get; init; }
        public Discounts DiscountTaxPolicy { get; init; }
        public Address? BillingAddress { get; init; }
    }
}
