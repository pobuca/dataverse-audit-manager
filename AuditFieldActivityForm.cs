using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class AuditFieldActivityForm : Form
    {
        private readonly AuditFieldActivitySnapshot _snapshot;
        private readonly SmoothDataGridView _grid = new SmoothDataGridView();
        private readonly TextBox _searchBox = new TextBox();
        private readonly CheckBox _withActivityOnlyCheckBox = new CheckBox();
        private readonly CheckBox _auditedOnlyCheckBox = new CheckBox();
        private string _sortProperty = "AuditEventCount";
        private SortOrder _sortOrder = SortOrder.Descending;

        public AuditFieldActivityForm(AuditFieldActivitySnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            Text = $"Field audit activity — {_snapshot.TableDisplayName}";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1040, 680);
            MinimumSize = new Size(780, 480);
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;

            Controls.Add(CreateGrid());
            Controls.Add(CreateFilters());
            Controls.Add(CreateSummary());
            Controls.Add(CreateFooter());

            _searchBox.TextChanged += (_, __) => PopulateGrid();
            _withActivityOnlyCheckBox.CheckedChanged += (_, __) => PopulateGrid();
            _auditedOnlyCheckBox.CheckedChanged += (_, __) => PopulateGrid();
            PopulateGrid();
        }

        private Control CreateSummary()
        {
            var mappedEvents = _snapshot.Fields.Sum(field => field.AuditEventCount);
            var warning = _snapshot.IsTruncated
                ? "  |  Latest 100,000 records sampled"
                : string.Empty;
            var unmapped = _snapshot.UnmappedFieldReferences > 0
                ? $"  |  {_snapshot.UnmappedFieldReferences:N0} unmapped field reference(s)"
                : string.Empty;
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(240, 245, 249),
                Padding = new Padding(12, 8, 12, 6)
            };
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text =
                    $"{_snapshot.TableDisplayName} ({_snapshot.TableLogicalName})  |  " +
                    $"{_snapshot.ScannedAuditRecords:N0} field-change audit record(s)  |  " +
                    $"{mappedEvents:N0} field occurrence(s){warning}{unmapped}\n" +
                    $"Period: {_snapshot.FromUtc.ToLocalTime():g} to {_snapshot.ToUtc.ToLocalTime():g}. " +
                    "Activity counts are not storage-byte estimates."
            });
            return panel;
        }

        private Control CreateFilters()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 43,
                Padding = new Padding(8, 7, 8, 5),
                ColumnCount = 4,
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var searchLabel = new Label { Text = "Field search:", AutoSize = true, Anchor = AnchorStyles.Left };
            _searchBox.Dock = DockStyle.Fill;
            _withActivityOnlyCheckBox.Text = "With activity only";
            _withActivityOnlyCheckBox.Checked = true;
            _withActivityOnlyCheckBox.AutoSize = true;
            _withActivityOnlyCheckBox.Anchor = AnchorStyles.Left;
            _auditedOnlyCheckBox.Text = "Currently audited only";
            _auditedOnlyCheckBox.AutoSize = true;
            _auditedOnlyCheckBox.Anchor = AnchorStyles.Left;

            panel.Controls.Add(searchLabel, 0, 0);
            panel.Controls.Add(_searchBox, 1, 0);
            panel.Controls.Add(_withActivityOnlyCheckBox, 2, 0);
            panel.Controls.Add(_auditedOnlyCheckBox, 3, 0);
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
            _grid.RowTemplate.Height = 26;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;

            _grid.Columns.Add(CreateColumn("AuditingStatus", "Auditing", 82));
            _grid.Columns.Add(CreateFillColumn("LogicalName", "Logical name", 25, 150));
            _grid.Columns.Add(CreateFillColumn("DisplayName", "Display name", 25, 150));
            _grid.Columns.Add(CreateColumn("Type", "Type", 105));
            _grid.Columns.Add(CreateColumn("AuditEventCount", "Audit events", 100));
            _grid.Columns.Add(CreateColumn("RecordPercentage", "% of records", 95));
            _grid.Columns.Add(CreateColumn("LastChangedUtc", "Last activity", 145));
            _grid.Columns[4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns[5].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
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
            var closeButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            ToolbarIcons.Configure(closeButton, ToolbarIcons.Close);
            footer.Controls.Add(closeButton);
            CancelButton = closeButton;
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
            IEnumerable<AuditFieldActivityItem> fields = _snapshot.Fields;
            if (_withActivityOnlyCheckBox.Checked)
                fields = fields.Where(field => field.AuditEventCount > 0);
            if (_auditedOnlyCheckBox.Checked)
                fields = fields.Where(field => field.AuditingEnabled);
            if (!string.IsNullOrWhiteSpace(search))
            {
                fields = fields.Where(field =>
                    Contains(field.LogicalName, search) || Contains(field.DisplayName, search));
            }

            fields = SortFields(fields);
            _grid.Rows.Clear();
            foreach (var field in fields)
            {
                var rowIndex = _grid.Rows.Add(
                    field.AuditingStatus,
                    field.LogicalName,
                    field.DisplayName,
                    field.Type,
                    field.AuditEventCount.ToString("N0"),
                    $"{field.RecordPercentage:N2}%",
                    field.LastChangedUtc?.ToLocalTime().ToString("g") ?? string.Empty);
                if (!field.AuditingEnabled)
                    _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DimGray;
            }

            UpdateSortGlyph();
            _grid.ClearSelection();
        }

        private IEnumerable<AuditFieldActivityItem> SortFields(IEnumerable<AuditFieldActivityItem> fields)
        {
            var descending = _sortOrder == SortOrder.Descending;
            switch (_sortProperty)
            {
                case "AuditingStatus":
                    return OrderText(fields, field => field.AuditingStatus, descending);
                case "LogicalName":
                    return OrderText(fields, field => field.LogicalName, descending);
                case "DisplayName":
                    return OrderText(fields, field => field.DisplayName, descending);
                case "Type":
                    return OrderText(fields, field => field.Type, descending);
                case "RecordPercentage":
                    return descending
                        ? fields.OrderByDescending(field => field.RecordPercentage)
                        : fields.OrderBy(field => field.RecordPercentage);
                case "LastChangedUtc":
                    return descending
                        ? fields.OrderByDescending(field => field.LastChangedUtc)
                        : fields.OrderBy(field => field.LastChangedUtc);
                default:
                    return descending
                        ? fields.OrderByDescending(field => field.AuditEventCount)
                        : fields.OrderBy(field => field.AuditEventCount);
            }
        }

        private static IOrderedEnumerable<AuditFieldActivityItem> OrderText(
            IEnumerable<AuditFieldActivityItem> fields,
            Func<AuditFieldActivityItem, string> keySelector,
            bool descending)
        {
            return descending
                ? fields.OrderByDescending(keySelector, StringComparer.OrdinalIgnoreCase)
                : fields.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs eventArgs)
        {
            if (eventArgs.ColumnIndex < 0)
                return;

            var property = _grid.Columns[eventArgs.ColumnIndex].Name;
            if (string.Equals(property, _sortProperty, StringComparison.Ordinal))
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortProperty = property;
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
    }
}
