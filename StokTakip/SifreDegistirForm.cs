using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;

namespace StokTakip
{
    public partial class SifreDegistirForm : Form
    {
        private TextBox txtEskiSifre;
        private TextBox txtYeniSifre;
        private TextBox txtYeniSifreTekrar;
        private Button btnDegistir;
        private Button btnIptal;

        public SifreDegistirForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Şifre Değiştir";
            this.Size = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(18, 18, 18);

            Panel pnlMain = new Panel
            {
                Size = new Size(350, 350),
                Location = new Point(25, 25),
                BackColor = Color.FromArgb(23, 23, 23)
            };

            // Logo ve Başlık
            Panel pnlLogoBaslik = new Panel
            {
                Size = new Size(350, 100),
                Location = new Point(0, 10),
                BackColor = Color.Transparent
            };

            PictureBox picLogo = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Image.FromFile(System.IO.Path.Combine(Application.StartupPath, "logo.png"))
            };

            Label lblBaslik = new Label
            {
                Text = "AsilhanKocJeans",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(90, 35),
                AutoSize = true
            };

            pnlLogoBaslik.Controls.AddRange(new Control[] { picLogo, lblBaslik });

            // Alt Başlık
            Label lblAltBaslik = new Label
            {
                Text = "Şifre Değiştirme",
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 12),
                Location = new Point(0, 120),
                Size = new Size(350, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Eski Şifre
            txtEskiSifre = new TextBox
            {
                Size = new Size(300, 40),
                Location = new Point(25, 160),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(28, 28, 28),
                ForeColor = Color.Silver,
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                TextAlign = HorizontalAlignment.Center
            };
            txtEskiSifre.PlaceholderText = "Mevcut Şifre";

            // Yeni Şifre
            txtYeniSifre = new TextBox
            {
                Size = new Size(300, 40),
                Location = new Point(25, 210),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(28, 28, 28),
                ForeColor = Color.Silver,
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                TextAlign = HorizontalAlignment.Center
            };
            txtYeniSifre.PlaceholderText = "Yeni Şifre";

            // Yeni Şifre Tekrar
            txtYeniSifreTekrar = new TextBox
            {
                Size = new Size(300, 40),
                Location = new Point(25, 260),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(28, 28, 28),
                ForeColor = Color.Silver,
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                TextAlign = HorizontalAlignment.Center
            };
            txtYeniSifreTekrar.PlaceholderText = "Yeni Şifre (Tekrar)";

            // Butonlar
            btnDegistir = new Button
            {
                Text = "ŞİFRE DEĞİŞTİR",
                Size = new Size(145, 35),
                Location = new Point(25, 310),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 151, 230),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            btnDegistir.FlatAppearance.BorderSize = 0;
            btnDegistir.Click += BtnDegistir_Click;

            btnIptal = new Button
            {
                Text = "ÇIKIŞ",
                Size = new Size(145, 35),
                Location = new Point(180, 310),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(23, 23, 23),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 11)
            };
            btnIptal.FlatAppearance.BorderColor = Color.DimGray;
            btnIptal.Click += (s, e) => this.Close();

            // Hover Efektleri
            btnDegistir.MouseEnter += (s, e) => btnDegistir.BackColor = Color.FromArgb(0, 130, 200);
            btnDegistir.MouseLeave += (s, e) => btnDegistir.BackColor = Color.FromArgb(0, 151, 230);
            
            btnIptal.MouseEnter += (s, e) => {
                btnIptal.BackColor = Color.FromArgb(35, 35, 35);
                btnIptal.ForeColor = Color.White;
            };
            btnIptal.MouseLeave += (s, e) => {
                btnIptal.BackColor = Color.FromArgb(23, 23, 23);
                btnIptal.ForeColor = Color.Silver;
            };

            // Enter tuşu ile giriş yapma
            this.AcceptButton = btnDegistir;
            this.CancelButton = btnIptal;

            // Kontrolleri panele ekle
            pnlMain.Controls.AddRange(new Control[] {
                pnlLogoBaslik,
                lblAltBaslik,
                txtEskiSifre,
                txtYeniSifre,
                txtYeniSifreTekrar,
                btnDegistir,
                btnIptal
            });

            // Form sürükleme özelliği
            this.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    this.Capture = false;
                    Message m = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
                    this.WndProc(ref m);
                }
            };

            this.Controls.Add(pnlMain);
        }

        private void BtnDegistir_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEskiSifre.Text) || 
                string.IsNullOrWhiteSpace(txtYeniSifre.Text) || 
                string.IsNullOrWhiteSpace(txtYeniSifreTekrar.Text))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtYeniSifre.Text != txtYeniSifreTekrar.Text)
            {
                MessageBox.Show("Yeni şifreler eşleşmiyor.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string mevcutSifre = null;
                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    using var cmd = new SQLiteCommand("SELECT Sifre FROM Sifre WHERE ID = 1", connection, transaction);
                    mevcutSifre = cmd.ExecuteScalar()?.ToString();
                });

                if (txtEskiSifre.Text != mevcutSifre)
                {
                    MessageBox.Show("Mevcut şifre yanlış.", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    // Önce mevcut şifreyi tekrar kontrol et
                    using (var cmdCheck = new SQLiteCommand("SELECT Sifre FROM Sifre WHERE ID = 1", connection, transaction))
                    {
                        var currentPassword = cmdCheck.ExecuteScalar()?.ToString();
                        if (currentPassword != txtEskiSifre.Text)
                        {
                            throw new Exception("Mevcut şifre değiştirilmiş. Lütfen tekrar deneyin.");
                        }
                    }

                    // Şifreyi güncelle
                    using (var cmdUpdate = new SQLiteCommand(@"
                        UPDATE Sifre 
                        SET Sifre = @yeniSifre 
                        WHERE ID = 1;", connection, transaction))
                    {
                        cmdUpdate.Parameters.AddWithValue("@yeniSifre", txtYeniSifre.Text);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    // Değişikliği kontrol et
                    using (var cmdVerify = new SQLiteCommand("SELECT Sifre FROM Sifre WHERE ID = 1", connection, transaction))
                    {
                        var updatedPassword = cmdVerify.ExecuteScalar()?.ToString();
                        if (updatedPassword != txtYeniSifre.Text)
                        {
                            throw new Exception("Şifre güncellenemedi. Lütfen tekrar deneyin.");
                        }
                    }
                });

                MessageBox.Show("Şifre başarıyla değiştirildi.", "Bilgi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifre değiştirilirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
