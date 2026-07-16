// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Evaluation;

/// <summary>
/// Evaluates rules against the facts in an <see cref="EvaluationContext"/> and returns aggregated results.
/// </summary>
public interface IEvaluator
{
    /// <summary>Evaluates rules against the facts in the given context and returns aggregated results.</summary>
    /// <param name="context">The evaluation context containing facts, configuration, and result accumulators.</param>
    /// <returns>The aggregated evaluation results.</returns>
    EvaluationResults Evaluate(EvaluationContext context);
}

