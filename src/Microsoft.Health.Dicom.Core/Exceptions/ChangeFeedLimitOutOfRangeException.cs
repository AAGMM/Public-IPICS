﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class ChangeFeedLimitOutOfRangeException : ValidationException
{
    public ChangeFeedLimitOutOfRangeException(int max)
        : base(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.ChangeFeedLimitOutOfRange, max))
    {
    }
}
