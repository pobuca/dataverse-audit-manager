using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class AuditCleanupForm : Form
    {
        private readonly AuditStorageItem _table;
        private readonly CheckBox _limitByDateCheckBox = new CheckBox();
        private readonly DateTimePicker _cutoffPicker = new DateTimePicker();
        private readonly TextBox _confirmationBox = new TextBox();
        private readonly Button _deleteButton = new Button();

        public AuditCleanupForm(AuditStorageItem table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));

            Text = "Delete table audit history";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(590, 350);
            AutoScaleMode = AutoScaleMode.Font;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(255, 244, 230),
                Padding = new Padding(14, 10, 14, 8)
            };
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 58, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Permanent operation\nDeleted audit history cannot be viewed or recovered."
            });

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 8),
                ColumnCount = 2,
                RowCount = 6
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            content.Controls.Add(new Label { Text = "Table:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            content.Controls.Add(new Label
            {
                Text = $"{_table.DisplayName} ({_table.LogicalName})",
                AutoSize = true,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Anchor = AnchorStyles.Left
            }, 1, 0);
            content.Controls.Add(new Label { Text = "Reported storage:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            content.Controls.Add(new Label
            {
                Text = FormatBytes(_table.SizeInBytes),
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 1, 1);

            _limitByDateCheckBox.Text = "Only delete audit records created before";
            _limitByDateCheckBox.Checked = true;
            _limitByDateCheckBox.AutoSize = true;
            _limitByDateCheckBox.Anchor = AnchorStyles.Left;
            _limitByDateCheckBox.CheckedChanged += (_, __) => _cutoffPicker.Enabled = _limitByDateCheckBox.Checked;
            content.Controls.Add(_limitByDateCheckBox, 0, 2);
            content.SetColumnSpan(_limitByDateCheckBox, 2);

            _cutoffPicker.Format = DateTimePickerFormat.Custom;
            _cutoffPicker.CustomFormat = "yyyy-MM-dd";
            _cutoffPicker.Value = DateTime.Today.AddDays(-90);
            _cutoffPicker.MaxDate = DateTime.Today;
            _cutoffPicker.Width = 140;
            content.Controls.Add(new Label { Text = "Cutoff date:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            content.Controls.Add(_cutoffPicker, 1, 3);

            content.Controls.Add(new Label
            {
                Text = $"Type {_table.LogicalName} to confirm:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 4);
            _confirmationBox.Dock = DockStyle.Fill;
            _confirmationBox.TextChanged += (_, __) => UpdateDeleteButton();
            content.Controls.Add(_confirmationBox, 1, 4);

            content.Controls.Add(new Label
            {
                Text = "Dataverse runs this as an asynchronous Bulk Delete system job. " +
                       "The server validates your privileges before creating the job.",
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 12, 0, 0)
            }, 0, 5);
            content.SetColumnSpan(content.GetControlFromPosition(0, 5), 2);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(9),
                WrapContents = false
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            ToolbarIcons.Configure(cancelButton, ToolbarIcons.Close);

            _deleteButton.Text = "Delete audit history";
            _deleteButton.AutoSize = true;
            _deleteButton.Enabled = false;
            _deleteButton.Click += (_, __) => ConfirmDelete();
            ToolbarIcons.Configure(_deleteButton, ToolbarIcons.Delete);
            footer.Controls.Add(cancelButton);
            footer.Controls.Add(_deleteButton);

            Controls.Add(content);
            Controls.Add(header);
            Controls.Add(footer);
            AcceptButton = _deleteButton;
            CancelButton = cancelButton;
        }

        public AuditCleanupRequest CleanupRequest => new AuditCleanupRequest
        {
            TableLogicalName = _table.LogicalName,
            TableDisplayName = _table.DisplayName,
            CreatedBeforeUtc = _limitByDateCheckBox.Checked
                ? _cutoffPicker.Value.Date.ToUniversalTime()
                : (DateTime?)null
        };

        private void UpdateDeleteButton()
        {
            _deleteButton.Enabled = string.Equals(
                _confirmationBox.Text.Trim(),
                _table.LogicalName,
                StringComparison.OrdinalIgnoreCase);
        }

        private void ConfirmDelete()
        {
            var scope = _limitByDateCheckBox.Checked
                ? $"audit history created before {_cutoffPicker.Value:yyyy-MM-dd}"
                : "all available audit history";
            if (MessageBox.Show(
                    this,
                    $"Permanently delete {scope} for '{_table.LogicalName}'?\n\n" +
                    "Dataverse will run the deletion in the background. This cannot be undone.",
                    "Confirm audit history deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var value = Math.Max(0, bytes);
            var display = (double)value;
            var unit = 0;
            while (display >= 1024 && unit < units.Length - 1)
            {
                display /= 1024;
                unit++;
            }

            return unit == 0 ? $"{value:N0} {units[unit]}" : $"{display:N2} {units[unit]}";
        }
    }
}
