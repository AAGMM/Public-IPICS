﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using System;

namespace Microsoft.Health.DicomCast.Core.UnitTests
{
    public static class DicomCastDatasetGenerator
    {
        public static readonly DateTime DefaultStudyDateTime = new DateTime(1974, 7, 10, 7, 10, 24);
        public static readonly DateTime DefaultSeriesDateTime = new DateTime(1974, 8, 10, 8, 10, 24);
        public const string DefaultSOPClassUID = "4444";
        public const string DefaultStudyDescription = "Study Description";
        public const string DefaultSeriesDescription = "Series Description";
        public const string DefaultModalitiesInStudy = "MODALITY";
        public const string DefaultModality = "MODALITY";
        public const string DefaultSeriesNumber = "1";
        public const string DefaultInstanceNumber = "1";
        public const string DefaultAccessionNumber = "1";

        public static DicomDataset CreateDicomDataset(string sopClassUid = null, string studyDescription = null, string seriesDescrition = null, string modalityInStudy = null, string modalityInSeries = null, string seriesNumber = null, string instanceNumber = null, string accessionNumber = null)
        {
            var ds = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian)
                {
                    { DicomTag.SOPClassUID, sopClassUid ?? DefaultSOPClassUID },
                    { DicomTag.StudyDate, DefaultStudyDateTime },
                    { DicomTag.StudyTime, DefaultStudyDateTime },
                    { DicomTag.SeriesDate, DefaultSeriesDateTime },
                    { DicomTag.SeriesTime, DefaultSeriesDateTime },
                    { DicomTag.StudyDescription, studyDescription ?? DefaultStudyDescription },
                    { DicomTag.SeriesDescription, seriesDescrition ?? DefaultSeriesDescription },
                    { DicomTag.ModalitiesInStudy, modalityInStudy ?? DefaultModalitiesInStudy },
                    { DicomTag.Modality, modalityInSeries ?? DefaultModality },
                    { DicomTag.SeriesNumber, seriesNumber ?? DefaultSeriesNumber },
                    { DicomTag.InstanceNumber, instanceNumber ?? DefaultInstanceNumber },
                    { DicomTag.AccessionNumber, accessionNumber ?? DefaultAccessionNumber },
                    { DicomTag.StudyInstanceUID, DicomUID.Generate().UID},
                    { DicomTag.SeriesInstanceUID, DicomUID.Generate().UID },
                    { DicomTag.SOPInstanceUID, DicomUID.Generate().UID}
                };

            return ds;
        }
    }
}
