# CodeRush AiGen Samples

This repository contains a small, focused C# solution used to demonstrate **CodeRush’s AiGen** capabilities inside Visual Studio.

The examples are intentionally realistic and minimal. Each scenario shows how AiGen behaves as a **pair-programming assistant**—working with context, hierarchy, and runtime state—without requiring verbose or prescriptive prompts.

---

## Prerequisites

- Visual Studio 2022
- .NET 8 SDK
- CodeRush (with AiGen enabled)

Clone the repo and open:

```
CodeRush.AiGen.Samples.sln
```

Build once to ensure everything is ready.

---

## Project Layout

```
CodeRush.AiGen.Main
├─ ContextAcquisition
├─ HyperOptimizedDeltas
├─ InFlightEdits
├─ DebugRuntimeState
├─ Shared

CodeRush.AiGen.Tests
```

Each folder in `CodeRush.AiGen.Main` corresponds to a specific AiGen capability demonstrated in the blog post.

---

## 1. Context Acquisition & Hierarchy-Aware Refactoring

**Folder:** `ContextAcquisition`

This example demonstrates how AiGen discovers and uses **related types up and down an inheritance hierarchy** to improve code quality.

### Files of interest
- `IOrderValidator`
- `BaseValidator<T>`
- `OrderValidator`

### Scenario
`OrderValidator.ValidateCore` contains validation logic that partially duplicates behavior already implemented in the base class.

Place your caret inside the duplicated guard code in `ValidateCore`.

### Example spoken prompts (all equivalent)
Any of the following work:

- **“Consolidate this logic with what we already have in the base class.”**
- **“Refactor this to reuse what’s already in the ancestor class.”**
- **“Use the inherited validation helpers instead of duplicating this.”**

AiGen is expected to:
- Traverse the inheritance hierarchy
- Identify reusable base-class logic
- Remove duplication while keeping order-specific checks local

No method names or implementation details need to be mentioned.

---

## 2. Hyper-Optimized Deltas (Small Change, Large Method)

**Folder:** `HyperOptimizedDeltas`  
**File:** `OrderTaxCalculator.cs`

This example shows how AiGen can modify **small, nuanced logic** inside a **large method** without regenerating the entire body.

### Scenario
Inside `ComputeTaxes`, there is a TODO describing a business rule:

> Promotional discounts are normally non-taxable, but some customers are override-eligible.

Place your caret near the TODO comment inside the loop.

### Example prompt
> “Update this logic so promotional discounts are excluded from tax calculation, except for override-eligible customers.”

AiGen should:
- Change only the relevant condition(s)
- Leave the rest of the method untouched
- Apply the change directly (no copy/paste)

This demonstrates why fine-grained deltas reduce cost and latency.

---

## 3. In-Flight Code Changes & Conflict Navigation

**Folder:** `InFlightEdits`  
**File:** `OrderSubmissionService.cs`

This example demonstrates how AiGen behaves while **you continue editing code**.

### Scenario A: Non-conflicting edits
1. Launch AiGen with:
   > “Add logging around failures in this method.”
2. While AiGen is running, edit the XML doc comment and add:
   > “Logs any failures.”

The AI change should integrate cleanly.

### Scenario B: Conflicting edits
1. Launch the same AiGen request.
2. While it’s running, modify one failure point (e.g., replace a `throw` with an early return).

AiGen will detect the conflict and flag it in the **AiGen Navigator**, rather than silently overwriting your change.

---

## 4. Debug-Time Runtime State → Test Generation

**Folder:** `DebugRuntimeState`  
**File:** `OrderAddressFormatter.cs`

This example shows how AiGen uses **live debug values** to generate meaningful tests.

### Steps
1. Run `CodeRush.AiGen.Main`.
2. Place a breakpoint on the call to:
   ```csharp
   formatter.BuildShippingLabel(order)
   ```
3. When stopped at the breakpoint, invoke AiGen and say:

> **“Create a test case for this method based on these debug time parameter values.  
> Add asserts to make sure the label has no double spaces and no dangling comma when region is blank.”**

AiGen will:
- Reconstruct the runtime object graph
- Create a new xUnit test
- Add meaningful assertions that catch the bug

No placeholder test is required.

---

## 5. Knowledge Sources (Optional)

AiGen can also consult **official documentation** when relevant:

- DevExpress MCP Server (for DevExpress APIs)
- Microsoft MCP Server (for Microsoft technologies)

Simply mention the relevant technology in your question when needed.

---

## Philosophy of These Examples

These samples are designed to show that:

- AiGen works best with **small, focused requests**
- You don’t need to name methods or dictate structure
- Minimal, human-shorthand prompts are sufficient
- Context (code, hierarchy, debug state) does the heavy lifting

AiGen behaves less like a command interface and more like a coding partner that understands where it is and what matters.

---

## Next Steps

- Clone the repo
- Open the solution
- Try each scenario in order
- Experiment with your own shorthand prompts

For more details, see the accompanying blog post.
