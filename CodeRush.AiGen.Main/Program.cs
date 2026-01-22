using CodeRush.AiGen.Main.DebugRuntimeState;

namespace CodeRush.AiGen.Main {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");

            var formatter = new OrderAddressFormatter();
            var order = CreateOrderThatTriggersTheBug();

            // Set a breakpoint on the next line:
            string label = formatter.BuildShippingLabel(order);

            Console.WriteLine(label);
        }

        static Order CreateOrderThatTriggersTheBug() {
            return new Order {
                OrderId = "DBG-1001",
                Subtotal = 120m,
                DiscountAmount = 10m,
                Customer = new Customer {
                    Id = "C-42",
                    DisplayName = "Ada Lovelace",
                    IsTaxExempt = false,
                    IsTaxExemptOverrideEligible = true,
                    BillingAddress = new Address {
                        Line1 = "123 Example St",
                        City = "Seattle",
                        Region = " ",              // INTENTIONAL: whitespace triggers the bug
                        PostalCode = "98101",
                        CountryCode = "US"
                    }
                }
            };
        }
    }
}
