﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class CustomTagEntryExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetValidCustomTagEntries))]
        public void GivenValidCustomTagEntry_WhenFormalizing_ThenShouldReturnSameEntry(CustomTagEntry entry)
        {
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.Path, normalized.Path);
            Assert.Equal(entry.VR, normalized.VR);
            Assert.Equal(entry.Level, normalized.Level);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]

        public void GivenStandardTagWithoutVR_WhenFormalizing_ThenVRShouldBeFilled(string vr)
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), vr, CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(tag.GetDefaultVR().Code, normalized.VR);
        }

        [Fact]
        public void GivenStandardTagWithVR_WhenFormalizing_ThenVRShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string vr = DicomVR.CS.Code;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), vr, CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(vr, normalized.VR);
        }

        [Fact]
        public void GivenTagOfLowerCase_WhenFormalizing_ThenTagShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath().ToLowerInvariant(), tag.GetDefaultVR().Code, CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.Path.ToUpperInvariant(), normalized.Path);
        }

        [Fact]
        public void GivenVROfLowerCase_WhenFormalizing_ThenVRShouldBeUpperCase()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            CustomTagEntry entry = new CustomTagEntry(tag.GetPath(), tag.GetDefaultVR().Code.ToLowerInvariant(), CustomTagLevel.Instance, CustomTagStatus.Added);
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(entry.VR.ToUpperInvariant(), normalized.VR);
        }

        [Fact]

        public void GivenStandardTagAsKeyword_WhenFormalizing_ThenVRShouldBeFilled()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            CustomTagEntry entry = new CustomTagEntry(path: tag.DictionaryEntry.Keyword, tag.GetDefaultVR().Code, CustomTagLevel.Instance, CustomTagStatus.Added);
            string expectedPath = tag.GetPath();
            CustomTagEntry normalized = entry.Normalize();
            Assert.Equal(normalized.Path, expectedPath);
        }

        public static IEnumerable<object[]> GetValidCustomTagEntries()
        {
            yield return new object[] { DicomTag.DeviceSerialNumber.BuildCustomTagEntry() }; // standard custom tag with VR
            yield return new object[] { new CustomTagEntry("12051003", DicomVRCode.OB, CustomTagLevel.Instance, CustomTagStatus.Added) }; // private tag with VR
        }
    }
}
