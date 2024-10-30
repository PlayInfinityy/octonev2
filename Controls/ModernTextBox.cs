using System;
using System.Drawing;
using System.Windows.Forms;
using octonev2.Theme;

namespace octonev2.Controls
{
    public class ModernTextBox : TextBox
    {
        private string _placeholderText = "";

        public new string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value;
                if (string.IsNullOrEmpty(Text))
                    Text = value;
            }
        }

        public ModernTextBox()
        {
            BorderStyle = BorderStyle.None;
            BackColor = ThemeColors.Surface;
            ForeColor = ThemeColors.TextPrimary;
            Font = new Font("Segoe UI", 10f);
            Padding = new Padding(10);

            GotFocus += OnFocusReceived;
            LostFocus += OnFocusLost;
        }

        private void OnFocusReceived(object? sender, EventArgs e)
        {
            if (Text == _placeholderText)
                Text = "";
        }

        private void OnFocusLost(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
                Text = _placeholderText;
        }
    }
}
