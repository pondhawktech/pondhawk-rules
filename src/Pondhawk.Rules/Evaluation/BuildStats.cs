// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;

namespace Pondhawk.Rules.Evaluation;

[StructLayout(LayoutKind.Auto)]
internal struct BuildStats
{
    internal int VariationsConsidered;
    internal int VariationsFound;
    internal int StepsAdded;
}
