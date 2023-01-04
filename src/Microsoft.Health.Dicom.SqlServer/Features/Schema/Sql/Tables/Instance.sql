/*************************************************************
    Instance Table
    Dicom instances with unique Partition Name, and Study, Series and Instance Uid
**************************************************************/
CREATE TABLE dbo.Instance (
    InstanceKey             BIGINT                     NOT NULL,            --PK
    SeriesKey               BIGINT                     NOT NULL,            --FK
    -- StudyKey needed to join directly from Study table to find a instance
    StudyKey                BIGINT                     NOT NULL,            --FK
    --instance keys used in WADO
    StudyInstanceUid        VARCHAR(64)                NOT NULL,
    SeriesInstanceUid       VARCHAR(64)                NOT NULL,
    SopInstanceUid          VARCHAR(64)                NOT NULL,
    --data consistency columns
    Watermark               BIGINT                     NOT NULL,
    Status                  TINYINT                    NOT NULL,
    LastStatusUpdatedDate   DATETIME2(7)               NOT NULL,
    --audit columns
    CreatedDate             DATETIME2(7)               NOT NULL,
    PartitionKey            INT                        NOT NULL DEFAULT 1,  --FK
    --instance metadata
    TransferSyntaxUid       VARCHAR(64)                NULL,
    HasFrameMetadata        BIT                        NOT NULL DEFAULT 0
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Instance on dbo.Instance
(
    SeriesKey,
    InstanceKey
)

--Filter indexes
-- Used in AddInstance, DeleteInstance, DeleteDeletedInstance, QIDO
CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_SopInstanceUid_PartitionKey on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid,
    PartitionKey
)
INCLUDE
(
    Status,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_Status_PartitionKey on dbo.Instance
(
    StudyInstanceUid,
    Status,
    PartitionKey    
)
INCLUDE
(
    SeriesInstanceUid,
    SopInstanceUid,
    Watermark,
    TransferSyntaxUid
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Instance_StudyInstanceUid_SeriesInstanceUid_Status_PartitionKey on dbo.Instance
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Status,
    PartitionKey    
)
INCLUDE
(
    SopInstanceUid,
    Watermark,
    TransferSyntaxUid
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in WADO and QIDO, putting PartitionKey last allows us to query across partitions in the future.
CREATE NONCLUSTERED INDEX IX_Instance_SopInstanceUid_Status_PartitionKey on dbo.Instance
(
    SopInstanceUid,
    Status,
    PartitionKey    
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in GetInstancesByWatermarkRange
CREATE UNIQUE NONCLUSTERED INDEX IX_Instance_Watermark_Status on dbo.Instance
(
    Watermark,
    Status
)
INCLUDE
(
    PartitionKey,
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

-- Cross apply indexes - partition identifiers are not needed
CREATE NONCLUSTERED INDEX IX_Instance_SeriesKey_Status_Watermark on dbo.Instance
(
    SeriesKey,
    Status,
    Watermark
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Instance_StudyKey_Status_Watermark on dbo.Instance
(
    StudyKey,
    Status,
    Watermark
)
INCLUDE
(
    StudyInstanceUid,
    SeriesInstanceUid,
    SopInstanceUid
)
WITH (DATA_COMPRESSION = PAGE)

-- Used in computed columns response
CREATE NONCLUSTERED INDEX IX_Instance_PartitionKey_Watermark on dbo.Instance
(
    PartitionKey,
    Watermark
)
WITH (DATA_COMPRESSION = PAGE)
