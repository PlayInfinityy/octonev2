using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using octonev2.Config;
using octonev2.Controls;
using octonev2.Theme;
using octonev2.Managers;

namespace octonev2.Forms
{
    public partial class DashboardForm : Form
    {
        private readonly string username;
        private readonly Panel titleBar;
        private readonly ModernButton closeButton;
        private readonly ModernButton minimizeButton;
        private readonly Label welcomeLabel;
        private readonly FlowLayoutPanel productList;
        private readonly Panel productDetails;
        private readonly Label productNameLabel;
        private readonly Label durationLabel;
        private readonly ModernButton injectButton;
        private readonly ModernButton redeemKeyButton;
        private readonly Panel keyRedemptionPanel;
        private readonly ModernTextBox keyInputBox;
        private readonly ModernButton submitKeyButton;
        private readonly MySqlConnection connection;
        private bool isDragging;
        private Point dragStart;

        public DashboardForm(string username)
        {
            InitializeComponent();
            this.username = username;
            connection = new MySqlConnection(DatabaseConfig.GetConnectionString());

            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = ThemeColors.Background;
            DoubleBuffered = true;

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

            welcomeLabel = new Label
            {
                Text = $"Welcome, {username}!",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = ThemeColors.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 50)
            };

            productList = new FlowLayoutPanel
            {
                Size = new Size(300, Height - 200),
                Location = new Point(20, 100),
                BackColor = ThemeColors.Surface,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(15),
                Margin = new Padding(0, 0, 0, 10)
            };

            productDetails = new Panel
            {
                Size = new Size(800, Height - 100),
                Location = new Point(340, 100),
                BackColor = ThemeColors.Surface,
                Visible = false
            };

            keyRedemptionPanel = new Panel
            {
                Size = new Size(800, Height - 100),
                Location = new Point(340, 100),
                BackColor = ThemeColors.Surface,
                Visible = false
            };

            productNameLabel = new Label
            {
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = ThemeColors.TextPrimary,
                Location = new Point(20, 20),
                AutoSize = true
            };

            durationLabel = new Label
            {
                Font = new Font("Segoe UI", 12f),
                ForeColor = ThemeColors.TextSecondary,
                Location = new Point(20, 60),
                AutoSize = true
            };

            injectButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "INJECT",
                Size = new Size(200, 45),
                Location = new Point(20, 100)
            };

            redeemKeyButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "REDEEM NEW KEY",
                Size = new Size(280, 45),
                Location = new Point(30, Height - 90),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            keyInputBox = new ModernTextBox
            {
                PlaceholderText = "Enter your license key",
                Size = new Size(400, 45),
                Location = new Point(20, 20)
            };

            submitKeyButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "ACTIVATE KEY",
                Size = new Size(200, 45),
                Location = new Point(20, 80)
            };

            LoadUserProducts();
            SetupControls();
            SetupEventHandlers();
        }

        private async Task LoadUserProducts()
        {
            try
            {
                await connection.OpenAsync();
                string query = @"
            SELECT 
                p.name as product_name,
                kr.expires_at,
                p.id as product_id
            FROM users u
            JOIN key_redemptions kr ON u.id = kr.user_id
            JOIN products p ON kr.product_id = p.id
            WHERE u.username = @username
            AND kr.is_active = TRUE";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var productButton = new ModernButton
                    {
                        Text = reader.GetString("product_name"),
                        Size = new Size(280, 45),
                        Tag = new
                        {
                            ProductId = reader.GetInt32("product_id"),
                            ExpiresAt = reader.GetDateTime("expires_at")
                        }
                    };

                    productButton.Click += ProductButton_Click;
                    productList.Controls.Add(productButton);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }



        private void ProductButton_Click(object sender, EventArgs e)
        {
            var button = (ModernButton)sender;
            var data = button.Tag;
            var productName = button.Text;
            var expiresAt = ((dynamic)data).ExpiresAt;

            productNameLabel.Text = productName;
            durationLabel.Text = $"Expires: {expiresAt:MM/dd/yyyy HH:mm:ss}";
            productDetails.Visible = true;
            keyRedemptionPanel.Visible = false;

            foreach (ModernButton productBtn in productList.Controls)
            {
                productBtn.BackColor = ThemeColors.Surface;
            }
            button.BackColor = ThemeColors.Accent;
        }

        private void SetupControls()
        {
            titleBar.Controls.AddRange(new Control[] { closeButton, minimizeButton });
            productDetails.Controls.AddRange(new Control[] {
                productNameLabel,
                durationLabel,
                injectButton
            });
            keyRedemptionPanel.Controls.AddRange(new Control[] {
                keyInputBox,
                submitKeyButton
            });
            Controls.AddRange(new Control[] {
                titleBar,
                welcomeLabel,
                productList,
                productDetails,
                keyRedemptionPanel,
                redeemKeyButton
            });
        }

        private void SetupEventHandlers()
        {
            closeButton.Click += (s, e) => Application.Exit();
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MouseDown += (s, e) => StartDragging(e);
            titleBar.MouseMove += (s, e) => HandleDragging(e);
            titleBar.MouseUp += (s, e) => StopDragging();
            Load += async (s, e) => await AnimationManager.PlayFadeInAnimation(this);
            injectButton.Click += (s, e) => MessageBox.Show("Injection feature not added yet uwu", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            redeemKeyButton.Click += (s, e) =>
            {
                productDetails.Visible = false;
                keyRedemptionPanel.Visible = true;
            };
            submitKeyButton.Click += async (s, e) =>
            {
                await ValidateAndRedeemKey(keyInputBox.Text);
                keyRedemptionPanel.Visible = false;
                productList.Controls.Clear();
                await LoadUserProducts();
            };
        }

        private async Task ValidateAndRedeemKey(string keyValue)
        {
            try
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO redeemed_keys (user_id, key_value, expires_at)
                    SELECT 
                        (SELECT id FROM users WHERE username = @username),
                        ak.key_value,
                        DATE_ADD(NOW(), INTERVAL ak.duration_days DAY)
                    FROM available_keys ak
                    WHERE ak.key_value = @keyValue
                    AND NOT EXISTS (
                        SELECT 1 FROM redeemed_keys 
                        WHERE key_value = ak.key_value
                    )";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@keyValue", keyValue);

                int result = await cmd.ExecuteNonQueryAsync();
                if (result > 0)
                {
                    MessageBox.Show("Key successfully redeemed!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Invalid or already used key.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error redeeming key: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
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
