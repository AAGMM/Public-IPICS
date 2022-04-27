# Extended Query Tags

## Overview

By default, the Medical Imaging Server for DICOM supports querying on the DICOM tags specified in the [conformance statement](https://github.com/microsoft/dicom-server/blob/main/docs/resources/conformance-statement.md#searchable-attributes). However, this list of tags may be expanded by enabling _extended query tags_. Using the APIs listed below, users can additionally index their DICOM studies, series, and instances on both standard and private DICOM tags such that they can be specified in QIDO-RS.



## APIs

### Version: v1-prerelease, v1

To help manage the supported tags in a given DICOM server instance, the following API endpoints have been added.

| API                                               | Description                                                  |
| ------------------------------------------------- | ------------------------------------------------------------ |
| POST       .../extendedquerytags                  | [Add Extended Query Tags](#add-extended-query-tags)          |
| GET         .../extendedquerytags                 | [List Extended Query Tags](#list-extended-query-tags)        |
| GET         .../extendedquerytags/{tagPath}       | [Get Extended Query Tag](#get-extended-query-tag)            |
| DELETE  .../extendedquerytags/{tagPath}           | [Delete Extended Query Tag](#delete-extended-query-tag)      |
| PATCH   .../extendedquerytags/{tagPath}           | [Update Extended Query Tag](#update-extended-query-tag)      |
| GET        .../extendedquerytags/{tagPath}/errors | [List Extended Query Tag Errors](#list-extended-query-tag-errors) |
| GET        .../operations/{operationId}           | [Get Operation](#get-operation)                              |

### Add Extended Query Tags

Add one or more extended query tags and starts a long-running operation that re-indexes current DICOM instances on the specified tag(s).

```http
POST .../extendedquerytags
```

#### Request Header

| Name         | Required | Type   | Description                     |
| ------------ | -------- | ------ | ------------------------------- |
| Content-Type | True     | string | `application/json` is supported |

#### Request Body

| Name | Required | Type                                                         | Description |
| ---- | -------- | ------------------------------------------------------------ | ----------- |
| body |          | [Extended Query Tag for Adding](#extended-query-tag-for-adding)`[]` |             |

#### Limitations

The following VR types are supported:

| VR   | Description           | Single Value Matching | Range Matching | Fuzzy Matching |
| ---- | --------------------- | --------------------- | -------------- | -------------- |
| AE   | Application Entity    | X                     |                |                |
| AS   | Age String            | X                     |                |                |
| CS   | Code String           | X                     |                |                |
| DA   | Date                  | X                     | X              |                |
| DS   | Decimal String        | X                     |                |                |
| DT   | Date Time             | X                     | X              |                |
| FD   | Floating Point Double | X                     |                |                |
| FL   | Floating Point Single | X                     |                |                |
| IS   | Integer String        | X                     |                |                |
| LO   | Long String           | X                     |                |                |
| PN   | Person Name           | X                     |                | X              |
| SH   | Short String          | X                     |                |                |
| SL   | Signed Long           | X                     |                |                |
| SS   | Signed Short          | X                     |                |                |
| TM   | Time                  | X                     | X              |                |
| UI   | Unique Identifier     | X                     |                |                |
| UL   | Unsigned Long         | X                     |                |                |
| US   | Unsigned Short        | X                     |                |                |

> Sequential tags i.e. tags under a tag of type Sequence of Items (SQ) are currently not supported.

> You can add up to 128 extended query tags.

> Only index the first value of a single valued data element that incorrectly has multiple values.

#### Responses

| Name              | Type                                        | Description                                                  |
| ----------------- | ------------------------------------------- | ------------------------------------------------------------ |
| 202 (Accepted)    | [Operation Reference](#operation-reference) | Extended query tag(s) have been added, and a long-running operation has been started to re-index existing DICOM instances |
| 400 (Bad Request) |                                             | Request body has invalid data                                |
| 409 (Conflict)    |                                             | One or more requested query tags already are supported       |

### List Extended Query Tags

Lists of all extended query tag(s).

```http
GET .../extendedquerytags
```

#### Responses

| Name     | Type                                          | Description                 |
| -------- | --------------------------------------------- | --------------------------- |
| 200 (OK) | [Extended Query Tag](#extended-query-tag)`[]` | Returns extended query tags |

### Get Extended Query Tag

Get an extended query tag.

```http
GET .../extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| tagPath | path | True     | string | tagPath is the path for the tag, which can be either tag or keyword. E.g. Patient Id is represented by `00100020` or `PatientId` |

####  Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 200 (OK)          | [Extended Query Tag](#extended-query-tag) | The extended query tag with the specified `tagPath`    |
| 400 (Bad Request) |                                           | Requested tag path is invalid                          |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

### Delete Extended Query Tag

Delete an extended query tag.

```http
DELETE .../extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| tagPath | path | True     | string | tagPath is the path for the tag, which can be either tag or keyword. E.g. Patient Id is represented by `00100020` or `PatientId` |

#### Responses

| Name              | Type | Description                                                  |
| ----------------- | ---- | ------------------------------------------------------------ |
| 204 (No Content)  |      | Extended query tag with requested tagPath has been successfully deleted. |
| 400 (Bad Request) |      | Requested tag path is invalid.                               |
| 404 (Not Found)   |      | Extended query tag with requested tagPath is not found       |

### Update Extended Query Tag

Update an extended query tag.

```http
PATCH .../extendedquerytags/{tagPath}
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| tagPath | path | True     | string | tagPath is the path for the tag, which can be either tag or keyword. E.g. Patient Id is represented by `00100020` or `PatientId` |

#### Request Header

| Name         | Required | Type   | Description                      |
| ------------ | -------- | ------ | -------------------------------- |
| Content-Type | True     | string | `application/json` is supported. |

#### Request Body

| Name | Required | Type                                                         | Description |
| ---- | -------- | ------------------------------------------------------------ | ----------- |
| body |          | [Extended Query Tag for Updating](#extended-query-tag-for-updating) |             |

#### Responses

| Name              | Type                                      | Description                                            |
| ----------------- | ----------------------------------------- | ------------------------------------------------------ |
| 20 (OK)           | [Extended Query Tag](#extended-query-tag) | The updated extended query tag                         |
| 400 (Bad Request) |                                           | Requested tag path or body is invalid                  |
| 404 (Not Found)   |                                           | Extended query tag with requested tagPath is not found |

### List Extended Query Tag Errors

Lists errors on an extended query tag.

```http
GET .../extendedquerytags/{tagPath}/errors
```

#### URI Parameters

| Name    | In   | Required | Type   | Description                                                  |
| ------- | ---- | -------- | ------ | ------------------------------------------------------------ |
| tagPath | path | True     | string | tagPath is the path for the tag, which can be either tag or keyword. E.g. Patient Id is represented by `00100020` or `PatientId` |

####  Responses

| Name              | Type                                                       | Description                                               |
| ----------------- | ---------------------------------------------------------- | --------------------------------------------------------- |
| 200 (OK)          | [Extended Query Tag Error](#extended-query-tag-error) `[]` | List of extended query tag errors associated with the tag |
| 400 (Bad Request) |                                                            | Requested tag path is invalid                             |
| 404 (Not Found)   |                                                            | Extended query tag with requested tagPath is not found    |

### Get Operation

Get a long-running operation.

```http
GET .../operations/{operationId}
```

#### URI Parameters

| Name        | In   | Required | Type   | Description      |
| ----------- | ---- | -------- | ------ | ---------------- |
| operationId | path | True     | string | The operation id |

#### Responses

| Name            | Type                    | Description                                  |
| --------------- | ----------------------- | -------------------------------------------- |
| 200 (OK)        | [Operation](#operation) | The completed operation for the specified ID |
| 202 (Accepted)  | [Operation](#operation) | The running operation for the specified ID   |
| 404 (Not Found) |                         | The operation is not found                   |

## QIDO with Extended Query Tags

### Tag Status

The [Status](#extended-query-tag-status) of Extended query tag indicates current status. When an extended query tag is first added, its status is set to `Adding`, and a long-running operation is kicked off to reindex existing DICOM instances. After the operation is completed, the tag status is updated to `Ready`. The extended query tag can now be used in [QIDO](../resources/conformance-statement.md#search-qido-rs).

For example, if the tag Manufacturer Model Name (0008,1090) is added, and in `Ready` status, hereafter the following queries can be used to filter stored instances by Manufacturer Model Name:

```http
../instances?ManufacturerModelName=Microsoft
```

They can also be used in conjunction with existing tags. E.g:

```http
../instances?00081090=Microsoft&PatientName=Jo&fuzzyMatching=true
```

> After extended query tag is added, any DICOM instance stored is indexed on it

### Tag Query Status

[QueryStatus](#extended-query-tag-status) indicates whether QIDO is allowed for the tag. When a reindex operation fails to process one or more DICOM instances for a tag, that tag's QueryStatus is set to `Disabled` automatically. You can choose to ignore indexing errors and allow queries to use this tag by setting the `QueryStatus` to `Enabled` via  [Update Extended Query Tag](#update-extended-query-tag) API. Any QIDO requests that reference at least one manually enabled tag will include the set of tags with indexing errors in the response header `erroneous-dicom-attributes`.

For example, suppose the extended query tag `PatientAge` had errors during reindexing, but was enabled manually. For the query below, you would be able to see `PatientAge` in the `erroneous-dicom-attributes` header.

```http
../instances?PatientAge=035Y
```

## Definitions

### Extended Query Tag

A non-standard DICOM tag that will be supported for QIDO-RS.

| Name           | Type                                                         | Description                                                  |
| -------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Path           | string                                                       | Path of tag, normally composed of group id and element id. E.g. `PatientId` (0010,0020) has path 00100020 |
| VR             | string                                                       | Value representation of this tag                             |
| PrivateCreator | string                                                       | Identification code of the implementer of this private tag   |
| Level          | [Extended Query Tag Level](#extended-query-tag-level)        | Level of extended query tag                                  |
| Status         | [Extended Query Tag Status](#extended-query-tag-status)      | Status of the extended query tag                             |
| QueryStatus    | [Extended Query Tag Query Status](#extended-query-tag-query-status) | Query status of extended query tag                           |
| Errors         | [Extended Query Tag Errors Reference](#extended-query-tag-errors-reference) | Reference to extended query tag errors                       |
| Operation      | [Operation Reference](#operation-reference)                  | Reference to a long-running operation                        |

**Example1:** a standard tag (0008,0070) in `Ready` status.

```json
{
    "status": "Ready",
    "level": "Instance",
    "queryStatus": "Enabled",
    "path": "00080070",
    "vr": "LO"
}
```

**Example2:**  a standard tag (0010,1010) in `Adding` status.  An operation with id `1a5d0306d9624f699929ee1a59ed57a0` is running on it, and 21 errors has occurred so far.

```json
{
    "status": "Adding",
    "level": "Study",
    "errors": {
        "count": 21,
        "href": "https://localhost:63838/extendedquerytags/00101010/errors"
    },
    "operation": {
        "id": "1a5d0306d9624f699929ee1a59ed57a0",
        "href": "https://localhost:63838/operations/1a5d0306d9624f699929ee1a59ed57a0"
    },
    "queryStatus": "Disabled",
    "path": "00101010",
    "vr": "AS"
}
```

### Operation Reference

Reference to a long-running operation.

| Name | Type   | Description          |
| ---- | ------ | -------------------- |
| Id   | string | operation id         |
| Href | string | Uri to the operation |

### Operation

Represents a long-running operation.

| Name            | Type                                  | Description                                                  |
| --------------- | ------------------------------------- | ------------------------------------------------------------ |
| OperationId     | string                                | The operation Id                                             |
| OperationType   | [Operation Type](#operation-type)     | Type of  the long running operation                          |
| CreatedTime     | string                                | Time when the operation was created                          |
| LastUpdatedTime | string                                | Time when the operation was updated last time                |
| Status          | [Operation Status](#operation-status) | Represents run time status of operation                      |
| PercentComplete | Integer                               | Percentage of work that has been completed by the operation  |
| Resources       | string`[]`                            | Collection of resources locations that the operation is creating or manipulating |

**Example:** a running reindex operation.

```json
{
    "resources": [
        "https://localhost:63838/extendedquerytags/00101010"
    ],
    "operationId": "a99a8b51-78d4-4fd9-b004-b6c0bcaccf1d",
    "type": "Reindex",
    "createdTime": "2021-10-06T16:40:02.5247083Z",
    "lastUpdatedTime": "2021-10-06T16:40:04.5152934Z",
    "status": "Running",
    "percentComplete": 10
}
```



### Operation Status

Represents run time status of long running operation.

| Name       | Type   | Description                                                  |
| ---------- | ------ | ------------------------------------------------------------ |
| NotStarted | string | The operation is not started                                 |
| Running    | string | The operation is executing and has not yet finished          |
| Completed  | string | The operation has finished successfully                      |
| Failed     | string | The operation has stopped prematurely after encountering one or more errors |

### Extended Query Tag Error

An error that occurred during an extended query tag indexing operation.

| Name              | Type   | Description                                       |
| ----------------- | ------ | ------------------------------------------------- |
| StudyInstanceUid  | string | Study instance UID where indexing errors occured  |
| SeriesInstanceUid | string | Series instance UID where indexing errors occured |
| SopInstanceUid    | string | Sop instance UID where indexing errors occured    |
| CreatedTime       | string | Time when error occured(UTC)                      |
| ErrorMessage      | string | Error message                                     |

**Example**:  an unexpected value length error on an DICOM instance. It occurred at 2021-10-06T16:41:44.4783136.

```json
{
    "studyInstanceUid": "2.25.253658084841524753870559471415339023884",
    "seriesInstanceUid": "2.25.309809095970466602239093351963447277833",
    "sopInstanceUid": "2.25.225286918605419873651833906117051809629",
    "createdTime": "2021-10-06T16:41:44.4783136",
    "errorMessage": "Value length is not expected."
}
```

### Extended Query Tag Errors Reference

Reference to extended query tag errors.

| Name  | Type    | Description                                      |
| ----- | ------- | ------------------------------------------------ |
| Count | Integer | Total number of errors on the extended query tag |
| Href  | string  | Uri to extended query tag errors                 |

### Operation Type

The type of  a long-running operation.

| Name    | Type   | Description                                                  |
| ------- | ------ | ------------------------------------------------------------ |
| Reindex | string | A reindex operation that updates the indices for previously added data based on new tags |

### Extended Query Tag Status

The status of  extended query tag.

| Name     | Type   | Description                                                  |
| -------- | ------ | ------------------------------------------------------------ |
| Adding   | string | The extended query tag has been added, and a long-running operation is reindexing existing DICOM instances |
| Ready    | string | The extended query tag  is ready for QIDO-RS                 |
| Deleting | string | The extended query tag  is being deleted                     |

### Extended Query Tag Level

The level of the DICOM information hierarchy where this tag applies.

| Name     | Type   | Description                                              |
| -------- | ------ | -------------------------------------------------------- |
| Instance | string | The extended query tag is relevant at the instance level |
| Series   | string | The extended query tag is relevant at the series level   |
| Study    | string | The extended query tag is relevant at the study level    |

### Extended Query Tag Query Status

The query status of extended query tag.

| Name     | Type   | Description                                         |
| -------- | ------ | --------------------------------------------------- |
| Disabled | string | The extended query tag is not allowed to be queried |
| Enabled  | string | The extended query tag is allowed to be queried     |

> Note:  Errors during reindex operation disables QIDO on the extended query tag. You can call [Update Extended Query Tag](#update-extended-query-tag) API to enable it.

### Extended Query Tag for Updating

Represents extended query tag for updating.

| Name        | Type                                                         | Description                            |
| ----------- | ------------------------------------------------------------ | -------------------------------------- |
| QueryStatus | [Extended Query Tag Query Status](#extended-query-tag-query-status) | The query status of extended query tag |

### Extended Query Tag for Adding

Represents extended query tag for adding.

| Name           | Required | Type                                                  | Description                                                  |
| -------------- | -------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| Path           | True     | string                                                | Path of tag, normally composed of group id and element id. E.g. `PatientId` (0010,0020) has path 00100020 |
| VR             |          | string                                                | Value representation of this tag.  It's optional for standard tag, and required for private tag |
| PrivateCreator |          | string                                                | Identification code of the implementer of this private tag. Only set when the tag is a private tag |
| Level          | True     | [Extended Query Tag Level](#extended-query-tag-level) | Represents the hierarchy at which this tag is relevant. Should be one of Study, Series or Instance |

**Example1:**  `MicrosoftPC` is defining the private tag (0401,1001) with the `SS` value representation on Instance level

```json
{
    "Path": "04011001",
    "VR": "SS",
    "PrivateCreator": "MicrosoftPC",
    "Level": "Instance"
}
```

**Example2:** the standard tag with keyword `ManufacturerModelName` with the `LO` value representation is defined on Series level

```json
{
    "Path": "ManufacturerModelName",
    "VR": "LO",
    "Level": "Series"
}
```

 **Example3:** the standard tag (0010,0040) is defined on studies: the value representation is already defined by the DICOM standard

```json
{
    "Path": "00100040",
    "Level": "Study"
}
```
