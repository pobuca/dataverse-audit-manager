using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;

namespace DataverseAuditManager.XrmToolBox
{
    internal static class DataverseAuditService
    {
        public static AuditSnapshot Load(IOrganizationService service)
        {
            if (service == null)
                throw new InvalidOperationException("Connect XrmToolBox to a Dataverse environment first.");

            var snapshot = LoadOrganizationSettings(service);
            snapshot.Tables = LoadTableMetadata(service);
            return snapshot;
        }

        public static ApplyResult Apply(
            IOrganizationService service,
            IEnumerable<AuditTableItem> tables,
            Action<int, string> reportProgress = null)
        {
            if (service == null)
                throw new InvalidOperationException("The Dataverse connection is not available.");

            reportProgress?.Invoke(5, "Validating staged audit changes...");
            var changedTables = tables.Where(table => table.IsChanged).ToList();
            var changedColumns = tables
                .SelectMany(table => table.Columns
                    .Where(column => column.IsChanged)
                    .Select(column => new { Table = table, Column = column }))
                .ToList();

            foreach (var table in changedTables.Where(table => !table.CanBeChanged))
                throw new InvalidOperationException($"Auditing cannot be changed for table '{table.LogicalName}'.");

            foreach (var change in changedColumns.Where(change => !change.Column.CanBeChanged))
            {
                throw new InvalidOperationException(
                    $"Auditing cannot be changed for column '{change.Table.LogicalName}.{change.Column.LogicalName}'.");
            }

            reportProgress?.Invoke(
                20,
                $"Retrieving current metadata for {changedTables.Count} table setting(s) and " +
                $"{changedColumns.Count} field setting(s)...");

            var retrieveRequests = new List<OrganizationRequest>();
            var retrieveDescriptions = new List<string>();

            foreach (var table in changedTables)
            {
                retrieveRequests.Add(new RetrieveEntityRequest
                {
                    LogicalName = table.LogicalName,
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                });
                retrieveDescriptions.Add($"retrieve table '{table.LogicalName}'");
            }

            foreach (var change in changedColumns)
            {
                retrieveRequests.Add(new RetrieveAttributeRequest
                {
                    EntityLogicalName = change.Table.LogicalName,
                    LogicalName = change.Column.LogicalName,
                    RetrieveAsIfPublished = true
                });
                retrieveDescriptions.Add(
                    $"retrieve field '{change.Table.LogicalName}.{change.Column.LogicalName}'");
            }

            var retrieveResponses = ExecuteBatches(service, retrieveRequests, retrieveDescriptions);
            reportProgress?.Invoke(40, "Comparing staged changes with current Dataverse settings...");

            var updateRequests = new List<OrganizationRequest>();
            var updateDescriptions = new List<string>();
            var affectedTables = new HashSet<string>(
                changedTables.Select(table => table.LogicalName)
                    .Concat(changedColumns.Select(change => change.Table.LogicalName)),
                StringComparer.OrdinalIgnoreCase);
            var result = new ApplyResult();
            var responseIndex = 0;

            foreach (var table in changedTables)
            {
                var metadata = ((RetrieveEntityResponse)retrieveResponses[responseIndex++]).EntityMetadata;
                if (metadata.IsAuditEnabled == null || !metadata.IsAuditEnabled.CanBeChanged)
                    throw new InvalidOperationException($"Auditing cannot be changed for table '{table.LogicalName}'.");

                if (metadata.IsAuditEnabled.Value == table.Enabled)
                {
                    result.AlreadyCurrentTables++;
                    continue;
                }

                metadata.IsAuditEnabled.Value = table.Enabled;
                updateRequests.Add(new UpdateEntityRequest { Entity = metadata });
                updateDescriptions.Add($"update table '{table.LogicalName}'");
                result.UpdatedTables++;
            }

            foreach (var change in changedColumns)
            {
                var metadata = ((RetrieveAttributeResponse)retrieveResponses[responseIndex++]).AttributeMetadata;
                if (metadata.IsAuditEnabled == null || !metadata.IsAuditEnabled.CanBeChanged)
                {
                    throw new InvalidOperationException(
                        $"Auditing cannot be changed for column '{change.Table.LogicalName}.{change.Column.LogicalName}'.");
                }

                if (metadata.IsAuditEnabled.Value == change.Column.Enabled)
                {
                    result.AlreadyCurrentColumns++;
                    continue;
                }

                metadata.IsAuditEnabled.Value = change.Column.Enabled;
                updateRequests.Add(new UpdateAttributeRequest
                {
                    EntityName = change.Table.LogicalName,
                    Attribute = metadata,
                    MergeLabels = true
                });
                updateDescriptions.Add(
                    $"update field '{change.Table.LogicalName}.{change.Column.LogicalName}'");
                result.UpdatedColumns++;
            }

            reportProgress?.Invoke(
                55,
                updateRequests.Count == 0
                    ? "Audit settings are already current; preparing to publish customizations..."
                    : $"Updating {result.UpdatedTables} table setting(s) and " +
                      $"{result.UpdatedColumns} field setting(s)...");
            ExecuteBatches(service, updateRequests, updateDescriptions);

            if (affectedTables.Count > 0)
            {
                reportProgress?.Invoke(
                    80,
                    $"Publishing customizations for {affectedTables.Count} affected table(s)...");

                var entitiesXml = string.Join(
                    string.Empty,
                    affectedTables
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                        .Select(name => $"<entity>{SecurityElement.Escape(name)}</entity>"));

                service.Execute(new PublishXmlRequest
                {
                    ParameterXml = $"<importexportxml><entities>{entitiesXml}</entities></importexportxml>"
                });
                result.PublishedTables = affectedTables.Count;
            }

            reportProgress?.Invoke(92, "Verifying effective audit settings after publishing...");
            try
            {
                var verificationRequests = new List<OrganizationRequest>();
                var verificationDescriptions = new List<string>();

                foreach (var table in changedTables)
                {
                    verificationRequests.Add(new RetrieveEntityRequest
                    {
                        LogicalName = table.LogicalName,
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = false
                    });
                    verificationDescriptions.Add($"verify table '{table.LogicalName}'");
                }

                foreach (var change in changedColumns)
                {
                    verificationRequests.Add(new RetrieveAttributeRequest
                    {
                        EntityLogicalName = change.Table.LogicalName,
                        LogicalName = change.Column.LogicalName,
                        RetrieveAsIfPublished = false
                    });
                    verificationDescriptions.Add(
                        $"verify field '{change.Table.LogicalName}.{change.Column.LogicalName}'");
                }

                var verificationResponses = ExecuteBatches(
                    service,
                    verificationRequests,
                    verificationDescriptions);
                var verificationIndex = 0;

                foreach (var table in changedTables)
                {
                    var metadata = ((RetrieveEntityResponse)verificationResponses[verificationIndex++]).EntityMetadata;
                    var actualEnabled = metadata.IsAuditEnabled?.Value == true;
                    if (actualEnabled != table.Enabled)
                    {
                        result.VerificationIssues.Add(new ApplyVerificationIssue
                        {
                            Table = table,
                            RequestedEnabled = table.Enabled,
                            ActualEnabled = actualEnabled
                        });
                    }
                }

                foreach (var change in changedColumns)
                {
                    var metadata = ((RetrieveAttributeResponse)verificationResponses[verificationIndex++]).AttributeMetadata;
                    var actualEnabled = metadata.IsAuditEnabled?.Value == true;
                    if (actualEnabled != change.Column.Enabled)
                    {
                        result.VerificationIssues.Add(new ApplyVerificationIssue
                        {
                            Table = change.Table,
                            Column = change.Column,
                            RequestedEnabled = change.Column.Enabled,
                            ActualEnabled = actualEnabled
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "The changes were submitted and the affected tables were published, but the effective audit settings could not be verified. Refresh the grid before retrying.",
                    exception);
            }

            reportProgress?.Invoke(
                100,
                result.VerificationIssues.Count == 0
                    ? "Audit settings applied, published, and verified."
                    : $"Completed with {result.VerificationIssues.Count} verification warning(s).");
            return result;
        }

        private static List<OrganizationResponse> ExecuteBatches(
            IOrganizationService service,
            IReadOnlyList<OrganizationRequest> requests,
            IReadOnlyList<string> descriptions)
        {
            const int batchSize = 100;
            var results = new OrganizationResponse[requests.Count];

            for (var offset = 0; offset < requests.Count; offset += batchSize)
            {
                var count = Math.Min(batchSize, requests.Count - offset);
                var batchRequests = new OrganizationRequestCollection();
                for (var index = 0; index < count; index++)
                    batchRequests.Add(requests[offset + index]);

                var batchResponse = (ExecuteMultipleResponse)service.Execute(new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests = batchRequests
                });

                foreach (var item in batchResponse.Responses)
                {
                    var globalIndex = offset + item.RequestIndex;
                    if (item.Fault != null)
                    {
                        throw new InvalidOperationException(
                            $"Dataverse failed to {descriptions[globalIndex]}: {item.Fault.Message}");
                    }

                    results[globalIndex] = item.Response;
                }

                for (var index = 0; index < count; index++)
                {
                    var globalIndex = offset + index;
                    if (results[globalIndex] == null)
                    {
                        throw new InvalidOperationException(
                            $"Dataverse did not return a response for '{descriptions[globalIndex]}'.");
                    }
                }
            }

            return results.ToList();
        }

        private static AuditSnapshot LoadOrganizationSettings(IOrganizationService service)
        {
            var query = new QueryExpression("organization")
            {
                ColumnSet = new ColumnSet(
                    "name",
                    "isauditenabled",
                    "auditretentionperiodv2",
                    "isuseraccessauditenabled"),
                TopCount = 1
            };

            var organization = service.RetrieveMultiple(query).Entities.SingleOrDefault()
                ?? throw new InvalidOperationException("The organization settings record was not returned.");

            return new AuditSnapshot
            {
                OrganizationName = organization.GetAttributeValue<string>("name") ?? "Dataverse",
                EnvironmentAuditingEnabled = organization.GetAttributeValue<bool>("isauditenabled"),
                RetentionPeriodDays = GetNullableInt32(organization, "auditretentionperiodv2"),
                UserAccessAuditingEnabled = organization.GetAttributeValue<bool>("isuseraccessauditenabled")
            };
        }

        public static List<AuditColumnItem> LoadColumns(IOrganizationService service, string tableLogicalName)
        {
            if (service == null)
                throw new InvalidOperationException("The Dataverse connection is not available.");

            var metadata = RetrieveAuditMetadata(service, tableLogicalName, includeColumns: true)
                .SingleOrDefault(table =>
                    string.Equals(table.LogicalName, tableLogicalName, StringComparison.OrdinalIgnoreCase));

            if (metadata == null)
                throw new InvalidOperationException($"Table metadata was not returned for '{tableLogicalName}'.");

            return MapColumns(metadata);
        }

        public static Dictionary<string, List<AuditColumnItem>> LoadAllColumns(IOrganizationService service)
        {
            if (service == null)
                throw new InvalidOperationException("The Dataverse connection is not available.");

            return RetrieveAuditMetadata(service, null, includeColumns: true)
                .Where(table => !string.IsNullOrWhiteSpace(table.LogicalName))
                .ToDictionary(
                    table => table.LogicalName,
                    MapColumns,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static List<AuditTableItem> LoadTableMetadata(IOrganizationService service)
        {
            return RetrieveAuditMetadata(service, null, includeColumns: false)
                .Where(table => !string.IsNullOrWhiteSpace(table.LogicalName))
                .Select(table =>
                {
                    var tableEnabled = table.IsAuditEnabled?.Value == true;
                    return new AuditTableItem
                    {
                        LogicalName = table.LogicalName,
                        DisplayName = GetLabel(table.DisplayName, table.LogicalName),
                        OriginalEnabled = tableEnabled,
                        Enabled = tableEnabled,
                        CanBeChanged = table.IsAuditEnabled?.CanBeChanged == true,
                        ColumnsLoaded = false
                    };
                })
                .OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<EntityMetadata> RetrieveAuditMetadata(
            IOrganizationService service,
            string tableLogicalName,
            bool includeColumns)
        {
            var entityProperties = new MetadataPropertiesExpression { AllProperties = false };
            entityProperties.PropertyNames.AddRange(
                "LogicalName",
                "DisplayName",
                "IsAuditEnabled",
                "IsPrivate");
            if (includeColumns)
                entityProperties.PropertyNames.Add("Attributes");

            var entityFilter = new MetadataFilterExpression(LogicalOperator.And);
            entityFilter.Conditions.Add(new MetadataConditionExpression(
                "IsPrivate",
                MetadataConditionOperator.Equals,
                false));
            if (!string.IsNullOrWhiteSpace(tableLogicalName))
            {
                entityFilter.Conditions.Add(new MetadataConditionExpression(
                    "LogicalName",
                    MetadataConditionOperator.Equals,
                    tableLogicalName));
            }

            var query = new EntityQueryExpression
            {
                Criteria = entityFilter,
                Properties = entityProperties
            };

            if (includeColumns)
            {
                var attributeProperties = new MetadataPropertiesExpression { AllProperties = false };
                attributeProperties.PropertyNames.AddRange(
                    "LogicalName",
                    "DisplayName",
                    "AttributeType",
                    "IsAuditEnabled");
                query.AttributeQuery = new AttributeQueryExpression
                {
                    Properties = attributeProperties
                };
            }

            var response = (RetrieveMetadataChangesResponse)service.Execute(
                new RetrieveMetadataChangesRequest { Query = query });
            return response.EntityMetadata;
        }

        private static List<AuditColumnItem> MapColumns(EntityMetadata table)
        {
            return (table.Attributes ?? Array.Empty<AttributeMetadata>())
                .Where(column =>
                    !string.IsNullOrWhiteSpace(column.LogicalName) &&
                    column.IsAuditEnabled != null)
                .Select(column => new AuditColumnItem
                {
                    LogicalName = column.LogicalName,
                    DisplayName = GetLabel(column.DisplayName, column.LogicalName),
                    Type = column.AttributeType?.ToString() ?? "Unknown",
                    OriginalEnabled = column.IsAuditEnabled.Value,
                    Enabled = column.IsAuditEnabled.Value,
                    CanBeChanged = column.IsAuditEnabled.CanBeChanged
                })
                .OrderBy(column => column.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int? GetNullableInt32(Entity entity, string attributeName)
        {
            return entity.Attributes.TryGetValue(attributeName, out var value) && value is int number
                ? number
                : (int?)null;
        }

        private static string GetLabel(Label label, string fallback)
        {
            return label?.UserLocalizedLabel?.Label
                ?? label?.LocalizedLabels?.FirstOrDefault()?.Label
                ?? fallback;
        }
    }
}
