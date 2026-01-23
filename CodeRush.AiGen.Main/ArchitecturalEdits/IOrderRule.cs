using System;

namespace CodeRush.AiGen.Main.ArchitecturalEdits;

public interface IOrderRule {
    RuleResult Apply(Order order);
}