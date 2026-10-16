using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using XrmToolBox.Extensibility;

namespace DataverseAuditManager.XrmToolBox
{
    public sealed class AuditManagerControl : PluginControlBase
    {
        private readonly BindingSource _tableSource = new BindingSource();
        private readonly BindingSource _columnSource = new BindingSource();
        private readonly DataGridView _tableGrid = new DataGridView();
        private readonly DataGridView _columnGrid = new DataGridView();
        private readonly ToolStripButton _applyButton = new ToolStripButton("Apply && Publish");
        private readonly ToolStripButton _revertButton = new ToolStripButton("Revert all");
        private readonly ToolStripButton _reviewChangesButton = new ToolStripButton("Review changes");
        private readonly TextBox _tableSearchBox = new TextBox();
        private readonly CheckBox _auditedTablesOnlyCheckBox = new CheckBox();
        private readonly TextBox _columnSearchBox = new TextBox();
        private readonly CheckBox _auditedColumnsOnlyCheckBox = new CheckBox();
        private readonly Label _environmentLabel = new Label();
        private readonly ToolStripStatusLabel _statusLabel = new ToolStripStatusLabel();
        private readonly Button _enableAllTablesButton = new Button();
        private readonly Button _disableAllTablesButton = new Button();
        private readonly Button _enableAllColumnsButton = new Button();
        private readonly Button _disableAllColumnsButton = new Button();

        private AuditSnapshot _snapshot;
        private bool _binding;

        public AuditManagerControl()
        {
            InitializeUi();
        }

        private AuditTableItem SelectedTable =>
            _tableGrid.CurrentRow?.DataBoundItem as AuditTableItem;

        private int PendingChanges =>
            _snapshot?.Tables.Sum(table => table.PendingChangeCount) ?? 0;

        private void InitializeUi()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;

            var toolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top,
                Padding = new Padding(4, 3, 4, 3)
            };

            var refreshButton = new ToolStripButton("Refresh");
            refreshButton.Click += (_, __) => ExecuteMethod(LoadMetadata);

            _applyButton.Enabled = false;
            _applyButton.Font = new Font(_applyButton.Font, FontStyle.Bold);
            _applyButton.Click += (_, __) => ExecuteMethod(ApplyChanges);

            _revertButton.Enabled = false;
            _revertButton.Click += (_, __) => RevertChanges();

            _reviewChangesButton.Enabled = false;
            _reviewChangesButton.Click += (_, __) => ReviewChanges();

            var exportButton = new ToolStripButton("Export JSON");
            exportButton.Click += (_, __) => ExportConfiguration();

            var closeButton = new ToolStripButton("Close");
            closeButton.Alignment = ToolStripItemAlignment.Right;
            closeButton.Click += (_, __) => CloseTool();

