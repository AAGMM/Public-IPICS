﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Base class for all client input validation exceptions
    /// </summary>
    public abstract class DicomValidationException : DicomServerException
    {
        public DicomValidationException(string message)
            : base(message)
        {
        }
    }
}
