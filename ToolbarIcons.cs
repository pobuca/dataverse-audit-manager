using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace DataverseAuditManager.XrmToolBox
{
    internal static class ToolbarIcons
    {
        private static readonly Color Blue = Color.FromArgb(0, 107, 253);
        private static readonly Color Teal = Color.FromArgb(34, 193, 197);
        private static readonly Color Magenta = Color.FromArgb(198, 67, 202);
        private static readonly Color Dark = Color.FromArgb(55, 65, 81);

        public static Image Refresh { get; } = DrawRefresh();
        public static Image Apply { get; } = DrawApply();
        public static Image Enable { get; } = DrawEnable();
        public static Image Disable { get; } = DrawDisable();
        public static Image Revert { get; } = DrawRevert();
        public static Image Review { get; } = DrawReview();
        public static Image Usage { get; } = DrawUsage();
        public static Image Export { get; } = DrawExport();
        public static Image Delete { get; } = DrawDelete();
        public static Image Close { get; } = DrawClose();

        public static void Configure(Button button, Image image)
        {
            button.Image = new Bitmap(image, new Size(14, 14));
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.Padding = new Padding(4, 0, 4, 0);
        }

        private static Bitmap CreateBitmap(Action<Graphics> draw)
        {
            var bitmap = new Bitmap(20, 20, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                draw(graphics);
            }

            return bitmap;
        }

        private static Image DrawRefresh()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Blue, 2.2f))
                    graphics.DrawArc(pen, 3, 3, 14, 14, 35, 285);

                using (var brush = new SolidBrush(Blue))
                {
                    graphics.FillPolygon(brush, new[]
                    {
                        new PointF(14.2f, 2.3f),
                        new PointF(18.1f, 3.1f),
                        new PointF(16.6f, 6.8f)
                    });
                }
            });
        }

        private static Image DrawApply()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Teal, 1.8f))
                    graphics.DrawEllipse(pen, 2.5f, 2.5f, 15, 15);
                using (var pen = CreatePen(Blue, 2.4f))
                {
                    graphics.DrawLines(pen, new[]
                    {
                        new PointF(5.3f, 10.1f),
                        new PointF(8.4f, 13.1f),
                        new PointF(14.8f, 6.8f)
                    });
                }
            });
        }

        private static Image DrawEnable()
        {
            return CreateBitmap(graphics =>
            {
                using (var track = CreateRoundedRectangle(new RectangleF(1.5f, 5, 17, 10), 5))
                using (var brush = new SolidBrush(Teal))
                using (var pen = CreatePen(Blue, 1.2f))
                {
                    graphics.FillPath(brush, track);
                    graphics.DrawPath(pen, track);
                }

                using (var brush = new SolidBrush(Color.White))
                    graphics.FillEllipse(brush, 10.2f, 6.2f, 7.6f, 7.6f);
                using (var pen = CreatePen(Blue, 1.0f))
                    graphics.DrawEllipse(pen, 10.2f, 6.2f, 7.6f, 7.6f);
            });
        }

        private static Image DrawDisable()
        {
            return CreateBitmap(graphics =>
            {
                using (var track = CreateRoundedRectangle(new RectangleF(1.5f, 5, 17, 10), 5))
                using (var pen = CreatePen(Magenta, 1.7f))
                    graphics.DrawPath(pen, track);

                using (var brush = new SolidBrush(Magenta))
                    graphics.FillEllipse(brush, 2.2f, 6.2f, 7.6f, 7.6f);
            });
        }

        private static Image DrawRevert()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Magenta, 2.2f))
                    graphics.DrawArc(pen, 4, 4, 13, 12, 205, 270);
                using (var brush = new SolidBrush(Magenta))
                {
                    graphics.FillPolygon(brush, new[]
                    {
                        new PointF(2.1f, 6.4f),
                        new PointF(7.4f, 5.1f),
                        new PointF(6.0f, 10.3f)
                    });
                }
            });
        }

        private static Image DrawReview()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Blue, 1.7f))
                {
                    graphics.DrawRectangle(pen, 3.2f, 2.2f, 13.2f, 15.3f);
                    graphics.DrawLine(pen, 7.5f, 6.0f, 13.8f, 6.0f);
                    graphics.DrawLine(pen, 7.5f, 10.0f, 13.8f, 10.0f);
                    graphics.DrawLine(pen, 7.5f, 14.0f, 13.8f, 14.0f);
                }
                using (var pen = CreatePen(Teal, 1.8f))
                {
                    graphics.DrawLines(pen, new[]
                    {
                        new PointF(4.3f, 9.8f),
                        new PointF(5.3f, 10.8f),
                        new PointF(6.8f, 8.8f)
                    });
                }
            });
        }

        private static Image DrawUsage()
        {
            return CreateBitmap(graphics =>
            {
                using (var brush = new SolidBrush(Teal))
                    graphics.FillRectangle(brush, 3, 11, 3, 6);
                using (var brush = new SolidBrush(Blue))
                    graphics.FillRectangle(brush, 8, 7, 3, 10);
                using (var brush = new SolidBrush(Magenta))
                    graphics.FillRectangle(brush, 13, 3, 3, 14);
                using (var pen = CreatePen(Dark, 1.4f))
                {
                    graphics.DrawLine(pen, 2, 17.5f, 18, 17.5f);
                    graphics.DrawLine(pen, 2.5f, 2, 2.5f, 18);
                }
            });
        }

        private static Image DrawExport()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Dark, 1.6f))
                    graphics.DrawRectangle(pen, 3, 2, 10, 15);
                using (var pen = CreatePen(Blue, 2.0f))
                {
                    graphics.DrawLine(pen, 14.5f, 7, 14.5f, 15);
                    graphics.DrawLines(pen, new[]
                    {
                        new PointF(11.5f, 12.3f),
                        new PointF(14.5f, 15.5f),
                        new PointF(17.5f, 12.3f)
                    });
                }
                using (var pen = CreatePen(Teal, 1.4f))
                {
                    graphics.DrawLine(pen, 5.2f, 6, 10.8f, 6);
                    graphics.DrawLine(pen, 5.2f, 9, 9.8f, 9);
                }
            });
        }

        private static Image DrawDelete()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Magenta, 1.7f))
                {
                    graphics.DrawLine(pen, 5, 6, 15, 6);
                    graphics.DrawLine(pen, 8, 3.5f, 12, 3.5f);
                    graphics.DrawRectangle(pen, 6, 7, 8, 10);
                    graphics.DrawLine(pen, 9, 9, 9, 15);
                    graphics.DrawLine(pen, 11, 9, 11, 15);
                }
            });
        }

        private static Image DrawClose()
        {
            return CreateBitmap(graphics =>
            {
                using (var pen = CreatePen(Dark, 2.2f))
                {
                    graphics.DrawLine(pen, 4.5f, 4.5f, 15.5f, 15.5f);
                    graphics.DrawLine(pen, 15.5f, 4.5f, 4.5f, 15.5f);
                }
            });
        }

        private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Pen CreatePen(Color color, float width)
        {
            return new Pen(color, width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
        }
    }
}
