using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PaperlessMCP.Client;
using PaperlessMCP.Models.Common;
using PaperlessMCP.Models.Documents;
using static PaperlessMCP.Utils.ParsingHelpers;

namespace PaperlessMCP.Tools;

/// <summary>
/// MCP tools for document operations.
/// </summary>
[McpServerToolType]
public static class DocumentTools
{
    [McpServerTool(Name = "paperless_documents_search")]
    [Description("Search for documents with full-text search and filters. Supports pagination.")]
    public static async Task<string> Search(
        PaperlessClient client,
        [Description("Full-text search query")] string? query = null,
        [Description("Filter by tag IDs (comma-separated)")] string? tags = null,
        [Description("Exclude tag IDs (comma-separated)")] string? tagsExclude = null,
        [Description("Filter by correspondent ID")] int? correspondent = null,
        [Description("Filter by document type ID")] int? documentType = null,
        [Description("Filter by storage path ID")] int? storagePath = null,
        [Description("Filter by documents created after this date (YYYY-MM-DD)")] string? createdAfter = null,
        [Description("Filter by documents created before this date (YYYY-MM-DD)")] string? createdBefore = null,
        [Description("Filter by documents added after this date (YYYY-MM-DD)")] string? addedAfter = null,
        [Description("Filter by documents added before this date (YYYY-MM-DD)")] string? addedBefore = null,
        [Description("Filter by archive serial number")] int? archiveSerialNumber = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size (default: 25, max: 100)")] int pageSize = 25,
        [Description("Ordering field (e.g., 'created', '-created', 'title')")] string? ordering = null,
        [Description("Include document content in results (default: false). Use paperless.documents.get for full content.")] bool includeContent = false,
        [Description("Max content length per document when includeContent=true (default: 500). Use 0 for unlimited.")] int contentMaxLength = 500)
    {
        var tagIds = ParseIntArray(tags);
        var tagExcludeIds = ParseIntArray(tagsExclude);

        DateTime? createdAfterDate = ParseDate(createdAfter);
        DateTime? createdBeforeDate = ParseDate(createdBefore);
        DateTime? addedAfterDate = ParseDate(addedAfter);
        DateTime? addedBeforeDate = ParseDate(addedBefore);

        var result = await client.SearchDocumentsAsync(
            query: query,
            tags: tagIds,
            tagsExclude: tagExcludeIds,
            correspondent: correspondent,
            documentType: documentType,
            storagePath: storagePath,
            createdAfter: createdAfterDate,
            createdBefore: createdBeforeDate,
            addedAfter: addedAfterDate,
            addedBefore: addedBeforeDate,
            archiveSerialNumber: archiveSerialNumber,
            page: page,
            pageSize: Math.Min(pageSize, 100),
            ordering: ordering
        ).ConfigureAwait(false);

        // Map to lightweight summaries to reduce response size
        var summaries = result.Results
            .Select(r => DocumentSummary.FromSearchResult(
                r,
                includeContent,
                contentMaxLength > 0 ? contentMaxLength : null))
            .ToList();

        var response = McpResponse<object>.Success(
            summaries,
            new McpMeta
            {
                Page = page,
                PageSize = pageSize,
                Total = result.Count,
                Next = result.Next,
                PaperlessBaseUrl = client.BaseUrl
            }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_get")]
    [Description("Get a document by its ID.")]
    public static async Task<string> Get(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("Include the document's notes (default: false). Notes can be very large and are not needed for filing/triage.")] bool includeNotes = false,
        [Description("Max characters of OCR content to return (default: 0 = unlimited).")] int contentMaxLength = 0)
    {
        var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

        if (document == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"Document with ID {id} not found",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        // Trim oversized fields: notes are often enormous and never needed for
        // triage/filing; content can be capped on request. Both default to lean.
        document = document with
        {
            Notes = includeNotes ? document.Notes : null,
            Content = (contentMaxLength > 0 && document.Content.Length > contentMaxLength)
                ? document.Content[..contentMaxLength] + "..."
                : document.Content,
        };

        var response = McpResponse<Document>.Success(
            document,
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_download")]
    [Description("Get download URLs for a document's original file, preview, and thumbnail.")]
    public static async Task<string> Download(
        PaperlessClient client,
        [Description("Document ID")] int id)
    {
        var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

        if (document == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"Document with ID {id} not found",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var downloadInfo = client.GetDocumentDownloadInfo(id, document.Title, document.OriginalFileName);

        var response = McpResponse<DocumentDownload>.Success(
            downloadInfo,
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_preview")]
    [Description("Get the preview URL for a document.")]
    public static async Task<string> Preview(
        PaperlessClient client,
        [Description("Document ID")] int id)
    {
        var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

        if (document == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"Document with ID {id} not found",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var downloadInfo = client.GetDocumentDownloadInfo(id, document.Title, document.OriginalFileName);

        var response = McpResponse<object>.Success(
            new { id, title = document.Title, preview_url = downloadInfo.PreviewUrl },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_thumbnail")]
    [Description("Get the thumbnail URL for a document.")]
    public static async Task<string> Thumbnail(
        PaperlessClient client,
        [Description("Document ID")] int id)
    {
        var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

        if (document == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"Document with ID {id} not found",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var downloadInfo = client.GetDocumentDownloadInfo(id, document.Title, document.OriginalFileName);

        var response = McpResponse<object>.Success(
            new { id, title = document.Title, thumbnail_url = downloadInfo.ThumbnailUrl },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_upload")]
    [Description("Upload a new document to Paperless-ngx. Provide file content as base64. For large files, use paperless.documents.upload_from_path instead.")]
    public static async Task<string> Upload(
        PaperlessClient client,
        [Description("Base64-encoded file content")] string fileContent,
        [Description("Original filename with extension")] string fileName,
        [Description("Document title (optional)")] string? title = null,
        [Description("Correspondent ID (optional)")] int? correspondent = null,
        [Description("Document type ID (optional)")] int? documentType = null,
        [Description("Storage path ID (optional)")] int? storagePath = null,
        [Description("Tag IDs (comma-separated, optional)")] string? tags = null,
        [Description("Archive serial number (optional)")] int? archiveSerialNumber = null,
        [Description("Created date (YYYY-MM-DD, optional)")] string? created = null)
    {
        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(fileContent);
        }
        catch (FormatException)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.Validation,
                "Invalid base64 file content",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var metadata = new DocumentUploadRequest
        {
            Title = title,
            Correspondent = correspondent,
            DocumentType = documentType,
            StoragePath = storagePath,
            Tags = ParseIntArray(tags)?.ToList(),
            ArchiveSerialNumber = archiveSerialNumber,
            Created = ParseDate(created)
        };

        var taskId = await client.UploadDocumentAsync(fileBytes, fileName, metadata).ConfigureAwait(false);

        if (taskId == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.UpstreamError,
                "Failed to upload document",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var response = McpResponse<object>.Success(
            new { task_id = taskId, status = "queued", message = "Document uploaded and queued for processing" },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_upload_from_path")]
    [Description("Upload a document from a local file path. More reliable than base64 upload for large files. Includes automatic retries.")]
    public static async Task<string> UploadFromPath(
        PaperlessClient client,
        [Description("Absolute path to the file to upload")] string filePath,
        [Description("Document title (optional, defaults to filename)")] string? title = null,
        [Description("Correspondent ID (optional)")] int? correspondent = null,
        [Description("Document type ID (optional)")] int? documentType = null,
        [Description("Storage path ID (optional)")] int? storagePath = null,
        [Description("Tag IDs (comma-separated, optional)")] string? tags = null,
        [Description("Archive serial number (optional)")] int? archiveSerialNumber = null,
        [Description("Created date (YYYY-MM-DD, optional)")] string? created = null)
    {
        // Expand ~ to home directory
        if (filePath.StartsWith("~/"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            filePath = Path.Combine(home, filePath[2..]);
        }

        // Validate path
        if (!Path.IsPathRooted(filePath))
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.Validation,
                "File path must be absolute",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        if (!File.Exists(filePath))
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"File not found: {filePath}",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var fileInfo = new FileInfo(filePath);
        var metadata = new DocumentUploadRequest
        {
            Title = title ?? Path.GetFileNameWithoutExtension(filePath),
            Correspondent = correspondent,
            DocumentType = documentType,
            StoragePath = storagePath,
            Tags = ParseIntArray(tags)?.ToList(),
            ArchiveSerialNumber = archiveSerialNumber,
            Created = ParseDate(created)
        };

        var (taskId, error) = await client.UploadDocumentFromPathAsync(filePath, metadata).ConfigureAwait(false);

        if (taskId == null)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.UpstreamError,
                error ?? "Failed to upload document",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var response = McpResponse<object>.Success(
            new
            {
                task_id = taskId,
                status = "queued",
                message = "Document uploaded and queued for processing",
                file_name = fileInfo.Name,
                file_size = fileInfo.Length
            },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_update")]
    [Description("Update document metadata (title, correspondent, type, tags, etc.).")]
    public static async Task<string> Update(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("New title (optional)")] string? title = null,
        [Description("Correspondent ID (optional, use -1 to clear)")] int? correspondent = null,
        [Description("Document type ID (optional, use -1 to clear)")] int? documentType = null,
        [Description("Storage path ID (optional, use -1 to clear)")] int? storagePath = null,
        [Description("Tag IDs to set (comma-separated, optional)")] string? tags = null,
        [Description("Archive serial number (optional)")] int? archiveSerialNumber = null,
        [Description("Created date (YYYY-MM-DD, optional)")] string? created = null,
        [Description("Document type by NAME (optional; exact, case-insensitive; resolved server-side; never creates). Mutually exclusive with documentType.")] string? documentTypeName = null,
        [Description("Correspondent by NAME (optional; exact, case-insensitive; resolved server-side; never creates). Mutually exclusive with correspondent.")] string? correspondentName = null)
    {
        string ValidationError(string message, object? details = null) =>
            JsonSerializer.Serialize(McpErrorResponse.Create(
                ErrorCodes.Validation, message, details,
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }));

        // Resolve name-based document type / correspondent up front; any failure aborts before writing.
        var resolvedDocumentType = documentType;
        if (documentTypeName != null)
        {
            if (documentType != null)
                return ValidationError("Provide either documentType (id) or documentTypeName, not both.");

            var matches = await client.FindDocumentTypesByNameAsync(documentTypeName).ConfigureAwait(false);
            if (matches.Count == 0)
                return ValidationError($"Unknown document type '{documentTypeName}'; no changes were made.");
            if (matches.Count > 1)
                return ValidationError($"Ambiguous document type '{documentTypeName}' (multiple matches); no changes were made.");
            resolvedDocumentType = matches[0].Id;
        }

        var resolvedCorrespondent = correspondent;
        if (correspondentName != null)
        {
            if (correspondent != null)
                return ValidationError("Provide either correspondent (id) or correspondentName, not both.");

            var matches = await client.FindCorrespondentsByNameAsync(correspondentName).ConfigureAwait(false);
            if (matches.Count == 0)
                return ValidationError($"Unknown correspondent '{correspondentName}'; no changes were made.");
            if (matches.Count > 1)
                return ValidationError($"Ambiguous correspondent '{correspondentName}' (multiple matches); no changes were made.");
            resolvedCorrespondent = matches[0].Id;
        }

        var request = new DocumentUpdateRequest
        {
            Title = title,
            Correspondent = resolvedCorrespondent == -1 ? null : resolvedCorrespondent,
            DocumentType = resolvedDocumentType == -1 ? null : resolvedDocumentType,
            StoragePath = storagePath == -1 ? null : storagePath,
            Tags = ParseIntArray(tags)?.ToList(),
            ArchiveSerialNumber = archiveSerialNumber,
            Created = ParseDate(created)
        };

        var result = await client.UpdateDocumentWithResultAsync(id, request).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var error = result.Error!;
            var errorResponse = McpErrorResponse.Create(
                error.StatusCode == System.Net.HttpStatusCode.NotFound ? ErrorCodes.NotFound : ErrorCodes.UpstreamError,
                $"Failed to update document {id}: {error.Message}",
                new { status_code = (int)error.StatusCode, response_body = error.ResponseBody },
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var response = McpResponse<Document>.Success(
            result.Value!,
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_add_tags_by_name")]
    [Description("Add one or more tags to a document by tag NAME (comma-separated), resolving names to IDs server-side. Existing tags are preserved; adding a tag the document already has is a no-op. Never creates tags — unknown or ambiguous names fail without changing anything.")]
    public static Task<string> AddTagsByName(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("Tag names to add (comma-separated, case-insensitive)")] string names)
        => ModifyTagsByName(client, id, names, add: true);

    [McpServerTool(Name = "paperless_documents_remove_tags_by_name")]
    [Description("Remove one or more tags from a document by tag NAME (comma-separated), resolving names to IDs server-side. Other tags are preserved; removing a tag the document doesn't have is a no-op. Never creates tags — unknown or ambiguous names fail without changing anything.")]
    public static Task<string> RemoveTagsByName(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("Tag names to remove (comma-separated, case-insensitive)")] string names)
        => ModifyTagsByName(client, id, names, add: false);

    /// <summary>
    /// Shared implementation for add/remove tags-by-name. Resolves every requested name to a
    /// tag id up front (atomic): if any name is unknown or ambiguous, nothing is written and an
    /// error listing the offending names is returned. Otherwise the resolved ids are unioned with
    /// (add) or subtracted from (remove) the document's current tags; the PATCH is skipped when the
    /// set is unchanged (idempotent). The resulting tag set is echoed back as {id, name} pairs.
    /// </summary>
    private static async Task<string> ModifyTagsByName(PaperlessClient client, int id, string names, bool add)
    {
        var requested = (names ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (requested.Length == 0)
        {
            return JsonSerializer.Serialize(McpErrorResponse.Create(
                ErrorCodes.Validation,
                "No tag names provided. Pass a comma-separated list of tag names.",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }));
        }

        // Resolve every requested name first so the operation is all-or-nothing.
        var resolvedIds = new HashSet<int>();
        var unknown = new List<string>();
        var ambiguous = new List<string>();
        foreach (var name in requested)
        {
            var matches = await client.FindTagsByNameAsync(name).ConfigureAwait(false);
            if (matches.Count == 0) unknown.Add(name);
            else if (matches.Count > 1) ambiguous.Add(name);
            else resolvedIds.Add(matches[0].Id);
        }

        if (unknown.Count > 0 || ambiguous.Count > 0)
        {
            return JsonSerializer.Serialize(McpErrorResponse.Create(
                ErrorCodes.Validation,
                "One or more tag names could not be resolved; no changes were made.",
                new { unknown_tags = unknown, ambiguous_tags = ambiguous },
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }));
        }

        var document = await client.GetDocumentAsync(id).ConfigureAwait(false);
        if (document == null)
        {
            return JsonSerializer.Serialize(McpErrorResponse.Create(
                ErrorCodes.NotFound,
                $"Document with ID {id} not found",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }));
        }

        var current = new HashSet<int>(document.Tags);
        var updated = new HashSet<int>(current);
        if (add) updated.UnionWith(resolvedIds);
        else updated.ExceptWith(resolvedIds);

        // Idempotent: only PATCH when the tag set actually changes.
        if (!updated.SetEquals(current))
        {
            var request = new DocumentUpdateRequest { Tags = updated.OrderBy(t => t).ToList() };
            var result = await client.UpdateDocumentWithResultAsync(id, request).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var error = result.Error!;
                return JsonSerializer.Serialize(McpErrorResponse.Create(
                    error.StatusCode == System.Net.HttpStatusCode.NotFound ? ErrorCodes.NotFound : ErrorCodes.UpstreamError,
                    $"Failed to update document {id}: {error.Message}",
                    new { status_code = (int)error.StatusCode, response_body = error.ResponseBody },
                    new McpMeta { PaperlessBaseUrl = client.BaseUrl }));
            }

            // Trust the server's echoed tag set for the response.
            updated = new HashSet<int>(result.Value!.Tags);
        }

        var nameMap = await LoadTagNameMap(client).ConfigureAwait(false);
        var tags = updated
            .OrderBy(tagId => tagId)
            .Select(tagId => new { id = tagId, name = nameMap.TryGetValue(tagId, out var n) ? n : null })
            .ToList();

        var response = McpResponse<object>.Success(
            new { document_id = id, tags },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl });
        return JsonSerializer.Serialize(response);
    }

    /// <summary>
    /// Pages through all tags and builds an id → name map for echoing a document's resulting tag set.
    /// </summary>
    private static async Task<Dictionary<int, string>> LoadTagNameMap(PaperlessClient client)
    {
        var map = new Dictionary<int, string>();
        var page = 1;
        while (true)
        {
            var result = await client.GetTagsAsync(page).ConfigureAwait(false);
            if (result.Results.Count == 0) break;
            foreach (var tag in result.Results) map[tag.Id] = tag.Name;
            if (string.IsNullOrEmpty(result.Next)) break;
            page++;
        }
        return map;
    }

    [McpServerTool(Name = "paperless_documents_delete")]
    [Description("Delete a document. Requires explicit confirmation.")]
    public static async Task<string> Delete(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("Must be true to confirm deletion")] bool confirm = false)
    {
        if (!confirm)
        {
            // Get document info for dry run
            var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

            if (document == null)
            {
                var notFoundResponse = McpErrorResponse.Create(
                    ErrorCodes.NotFound,
                    $"Document with ID {id} not found",
                    meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
                );
                return JsonSerializer.Serialize(notFoundResponse);
            }

            var dryRunResponse = McpErrorResponse.Create(
                ErrorCodes.ConfirmationRequired,
                "Deletion requires confirm=true. This is a dry run showing what would be deleted.",
                new
                {
                    document_id = id,
                    title = document.Title,
                    original_file_name = document.OriginalFileName,
                    created = document.Created
                },
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(dryRunResponse);
        }

        var success = await client.DeleteDocumentAsync(id).ConfigureAwait(false);

        if (!success)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.UpstreamError,
                $"Failed to delete document with ID {id}",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var response = McpResponse<object>.Success(
            new { deleted = true, document_id = id },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_bulk_update")]
    [Description("Perform bulk operations on multiple documents. Supports dry run mode.")]
    public static async Task<string> BulkUpdate(
        PaperlessClient client,
        [Description("Document IDs (comma-separated)")] string documentIds,
        [Description("Operation: add_tag, remove_tag, set_correspondent, set_document_type, set_storage_path, delete, reprocess")] string operation,
        [Description("Parameter value (e.g., tag ID, correspondent ID)")] int? value = null,
        [Description("Dry run mode - shows what would change without applying")] bool dryRun = true,
        [Description("Must be true to execute the operation")] bool confirm = false)
    {
        var ids = ParseIntArray(documentIds);

        if (ids == null || ids.Length == 0)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.Validation,
                "No valid document IDs provided",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var validOperations = new[] { "add_tag", "remove_tag", "set_correspondent", "set_document_type", "set_storage_path", "delete", "reprocess" };
        if (!validOperations.Contains(operation))
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.Validation,
                $"Invalid operation. Valid operations: {string.Join(", ", validOperations)}",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        if (dryRun || !confirm)
        {
            var dryRunResult = new BulkOperationResult
            {
                AffectedIds = ids,
                Warnings = new List<string>
                {
                    dryRun ? "This is a dry run. Set dry_run=false and confirm=true to execute." : "Set confirm=true to execute the operation."
                },
                Executed = false
            };

            var dryRunResponse = McpResponse<BulkOperationResult>.Success(
                dryRunResult,
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(dryRunResponse);
        }

        object? parameters = operation switch
        {
            "add_tag" or "remove_tag" => new { tag = value },
            "set_correspondent" => new { correspondent = value },
            "set_document_type" => new { document_type = value },
            "set_storage_path" => new { storage_path = value },
            _ => null
        };

        var success = await client.BulkEditDocumentsAsync(ids, operation, parameters).ConfigureAwait(false);

        if (!success)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.UpstreamError,
                "Bulk operation failed",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var result = new BulkOperationResult
        {
            AffectedIds = ids,
            Executed = true
        };

        var response = McpResponse<BulkOperationResult>.Success(
            result,
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

    [McpServerTool(Name = "paperless_documents_reprocess")]
    [Description("Reprocess a document's OCR and content extraction.")]
    public static async Task<string> Reprocess(
        PaperlessClient client,
        [Description("Document ID")] int id,
        [Description("Must be true to confirm reprocessing")] bool confirm = false)
    {
        if (!confirm)
        {
            var document = await client.GetDocumentAsync(id).ConfigureAwait(false);

            if (document == null)
            {
                var notFoundResponse = McpErrorResponse.Create(
                    ErrorCodes.NotFound,
                    $"Document with ID {id} not found",
                    meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
                );
                return JsonSerializer.Serialize(notFoundResponse);
            }

            var dryRunResponse = McpErrorResponse.Create(
                ErrorCodes.ConfirmationRequired,
                "Reprocessing requires confirm=true. This will re-run OCR on the document.",
                new { document_id = id, title = document.Title },
                new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(dryRunResponse);
        }

        var success = await client.BulkEditDocumentsAsync(new[] { id }, "reprocess").ConfigureAwait(false);

        if (!success)
        {
            var errorResponse = McpErrorResponse.Create(
                ErrorCodes.UpstreamError,
                $"Failed to reprocess document with ID {id}",
                meta: new McpMeta { PaperlessBaseUrl = client.BaseUrl }
            );
            return JsonSerializer.Serialize(errorResponse);
        }

        var response = McpResponse<object>.Success(
            new { document_id = id, status = "queued", message = "Document queued for reprocessing" },
            new McpMeta { PaperlessBaseUrl = client.BaseUrl }
        );
        return JsonSerializer.Serialize(response);
    }

}
