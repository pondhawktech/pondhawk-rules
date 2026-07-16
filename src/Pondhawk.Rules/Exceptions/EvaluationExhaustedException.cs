// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Exceptions;

/// <summary>
/// Thrown when an evaluation exceeds the maximum allowed duration or evaluation count.
/// </summary>
public sealed class EvaluationExhaustedException : Exception
{

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationExhaustedException"/> class with the evaluation results.
    /// </summary>
    /// <param name="result">The evaluation results at the time the evaluation was exhausted.</param>
    public EvaluationExhaustedException(EvaluationResults result) : base("The current evaluation ended due to excessive duration or evaluation count. Check the results for circular rules. (FiredRules count very high)")
    {
        Result = result;
    }

    /// <summary>
    /// Gets the evaluation results that triggered this exception.
    /// </summary>
    public EvaluationResults Result { get; }

}
