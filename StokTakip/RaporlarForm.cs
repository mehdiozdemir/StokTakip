using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace StokTakip
{
    public partial class RaporlarForm : Form
    {
        private readonly string dbDosyasi = "StokTakip.db";
        private DataGridView? dgvRaporlar;
        private DateTimePicker? dtpBaslangic;
        private DateTimePicker? dtpBitis;
        private Button? btnRaporGoster;
        private Button? btnGorunumDegistir;
        private Label? lblToplamSatis;
        private Label? lblToplamKar;
        private bool aylikGorunum = false;
        private ContextMenuStrip? gridMenuStrip;
        private bool isLoading = false;
        private Button? btnGunluk;
        private Button? btnAylik;
        private Button? btnYillik;
        private string aktifGorunum = "Günlük";
        private Chart? chartSatislar;
        private Button? btnGrafikGoster;
        private Panel? pnlGrafik;
        private bool grafikGorunumu = false;

        public RaporlarForm()
        {
            InitializeComponent();
            FormTasarimOlustur();

            // Form yüklendiğinde başlık çubuğunu siyah yap
            this.HandleCreated += (s, e) => 
            {
                DarkModeHelper.UseImmersiveDarkMode(this.Handle, true);
            };
        }

        private void LoadTheme()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            foreach (Control control in this.Controls)
            {
                if (control is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.BackColor = Color.FromArgb(45, 45, 45);
                    btn.ForeColor = Color.White;
                }
                else if (control is ComboBox cmb)
                {
                    cmb.BackColor = Color.FromArgb(45, 45, 45);
                    cmb.ForeColor = Color.White;
                }
                else if (control is TextBox txt)
                {
                    txt.BackColor = Color.FromArgb(45, 45, 45);
                    txt.ForeColor = Color.White;
                }
            }
        }

        private void GunlukGorunumGoster(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            string sql = @"
                WITH IadeBilgileri AS (
                    SELECT 
                        SatisId,
                        SUM(Miktar) as IadeMiktar,
                        SUM(Miktar * BirimFiyat) as IadeTutar,
                        COUNT(*) as IadeIslemSayisi
                    FROM Iadeler
                    GROUP BY SatisId
                )
                SELECT 
                    s.SatisId as Id,
                    strftime('%d.%m.%Y %H:%M', s.SatisTarihi) as Tarih,
                    s.MusteriAdi,
                    u.UrunAdi,
                    k.KategoriAdi as Kategori,
                    sd.Miktar as SatisMiktari,
                    COALESCE(i.IadeMiktar, 0) as IadeMiktar,
                    (sd.Miktar - COALESCE(i.IadeMiktar, 0)) as NetMiktar,
                    sd.BirimFiyat,
                    (sd.Miktar * sd.BirimFiyat) as BrutSatis,
                    COALESCE(i.IadeTutar, 0) as IadeTutar,
                    (sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                    u.AlisFiyati as BirimMaliyet,
                    ((sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as ToplamMaliyet,
                    (sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) - 
                    ((sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar,
                    CASE 
                        WHEN COALESCE(i.IadeMiktar, 0) >= sd.Miktar THEN 'Tam İade'
                        WHEN COALESCE(i.IadeMiktar, 0) > 0 THEN 'Kısmi İade'
                        ELSE 'Hayır'
                    END as IadeDurumu
                FROM Satislar s
                JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                JOIN Urunler u ON sd.UrunId = u.Id
                JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                WHERE datetime(s.SatisTarihi) BETWEEN datetime(@BaslangicTarihi) AND datetime(@BitisTarihi)
                ORDER BY s.SatisTarihi DESC";

            var parameters = new[]
            {
                new SQLiteParameter("@BaslangicTarihi", baslangicTarihi.ToString("yyyy-MM-dd HH:mm:ss")),
                new SQLiteParameter("@BitisTarihi", bitisTarihi.ToString("yyyy-MM-dd HH:mm:ss"))
            };

            var dt = DatabaseManager.ExecuteQuery(sql, parameters);
            dgvRaporlar.DataSource = dt;
            FormatGunlukGorunum();
            HesaplaVeGosterToplamlar(dt);
        }

        private void AylikGorunumGoster(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            string sql = @"
                WITH IadeBilgileri AS (
                    SELECT 
                        SatisId,
                        SUM(Miktar) as IadeMiktar,
                        SUM(Miktar * BirimFiyat) as IadeTutar
                    FROM Iadeler
                    GROUP BY SatisId
                )
                SELECT 
                    strftime('%d.%m.%Y', s.SatisTarihi) as Tarih,
                    COUNT(DISTINCT s.SatisId) as IslemSayisi,
                    SUM(sd.Miktar) as ToplamMiktar,
                    SUM(COALESCE(i.IadeMiktar, 0)) as ToplamIadeMiktar,
                    SUM(sd.Miktar - COALESCE(i.IadeMiktar, 0)) as NetMiktar,
                    SUM(sd.Miktar * sd.BirimFiyat) as BrutSatis,
                    SUM(COALESCE(i.IadeTutar, 0)) as ToplamIade,
                    SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                    SUM(sd.Miktar * u.AlisFiyati) as ToplamMaliyet,
                    SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) - 
                    SUM((sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar
                FROM Satislar s
                JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                JOIN Urunler u ON sd.UrunId = u.Id
                LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                WHERE datetime(s.SatisTarihi) BETWEEN datetime(@BaslangicTarihi) AND datetime(@BitisTarihi)
                GROUP BY strftime('%d.%m.%Y', s.SatisTarihi)
                ORDER BY s.SatisTarihi DESC";

            var parameters = new[]
            {
                new SQLiteParameter("@BaslangicTarihi", baslangicTarihi.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@BitisTarihi", bitisTarihi.ToString("yyyy-MM-dd"))
            };

            var dt = DatabaseManager.ExecuteQuery(sql, parameters);
            dgvRaporlar.DataSource = dt;
            FormatAylikGorunum();
            HesaplaVeGosterToplamlar(dt);
        }

        private void YillikGorunumGoster(DateTime baslangicTarihi, DateTime bitisTarihi)
        {
            string sql = @"
                WITH IadeBilgileri AS (
                    SELECT 
                        SatisId,
                        SUM(Miktar) as IadeMiktar,
                        SUM(Miktar * BirimFiyat) as IadeTutar
                    FROM Iadeler
                    GROUP BY SatisId
                )
                SELECT 
                    strftime('%m.%Y', s.SatisTarihi) as Ay,
                    COUNT(DISTINCT s.SatisId) as IslemSayisi,
                    SUM(sd.Miktar) as ToplamMiktar,
                    SUM(COALESCE(i.IadeMiktar, 0)) as ToplamIadeMiktar,
                    SUM(sd.Miktar - COALESCE(i.IadeMiktar, 0)) as NetMiktar,
                    SUM(sd.Miktar * sd.BirimFiyat) as BrutSatis,
                    SUM(COALESCE(i.IadeTutar, 0)) as ToplamIade,
                    SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                    SUM(sd.Miktar * u.AlisFiyati) as ToplamMaliyet,
                    SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) - 
                    SUM((sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar,
                    CASE strftime('%m', s.SatisTarihi)
                        WHEN '01' THEN 'Ocak'
                        WHEN '02' THEN 'Şubat'
                        WHEN '03' THEN 'Mart'
                        WHEN '04' THEN 'Nisan'
                        WHEN '05' THEN 'Mayıs'
                        WHEN '06' THEN 'Haziran'
                        WHEN '07' THEN 'Temmuz'
                        WHEN '08' THEN 'Ağustos'
                        WHEN '09' THEN 'Eylül'
                        WHEN '10' THEN 'Ekim'
                        WHEN '11' THEN 'Kasım'
                        WHEN '12' THEN 'Aralık'
                    END as AyAdi
                FROM Satislar s
                JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                JOIN Urunler u ON sd.UrunId = u.Id
                LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                WHERE datetime(s.SatisTarihi) BETWEEN datetime(@BaslangicTarihi) AND datetime(@BitisTarihi)
                GROUP BY strftime('%m.%Y', s.SatisTarihi)
                ORDER BY s.SatisTarihi ASC";

            var parameters = new[]
            {
                new SQLiteParameter("@BaslangicTarihi", baslangicTarihi.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@BitisTarihi", bitisTarihi.ToString("yyyy-MM-dd"))
            };

            var dt = DatabaseManager.ExecuteQuery(sql, parameters);
            dgvRaporlar.DataSource = dt;
            FormatYillikGorunum();
            HesaplaVeGosterToplamlar(dt);
        }

        private void FormatAylikGorunum()
        {
            if (dgvRaporlar?.Columns == null) return;

            // Sütun başlıklarını düzenle
            dgvRaporlar.Columns["Tarih"].HeaderText = "Tarih";
            dgvRaporlar.Columns["IslemSayisi"].HeaderText = "İşlem Sayısı";
            dgvRaporlar.Columns["ToplamMiktar"].HeaderText = "Toplam Satış";
            dgvRaporlar.Columns["ToplamIadeMiktar"].HeaderText = "İade Miktarı";
            dgvRaporlar.Columns["NetMiktar"].HeaderText = "Net Satış";
            dgvRaporlar.Columns["BrutSatis"].HeaderText = "Brüt Satış (₺)";
            dgvRaporlar.Columns["ToplamIade"].HeaderText = "İade Tutarı (₺)";
            dgvRaporlar.Columns["NetSatis"].HeaderText = "Net Satış (₺)";
            dgvRaporlar.Columns["ToplamMaliyet"].HeaderText = "Maliyet (₺)";
            dgvRaporlar.Columns["Kar"].HeaderText = "Kar/Zarar (₺)";

            // AutoSize modunu kapatıp manuel genişlik ayarla
            dgvRaporlar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Sütun genişliklerini ayarla
            dgvRaporlar.Columns["Tarih"].Width = 100;
            dgvRaporlar.Columns["IslemSayisi"].Width = 100;
            dgvRaporlar.Columns["ToplamMiktar"].Width = 100;
            dgvRaporlar.Columns["ToplamIadeMiktar"].Width = 100;
            dgvRaporlar.Columns["NetMiktar"].Width = 100;
            dgvRaporlar.Columns["BrutSatis"].Width = 120;
            dgvRaporlar.Columns["ToplamIade"].Width = 120;
            dgvRaporlar.Columns["NetSatis"].Width = 120;
            dgvRaporlar.Columns["ToplamMaliyet"].Width = 120;
            dgvRaporlar.Columns["Kar"].Width = 120;

            // Para formatı ve hizalama
            string[] paraBirimiSutunlari = { "BrutSatis", "ToplamIade", "NetSatis", "ToplamMaliyet", "Kar" };
            foreach (var sutun in paraBirimiSutunlari)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Format = "N2";
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Sayısal değerleri sağa yasla
            string[] sayisalSutunlar = { "IslemSayisi", "ToplamMiktar", "ToplamIadeMiktar", "NetMiktar" };
            foreach (var sutun in sayisalSutunlar)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Zebra desenli satırlar ve renklendirme
            bool isZebra = true;
            Color zebraColor1 = Color.FromArgb(40, 40, 40);
            Color zebraColor2 = Color.FromArgb(50, 50, 50);

            foreach (DataGridViewRow row in dgvRaporlar.Rows)
            {
                if (row.IsNewRow) continue;

                row.DefaultCellStyle.BackColor = isZebra ? zebraColor1 : zebraColor2;
                row.DefaultCellStyle.ForeColor = Color.White;
                isZebra = !isZebra;

                // Kar/Zarar hücresi renklendirmesi
                if (row.Cells["Kar"].Value != null)
                {
                    decimal kar = Convert.ToDecimal(row.Cells["Kar"].Value);
                    row.Cells["Kar"].Style.ForeColor = kar >= 0 ? Color.Lime : Color.Red;
                }
            }

            dgvRaporlar.ClearSelection();
        }

        private void FormatYillikGorunum()
        {
            if (dgvRaporlar?.Columns == null) return;

            // Sütun başlıklarını düzenle
            dgvRaporlar.Columns["Ay"].Visible = false;
            dgvRaporlar.Columns["AyAdi"].HeaderText = "Ay";
            dgvRaporlar.Columns["IslemSayisi"].HeaderText = "İşlem Sayısı";
            dgvRaporlar.Columns["ToplamMiktar"].HeaderText = "Toplam Satış";
            dgvRaporlar.Columns["ToplamIadeMiktar"].HeaderText = "İade Miktarı";
            dgvRaporlar.Columns["NetMiktar"].HeaderText = "Net Satış";
            dgvRaporlar.Columns["BrutSatis"].HeaderText = "Brüt Satış (₺)";
            dgvRaporlar.Columns["ToplamIade"].HeaderText = "İade Tutarı (₺)";
            dgvRaporlar.Columns["NetSatis"].HeaderText = "Net Satış (₺)";
            dgvRaporlar.Columns["ToplamMaliyet"].HeaderText = "Maliyet (₺)";
            dgvRaporlar.Columns["Kar"].HeaderText = "Kar/Zarar (₺)";

            // AutoSize modunu kapatıp manuel genişlik ayarla
            dgvRaporlar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Sütun genişliklerini ayarla
            dgvRaporlar.Columns["AyAdi"].Width = 100;
            dgvRaporlar.Columns["IslemSayisi"].Width = 100;
            dgvRaporlar.Columns["ToplamMiktar"].Width = 100;
            dgvRaporlar.Columns["ToplamIadeMiktar"].Width = 100;
            dgvRaporlar.Columns["NetMiktar"].Width = 100;
            dgvRaporlar.Columns["BrutSatis"].Width = 120;
            dgvRaporlar.Columns["ToplamIade"].Width = 120;
            dgvRaporlar.Columns["NetSatis"].Width = 120;
            dgvRaporlar.Columns["ToplamMaliyet"].Width = 120;
            dgvRaporlar.Columns["Kar"].Width = 120;

            // Para formatı ve hizalama
            string[] paraBirimiSutunlari = { "BrutSatis", "ToplamIade", "NetSatis", "ToplamMaliyet", "Kar" };
            foreach (var sutun in paraBirimiSutunlari)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Format = "N2";
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Sayısal değerleri sağa yasla
            string[] sayisalSutunlar = { "IslemSayisi", "ToplamMiktar", "ToplamIadeMiktar", "NetMiktar" };
            foreach (var sutun in sayisalSutunlar)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Zebra desenli satırlar ve renklendirme
            bool isZebra = true;
            Color zebraColor1 = Color.FromArgb(40, 40, 40);
            Color zebraColor2 = Color.FromArgb(50, 50, 50);

            foreach (DataGridViewRow row in dgvRaporlar.Rows)
            {
                if (row.IsNewRow) continue;

                row.DefaultCellStyle.BackColor = isZebra ? zebraColor1 : zebraColor2;
                row.DefaultCellStyle.ForeColor = Color.White;
                isZebra = !isZebra;

                // Kar/Zarar hücresi renklendirmesi
                if (row.Cells["Kar"].Value != null)
                {
                    decimal kar = Convert.ToDecimal(row.Cells["Kar"].Value);
                    row.Cells["Kar"].Style.ForeColor = kar >= 0 ? Color.Lime : Color.Red;
                }
            }

            dgvRaporlar.ClearSelection();
        }

        private void HesaplaVeGosterToplamlar(DataTable dt)
        {
            if (dt == null || lblToplamSatis == null || lblToplamKar == null) return;

            decimal toplamSatis = 0;
            decimal toplamKar = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("NetSatis"))
                {
                    toplamSatis += Convert.ToDecimal(row["NetSatis"]);
                }
                if (dt.Columns.Contains("ToplamKar"))
                {
                    toplamKar += Convert.ToDecimal(row["ToplamKar"]);
                }
                else if (dt.Columns.Contains("Kar"))
                {
                    toplamKar += Convert.ToDecimal(row["Kar"]);
                }
            }

            lblToplamSatis.Text = $"Toplam Satış: ₺{toplamSatis:N2} ({dt.Rows.Count} işlem)";
            lblToplamKar.Text = $"Toplam Kar: {toplamKar:N2}";
            lblToplamKar.ForeColor = toplamKar >= 0 ? Color.Lime : Color.Red;
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Text = "Raporlar";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += RaporlarForm_FormClosing;
        }

        private void RaporlarForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !Application.OpenForms.OfType<MainForm>().First().IsDisposed)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void FormTasarimOlustur()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Padding = new Padding(10);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Üst Panel (Filtreler)
            Panel pnlUst = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Başlık
            Label lblBaslik = new Label
            {
                Text = "SATIŞ RAPORLARI",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20)
            };

            // Görünüm Butonları
            FlowLayoutPanel pnlGorunum = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = 360,
                Height = 35,
                Location = new Point(300, 20),
                BackColor = Color.Transparent
            };

            btnGunluk = new Button
            {
                Text = "📅 GÜNLÜK",
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnGunluk.FlatAppearance.BorderSize = 0;
            btnGunluk.Click += BtnGunluk_Click;

            btnAylik = new Button
            {
                Text = "📅 AYLIK",
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnAylik.FlatAppearance.BorderSize = 0;
            btnAylik.Click += BtnAylik_Click;

            btnYillik = new Button
            {
                Text = "📅 YILLIK",
                Size = new Size(110, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnYillik.FlatAppearance.BorderSize = 0;
            btnYillik.Click += BtnYillik_Click;

            pnlGorunum.Controls.AddRange(new Control[] { btnGunluk, btnAylik, btnYillik });

            // Tarih Seçici
            Label lblTarih = new Label
            {
                Text = "Tarih Aralığı:",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 70)
            };

            dtpBaslangic = new DateTimePicker
            {
                Width = 150,
                Location = new Point(120, 68),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            dtpBaslangic.ValueChanged += dtpBaslangic_ValueChanged;

            Label lblTarihAyrac = new Label
            {
                Text = "-",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(280, 70)
            };

            dtpBitis = new DateTimePicker
            {
                Width = 150,
                Location = new Point(300, 68),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            dtpBitis.ValueChanged += dtpBitis_ValueChanged;

            btnRaporGoster = new Button
            {
                Text = "🔍 Raporu Göster",
                Width = 150,
                Height = 35,
                Location = new Point(480, 65),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRaporGoster.FlatAppearance.BorderSize = 0;
            btnRaporGoster.Click += BtnRaporGoster_Click;

            // Toplam Bilgileri
            lblToplamSatis = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(650, 70)
            };

            lblToplamKar = new Label
            {
                AutoSize = true,
                ForeColor = Color.Lime,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(900, 70)
            };

            // Grafik Butonu
            btnGrafikGoster = new Button
            {
                Text = "📊 GRAFİK",
                Size = new Size(110, 35),
                Location = new Point(650, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnGrafikGoster.FlatAppearance.BorderSize = 0;
            btnGrafikGoster.Click += BtnGrafikGoster_Click;

            // Alt Panel (Grid ve Grafik için container)
            Panel pnlAlt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // DataGridView
            dgvRaporlar = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Black,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(45, 45, 45),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            // Grafik Paneli
            pnlGrafik = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Chart Kontrolü
            chartSatislar = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Chart ayarları
            chartSatislar.ChartAreas.Add(new ChartArea("SatisArea"));
            chartSatislar.ChartAreas[0].BackColor = Color.FromArgb(30, 30, 30);
            chartSatislar.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chartSatislar.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            chartSatislar.ChartAreas[0].AxisX.LineColor = Color.White;
            chartSatislar.ChartAreas[0].AxisY.LineColor = Color.White;
            chartSatislar.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.FromArgb(45, 45, 45);
            chartSatislar.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.FromArgb(45, 45, 45);
            chartSatislar.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 9);
            chartSatislar.ChartAreas[0].AxisY.LabelStyle.Font = new Font("Segoe UI", 9);
            chartSatislar.ChartAreas[0].AxisY.LabelStyle.Format = "₺{0:N0}";

            // Satış Serisi
            var satisSeries = new Series("Satışlar")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(0, 183, 195),
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 9),
                LabelForeColor = Color.White
            };
            chartSatislar.Series.Add(satisSeries);

            // Kar Serisi
            var karSeries = new Series("Kar")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Lime,
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 9),
                LabelForeColor = Color.White,
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8
            };
            chartSatislar.Series.Add(karSeries);

            // Legend ayarları
            chartSatislar.Legends.Add(new Legend("MainLegend"));
            chartSatislar.Legends[0].BackColor = Color.FromArgb(30, 30, 30);
            chartSatislar.Legends[0].ForeColor = Color.White;
            chartSatislar.Legends[0].Font = new Font("Segoe UI", 9);
            chartSatislar.Legends[0].Docking = Docking.Top;

            // Grafik başlığı
            chartSatislar.Titles.Add(new Title
            {
                Text = "Satış ve Kar Grafiği",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Alignment = ContentAlignment.TopCenter
            });

            // DataGridView ayarları
            dgvRaporlar.RowHeadersVisible = false;
            dgvRaporlar.EnableHeadersVisualStyles = false;
            dgvRaporlar.ColumnHeadersHeight = 40;
            dgvRaporlar.RowTemplate.Height = 35;

            dgvRaporlar.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgvRaporlar.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRaporlar.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 45, 45);
            dgvRaporlar.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            dgvRaporlar.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgvRaporlar.DefaultCellStyle.ForeColor = Color.White;
            dgvRaporlar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 183, 195);
            dgvRaporlar.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvRaporlar.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvRaporlar.DefaultCellStyle.Padding = new Padding(5);

            // Sağ Tık Menüsü
            gridMenuStrip = new ContextMenuStrip();
            var iadeItem = new ToolStripMenuItem("🔄 İade İşlemi", null, IadeIslemi_Click);
            var silItem = new ToolStripMenuItem("❌ Satışı Sil", null, SatisiSil_Click);
            gridMenuStrip.Items.AddRange(new ToolStripItem[] { iadeItem, silItem });
            dgvRaporlar.ContextMenuStrip = gridMenuStrip;

            // Üst panel kontrolleri
            pnlUst.Controls.Add(btnGrafikGoster);
            pnlUst.Controls.AddRange(new Control[] {
                lblBaslik,
                pnlGorunum,
                lblTarih, dtpBaslangic, lblTarihAyrac, dtpBitis,
                btnRaporGoster,
                lblToplamSatis, lblToplamKar
            });

            // Panelleri ana layout'a ekle
            pnlGrafik.Controls.Add(chartSatislar);
            pnlAlt.Controls.Add(pnlGrafik);
            pnlAlt.Controls.Add(dgvRaporlar);
            mainLayout.Controls.Add(pnlUst, 0, 0);
            mainLayout.Controls.Add(pnlAlt, 0, 1);
            this.Controls.Add(mainLayout);

            // Tema uygula
            LoadTheme();

            btnGorunumDegistir = new Button
            {
                Text = aylikGorunum ? "📅 Günlük G��rünüm" : "📅 Aylık Görünüm",
                Size = new Size(150, 35),
                Location = new Point(20, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            btnGorunumDegistir.FlatAppearance.BorderSize = 0;
            btnGorunumDegistir.Click += BtnGorunumDegistir_Click;

            this.Controls.Add(btnGorunumDegistir);

            // Excel'e Aktar Butonu
            Button btnExcel = new Button
            {
                Text = "📊 EXCEL'E AKTAR",
                Size = new Size(150, 35),
                Location = new Point(1000, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExcel.FlatAppearance.BorderSize = 0;
            btnExcel.Click += BtnExcel_Click;

            // Kontrolleri panele ekle
            pnlUst.Controls.Add(btnExcel);
        }

        private void BtnGorunumDegistir_Click(object? sender, EventArgs e)
        {
            if (btnGorunumDegistir == null) return;
            aylikGorunum = !aylikGorunum;
            btnGorunumDegistir.Text = aylikGorunum ? "📅 Günlük Görünüm" : "📅 Aylık Görünüm";
            RaporGoster();
        }

        private void BtnRaporGoster_Click(object? sender, EventArgs e)
        {
            RaporGoster();
        }

        private void IadeIslemi_Click(object? sender, EventArgs e)
        {
            if (dgvRaporlar?.CurrentRow == null) return;

            if (aylikGorunum)
            {
                MessageBox.Show("İade işlemi için lütfen günlük görünüme geçiniz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Seçili satış için iade işlemi yapmak istediğinize emin misiniz?",
                "İade İşlemi Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    int satisId = Convert.ToInt32(dgvRaporlar.CurrentRow.Cells["Id"].Value);
                    
                    // İade edilmiş mi kontrolü
                    string iadeDurumu = dgvRaporlar.CurrentRow.Cells["IadeDurumu"].Value.ToString();
                    if (iadeDurumu == "Tam İade")
                    {
                        MessageBox.Show("Bu satış zaten iade edilmiş!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    IadeIsleminiGerceklestir(satisId);
                    RaporGoster(); // Raporu yenile
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"İade işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SatisiSil_Click(object? sender, EventArgs e)
        {
            if (dgvRaporlar?.CurrentRow == null) return;

            var result = MessageBox.Show(
                "Seçili satışı silmek istediğinize emin misiniz? Bu işlem geri alınamaz!",
                "Satış Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    int satisId = Convert.ToInt32(dgvRaporlar.CurrentRow.Cells["Id"].Value);
                    SatisiSil(satisId);
                    RaporGoster(); // Raporu yenile
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Satış silme işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void IadeIsleminiGerceklestir(int satisId)
        {
            try
            {
                string satisQuery = @"
                    SELECT 
                        s.SatisId,
                        sd.UrunId,
                        sd.Miktar,
                        sd.BirimFiyat,
                        u.UrunAdi,
                        COALESCE((SELECT SUM(Miktar) FROM Iadeler WHERE SatisId = s.SatisId), 0) as ToplamIadeEdilen,
                        CASE 
                            WHEN COALESCE((SELECT SUM(Miktar) FROM Iadeler WHERE SatisId = s.SatisId), 0) >= sd.Miktar THEN 'Tam İade'
                            WHEN COALESCE((SELECT SUM(Miktar) FROM Iadeler WHERE SatisId = s.SatisId), 0) > 0 THEN 'Kısmi İade'
                            ELSE 'Hayır'
                        END as IadeDurumu
                    FROM Satislar s
                    JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                    JOIN Urunler u ON sd.UrunId = u.Id
                    WHERE s.SatisId = @SatisId";

                var parameters = new[] { new SQLiteParameter("@SatisId", satisId) };
                var dt = DatabaseManager.ExecuteQuery(satisQuery, parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    int urunId = Convert.ToInt32(row["UrunId"]);
                    int miktar = Convert.ToInt32(row["Miktar"]);
                    decimal birimFiyat = Convert.ToDecimal(row["BirimFiyat"]);
                    string urunAdi = row["UrunAdi"].ToString();
                    int toplamIadeEdilen = Convert.ToInt32(row["ToplamIadeEdilen"]);
                    string iadeDurumu = row["IadeDurumu"].ToString();

                    if (iadeDurumu == "Tam İade")
                    {
                        MessageBox.Show("Bu satış zaten iade edilmiş!", "Uyarı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (toplamIadeEdilen >= miktar)
                    {
                        MessageBox.Show("Bu satış için iade hakkı kalmamıştır!", "Uyarı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (var iadeForm = new IadeForm(satisId, urunId, urunAdi, miktar - toplamIadeEdilen, birimFiyat))
                    {
                        if (iadeForm.ShowDialog() == DialogResult.OK)
                        {
                            MessageBox.Show("İade işlemi başarıyla tamamlandı.", "Başarılı", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            RaporGoster(); // Raporu yenile
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Satış kaydı bulunamadı!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İade işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SatisiSil(int satisId)
        {
            try
            {
                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    // Satış bilgilerini al
                    string satisQuery = @"
                        SELECT sd.UrunId, sd.Miktar, COALESCE(i.IadeMiktar, 0) as IadeMiktar
                        FROM Satislar s
                        JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                        LEFT JOIN (
                            SELECT SatisId, SUM(Miktar) as IadeMiktar
                            FROM Iadeler
                            GROUP BY SatisId
                        ) i ON s.SatisId = i.SatisId
                        WHERE s.SatisId = @SatisId";

                    using (var cmd = new SQLiteCommand(satisQuery, connection))
                    {
                        cmd.Transaction = transaction;
                        cmd.Parameters.AddWithValue("@SatisId", satisId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int urunId = Convert.ToInt32(reader["UrunId"]);
                                int miktar = Convert.ToInt32(reader["Miktar"]);
                                int iadeMiktar = Convert.ToInt32(reader["IadeMiktar"]);
                                int iadeDisiMiktar = miktar - iadeMiktar;

                                if (iadeDisiMiktar > 0)
                                {
                                    // Ürün stokunu geri ekle
                                    string stokGuncelleQuery = "UPDATE Urunler SET Miktar = Miktar + @Miktar WHERE Id = @UrunId";
                                    using (var stokCmd = new SQLiteCommand(stokGuncelleQuery, connection))
                                    {
                                        stokCmd.Transaction = transaction;
                                        stokCmd.Parameters.AddWithValue("@Miktar", iadeDisiMiktar);
                                        stokCmd.Parameters.AddWithValue("@UrunId", urunId);
                                        stokCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // İade kayıtlarını sil
                        string iadeDeleteQuery = "DELETE FROM Iadeler WHERE SatisId = @SatisId";
                        using (var iadeCmd = new SQLiteCommand(iadeDeleteQuery, connection))
                        {
                            iadeCmd.Transaction = transaction;
                            iadeCmd.Parameters.AddWithValue("@SatisId", satisId);
                            iadeCmd.ExecuteNonQuery();
                        }

                        // Satış detaylarını sil
                        string satisDetayDeleteQuery = "DELETE FROM SatisDetaylari WHERE SatisId = @SatisId";
                        using (var satisDetayCmd = new SQLiteCommand(satisDetayDeleteQuery, connection))
                        {
                            satisDetayCmd.Transaction = transaction;
                            satisDetayCmd.Parameters.AddWithValue("@SatisId", satisId);
                            satisDetayCmd.ExecuteNonQuery();
                        }

                        // Satışı sil
                        string silQuery = "DELETE FROM Satislar WHERE SatisId = @SatisId";
                        using (var silCmd = new SQLiteCommand(silQuery, connection))
                        {
                            silCmd.Transaction = transaction;
                            silCmd.Parameters.AddWithValue("@SatisId", satisId);
                            silCmd.ExecuteNonQuery();
                        }
                    }
                });

                MessageBox.Show("Satış başarıyla silindi.", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RaporGoster(); // Raporu yenile
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Satış silme işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RaporGoster()
        {
            if (isLoading) return;
            isLoading = true;

            try
            {
                DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                DateTime bitisTarihi = dtpBitis.Value.Date;

                switch (aktifGorunum)
                {
                    case "Günlük":
                        bitisTarihi = bitisTarihi.AddDays(1).AddSeconds(-1);
                        GunlukGorunumGoster(baslangicTarihi, bitisTarihi);
                        break;

                    case "Aylık":
                        bitisTarihi = bitisTarihi.AddDays(1).AddSeconds(-1);
                        AylikGorunumGoster(baslangicTarihi, bitisTarihi);
                        break;

                    case "Yıllık":
                        bitisTarihi = bitisTarihi.AddDays(1).AddSeconds(-1);
                        YillikGorunumGoster(baslangicTarihi, bitisTarihi);
                        break;
                }
            }
            finally
            {
                isLoading = false;
            }

            if (grafikGorunumu)
            {
                GrafigiGuncelle();
            }
        }

        private void FormatGunlukGorunum()
        {
            if (dgvRaporlar?.Columns == null) return;

            // Sütun başlıklarını düzenle
            dgvRaporlar.Columns["Id"].Visible = false;
            dgvRaporlar.Columns["Tarih"].HeaderText = "Tarih";
            dgvRaporlar.Columns["MusteriAdi"].HeaderText = "Müşteri Adı";
            dgvRaporlar.Columns["UrunAdi"].HeaderText = "Ürün Adı";
            dgvRaporlar.Columns["Kategori"].HeaderText = "Kategori";
            dgvRaporlar.Columns["SatisMiktari"].HeaderText = "Satış Mik.";
            dgvRaporlar.Columns["IadeMiktar"].HeaderText = "İade Mik.";
            dgvRaporlar.Columns["NetMiktar"].HeaderText = "Net Mik.";
            dgvRaporlar.Columns["BirimFiyat"].HeaderText = "Birim Fiy.";
            dgvRaporlar.Columns["BrutSatis"].HeaderText = "Brüt Satış";
            dgvRaporlar.Columns["IadeTutar"].HeaderText = "İade Tut.";
            dgvRaporlar.Columns["NetSatis"].HeaderText = "Net Satış";
            dgvRaporlar.Columns["BirimMaliyet"].HeaderText = "Birim Mal.";
            dgvRaporlar.Columns["ToplamMaliyet"].HeaderText = "Top. Mal.";
            dgvRaporlar.Columns["Kar"].HeaderText = "Kar/Zarar";
            dgvRaporlar.Columns["IadeDurumu"].HeaderText = "İade";

            // AutoSize modunu kapatıp manuel genişlik ayarla
            dgvRaporlar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Sütun genişliklerini ayarla
            dgvRaporlar.Columns["Tarih"].Width = 120;
            dgvRaporlar.Columns["MusteriAdi"].Width = 150;
            dgvRaporlar.Columns["UrunAdi"].Width = 150;
            dgvRaporlar.Columns["Kategori"].Width = 100;
            dgvRaporlar.Columns["SatisMiktari"].Width = 80;
            dgvRaporlar.Columns["IadeMiktar"].Width = 80;
            dgvRaporlar.Columns["NetMiktar"].Width = 80;
            dgvRaporlar.Columns["BirimFiyat"].Width = 90;
            dgvRaporlar.Columns["BrutSatis"].Width = 100;
            dgvRaporlar.Columns["IadeTutar"].Width = 90;
            dgvRaporlar.Columns["NetSatis"].Width = 100;
            dgvRaporlar.Columns["BirimMaliyet"].Width = 90;
            dgvRaporlar.Columns["ToplamMaliyet"].Width = 100;
            dgvRaporlar.Columns["Kar"].Width = 100;
            dgvRaporlar.Columns["IadeDurumu"].Width = 80;

            // Para formatı
            string[] paraBirimiSutunlari = { "BirimFiyat", "BrutSatis", "IadeTutar", "NetSatis", 
                                           "BirimMaliyet", "ToplamMaliyet", "Kar" };
            foreach (var sutun in paraBirimiSutunlari)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Format = "N2";
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Miktar sütunlarını sağa yasla
            string[] miktarSutunlari = { "SatisMiktari", "IadeMiktar", "NetMiktar" };
            foreach (var sutun in miktarSutunlari)
            {
                if (dgvRaporlar.Columns[sutun] != null)
                {
                    dgvRaporlar.Columns[sutun].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            bool isZebra = true;
            Color zebraColor1 = Color.FromArgb(40, 40, 40);
            Color zebraColor2 = Color.FromArgb(50, 50, 50);

            foreach (DataGridViewRow row in dgvRaporlar.Rows)
            {
                if (row.IsNewRow) continue;

                // Varsayılan zebra renklendirmesi
                row.DefaultCellStyle.BackColor = isZebra ? zebraColor1 : zebraColor2;
                row.DefaultCellStyle.ForeColor = Color.White;
                isZebra = !isZebra;

                // İade durumuna göre renklendirme
                if (row.Cells["IadeDurumu"].Value != null)
                {
                    string iadeDurumu = row.Cells["IadeDurumu"].Value.ToString();
                    if (iadeDurumu == "Tam İade")
                    {
                        row.DefaultCellStyle.BackColor = Color.DarkRed;
                    }
                    else if (iadeDurumu == "Kısmi İade")
                    {
                        row.DefaultCellStyle.BackColor = Color.DarkOrange;
                    }
                }

                // Kar/Zarar hücresi renklendirmesi
                if (row.Cells["Kar"].Value != null)
                {
                    decimal kar = Convert.ToDecimal(row.Cells["Kar"].Value);
                    row.Cells["Kar"].Style.ForeColor = kar >= 0 ? Color.Lime : Color.Red;
                }
            }

            dgvRaporlar.ClearSelection();
        }

        private void BtnGunluk_Click(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                isLoading = true;
                try
                {
                    aktifGorunum = "Günlük";
                    btnGunluk.BackColor = Color.FromArgb(0, 183, 195);
                    btnAylik.BackColor = Color.FromArgb(45, 45, 45);
                    btnYillik.BackColor = Color.FromArgb(45, 45, 45);

                    dtpBaslangic.Value = DateTime.Today;
                    dtpBitis.Value = DateTime.Today;
                    
                    DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                    DateTime bitisTarihi = dtpBitis.Value.Date.AddDays(1).AddSeconds(-1);
                    GunlukGorunumGoster(baslangicTarihi, bitisTarihi);

                    // Grafik görünürse güncelle
                    if (pnlGrafik != null && pnlGrafik.Visible)
                    {
                        GrafigiGuncelle();
                    }
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        private void BtnAylik_Click(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                isLoading = true;
                try
                {
                    aktifGorunum = "Aylık";
                    btnGunluk.BackColor = Color.FromArgb(45, 45, 45);
                    btnAylik.BackColor = Color.FromArgb(0, 183, 195);
                    btnYillik.BackColor = Color.FromArgb(45, 45, 45);

                    dtpBaslangic.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    dtpBitis.Value = dtpBaslangic.Value.AddMonths(1).AddDays(-1);
                    
                    DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                    DateTime bitisTarihi = dtpBitis.Value.Date.AddDays(1).AddSeconds(-1);
                    AylikGorunumGoster(baslangicTarihi, bitisTarihi);

                    // Grafik görünürse güncelle
                    if (pnlGrafik != null && pnlGrafik.Visible)
                    {
                        GrafigiGuncelle();
                    }
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        private void BtnYillik_Click(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                isLoading = true;
                try
                {
                    aktifGorunum = "Yıllık";
                    btnGunluk.BackColor = Color.FromArgb(45, 45, 45);
                    btnAylik.BackColor = Color.FromArgb(45, 45, 45);
                    btnYillik.BackColor = Color.FromArgb(0, 183, 195);

                    dtpBaslangic.Value = new DateTime(DateTime.Today.Year, 1, 1);
                    dtpBitis.Value = new DateTime(DateTime.Today.Year, 12, 31);
                    
                    DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                    DateTime bitisTarihi = dtpBitis.Value.Date.AddDays(1).AddSeconds(-1);
                    YillikGorunumGoster(baslangicTarihi, bitisTarihi);

                    // Grafik görünürse güncelle
                    if (pnlGrafik != null && pnlGrafik.Visible)
                    {
                        GrafigiGuncelle();
                    }
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        private void dtpBaslangic_ValueChanged(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                isLoading = true;
                try
                {
                    DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                    DateTime bitisTarihi = dtpBitis.Value.Date.AddDays(1).AddSeconds(-1);

                    switch (aktifGorunum)
                    {
                        case "Günlük":
                            GunlukGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                        case "Aylık":
                            AylikGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                        case "Yıllık":
                            YillikGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                    }

                    // Grafik görünürse güncelle
                    if (pnlGrafik != null && pnlGrafik.Visible)
                    {
                        GrafigiGuncelle();
                    }
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        private void dtpBitis_ValueChanged(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                isLoading = true;
                try
                {
                    DateTime baslangicTarihi = dtpBaslangic.Value.Date;
                    DateTime bitisTarihi = dtpBitis.Value.Date.AddDays(1).AddSeconds(-1);

                    switch (aktifGorunum)
                    {
                        case "Günlük":
                            GunlukGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                        case "Aylık":
                            AylikGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                        case "Yıllık":
                            YillikGorunumGoster(baslangicTarihi, bitisTarihi);
                            break;
                    }

                    // Grafik görünürse güncelle
                    if (pnlGrafik != null && pnlGrafik.Visible)
                    {
                        GrafigiGuncelle();
                    }
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        private void VeritabaniniKontrolEt()
        {
            string projeKlasoru = Application.StartupPath;
            string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);

            try
            {
                if (!File.Exists(dbYolu))
                {
                    SQLiteConnection.CreateFile(dbYolu);
                }

                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                {
                    baglanti.Open();

                    // Satislar tablosunu güncelle (IadeEdildi, IadeTarihi ve MusteriAdi kolonları ekle)
                    string alterSatislarTable = @"
                        CREATE TABLE IF NOT EXISTS Satislar_Temp (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UrunId INTEGER,
                            MusteriAdi TEXT,
                            Miktar INTEGER,
                            BirimFiyat DECIMAL(10,2),
                            ToplamFiyat DECIMAL(10,2),
                            Tarih DATETIME DEFAULT (datetime('now', 'localtime')),
                            IadeEdildi INTEGER DEFAULT 0,
                            IadeTarihi DATETIME,
                            FOREIGN KEY(UrunId) REFERENCES Urunler(Id)
                        );

                        INSERT INTO Satislar_Temp (Id, UrunId, MusteriAdi, Miktar, BirimFiyat, ToplamFiyat, Tarih)
                        SELECT Id, UrunId, COALESCE(MusteriAdi, 'Bilinmiyor') as MusteriAdi, Miktar, BirimFiyat, ToplamFiyat, Tarih FROM Satislar;

                        DROP TABLE IF EXISTS Satislar;

                        ALTER TABLE Satislar_Temp RENAME TO Satislar;";

                    using (SQLiteCommand cmd = new SQLiteCommand(alterSatislarTable, baglanti))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Iadeler tablosunu oluştur
                    string createIadelerTable = @"
                        CREATE TABLE IF NOT EXISTS Iadeler (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SatisId INTEGER,
                            UrunId INTEGER,
                            Miktar INTEGER,
                            BirimFiyat DECIMAL(10,2),
                            ToplamFiyat DECIMAL(10,2),
                            Tarih DATETIME DEFAULT (datetime('now', 'localtime')),
                            FOREIGN KEY(SatisId) REFERENCES Satislar(Id),
                            FOREIGN KEY(UrunId) REFERENCES Urunler(Id)
                        )";

                    using (SQLiteCommand cmd = new SQLiteCommand(createIadelerTable, baglanti))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı güncellenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGrafikGoster_Click(object sender, EventArgs e)
        {
            if (pnlGrafik == null || dgvRaporlar == null) return;

            // Panel görünürlüğünü değiştir
            pnlGrafik.BringToFront(); // Önemli: Grafik panelini öne getir
            pnlGrafik.Visible = !pnlGrafik.Visible;
            dgvRaporlar.Visible = !pnlGrafik.Visible;

            // Buton rengini değiştir
            if (sender is Button btn)
            {
                btn.BackColor = pnlGrafik.Visible ? 
                    Color.FromArgb(0, 183, 195) : Color.FromArgb(45, 45, 45);
            }

            // Grafik görünürse verileri güncelle
            if (pnlGrafik.Visible)
            {
                GrafigiGuncelle();
            }
        }

        private void GrafigiGuncelle()
        {
            if (chartSatislar == null) return;

            try
            {
                // Serileri temizle
                chartSatislar.Series["Satışlar"].Points.Clear();
                chartSatislar.Series["Kar"].Points.Clear();

                string sorgu = "";
                switch (aktifGorunum)
                {
                    case "Günlük":
                        sorgu = @"
                            WITH IadeBilgileri AS (
                                SELECT 
                                    SatisId,
                                    SUM(Miktar) as IadeMiktar,
                                    SUM(Miktar * BirimFiyat) as IadeTutar
                                FROM Iadeler
                                GROUP BY SatisId
                            )
                            SELECT 
                                strftime('%H:00', s.SatisTarihi) as Saat,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0) - 
                                    (sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar
                            FROM Satislar s
                            JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                            JOIN Urunler u ON sd.UrunId = u.Id
                            LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                            WHERE date(s.SatisTarihi) = date(@BaslangicTarihi)
                            GROUP BY strftime('%H', s.SatisTarihi)
                            ORDER BY Saat";
                        break;

                    case "Aylık":
                        sorgu = @"
                            WITH IadeBilgileri AS (
                                SELECT 
                                    SatisId,
                                    SUM(Miktar) as IadeMiktar,
                                    SUM(Miktar * BirimFiyat) as IadeTutar
                                FROM Iadeler
                                GROUP BY SatisId
                            )
                            SELECT 
                                strftime('%d.%m.%Y', s.SatisTarihi) as Tarih,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0) - 
                                    (sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar
                            FROM Satislar s
                            JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                            JOIN Urunler u ON sd.UrunId = u.Id
                            LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                            WHERE date(s.SatisTarihi) BETWEEN date(@BaslangicTarihi) AND date(@BitisTarihi)
                            GROUP BY date(s.SatisTarihi)
                            ORDER BY s.SatisTarihi";
                        break;

                    case "Yıllık":
                        sorgu = @"
                            WITH IadeBilgileri AS (
                                SELECT 
                                    SatisId,
                                    SUM(Miktar) as IadeMiktar,
                                    SUM(Miktar * BirimFiyat) as IadeTutar
                                FROM Iadeler
                                GROUP BY SatisId
                            )
                            SELECT 
                                strftime('%m.%Y', s.SatisTarihi) as Ay,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0)) as NetSatis,
                                SUM(sd.Miktar * sd.BirimFiyat - COALESCE(i.IadeTutar, 0) - 
                                    (sd.Miktar - COALESCE(i.IadeMiktar, 0)) * u.AlisFiyati) as Kar
                            FROM Satislar s
                            JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                            JOIN Urunler u ON sd.UrunId = u.Id
                            LEFT JOIN IadeBilgileri i ON s.SatisId = i.SatisId
                            WHERE date(s.SatisTarihi) BETWEEN date(@BaslangicTarihi) AND date(@BitisTarihi)
                            GROUP BY strftime('%m.%Y', s.SatisTarihi)
                            ORDER BY s.SatisTarihi";
                        break;
                }

                if (dtpBaslangic != null && dtpBitis != null)
                {
                    var parameters = new[]
                    {
                        new SQLiteParameter("@BaslangicTarihi", dtpBaslangic.Value.ToString("yyyy-MM-dd")),
                        new SQLiteParameter("@BitisTarihi", dtpBitis.Value.ToString("yyyy-MM-dd"))
                    };

                    var dt = DatabaseManager.ExecuteQuery(sorgu, parameters);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            string etiket = row[0].ToString() ?? "";
                            decimal netSatis = Convert.ToDecimal(row["NetSatis"]);
                            decimal kar = Convert.ToDecimal(row["Kar"]);

                            var satisPt = chartSatislar.Series["Satışlar"].Points.Add(Convert.ToDouble(netSatis));
                            satisPt.AxisLabel = etiket;

                            var karPt = chartSatislar.Series["Kar"].Points.Add(Convert.ToDouble(kar));
                            karPt.AxisLabel = etiket;
                        }

                        // Grafik başlığını güncelle
                        chartSatislar.Titles.Clear();
                        chartSatislar.Titles.Add(new Title
                        {
                            Text = $"{aktifGorunum} Satış ve Kar Grafiği",
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 12, FontStyle.Bold),
                            Alignment = ContentAlignment.TopCenter
                        });
                    }
                    else
                    {
                        MessageBox.Show("Seçilen tarih aralığında veri bulunamadı.", "Bilgi",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafik güncellenirken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvRaporlar.Rows.Count == 0)
                {
                    MessageBox.Show("Aktarılacak veri bulunamadı.", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                    FileName = $"Satis_Raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage(new FileInfo(saveDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Satış Raporu");

                        // Başlık satırı
                        for (int i = 0; i < dgvRaporlar.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = dgvRaporlar.Columns[i].HeaderText;
                        }

                        // Veriler
                        for (int row = 0; row < dgvRaporlar.Rows.Count; row++)
                        {
                            for (int col = 0; col < dgvRaporlar.Columns.Count; col++)
                            {
                                var value = dgvRaporlar.Rows[row].Cells[col].Value;
                                worksheet.Cells[row + 2, col + 1].Value = value;

                                // Para birimi formatı
                                if (value is decimal || dgvRaporlar.Columns[col].DefaultCellStyle.Format == "N2")
                                {
                                    worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "#,##0.00";
                                }
                            }
                        }

                        // Başlık stili
                        using (var range = worksheet.Cells[1, 1, 1, dgvRaporlar.Columns.Count])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 183, 195));
                            range.Style.Font.Color.SetColor(Color.White);
                        }

                        // Toplam satırı
                        int lastRow = dgvRaporlar.Rows.Count + 2;
                        worksheet.Cells[lastRow, 1].Value = "TOPLAM";
                        worksheet.Cells[lastRow, 1].Style.Font.Bold = true;

                        // Toplam değerleri
                        decimal toplamSatis = 0, toplamKar = 0;
                        foreach (DataGridViewRow row in dgvRaporlar.Rows)
                        {
                            if (row.Cells["NetSatis"].Value != null)
                                toplamSatis += Convert.ToDecimal(row.Cells["NetSatis"].Value);
                            if (row.Cells["Kar"].Value != null)
                                toplamKar += Convert.ToDecimal(row.Cells["Kar"].Value);
                        }

                        // Toplam satırını doldur
                        int netSatisIndex = dgvRaporlar.Columns["NetSatis"].Index;
                        int karIndex = dgvRaporlar.Columns["Kar"].Index;
                        worksheet.Cells[lastRow, netSatisIndex + 1].Value = toplamSatis;
                        worksheet.Cells[lastRow, karIndex + 1].Value = toplamKar;
                        worksheet.Cells[lastRow, netSatisIndex + 1].Style.Numberformat.Format = "#,##0.00";
                        worksheet.Cells[lastRow, karIndex + 1].Style.Numberformat.Format = "#,##0.00";

                        // Tablo formatı
                        var tableRange = worksheet.Cells[1, 1, lastRow, dgvRaporlar.Columns.Count];
                        tableRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                        // Otomatik sütun genişliği
                        worksheet.Cells.AutoFitColumns();

                        // Rapor bilgileri
                        worksheet.Cells[lastRow + 2, 1].Value = $"Rapor Türü: {aktifGorunum}";
                        worksheet.Cells[lastRow + 3, 1].Value = $"Başlangıç Tarihi: {dtpBaslangic.Value:dd.MM.yyyy}";
                        worksheet.Cells[lastRow + 4, 1].Value = $"Bitiş Tarihi: {dtpBitis.Value:dd.MM.yyyy}";
                        worksheet.Cells[lastRow + 5, 1].Value = $"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";

                        package.Save();
                    }

                    MessageBox.Show("Rapor başarıyla Excel'e aktarıldı.", "Başarılı",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Excel dosyasını aç
                    System.Diagnostics.Process.Start("explorer.exe", saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel'e aktarma sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 