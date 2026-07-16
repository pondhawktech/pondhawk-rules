// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;

namespace Pondhawk.Rules.Builder;

/// <summary>
/// Base class for rule set implementations, providing evaluation and threshold-based decision support.
/// </summary>
public abstract class AbstractRuleSet : AbstractEvaluator, IRuleSet
{
    /// <summary>Initializes a new instance of the <see cref="AbstractRuleSet"/> class with default threshold of zero.</summary>
    protected AbstractRuleSet()
    {
        DecisionThreshold = 0;
    }

    /// <inheritdoc />
    public int DecisionThreshold { get; set; }

    /// <inheritdoc />
    public Func<object, bool> Predicate => fact => Decide(fact);

    /// <inheritdoc />
    public virtual bool Decide(params object[] facts)
    {
        EvaluationResults result = Evaluate(facts);
        return result.Score >= DecisionThreshold;
    }

    /// <inheritdoc />
    public virtual bool Decide(int threshold, params object[] facts)
    {
        EvaluationResults result = Evaluate(facts);
        return result.Score >= threshold;
    }

    /// <inheritdoc />
    public abstract EvaluationContext GetEvaluationContext();

    /// <inheritdoc />
    public IEnumerable<string> GetNamespaceFilters() => GetNamespaces();

}
