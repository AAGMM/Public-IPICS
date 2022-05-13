﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Web;
using DicomAudit = Microsoft.Health.Dicom.Api.Features.Audit;

namespace Microsoft.Health.Dicom.Api.Controllers;

[ApiVersion("1.0-prerelease")]
[ApiVersion("1")]
[ServiceFilter(typeof(DicomAudit.AuditLoggingFilterAttribute))]
public class ExtendedQueryTagController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExtendedQueryTagController> _logger;
    private readonly bool _featureEnabled;

    public ExtendedQueryTagController(
        IMediator mediator,
        IOptions<FeatureConfiguration> featureConfiguration,
        ILogger<ExtendedQueryTagController> logger)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _mediator = mediator;
        _logger = logger;
        _featureEnabled = featureConfiguration.Value.EnableExtendedQueryTags;
    }

    [HttpPost]
    [BodyModelStateValidator]
    [Produces(KnownContentTypes.ApplicationJson)]
    [Consumes(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(AddExtendedQueryTagResponse), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [VersionedRoute(KnownRoutes.ExtendedQueryTagRoute)]
    [AuditEventType(AuditEventSubType.AddExtendedQueryTag)]
    public async Task<IActionResult> PostAsync([Required][FromBody] IReadOnlyCollection<AddExtendedQueryTagEntry> extendedQueryTags)
    {
        _logger.LogInformation("DICOM Web Add Extended Query Tag request received, with extendedQueryTags {ExtendedQueryTags}.", extendedQueryTags);

        EnsureFeatureIsEnabled();
        AddExtendedQueryTagResponse response = await _mediator.AddExtendedQueryTagsAsync(extendedQueryTags, HttpContext.RequestAborted);

        Response.AddLocationHeader(response.Operation.Href);
        return StatusCode((int)HttpStatusCode.Accepted, response.Operation);
    }

    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(DeleteExtendedQueryTagResponse), (int)HttpStatusCode.NoContent)]
    [HttpDelete]
    [VersionedRoute(KnownRoutes.DeleteExtendedQueryTagRoute)]
    [AuditEventType(AuditEventSubType.RemoveExtendedQueryTag)]
    public async Task<IActionResult> DeleteAsync(string tagPath)
    {
        _logger.LogInformation("DICOM Web Delete Extended Query Tag request received, with extended query tag path {TagPath}.", tagPath);

        EnsureFeatureIsEnabled();
        await _mediator.DeleteExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Handles requests to get all extended query tags.
    /// </summary>
    /// <param name="options">Options for configuring which tags are returned.</param>
    /// <returns>
    /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
    /// extended query tag or if no extended query tags are stored. Returns OK with a JSON body of all tags in other cases.
    /// </returns>
    [HttpGet]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(IEnumerable<GetExtendedQueryTagEntry>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [VersionedRoute(KnownRoutes.ExtendedQueryTagRoute)]
    [AuditEventType(AuditEventSubType.GetAllExtendedQueryTags)]
    [QueryModelStateValidator]
    public async Task<IActionResult> GetTagsAsync([FromQuery] PaginationOptions options)
    {
        // TODO: Enforce the above data annotations with ModelState.IsValid or use the [ApiController] attribute
        // for automatic error generation. However, we should change all errors across the API surface.
        _logger.LogInformation("DICOM Web Get Extended Query Tag request received for all extended query tags");

        EnsureArg.IsNotNull(options, nameof(options));
        EnsureFeatureIsEnabled();
        GetExtendedQueryTagsResponse response = await _mediator.GetExtendedQueryTagsAsync(
            options.Limit,
            options.Offset,
            HttpContext.RequestAborted);

        return StatusCode(
            (int)HttpStatusCode.OK, response.ExtendedQueryTags);
    }

    /// <summary>
    /// Handles requests to get individual extended query tags.
    /// </summary>
    /// <param name="tagPath">Path for requested extended query tag.</param>
    /// <returns>
    /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
    /// extended query tag. Returns OK with a JSON body of requested tag in other cases.
    /// </returns>
    [HttpGet]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(GetExtendedQueryTagEntry), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [VersionedRoute(KnownRoutes.GetExtendedQueryTagRoute, Name = KnownRouteNames.GetExtendedQueryTag)]
    [AuditEventType(AuditEventSubType.GetExtendedQueryTag)]
    public async Task<IActionResult> GetTagAsync(string tagPath)
    {
        _logger.LogInformation("DICOM Web Get Extended Query Tag request received for extended query tag: {TagPath}", tagPath);

        EnsureFeatureIsEnabled();
        GetExtendedQueryTagResponse response = await _mediator.GetExtendedQueryTagAsync(tagPath, HttpContext.RequestAborted);

        return StatusCode(
            (int)HttpStatusCode.OK, response.ExtendedQueryTag);
    }

    /// <summary>
    /// Handles requests to get extended query tag errors.
    /// </summary>
    /// <param name="tagPath">Path for requested extended query tag.</param>
    /// <param name="options">Options for configuring which errors are returned.</param>
    /// <returns>
    /// Returns Bad Request if given path can't be parsed. Returns Not Found if given path doesn't map to a stored
    /// error. Returns OK with a JSON body of requested tag error in other cases.
    /// </returns>
    [HttpGet]
    [Produces(KnownContentTypes.ApplicationJson)]
    [ProducesResponseType(typeof(IEnumerable<ExtendedQueryTagError>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [VersionedRoute(KnownRoutes.GetExtendedQueryTagErrorsRoute, Name = KnownRouteNames.GetExtendedQueryTagErrors)]
    [AuditEventType(AuditEventSubType.GetExtendedQueryTagErrors)]
    [QueryModelStateValidator]
    public async Task<IActionResult> GetTagErrorsAsync(
        [FromRoute] string tagPath,
        [FromQuery] PaginationOptions options)
    {
        _logger.LogInformation("DICOM Web Get Extended Query Tag Errors request received for extended query tag: {TagPath}", tagPath);

        EnsureArg.IsNotNull(options, nameof(options));
        EnsureFeatureIsEnabled();
        GetExtendedQueryTagErrorsResponse response = await _mediator.GetExtendedQueryTagErrorsAsync(
            tagPath,
            options.Limit,
            options.Offset,
            HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.ExtendedQueryTagErrors);
    }

    [HttpPatch]
    [Produces(KnownContentTypes.ApplicationJson)]
    [BodyModelStateValidator]
    [ProducesResponseType(typeof(GetExtendedQueryTagEntry), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [VersionedRoute(KnownRoutes.UpdateExtendedQueryTagQueryStatusRoute)]
    [AuditEventType(AuditEventSubType.UpdateExtendedQueryTag)]
    public async Task<IActionResult> UpdateTagAsync([FromRoute] string tagPath, [FromBody] UpdateExtendedQueryTagOptions newValue)
    {
        EnsureArg.IsNotNull(newValue, nameof(newValue));
        _logger.LogInformation("DICOM Web Update Extended Query Tag Query Status request received for extended query tag {TagPath} and new value {NewValue}", tagPath, $"{nameof(newValue.QueryStatus)}: '{newValue.QueryStatus}'");

        EnsureFeatureIsEnabled();
        var response = await _mediator.UpdateExtendedQueryTagAsync(tagPath, newValue.ToEntry(), HttpContext.RequestAborted);

        return StatusCode((int)HttpStatusCode.OK, response.TagEntry);
    }

    private void EnsureFeatureIsEnabled()
    {
        if (!_featureEnabled)
        {
            throw new ExtendedQueryTagFeatureDisabledException();
        }
    }
}
