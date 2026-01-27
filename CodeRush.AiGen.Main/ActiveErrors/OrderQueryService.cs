// Demo toggle:
// TODO: Uncomment the line below to intentionally introduce a compiler error on the `where` clause, below.
//#define DEMO_ACTIVE_ERRORS

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace CodeRush.AiGen.Main.ActiveErrorAnalysis;

/// <summary>
/// Provides query-oriented operations over orders.
/// This class contains an intentionally-introduced compiler error for AiGen demo purposes.
/// </summary>
public sealed class OrderQueryService {


#if DEMO_ACTIVE_ERRORS
    // TODO: Put the caret on the Where(...) call below and ask AiGen to *"Fix this."*
    public async Task<int> GetHighValueOrderCountAsync(decimal minSubtotal) {
        return GetOrdersAsync()
            .Where(o => o.Subtotal >= minSubtotal)
            .Count();
    }
#endif

    public async Task<List<Order>> GetOrdersAsync() {
        // Simulated async data fetch.
        await Task.Delay(50).ConfigureAwait(false);

        return new List<Order> {
            new() { OrderId = "A-1001", Subtotal = 120m },
            new() { OrderId = "A-1002", Subtotal = 85m },
            new() { OrderId = "A-1003", Subtotal = 210m }
        };
    }
}
