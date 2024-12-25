using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace StokTakip
{
    public partial class VeriTabaniYedeklemeForm : Form
    {
        private readonly string dbDosyasi = "StokTakip.db";
        private Label lblSonYedeklemeTarihi;
        private System.Windows.Forms.Timer otomatikYedeklemeTimer;
        private const string YEDEKLEME_AYARLARI_DOSYASI = "yedekleme_ayarlari.json";
        private const string YEDEKLEME_GECMISI_DOSYASI = "yedekleme_gecmisi.json";

        public class YedeklemeAyarlari
        {
            public bool OtomatikYedeklemeAktif { get; set; }
            public int YedeklemeAraligi { get; set; } // Saat cinsinden
            public int EskiYedekSaklamaSuresi { get; set; } // Gün cinsinden
            public string YedeklemeDizini { get; set; }
        }

        public class YedeklemeKaydi
        {
            public DateTime Tarih { get; set; }
            public string DosyaAdi { get; set; }
            public long DosyaBoyutu { get; set; }
            public bool Basarili { get; set; }
            public string Aciklama { get; set; }
        }

        private YedeklemeAyarlari ayarlar;
        private List<YedeklemeKaydi> yedeklemeGecmisi;

        public VeriTabaniYedeklemeForm()
        {
            InitializeComponent();
            FormTasarimOlustur();
            AyarlariYukle();
            YedeklemeGecmisiniYukle();
            OtomatikYedeklemeTimerKur();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            this.HandleCreated += (s, e) => 
            {
                DarkModeHelper.UseImmersiveDarkMode(this.Handle, true);
            };
        }

        private void OtomatikYedeklemeTimerKur()
        {
            otomatikYedeklemeTimer = new System.Windows.Forms.Timer();
            otomatikYedeklemeTimer.Interval = 60000; // Her dakika kontrol et
            otomatikYedeklemeTimer.Tick += OtomatikYedeklemeTimer_Tick;
            otomatikYedeklemeTimer.Enabled = ayarlar.OtomatikYedeklemeAktif;
        }

        private void OtomatikYedeklemeTimer_Tick(object sender, EventArgs e)
        {
            if (!ayarlar.OtomatikYedeklemeAktif) return;

            var sonYedekleme = yedeklemeGecmisi
                .Where(y => y.Basarili)
                .OrderByDescending(y => y.Tarih)
                .FirstOrDefault();

            if (sonYedekleme == null || 
                (DateTime.Now - sonYedekleme.Tarih).TotalHours >= ayarlar.YedeklemeAraligi)
            {
                OtomatikYedeklemeYap();
            }

            // Eski yedekleri temizle
            EskiYedekleriTemizle();
        }

        private void OtomatikYedeklemeYap()
        {
            try
            {
                string yedekDosyaAdi = $"OtomatikYedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                string yedekYolu = Path.Combine(ayarlar.YedeklemeDizini, yedekDosyaAdi);

                // Yedekleme dizinini kontrol et
                if (!Directory.Exists(ayarlar.YedeklemeDizini))
                {
                    Directory.CreateDirectory(ayarlar.YedeklemeDizini);
                }

                string kaynakDosya = Path.Combine(Application.StartupPath, dbDosyasi);
                File.Copy(kaynakDosya, yedekYolu, true);

                var yeniKayit = new YedeklemeKaydi
                {
                    Tarih = DateTime.Now,
                    DosyaAdi = yedekDosyaAdi,
                    DosyaBoyutu = new FileInfo(yedekYolu).Length,
                    Basarili = true,
                    Aciklama = "Otomatik yedekleme başarılı"
                };

                yedeklemeGecmisi.Add(yeniKayit);
                YedeklemeGecmisiniKaydet();
                SonYedeklemeTarihiniGoster();
            }
            catch (Exception ex)
            {
                var yeniKayit = new YedeklemeKaydi
                {
                    Tarih = DateTime.Now,
                    DosyaAdi = "",
                    DosyaBoyutu = 0,
                    Basarili = false,
                    Aciklama = $"Otomatik yedekleme hatası: {ex.Message}"
                };

                yedeklemeGecmisi.Add(yeniKayit);
                YedeklemeGecmisiniKaydet();
            }
        }

        private void EskiYedekleriTemizle()
        {
            try
            {
                var eskiTarih = DateTime.Now.AddDays(-ayarlar.EskiYedekSaklamaSuresi);
                var eskiYedekler = Directory.GetFiles(ayarlar.YedeklemeDizini, "OtomatikYedek_*.db")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTime < eskiTarih);

                foreach (var eskiYedek in eskiYedekler)
                {
                    File.Delete(eskiYedek.FullName);
                }

                // Geçmiş kayıtlarını da temizle
                yedeklemeGecmisi.RemoveAll(y => y.Tarih < eskiTarih);
                YedeklemeGecmisiniKaydet();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eski yedekler temizlenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AyarlariYukle()
        {
            try
            {
                string ayarlarYolu = Path.Combine(Application.StartupPath, YEDEKLEME_AYARLARI_DOSYASI);
                if (File.Exists(ayarlarYolu))
                {
                    string json = File.ReadAllText(ayarlarYolu);
                    ayarlar = System.Text.Json.JsonSerializer.Deserialize<YedeklemeAyarlari>(json);
                }
                else
                {
                    // Varsayılan ayarlar
                    ayarlar = new YedeklemeAyarlari
                    {
                        OtomatikYedeklemeAktif = true,
                        YedeklemeAraligi = 24, // 24 saat
                        EskiYedekSaklamaSuresi = 30, // 30 gün
                        YedeklemeDizini = Path.Combine(Application.StartupPath, "Yedekler")
                    };
                    AyarlariKaydet();
                }
            }
            catch
            {
                // Hata durumunda varsayılan ayarları kullan
                ayarlar = new YedeklemeAyarlari
                {
                    OtomatikYedeklemeAktif = false,
                    YedeklemeAraligi = 24,
                    EskiYedekSaklamaSuresi = 30,
                    YedeklemeDizini = Path.Combine(Application.StartupPath, "Yedekler")
                };
            }
        }

        private void AyarlariKaydet()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(ayarlar);
                File.WriteAllText(Path.Combine(Application.StartupPath, YEDEKLEME_AYARLARI_DOSYASI), json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void YedeklemeGecmisiniYukle()
        {
            try
            {
                string gecmisYolu = Path.Combine(Application.StartupPath, YEDEKLEME_GECMISI_DOSYASI);
                if (File.Exists(gecmisYolu))
                {
                    string json = File.ReadAllText(gecmisYolu);
                    yedeklemeGecmisi = System.Text.Json.JsonSerializer.Deserialize<List<YedeklemeKaydi>>(json);
                    GuncelSonYedeklemeTarihiniGoster(); // Yedekleme geçmişi yüklendiğinde tarihi göster
                }
                else
                {
                    yedeklemeGecmisi = new List<YedeklemeKaydi>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedekleme geçmişi yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                yedeklemeGecmisi = new List<YedeklemeKaydi>();
            }
        }

        private void YedeklemeGecmisiniKaydet()
        {
            try
            {
                string gecmisYolu = Path.Combine(Application.StartupPath, YEDEKLEME_GECMISI_DOSYASI);
                string json = System.Text.Json.JsonSerializer.Serialize(yedeklemeGecmisi);
                File.WriteAllText(gecmisYolu, json);
                GuncelSonYedeklemeTarihiniGoster(); // Yedekleme geçmişi kaydedildikten sonra tarihi güncelle
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedekleme geçmişi kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Text = "Veritabanı Yedekleme";
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void SonYedeklemeTarihiniGoster()
        {
            try
            {
                string sonYedeklemeDosyasi = Path.Combine(Application.StartupPath, "son_yedekleme.txt");
                if (File.Exists(sonYedeklemeDosyasi))
                {
                    string tarih = File.ReadAllText(sonYedeklemeDosyasi);
                    lblSonYedeklemeTarihi.Text = $"Son Yedekleme: {tarih}";
                }
                else
                {
                    lblSonYedeklemeTarihi.Text = "Son Yedekleme: Henüz yedekleme yapılmadı";
                }
            }
            catch
            {
                lblSonYedeklemeTarihi.Text = "Son Yedekleme: Bilgi alınamadı";
            }
        }

        private void FormTasarimOlustur()
        {
            try
            {
                this.Text = "Veritabanı Yedekleme";
                this.Size = new Size(800, 600);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.BackColor = Color.FromArgb(18, 18, 18);
                this.ForeColor = Color.White;

                // Ana Panel
                Panel pnlAna = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(18, 18, 18)
                };

                // Son Yedekleme Tarihi Label
                lblSonYedeklemeTarihi = new Label
                {
                    Text = "Son Yedekleme: -",
                    AutoSize = true,
                    Location = new Point(20, 20),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };
                pnlAna.Controls.Add(lblSonYedeklemeTarihi);

                // Yedekleme Butonu
                Button btnYedekle = new Button
                {
                    Text = "💾 YEDEKLE",
                    Size = new Size(200, 45),
                    Location = new Point(20, 80),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 183, 195),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnYedekle.FlatAppearance.BorderSize = 0;
                btnYedekle.Click += BtnYedekle_Click;
                pnlAna.Controls.Add(btnYedekle);

                // Geri Yükleme Butonu
                Button btnGeriYukle = new Button
                {
                    Text = "🔄 GERİ YÜKLE",
                    Size = new Size(200, 45),
                    Location = new Point(20, 140),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnGeriYukle.FlatAppearance.BorderSize = 0;
                btnGeriYukle.Click += BtnGeriYukle_Click;
                pnlAna.Controls.Add(btnGeriYukle);

                // Excel Aktarım Butonu
                Button btnExcel = new Button
                {
                    Text = "📊 EXCEL'E AKTAR",
                    Size = new Size(200, 45),
                    Location = new Point(20, 200),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnExcel.FlatAppearance.BorderSize = 0;
                btnExcel.Click += BtnExcel_Click;
                pnlAna.Controls.Add(btnExcel);

                // Ayarlar Butonu
                Button btnAyarlar = new Button
                {
                    Text = "⚙️ AYARLAR",
                    Size = new Size(200, 45),
                    Location = new Point(20, 260),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnAyarlar.FlatAppearance.BorderSize = 0;
                btnAyarlar.Click += BtnAyarlar_Click;
                pnlAna.Controls.Add(btnAyarlar);

                // Yedekleme Geçmişi Butonu
                Button btnGecmis = new Button
                {
                    Text = "📋 GEÇMİŞ",
                    Size = new Size(200, 45),
                    Location = new Point(20, 320),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnGecmis.FlatAppearance.BorderSize = 0;
                btnGecmis.Click += BtnGecmis_Click;
                pnlAna.Controls.Add(btnGecmis);

                this.Controls.Add(pnlAna);
                GuncelSonYedeklemeTarihiniGoster();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form tasarımı oluşturulurken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GuncelSonYedeklemeTarihiniGoster()
        {
            try
            {
                if (yedeklemeGecmisi != null && yedeklemeGecmisi.Count > 0)
                {
                    var sonYedekleme = yedeklemeGecmisi.OrderByDescending(y => y.Tarih).FirstOrDefault();
                    if (sonYedekleme != null)
                    {
                        lblSonYedeklemeTarihi.Text = $"Son Yedekleme: {sonYedekleme.Tarih:dd.MM.yyyy HH:mm}";
                        lblSonYedeklemeTarihi.ForeColor = Color.Lime;
                    }
                }
                else
                {
                    lblSonYedeklemeTarihi.Text = "Son Yedekleme: Henüz yedekleme yapılmadı";
                    lblSonYedeklemeTarihi.ForeColor = Color.Yellow;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Son yedekleme tarihi gösterilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool VeritabaniBütünlükKontrol()
        {
            try
            {
                string connectionString = $"Data Source={Path.Combine(Application.StartupPath, dbDosyasi)};Version=3;";
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("PRAGMA integrity_check;", connection))
                    {
                        string result = command.ExecuteScalar().ToString();
                        return result == "ok";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı kontrol edilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void BtnYedekle_Click(object sender, EventArgs e)
        {
            try
            {
                // Veritabanı bütünlük kontrolü
                if (!VeritabaniBütünlükKontrol())
                {
                    if (MessageBox.Show("Veritabanında bütünlük sorunu tespit edildi. Yine de yedeklemek istiyor musunuz?",
                        "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "SQLite Veritabanı (*.db)|*.db",
                    FileName = $"StokTakip_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string kaynakDosya = Path.Combine(Application.StartupPath, dbDosyasi);
                    
                    // Yedekleme boyutu kontrolü
                    FileInfo sourceFile = new FileInfo(kaynakDosya);
                    if (sourceFile.Length > 1024 * 1024 * 100) // 100MB üzeri
                    {
                        if (MessageBox.Show("Veritabanı dosyası 100MB'dan büyük. Yedekleme uzun sürebilir. Devam etmek istiyor musunuz?",
                            "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        {
                            return;
                        }
                    }

                    // İlerleme formu
                    using (var progress = new Form())
                    {
                        progress.Text = "Yedekleniyor...";
                        progress.StartPosition = FormStartPosition.CenterScreen;
                        progress.FormBorderStyle = FormBorderStyle.FixedDialog;
                        progress.ControlBox = false;
                        progress.Size = new Size(300, 75);

                        var progressBar = new ProgressBar
                        {
                            Style = ProgressBarStyle.Marquee,
                            Size = new Size(250, 23),
                            Location = new Point(25, 15)
                        };
                        progress.Controls.Add(progressBar);

                        // Yedekleme işlemini arka planda yap
                        var backgroundWorker = new System.ComponentModel.BackgroundWorker();
                        backgroundWorker.DoWork += (s, args) =>
                        {
                            File.Copy(kaynakDosya, saveDialog.FileName, true);
                        };

                        backgroundWorker.RunWorkerCompleted += (s, args) =>
                        {
                            progress.Close();
                            if (args.Error == null)
                            {
                                // Son yedekleme tarihini kaydet
                                string sonYedeklemeDosyasi = Path.Combine(Application.StartupPath, "son_yedekleme.txt");
                                File.WriteAllText(sonYedeklemeDosyasi, DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                                SonYedeklemeTarihiniGoster();

                                MessageBox.Show("Veritabanı başarıyla yedeklendi.", "Başarılı",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show($"Yedekleme sırasında bir hata oluştu: {args.Error.Message}", "Hata",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };

                        backgroundWorker.RunWorkerAsync();
                        progress.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedekleme sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGeriYukle_Click(object sender, EventArgs e)
        {
            try
            {
                // Yetki kontrolü
                if (MessageBox.Show("Bu işlem mevcut veritabanını tamamen değiştirecek ve geri alınamaz. " +
                    "Devam etmek için yönetici yetkisi gereklidir. Devam etmek istiyor musunuz?",
                    "Yetki Kontrolü", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }

                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "SQLite Veritabanı (*.db)|*.db",
                    Title = "Yedek Dosyasını Seçin"
                };

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen dosyanın geçerliliğini kontrol et
                    try
                    {
                        using (var connection = new SQLiteConnection($"Data Source={openDialog.FileName};Version=3;"))
                        {
                            connection.Open();
                            using (var command = new SQLiteCommand("SELECT count(*) FROM sqlite_master;", connection))
                            {
                                command.ExecuteScalar();
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Seçilen dosya geçerli bir SQLite veritabanı değil.", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (MessageBox.Show("Mevcut veritabanı yedekten geri yüklenecek. Bu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                        "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        string hedefDosya = Path.Combine(Application.StartupPath, dbDosyasi);
                        
                        // Mevcut veritabanının yedeğini al
                        string yedekDosya = Path.Combine(Application.StartupPath, 
                            $"OtomatikYedek_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                        File.Copy(hedefDosya, yedekDosya, true);

                        File.Copy(openDialog.FileName, hedefDosya, true);

                        MessageBox.Show("Veritabanı başarıyla geri yüklendi. Uygulamayı yeniden başlatmanız gerekiyor.", 
                            "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        Application.Restart();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geri yükleme sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExcel_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                    FileName = $"StokTakip_Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var package = new ExcelPackage(new FileInfo(saveDialog.FileName)))
                    {
                        // Ürünler Sayfası
                        var wsUrunler = package.Workbook.Worksheets.Add("Ürünler");
                        var dtUrunler = GetTableData(@"
                            SELECT 
                                u.Id as UrunId, 
                                u.UrunAdi, 
                                k.KategoriAdi, 
                                u.Miktar as StokMiktari, 
                                u.AlisFiyati, 
                                u.SatisFiyati, 
                                u.KritikStok
                            FROM Urunler u
                            LEFT JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                            ORDER BY u.Id");
                        ExportToExcel(wsUrunler, dtUrunler);

                        // Satışlar Sayfası
                        var wsSatislar = package.Workbook.Worksheets.Add("Satışlar");
                        var dtSatislar = GetTableData(@"
                            SELECT 
                                s.SatisId,
                                s.SatisTarihi,
                                s.MusteriAdi,
                                u.UrunAdi,
                                sd.Miktar,
                                sd.BirimFiyat,
                                sd.ToplamFiyat as ToplamTutar
                            FROM Satislar s
                            JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                            JOIN Urunler u ON sd.UrunId = u.Id
                            ORDER BY s.SatisTarihi DESC");
                        ExportToExcel(wsSatislar, dtSatislar);

                        // İadeler Sayfası
                        var wsIadeler = package.Workbook.Worksheets.Add("İadeler");
                        var dtIadeler = GetTableData(@"
                            SELECT 
                                i.IadeId,
                                i.IadeTarihi,
                                u.UrunAdi,
                                i.Miktar,
                                i.BirimFiyat,
                                i.ToplamTutar
                            FROM Iadeler i
                            JOIN Urunler u ON i.UrunId = u.Id
                            ORDER BY i.IadeTarihi DESC");
                        ExportToExcel(wsIadeler, dtIadeler);

                        package.Save();
                    }

                    MessageBox.Show("Veriler başarıyla Excel'e aktarıldı.", "Başarılı",
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

        private DataTable GetTableData(string query)
        {
            var dt = new DataTable();
            string connectionString = $"Data Source={Path.Combine(Application.StartupPath, dbDosyasi)};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        adapter.Fill(dt);
                    }
                }
            }

            return dt;
        }

        private void ExportToExcel(ExcelWorksheet worksheet, DataTable dataTable)
        {
            try
            {
                // Başlıkları ekle
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = dataTable.Columns[col].ColumnName;
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 183, 195));
                    worksheet.Cells[1, col + 1].Style.Font.Color.SetColor(Color.White);
                }

                // Verileri ekle
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var value = dataTable.Rows[row][col];
                        
                        // Tarih değerlerini kontrol et ve formatla
                        if (value != null && value != DBNull.Value)
                        {
                            if (dataTable.Columns[col].ColumnName.ToLower().Contains("tarih"))
                            {
                                if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
                                {
                                    worksheet.Cells[row + 2, col + 1].Value = dateValue;
                                    worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                                }
                            }
                            else if (value is decimal || value is double || value is float)
                            {
                                worksheet.Cells[row + 2, col + 1].Value = Convert.ToDouble(value);
                                worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "#,##0.00";
                            }
                            else
                            {
                                worksheet.Cells[row + 2, col + 1].Value = value;
                            }
                        }
                    }
                }

                // Sütun genişliklerini otomatik ayarla
                worksheet.Cells.AutoFitColumns();

                // Tablo formatını uygula
                if (dataTable.Rows.Count > 0)
                {
                    var range = worksheet.Cells[1, 1, dataTable.Rows.Count + 1, dataTable.Columns.Count];
                    
                    // Kenarlıkları ayarla
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    // Alternatif satır renklendirmesi
                    for (int row = 2; row <= dataTable.Rows.Count + 1; row++)
                    {
                        if (row % 2 == 0)
                        {
                            var rowRange = worksheet.Cells[row, 1, row, dataTable.Columns.Count];
                            rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            rowRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel formatı oluşturulurken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAyarlar_Click(object sender, EventArgs e)
        {
            using (var ayarlarForm = new Form())
            {
                ayarlarForm.Text = "Yedekleme Ayarları";
                ayarlarForm.Size = new Size(550, 620);
                ayarlarForm.StartPosition = FormStartPosition.CenterParent;
                ayarlarForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                ayarlarForm.MaximizeBox = false;
                ayarlarForm.MinimizeBox = false;
                ayarlarForm.BackColor = Color.FromArgb(18, 18, 18);
                ayarlarForm.ForeColor = Color.White;
                ayarlarForm.Padding = new Padding(20);

                // Panel oluştur (scrollbar için)
                var panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.FromArgb(18, 18, 18)
                };

                // Başlık
                var lblBaslik = new Label
                {
                    Text = "YEDEKLEME AYARLARI",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 183, 195),
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                // Otomatik Yedekleme Grubu
                var grpOtomatik = new GroupBox
                {
                    Text = "Otomatik Yedekleme",
                    Location = new Point(20, 60),
                    Size = new Size(480, 110),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Padding = new Padding(10)
                };

                var chkOtomatik = new CheckBox
                {
                    Text = "Otomatik yedeklemeyi aktif hale getir",
                    Checked = ayarlar.OtomatikYedeklemeAktif,
                    Location = new Point(20, 35),
                    Size = new Size(440, 24),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.FromArgb(30, 30, 30)
                };

                var lblOtomatikAciklama = new Label
                {
                    Text = "Otomatik yedekleme, veritabanınızı belirlediğiniz aralıklarla otomatik olarak yedekler.",
                    Location = new Point(20, 65),
                    Size = new Size(440, 35),
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9)
                };

                grpOtomatik.Controls.AddRange(new Control[] { chkOtomatik, lblOtomatikAciklama });

                // Yedekleme Aralığı Grubu
                var grpAralik = new GroupBox
                {
                    Text = "Yedekleme Sıklığı",
                    Location = new Point(20, 180),
                    Size = new Size(480, 110),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Padding = new Padding(10)
                };

                var lblAralik = new Label
                {
                    Text = "Her",
                    Location = new Point(20, 40),
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var numAralik = new NumericUpDown
                {
                    Location = new Point(60, 38),
                    Size = new Size(70, 25),
                    Minimum = 1,
                    Maximum = 168,
                    Value = ayarlar.YedeklemeAraligi,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var lblSaatSonra = new Label
                {
                    Text = "saatte bir yedekle",
                    Location = new Point(140, 40),
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var lblAralikAciklama = new Label
                {
                    Text = "Örnek: 24 saat seçerseniz, günde bir kez yedekleme yapılır.",
                    Location = new Point(20, 70),
                    Size = new Size(440, 30),
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9)
                };

                grpAralik.Controls.AddRange(new Control[] { lblAralik, numAralik, lblSaatSonra, lblAralikAciklama });

                // Yedek Saklama Grubu
                var grpSaklama = new GroupBox
                {
                    Text = "Yedek Dosyaları",
                    Location = new Point(20, 300),
                    Size = new Size(480, 110),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Padding = new Padding(10)
                };

                var lblSaklama = new Label
                {
                    Text = "Son",
                    Location = new Point(20, 40),
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var numSaklama = new NumericUpDown
                {
                    Location = new Point(60, 38),
                    Size = new Size(70, 25),
                    Minimum = 1,
                    Maximum = 365,
                    Value = ayarlar.EskiYedekSaklamaSuresi,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var lblGunluk = new Label
                {
                    Text = "günlük yedekleri sakla",
                    Location = new Point(140, 40),
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10)
                };

                var lblSaklamaAciklama = new Label
                {
                    Text = "Eski yedekler otomatik olarak silinir. Önemli yedekleri farklı bir konuma taşıyın.",
                    Location = new Point(20, 70),
                    Size = new Size(440, 30),
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9)
                };

                grpSaklama.Controls.AddRange(new Control[] { lblSaklama, numSaklama, lblGunluk, lblSaklamaAciklama });

                // Yedekleme Dizini Grubu
                var grpDizin = new GroupBox
                {
                    Text = "Yedekleme Konumu",
                    Location = new Point(20, 420),
                    Size = new Size(480, 120),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Padding = new Padding(10)
                };

                var txtDizin = new TextBox
                {
                    Location = new Point(20, 35),
                    Size = new Size(350, 25),
                    Text = ayarlar.YedeklemeDizini,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var btnDizinSec = new Button
                {
                    Text = "Gözat",
                    Location = new Point(380, 34),
                    Size = new Size(80, 27),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnDizinSec.FlatAppearance.BorderSize = 0;

                var lblDizinAciklama = new Label
                {
                    Text = "Yedekleme dosyalarının kaydedileceği klasörü seçin.\nVarsayılan: Uygulama klasörü içindeki 'Yedekler' dizini",
                    Location = new Point(20, 70),
                    Size = new Size(440, 40),
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9)
                };

                btnDizinSec.Click += (s, ev) =>
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        if (Directory.Exists(txtDizin.Text))
                            fbd.SelectedPath = txtDizin.Text;

                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            txtDizin.Text = fbd.SelectedPath;
                        }
                    }
                };

                grpDizin.Controls.AddRange(new Control[] { txtDizin, btnDizinSec, lblDizinAciklama });

                // Kaydetme Butonu
                var btnKaydet = new Button
                {
                    Text = "AYARLARI KAYDET",
                    Size = new Size(200, 40),
                    Location = new Point(300, 550),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 183, 195),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnKaydet.FlatAppearance.BorderSize = 0;

                btnKaydet.Click += (s, ev) =>
                {
                    // Dizin kontrolü
                    if (!Directory.Exists(txtDizin.Text))
                    {
                        try
                        {
                            Directory.CreateDirectory(txtDizin.Text);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Yedekleme dizini oluşturulamadı: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    ayarlar.OtomatikYedeklemeAktif = chkOtomatik.Checked;
                    ayarlar.YedeklemeAraligi = (int)numAralik.Value;
                    ayarlar.EskiYedekSaklamaSuresi = (int)numSaklama.Value;
                    ayarlar.YedeklemeDizini = txtDizin.Text;

                    AyarlariKaydet();
                    otomatikYedeklemeTimer.Enabled = ayarlar.OtomatikYedeklemeAktif;

                    MessageBox.Show("Ayarlarınız başarıyla kaydedildi.", "Başarılı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ayarlarForm.Close();
                };

                // Kontrolleri panele ekle
                panel.Controls.AddRange(new Control[] {
                    lblBaslik,
                    grpOtomatik,
                    grpAralik,
                    grpSaklama,
                    grpDizin,
                    btnKaydet
                });

                ayarlarForm.Controls.Add(panel);
                ayarlarForm.ShowDialog();
            }
        }

        private void BtnGecmis_Click(object sender, EventArgs e)
        {
            using (var gecmisForm = new Form())
            {
                gecmisForm.Text = "Yedekleme Geçmişi";
                gecmisForm.Size = new Size(800, 400);
                gecmisForm.StartPosition = FormStartPosition.CenterParent;
                gecmisForm.BackColor = Color.FromArgb(18, 18, 18);

                var listView = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White
                };

                listView.Columns.AddRange(new[]
                {
                    new ColumnHeader { Text = "Tarih", Width = 150 },
                    new ColumnHeader { Text = "Dosya Adı", Width = 200 },
                    new ColumnHeader { Text = "Boyut (MB)", Width = 100 },
                    new ColumnHeader { Text = "Durum", Width = 100 },
                    new ColumnHeader { Text = "Açıklama", Width = 200 }
                });

                foreach (var kayit in yedeklemeGecmisi.OrderByDescending(y => y.Tarih))
                {
                    var item = new ListViewItem(new[]
                    {
                        kayit.Tarih.ToString("dd.MM.yyyy HH:mm:ss"),
                        kayit.DosyaAdi,
                        (kayit.DosyaBoyutu / 1024.0 / 1024.0).ToString("N2"),
                        kayit.Basarili ? "Başarılı" : "Başarısız",
                        kayit.Aciklama
                    });

                    item.ForeColor = kayit.Basarili ? Color.White : Color.Red;
                    listView.Items.Add(item);
                }

                gecmisForm.Controls.Add(listView);
                gecmisForm.ShowDialog();
            }
        }
    }
} 