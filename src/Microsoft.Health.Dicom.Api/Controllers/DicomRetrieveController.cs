﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [Authorize]
    public class DicomRetrieveController : Controller
    {
        public const string TransferSyntaxHeaderName = "transfer-syntax";
        private const string AcceptHeaderName = "accept";
        private readonly IMediator _mediator;
        private readonly ILogger<DicomRetrieveController> _logger;

        public DicomRetrieveController(IMediator mediator, ILogger<DicomRetrieveController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.PartialContent)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/")]
        public async Task<IActionResult> GetStudyAsync([FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax, string studyInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomStudyAsync(studyInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<DicomDataset>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/metadata")]
        public async Task<IActionResult> GetStudyMetadataAsync(string studyInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomStudyMetadataAsync(studyInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.PartialContent)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}")]
        public async Task<IActionResult> GetSeriesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomSeriesAsync(
                                studyInstanceUID, seriesInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/metadata")]
        public async Task<IActionResult> GetSeriesMetadataAsync(string studyInstanceUID, string seriesInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomSeriesMetadataAsync(
                                studyInstanceUID, seriesInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream, KnownContentTypes.ApplicationDicom)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}")]
        public async Task<IActionResult> GetInstanceAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomInstanceAsync(
                            studyInstanceUID, seriesInstanceUID, sopInstanceUID, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/metadata")]
        public async Task<IActionResult> GetInstanceMetadataAsync(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Metadata Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomMetadataResponse response = await _mediator.RetrieveDicomInstanceMetadataAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, HttpContext.RequestAborted);
            return StatusCode(response.StatusCode, response.ResponseMetadata);
        }

        [AcceptContentFilter(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/rendered")]
        public async Task<IActionResult> GetInstanceRenderedAsync(
            [FromHeader(Name = AcceptHeaderName)] string requestedFormat,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Rendered Requested request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomInstanceRenderedAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, requestedFormat, false, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
        [ProducesResponseType(typeof(IEnumerable<Stream>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/thumbnail")]
        public async Task<IActionResult> GetInstanceThumbnailRenderedAsync(
            [FromHeader(Name = AcceptHeaderName)] string requestedFormat,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID)
        {
            _logger.LogInformation($"DICOM Web Retrieve Thumbnail Requested request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomInstanceRenderedAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, requestedFormat, true, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ApplicationOctetStream)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/frames/{frames}")]
        public async Task<IActionResult> GetFramesAsync(
            [FromHeader(Name = TransferSyntaxHeaderName)] string transferSyntax,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID,
            [ModelBinder(typeof(IntArrayModelBinder))] int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}', frames: '{string.Join(", ", frames ?? Array.Empty<int>())}'.");
            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomFramesAsync(
                            studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames, transferSyntax, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/frames/{frames}/rendered")]
        public async Task<IActionResult> GetFrameRenderedAsync(
            [FromHeader(Name = AcceptHeaderName)] string requestedFormat,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID,
            int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction Rendered request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}', frames: '{string.Join(", ", frames)}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomFramesRenderedAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames, requestedFormat, false, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        [AcceptContentFilter(KnownContentTypes.ImageJpeg, KnownContentTypes.ImagePng)]
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.NotAcceptable)]
        [HttpGet]
        [Route("studies/{studyInstanceUID}/series/{seriesInstanceUID}/instances/{sopInstanceUID}/frames/{frames}/thumbnail")]
        public async Task<IActionResult> GetFrameThumbnailRenderedAsync(
            [FromHeader(Name = AcceptHeaderName)] string requestedFormat,
            string studyInstanceUID,
            string seriesInstanceUID,
            string sopInstanceUID,
            int[] frames)
        {
            _logger.LogInformation($"DICOM Web Retrieve Transaction Thumbnail request received, for study: '{studyInstanceUID}', series: '{seriesInstanceUID}', instance: '{sopInstanceUID}', frames: '{string.Join(", ", frames)}'.");

            RetrieveDicomResourceResponse response = await _mediator.RetrieveDicomFramesRenderedAsync(
                studyInstanceUID, seriesInstanceUID, sopInstanceUID, frames, requestedFormat, true, HttpContext.RequestAborted);
            return ConvertToActionResult(response);
        }

        private IActionResult ConvertToActionResult(RetrieveDicomResourceResponse response)
        {
            if (response.ResponseStreams == null)
            {
                return StatusCode(response.StatusCode);
            }

            return new MultipartResult(response.StatusCode, response.ResponseStreams.Select(x => new MultipartItem(KnownContentTypes.ApplicationDicom, x)).ToList());
        }
    }
}
