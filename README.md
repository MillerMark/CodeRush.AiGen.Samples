# CodeRush AiGen Samples

This repository contains a small C# solution that demonstrates **CodeRush’s AiGen** capabilities inside Visual Studio.

Examples show how AiGen can behave as a **pair-programming assistant** — working with context, hierarchy, and runtime state — without requiring detailed instructions in the prompts.

---

## Prerequisites

- Visual Studio 2022 or higher
- .NET 8 SDK
- CodeRush 25.2 or higher (with [AiGen](https://community.devexpress.com/Blogs/markmiller/archive/2025/06/24/aigen-amp-aifind-in-coderush-for-visual-studio.aspx) enabled)

Clone the repo and open:

```
CodeRush.AiGen.Samples.sln
```

## Main Project Layout

```
CodeRush.AiGen.Main
├─ ContextAcquisition
├─ HyperOptimizedDeltas
├─ InFlightEdits
├─ DebugRuntimeState
└─ Shared
```

Each folder in `CodeRush.AiGen.Main` corresponds to a specific AiGen capability demonstrated in the blog post. The `Shared` folder contains common models and functionality used across examples. 

Additionally, the `CodeRush.AiGen.Tests` project contains a single test case.

---

## Launching AiGen

To setup AiGen, follow the instructions [here](https://community.devexpress.com/blogs/markmiller/archive/2025/09/08/advanced-ai-setup-for-aigen-and-aifind-in-coderush-for-visual-studio.aspx#general-setup).

Once you've specified API keys and selected your AI model, you can invoke AiGen via voice by **double-tapping** the **right** **Ctrl** key (and holding it down while you speak), or by pressing **Caps**+**G** for a text prompt (if you have [Caps as a Modifier](https://docs.devexpress.com/CodeRushForRoslyn/403629/getting-started/keyboard-shortcuts/caps-as-a-modifier) enabled)

---

## 1. Context Acquisition & Hierarchy-Aware Refactoring

📁**Folder:** `ContextAcquisition`

Demonstrates how AiGen discovers and uses **related types up and down an inheritance hierarchy** to improve the quality of the AI coding response.

### Files of interest
- 📄`IOrderValidator.cs`
- 📄`BaseValidator.cs`
- 📄`OrderValidator.cs`

### Scenario
Inside `OrderValidator`, the `ValidateCore()` method contains validation logic that partially duplicates some behavior already implemented elsewhere in the solution:

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
**Double=tap** the **right** **Ctrl** key and keep it held down while you say one of the following (or similar):

- _“Consolidate this logic with what we already have in the base class.”_
- _“Take a look at the ancestor class and see if we can reuse any of that code here.”_
- _“Let's use the inherited validation helpers instead of duplicating their logic here.”_

Release the **Ctrl** key when you are done speaking. If you prefer to type this in, press **Caps**+**G** to bring up a prompt window.

AiGen should:
- Traverse the inheritance hierarchy
- Identify reusable base-class logic
- Remove duplication while keeping `order`-specific checks local

The ending code should look something like this:

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
```

There's rarely a need to explicitly mention method names, type names, or specific implementation details. AiGen keeps the tone conversational, inferring intent from Visual Studio context and the surrounding code.

AI can make mistakes. If you get a result you don't like you can always hit **undo** (**Ctrl**+**Z**) and try again.

---

## 2. Hyper-Optimized Deltas (Small Change, Large Method)

📁**Folder:** `HyperOptimizedDeltas`  
📄**File:** `OrderTaxCalculator.cs`

This next example shows how AiGen can modify **small bits of logic** quickly inside one or more **large methods** without regenerating entire method bodies.

### Scenario 
Inside `OrderTaxCalculator`'s `ComputeTaxes()` method, there is a TODO describing a business rule:

> Promotional discounts are normally non-taxable, but some customers are override-eligible => tax must be computed.

Place your caret near the TODO comment inside the loop.

### Example prompts (all equivalent)
**Double=tap** the **right** **Ctrl** key and keep it held down while you say one of the following (or similar):

- _“Update this logic so promotional discounts are excluded from tax calculation, except for override-eligible customers.”_
- _“Taxes calculated here should not include promotional discounts unless the customer is override-eligible.”_

AiGen should:
- Change only the relevant condition(s)
- Leave the rest of the method untouched
- Apply the change directly (no copy/paste)

AiGen should remove the old assignment to taxableBase (`decimal taxableBase = order.Subtotal - order.DiscountAmount;`) and replace it with something like this:

```csharp
            decimal taxableBase = order.Subtotal;

            // Promotional discounts are excluded from tax, except for override-eligible customers.
            if (order.HasDiscount && customer.IsTaxExemptOverrideEligible) {
                taxableBase -= order.DiscountAmount;
            }
```

or you might get something like this:

```csharp
            decimal taxableBase = customer.IsTaxExemptOverrideEligible
                ? (order.Subtotal - order.DiscountAmount)
                : order.Subtotal;
```

Note the size of the method and the speed of the AI response (and compare AiGen's speed to other AI coding assistants working on similar tasks). This example demonstrates a high-speed AI response using smaller-grained deltas, which tend to reduce AI cost and latency.

---

## 3. In-Flight Code Changes & Conflict Navigation

📁**Folder:** `InFlightEdits`  
📄**File:** `OrderSubmissionService.cs`

This example demonstrates how AiGen behaves **when the code changes while an AI request is in-flight**.

### Scenario A: Non-conflicting edits
1. Open `OrderSubmissionService.cs`
2. Move the caret into the `OrderProcessingResult()` method.
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

The conflict report shows the original code at request time and the current code at apply time, as well as the attempted replacement. You might see somethineg like this:

> The change for this member was skipped because the target code changed inflight.
> 
> Original code at request time:
```csharp
        if (order.Customer is null)
            throw new InvalidOperationException("Customer is required.");
```
> 
> Current code at apply time:
```csharp
        if (order.Customer is null)
            return null;
```
> 
> Attempted replacement:
```csharp
        if (order.Customer is null) {
            Console.Error.WriteLine($"[{nameof(OrderSubmissionService)}] Order submission failed: {nameof(order.Customer)} is required. OrderId='{order.OrderId ?? "<null>"}'.");
            throw new InvalidOperationException("Customer is required.");«Caret»
        }
```
>
> Original and current code blocks must match on landing.

---

## 4. Debug-Time Runtime State → Test Generation

📁**Folder:** `DebugRuntimeState` 
### Files of interest
- 📄`OrderAddressFormatter.cs`
- 📄`Program.cs`

This example shows how AiGen can use **live debug values** to generate meaningful test cases.

### Steps
1. Open `Program.cs`
2. Place a breakpoint on the call to:
   ```csharp
   formatter.BuildShippingLabel(order)
   ```
3. Run the program (`CodeRush.AiGen.Main`).
4. When stopped at the breakpoint, invoke AiGen and say:

_“Create a test case for this method based on these debug time parameter values. Add asserts to make sure the label has no double spaces and no dangling comma when the region is blank.”_

AiGen will:
- Reconstruct the runtime object graph
- Create a new xUnit test
- Add meaningful assertions that catch the bug

No placeholder test is required.

---

## Philosophy

These samples are designed to show:

- AiGen can handle **small, focused requests** in large methods/files.
- Minimal, human-shorthand prompts are sufficient.
- You can speak conversationally as if you were working with a pair programmer.
- AiGen's rich contextual awareness means you rarely need to name methods/types or dictate structure.
- Context (code, hierarchy, debug state) does the heavy lifting

AiGen behaves less like a command interface and more like a coding partner that works to understands where it is and what matters.

---

## Next Steps

- Clone the repo
- Open the solution
- Try each scenario in order
- Experiment with your own prompts

For more details, see the accompanying blog post.
