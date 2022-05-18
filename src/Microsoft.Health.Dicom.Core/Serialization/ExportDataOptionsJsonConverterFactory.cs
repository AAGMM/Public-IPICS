﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Serialization;

internal sealed class ExportDataOptionsJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ExportDataOptions<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type arg = EnsureArg.IsNotNull(typeToConvert, nameof(typeToConvert)).GetGenericArguments()[0];
        if (arg == typeof(ExportSourceType))
            return new ExportDataOptionsJsonConverter<ExportSourceType>(MapSourceType);
        else if (arg == typeof(ExportDestinationType))
            return new ExportDataOptionsJsonConverter<ExportDestinationType>(MapDestinationType);
        else
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidType, typeToConvert));
    }

    private static Type MapSourceType(ExportSourceType type)
        => type switch
        {
            ExportSourceType.Identifiers => typeof(IdentifierExportOptions),
            _ => throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DicomCoreResource.UnexpectedValue,
                    nameof(ExportDataOptions<ExportSourceType>.Type),
                    nameof(ExportSourceType.Identifiers))),
        };

    private static Type MapDestinationType(ExportDestinationType type)
        => type switch
        {
            ExportDestinationType.AzureBlob => typeof(AzureBlobExportOptions),
            _ => throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DicomCoreResource.UnexpectedValue,
                    nameof(ExportDataOptions<ExportDestinationType>.Type),
                    nameof(ExportDestinationType.AzureBlob))),
        };
}
