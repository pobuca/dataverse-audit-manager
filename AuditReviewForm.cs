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
        private readonly Label _summaryLabel = new Label();
        private readonly Panel _warningPanel = new Panel();
        private readonly Label _warningLabel = new Label();
        private readonly Button _applyButton = new Button();
        private readonly Button _revertAllButton = new Button();

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
            ConfigureWarningPanel();
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

            _applyButton.Text = "Apply && Publish";
            _applyButton.AutoSize = true;
            _applyButton.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            _applyButton.Click += (_, __) => ConfirmApply();

            _revertAllButton.Text = "Revert all";
            _revertAllButton.AutoSize = true;
            _revertAllButton.Click += (_, __) => ConfirmRevertAll();

            footer.Controls.Add(closeButton);
            footer.Controls.Add(_applyButton);
            footer.Controls.Add(_revertAllButton);
            Controls.Add(_changesGrid);
            Controls.Add(filters);
            Controls.Add(_warningPanel);
            Controls.Add(summaryPanel);
            Controls.Add(footer);

            AcceptButton = _applyButton;
            CancelButton = closeButton;
            RefreshReview();
        }

        private Control CreateSummaryPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(240, 245, 249),
                Padding = new Padding(12, 8, 12, 6)
            };
            _summaryLabel.Dock = DockStyle.Fill;
            _summaryLabel.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(_summaryLabel);
            return panel;
        }

        private void ConfigureWarningPanel()
        {
            _warningPanel.Dock = DockStyle.Top;
            _warningPanel.Height = 44;
            _warningPanel.BackColor = Color.FromArgb(255, 244, 206);
            _warningPanel.Padding = new Padding(12, 7, 12, 7);

            _warningLabel.Dock = DockStyle.Fill;
            _warningLabel.AutoEllipsis = true;
            _warningLabel.TextAlign = ContentAlignment.MiddleLeft;
            _warningLabel.ForeColor = Color.FromArgb(102, 60, 0);
            _warningPanel.Controls.Add(_warningLabel);
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
            _changesGrid.CellContentClick += ChangesGrid_CellContentClick;

            _changesGrid.Columns.Add(CreateTextColumn("Table", 210, DataGridViewAutoSizeColumnMode.Fill));
            _changesGrid.Columns.Add(CreateTextColumn("Setting", 225, DataGridViewAutoSizeColumnMode.Fill));
            _changesGrid.Columns.Add(CreateTextColumn("Logical name", 175));
            _changesGrid.Columns.Add(CreateTextColumn("Scope", 65));
            _changesGrid.Columns.Add(CreateTextColumn("Action", 85));
            _changesGrid.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = string.Empty,
                Text = "Revert",
                UseColumnTextForButtonValue = true,
                Width = 74,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
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

        private void RefreshReview()
        {
            var tableChanges = _changedTables.Count(table => table.IsChanged);
            var fieldChanges = _changedTables.Sum(table => table.Columns.Count(column => column.IsChanged));
            var total = tableChanges + fieldChanges;
            var enabling = _changedTables.Sum(table => (table.IsChanged && table.Enabled ? 1 : 0) +
                table.Columns.Count(column => column.IsChanged && column.Enabled));
            var disabling = total - enabling;

            _summaryLabel.Text = $"{total} pending change(s)  |  {enabling} enabling  |  {disabling} disabling  |  " +
                                 $"{tableChanges} table(s)  |  {fieldChanges} field(s)  |  Nothing has been applied yet.";
            _applyButton.Enabled = total > 0;
            _revertAllButton.Enabled = total > 0;

            var warnings = _changedTables
                .Where(table =>
                    (table.IsChanged || table.Columns.Any(column => column.IsChanged)) &&
                    !table.Enabled &&
                    table.Columns.Any(column => column.Enabled))
                .Select(table =>
                    $"{DisplayName(table.DisplayName, table.LogicalName)} ({table.Columns.Count(column => column.Enabled)})")
                .ToList();
            _warningPanel.Visible = warnings.Count > 0;
            _warningLabel.Text = warnings.Count == 0
                ? string.Empty
                : "Warning: enabled field settings will be inactive for disabled tables: " + string.Join(", ", warnings) + ".";

            PopulateChangesGrid();
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
                    change.Enabled ? "Enable" : "Disable",
                    "Revert");
                var row = _changesGrid.Rows[rowIndex];
                row.Tag = change;
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
                        Table = table,
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
                        Table = table,
                        Column = column,
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

        private void ChangesGrid_CellContentClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex != 5)
                return;

            if (_changesGrid.Rows[eventArgs.RowIndex].Tag is AuditChangeItem change)
            {
                change.Revert();
                RefreshReview();
            }
        }

        private void ConfirmApply()
        {
            var tableChanges = _changedTables.Count(table => table.IsChanged);
            var fieldChanges = _changedTables.Sum(table => table.Columns.Count(column => column.IsChanged));
            if (tableChanges + fieldChanges == 0)
                return;

            if (MessageBox.Show(
                    this,
                    $"Apply {tableChanges} table change(s) and {fieldChanges} field change(s)?\n\n" +
                    "The affected tables will be published after the updates complete.",
                    "Apply audit configuration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ConfirmRevertAll()
        {
            if (MessageBox.Show(
                    this,
                    "Discard all pending audit changes?",
                    "Revert all changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            foreach (var table in _changedTables)
                table.Revert();

            RefreshReview();
            DialogResult = DialogResult.Abort;
            Close();
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
        public AuditTableItem Table { get; set; }
        public AuditColumnItem Column { get; set; }
        public string TableName { get; set; }
        public string TableLogicalName { get; set; }
        public string SettingName { get; set; }
        public string LogicalName { get; set; }
        public string Scope { get; set; }
        public bool Enabled { get; set; }

        public void Revert()
        {
            if (Column == null)
                Table.Enabled = Table.OriginalEnabled;
            else
                Column.Enabled = Column.OriginalEnabled;
        }
    }
}
