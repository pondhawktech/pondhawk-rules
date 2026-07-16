// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Evaluation;

internal sealed class Schema
{
    public Type FactType { get; set; }
    public List<int> Members { get; } = [];
}

