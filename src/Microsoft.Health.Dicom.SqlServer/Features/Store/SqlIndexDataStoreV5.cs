﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    /// <summary>
    /// Sql IndexDataStore version 5.
    /// </summary>
    internal class SqlIndexDataStoreV5 : SqlIndexDataStoreV4
    {
        public SqlIndexDataStoreV5(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
            : base(sqlConnectionWrapperFactory)
        {
        }

        public override SchemaVersion Version => SchemaVersion.V5;

        public override async Task<long> BeginCreateInstanceIndexAsync(int partitionKey, DicomDataset instance, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags.Where(tag => tag.IsExtendedQueryTag), Version);
                V8.AddInstanceV2TableValuedParameters parameters = new V8.AddInstanceV2TableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows
                );

                V8.AddInstanceV2.PopulateCommand(
                    sqlCommandWrapper,
                    instance.GetString(DicomTag.StudyInstanceUID),
                    instance.GetString(DicomTag.SeriesInstanceUID),
                    instance.GetString(DicomTag.SOPInstanceUID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientID),
                    instance.GetSingleValueOrDefault<string>(DicomTag.PatientName),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName),
                    instance.GetStringDateAsDate(DicomTag.StudyDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.StudyDescription),
                    instance.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber),
                    instance.GetSingleValueOrDefault<string>(DicomTag.Modality),
                    instance.GetStringDateAsDate(DicomTag.PerformedProcedureStepStartDate),
                    instance.GetStringDateAsDate(DicomTag.PatientBirthDate),
                    instance.GetSingleValueOrDefault<string>(DicomTag.ManufacturerModelName),
                    (byte)IndexStatus.Creating,
                    parameters);

                try
                {
                    return (long)(await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlErrorCodes.Conflict)
                    {
                        if (ex.State == (byte)IndexStatus.Creating)
                        {
                            throw new PendingInstanceException();
                        }

                        throw new InstanceAlreadyExistsException();
                    }

                    throw new DataStoreException(ex);
                }
            }
        }

        public override async Task ReindexInstanceAsync(DicomDataset instance, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                var rows = ExtendedQueryTagDataRowsBuilder.Build(instance, queryTags, Version);
                V8.IndexInstanceV2TableValuedParameters parameters = new V8.IndexInstanceV2TableValuedParameters(
                    rows.StringRows,
                    rows.LongRows,
                    rows.DoubleRows,
                    rows.DateTimeWithUtcRows,
                    rows.PersonNameRows);

                V8.IndexInstanceV2.PopulateCommand(sqlCommandWrapper, watermark, parameters);

                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    throw ex.Number switch
                    {
                        SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                        SqlErrorCodes.Conflict => new PendingInstanceException(),
                        _ => new DataStoreException(ex),
                    };

                }
            }
        }
    }
}
