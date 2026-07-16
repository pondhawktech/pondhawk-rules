// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Pondhawk.Rules.Tests")]

namespace Pondhawk.Rules.Evaluation;

[StructLayout(LayoutKind.Auto)]
internal readonly struct EvaluationStep
{
    public int Priority { get; init; }
    public long Selector { get; init; }
    public int Signature { get; init; }
}

