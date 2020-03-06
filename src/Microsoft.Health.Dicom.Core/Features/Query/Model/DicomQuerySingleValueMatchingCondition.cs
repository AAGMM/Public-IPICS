﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DicomQuerySingleValueMatchingCondition<T> : DicomQueryFilterCondition
    {
        internal DicomQuerySingleValueMatchingCondition(DicomTag tag, T value)
            : base(tag)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
