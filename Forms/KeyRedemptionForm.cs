 using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using octonev2.Config;
using octonev2.Controls;
using octonev2.Managers;
using octonev2.Theme;

namespace octonev2.Forms
{
    public partial class KeyRedemptionForm : Form
    {
        private readonly Panel mainPanel;
        private readonly ModernTextBox keyBox;
        private readonly ModernButton redeemButton;
        private readonly ModernButton cancelButton;
        private readonly ModernButton closeButton;
        private readonly ModernButton minimizeButton;
        private readonly Panel titleBar;
        private readonly MySqlConnection connection;
        private readonly string username;
        private bool isDragging;
        private Point dragStart;

        public KeyRedemptionForm(string username)
        {
            this.username = username;

            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = ThemeColors.Background;
            DoubleBuffered = true;

            connection = new MySqlConnection(DatabaseConfig.GetConnectionString());

            titleBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = ThemeColors.SurfaceLight
            };

            closeButton = new ModernButton
            {
                Text = "×",
                Size = new Size(30, 30),
                Location = new Point(Width - 30, 0),
                ForeColor = ThemeColors.TextPrimary,
                BackColor = Color.Transparent
            };

            minimizeButton = new ModernButton
            {
                Text = "−",
                Size = new Size(30, 30),
                Location = new Point(Width - 60, 0),
                ForeColor = ThemeColors.TextPrimary,
                BackColor = Color.Transparent
            };

            mainPanel = new Panel
            {
                Size = new Size(360, 240),
                Location = new Point(20, 40),
                BackColor = ThemeColors.SurfaceLight
            };

            keyBox = new ModernTextBox
            {
                PlaceholderText = "Enter License Key",
                Location = new Point(30, 70),
                Size = new Size(300, 45)
            };

            redeemButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "REDEEM",
                Location = new Point(30, 130),
                Size = new Size(145, 45)
            };

            cancelButton = new ModernButton
            {
                Text = "CANCEL",
                Location = new Point(185, 130),
                Size = new Size(145, 45)
            };

            SetupControls();
            SetupEventHandlers();
        }

        private void SetupControls()
        {
            titleBar.Controls.AddRange(new Control[] { closeButton, minimizeButton });
            mainPanel.Controls.AddRange(new Control[] {
                keyBox,
                redeemButton,
                cancelButton
            });
            Controls.AddRange(new Control[] { titleBar, mainPanel });
        }

        private void SetupEventHandlers()
        {
            redeemButton.Click += async (s, e) => await HandleKeyRedemption();
            cancelButton.Click += (s, e) => Application.Exit();
            closeButton.Click += (s, e) => Application.Exit();
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MouseDown += (s, e) => StartDragging(e);
            titleBar.MouseMove += (s, e) => HandleDragging(e);
            titleBar.MouseUp += (s, e) => StopDragging();
            Load += async (s, e) => await AnimationManager.PlayFadeInAnimation(this);
        }

        private async Task HandleKeyRedemption()
        {
            if (string.IsNullOrEmpty(keyBox.Text))
            {
                ShowError("Please enter a license key.");
                return;
            }

            try
            {
                await connection.OpenAsync();
                if (await ValidateAndRedeemKey(keyBox.Text))
                {
                    MessageBox.Show("Key redeemed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await AnimationManager.PlayFadeOutAnimation(this);
                    Hide();
                    new DashboardForm(username).Show();
                }
                else
                {
                    ShowError("Invalid or already redeemed key.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Key redemption failed: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        private async Task<bool> ValidateAndRedeemKey(string keyValue)
        {
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Check if key exists and is available
                string checkQuery = "SELECT id, duration_days FROM available_keys WHERE key_value = @key";
                int keyId;
                int durationDays;

                using (var cmd = new MySqlCommand(checkQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@key", keyValue);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return false;

                    keyId = reader.GetInt32("id");
                    durationDays = reader.GetInt32("duration_days");
                }

                // Get user ID
                string userQuery = "SELECT id FROM users WHERE username = @username";
                int userId;

                using (var cmd = new MySqlCommand(userQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return false;

                    userId = reader.GetInt32("id");
                }

                // Move key to redeemed_keys
                string redeemQuery = @"
                    INSERT INTO redeemed_keys (key_value, user_id, expires_at) 
                    VALUES (@key, @userId, DATE_ADD(NOW(), INTERVAL @days DAY))";

                using (var cmd = new MySqlCommand(redeemQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@key", keyValue);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@days", durationDays);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Remove from available_keys
                string deleteQuery = "DELETE FROM available_keys WHERE id = @keyId";
                using (var cmd = new MySqlCommand(deleteQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@keyId", keyId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Key Redemption Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void StartDragging(MouseEventArgs e)
        {
            isDragging = true;
            dragStart = new Point(e.X, e.Y);
        }

        private void HandleDragging(MouseEventArgs e)
        {
            if (!isDragging) return;
            var p = PointToScreen(e.Location);
            Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
        }

        private void StopDragging()
        {
            isDragging = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 10;

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Region = new Region(path);
        }
    }
}
