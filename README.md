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

## 3. In-Flight Edits, Parallel Agents, and Conflict Isolation

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

The pending AI change should still integrate cleanly even though we changed the code while the AI request was in-flight. This allows you to continue editing while AiGen works, without blocking your workflow.

### Scenario B: Parallel, Non-Conflicting Changes (Multiple AI Agents)

In this scenario, we’ll launch **two AI agents simultaneously** to apply independent changes to the same method.

Start by undoing any previous edits and restoring `OrderSubmissionService.Submit()` to its original state.

Open `TrackOperationAttribute.cs` in the `Shared` folder and review the attribute. This is the telemetry metadata we’ll apply. Note that it includes:

- A **Name** describing the tracked operation  
- A **Category** grouping related telemetry events  

Switch back to `OrderSubmissionService.cs` and place the caret inside the `Submit()` method. Perform the next two steps **back-to-back**:

1. Launch the first AI agent with:
   > _“Add logging around failures in this method.”_

2. Launch a second AI agent with:
   > _“Let’s add telemetry with the track operation attribute and set the category to orders.”_

When the AI responses land, the **AiGen Navigator** will show multiple result tabs.
<img width="759" height="505" alt="image" src="https://github.com/user-attachments/assets/57925e1d-202e-4396-9ef4-478914475cfb" />

Agents may complete in a different order than they were launched.

Notice that:
- One agent modifies the **method body** (logging)
- The other modifies the **method metadata** (the attribute)

Because these changes target **different structural regions**, both land successfully without conflict.

This demonstrates that skilled developers can safely run multiple AI agents in parallel when:
- The requested changes **do not overlap**, and
- Each agent’s task can be completed **independently** of the others.

**Note:** When multiple in-flight AI agents complete their changes, undo follows **landing order**, not launch order — the most recently applied change is undone first. This mirrors standard Visual Studio editor behavior and keeps AI changes fully integrated into the undo stack.

### Scenario C: Conflicting edits
1. Press undo (**Ctrl**+**Z**) to restore `OrderSubmissionService` to its original state.
2. Launch the same AiGen request (e.g., _“I want you to log any failures you find in this method”_).
3. While the AI request is in flight, modify one of the failure points (e.g., replace a `throw` with an early return).

When your request lands, AiGen detects the overlapping change and **blocks only the conflicting delta**, while allowing all other non-conflicting updates to apply normally.

<img width="1520" height="692" alt="image" src="https://github.com/user-attachments/assets/3e688451-46a4-4d6b-a5ba-b75bcfe28cc7" />

Because AiGen produces **fine-grained deltas**, conflicts are isolated to the smallest possible change. A single overlapping edit does not invalidate the rest of the AI response — only the affected update is held back, while all other safe modifications are integrated.

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

This section demonstrates how AiGen behaves when code changes while AI requests are in flight — including parallel agents and isolated conflicts.

---

## 4. Debug-Time Runtime State → Test Generation

📁**Folder:** `DebugRuntimeState` 
### Files of interest
- 📄`OrderAddressFormatter.cs`
- 📄`Program.cs`

This example shows how AiGen can use **live debug values** to generate test cases grounded in **actual runtime state**, not hand-constructed input.

### Steps
1. Open `OrderAddressFormatter.cs`
2. Place a **breakpoint** on the last line of the `BuildShippingLabel()` method:
   ```csharp
   return $"{name} — {cityRegionPostal}";
   ```
3. Run the program (`CodeRush.AiGen.Main`).
4. When execution stops at the breakpoint, you can inspect the `cityRegionPostal` variable. The debug-time value is "Seattle,   98101". Further debug-time exploration might reveal the `Region` field is empty.
   Assuming an empty region is allowed, the resulting label is malformed: it contains a **dangling comma** and **extra whitespace** caused by an empty `Region` value. We can fix this, but it's a good idea to add a test case to catch this condition first.
6. Invoke AiGen and say:

_“Create a test case for this method based on these debug time parameter values. Add asserts to make sure the label has no double spaces and no dangling comma when the region is blank.”_

AiGen will:
- Reconstruct the runtime object graph from live debug values
- Locate the appropriate `OrderAddressFormatterTests` fixture
- Generate a new xUnit test that reproduces the observed state and behavior
- Add targeted assertions that detect the formatting defect

You should get a test case like this (note the object graph reconstruction at the top, which recreates the exact debug-time state that exposed the bug):
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

---

## Philosophy

These samples are designed to show:

- AiGen supports both **large, multi-file changes** and **small, precise edits**.
- This release highlights workflows where **fast, fine-grained changes** make AI practical for everyday development.
- Minimal, shorthand prompts are sufficient—there’s no need to script the AI.
- You can interact naturally, as if working with a **pair programmer**.
- AiGen’s contextual awareness means you rarely need to name symbols or dictate structure.
- Code context, type hierarchy, and debug-time state do most of the heavy lifting.

AiGen behaves less like a command interface and more like a coding partner that understands context, intent, and scope — whether you’re making a small targeted edit or a broad, cross-cutting change.

---

## Next Steps

- Clone the repo
- Open the solution
- Try each scenario in order
- Experiment with your own prompts

For more details, see the accompanying [blog post](https://int.devexpress.com/community/blogs/markmiller/archive/2026/01/07/new-aigen-functionality-in-coderush-for-visual-studio.aspx).
