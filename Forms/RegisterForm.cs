using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using octonev2.Config;
using octonev2.Controls;
using octonev2.Managers;
using octonev2.Theme;
using System.Security.Cryptography;
using System.Text;
using System.Data;

namespace octonev2.Forms
{
    public partial class RegisterForm : Form
    {
        private readonly Panel mainPanel;
        private readonly PictureBox logoPictureBox;
        private readonly ModernTextBox usernameBox;
        private readonly ModernTextBox passwordBox;
        private readonly ModernTextBox confirmPasswordBox;
        private readonly ModernTextBox licenseKeyBox;
        private readonly ModernButton registerButton;
        private readonly ModernButton backButton;
        private readonly ModernButton closeButton;
        private readonly ModernButton minimizeButton;
        private readonly Panel titleBar;
        private readonly MySqlConnection connection;
        private bool isDragging;
        private Point dragStart;

        public RegisterForm()
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
                Size = new Size(100, 100),
                Location = new Point((mainPanel.Width - 100) / 2, 20),
                BackColor = Color.Transparent
            };

            usernameBox = new ModernTextBox
            {
                PlaceholderText = "Username",
                Location = new Point(50, 140),
                Size = new Size(300, 45)
            };

            passwordBox = new ModernTextBox
            {
                PlaceholderText = "Password",
                UseSystemPasswordChar = true,
                Location = new Point(50, 190),
                Size = new Size(300, 45)
            };

            confirmPasswordBox = new ModernTextBox
            {
                PlaceholderText = "Confirm Password",
                UseSystemPasswordChar = true,
                Location = new Point(50, 240),
                Size = new Size(300, 45)
            };

            licenseKeyBox = new ModernTextBox
            {
                PlaceholderText = "License Key",
                Location = new Point(50, 290),
                Size = new Size(300, 45)
            };

            registerButton = new ModernButton(ThemeColors.Accent)
            {
                Text = "REGISTER",
                Location = new Point(50, 340),
                Size = new Size(300, 45)
            };

            backButton = new ModernButton
            {
                Text = "BACK TO LOGIN",
                Location = new Point(50, 390),
                Size = new Size(300, 45)
            };

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
                confirmPasswordBox,
                licenseKeyBox,
                registerButton,
                backButton
            });
            Controls.AddRange(new Control[] { titleBar, mainPanel });
        }

        private void SetupEventHandlers()
        {
            registerButton.Click += async (s, e) => await HandleRegistration();
            backButton.Click += async (s, e) => await ShowLoginForm();
            closeButton.Click += (s, e) => Application.Exit();
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            titleBar.MouseDown += (s, e) => StartDragging(e);
            titleBar.MouseMove += (s, e) => HandleDragging(e);
            titleBar.MouseUp += (s, e) => StopDragging();
            Load += async (s, e) => await AnimationManager.PlayFadeInAnimation(this);
        }

        private async Task<bool> ValidateLicenseKey(string licenseKey)
        {
            string query = @"
        SELECT * FROM available_keys 
        WHERE key_value = @key";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@key", licenseKey);

            // Log the exact query being executed
            MessageBox.Show($"Executing query: {cmd.CommandText}\nWith key value: {licenseKey}", "Debug");

            using var reader = await cmd.ExecuteReaderAsync();
            var isValid = await reader.ReadAsync();

            // Log the result
            if (isValid)
            {
                string keyValue = reader.GetString("key_value");
                MessageBox.Show($"Found key in database: {keyValue}", "Debug");
            }
            else
            {
                MessageBox.Show("No key found in database", "Debug");
            }

            return isValid;
        }

        private async Task HandleRegistration()
        {
            if (!ValidateInputs(out string errorMessage))
            {
                ShowError(errorMessage);
                return;
            }

            try
            {
                await connection.OpenAsync();
                if (await ValidateLicenseKey(licenseKeyBox.Text))
                {
                    await RegisterUser();
                    await AnimationManager.PlayFadeOutAnimation(this);
                    Hide();
                    new LoginForm().Show();
                }
                else
                {
                    ShowError("Invalid or expired license key.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Registration failed: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        private bool ValidateInputs(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(usernameBox.Text) ||
                string.IsNullOrEmpty(passwordBox.Text) ||
                string.IsNullOrEmpty(confirmPasswordBox.Text) ||
                string.IsNullOrEmpty(licenseKeyBox.Text))
            {
                errorMessage = "All fields are required.";
                return false;
            }

            if (passwordBox.Text != confirmPasswordBox.Text)
            {
                errorMessage = "Passwords do not match.";
                return false;
            }

            return true;
        }

        private async Task RegisterUser()
        {
            string hashedPassword = HashPassword(passwordBox.Text);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                string insertUserQuery = @"
            INSERT INTO users (username, password_hash) 
            VALUES (@username, @password);
            SELECT LAST_INSERT_ID();";

                long userId;
                using (var cmd = new MySqlCommand(insertUserQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@username", usernameBox.Text);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);
                    userId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                string redeemKeyQuery = @"
            INSERT INTO key_redemptions (user_id, key_value, product_id, expires_at)
            SELECT 
                @userId,
                ak.key_value,
                ak.product_id,
                DATE_ADD(CURRENT_TIMESTAMP, INTERVAL ak.duration_days DAY)
            FROM available_keys ak
            WHERE ak.key_value = @key;

            UPDATE available_keys 
            SET is_used = TRUE 
            WHERE key_value = @key;";

                using (var cmd = new MySqlCommand(redeemKeyQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@key", licenseKeyBox.Text);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                MessageBox.Show("Registration successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        private async Task ShowLoginForm()
        {
            await AnimationManager.PlayFadeOutAnimation(this);
            Hide();
            new LoginForm().Show();
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Registration Error",
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
