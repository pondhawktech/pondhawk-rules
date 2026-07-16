// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Humanizer;

namespace Pondhawk.Rules.Validators;

/// <summary>
/// Boolean validation extensions (e.g. Required, IsTrue, IsFalse).
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Ex suffix is an intentional domain convention for extension method classes")]
public static class BoolValidatorEx
{

    /// <summary>
    /// Validates that the boolean property is <c>true</c> (i.e. the value is required to be truthy).
    /// </summary>
    /// <typeparam name="TFact">The fact type.</typeparam>
    /// <param name="validator">The validator to extend.</param>
    /// <returns>The validator for fluent chaining.</returns>
    public static IValidator<TFact, bool> Required<TFact>(this IValidator<TFact, bool> validator) where TFact : class
    {
        var v = validator.Is((f, v) => v);

        var propName = validator.PropertyName.Humanize(LetterCasing.Title);

        v.Otherwise($"{propName} is required");

        return v;
    }

    /// <summary>
    /// Validates that the boolean property is <c>true</c>.
    /// </summary>
    /// <typeparam name="TFact">The fact type.</typeparam>
    /// <param name="validator">The validator to extend.</param>
    /// <returns>The validator for fluent chaining.</returns>
    public static IValidator<TFact, bool> IsTrue<TFact>(this IValidator<TFact, bool> validator) where TFact : class
    {

        var v = validator.Is((f, v) => v);

        var propName = validator.PropertyName.Humanize(LetterCasing.Title);

        v.Otherwise($"{propName} is not true");

        return v;

    }

    /// <summary>
    /// Validates that the boolean property is <c>false</c>.
    /// </summary>
    /// <typeparam name="TFact">The fact type.</typeparam>
    /// <param name="validator">The validator to extend.</param>
    /// <returns>The validator for fluent chaining.</returns>
    public static IValidator<TFact, bool> IsFalse<TFact>(this IValidator<TFact, bool> validator) where TFact : class
    {

        var v = validator.IsNot((f, v) => v);

        var propName = validator.PropertyName.Humanize(LetterCasing.Title);

        v.Otherwise($"{propName} is not false");

        return v;

    }


}
