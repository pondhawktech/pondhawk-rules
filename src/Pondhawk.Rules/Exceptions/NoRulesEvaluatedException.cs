// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Exceptions;

/// <summary>
/// Thrown when an evaluation completes without evaluating any rules.
/// </summary>
public sealed class NoRulesEvaluatedException : Exception
{

    /// <summary>
    /// Initializes a new instance of the <see cref="NoRulesEvaluatedException"/> class with the evaluation results.
    /// </summary>
    /// <param name="result">The evaluation results indicating no rules were evaluated.</param>
    public NoRulesEvaluatedException(EvaluationResults result) : base("The current evaluation ended without evaluating any rules. (TotalEvaluated == 0)")
    {
        Result = result;
    }

    /// <summary>
    /// Gets the evaluation results that triggered this exception.
    /// </summary>
    public EvaluationResults Result { get; }

}
