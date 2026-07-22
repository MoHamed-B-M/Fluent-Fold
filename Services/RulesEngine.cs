using System.Text.RegularExpressions;
using FluentFold.Models;

namespace FluentFold.Services;

public sealed class RulesEngine : IRulesEngine
{
    public string ApplyRules(string fileName, IReadOnlyList<RuleModel>? rules)
    {
        if (rules is null || rules.Count == 0)
            return string.Empty;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(nameWithoutExt))
            return string.Empty;

        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.TriggerValue))
                continue;

            var match = rule.TriggerType switch
            {
                TriggerType.Contains => nameWithoutExt.Contains(rule.TriggerValue, StringComparison.OrdinalIgnoreCase),
                TriggerType.StartsWith => nameWithoutExt.StartsWith(rule.TriggerValue, StringComparison.OrdinalIgnoreCase),
                TriggerType.EndsWith => nameWithoutExt.EndsWith(rule.TriggerValue, StringComparison.OrdinalIgnoreCase),
                TriggerType.Regex => Regex.IsMatch(nameWithoutExt, rule.TriggerValue, RegexOptions.IgnoreCase),
                _ => false
            };

            if (match)
                return rule.TargetCategory;
        }

        return string.Empty;
    }
}
