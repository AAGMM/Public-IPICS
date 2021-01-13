﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public static class DicomTagExtensions
    {
        public static CustomTagEntry BuildCustomTagEntry(this DicomTag tag, CustomTagLevel level = CustomTagLevel.Series)
        {
            return new CustomTagEntry(path: tag.GetPath(), vr: tag.DictionaryEntry.ValueRepresentations[0].Code, level: level);
        }
    }
}
