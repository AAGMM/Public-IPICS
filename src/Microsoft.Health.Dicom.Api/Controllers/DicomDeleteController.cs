﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Delete;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    [Authorize]
    public class DicomDeleteController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DicomDeleteController> _logger;

        public DicomDeleteController(IMediator mediator, ILogger<DicomDeleteController> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _logger = logger;
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.StudyRoute)]
        public async Task<IActionResult> DeleteStudyAsync(string studyInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Delete Study request received, with study instance UID '{studyInstanceUid}'.");

            DicomDeleteResourcesResponse dicomDeleteResponse = await _mediator.DeleteDicomStudyAsync(
                studyInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.SeriesRoute)]
        public async Task<IActionResult> DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Delete Series request received, with study instance UID '{studyInstanceUid}' and series UID '{seriesInstanceUid}'.");

            DicomDeleteResourcesResponse dicomDeleteResponse = await _mediator.DeleteDicomSeriesAsync(
                studyInstanceUid, seriesInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [Route(KnownRoutes.InstanceRoute)]
        public async Task<IActionResult> DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            _logger.LogInformation($"DICOM Web Delete Instance request received, with study instance UID '{studyInstanceUid}', series UID '{seriesInstanceUid}' and instance UID '{sopInstanceUid}'.");

            DicomDeleteResourcesResponse dicomDeleteResponse = await _mediator.DeleteDicomInstanceAsync(
                studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
