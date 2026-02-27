namespace CodeRush.AiGen.Main.ContextAcquisition;

public sealed class OrderValidator : BaseValidator<Order>, IOrderValidator {
    public OrderValidator(bool stopOnFirstError = true) : base(stopOnFirstError) { }

    protected override void ValidateCore(Order order, ValidationResult result) {
        if (order is null) {
            result.Add("Order is required.");
            return;
        }

        // TODO: Ask AiGen to consolidate this based on what we have in the base class.

        var customer = order.Customer;

        if (customer is null) {
            result.Add("Customer is required.");
            if (StopOnFirstError) return;
        }

        if (customer?.BillingAddress is null) {
            result.Add("Billing address is required.");
            if (StopOnFirstError) return;
        }

        if (string.IsNullOrWhiteSpace(customer?.BillingAddress?.CountryCode)) {
            result.Add("Billing address country is required.");
            if (StopOnFirstError) return;
        }

        if (string.IsNullOrWhiteSpace(order.OrderId)) {
            result.Add("OrderId is required.");
        }
    }








    //`![](C36AAB5D42A77F8CC8C3AC554BCE40DD.png;crcommand:OpenFile:OrderTaxCalculator.cs;;0.01309,0.01309)

}
