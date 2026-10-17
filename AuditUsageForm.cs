using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using IOrganizationService = Microsoft.Xrm.Sdk.IOrganizationService;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class AuditUsageForm : Form
    {
        private readonly AuditStorageSnapshot _snapshot;
        private readonly IOrganizationService _service;
        private readonly SmoothDataGridView _grid = new SmoothDataGridView();
        private readonly TextBox _searchBox = new TextBox();
        private readonly CheckBox _withUsageOnlyCheckBox = new CheckBox();
        private readonly CheckBox _auditedOnlyCheckBox = new CheckBox();
        private readonly Label _summaryLabel = new Label();
        private readonly ComboBox _activityRangeBox = new ComboBox();
        private readonly Button _fieldActivityButton = new Button();
        private readonly Button _deleteHistoryButton = new Button();
        private readonly Button _refreshButton = new Button();
        private readonly Button _closeButton = new Button();
        private string _summaryText;
        private string _sortProperty = "SizeInBytes";
        private SortOrder _sortOrder = SortOrder.Descending;

        public AuditUsageForm(AuditStorageSnapshot snapshot, IOrganizationService service)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _service = service ?? throw new ArgumentNullException(nameof(service));

            Text = "Audit storage usage by table";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 680);
            MinimumSize = new Size(900, 480);
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            Controls.Add(CreateGrid());
            Controls.Add(CreateFilters());
            Controls.Add(CreateSummary());
            Controls.Add(CreateFooter());

            _searchBox.TextChanged += (_, __) => PopulateGrid();
            _withUsageOnlyCheckBox.CheckedChanged += (_, __) => PopulateGrid();
            _auditedOnlyCheckBox.CheckedChanged += (_, __) => PopulateGrid();
            _grid.SelectionChanged += (_, __) => UpdateActionState();
            _grid.CellDoubleClick += (_, eventArgs) =>
            {
                if (eventArgs.RowIndex >= 0)
                    ShowFieldActivity();
            };

            PopulateGrid();
        }

        private Control CreateSummary()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(240, 245, 249),
                Padding = new Padding(12, 8, 12, 6)
            };

            _summaryLabel.Dock = DockStyle.Fill;
            _summaryLabel.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            _summaryText =
                $"{_snapshot.OrganizationName}  |  Total audit storage: " +
                $"{FormatBytes(_snapshot.TotalSizeInBytes)}  |  " +
                $"{_snapshot.Tables.Count(table => table.SizeInBytes > 0)} table(s) with usage  |  " +
                $"Retrieved {_snapshot.RetrievedAtUtc.ToLocalTime():g}";
            _summaryLabel.Text = _summaryText;
            panel.Controls.Add(_summaryLabel);
            return panel;
        }

        private Control CreateFilters()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 43,
                Padding = new Padding(8, 7, 8, 5),
                ColumnCount = 8,
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var searchLabel = new Label
            {
                Text = "Table search:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            _searchBox.Dock = DockStyle.Fill;

            _withUsageOnlyCheckBox.Text = "With usage only";
            _withUsageOnlyCheckBox.AutoSize = true;
            _withUsageOnlyCheckBox.Checked = true;
            _withUsageOnlyCheckBox.Anchor = AnchorStyles.Left;

            _auditedOnlyCheckBox.Text = "Currently audited only";
            _auditedOnlyCheckBox.AutoSize = true;
            _auditedOnlyCheckBox.Anchor = AnchorStyles.Left;

            var activityLabel = new Label
            {
                Text = "Field activity:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            _activityRangeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _activityRangeBox.Items.AddRange(new object[]
            {
                "Last 7 days",
                "Last 30 days",
                "Last 90 days",
                "Last 365 days"
            });
            _activityRangeBox.SelectedIndex = 1;
            _activityRangeBox.Width = 105;
            _activityRangeBox.Anchor = AnchorStyles.Left;

            var note = new Label
            {
                Text = "Usage view is read-only",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Anchor = AnchorStyles.Left
            };

            panel.Controls.Add(searchLabel, 0, 0);
            panel.Controls.Add(_searchBox, 1, 0);
            panel.Controls.Add(_withUsageOnlyCheckBox, 2, 0);
            panel.Controls.Add(_auditedOnlyCheckBox, 3, 0);
            panel.Controls.Add(activityLabel, 5, 0);
            panel.Controls.Add(_activityRangeBox, 6, 0);
            panel.Controls.Add(note, 7, 0);
            return panel;
        }

        private Control CreateGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.RowHeadersVisible = false;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _grid.RowTemplate.Height = 26;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;

            _grid.Columns.Add(CreateColumn("Rank", "#", 48));
            _grid.Columns.Add(CreateFillColumn("LogicalName", "Logical name", 28, 160));
            _grid.Columns.Add(CreateFillColumn("DisplayName", "Display name", 28, 160));
            _grid.Columns.Add(CreateColumn("AuditingStatus", "Auditing", 92));
            _grid.Columns.Add(CreateColumn("ObjectTypeCode", "Type code", 80));
            _grid.Columns.Add(CreateFillColumn("SizeInBytes", "Audit storage", 18, 120));
            _grid.Columns.Add(CreateColumn("Percentage", "% of total", 90));

            _grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns[6].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return _grid;
        }

        private Control CreateFooter()
        {
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                WrapContents = false
            };

            _closeButton.Text = "Close";
            _closeButton.AutoSize = true;
            _closeButton.DialogResult = DialogResult.Cancel;

            _refreshButton.Text = "Refresh usage";
            _refreshButton.AutoSize = true;
            _refreshButton.DialogResult = DialogResult.Retry;
            _fieldActivityButton.Text = "Field activity...";
            _fieldActivityButton.AutoSize = true;
            _fieldActivityButton.Enabled = false;
            _fieldActivityButton.Click += (_, __) => ShowFieldActivity();

            _deleteHistoryButton.Text = "Delete history...";
            _deleteHistoryButton.AutoSize = true;
            _deleteHistoryButton.Enabled = false;
            _deleteHistoryButton.Click += (_, __) => DeleteHistory();

            ToolbarIcons.Configure(_closeButton, ToolbarIcons.Close);
            ToolbarIcons.Configure(_refreshButton, ToolbarIcons.Refresh);
            ToolbarIcons.Configure(_fieldActivityButton, ToolbarIcons.Usage);
            ToolbarIcons.Configure(_deleteHistoryButton, ToolbarIcons.Delete);

            footer.Controls.Add(_closeButton);
            footer.Controls.Add(_refreshButton);
            footer.Controls.Add(_fieldActivityButton);
            footer.Controls.Add(_deleteHistoryButton);
            AcceptButton = _refreshButton;
            CancelButton = _closeButton;
            return footer;
        }

        private static DataGridViewTextBoxColumn CreateColumn(string name, string header, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                ReadOnly = true
            };
        }

        private static DataGridViewTextBoxColumn CreateFillColumn(
            string name,
            string header,
            float fillWeight,
            int minimumWidth)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = fillWeight,
                MinimumWidth = minimumWidth,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                ReadOnly = true
            };
        }

        private void PopulateGrid()
        {
            var search = _searchBox.Text.Trim();
            IEnumerable<AuditStorageItem> tables = _snapshot.Tables;

            if (_withUsageOnlyCheckBox.Checked)
                tables = tables.Where(table => table.SizeInBytes > 0);
            if (_auditedOnlyCheckBox.Checked)
                tables = tables.Where(table => table.AuditingEnabled == true);
            if (!string.IsNullOrWhiteSpace(search))
            {
                tables = tables.Where(table =>
                    Contains(table.LogicalName, search) || Contains(table.DisplayName, search));
            }

            tables = SortTables(tables);
            _grid.Rows.Clear();
            foreach (var table in tables)
            {
                var rowIndex = _grid.Rows.Add(
                    table.Rank,
                    table.LogicalName,
                    table.DisplayName,
                    table.AuditingStatus,
                    table.ObjectTypeCode?.ToString() ?? string.Empty,
                    FormatBytes(table.SizeInBytes),
                    $"{table.Percentage:N2}%");
                var row = _grid.Rows[rowIndex];
                row.Tag = table;
                if (table.AuditingEnabled == false)
                    row.DefaultCellStyle.ForeColor = Color.DimGray;
            }

            UpdateSortGlyph();
            _grid.ClearSelection();
            UpdateActionState();
        }

        private IEnumerable<AuditStorageItem> SortTables(IEnumerable<AuditStorageItem> tables)
        {
            var descending = _sortOrder == SortOrder.Descending;
            switch (_sortProperty)
            {
                case "Rank":
                    return descending ? tables.OrderByDescending(table => table.Rank) : tables.OrderBy(table => table.Rank);
                case "LogicalName":
                    return OrderText(tables, table => table.LogicalName, descending);
                case "DisplayName":
                    return OrderText(tables, table => table.DisplayName, descending);
                case "AuditingStatus":
                    return OrderText(tables, table => table.AuditingStatus, descending)
                        .ThenBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase);
                case "ObjectTypeCode":
                    return descending
                        ? tables.OrderByDescending(table => table.ObjectTypeCode)
                        : tables.OrderBy(table => table.ObjectTypeCode);
                case "Percentage":
                    return descending
                        ? tables.OrderByDescending(table => table.Percentage)
                        : tables.OrderBy(table => table.Percentage);
                default:
                    return descending
                        ? tables.OrderByDescending(table => table.SizeInBytes)
                        : tables.OrderBy(table => table.SizeInBytes);
            }
        }

        private static IOrderedEnumerable<AuditStorageItem> OrderText(
            IEnumerable<AuditStorageItem> tables,
            Func<AuditStorageItem, string> keySelector,
            bool descending)
        {
            return descending
                ? tables.OrderByDescending(keySelector, StringComparer.OrdinalIgnoreCase)
                : tables.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        private AuditStorageItem SelectedTable =>
            _grid.SelectedRows.Count == 1
                ? _grid.SelectedRows[0].Tag as AuditStorageItem
                : _grid.CurrentRow?.Tag as AuditStorageItem;

        private int SelectedActivityDays
        {
            get
            {
                var ranges = new[] { 7, 30, 90, 365 };
                var index = _activityRangeBox.SelectedIndex;
                return index >= 0 && index < ranges.Length ? ranges[index] : 30;
            }
        }

        private void UpdateActionState()
        {
            var selected = SelectedTable;
            _fieldActivityButton.Enabled = selected != null;
            _deleteHistoryButton.Enabled = selected != null && selected.SizeInBytes > 0;
        }

        private async void ShowFieldActivity()
        {
            var table = SelectedTable;
            if (table == null)
                return;

            SetActivityBusy(true, $"Loading {SelectedActivityDays}-day field activity for {table.LogicalName}...");
            try
            {
                IProgress<string> progress = new Progress<string>(message => _summaryLabel.Text = message);
                var days = SelectedActivityDays;
                var snapshot = await Task.Run(() => DataverseAuditService.LoadAuditFieldActivity(
                    _service,
                    table,
                    days,
                    (_, message) => progress.Report(message)));

                _summaryLabel.Text = _summaryText;
                using (var dialog = new AuditFieldActivityForm(snapshot))
                    dialog.ShowDialog(this);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    exception.GetBaseException().Message,
                    "Load field audit activity",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _summaryLabel.Text = _summaryText;
                SetActivityBusy(false, null);
            }
        }

        private async void DeleteHistory()
        {
            var table = SelectedTable;
            if (table == null)
                return;

            AuditCleanupRequest cleanup;
            using (var dialog = new AuditCleanupForm(table))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                cleanup = dialog.CleanupRequest;
            }

            SetActivityBusy(true, $"Submitting audit cleanup for {table.LogicalName}...");
            try
            {
                var result = await Task.Run(() => DataverseAuditService.StartAuditCleanup(_service, cleanup));
                _summaryLabel.Text = _summaryText;
                MessageBox.Show(
                    this,
                    $"The audit cleanup job was submitted successfully.\n\n" +
                    $"Job: {result.JobName}\n" +
                    $"Job ID: {result.JobId}\n\n" +
                    "Dataverse will run it asynchronously. Monitor Bulk deletion system jobs, " +
                    "then refresh audit usage after it completes.",
                    "Audit cleanup submitted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    exception.GetBaseException().Message,
                    "Submit audit cleanup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _summaryLabel.Text = _summaryText;
                SetActivityBusy(false, null);
            }
        }

        private void SetActivityBusy(bool busy, string message)
        {
            UseWaitCursor = busy;
            _grid.Enabled = !busy;
            _searchBox.Enabled = !busy;
            _withUsageOnlyCheckBox.Enabled = !busy;
            _auditedOnlyCheckBox.Enabled = !busy;
            _activityRangeBox.Enabled = !busy;
            _refreshButton.Enabled = !busy;
            _closeButton.Enabled = !busy;
            ControlBox = !busy;
            if (busy)
            {
                _fieldActivityButton.Enabled = false;
                _deleteHistoryButton.Enabled = false;
                if (!string.IsNullOrWhiteSpace(message))
                    _summaryLabel.Text = message;
            }
            else
            {
                UpdateActionState();
            }
        }

        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs eventArgs)
        {
            if (eventArgs.ColumnIndex < 0)
                return;

            var selectedProperty = _grid.Columns[eventArgs.ColumnIndex].Name;
            if (string.Equals(_sortProperty, selectedProperty, StringComparison.Ordinal))
            {
                _sortOrder = _sortOrder == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                _sortProperty = selectedProperty;
                _sortOrder = SortOrder.Ascending;
            }

            PopulateGrid();
        }

        private void UpdateSortGlyph()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.HeaderCell.SortGlyphDirection =
                    string.Equals(column.Name, _sortProperty, StringComparison.Ordinal)
                        ? _sortOrder
                        : SortOrder.None;
            }
        }

        private static bool Contains(string value, string search)
        {
            return value?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var value = Math.Max(0, bytes);
            var unitIndex = 0;
            var displayValue = (double)value;
            while (displayValue >= 1024 && unitIndex < units.Length - 1)
            {
                displayValue /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{value:N0} {units[unitIndex]}"
                : $"{displayValue:N2} {units[unitIndex]}";
        }
    }
}
