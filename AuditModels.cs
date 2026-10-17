using System;
using System.Collections.Generic;
using System.Linq;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class AuditSnapshot
    {
        public string OrganizationName { get; set; }
        public bool EnvironmentAuditingEnabled { get; set; }
        public int? RetentionPeriodDays { get; set; }
        public bool UserAccessAuditingEnabled { get; set; }
        public List<AuditTableItem> Tables { get; set; } = new List<AuditTableItem>();
    }

    internal sealed class AuditTableItem
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool OriginalEnabled { get; set; }
        public bool Enabled { get; set; }
        public bool CanBeChanged { get; set; }
        public bool ColumnsLoaded { get; set; }
        public bool ColumnsLoading { get; set; }
        public List<AuditColumnItem> Columns { get; set; } = new List<AuditColumnItem>();

        public bool IsChanged => OriginalEnabled != Enabled;
        public string Pending => IsChanged || Columns.Any(column => column.IsChanged) ? "●" : "";
        public string Changeable => CanBeChanged ? "Yes" : "No";
        public string AuditedColumns => ColumnsLoaded
            ? $"{Columns.Count(column => column.Enabled)} / {Columns.Count}"
            : ColumnsLoading ? "Loading..." : "On selection";
        public int PendingChangeCount => (IsChanged ? 1 : 0) + Columns.Count(column => column.IsChanged);

        public void Revert()
        {
            Enabled = OriginalEnabled;
            foreach (var column in Columns)
                column.Enabled = column.OriginalEnabled;
        }

        public void AcceptChanges()
        {
            OriginalEnabled = Enabled;
            foreach (var column in Columns)
                column.OriginalEnabled = column.Enabled;
        }
    }

    internal sealed class AuditColumnItem
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public bool OriginalEnabled { get; set; }
        public bool Enabled { get; set; }
        public bool CanBeChanged { get; set; }
        public bool IsVirtual { get; set; }

        public bool IsChanged => OriginalEnabled != Enabled;
        public string Pending => IsChanged ? "●" : "";
        public string Changeable => CanBeChanged
            ? "Yes"
            : IsVirtual ? "No (virtual)" : "No";
    }

    internal sealed class ApplyResult
    {
        public int UpdatedTables { get; set; }
        public int UpdatedColumns { get; set; }
        public int AlreadyCurrentTables { get; set; }
        public int AlreadyCurrentColumns { get; set; }
        public int PublishedTables { get; set; }
        public List<ApplyVerificationIssue> VerificationIssues { get; set; } =
            new List<ApplyVerificationIssue>();
    }

    internal sealed class ApplyVerificationIssue
    {
        public AuditTableItem Table { get; set; }
        public AuditColumnItem Column { get; set; }
        public bool RequestedEnabled { get; set; }
        public bool ActualEnabled { get; set; }

        public string LogicalName => Column == null
            ? Table.LogicalName
            : $"{Table.LogicalName}.{Column.LogicalName}";
    }

    internal sealed class AuditStorageSnapshot
    {
        public string OrganizationName { get; set; }
        public DateTimeOffset RetrievedAtUtc { get; set; }
        public List<AuditStorageItem> Tables { get; set; } = new List<AuditStorageItem>();
        public long TotalSizeInBytes => Tables.Sum(table => table.SizeInBytes);
    }

    internal sealed class AuditStorageItem
    {
        public int Rank { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public int? ObjectTypeCode { get; set; }
        public bool? AuditingEnabled { get; set; }
        public long SizeInBytes { get; set; }
        public double Percentage { get; set; }

        public string AuditingStatus => AuditingEnabled.HasValue
            ? AuditingEnabled.Value ? "Enabled" : "Disabled"
            : "Unknown";
    }

    internal sealed class AuditExport
    {
        public DateTimeOffset ExportedAtUtc { get; set; }
        public string OrganizationName { get; set; }
        public bool EnvironmentAuditingEnabled { get; set; }
        public List<AuditExportTable> Tables { get; set; } = new List<AuditExportTable>();
    }

    internal sealed class AuditExportTable
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public bool CanBeChanged { get; set; }
        public List<AuditExportColumn> Columns { get; set; } = new List<AuditExportColumn>();
    }

    internal sealed class AuditExportColumn
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public bool Enabled { get; set; }
        public bool CanBeChanged { get; set; }
    }
}
