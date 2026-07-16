// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules;

namespace Pondhawk.Rules.Validators;

/// <summary>
/// Default implementation of <see cref="IEnumerableValidator{TFact, TType}"/> for enumerable property validation.
/// </summary>
/// <param name="rule">The validation rule that owns this validator.</param>
/// <param name="group">The group name used to categorize violation messages.</param>
/// <param name="propertyName">The name of the enumerable property being validated.</param>
/// <param name="extractor">A function that extracts the enumerable from the fact.</param>
public class EnumerableValidator<TFact, TType>(ValidationRule<TFact> rule, string group, string propertyName, Func<TFact, IEnumerable<TType>> extractor)
    : BaseValidator<TFact>(rule, group), IEnumerableValidator<TFact, TType>
{
    /// <inheritdoc />
    public string GroupName { get; } = group;

    /// <inheritdoc />
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the function that extracts the enumerable from the fact.
    /// </summary>
    protected Func<TFact, IEnumerable<TType>> Extractor { get; } = extractor;


    /// <inheritdoc />
    public IEnumerableValidator<TFact, TType> Is(Func<TFact, IEnumerable<TType>, bool> condition)
    {
        bool Cond(TFact f) => condition(f, Extractor(f));
        Conditions.Add(Cond);
        return this;
    }


    /// <inheritdoc />
    public IEnumerableValidator<TFact, TType> IsNot(Func<TFact, IEnumerable<TType>, bool> condition)
    {
        bool Cond(TFact f) => !condition(f, Extractor(f));
        Conditions.Add(Cond);
        return this;
    }
}
