using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace StokTakip
{
    public partial class MainForm : Form
    {
        private Panel? pnlIcerik;
        private readonly Sepet aktifSepet;
        private SepetForm? sepetForm;
        private DataGridView dgvBorclar;

        public MainForm()
        {
            InitializeComponent();
            aktifSepet = new Sepet();
            FormTasarimOlustur();

            // Form yüklendiğinde başlık çubuğunu siyah yap
            this.HandleCreated += (s, e) => 
            {
                DarkModeHelper.UseImmersiveDarkMode(this.Handle, true);
            };
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Text = "AsilhanKocJeans - Stok Takip";
            this.WindowState = FormWindowState.Maximized;
            this.DoubleBuffered = true;
            string iconPath = Path.Combine(Application.StartupPath, "logo.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
        }

        private void FormTasarimOlustur()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);

            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            // Sol Panel (Menü)
            Panel pnlMenu = new Panel
            {
                Width = 250,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(24, 24, 24),
                Padding = new Padding(10)
            };

            // Logo/Başlık
            Panel pnlLogoBaslik = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.Transparent,
                Padding = new Padding(10)
            };

            PictureBox picLogo = new PictureBox
            {
                Size = new Size(70, 70),
                Location = new Point(85, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Image.FromFile(Path.Combine(Application.StartupPath, "logo.png"))
            };

            Label lblLogo = new Label
            {
                Text = "AsilhanKocJeans",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(10, 95),
                Size = new Size(230, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlLogoBaslik.Controls.AddRange(new Control[] { picLogo, lblLogo });

            // Menü Butonları
            var menuButtons = new[]
            {
                CreateMenuButton("📦 ÜRÜNLER", "Urunler"),
                CreateMenuButton("📑 KATEGORİLER", "Kategoriler"),
                CreateMenuButton("🛒 SEPET", "Sepet"),
                CreateMenuButton("💵 BORÇ TAKİP", "BorcTakip"),
                CreateMenuButton("📊 RAPORLAR", "Raporlar"),
                CreateMenuButton("💾 YEDEKLEME", "Yedekleme")
            };

            Button btnCikis = new Button
            {
                Text = "🚪 ÇIKIŞ",
                Dock = DockStyle.Bottom,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnCikis.FlatAppearance.BorderSize = 0;
            btnCikis.Click += BtnCikis_Click;

            // Butonlar arası boşluk ve hover efektleri
            foreach (var btn in menuButtons)
            {
                btn.Margin = new Padding(0, 0, 0, 5);
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(45, 45, 45);
                btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(30, 30, 30);
            }

            // Çıkış butonu hover efekti
            btnCikis.MouseEnter += (s, e) => btnCikis.BackColor = Color.FromArgb(200, 40, 40);
            btnCikis.MouseLeave += (s, e) => btnCikis.BackColor = Color.FromArgb(180, 30, 30);

            // İçerik Panel
            pnlIcerik = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20)
            };

            // Kontrolleri panellere ekle
            pnlMenu.Controls.Add(btnCikis);
            foreach (var btn in menuButtons.Reverse())
            {
                pnlMenu.Controls.Add(btn);
            }
            pnlMenu.Controls.Add(pnlLogoBaslik);

            pnlAna.Controls.Add(pnlIcerik);
            pnlAna.Controls.Add(pnlMenu);
            this.Controls.Add(pnlAna);

            // Varsayılan olarak ürünler sayfasını göster
            FormGoster(new UrunListeForm());
        }

        private Button CreateMenuButton(string text, string tag)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = tag
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += MenuButon_Click;
            return btn;
        }

        private void MenuButon_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    string sayfa = btn.Tag.ToString() ?? "";
                    Form? yeniForm = null;

                    switch (sayfa)
                    {
                        case "Urunler":
                            yeniForm = new UrunListeForm();
                            break;
                        case "Kategoriler":
                            yeniForm = new KategoriYonetimForm();
                            break;
                        case "Sepet":
                            if (sepetForm == null || sepetForm.IsDisposed)
                            {
                                sepetForm = new SepetForm(aktifSepet);
                            }
                            yeniForm = sepetForm;
                            break;
                        case "BorcTakip":
                            yeniForm = new BorcTakipForm();
                            break;
                        case "Raporlar":
                            yeniForm = new RaporlarForm();
                            break;
                        case "Yedekleme":
                            yeniForm = new VeriTabaniYedeklemeForm();
                            break;
                    }

                    if (yeniForm != null)
                    {
                        FormGoster(yeniForm);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Sayfa gösterilirken hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public async void FormGoster(Form form)
        {
            if (pnlIcerik == null) return;

            try
            {
                pnlIcerik.SuspendLayout();

                // Mevcut formu kapat
                if (pnlIcerik.Controls.Count > 0)
                {
                    var currentForm = pnlIcerik.Controls[0] as Form;
                    if (currentForm != null)
                    {
                        currentForm.Hide();
                        pnlIcerik.Controls.Clear();
                        currentForm.Dispose();
                    }
                }

                // Yeni formu ayarla
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;

                pnlIcerik.Controls.Add(form);
                
                // Form yüklenmeden önce kısa bir gecikme ekle
                await Task.Delay(100);
                
                form.Show();
                
                // Form gösterildikten sonra refresh çağır
                if (form is UrunListeForm urunListeForm)
                {
                    await Task.Delay(100);
                    urunListeForm.RefreshList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form gösterilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                pnlIcerik.ResumeLayout();
            }
        }

        public void UrunDuzenle(int urunId)
        {
            try
            {
                using (var urunEkleForm = new UrunEkleForm(urunId))
                {
                    urunEkleForm.StartPosition = FormStartPosition.CenterScreen;
                    if (urunEkleForm.ShowDialog() == DialogResult.OK)
                    {
                        YenileUrunListesi();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün düzenleme sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void YeniUrunEkle()
        {
            try
            {
                using (var urunEkleForm = new UrunEkleForm())
                {
                    urunEkleForm.StartPosition = FormStartPosition.CenterScreen;
                    if (urunEkleForm.ShowDialog() == DialogResult.OK)
                    {
                        FormGoster(new UrunListeForm());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yeni ürün ekleme sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SepeteUrunEkle(int urunId, string urunAdi, int miktar, decimal birimFiyat)
        {
            try
            {
                aktifSepet.UrunEkle(urunId, urunAdi, miktar, birimFiyat);
                MessageBox.Show($"{urunAdi} ürününden {miktar} adet sepete eklendi.", "Başarılı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sepete ürün eklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCikis_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    if (MessageBox.Show("Uygulamadan çıkmak istediğinizden emin misiniz?", "Çıkış",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // İçerik panelini temizle
                if (pnlIcerik != null)
                {
                    Control[] controls = new Control[pnlIcerik.Controls.Count];
                    pnlIcerik.Controls.CopyTo(controls, 0);
                    
                    foreach (Control ctrl in controls)
                    {
                        if (ctrl is Form frm && !frm.IsDisposed)
                        {
                            frm.Hide();
                            frm.Close();
                            frm.Dispose();
                        }
                    }
                    pnlIcerik.Controls.Clear();
                }

                // Sepet formunu temizle
                if (sepetForm != null && !sepetForm.IsDisposed)
                {
                    sepetForm.Hide();
                    sepetForm.Close();
                    sepetForm.Dispose();
                }

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form kapatılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void YenileUrunListesi()
        {
            try
            {
                if (pnlIcerik?.Controls.Count > 0)
                {
                    var currentForm = pnlIcerik.Controls[0] as UrunListeForm;
                    if (currentForm != null && !currentForm.IsDisposed)
                    {
                        currentForm.RefreshList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün listesi yenilenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUrunListele_Click(object sender, EventArgs e)
        {
            using (var urunListeForm = new UrunListeForm())
            {
                urunListeForm.ShowDialog();
            }
        }

        private void btnBorcTakip_Click(object sender, EventArgs e)
        {
            try
            {
                using (var borcForm = new Form())
                {
                    borcForm.Text = "Borç Takip";
                    borcForm.Size = new Size(800, 600);
                    borcForm.StartPosition = FormStartPosition.CenterParent;
                    borcForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    borcForm.MaximizeBox = false;
                    borcForm.MinimizeBox = false;
                    borcForm.BackColor = Color.FromArgb(30, 30, 30);

                    dgvBorclar = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        BackgroundColor = Color.FromArgb(45, 45, 45),
                        ForeColor = Color.White,
                        GridColor = Color.FromArgb(60, 60, 60),
                        BorderStyle = BorderStyle.None,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        ReadOnly = true,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        MultiSelect = false,
                        RowHeadersVisible = false
                    };

                    dgvBorclar.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
                    dgvBorclar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
                    dgvBorclar.DefaultCellStyle.SelectionForeColor = Color.White;
                    dgvBorclar.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                    dgvBorclar.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    dgvBorclar.EnableHeadersVisualStyles = false;

                    BorclariListele();

                    Panel pnlBorclar = new Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(30, 30, 30),
                        Padding = new Padding(20)
                    };

                    pnlBorclar.Controls.Add(dgvBorclar);
                    borcForm.Controls.Add(pnlBorclar);
                    borcForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borçlar formu açılırken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BorclariListele()
        {
            try
            {
                var dt = DatabaseManager.ExecuteQuery(@"
                    SELECT 
                        BorcId,
                        MusteriAdi,
                        BorcMiktari,
                        OdenenMiktar,
                        ROUND(BorcMiktari - OdenenMiktar, 2) as KalanBorc,
                        Aciklama,
                        BorcTarihi,
                        Durumu,
                        OdemeTarihi
                    FROM Borclar
                    ORDER BY BorcTarihi DESC");

                dgvBorclar.DataSource = dt;

                if (dgvBorclar.Columns["BorcId"] != null) dgvBorclar.Columns["BorcId"].Visible = false;
                if (dgvBorclar.Columns["MusteriAdi"] != null) dgvBorclar.Columns["MusteriAdi"].HeaderText = "Müşteri Adı";
                if (dgvBorclar.Columns["BorcMiktari"] != null) dgvBorclar.Columns["BorcMiktari"].HeaderText = "Borç Miktarı";
                if (dgvBorclar.Columns["OdenenMiktar"] != null) dgvBorclar.Columns["OdenenMiktar"].HeaderText = "Ödenen Miktar";
                if (dgvBorclar.Columns["KalanBorc"] != null) dgvBorclar.Columns["KalanBorc"].HeaderText = "Kalan Borç";
                if (dgvBorclar.Columns["Aciklama"] != null) dgvBorclar.Columns["Aciklama"].HeaderText = "Açıklama";
                if (dgvBorclar.Columns["BorcTarihi"] != null) dgvBorclar.Columns["BorcTarihi"].HeaderText = "Borç Tarihi";
                if (dgvBorclar.Columns["Durumu"] != null) dgvBorclar.Columns["Durumu"].HeaderText = "Durumu";
                if (dgvBorclar.Columns["OdemeTarihi"] != null) dgvBorclar.Columns["OdemeTarihi"].HeaderText = "Ödeme Tarihi";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borçlar listelenirken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBorcEkle_Click(object sender, EventArgs e)
        {
            try
            {
                using (var borcForm = new Form())
                {
                    borcForm.Text = "Yeni Borç Ekle";
                    borcForm.Size = new Size(400, 350);
                    borcForm.StartPosition = FormStartPosition.CenterParent;
                    borcForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    borcForm.MaximizeBox = false;
                    borcForm.MinimizeBox = false;
                    borcForm.BackColor = Color.FromArgb(30, 30, 30);

                    TableLayoutPanel panel = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        Padding = new Padding(20),
                        RowCount = 5,
                        ColumnCount = 2
                    };

                    // Müşteri Adı
                    Label lblMusteri = new Label
                    {
                        Text = "Müşteri Adı:",
                        ForeColor = Color.White,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    TextBox txtMusteri = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(45, 45, 45),
                        ForeColor = Color.White
                    };

                    // Borç Miktarı
                    Label lblMiktar = new Label
                    {
                        Text = "Borç Miktarı:",
                        ForeColor = Color.White,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    TextBox txtMiktar = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(45, 45, 45),
                        ForeColor = Color.White
                    };

                    // Açıklama
                    Label lblAciklama = new Label
                    {
                        Text = "Açıklama:",
                        ForeColor = Color.White,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    TextBox txtAciklama = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(45, 45, 45),
                        ForeColor = Color.White,
                        Multiline = true,
                        Height = 60
                    };

                    // Kaydet Butonu
                    Button btnKaydet = new Button
                    {
                        Text = "Kaydet",
                        BackColor = Color.FromArgb(0, 123, 255),
                        ForeColor = Color.White,
                        Dock = DockStyle.Fill,
                        FlatStyle = FlatStyle.Flat
                    };
                    btnKaydet.Click += (s, ev) =>
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(txtMusteri.Text))
                            {
                                MessageBox.Show("Müşteri adı boş olamaz!", "Uyarı",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            if (!decimal.TryParse(txtMiktar.Text.Replace(",", "."), 
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, 
                                out decimal borcMiktari) || borcMiktari <= 0)
                            {
                                MessageBox.Show("Geçerli bir borç miktarı giriniz!", "Uyarı",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            string sorgu = @"
                                INSERT INTO Borclar (MusteriAdi, BorcMiktari, Aciklama, Tarih, OdemeDurumu, Durumu, OdenenMiktar, OdemeTarihi)
                                VALUES (@MusteriAdi, @BorcMiktari, @Aciklama, @Tarih, 0, 'Ödenmedi', 0, NULL)";

                            var parameters = new[]
                            {
                                new SQLiteParameter("@MusteriAdi", txtMusteri.Text.Trim()),
                                new SQLiteParameter("@BorcMiktari", borcMiktari),
                                new SQLiteParameter("@Aciklama", txtAciklama.Text.Trim()),
                                new SQLiteParameter("@Tarih", DateTime.Now.ToString("yyyy-MM-dd"))
                            };

                            DatabaseManager.ExecuteNonQuery(sorgu, parameters);

                            MessageBox.Show("Borç başarıyla eklendi!", "Bilgi",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            borcForm.DialogResult = DialogResult.OK;
                            borcForm.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Borç eklenirken bir hata oluştu: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };

                    panel.Controls.Add(lblMusteri, 0, 0);
                    panel.Controls.Add(txtMusteri, 1, 0);
                    panel.Controls.Add(lblMiktar, 0, 1);
                    panel.Controls.Add(txtMiktar, 1, 1);
                    panel.Controls.Add(lblAciklama, 0, 2);
                    panel.Controls.Add(txtAciklama, 1, 2);
                    panel.Controls.Add(btnKaydet, 1, 3);

                    borcForm.Controls.Add(panel);
                    borcForm.ShowDialog();

                    if (borcForm.DialogResult == DialogResult.OK)
                    {
                        btnBorcTakip_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borç ekleme formu açılırken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 