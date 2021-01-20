﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Status of custom tag.
    /// </summary>
    public enum CustomTagStatus
    {
        /// <summary>
        /// The custom tag is being reindexed.
        /// </summary>
        Reindexing = 0,

        /// <summary>
        /// The custom tag has been added to system.
        /// </summary>
        Added = 1,

        /// <summary>
        /// The custom tag is being deindexed.
        /// </summary>
        Deindexing = 2,
    }
}
