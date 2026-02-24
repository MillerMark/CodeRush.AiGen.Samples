# CodeRush AiGen Samples

This repository contains a small C# solution that demonstrates **CodeRush’s AiGen** capabilities inside Visual Studio.

The examples show how AiGen behaves like a **pair-programming assistant** — using code context, type hierarchy, and runtime state to deliver precise results without requiring verbose prompts.

---

## Prerequisites

- Visual Studio 2022 or higher
- CodeRush 25.23 or higher (with [AiGen](https://community.devexpress.com/Blogs/markmiller/archive/2025/06/24/aigen-amp-aifind-in-coderush-for-visual-studio.aspx) enabled)
- An AI provider configured in CodeRush (OpenAI, Azure OpenAI, etc. -- see [Advanced AiGen Setup](https://community.devexpress.com/Blogs/markmiller/archive/2025/09/09/advanced-ai-setup-for-aigen-and-aifind-in-coderush-for-visual-studio.aspx))

Clone the repo and open:

```
CodeRush.AiGen.Samples.sln
```

## Main Project Layout

```
CodeRush.AiGen.Main
├─ ActiveErrors
├─ ArchitecturalEdits
├─ ContextAcquisition
├─ DebugRuntimeState
├─ FineGrainedDeltas
├─ InFlightEdits
└─ Shared
```

Each folder in `CodeRush.AiGen.Main` corresponds to a specific AiGen capability demonstrated in the blog post. The **Shared** folder contains common models and functionality used across examples. The **InFlightEdits** folder includes examples demonstrating parallel AI agents, conflict isolation, and partial landing of fine-grained deltas.

Additionally, the `CodeRush.AiGen.Tests` project contains a baseline test case and is extended by AiGen in the **DebugRuntimeState** example.

---

## Launching AiGen

To set up AiGen, follow the instructions [here](https://community.devexpress.com/blogs/markmiller/archive/2025/09/08/advanced-ai-setup-for-aigen-and-aifind-in-coderush-for-visual-studio.aspx#general-setup).

Once you've specified API keys and selected your AI model, you can invoke AiGen via voice by **double-tapping** the **right** **Ctrl** key (and holding it down while you speak), or by pressing **Caps**+**G** for a text prompt (if you have [Caps as a Modifier](https://docs.devexpress.com/CodeRushForRoslyn/403629/getting-started/keyboard-shortcuts/caps-as-a-modifier) enabled)

---

## 1. Context Acquisition & Hierarchy-Aware Refactoring

📁Folder: **ContextAcquisition**

Demonstrates how AiGen discovers and incorporates **related types across an inheritance hierarchy** to improve the quality and correctness of the AI coding response.

### Files of interest
- 📄**IOrderValidator.cs**
- 📄**BaseValidator.cs**
- 📄**OrderValidator.cs**

### Scenario
Inside `OrderValidator`, the `ValidateCore()` method contains validation logic that **partially duplicates behavior implemented elsewhere in the type hierarchy**:

```csharp
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
```

If you examine the ancestor class `BaseValidator<T>`, you can see its `RequireCustomer()` method contains somewhat similar code:

```csharp
    protected void RequireCustomer(Customer? customer, ValidationResult result) {
        if (customer is null) {
            result.Add("Customer is required.");
            if (StopOnFirstError)
                return;
        }

        if (customer?.BillingAddress is null) {
            result.Add("Billing address is required.");
            if (StopOnFirstError)
                return;
        }

        if (string.IsNullOrWhiteSpace(customer?.BillingAddress?.CountryCode)) {
            result.Add("Billing address country is required.");
        }
    }
```

Let's use AiGen to consolidate this duplication.

Back in the `OrderValidator` class, place your caret inside the `ValidateCore()` method.

### Example spoken prompts (all equivalent)
**Double-tap** the **right** **Ctrl** key and keep it held down while you say one of the following (or similar):

- _“Consolidate this logic with what we already have in the base class.”_
- _“Take a look at the ancestor class and see if we can reuse any of that code here.”_
- _“Let's use the inherited validation helpers instead of duplicating their logic here.”_

Release the **Ctrl** key when you are done speaking. If you prefer to type this in, press **Caps**+**G** to bring up a prompt window.

AiGen should:
- Traverse the inheritance hierarchy
- Identify reusable base-class validation logic
- Remove duplication while keeping `order`-specific rules local

The ending code might look something like this:

```csharp
    protected override void ValidateCore(Order order, ValidationResult result) {
        if (order is null) {
            result.Add("Order is required.");
            return;
        }

        RequireCustomer(order.Customer, result);
        if (StopOnFirstError && !result.IsValid) return;

        if (string.IsNullOrWhiteSpace(order.OrderId)) {
            result.Add("OrderId is required.");
        }
    }
```

There’s rarely a need to explicitly mention method names, type names, or implementation details. AiGen infers intent from Visual Studio context, the type hierarchy, and surrounding code.

As with any AI-assisted change, review the results. If needed, you can undo (Ctrl+Z) and refine the prompt.

---

## 2. Fine-Grained Deltas (Small Change in a Large Method)

📁Folder: **FineGrainedDeltas**

📄File: **OrderTaxCalculator.cs**

This example shows how AiGen applies **fine-grained deltas** — modifying small, targeted regions inside large methods **without regenerating entire method bodies**.

### Scenario 
Open the **FineGrainedDeltas** folder. Inside `OrderTaxCalculator`'s `ComputeTaxes()` method, there is a TODO describing a business rule:

> // TODO: Apply the customer's discount tax policy when calculating taxableBase.

Place your caret on the `taxableBase` identifier below the `TODO` comment (inside the loop that computes the taxable base).

### Example prompts (all equivalent)
**Double-tap** the **right** **Ctrl** key and keep it held down while you say one of the following (or similar):

- _“Can we check the customer's discount policy so we can correctly calculate the taxable base?”_
- _“Use the customer's discount policy for the right taxable base.”_

Short prompts can also work (with the caret on `taxableBase`):

- _“Let's incorporate the customer discount policy.”_
- _“Check the customer's discount policy.”_

AiGen should:
- Change only the minimal code region required
- Leave the rest of the method untouched
- Apply the update directly in the editor (no manual copy/paste)

AiGen might remove the original assignment to `taxableBase` (e.g., `decimal taxableBase = order.Subtotal - order.DiscountAmount;`) and replace it with something like this:

```csharp
decimal taxableBase = customer.DiscountPolicy switch {
    Customer.Discounts.FullyTaxable => order.Subtotal,
    _ => order.Subtotal - order.DiscountAmount
};
```

or you might get something like this:

```csharp
decimal taxableBase;

// Apply customer's discount tax policy.
if (order.HasDiscount && customer.DiscountPolicy == Customer.Discounts.FullyTaxable) {
    taxableBase = order.Subtotal;
}
else {
    taxableBase = order.Subtotal - order.DiscountAmount;
}
```

Note both the size of the method and how little code AiGen needs to generate to apply the change. 

The prompt doesn’t require exact symbol names or structured references — we describe intent, and AiGen resolves the implementation from surrounding context.

This example demonstrates fine-grained deltas in practice: smaller outputs, lower token usage, reduced latency, and a more immediate turnaround — especially when working inside large methods.

After AiGen applies the change, you can find evidence of the smaller delta in two places:
1. **AiGen Navigator status bar**: Check the **Reasoning out** token count and the **elapsed time** (114 output tokens and 3.2 seconds, respectively, in the screenshot below). Fine-grained deltas yield small output token counts — typically far smaller than the count required to regenerate an entire method.
<img width="713" height="284" alt="image" src="https://github.com/user-attachments/assets/bf81b544-c280-4b47-a5d3-ab4e6cc4e504" />

2. **Editor selection**: Selecting a change in the AiGen Navigator (e.g., "∆ selection") highlights the inserted/modified region, making the delta boundary immediately visible. In the screenshot below, the selection represents the small portion of the method that was regenerated. The rest of the method was untouched by AI.
<img width="607" height="804" alt="image" src="https://github.com/user-attachments/assets/56c5f58b-d7fb-4166-8767-0db99eaa30f3" />

In contrast, regenerating the entire method (common in many AI tools) would scale output tokens with method size, increasing latency and cost.

---

## 3. In-Flight Edits, Parallel Agents, and Conflict Isolation

📁Folder: **InFlightEdits**

📄File: **OrderSubmissionService.cs**

This example demonstrates how AiGen behaves **when the code changes while an AI request is in-flight**. The demos in this section feature two changes that need to be executed back-to-back quickly; it's recommended to look ahead to understand all the steps in each scenario before launching AiGen.

### Scenario A: Non-conflicting edits
1. From the **InFlightEdits** folder, open **OrderSubmissionService.cs**
2. Move the caret into the `Submit()` method.
3. Launch AiGen with:
   > _“Add logging around failures in this method.”_
4. While AiGen is running, append the method's XML doc comment with this (manually typed or pasted change):
   > `Logs any failures.`

You might get something like this:

```csharp
    /// <summary>
    /// Submits an order for processing. Logs any failures.
    /// </summary>
    public OrderProcessingResult? Submit(Order? order)
    {
        // Check 1 - valid input
        if (order is null)
        {
            Console.Error.WriteLine("[OrderSubmissionService.Submit] Failure: order is null.");
            throw new ArgumentNullException(nameof(order));
        }

        if (string.IsNullOrWhiteSpace(order.OrderId))
        {
            Console.Error.WriteLine("[OrderSubmissionService.Submit] Failure: OrderId is required.");
            throw new InvalidOperationException("OrderId is required.");
        }

        if (order.Customer is null)
        {
            Console.Error.WriteLine($"[OrderSubmissionService.Submit] Failure: Customer is required. OrderId='{order.OrderId}'.");
            throw new InvalidOperationException("Customer is required.");
        }

        // Check 2 - operational prerequisites
        if (order.Customer.BillingAddress is null)
        {
            Console.Error.WriteLine($"[OrderSubmissionService.Submit] Failure: Billing address is required. OrderId='{order.OrderId}'.");
            throw new InvalidOperationException("Billing address is required.");
        }

        if (string.IsNullOrWhiteSpace(order.Customer.BillingAddress.CountryCode))
        {
            Console.Error.WriteLine($"[OrderSubmissionService.Submit] Failure: Billing address country is required. OrderId='{order.OrderId}'.");
            throw new InvalidOperationException("Billing address country is required.");
        }

        // Check 3 - operational success
        bool externalAccepted = ExternalGatewayAccepts(order);
        if (!externalAccepted)
        {
            Console.Error.WriteLine($"[OrderSubmissionService.Submit] Failure: External gateway rejected the order. OrderId='{order.OrderId}'.");
            throw new InvalidOperationException("External gateway rejected the order.");
        }

        // All good.
        return OrderProcessingResult.Ok();
    }
```

Notice the AI modifications integrated cleanly even though we changed the code while the AI request was in-flight. You can continue editing code while AiGen processes each request, so there are no blocks or pauses in your workflow.

### Scenario B: Parallel, Non-Conflicting Changes (Multiple AI Agents)

In this scenario, we’ll launch **two AI agents simultaneously** to apply independent changes to the same method.

Start by **undoing** any **previous edits** and restoring `OrderSubmissionService.Submit()` to its original state. And you can **close** the **AiGen Navigator** if it is still open.

Open **TrackOperationAttribute.cs** in the **Shared** folder and review the attribute. This is the telemetry metadata we’ll apply. Note that it includes:

- A **Name** describing the tracked operation  
- A **Category** grouping related telemetry events  

Switch back to **OrderSubmissionService.cs** and place the caret inside the `Submit()` method. Perform the next two steps **back-to-back**:

1. Launch the first AI agent with:
   > _“Add logging around failures in this method.”_

2. Launch a second AI agent with one of these:
   > _“Add the track operation attribute. Category is orders.”_

The goal here is to start up a second agent while the first is still inflight. If the first AI response lands before you can launch the second, perform an undo (close the AiGen Navigator) and then copy the second prompt to the clipboard. Then after launching the first agent by voice, invoke the second agent with **Caps**+**G** (plus a paste).

<img width="537" height="217" alt="image" src="https://github.com/user-attachments/assets/fc45ed10-ae40-4768-9ccd-81f6aa98d429" />

When multiple AI responses land, the **AiGen Navigator** will show a result tab for each response.
<img width="759" height="505" alt="image" src="https://github.com/user-attachments/assets/57925e1d-202e-4396-9ef4-478914475cfb" />

Agents may complete in a different order than they were launched.

Notice that:
- One agent modifies the **method body** (logging)
- The other modifies the **method metadata** (the attribute)

Because these changes target **different structural regions**, both land successfully without conflict.

Skilled developers can safely run multiple AI agents in parallel when:
- The requested changes **do not overlap**, and
- Each agent’s task can be completed **independently** of the others.

**Note:** When multiple in-flight AI agents complete their changes, undo follows **landing order**, not launch order — the most recently applied change is undone first. This mirrors standard Visual Studio editor behavior and keeps AI changes fully integrated into the undo stack.

### Scenario C: Conflicting edits
1. Close the AiGen Navigator (if it's still up) and press undo (**Ctrl**+**Z**) until you've restored `OrderSubmissionService` to its original state.
2. Launch the same AiGen request (e.g., _“I want you to log any individual failures you find in this method”_).
3. While the AI request is in flight, modify one of the failure points (e.g., replace a `throw` with an early return).

When your request lands, AiGen detects the overlapping change and **blocks only the conflicting delta**, while allowing all other non-conflicting updates to apply normally. The AiGen Navigator should look something like this (multiple selection changes, with one conflict). If the Navigator only shows a single conflict, you might want to undo try this scenario again.

<img width="1520" height="692" alt="image" src="https://github.com/user-attachments/assets/3e688451-46a4-4d6b-a5ba-b75bcfe28cc7" />

Because AiGen typically produces **fine-grained deltas**, conflicts are isolated to the smallest possible change. A single overlapping edit does not invalidate the rest of the AI response — only the affected update is held back, while all other safe modifications are integrated.

The conflict report for the blocked delta shows the original code at request time and the current code at apply time, as well as the attempted replacement. You might see something like this:

**The change for this member was skipped because the target code changed inflight.**

**Original code at request time:**
```csharp
        if (order.Customer is null)
            throw new InvalidOperationException("Customer is required.");
```

**Current code at apply time:**
```csharp
        if (order.Customer is null)
            return null;
```

**Attempted replacement:**
```csharp
        if (order.Customer is null) {
            Console.Error.WriteLine($"[{nameof(OrderSubmissionService)}] Order submission failed: {nameof(order.Customer)} is required. OrderId='{order.OrderId ?? "<null>"}'.");
            throw new InvalidOperationException("Customer is required.");«Caret»
        }
```

**Original and current code blocks must match on landing.**

This section demonstrates how AiGen behaves when the code changes while AI requests are in flight — including parallel agents and isolated conflicts.

---

## 4. Debug-Time Runtime State → Test Case Generation

📁Folder: **DebugRuntimeState**
### Files of interest
- 📄**OrderAddressFormatter.cs**
- 📄**Program.cs**

This example shows how AiGen can use **live debug values** to generate test cases grounded in **actual runtime state**.

### Steps
1. In the **DebugRuntimeState** folder, open **OrderAddressFormatter.cs**
2. Place a **breakpoint** on the last line of the `BuildShippingLabel()` method:
   ```csharp
   return $"{name} — {cityRegionPostal}";
   ```
3. Run the program (`CodeRush.AiGen.Main`).
4. When execution stops at the breakpoint, you can inspect the `cityRegionPostal` variable. The debug-time value is "Seattle,&nbsp;&nbsp;&nbsp;98101". Further debug-time exploration might reveal the `Region` field is empty.
   Assuming an empty region is allowed, the resulting label is malformed: it contains a **dangling comma** and **extra whitespace** caused by an empty `Region` value. We can fix this, but it's a good idea to add a test case to catch this condition first.
5. Invoke AiGen and say:

> _“Create a test case for this method based on these debug time parameter values. Add asserts to make sure the label has no double spaces, and when the region is blank, make sure there is no dangling comma.”_

While waiting for the response, you might want to drill into the `order` parameter. It has a `Customer` property that in turn holds the `BillingAddress` with the empty region that led to this bug.

<img width="618" height="183" alt="image" src="https://github.com/user-attachments/assets/a673515f-5463-43ce-8ae5-5afec7418ebd" />

AiGen will:
- Reconstruct the runtime object graph from live debug values
- Locate the appropriate `OrderAddressFormatterTests` fixture
- Generate a new xUnit test that reproduces the observed state and behavior
- Add targeted assertions that detect the formatting defect

You should get a test case like this (note the object graph reconstruction at the top, which recreates the somewhat sophisticated debug-time state that exposed the bug):
```csharp
    [Fact]
    public void BuildShippingLabel_RegionBlank_NoDoubleSpaces_AndNoDanglingComma() {
        // Arrange (debug-time values)
        var address = new Address {
            City = "Seattle",
            Region = " ", // blank/whitespace
            PostalCode = "98101",
            CountryCode = "US",
            Line1 = "123 Example St"
        };
        var customer = new Customer { DisplayName = "Ada Lovelace", BillingAddress = address, Id = "C-42" };
        var order = new Order {
            Customer = customer,
            DiscountAmount = 10m,
            Subtotal = 120m,
            OrderId = "DBG-1001",
            TaxAmount = 0m
        };

        var formatter = new OrderAddressFormatter();

        // Act
        string label = formatter.BuildShippingLabel(order);

        // Assert
        Assert.DoesNotContain("  ", label);       // no double spaces anywhere
        Assert.DoesNotContain(", ", label);       // no dangling comma + space
        Assert.DoesNotMatch(@",\s*(—|$)", label); // no comma before em-dash or end
        Assert.DoesNotMatch(@",\s+\d", label);    // no comma followed by spaces then digits (e.g., ",  98101")
        Assert.Contains("Seattle", label);
        Assert.Contains("98101", label);
    }
```

This workflow makes it practical to capture real-world edge cases in the moment they are discovered. Instead of manually attempting to duplicate observed state, you can promote live runtime data directly into a durable, repeatable test.

After creating the new test case, you can stop debugging and run it if you like (it should fail since the code hasn't been fixed yet).

---
## 5. Large-Scale Architectural Changes (Cross-Cutting Updates)

📁Folder: **ArchitecturalEdits**
### Files of interest
- 📄**IOrderRule.cs**
- 📄**RuleResult.cs**

### Creating Interface Implementers in Bulk
This example demonstrates AiGen’s ability to perform large-scale architectural edits, including generating multiple new types and evolving an interface contract across all implementers.

Open **IOrderRule.cs**, and place the caret inside the `IOrderRule` interface:

```csharp
namespace CodeRush.AiGen.Main.ArchitecturalEdits;

public interface IOrderRule { 
    RuleResult Apply(Order order);
}
```

Prompt (spoken or typed): 
> _“I need ten non-trivial implementers of this interface. Create them in the rules namespace.”_

Because the configured reasoning model is synthesizing several non-trivial implementations, this step typically takes longer to complete (around 20–35 seconds).

When the request finishes, AiGen will have created ten concrete `IOrderRule` implementations under a new namespace:
`CodeRush.AiGen.Main.ArchitecturalEdits.Rules`

<img width="366" height="307" alt="image" src="https://github.com/user-attachments/assets/493eec51-d7cf-4313-88f0-cfa7e26a3d34" />

You can press **F7**/**F8** to navigate back and forth through these new classes.

Each class should:
 * Implement `IOrderRule`
 * Perform a distinct, non-trivial order rule
 * Produce meaningful `RuleResult` outcomes
 * Reflect realistic order-processing concerns (fraud, pricing, eligibility, fulfillment, etc.)

This step demonstrates AiGen’s ability to:
 * Generate multiple production-quality types in a single request
 * Enforce a shared interface contract across many implementations
 * Respect namespace, folder structure, and solution organization
 * Perform coordinated, multi-file edits across a codebase
 * Introduce new architectural layers without manual scaffolding

### Evolving the Contract Across the Hierarchy
Return the caret to `IOrderRule`, then invoke AiGen with:
> _“Add two properties — name and description — and update all implementers.”_

AiGen should:
 * Add `Name` and `Description` properties to the interface
 * Update all existing implementations to satisfy the expanded contract
 * Populate meaningful, domain-appropriate values for each rule

Because this step modifies existing code rather than generating new types, it typically completes **significantly faster** than the previous bulk-creation prompt.

Together, these two prompts demonstrate AiGen’s ability to:
 * Perform **large-scale architectural refactoring**
 * Maintain **consistency across many implementations**
 * Propagate **interface contract changes safely across a hierarchy**
 * Coordinate **multi-file edits with minimal developer intervention**

Earlier examples emphasized fast, fine-grained edits. This scenario shows the other side of AiGen: the ability to perform broad, cross-cutting architectural changes when needed.

---

## 6. Active Error Awareness (Fixing Compiler Errors from Editor Context)

📁Folder: **ActiveErrorAnalysis**  
📄File: **OrderQueryService.cs**

This example demonstrates AiGen’s ability to **read active compiler errors** from the editor and resolve them using code context and diagnostics -- without the need to describe the problem in detail.

### Enabling the Demo Error

Open **OrderQueryService.cs** and **uncomment** the demo toggle at the top of the file:

```csharp
//#define DEMO_ACTIVE_ERRORS
```

This intentionally introduces a compiler error inside `GetHighValueOrderCountAsync()`.

### Scenario

The method applies LINQ operations directly to the result of `GetOrdersAsync()`, which returns a `Task<List<Order>>`. This produces a compile-time error because LINQ operators like `Where()` operate on collections — not tasks. 

```csharp
public async Task<int> GetHighValueOrderCountAsync(decimal minSubtotal) {
    return GetOrdersAsync()
        .Where(o => o.Subtotal >= minSubtotal)
        .Count();
}
```

<img width="916" height="142" alt="image" src="https://github.com/user-attachments/assets/bb004a43-0fd7-4201-8c7e-b41e7a49363f" />

What we need to do is wait for the orders to arrive before filtering with `Where()`.

### Fixing the Error with AiGen

1. Place your caret on the `.Where(...)` call (where the error is surfaced)
2. Invoke AiGen and say:
   
> _“Fix this.”_

AiGen will:
 * Detect the active compiler error at the caret
 * Infer the intended async behavior
 * Rewrite the logic to correctly await the task before applying LINQ

A typical result might look like this:

```csharp
public async Task<int> GetHighValueOrderCountAsync(decimal minSubtotal) {
    var orders = await GetOrdersAsync().ConfigureAwait(false);
    return orders.Count(o => o.Subtotal >= minSubtotal);
}
```

### What This Demonstrates
This scenario shows AiGen can:
 * Read active error diagnostics directly from the editor
 * Infer correct async + LINQ behavior without explicit instructions
 * Apply non-trivial fixes based on compiler feedback and surrounding context
 * Resolve errors with a minimal prompt — even something as short as “Fix this.”

Instead of describing the problem in detail, you simply point AiGen at the error and let the **code context, diagnostics, and intent **guide the solution. You can also drive fixes from Visual Studio's **Error List** tool window.

## Key Takeaways

These samples demonstrate how AiGen fits into real development workflows:

- AiGen supports both **large, multi-file architectural changes** and **small, high-precision edits**.
- **Fast, fine-grained changes** make AI practical for frequent, low-friction use in everyday coding.
- Effective prompts can be **short and informal** — there’s no need to script or over-specify intent.
- AiGen leverages **code context, type hierarchy, live compiler diagnostics, editor state, and debug-time values** to infer what matters.
- AiGen can **analyze active compiler errors** and generate targeted fixes with minimal instruction.
- Developers rarely need to name symbols, dictate structure, or manually scope changes — **context and diagnostics drive behavior**.

In practice, AiGen behaves like a **context-aware coding partner** — resolving active errors, promoting runtime state into tests, applying surgical edits, and executing broad architectural changes when needed.

---

## Next Steps

- Clone the repository  
- Open the solution  
- Run through the scenarios in order  
- Experiment with your own prompts and workflows  

For more background and implementation details, see the accompanying blog post:  
https://int.devexpress.com/community/blogs/markmiller/archive/2026/01/07/new-aigen-functionality-in-coderush-for-visual-studio.aspx
