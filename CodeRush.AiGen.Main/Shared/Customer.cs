namespace CodeRush.AiGen.Main.Shared {
    public sealed class Customer {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public bool IsTaxExempt { get; init; }
        public bool IsTaxExemptOverrideEligible { get; init; } // used in the delta example
        public Address? BillingAddress { get; init; }
    }
}
