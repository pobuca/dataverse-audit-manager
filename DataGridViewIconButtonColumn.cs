using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class DataGridViewIconButtonColumn : DataGridViewButtonColumn
    {
        public DataGridViewIconButtonColumn()
        {
            CellTemplate = new DataGridViewIconButtonCell();
        }

        public Image Icon { get; set; }

        public override object Clone()
        {
            var clone = (DataGridViewIconButtonColumn)base.Clone();
            clone.Icon = Icon;
            return clone;
        }
    }

    internal sealed class DataGridViewIconButtonCell : DataGridViewButtonCell
    {
        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates elementState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(
                graphics,
                clipBounds,
                cellBounds,
                rowIndex,
                elementState,
                value,
                string.Empty,
                errorText,
                cellStyle,
                advancedBorderStyle,
                paintParts);

            var column = OwningColumn as DataGridViewIconButtonColumn;
            var icon = column?.Icon;
            var text = Convert.ToString(formattedValue) ?? string.Empty;
            var textSize = TextRenderer.MeasureText(
                graphics,
                text,
                cellStyle.Font,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            var iconSize = icon == null ? 0 : Math.Min(14, cellBounds.Height - 10);
            var gap = iconSize > 0 && textSize.Width > 0 ? 4 : 0;
            var contentWidth = iconSize + gap + textSize.Width;
            var contentX = cellBounds.X + Math.Max(3, (cellBounds.Width - contentWidth) / 2);

            if (icon != null && iconSize > 0)
            {
                graphics.DrawImage(
                    icon,
                    new Rectangle(
                        contentX,
                        cellBounds.Y + (cellBounds.Height - iconSize) / 2,
                        iconSize,
                        iconSize));
                contentX += iconSize + gap;
            }

            var foreground = (elementState & DataGridViewElementStates.Selected) != 0
                ? cellStyle.SelectionForeColor
                : cellStyle.ForeColor;
            TextRenderer.DrawText(
                graphics,
                text,
                cellStyle.Font,
                new Rectangle(
                    contentX,
                    cellBounds.Y,
                    Math.Max(0, cellBounds.Right - contentX - 2),
                    cellBounds.Height),
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }
}
