// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
public class RetrieveRenderedService : IRetrieveRenderedService
{
    private readonly IFileStore _blobDataStore;
    private readonly IInstanceStore _instanceStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RetrieveConfiguration _retrieveConfiguration;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<RetrieveRenderedService> _logger;

    public RetrieveRenderedService(
        IInstanceStore instanceStore,
        IFileStore blobDataStore,
        IDicomRequestContextAccessor dicomRequestContextAccessor,
        IOptionsSnapshot<RetrieveConfiguration> retrieveConfiguration,
        RecyclableMemoryStreamManager recyclableMemoryStreamManager,
        ILogger<RetrieveRenderedService> logger)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        EnsureArg.IsNotNull(blobDataStore, nameof(blobDataStore));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        EnsureArg.IsNotNull(retrieveConfiguration?.Value, nameof(retrieveConfiguration));
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _instanceStore = instanceStore;
        _blobDataStore = blobDataStore;
        _dicomRequestContextAccessor = dicomRequestContextAccessor;
        _retrieveConfiguration = retrieveConfiguration?.Value;
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _logger = logger;
    }

    public async Task<RetrieveRenderedResponse> RetrieveRenderedImageAsync(RetrieveRenderedRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        // To keep track of how long render operation is taking
        Stopwatch sw = new Stopwatch();

        int partitionKey = _dicomRequestContextAccessor.RequestContext.GetPartitionKey();
        AcceptHeader returnHeader = GetValidRenderAcceptHeader(request.AcceptHeaders);

        if (request.Quality < 1 || request.Quality > 100)
        {
            throw new BadRequestException(DicomCoreResource.InvalidImageQuality);
        }

        try
        {
            // this call throws NotFound when zero instance found
            InstanceMetadata instance = (await _instanceStore.GetInstancesWithProperties(
                ResourceType.Instance, partitionKey, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid, cancellationToken)).First();

            FileProperties fileProperties = await RetrieveHelpers.CheckFileSize(_blobDataStore, _retrieveConfiguration.MaxDicomFileSize, instance.VersionedInstanceIdentifier.Version, true, cancellationToken);
            using Stream stream = await _blobDataStore.GetFileAsync(instance.VersionedInstanceIdentifier.Version, cancellationToken);
            sw.Start();

            DicomFile dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
            DicomPixelData dicomPixelData = dicomFile.GetPixelDataAndValidateFrames(new[] { request.FrameNumber });

            Stream resultStream = await ConvertToImage(dicomFile, request.FrameNumber, returnHeader.MediaType.ToString(), request.Quality, cancellationToken);
            string outputContentType = returnHeader.MediaType.ToString();

            sw.Stop();
            _logger.LogInformation("Render from dicom to {OutputContentType}, uncompressed file size was {UncompressedFrameSize}, output frame size is {OutputFrameSize} and took {ElapsedMilliseconds} ms", outputContentType, stream.Length, resultStream.Length, sw.ElapsedMilliseconds);

            return new RetrieveRenderedResponse(resultStream, resultStream.Length, outputContentType);
        }

        catch (DataStoreException e)
        {
            // Log request details associated with exception. Note that the details are not for the store call that failed but for the request only.
            _logger.LogError(e, "Error retrieving dicom resource to render");
            throw;
        }

    }

    private async Task<Stream> ConvertToImage(DicomFile dicomFile, int frameNumber, string mediaType, int quality, CancellationToken cancellationToken)
    {
        try
        {
            DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
            using var img = dicomImage.RenderImage(frameNumber);
            using var sharpImage = img.AsSharpImage();
            MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();

            if (mediaType.Equals(KnownContentTypes.ImageJpeg, StringComparison.OrdinalIgnoreCase))
            {
                JpegEncoder jpegEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder();
                jpegEncoder.Quality = quality;
                await sharpImage.SaveAsJpegAsync(resultStream, jpegEncoder, cancellationToken: cancellationToken);
            }
            else
            {
                await sharpImage.SaveAsPngAsync(resultStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder(), cancellationToken: cancellationToken);
            }

            resultStream.Position = 0;

            return resultStream;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error rendering dicom file into {OutputConentType} media type", mediaType);
            throw new DicomImageException();
        }
    }

    private static AcceptHeader GetValidRenderAcceptHeader(IReadOnlyCollection<AcceptHeader> acceptHeaders)
    {
        EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));

        if (acceptHeaders.Count > 1)
        {
            throw new NotAcceptableException(DicomCoreResource.MultipleAcceptHeadersNotSupported);
        }
        else if (acceptHeaders.Count == 1 && acceptHeaders.First().MediaType != null && !StringSegment.Equals(acceptHeaders.First().MediaType, KnownContentTypes.ImageJpeg, StringComparison.InvariantCultureIgnoreCase))
        {
            if (StringSegment.Equals(acceptHeaders.First().MediaType, KnownContentTypes.ImagePng, StringComparison.InvariantCultureIgnoreCase))
            {
                return new AcceptHeader(KnownContentTypes.ImagePng, PayloadTypes.SinglePart);
            }
            else if (!StringSegment.Equals(acceptHeaders.First().MediaType, KnownContentTypes.ImageJpeg, StringComparison.InvariantCultureIgnoreCase))
            {
            throw new NotAcceptableException(DicomCoreResource.NotAcceptableHeaders);
        }
        }

        return new AcceptHeader(KnownContentTypes.ImageJpeg, PayloadTypes.SinglePart);
    }
}
