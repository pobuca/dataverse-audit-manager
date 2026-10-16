using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class AuditReviewForm : Form
    {
        private readonly List<AuditTableItem> _changedTables;
        private readonly DataGridView _changesGrid = new DataGridView();
        private readonly ComboBox _scopeFilter = new ComboBox();
        private readonly ComboBox _directionFilter = new ComboBox();

        public AuditReviewForm(IEnumerable<AuditTableItem> tables)
        {
            _changedTables = tables
                .Where(table => table.IsChanged || table.Columns.Any(column => column.IsChanged))
                .ToList();

            Text = "Review pending audit changes";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(920, 580);
            MinimumSize = new Size(700, 420);
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            var summaryPanel = CreateSummaryPanel();
            var warningPanel = CreateWarningPanel();
            var filters = CreateFiltersPanel();
            ConfigureChangesGrid();

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                WrapContents = false
            };

            var closeButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            var applyButton = new Button
            {
                Text = "Apply && Publish",
                AutoSize = true,
                DialogResult = DialogResult.OK,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold)
            };

            footer.Controls.Add(closeButton);
            footer.Controls.Add(applyButton);
            Controls.Add(_changesGrid);
            Controls.Add(filters);
            if (warningPanel != null)
                Controls.Add(warningPanel);
            Controls.Add(summaryPanel);
            Controls.Add(footer);

            AcceptButton = applyButton;
            CancelButton = closeButton;
            PopulateChangesGrid();
        }

        private Control CreateSummaryPanel()
        {
            var tableChanges = _changedTables.Count(table => table.IsChanged);
            var fieldChanges = _changedTables.Sum(table => table.Columns.Count(column => column.IsChanged));
            var total = tableChanges + fieldChanges;
            var enabling = _changedTables.Sum(table => (table.IsChanged && table.Enabled ? 1 : 0) +
                table.Columns.Count(column => column.IsChanged && column.Enabled));
            var disabling = total - enabling;

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(240, 245, 249),
                Padding = new Padding(12, 8, 12, 6)
            };
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = $"{total} pending change(s)  |  {enabling} enabling  |  {disabling} disabling  |  " +
                       $"{tableChanges} table(s)  |  {fieldChanges} field(s)  |  Nothing has been applied yet."
            });
            return panel;
        }

        private Control CreateWarningPanel()
        {
            var warnings = _changedTables
                .Where(table => !table.Enabled && table.Columns.Any(column => column.Enabled))
                .Select(table =>
                    $"{DisplayName(table.DisplayName, table.LogicalName)} ({table.Columns.Count(column => column.Enabled)})")
                .ToList();

            if (warnings.Count == 0)
                return null;

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(255, 244, 206),
                Padding = new Padding(12, 7, 12, 7)
            };
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(102, 60, 0),
                Text = "Warning: enabled field settings will be inactive for disabled tables: " + string.Join(", ", warnings) + "."
            });
            return panel;
        }

        private Control CreateFiltersPanel()
        {
            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Padding = new Padding(8, 6, 8, 4),
                ColumnCount = 5,
                RowCount = 1
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var scopeLabel = new Label { Text = "Show:", AutoSize = true, Anchor = AnchorStyles.Left };
            _scopeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _scopeFilter.Dock = DockStyle.Fill;
            _scopeFilter.Items.AddRange(new object[] { "All items", "Tables only", "Fields only" });
            _scopeFilter.SelectedIndex = 0;

            var directionLabel = new Label { Text = "Action:", AutoSize = true, Anchor = AnchorStyles.Left };
            _directionFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _directionFilter.Dock = DockStyle.Fill;
            _directionFilter.Items.AddRange(new object[] { "All actions", "Enable", "Disable" });
            _directionFilter.SelectedIndex = 0;

            filters.Controls.Add(scopeLabel, 0, 0);
            filters.Controls.Add(_scopeFilter, 1, 0);
            filters.Controls.Add(directionLabel, 2, 0);
            filters.Controls.Add(_directionFilter, 3, 0);

            _scopeFilter.SelectedIndexChanged += (_, __) => PopulateChangesGrid();
            _directionFilter.SelectedIndexChanged += (_, __) => PopulateChangesGrid();
            return filters;
        }

        private void ConfigureChangesGrid()
        {
            _changesGrid.Dock = DockStyle.Fill;
            _changesGrid.ReadOnly = true;
            _changesGrid.AllowUserToAddRows = false;
            _changesGrid.AllowUserToDeleteRows = false;
            _changesGrid.AllowUserToResizeRows = false;
            _changesGrid.AllowUserToOrderColumns = false;
            _changesGrid.AutoGenerateColumns = false;
            _changesGrid.MultiSelect = false;
            _changesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _changesGrid.RowHeadersVisible = false;
            _changesGrid.BackgroundColor = Color.White;
            _changesGrid.BorderStyle = BorderStyle.FixedSingle;
            _changesGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _changesGrid.RowTemplate.Height = 29;
            _changesGrid.DefaultCellStyle.SelectionBackColor = Color.White;
            _changesGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
            _changesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _changesGrid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
            _changesGrid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.Black;

            _changesGrid.Columns.Add(CreateTextColumn("Table", 220, DataGridViewAutoSizeColumnMode.Fill));
            _changesGrid.Columns.Add(CreateTextColumn("Setting", 240, DataGridViewAutoSizeColumnMode.Fill));
            _changesGrid.Columns.Add(CreateTextColumn("Logical name", 190));
            _changesGrid.Columns.Add(CreateTextColumn("Scope", 70));
            _changesGrid.Columns.Add(CreateTextColumn("Action", 95));
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string header,
            int width,
            DataGridViewAutoSizeColumnMode autoSizeMode = DataGridViewAutoSizeColumnMode.None)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                AutoSizeMode = autoSizeMode,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private void PopulateChangesGrid()
        {
            _changesGrid.Rows.Clear();

            foreach (var change in BuildFriendlyChanges().Where(MatchesFilters))
            {
                var rowIndex = _changesGrid.Rows.Add(
                    change.TableName,
                    change.SettingName,
                    change.LogicalName,
                    change.Scope,
                    change.Enabled ? "Enable" : "Disable");
                var row = _changesGrid.Rows[rowIndex];
                row.Cells[0].ToolTipText = change.TableLogicalName;
                StyleChangeRow(row, change.Enabled);
            }

            _changesGrid.ClearSelection();
        }

        private IEnumerable<AuditChangeItem> BuildFriendlyChanges()
        {
            foreach (var table in _changedTables)
            {
                var tableName = DisplayName(table.DisplayName, table.LogicalName);
                if (table.IsChanged)
                {
                    yield return new AuditChangeItem
                    {
                        TableName = tableName,
                        TableLogicalName = table.LogicalName,
                        SettingName = "Table auditing",
                        LogicalName = table.LogicalName,
                        Scope = "Table",
                        Enabled = table.Enabled
                    };
                }

                foreach (var column in table.Columns.Where(column => column.IsChanged))
                {
                    yield return new AuditChangeItem
                    {
                        TableName = tableName,
                        TableLogicalName = table.LogicalName,
                        SettingName = DisplayName(column.DisplayName, column.LogicalName),
                        LogicalName = column.LogicalName,
                        Scope = "Field",
                        Enabled = column.Enabled
                    };
                }
            }
        }

        private bool MatchesFilters(AuditChangeItem change)
        {
            if (_scopeFilter.SelectedIndex == 1 && change.Scope != "Table")
                return false;
            if (_scopeFilter.SelectedIndex == 2 && change.Scope != "Field")
                return false;
            if (_directionFilter.SelectedIndex == 1 && !change.Enabled)
                return false;
            if (_directionFilter.SelectedIndex == 2 && change.Enabled)
                return false;
            return true;
        }

        private static void StyleChangeRow(DataGridViewRow row, bool enabling)
        {
            row.Cells[2].Style.ForeColor = Color.DimGray;
            row.Cells[3].Style.ForeColor = Color.DimGray;
            row.Cells[4].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            row.Cells[4].Style.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            row.Cells[4].Style.BackColor = enabling
                ? Color.FromArgb(220, 245, 225)
                : Color.FromArgb(253, 226, 224);
            row.Cells[4].Style.ForeColor = enabling
                ? Color.FromArgb(20, 105, 50)
                : Color.FromArgb(160, 35, 30);
            row.Cells[4].Style.SelectionBackColor = row.Cells[4].Style.BackColor;
            row.Cells[4].Style.SelectionForeColor = row.Cells[4].Style.ForeColor;
        }

        private static string DisplayName(string displayName, string logicalName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? logicalName : displayName;
        }
    }

    internal sealed class AuditChangeItem
    {
        public string TableName { get; set; }
        public string TableLogicalName { get; set; }
        public string SettingName { get; set; }
        public string LogicalName { get; set; }
        public string Scope { get; set; }
        public bool Enabled { get; set; }
    }
}