            toolStrip.Items.Add(refreshButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(_applyButton);
            toolStrip.Items.Add(_revertButton);
            toolStrip.Items.Add(_reviewChangesButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(exportButton);
            toolStrip.Items.Add(closeButton);

            var summaryPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(240, 245, 249),
                Padding = new Padding(10, 8, 10, 8)
            };
            _environmentLabel.AutoSize = true;
            _environmentLabel.Font = new Font(Font, FontStyle.Bold);
            _environmentLabel.Text = "Connect to an environment and click Refresh.";
            summaryPanel.Controls.Add(_environmentLabel);

            ConfigureTableGrid();
            ConfigureColumnGrid();

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            Load += (_, __) =>
            {
                var availableWidth = split.ClientSize.Width - split.SplitterWidth;
                if (availableWidth > 600)
                    split.SplitterDistance = (int)(availableWidth * 0.42);
            };

            var tableActions = CreateActionsPanel();
            var tableSearchLabel = new Label
            {
                AutoSize = true,
                Text = "Table search:",
                Anchor = AnchorStyles.Left
            };
            _tableSearchBox.Dock = DockStyle.Fill;
            _tableSearchBox.TextChanged += (_, __) => ApplyTableFilter();

            _auditedTablesOnlyCheckBox.Text = "Audited only";
            _auditedTablesOnlyCheckBox.AutoSize = true;
            _auditedTablesOnlyCheckBox.Anchor = AnchorStyles.Left;
            _auditedTablesOnlyCheckBox.CheckedChanged += (_, __) => ApplyTableFilter();

            _enableAllTablesButton.Text = "Enable all";
            _enableAllTablesButton.AutoSize = true;
            _enableAllTablesButton.Enabled = false;
            _enableAllTablesButton.Click += (_, __) => SetAllVisibleTables(true);

            _disableAllTablesButton.Text = "Disable all";
            _disableAllTablesButton.AutoSize = true;
            _disableAllTablesButton.Enabled = false;
            _disableAllTablesButton.Click += (_, __) => SetAllVisibleTables(false);

            tableActions.Controls.Add(tableSearchLabel, 0, 0);
            tableActions.Controls.Add(_tableSearchBox, 1, 0);
            tableActions.Controls.Add(_auditedTablesOnlyCheckBox, 2, 0);
            tableActions.Controls.Add(_enableAllTablesButton, 3, 0);
            tableActions.Controls.Add(_disableAllTablesButton, 4, 0);

            var tableTitle = CreateSectionTitle("Tables");
            split.Panel1.Controls.Add(_tableGrid);
            split.Panel1.Controls.Add(tableActions);
            split.Panel1.Controls.Add(tableTitle);

            var columnActions = CreateActionsPanel();
            var columnSearchLabel = new Label
            {
                AutoSize = true,
                Text = "Field search:",
                Anchor = AnchorStyles.Left
            };
            _columnSearchBox.Dock = DockStyle.Fill;
            _columnSearchBox.TextChanged += (_, __) => ApplyColumnFilter();

            _auditedColumnsOnlyCheckBox.Text = "Audited only";
            _auditedColumnsOnlyCheckBox.AutoSize = true;
            _auditedColumnsOnlyCheckBox.Anchor = AnchorStyles.Left;
            _auditedColumnsOnlyCheckBox.CheckedChanged += (_, __) => ApplyColumnFilter();

            _enableAllColumnsButton.Text = "Enable all";
            _enableAllColumnsButton.AutoSize = true;
            _enableAllColumnsButton.Enabled = false;
            _enableAllColumnsButton.Click += (_, __) => SetAllVisibleColumns(true);

            _disableAllColumnsButton.Text = "Disable all";
            _disableAllColumnsButton.AutoSize = true;
            _disableAllColumnsButton.Enabled = false;
            _disableAllColumnsButton.Click += (_, __) => SetAllVisibleColumns(false);

            columnActions.Controls.Add(columnSearchLabel, 0, 0);
            columnActions.Controls.Add(_columnSearchBox, 1, 0);
            columnActions.Controls.Add(_auditedColumnsOnlyCheckBox, 2, 0);
            columnActions.Controls.Add(_enableAllColumnsButton, 3, 0);
            columnActions.Controls.Add(_disableAllColumnsButton, 4, 0);

            var columnTitle = CreateSectionTitle("Columns for selected table");
            split.Panel2.Controls.Add(_columnGrid);
            split.Panel2.Controls.Add(columnActions);
            split.Panel2.Controls.Add(columnTitle);

            var statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            _statusLabel.Text = "Ready";
            statusStrip.Items.Add(_statusLabel);

            Controls.Add(split);
            Controls.Add(summaryPanel);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
            ResumeLayout();
        }

