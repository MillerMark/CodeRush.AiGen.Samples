using System;

namespace CodeRush.AiGen.Main.ArchitecturalEdits;

public interface IOrderRule {
    string Name { get; }
    RuleResult Apply(Order order);
}