﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveTransferSyntaxHandler : IRetrieveTransferSyntaxHandler
    {
        private static readonly IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> AcceptableDescriptors =
           new Dictionary<ResourceType, AcceptHeaderDescriptors>()
           {
                { ResourceType.Study, DescriptorsForGetStudy() },
                { ResourceType.Series, DescriptorsForGetSeries() },
                { ResourceType.Instance, DescriptorsForGetInstance() },
                { ResourceType.Frames, DescriptorsForGetFrame() },
           };

        private static AcceptHeaderDescriptors DescriptorsForGetStudy()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.MultipartRelated,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetSeries()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.MultipartRelated,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetInstance()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.SinglePart,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetFrame()
        {
            // Follow http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_8.7.3
            return new AcceptHeaderDescriptors(
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ApplicationOctetStream,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageJpeg,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEGProcess14SV1.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEGProcess14)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageDicomRle,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.RLELossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.RLELossless)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageJpegLs,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEGLSLossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEGLSLossless, DicomTransferSyntax.JPEGLSNearLossless)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageJpeg2000,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Lossless, DicomTransferSyntax.JPEG2000Lossy)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageJpeg2000Part2,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly, DicomTransferSyntax.JPEG2000Part2MultiComponent)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.VideoMpeg2,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.MPEG2.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.MPEG2, DicomTransferSyntax.MPEG2MainProfileHighLevel)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.VideoMp4,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41,
                     DicomTransferSyntax.MPEG4AVCH264BDCompatibleHighProfileLevel41,
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For2DVideo,
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For3DVideo,
                     DicomTransferSyntax.MPEG4AVCH264StereoHighProfileLevel42)));
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params DicomTransferSyntax[] transferSyntaxes)
        {
            return GetAcceptableTransferSyntaxSet(transferSyntaxes.Select(item => item.UID.UID).ToArray());
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params string[] transferSyntaxes)
        {
            return new HashSet<string>(transferSyntaxes, StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetTransferSyntax(ResourceType resourceType, IEnumerable<AcceptHeader> acceptHeaders)
        {
            EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));
            AcceptHeaderDescriptors descriptors = AcceptableDescriptors[resourceType];

            // get all accceptable headers and sort by quality (ascendently)
            SortedDictionary<AcceptHeader, string> accepted = new SortedDictionary<AcceptHeader, string>(new AcceptHeaderQualityComparer());
            foreach (AcceptHeader header in acceptHeaders)
            {
                AcceptHeaderDescriptor acceptableHeaderDescriptor;
                string transfersyntax;
                if (descriptors.TryGetMatchedDescriptor(header, out acceptableHeaderDescriptor, out transfersyntax))
                {
                    accepted.Add(header, transfersyntax);
                }
            }

            if (accepted.Count == 0)
            {
                throw new NotAcceptableException(DicomCoreResource.NotAcceptableHeaders);
            }

            // Last elment has largest quality
            return accepted.Last().Value;
        }
    }
}
