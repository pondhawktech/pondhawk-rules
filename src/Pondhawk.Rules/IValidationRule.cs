// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Builder;

namespace Pondhawk.Rules;

/// <summary>
/// Marker interface for validation rules targeting fact type <typeparamref name="TFact"/>.
/// </summary>
public interface IValidationRule<out TFact> : IRule
{

}