        private static TableLayoutPanel CreateActionsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(8, 4, 8, 4),
                ColumnCount = 5,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return panel;
        }

        private static Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = text,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                BackColor = Color.FromArgb(31, 78, 120),
                ForeColor = Color.White,
                Padding = new Padding(8, 7, 0, 0)
            };
        }

        private void ConfigureTableGrid()
        {
            ConfigureGrid(_tableGrid);
            _tableGrid.DataSource = _tableSource;
            _tableGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Pending",
                HeaderText = "",
                Width = 28,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _tableGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Enabled",
                HeaderText = "Audit",
                Width = 52
            });
            _tableGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LogicalName",
                HeaderText = "Logical name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 32,
                MinimumWidth = 140,
                ReadOnly = true
            });
            _tableGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DisplayName",
                HeaderText = "Display name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 32,
                MinimumWidth = 140,
                ReadOnly = true
            });
            _tableGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AuditedColumns",
                HeaderText = "Audited fields",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18,
                MinimumWidth = 95,
                ReadOnly = true
            });
            _tableGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Changeable",
                HeaderText = "Changeable",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18,
                MinimumWidth = 85,
                ReadOnly = true
            });

            _tableGrid.SelectionChanged += (_, __) =>
            {
                if (!_binding)
                    LoadSelectedTableColumns();
            };
            _tableGrid.CurrentCellDirtyStateChanged += (_, __) => CommitDirtyCell(_tableGrid);
            _tableGrid.CellBeginEdit += TableGrid_CellBeginEdit;
            _tableGrid.CellValueChanged += (_, __) => OnTableGridValueChanged();
            _tableGrid.CellFormatting += TableGrid_CellFormatting;
        }

        private void ConfigureColumnGrid()
        {
            ConfigureGrid(_columnGrid);
            _columnGrid.DataSource = _columnSource;
            _columnGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Pending",
                HeaderText = "",
                Width = 28,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            _columnGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Enabled",
                HeaderText = "Audit",
                Width = 52
            });
            _columnGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "LogicalName",
                HeaderText = "Logical name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 32,
                MinimumWidth = 150,
                ReadOnly = true
            });
            _columnGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DisplayName",
                HeaderText = "Display name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 32,
                MinimumWidth = 150,
                ReadOnly = true
            });
            _columnGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Type",
                HeaderText = "Type",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18,
                MinimumWidth = 95,
                ReadOnly = true
            });
            _columnGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Changeable",
                HeaderText = "Changeable",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18,
                MinimumWidth = 85,
                ReadOnly = true
            });

            _columnGrid.CurrentCellDirtyStateChanged += (_, __) => CommitDirtyCell(_columnGrid);
            _columnGrid.CellBeginEdit += ColumnGrid_CellBeginEdit;
            _columnGrid.CellValueChanged += (_, __) => OnColumnGridValueChanged();
            _columnGrid.CellFormatting += ColumnGrid_CellFormatting;
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 24;
            grid.DataError += (_, __) => { };
        }

        private void LoadMetadata()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Reading organization and table audit settings...",
                Work = (_, eventArgs) => eventArgs.Result = DataverseAuditService.Load(Service),
                PostWorkCallBack = eventArgs =>
                {
                    if (eventArgs.Error != null)
                    {
                        ShowErrorDialog(eventArgs.Error, "Load audit metadata");
                        return;
                    }

                    _snapshot = (AuditSnapshot)eventArgs.Result;
                    BindSnapshot();
                }
            });
        }

        private void BindSnapshot()
        {
            _binding = true;
            try
            {
                _environmentLabel.Text =
                    $"{_snapshot.OrganizationName}   |   Environment auditing: " +
                    $"{(_snapshot.EnvironmentAuditingEnabled ? "Enabled" : "Disabled")}   |   " +
                    $"Retention: {(_snapshot.RetentionPeriodDays.HasValue ? _snapshot.RetentionPeriodDays + " days" : "Not set")}   |   " +
                    $"User access auditing: {(_snapshot.UserAccessAuditingEnabled ? "Enabled" : "Disabled")}";
                _environmentLabel.ForeColor = _snapshot.EnvironmentAuditingEnabled
                    ? Color.DarkGreen
                    : Color.DarkRed;
                ApplyTableFilter();
                UpdatePendingState();
            }
            finally
            {
                _binding = false;
            }

            LoadSelectedTableColumns();
        }

        private void ApplyTableFilter()
        {
            if (_snapshot == null)
            {
                _tableSource.DataSource = null;
                _columnSource.DataSource = null;
                _enableAllTablesButton.Enabled = false;
                _disableAllTablesButton.Enabled = false;
                return;
            }

            var selectedName = SelectedTable?.LogicalName;
            var search = _tableSearchBox.Text.Trim();
            IEnumerable<AuditTableItem> tables = _snapshot.Tables;

            if (!string.IsNullOrWhiteSpace(search))
            {
                tables = tables.Where(table =>
                    Contains(table.LogicalName, search) ||
                    Contains(table.DisplayName, search));
            }

            if (_auditedTablesOnlyCheckBox.Checked)
                tables = tables.Where(table => table.Enabled);

            var wasBinding = _binding;
            _binding = true;
            try
            {
                var visibleTables = tables.ToList();
                _tableSource.DataSource = visibleTables;
                _enableAllTablesButton.Enabled = visibleTables.Any(table => table.CanBeChanged && !table.Enabled);
                _disableAllTablesButton.Enabled = visibleTables.Any(table => table.CanBeChanged && table.Enabled);
                SelectTable(selectedName);
                ApplyColumnFilter();
            }
            finally
            {
                _binding = wasBinding;
            }

            if (!wasBinding)
                LoadSelectedTableColumns();
        }

        private void ApplyColumnFilter()
        {
            var table = SelectedTable;
            if (table == null || !table.ColumnsLoaded)
            {
                _columnSource.DataSource = null;
                _enableAllColumnsButton.Enabled = false;
                _disableAllColumnsButton.Enabled = false;
                return;
            }

            var search = _columnSearchBox.Text.Trim();
            IEnumerable<AuditColumnItem> columns = table.Columns;
            if (!string.IsNullOrWhiteSpace(search))
            {
                columns = columns.Where(column =>
                    Contains(column.LogicalName, search) ||
                    Contains(column.DisplayName, search) ||
                    Contains(column.Type, search));
            }

            if (_auditedColumnsOnlyCheckBox.Checked)
                columns = columns.Where(column => column.Enabled);

            var wasBinding = _binding;
            _binding = true;
            try
            {
                var visibleColumns = columns.ToList();
                _columnSource.DataSource = visibleColumns;
                _enableAllColumnsButton.Enabled = visibleColumns.Any(column => column.CanBeChanged && !column.Enabled);
                _disableAllColumnsButton.Enabled = visibleColumns.Any(column => column.CanBeChanged && column.Enabled);
            }
            finally
            {
                _binding = wasBinding;
            }
        }

        private void LoadSelectedTableColumns()
        {
            var table = SelectedTable;
            if (table == null)
            {
                ApplyColumnFilter();
                return;
            }

            if (table.ColumnsLoaded)
            {
                ApplyColumnFilter();
                return;
            }

            if (table.ColumnsLoading)
                return;

            table.ColumnsLoading = true;
            _columnSource.DataSource = null;
            _enableAllColumnsButton.Enabled = false;
            _disableAllColumnsButton.Enabled = false;
            RefreshTableRow(table);

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Reading fields for {table.DisplayName} ({table.LogicalName})...",
                Work = (_, eventArgs) =>
                    eventArgs.Result = DataverseAuditService.LoadColumns(Service, table.LogicalName),
                PostWorkCallBack = eventArgs =>
                {
                    table.ColumnsLoading = false;
                    if (eventArgs.Error != null)
                    {
                        var wasBinding = _binding;
                        _binding = true;
                        try
                        {
                            RefreshTableRow(table);
                        }
                        finally
                        {
                            _binding = wasBinding;
                        }

                        ShowErrorDialog(eventArgs.Error, $"Load fields for {table.LogicalName}");
                        return;
                    }

                    table.Columns = (List<AuditColumnItem>)eventArgs.Result;
                    table.ColumnsLoaded = true;
                    RefreshTableRow(table);

                    if (string.Equals(SelectedTable?.LogicalName, table.LogicalName, StringComparison.OrdinalIgnoreCase))
                        ApplyColumnFilter();

                    UpdatePendingState();
                }
            });
        }

        private static bool Contains(string value, string search)
        {
            return value?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetAllVisibleTables(bool enabled)
        {
            var tables = _tableSource.List.Cast<AuditTableItem>()
                .Where(table => table.CanBeChanged)
                .ToList();
            if (tables.Count == 0)
                return;

            foreach (var table in tables)
                table.Enabled = enabled;

            ApplyTableFilter();
            UpdatePendingState();
        }

        private void SetAllVisibleColumns(bool enabled)
        {
            var columns = _columnSource.List.Cast<AuditColumnItem>()
                .Where(column => column.CanBeChanged)
                .ToList();
            if (columns.Count == 0)
                return;

            foreach (var column in columns)
                column.Enabled = enabled;

            ApplyColumnFilter();
            RefreshTableRow(SelectedTable);
            UpdatePendingState();
        }

        private void ReviewChanges()
        {
            _tableGrid.EndEdit();
            _columnGrid.EndEdit();
            _tableSource.EndEdit();
            _columnSource.EndEdit();

            if (_snapshot == null || PendingChanges == 0)
            {
                MessageBox.Show(this, "There are no pending audit changes.", "Review changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new AuditReviewForm(_snapshot.Tables))
            {
                var result = dialog.ShowDialog(this);
                ApplyTableFilter();
                UpdatePendingState();

                if (result == DialogResult.OK)
                    ExecuteMethod(() => ApplyChanges(true));
            }
        }

        private void RevertChanges()
        {
            if (_snapshot == null || PendingChanges == 0)
                return;

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

            foreach (var table in _snapshot.Tables)
                table.Revert();

            ApplyTableFilter();
            UpdatePendingState();
        }

        private void ApplyChanges()
        {
            ApplyChanges(false);
        }

        private void ApplyChanges(bool reviewed)
        {
            _tableGrid.EndEdit();
            _columnGrid.EndEdit();
            _tableSource.EndEdit();
            _columnSource.EndEdit();

            var tableChanges = _snapshot.Tables.Count(table => table.IsChanged);
            var columnChanges = _snapshot.Tables.Sum(table => table.Columns.Count(column => column.IsChanged));
            if (tableChanges + columnChanges == 0)
                return;

            var confirmation =
                $"Apply {tableChanges} table change(s) and {columnChanges} column change(s)?\n\n" +
                "The affected tables will be published after the updates complete.";

            if (!reviewed && MessageBox.Show(
                    this,
                    confirmation,
                    "Apply audit configuration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Applying audit settings and publishing affected tables...",
                Work = (_, eventArgs) =>
                    eventArgs.Result = DataverseAuditService.Apply(Service, _snapshot.Tables),
                PostWorkCallBack = eventArgs =>
                {
                    if (eventArgs.Error != null)
                    {
                        ShowErrorDialog(eventArgs.Error, "Apply audit configuration");
                        return;
                    }

                    var result = (ApplyResult)eventArgs.Result;
                    MessageBox.Show(
                        this,
                        $"Updated {result.UpdatedTables} table setting(s) and " +
                        $"{result.UpdatedColumns} column setting(s).\n" +
                        $"Published {result.PublishedTables} affected table(s).",
                        "Audit configuration updated",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    LoadMetadata();
                }
            });
        }

        private void ExportConfiguration()
        {
            if (_snapshot == null)
            {
                MessageBox.Show(this, "Load audit metadata first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName;
            using (var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "dataverse-audit-configuration.json",
                AddExtension = true,
                DefaultExt = "json"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                fileName = dialog.FileName;
            }

            var unloadedTables = _snapshot.Tables.Where(table => !table.ColumnsLoaded).ToList();
            if (unloadedTables.Count == 0)
            {
                WriteExportConfiguration(fileName);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading all remaining field metadata for export...",
                Work = (_, eventArgs) => eventArgs.Result = DataverseAuditService.LoadAllColumns(Service),
                PostWorkCallBack = eventArgs =>
                {
                    if (eventArgs.Error != null)
                    {
                        ShowErrorDialog(eventArgs.Error, "Load fields for export");
                        return;
                    }

                    var columnsByTable = (Dictionary<string, List<AuditColumnItem>>)eventArgs.Result;
                    foreach (var table in unloadedTables)
                    {
                        if (!columnsByTable.TryGetValue(table.LogicalName, out var columns))
                            continue;

                        table.Columns = columns;
                        table.ColumnsLoaded = true;
                        table.ColumnsLoading = false;
                    }

                    _tableSource.ResetBindings(false);
                    ApplyColumnFilter();
                    WriteExportConfiguration(fileName);
                }
            });
        }

        private void WriteExportConfiguration(string fileName)
        {
            var export = new AuditExport
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                OrganizationName = _snapshot.OrganizationName,
                EnvironmentAuditingEnabled = _snapshot.EnvironmentAuditingEnabled,
                Tables = _snapshot.Tables.Select(table => new AuditExportTable
                {
                    LogicalName = table.LogicalName,
                    DisplayName = table.DisplayName,
                    Enabled = table.Enabled,
                    CanBeChanged = table.CanBeChanged,
                    Columns = table.Columns.Select(column => new AuditExportColumn
                    {
                        LogicalName = column.LogicalName,
                        DisplayName = column.DisplayName,
                        Type = column.Type,
                        Enabled = column.Enabled,
                        CanBeChanged = column.CanBeChanged
                    }).ToList()
                }).ToList()
            };

            File.WriteAllText(fileName, JsonConvert.SerializeObject(export, Formatting.Indented));
            _statusLabel.Text = $"Exported configuration to {fileName}";
        }

        private void RefreshTableRow(AuditTableItem table)
        {
            if (table == null)
                return;

            var index = _tableSource.List?.IndexOf(table) ?? -1;
            if (index >= 0)
                _tableSource.ResetItem(index);
        }

        private void UpdatePendingState()
        {
            var pending = PendingChanges;
            _applyButton.Enabled = pending > 0;
            _revertButton.Enabled = pending > 0;
            _reviewChangesButton.Enabled = pending > 0;
            _statusLabel.Text = _snapshot == null
                ? "Ready"
                : $"{_snapshot.Tables.Count} tables loaded | {pending} pending change(s)";
            _tableGrid.Invalidate();
            _columnGrid.Invalidate();
        }

        private void OnTableGridValueChanged()
        {
            if (_binding)
                return;

            _tableSource.EndEdit();
            _columnSource.EndEdit();
            ApplyTableFilter();
            UpdatePendingState();
        }

        private void OnColumnGridValueChanged()
        {
            if (_binding)
                return;

            _tableSource.EndEdit();
            _columnSource.EndEdit();
            ApplyColumnFilter();
            RefreshTableRow(SelectedTable);
            UpdatePendingState();
        }

        private static void CommitDirtyCell(DataGridView grid)
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void TableGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs eventArgs)
        {
            if (eventArgs.ColumnIndex != 1)
            {
                eventArgs.Cancel = true;
                return;
            }

            if (_tableGrid.Rows[eventArgs.RowIndex].DataBoundItem is AuditTableItem table && !table.CanBeChanged)
                eventArgs.Cancel = true;
        }

        private void ColumnGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs eventArgs)
        {
            if (eventArgs.ColumnIndex != 1)
            {
                eventArgs.Cancel = true;
                return;
            }

            if (_columnGrid.Rows[eventArgs.RowIndex].DataBoundItem is AuditColumnItem column && !column.CanBeChanged)
                eventArgs.Cancel = true;
        }

        private void TableGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs eventArgs)
        {
            if (!(_tableGrid.Rows[eventArgs.RowIndex].DataBoundItem is AuditTableItem table))
                return;

            FormatRow(_tableGrid.Rows[eventArgs.RowIndex], table.CanBeChanged, table.IsChanged || table.Columns.Any(column => column.IsChanged));
        }

        private void ColumnGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs eventArgs)
        {
            if (!(_columnGrid.Rows[eventArgs.RowIndex].DataBoundItem is AuditColumnItem column))
                return;

            FormatRow(_columnGrid.Rows[eventArgs.RowIndex], column.CanBeChanged, column.IsChanged);
        }

        private static void FormatRow(DataGridViewRow row, bool canBeChanged, bool changed)
        {
            row.DefaultCellStyle.BackColor = changed
                ? Color.FromArgb(255, 248, 220)
                : canBeChanged
                    ? Color.White
                    : Color.FromArgb(240, 240, 240);
            row.DefaultCellStyle.ForeColor = canBeChanged ? Color.Black : Color.DimGray;
        }

        private void SelectTable(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
                return;

            foreach (DataGridViewRow row in _tableGrid.Rows)
            {
                if (row.DataBoundItem is AuditTableItem table &&
                    string.Equals(table.LogicalName, logicalName, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    _tableGrid.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }
    }
}
