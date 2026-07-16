// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules;

namespace Pondhawk.Rules.Validators;

/// <summary>
/// Default implementation of <see cref="IValidator{TFact, TType}"/> for single-value property validation.
/// </summary>
/// <param name="rule">The validation rule that owns this validator.</param>
/// <param name="group">The group name used to categorize violation messages.</param>
/// <param name="propertyName">The name of the property being validated.</param>
/// <param name="extractor">A function that extracts the property value from the fact.</param>
public class Validator<TFact, TType>(ValidationRule<TFact> rule, string group, string propertyName, Func<TFact, TType> extractor)
    : BaseValidator<TFact>(rule, group), IValidator<TFact, TType>
{
    /// <inheritdoc />
    public string GroupName { get; } = group;

    /// <inheritdoc />
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the function that extracts the property value from the fact.
    /// </summary>
    protected Func<TFact, TType> Extractor { get; } = extractor;


    /// <inheritdoc />
    public IValidator<TFact, TType> Is(Func<TFact, TType, bool> condition)
    {
        bool Cond(TFact f) => condition(f, Extractor(f));
        Conditions.Add(Cond);
        return this;
    }


    /// <inheritdoc />
    public IValidator<TFact, TType> IsNot(Func<TFact, TType, bool> condition)
    {
        bool Cond(TFact f) => !condition(f, Extractor(f));
        Conditions.Add(Cond);
        return this;
    }

}
