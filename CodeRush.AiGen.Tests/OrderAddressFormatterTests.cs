using CodeRush.AiGen.Main.Shared;
using CodeRush.AiGen.Main.DebugRuntimeState;

namespace CodeRush.AiGen.Tests;

public class OrderAddressFormatterTests {
    [Fact]
    public void BuildShippingLabel_Simple_ReturnsExpected() {
        // Arrange
        var address = new Address { City = "Seattle", Region = "WA", PostalCode = "98101" };
        var customer = new Customer { DisplayName = "Ada Smith", BillingAddress = address, DiscountPolicy = Customer.Discounts.ReduceTaxableBase };
        var order = new Order { Customer = customer };

        var formatter = new OrderAddressFormatter();

        // Act
        string label = formatter.BuildShippingLabel(order);

        // Assert
        Assert.Equal("Ada Smith — Seattle, WA 98101", label);
    }
}






//`![](C36AAB5D42A77F8CC8C3AC554BCE40DD.png;crcommand:OpenFile:IOrderRule.cs;;0.01309,0.01309)
