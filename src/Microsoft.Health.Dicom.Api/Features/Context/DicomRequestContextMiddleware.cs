﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;

namespace Microsoft.Health.Dicom.Api.Features.Context;

public class DicomRequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public DicomRequestContextMiddleware(RequestDelegate next)
    {
        _next = EnsureArg.IsNotNull(next, nameof(next));
    }

    public async Task Invoke(HttpContext context, IDicomRequestContextAccessor dicomRequestContextAccessor)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));
        HttpRequest request = context.Request;

        var baseUri = new Uri(UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase));

        var uri = new Uri(UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            request.Path,
            request.QueryString));

        var dicomRequestContext = new DicomRequestContext(
            method: request.Method,
            uri,
            baseUri,
            correlationId: System.Diagnostics.Activity.Current?.RootId,
            context.Request.Headers,
            context.Response.Headers);

        dicomRequestContextAccessor.RequestContext = dicomRequestContext;

        // Call the next delegate/middleware in the pipeline
        await _next(context);
    }
}
