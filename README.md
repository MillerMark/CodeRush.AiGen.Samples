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
├─ ContextAcquisition
├─ FineGrainedDeltas
├─ InFlightEdits
├─ DebugRuntimeState
└─ Shared
```

Each folder in `CodeRush.AiGen.Main` corresponds to a specific AiGen capability demonstrated in the blog post. The **Shared** folder contains common models and functionality used across examples. The **InFlightEdits** folder includes examples demonstrating parallel AI agents, conflict isolation, and partial landing of fine-grained deltas.

Additionally, the `CodeRush.AiGen.Tests` project contains a baseline test case and is extended by AiGen in the DebugRuntimeState example.

---

## Launching AiGen

To set up AiGen, follow the instructions [here](https://community.devexpress.com/blogs/markmiller/archive/2025/09/08/advanced-ai-setup-for-aigen-and-aifind-in-coderush-for-visual-studio.aspx#general-setup).

Once you've specified API keys and selected your AI model, you can invoke AiGen via voice by **double-tapping** the **right** **Ctrl** key (and holding it down while you speak), or by pressing **Caps**+**G** for a text prompt (if you have [Caps as a Modifier](https://docs.devexpress.com/CodeRushForRoslyn/403629/getting-started/keyboard-shortcuts/caps-as-a-modifier) enabled)

---

## 1. Context Acquisition & Hierarchy-Aware Refactoring

📁**Folder:** `ContextAcquisition`

Demonstrates how AiGen discovers and incorporates **related types across an inheritance hierarchy** to improve the quality and correctness of the AI coding response.

### Files of interest
- 📄`IOrderValidator.cs`
- 📄`BaseValidator.cs`
- 📄`OrderValidator.cs`

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

If you look at the ancestor class `BaseValidator<T>`, you can see its `RequireCustomer()` method contains somewhat similar code:

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

## 2. Fine-grained Deltas (Small Change in a Large Method)

📁**Folder:** `FineGrainedDeltas`  
📄**File:** `OrderTaxCalculator.cs`

This example shows how AiGen can apply **fine-grained deltas** — modifying small, targeted regions inside large methods **without regenerating entire method bodies**.

### Scenario 
Inside `OrderTaxCalculator`'s `ComputeTaxes()` method, there is a TODO describing a business rule:

> Promotional discounts are normally non-taxable, but some customers are override-eligible => tax must be computed.

Place your caret near the `TODO` comment inside the loop that computes the taxable base.

### Example prompts (all equivalent)
**Double-tap** the **right** **Ctrl** key and keep it held down while you say one of the following (or similar):

- _“Update this logic so promotional discounts are excluded from tax calculation, except for override-eligible customers.”_
- _“This code is always subtracting the discount to get a taxable base, but I only want to do that when the customer is not override eligible.”_

AiGen should:
- Change only the minimal code region required
- Leave the rest of the method untouched
- Apply the update directly in the editor (no manual copy/paste)

AiGen might remove the original assignment to `taxableBase` (e.g., `decimal taxableBase = order.Subtotal - order.DiscountAmount;`) and replace it with something like this:

```csharp
            // Promotional discounts are normally non-taxable.
            // For override-eligible customers, discounts are taxable (i.e., discount does NOT reduce the taxable base).
            decimal taxableBase = customer.IsTaxExemptOverrideEligible
                ? order.Subtotal
                : order.Subtotal - order.DiscountAmount;
```

or you might get something like this:

```csharp
            decimal taxableBase = order.Subtotal;

            // Discounts reduce taxable base only when the customer is NOT override-eligible.
            if (!customer.IsTaxExemptOverrideEligible)
                taxableBase -= order.DiscountAmount;
```

Note both the size of the method and how little code AiGen needs to generate to apply the change. The prompt contains no symbol names — we simply describe the desired behavior, and AiGen infers the implementation from context.

This example demonstrates fine-grained deltas in practice: smaller outputs, lower token usage, reduced latency, and a more immediate turnaround — especially when working inside large methods.

---

## 3. In-Flight Edits, Parallel Agents & Conflict Isolation

📁**Folder:** `InFlightEdits`  
📄**File:** `OrderSubmissionService.cs`

This example demonstrates how AiGen behaves **when the code changes while an AI request is in-flight**.

### Scenario A: Non-conflicting edits
1. Open `OrderSubmissionService.cs`
2. Move the caret into the `Submit()` method.
3. Launch AiGen with:
   > _“Add logging around failures in this method.”_
4. While AiGen is running, append the method's XML doc comment with this:
   > `Logs any failures.`

The pending AI change should still integrate cleanly even though we changed the code while the AI request was in-flight.

### Scenario B: Conflicting edits
1. Press undo (**Ctrl**+**Z**) to restore `OrderSubmissionService` to its original state.
2. Launch the same AiGen request (e.g., _“I want you to log any failures you find in this method”_).
3. While the AI request is in flight, modify one of the failure points (e.g., replace a `throw` with an early return).

When your request lands, AiGen will detect the conflict and flag it in the **AiGen Navigator**, rather than silently overwriting your change.

<img width="1520" height="692" alt="image" src="https://github.com/user-attachments/assets/3e688451-46a4-4d6b-a5ba-b75bcfe28cc7" />

The conflict report shows the original code at request time and the current code at apply time, as well as the attempted replacement. You might see something like this:

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

---

## 4. Debug-Time Runtime State → Test Generation

📁**Folder:** `DebugRuntimeState` 
### Files of interest
- 📄`OrderAddressFormatter.cs`
- 📄`Program.cs`

This example shows how AiGen can use **live debug values** to generate meaningful test cases.

### Steps
1. Open `OrderAddressFormatter.cs`
2. Place a **breakpoint** on the last line of the `BuildShippingLabel()` method:
   ```csharp
   return $"{name} — {cityRegionPostal}";
   ```
3. Run the program (`CodeRush.AiGen.Main`).
4. When execution stops at the breakpoint, you can inspect the `cityRegionPostal` variable. The debug-time value is "Seattle,   98101". Further debug-time exploration might reveal the `Region` field is empty. Assuming an empty region is allowed, the resulting format is less than ideal -- it has a dangling comma followed by three spaces (it needs only a single space when the `Region` is empty). We can fix this, but it's a good idea to add a test case to catch this condition first.
5. Invoke AiGen and say:

_“Create a test case for this method based on these debug time parameter values. Add asserts to make sure the label has no double spaces and no dangling comma when the region is blank.”_

AiGen will:
- Reconstruct the runtime object graph
- Find the corresponding `OrderAddressFormatterTests` test fixture.
- Add a new xUnit test with meaningful assertions that catch the bug

You should get a test case like this (notice the complex object construction code at the beginning to recreate the debug-time state which helped us discover the bug):
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

---

## Philosophy

These samples are designed to show:

- AiGen can quickly handle **small, focused requests** in large methods/files.
- Minimal, human-shorthand prompts are sufficient.
- You can speak conversationally as if you were working with a pair programmer.
- AiGen's rich contextual awareness means you rarely need to name methods/types or dictate structure.
- Context (code, hierarchy, debug state) does the heavy lifting

AiGen behaves less like a command interface and more like a coding partner that works to understand where it is and what matters.

---

## Next Steps

- Clone the repo
- Open the solution
- Try each scenario in order
- Experiment with your own prompts

For more details, see the accompanying [blog post](https://int.devexpress.com/community/blogs/markmiller/archive/2026/01/07/new-aigen-functionality-in-coderush-for-visual-studio.aspx).
