using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using octonev2.Theme;

namespace octonev2.Controls
{
    public class ModernButton : Button
    {
        private readonly Color buttonColor;
        private bool isHovered = false;
        private bool isPressed = false;

        public ModernButton(Color? customColor = null)
        {
            buttonColor = customColor ?? ThemeColors.Surface;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = buttonColor;
            ForeColor = ThemeColors.TextPrimary;
            Font = new Font("Segoe UI", 10f);
            Cursor = Cursors.Hand;
            Size = new Size(150, 40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 5;

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Region = new Region(path);

            using var brush = new SolidBrush(
                isPressed ? Color.FromArgb(200, buttonColor) :
                isHovered ? Color.FromArgb(230, buttonColor) :
                buttonColor);
            e.Graphics.FillPath(brush, path);

            using var textBrush = new SolidBrush(ForeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString(Text, Font, textBrush, ClientRectangle, format);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            isPressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }
    }
}
