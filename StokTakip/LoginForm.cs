using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StokTakip
{
    public partial class LoginForm : Form
    {
        private SQLiteConnection? baglanti;
        private TextBox txtSifre;
        private Button btnGiris;

        public LoginForm()
        {
            VeritabaniBaglantisiOlustur();
            FormTasarimOlustur();
        }

        private void VeritabaniBaglantisiOlustur()
        {
            try
            {
                string connectionString = $"Data Source={Program.DbPath};Version=3;";
                baglanti = new SQLiteConnection(connectionString);
                baglanti.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı bağlantısı oluşturulurken hata: {ex.Message}", "Bağlantı Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormTasarimOlustur()
        {
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Icon = new Icon(Path.Combine(Application.StartupPath, "logo.ico"));

            Panel pnlLogin = new Panel
            {
                Size = new Size(350, 300),
                Location = new Point(25, 25),
                BackColor = Color.FromArgb(23, 23, 23)
            };

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
                Image = Image.FromFile(Path.Combine(Application.StartupPath, "logo.png"))
            };

            Label lblFirmaAdi = new Label
            {
                Text = "AsilhanKocJeans",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(90, 35),
                AutoSize = true
            };

            pnlLogoBaslik.Controls.AddRange(new Control[] { picLogo, lblFirmaAdi });

            Label lblHosgeldin = new Label
            {
                Text = "Merhaba Asilhan Bey",
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 12),
                Location = new Point(0, 120),
                Size = new Size(350, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            txtSifre = new TextBox
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
            txtSifre.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { GirisYap(txtSifre.Text); } };

            btnGiris = new Button
            {
                Text = "GİRİŞ YAP",
                Size = new Size(300, 45),
                Location = new Point(25, 210),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 151, 230),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btnGiris.FlatAppearance.BorderSize = 0;
            btnGiris.Click += BtnGiris_Click;

            Button btnSifreDegistir = new Button
            {
                Text = "ŞİFRE DEĞİŞTİR",
                Size = new Size(145, 35),
                Location = new Point(25, 255),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(23, 23, 23),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 11)
            };
            btnSifreDegistir.FlatAppearance.BorderColor = Color.DimGray;
            btnSifreDegistir.Click += BtnSifreDegistir_Click;

            Button btnCikis = new Button
            {
                Text = "ÇIKIŞ",
                Size = new Size(145, 35),
                Location = new Point(180, 255),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(23, 23, 23),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 11)
            };
            btnCikis.FlatAppearance.BorderColor = Color.DimGray;
            btnCikis.Click += (s, e) => {
                if (MessageBox.Show("Uygulamadan çıkmak istediğinizden emin misiniz?", "Çıkış",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.Close();
                }
            };

            pnlLogin.Controls.AddRange(new Control[] { 
                pnlLogoBaslik, lblHosgeldin, txtSifre, 
                btnGiris, btnSifreDegistir, btnCikis 
            });
            this.Controls.Add(pnlLogin);

            this.MouseDown += (s, e) => { 
                if (e.Button == MouseButtons.Left) { 
                    this.Capture = false; 
                    Message m = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero); 
                    this.WndProc(ref m); 
                } 
            };
        }

        private void BtnGiris_Click(object sender, EventArgs e)
        {
            GirisYap(txtSifre.Text);
        }

        private void BtnSifreDegistir_Click(object sender, EventArgs e)
        {
            using (var sifreDegistirForm = new SifreDegistirForm())
            {
                sifreDegistirForm.ShowDialog();
            }
        }

        private string SifreyiHashle(string sifre)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(sifre);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private void GirisYap(string sifre)
        {
            if (baglanti == null)
            {
                MessageBox.Show("Veritabanı bağlantısı kurulamadı!", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string sorgu = "SELECT COUNT(*) FROM Sifre WHERE Sifre = @Sifre";
                using (var cmd = new SQLiteCommand(sorgu, baglanti))
                {
                    cmd.Parameters.AddWithValue("@Sifre", sifre);
                    int sonuc = Convert.ToInt32(cmd.ExecuteScalar());

                    if (sonuc > 0)
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Şifre yanlış! Varsayılan şifre: admin", "Hata", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtSifre.Text = "";
                        txtSifre.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş yapılırken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (baglanti != null && baglanti.State == System.Data.ConnectionState.Open)
            {
                baglanti.Close();
                baglanti.Dispose();
            }
        }
    }
}