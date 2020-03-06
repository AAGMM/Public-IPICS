﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class QueryInstanceTests
    {
        [Fact]
        public void GivenInvalidParameters_WhenCreatingQueryInstance_ExceptionsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryInstance(null, new Dictionary<string, object[]>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance(string.Empty, new Dictionary<string, object[]>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance(new string('a', 65), new Dictionary<string, object[]>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance("AA#AA", new Dictionary<string, object[]>()));
            Assert.Throws<ArgumentNullException>(() => new QueryInstance(Guid.NewGuid().ToString(), null));

            Assert.Throws<ArgumentNullException>(() => QueryInstance.Create(null, null));
            Assert.Throws<ArgumentNullException>(() => QueryInstance.Create(new DicomDataset(), null));
            Assert.Throws<ArgumentException>(() => QueryInstance.Create(new DicomDataset(), Array.Empty<DicomAttributeId>()));
        }

        [Fact]
        public void GivenDicomTagsToIndex_WhenCreatingQueryInstance_TagsAreExtractedAndIndexed()
        {
            var referringPhysicianName = "TestPhysician";
            var dicomSequence = new DicomSequence(
                DicomTag.ReferringPhysicianIdentificationSequence,
                new DicomDataset()
                {
                    { DicomTag.ReferringPhysicianName, referringPhysicianName },
                });

            var dicomDataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.PatientName, Guid.NewGuid().ToString() },
                { DicomTag.StudyDate, DateTime.UtcNow },
            };
            dicomDataset.Add(dicomSequence);

            var patientNameAttribute = new DicomAttributeId(DicomTag.PatientName);
            var studyDateAttribute = new DicomAttributeId(DicomTag.StudyDate);
            var referringPhysicianNameAttribute = new DicomAttributeId(DicomTag.ReferringPhysicianIdentificationSequence, DicomTag.ReferringPhysicianName);
            var studyTimeAttribute = new DicomAttributeId(DicomTag.StudyTime);
            var instance = QueryInstance.Create(
                dicomDataset, new[] { patientNameAttribute, studyDateAttribute, referringPhysicianNameAttribute, studyTimeAttribute });

            Assert.NotNull(instance);
            Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID), instance.InstanceUID);

            Assert.Equal(3, instance.Attributes.Count);
            Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.PatientName), instance.Attributes[patientNameAttribute.AttributeId][0]);
            Assert.Equal(dicomDataset.GetSingleValue<DateTime>(DicomTag.StudyDate), instance.Attributes[studyDateAttribute.AttributeId][0]);
            Assert.Equal(referringPhysicianName, instance.Attributes[referringPhysicianNameAttribute.AttributeId][0]);
        }
    }
}
