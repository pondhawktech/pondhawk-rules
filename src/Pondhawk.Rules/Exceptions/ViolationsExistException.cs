// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Exceptions;

/// <summary>
/// Thrown when rule evaluation produces one or more violation events.
/// </summary>
public sealed class ViolationsExistException : Exception
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ViolationsExistException"/> class with the evaluation results that contain violations.
    /// </summary>
    /// <param name="result">The evaluation results containing violation events.</param>
    public ViolationsExistException(EvaluationResults result) : base("Violation events occurred during evaluation")
    {
        Result = result;
    }

    /// <summary>
    /// Gets the evaluation results that triggered this exception.
    /// </summary>
    public EvaluationResults Result { get; }

    /// <summary>
    /// Gets the violation events from the evaluation results.
    /// </summary>
    public IEnumerable<RuleEvent> Violations
    {
        get { return Result.Events.Where(e => e.Category == RuleEvent.EventCategory.Violation); }
    }

}
