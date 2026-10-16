using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal sealed class SmoothDataGridView : DataGridView
    {
        public SmoothDataGridView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
