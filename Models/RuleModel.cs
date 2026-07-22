namespace FluentFold.Models;

/// <summary>Defines the type of trigger matching to apply.</summary>
public enum TriggerType
{
    /// <summary>Match if the file name contains the trigger value.</summary>
    Contains,
    /// <summary>Match if the file name starts with the trigger value.</summary>
    StartsWith,
    /// <summary>Match if the file name ends with the trigger value.</summary>
    EndsWith,
    /// <summary>Match if the file name matches the trigger regex pattern.</summary>
    Regex
}

/// <summary>Defines a trigger rule that assigns a category when a condition is met.</summary>
public sealed class RuleModel
{
    /// <summary>The type of trigger matching to apply.</summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Contains;
    /// <summary>The value to match against the file name.</summary>
    public string TriggerValue { get; set; } = string.Empty;
    /// <summary>The category to assign if the rule matches.</summary>
    public string TargetCategory { get; set; } = string.Empty;
}
