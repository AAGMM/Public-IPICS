﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class Transcoder : ITranscoder
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private static readonly DicomTransferSyntax DefaultTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

        public Transcoder(
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task<Stream> TranscodeFileAsync(Stream stream, string requestedTransferSyntax)
        {
            DicomTransferSyntax parsedDicomTransferSyntax =
                   string.IsNullOrWhiteSpace(requestedTransferSyntax) ?
                       DefaultTransferSyntax :
                       DicomTransferSyntax.Parse(requestedTransferSyntax);

            bool canTranscode = false;
            DicomFile dicomFile;

            try
            {
                dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
                canTranscode = dicomFile.Dataset.CanTranscodeDataset(parsedDicomTransferSyntax);
            }
            catch (DicomFileException)
            {
                throw;
            }

            stream.Seek(0, SeekOrigin.Begin);

            // If the instance is not transcodeable a TranscodingException should be thrown.
            if (!canTranscode)
            {
                throw new TranscodingException();
            }

            return await TranscodeFileAsync(dicomFile, parsedDicomTransferSyntax);
        }

        public Stream TranscodeFrame(DicomFile dicomFile, int frameIndex, string requestedTransferSyntax)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            DicomDataset dataset = dicomFile.Dataset;

            // Validate requested frame index exists in file.
            dicomFile.GetPixelDataAndValidateFrames(new[] { frameIndex });

            DicomTransferSyntax parsedDicomTransferSyntax =
                   string.IsNullOrWhiteSpace(requestedTransferSyntax) ?
                       DefaultTransferSyntax :
                       DicomTransferSyntax.Parse(requestedTransferSyntax);

            IByteBuffer resultByteBuffer;

            if (!dicomFile.Dataset.CanTranscodeDataset(parsedDicomTransferSyntax))
            {
                throw new TranscodingException();
            }

            // Decompress single frame from source dataset
            var transcoder = new DicomTranscoder(dataset.InternalTransferSyntax, parsedDicomTransferSyntax);
            resultByteBuffer = transcoder.DecodeFrame(dataset, frameIndex);

            return _recyclableMemoryStreamManager.GetStream("RetrieveDicomResourceHandler.GetFrameAsDicomData", resultByteBuffer.Data, 0, resultByteBuffer.Data.Length);
        }

        private async Task<Stream> TranscodeFileAsync(DicomFile dicomFile, DicomTransferSyntax requestedTransferSyntax)
        {
            try
            {
                var transcoder = new DicomTranscoder(
                    dicomFile.Dataset.InternalTransferSyntax,
                    requestedTransferSyntax);
                dicomFile = transcoder.Transcode(dicomFile);
            }
            catch
            {
                // TODO: Reevaluate this while fixing transcoding handling.
                // We catch all here as Transcoder can throw a wide variety of things.
                // Basically this means codec failure - a quite extraordinary situation, but not impossible
                // Proper solution here would be to actually try transcoding all the files that we are
                // returning and either form a PartialContent or NotAcceptable response with an extra error message in
                // the headers. Because transcoding is an expensive operation, we choose to do it from within the
                // LazyTransformReadOnlyStream at the time when response is being formed by the server, therefore this code
                // is called from ASP.NET framework and at this point we can not change our server response.
                // The decision for now is just to return an empty stream here letting the client handle it.
                // In the future a more optimal solution may involve maintaining a cache of transcoded images and
                // using that to determine if transcoding is possible from within the Handle method.

                throw new TranscodingException();
            }

            MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

            if (dicomFile != null)
            {
                await dicomFile.SaveAsync(resultStream);
                resultStream.Seek(offset: 0, loc: SeekOrigin.Begin);
            }

            return resultStream;
        }
    }
}
