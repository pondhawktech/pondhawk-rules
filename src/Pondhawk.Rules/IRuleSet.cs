// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;

namespace Pondhawk.Rules;

/// <summary>
/// A built rule set that can evaluate facts, validate entities, and make weighted decisions.
/// </summary>
/// <remarks>
/// <para>Core operations:</para>
/// <list type="bullet">
/// <item><c>Evaluate(facts)</c> -- Run all matching rules against the given facts, returning <see cref="EvaluationResults"/>.</item>
/// <item><c>TryValidate(fact, out violations)</c> -- Validate a single entity (via extension method in <see cref="RuleSetExtensions"/>).</item>
/// <item><c>Decide(facts)</c> -- Evaluate and return true if score meets the <see cref="DecisionThreshold"/>.</item>
/// </list>
/// <para>By default, <c>Evaluate</c> throws <c>ViolationsExistException</c> if violations are found
/// and <c>NoRulesEvaluatedException</c> if no rules match. Use <c>EvaluationContext.SuppressExceptions()</c> to disable.</para>
/// </remarks>
public interface IRuleSet : IEvaluator
{
    /// <summary>Creates a new <see cref="EvaluationContext"/> configured for this rule set.</summary>
    /// <returns>A new evaluation context.</returns>
    EvaluationContext GetEvaluationContext();

    /// <summary>Gets or sets the default score threshold used by <see cref="Decide(object[])"/>.</summary>
    int DecisionThreshold { get; set; }

    /// <summary>Gets a predicate function that evaluates a single fact using <see cref="Decide(object[])"/>.</summary>
    Func<object, bool> Predicate { get; }

    /// <summary>Evaluates the facts and returns <c>true</c> if the score meets the <see cref="DecisionThreshold"/>.</summary>
    /// <param name="facts">The facts to evaluate.</param>
    /// <returns><c>true</c> if the evaluation score meets or exceeds the threshold.</returns>
    bool Decide(params object[] facts);

    /// <summary>Evaluates the facts and returns <c>true</c> if the score meets the specified threshold.</summary>
    /// <param name="threshold">The score threshold to apply.</param>
    /// <param name="facts">The facts to evaluate.</param>
    /// <returns><c>true</c> if the evaluation score meets or exceeds the threshold.</returns>
    bool Decide(int threshold, params object[] facts);

    /// <summary>Gets the namespace filters applied to this rule set.</summary>
    /// <returns>The namespace filter strings.</returns>
    IEnumerable<string> GetNamespaceFilters();
}
