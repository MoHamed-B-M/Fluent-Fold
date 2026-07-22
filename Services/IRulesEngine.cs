using FluentFold.Models;

namespace FluentFold.Services;

/// <summary>Evaluates trigger-based rules against file names to determine categories.</summary>
public interface IRulesEngine
{
    /// <summary>Applies trigger rules to a file name and returns the matching category.</summary>
    /// <param name="fileName">The file name (with extension) to evaluate.</param>
    /// <param name="rules">The trigger rules to apply.</param>
    /// <returns>The target category if a rule matches; otherwise, an empty string.</returns>
    string ApplyRules(string fileName, IReadOnlyList<RuleModel>? rules);
}
