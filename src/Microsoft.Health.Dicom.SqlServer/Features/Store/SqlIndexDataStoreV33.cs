// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

/// <summary>
/// Sql IndexDataStore version 33.
/// </summary>
internal class SqlIndexDataStoreV33 : SqlIndexDataStoreV32
{
    public SqlIndexDataStoreV33(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        : base(sqlConnectionWrapperFactory)
    {
    }

    public override SchemaVersion Version => SchemaVersion.V33;
    public override async Task<IEnumerable<InstanceMetadata>> BeginUpdateInstanceAsync(int partitionKey, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        var results = new List<InstanceMetadata>();

        using (SqlConnectionWrapper sqlConnectionWrapper = await SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            VLatest.BeginUpdateInstanceV33.PopulateCommand(
                sqlCommandWrapper,
                partitionKey,
                studyInstanceUid);

            try
            {
                using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        (string rStudyInstanceUid,
                            string rSeriesInstanceUid,
                            string rSopInstanceUid,
                            long watermark,
                            string rTransferSyntaxUid,
                            bool rHasFrameMetadata,
                            long? originalWatermark,
                            long? newWatermark) = reader.ReadRow(
                               VLatest.Instance.StudyInstanceUid,
                               VLatest.Instance.SeriesInstanceUid,
                               VLatest.Instance.SopInstanceUid,
                               VLatest.Instance.Watermark,
                               VLatest.Instance.TransferSyntaxUid,
                               VLatest.Instance.HasFrameMetadata,
                               VLatest.Instance.OriginalWatermark,
                               VLatest.Instance.NewWatermark);

                        results.Add(
                            new InstanceMetadata(
                                new VersionedInstanceIdentifier(
                                    rStudyInstanceUid,
                                    rSeriesInstanceUid,
                                    rSopInstanceUid,
                                    watermark,
                                    partitionKey),
                                new InstanceProperties()
                                {
                                    TransferSyntaxUid = rTransferSyntaxUid,
                                    HasFrameMetadata = rHasFrameMetadata,
                                    OriginalVersion = originalWatermark,
                                    NewVersion = newWatermark
                                }));
                    }
                }
                return results;
            }
            catch (SqlException ex)
            {
                throw ex.Number switch
                {
                    SqlErrorCodes.NotFound => new InstanceNotFoundException(),
                    _ => new DataStoreException(ex),
                };
            }
        }
    }
}
