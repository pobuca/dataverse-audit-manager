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

        public static ApplyResult Apply(IOrganizationService service, IEnumerable<AuditTableItem> tables)
        {
            if (service == null)
                throw new InvalidOperationException("The Dataverse connection is not available.");

            var changedTables = tables.Where(table => table.IsChanged).ToList();
            var changedColumns = tables
                .SelectMany(table => table.Columns
                    .Where(column => column.IsChanged)
                    .Select(column => new { Table = table, Column = column }))
                .ToList();

            var affectedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new ApplyResult();

            foreach (var table in changedTables)
            {
                if (!table.CanBeChanged)
                    throw new InvalidOperationException($"Auditing cannot be changed for table '{table.LogicalName}'.");

                var retrieve = (RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
                {
                    LogicalName = table.LogicalName,
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                });

                if (retrieve.EntityMetadata.IsAuditEnabled == null ||
                    !retrieve.EntityMetadata.IsAuditEnabled.CanBeChanged)
                {
                    throw new InvalidOperationException($"Auditing cannot be changed for table '{table.LogicalName}'.");
                }

                retrieve.EntityMetadata.IsAuditEnabled.Value = table.Enabled;
                service.Execute(new UpdateEntityRequest
                {
                    Entity = retrieve.EntityMetadata
                });

                affectedTables.Add(table.LogicalName);
                result.UpdatedTables++;
            }

            foreach (var change in changedColumns)
            {
                if (!change.Column.CanBeChanged)
                {
                    throw new InvalidOperationException(
                        $"Auditing cannot be changed for column '{change.Table.LogicalName}.{change.Column.LogicalName}'.");
                }

                var retrieve = (RetrieveAttributeResponse)service.Execute(new RetrieveAttributeRequest
                {
                    EntityLogicalName = change.Table.LogicalName,
                    LogicalName = change.Column.LogicalName,
                    RetrieveAsIfPublished = true
                });

                if (retrieve.AttributeMetadata.IsAuditEnabled == null ||
                    !retrieve.AttributeMetadata.IsAuditEnabled.CanBeChanged)
                {
                    throw new InvalidOperationException(
                        $"Auditing cannot be changed for column '{change.Table.LogicalName}.{change.Column.LogicalName}'.");
                }

                retrieve.AttributeMetadata.IsAuditEnabled.Value = change.Column.Enabled;
                service.Execute(new UpdateAttributeRequest
                {
                    EntityName = change.Table.LogicalName,
                    Attribute = retrieve.AttributeMetadata,
                    MergeLabels = true
                });

                affectedTables.Add(change.Table.LogicalName);
                result.UpdatedColumns++;
            }

            if (affectedTables.Count > 0)
            {
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

            return result;
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
