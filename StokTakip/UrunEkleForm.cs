using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace StokTakip
{
    public partial class UrunEkleForm : Form
    {
        private readonly int? duzenlenecekUrunId;
        private readonly string dbDosyasi = "StokTakip.db";
        private PictureBox? pbUrunResmi;
        private string secilenResimYolu = "";
        private byte[]? resimBytes;
        private IContainer? components;
        private bool isDisposing = false;

        // Form kontrolleri
        private TextBox? txtUrunAdi;
        private TextBox? txtAciklama;
        private TextBox? txtTedarikci;
        private TextBox? txtRafKodu;
        private TextBox? txtMiktar;
        private TextBox? txtMinStok;
        private TextBox? txtKritikStok;
        private TextBox? txtAlisFiyati;
        private TextBox? txtSatisFiyati;
        private ComboBox? cmbBirim;
        private ComboBox? cmbKategori;
        private FlowLayoutPanel? flowPanel;
        private Button? btnRenkSec;
        private Panel? pnlSecilenRenk;
        private string secilenRenk = "";

        public UrunEkleForm(int? urunId = null)
        {
            duzenlenecekUrunId = urunId;
            InitializeComponent();
            FormTasarimOlustur();
            KategorileriYukle();

            // Form yüklendiğinde başlık çubuğunu siyah yap
            this.HandleCreated += (s, e) => 
            {
                DarkModeHelper.UseImmersiveDarkMode(this.Handle, true);
            };

            // Form yüklendiğinde ürün bilgilerini doldur
            if (duzenlenecekUrunId.HasValue)
            {
                this.Load += (s, e) => UrunBilgileriniDoldur();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposing)
            {
                isDisposing = true;
                if (disposing)
                {
                    // Managed kaynakları temizle
                    if (components != null)
                    {
                        components.Dispose();
                    }

                    if (pbUrunResmi?.Image != null)
                    {
                        pbUrunResmi.Image.Dispose();
                        pbUrunResmi.Image = null;
                    }

                    // Form kontrollerini temizle
                    foreach (Control control in this.Controls)
                    {
                        control.Dispose();
                    }
                }

                // Unmanaged kaynakları temizle
                base.Dispose(disposing);
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Text = "Ürün Ekle";
        }

        private void FormTasarimOlustur()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            
            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20),
                AutoScroll = true
            };

            // Sol Panel (Form Alanları)
            Panel pnlSol = new Panel
            {
                Width = 500,
                Height = 800,
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20),
                AutoScroll = true
            };

            // Sağ Panel (Resim Alanı)
            Panel pnlSag = new Panel
            {
                Width = 400,
                Height = 500,
                Location = new Point(540, 20),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Resim Alanı
            pbUrunResmi = new PictureBox
            {
                Width = 360,
                Height = 360,
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(40, 40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Resim Seç Butonu
            Button btnResimSec = new Button
            {
                Text = "🖼️ Resim Seç",
                Size = new Size(360, 45),
                Location = new Point(20, 400),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            btnResimSec.FlatAppearance.BorderSize = 0;
            btnResimSec.Click += BtnResimSec_Click;

            // Resim Kaldır Butonu
            Button btnResimKaldir = new Button
            {
                Text = "🗑️ Resmi Kaldır",
                Size = new Size(360, 45),
                Location = new Point(20, 450),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            btnResimKaldir.FlatAppearance.BorderSize = 0;
            btnResimKaldir.Click += BtnResimKaldir_Click;

            // Form elemanları için FlowLayoutPanel
            flowPanel = new FlowLayoutPanel
            {
                Width = 460,
                Height = 700,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 0, 0, 60)
            };

            // Form Alanlarını Oluştur
            OlusturFormAlanlari();

            // Butonlar için panel
            Panel pnlButonlar = new Panel
            {
                Width = 460,
                Height = 120,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };

            // Kaydet butonu
            Button btnKaydet = new Button
            {
                Name = "btnKaydet",
                Text = duzenlenecekUrunId.HasValue ? "💾 GÜNCELLE" : "💾 KAYDET",
                Width = 440,
                Height = 45,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 10)
            };
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.Click += BtnKaydet_Click;

            // İptal butonu
            Button btnIptal = new Button
            {
                Name = "btnIptal",
                Text = "❌ İPTAL",
                Width = 440,
                Height = 45,
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIptal.FlatAppearance.BorderSize = 0;
            btnIptal.Click += BtnIptal_Click;

            // Butonları panele ekle
            pnlButonlar.Controls.Add(btnKaydet);
            pnlButonlar.Controls.Add(btnIptal);

            // Resim kontrollerini sağ panele ekle
            pnlSag.Controls.AddRange(new Control[] { pbUrunResmi, btnResimSec, btnResimKaldir });

            // Flow paneli sol panele ekle
            pnlSol.Controls.Add(flowPanel);

            // Buton panelini sol panele ekle
            pnlSol.Controls.Add(pnlButonlar);

            // Panelleri ana panele ekle
            pnlAna.Controls.AddRange(new Control[] { pnlSol, pnlSag });

            // Ana paneli forma ekle
            this.Controls.Add(pnlAna);

            // Form boyutunu ayarla
            this.ClientSize = new Size(960, 850);
        }

        private void OlusturFormAlanlari()
        {
            if (flowPanel == null) return;

            // Kategori ComboBox'ı
            Panel pnlKategori = new Panel
            {
                Width = 440,
                Height = 65,
                Margin = new Padding(0, 0, 0, 5)
            };

            Label lblKategori = new Label
            {
                Text = "Kategori *",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 0),
                AutoSize = true
            };

            cmbKategori = new ComboBox
            {
                Width = 440,
                Height = 35,
                Location = new Point(0, 25),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            pnlKategori.Controls.AddRange(new Control[] { lblKategori, cmbKategori });
            flowPanel.Controls.Add(pnlKategori);

            // Form alanları tanımlamaları
            var alanlar = new (string Name, string Placeholder, bool IsNumeric, bool Required)[]
            {
                ("UrunAdi", "Ürün Adı", false, true),
                ("Aciklama", "Açıklama", false, false),
                ("Tedarikci", "Tedarikçi", false, false),
                ("RafKodu", "Raf Kodu", false, false),
                ("Miktar", "Miktar", true, true),
                ("MinStok", "Min. Stok", true, false),
                ("KritikStok", "Kritik Stok", true, false),
                ("AlisFiyati", "Alış Fiyatı", true, true),
                ("SatisFiyati", "Satış Fiyatı", true, true)
            };

            foreach (var (name, placeholder, isNumeric, required) in alanlar)
            {
                Panel pnlAlan = new Panel
                {
                    Width = 440,
                    Height = 65,
                    Margin = new Padding(0, 0, 0, 5)
                };

                Label lbl = new Label
                {
                    Text = placeholder + (required ? " *" : ""),
                    ForeColor = Color.FromArgb(200, 200, 200),
                    Font = new Font("Segoe UI", 10),
                    Location = new Point(0, 0),
                    AutoSize = true
                };

                TextBox txt = new TextBox
                {
                    Width = 440,
                    Height = 35,
                    Location = new Point(0, 25),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.Gray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = new TextBoxInfo 
                    { 
                        Placeholder = placeholder,
                        IsNumeric = isNumeric,
                        IsRequired = required
                    }
                };

                // Sayısal alanlar için özel ayarlar
                if (isNumeric)
                {
                    txt.KeyPress += (s, e) =>
                    {
                        // Sadece rakam, backspace ve virgül girişine izin ver
                        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && 
                            (e.KeyChar != ',' || (e.KeyChar == ',' && (s as TextBox)?.Text.Contains(',') == true)))
                        {
                            e.Handled = true;
                        }
                    };

                    txt.TextChanged += (s, e) =>
                    {
                        if (s is TextBox textBox)
                        {
                            // Virgülden sonra en fazla 2 basamak olmasını sağla
                            if (textBox.Text.Contains(","))
                            {
                                string[] parts = textBox.Text.Split(',');
                                if (parts.Length > 1 && parts[1].Length > 2)
                                {
                                    textBox.Text = parts[0] + "," + parts[1].Substring(0, 2);
                                    textBox.SelectionStart = textBox.Text.Length;
                                }
                            }
                        }
                    };
                }

                // Placeholder davranışı
                txt.GotFocus += (s, e) =>
                {
                    if (s is TextBox textBox && textBox.ForeColor == Color.Gray)
                    {
                        textBox.Text = "";
                        textBox.ForeColor = Color.White;
                    }
                };

                txt.LostFocus += (s, e) =>
                {
                    if (s is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        var info = textBox.Tag as TextBoxInfo;
                        textBox.Text = info?.Placeholder ?? "";
                        textBox.ForeColor = Color.Gray;
                    }
                };

                // Form kontrollerini sınıf değişkenlerine ata
                switch (name)
                {
                    case "UrunAdi": txtUrunAdi = txt; break;
                    case "Aciklama": txtAciklama = txt; break;
                    case "Tedarikci": txtTedarikci = txt; break;
                    case "RafKodu": txtRafKodu = txt; break;
                    case "Miktar": txtMiktar = txt; break;
                    case "MinStok": txtMinStok = txt; break;
                    case "KritikStok": txtKritikStok = txt; break;
                    case "AlisFiyati": txtAlisFiyati = txt; break;
                    case "SatisFiyati": txtSatisFiyati = txt; break;
                }

                pnlAlan.Controls.AddRange(new Control[] { lbl, txt });
                flowPanel.Controls.Add(pnlAlan);
            }

            // Birim ComboBox'ı ekle
            Panel pnlBirim = new Panel
            {
                Width = 440,
                Height = 65,
                Margin = new Padding(0, 0, 0, 5)
            };

            Label lblBirim = new Label
            {
                Text = "Birim",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 0),
                AutoSize = true
            };

            cmbBirim = new ComboBox
            {
                Width = 440,
                Height = 35,
                Location = new Point(0, 25),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBirim.Items.AddRange(new string[] { "Adet", "Kg", "Lt", "Mt" });
            cmbBirim.SelectedIndex = 0;

            pnlBirim.Controls.AddRange(new Control[] { lblBirim, cmbBirim });
            flowPanel.Controls.Add(pnlBirim);

            // Renk seçici panel ve buton
            Panel pnlRenkSecici = new Panel
            {
                Width = 440,
                Height = 80,
                Margin = new Padding(0, 10, 0, 10),
                BackColor = Color.FromArgb(30, 30, 30)
            };

            Label lblRenk = new Label
            {
                Text = "Ürün Rengi",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(0, 0),
                AutoSize = true
            };

            pnlSecilenRenk = new Panel
            {
                Width = 30,
                Height = 30,
                Location = new Point(0, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            btnRenkSec = new Button
            {
                Text = "🎨 Renk Seç",
                Width = 395,
                Height = 30,
                Location = new Point(45, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btnRenkSec.FlatAppearance.BorderSize = 0;
            btnRenkSec.Click += BtnRenkSec_Click;

            pnlRenkSecici.Controls.AddRange(new Control[] { lblRenk, pnlSecilenRenk, btnRenkSec });
            flowPanel.Controls.Add(pnlRenkSecici);
        }

        private void UrunBilgileriniDoldur()
        {
            try
            {
                string projeKlasoru = Application.StartupPath;
                string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                {
                    baglanti.Open();
                    string sorgu = "SELECT * FROM Urunler WHERE Id = @UrunId";
                    using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                    {
                        cmd.Parameters.AddWithValue("@UrunId", duzenlenecekUrunId.Value);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                // Form kontrollerini bul ve değerleri doldur
                                var controls = this.Controls[0].Controls;
                                
                                var txtUrunAdi = controls.Find("txtUrunAdi", true).FirstOrDefault() as TextBox;
                                var txtAciklama = controls.Find("txtAciklama", true).FirstOrDefault() as TextBox;
                                var txtTedarikci = controls.Find("txtTedarikci", true).FirstOrDefault() as TextBox;
                                var txtRafKodu = controls.Find("txtRafKodu", true).FirstOrDefault() as TextBox;
                                var txtMiktar = controls.Find("txtMiktar", true).FirstOrDefault() as TextBox;
                                var txtMinStok = controls.Find("txtMinStok", true).FirstOrDefault() as TextBox;
                                var txtKritikStok = controls.Find("txtKritikStok", true).FirstOrDefault() as TextBox;
                                var txtAlisFiyati = controls.Find("txtAlisFiyati", true).FirstOrDefault() as TextBox;
                                var txtSatisFiyati = controls.Find("txtSatisFiyati", true).FirstOrDefault() as TextBox;
                                var cmbBirim = controls.Find("cmbBirim", true).FirstOrDefault() as ComboBox;
                                pbUrunResmi = controls.Find("pbUrunResmi", true).FirstOrDefault() as PictureBox;

                                if (txtUrunAdi != null) txtUrunAdi.Text = dr["UrunAdi"].ToString();
                                if (txtAciklama != null) txtAciklama.Text = dr["Aciklama"].ToString();
                                if (txtTedarikci != null) txtTedarikci.Text = dr["Tedarikci"].ToString();
                                if (txtRafKodu != null) txtRafKodu.Text = dr["RafKodu"].ToString();
                                if (txtMiktar != null) txtMiktar.Text = dr["Miktar"].ToString();
                                if (txtMinStok != null) txtMinStok.Text = dr["MinStok"].ToString();
                                if (txtKritikStok != null) txtKritikStok.Text = dr["KritikStok"].ToString();
                                if (txtAlisFiyati != null) txtAlisFiyati.Text = Convert.ToDecimal(dr["AlisFiyati"]).ToString("N2");
                                if (txtSatisFiyati != null) txtSatisFiyati.Text = Convert.ToDecimal(dr["SatisFiyati"]).ToString("N2");
                                if (cmbBirim != null) cmbBirim.SelectedItem = dr["Birim"].ToString();

                                if (pbUrunResmi != null && !dr.IsDBNull(dr.GetOrdinal("UrunResmi")))
                                {
                                    resimBytes = (byte[])dr["UrunResmi"];
                                    using (var ms = new MemoryStream(resimBytes))
                                    {
                                        pbUrunResmi.Image?.Dispose();
                                        pbUrunResmi.Image = Image.FromStream(ms);
                                    }
                                }

                                // Kaydet butonunu güncelle
                                var btnKaydet = controls.Find("btnKaydet", true).FirstOrDefault() as Button;
                                if (btnKaydet != null)
                                {
                                    btnKaydet.Text = "💾 GÜNCELLE";
                                    btnKaydet.Tag = duzenlenecekUrunId;
                                }

                                // Renk bilgisini doldur
                                if (dr["Renk"] != DBNull.Value)
                                {
                                    secilenRenk = dr["Renk"].ToString();
                                    if (!string.IsNullOrEmpty(secilenRenk))
                                    {
                                        try
                                        {
                                            pnlSecilenRenk.BackColor = ColorTranslator.FromHtml(secilenRenk);
                                        }
                                        catch { /* Renk dönüştürme hatası olursa varsayılan renk kalır */ }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün bilgileri yüklenirken hata olu��tu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void KategorileriYukle()
        {
            if (cmbKategori == null) return;

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={Program.DbPath};Version=3;"))
                {
                    baglanti.Open();
                    string sorgu = "SELECT KategoriId, KategoriAdi FROM Kategoriler ORDER BY KategoriAdi";
                    using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                    {
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            cmbKategori.Items.Clear();
                            cmbKategori.Items.Add(new ComboBoxItem { Id = 0, Text = "Kategori Seçiniz" });
                            
                            while (dr.Read())
                            {
                                cmbKategori.Items.Add(new ComboBoxItem
                                {
                                    Id = Convert.ToInt32(dr["KategoriId"]),
                                    Text = dr["KategoriAdi"].ToString() ?? ""
                                });
                            }

                            cmbKategori.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kategoriler yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UrunKaydet()
        {
            try
            {
                // Validasyonlar
                if (string.IsNullOrWhiteSpace(txtUrunAdi?.Text))
                {
                    MessageBox.Show("Ürün adı boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Veritabanı işlemleri
                string projeKlasoru = Application.StartupPath;
                string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                {
                    baglanti.Open();
                    using (var transaction = baglanti.BeginTransaction())
                    {
                        try
                        {
                            // ... existing code ...

                            transaction.Commit();
                            MessageBox.Show("Ürün başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Ana formdaki ürün listesini güncelle
                            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                            if (mainForm != null)
                            {
                                mainForm.YenileUrunListesi();
                            }
                            
                            // Formu kapat
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Kayıt işlemi sırasında bir hata oluştu: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResimSec()
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                    ofd.Title = "Ürün Resmi Seç";

                    if (ofd.ShowDialog() == DialogResult.OK && pbUrunResmi != null)
                    {
                        // Mevcut resmi temizle
                        if (pbUrunResmi.Image != null && pbUrunResmi.Image != DefaultImages.NoImage)
                        {
                            pbUrunResmi.Image.Dispose();
                            pbUrunResmi.Image = null;
                        }

                        // Yeni resmi yükle ve boyutlandır
                        using (var originalImage = Image.FromFile(ofd.FileName))
                        {
                            // Resmi yeniden boyutlandır
                            using (var resizedImage = new Bitmap(originalImage, new Size(800, 800)))
                            {
                                using (var ms = new MemoryStream())
                                {
                                    resizedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    resimBytes = ms.ToArray();
                                    using (var displayMs = new MemoryStream(resimBytes))
                                    {
                                        pbUrunResmi.Image = Image.FromStream(displayMs);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Resim yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResimKaldir()
        {
            try
            {
                if (pbUrunResmi?.Image != null && pbUrunResmi.Image != DefaultImages.NoImage)
                {
                    pbUrunResmi.Image.Dispose();
                    pbUrunResmi.Image = null;
                }
                resimBytes = null;
                if (pbUrunResmi != null)
                    pbUrunResmi.Image = DefaultImages.NoImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Resim kaldırılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnResimSec_Click(object? sender, EventArgs e)
        {
            ResimSec();
        }

        private void BtnResimKaldir_Click(object? sender, EventArgs e)
        {
            ResimKaldir();
        }

        private void BtnIptal_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void FormTemizle()
        {
            try
            {
                // Form kontrollerini bul
                var controls = this.Controls[0].Controls;
                var txtUrunAdi = controls.Find("txtUrunAdi", true).FirstOrDefault() as TextBox;
                var txtAciklama = controls.Find("txtAciklama", true).FirstOrDefault() as TextBox;
                var txtTedarikci = controls.Find("txtTedarikci", true).FirstOrDefault() as TextBox;
                var txtRafKodu = controls.Find("txtRafKodu", true).FirstOrDefault() as TextBox;
                var txtMiktar = controls.Find("txtMiktar", true).FirstOrDefault() as TextBox;
                var txtMinStok = controls.Find("txtMinStok", true).FirstOrDefault() as TextBox;
                var txtKritikStok = controls.Find("txtKritikStok", true).FirstOrDefault() as TextBox;
                var txtAlisFiyati = controls.Find("txtAlisFiyati", true).FirstOrDefault() as TextBox;
                var txtSatisFiyati = controls.Find("txtSatisFiyati", true).FirstOrDefault() as TextBox;
                var cmbBirim = controls.Find("cmbBirim", true).FirstOrDefault() as ComboBox;

                // TextBox'ları temizle
                if (txtUrunAdi != null) txtUrunAdi.Text = "";
                if (txtAciklama != null) txtAciklama.Text = "";
                if (txtTedarikci != null) txtTedarikci.Text = "";
                if (txtRafKodu != null) txtRafKodu.Text = "";
                if (txtMiktar != null) txtMiktar.Text = "0";
                if (txtMinStok != null) txtMinStok.Text = "0";
                if (txtKritikStok != null) txtKritikStok.Text = "0";
                if (txtAlisFiyati != null) txtAlisFiyati.Text = "0,00";
                if (txtSatisFiyati != null) txtSatisFiyati.Text = "0,00";

                // ComboBox'ı sıfırla
                if (cmbBirim != null) cmbBirim.SelectedIndex = 0;

                // Resmi temizle
                ResimKaldir();

                secilenRenk = "";
                if (pnlSecilenRenk != null)
                    pnlSecilenRenk.BackColor = Color.White;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form temizlenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRenkSec_Click(object sender, EventArgs e)
        {
            // Renk seçme paneli
            Form renkForm = new Form
            {
                Text = "Renk Seç",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true
            };

            FlowLayoutPanel pnlRenkler = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true
            };

            // Temel renkler
            var renkler = new[]
            {
                new { Ad = "Kırmızı", Deger = "#FF0000" },
                new { Ad = "Mavi", Deger = "#0000FF" },
                new { Ad = "Yeşil", Deger = "#00FF00" },
                new { Ad = "Sarı", Deger = "#FFFF00" },
                new { Ad = "Mor", Deger = "#800080" },
                new { Ad = "Turuncu", Deger = "#FFA500" },
                new { Ad = "Siyah", Deger = "#000000" },
                new { Ad = "Beyaz", Deger = "#FFFFFF" },
                new { Ad = "Gri", Deger = "#808080" },
                new { Ad = "Kahverengi", Deger = "#8B4513" }
            };

            foreach (var renk in renkler)
            {
                Panel pnlRenk = new Panel
                {
                    Width = 100,
                    Height = 80,
                    Margin = new Padding(10),
                    BackColor = ColorTranslator.FromHtml(renk.Deger),
                    Cursor = Cursors.Hand
                };

                Label lblRenkAdi = new Label
                {
                    Text = renk.Ad,
                    Dock = DockStyle.Bottom,
                    Height = 25,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 45)
                };

                pnlRenk.Controls.Add(lblRenkAdi);
                pnlRenk.Click += (s, ev) =>
                {
                    secilenRenk = renk.Deger;
                    pnlSecilenRenk.BackColor = ColorTranslator.FromHtml(renk.Deger);
                    renkForm.Close();
                };

                pnlRenkler.Controls.Add(pnlRenk);
            }

            renkForm.Controls.Add(pnlRenkler);
            renkForm.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Resmi temizle
                if (pbUrunResmi?.Image != null)
                {
                    pbUrunResmi.Image.Dispose();
                    pbUrunResmi.Image = null;
                }
                resimBytes = null;

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form kapatılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            try
            {
                // Form kontrollerinin null kontrolü
                if (txtUrunAdi == null || txtAciklama == null || txtTedarikci == null || 
                    txtRafKodu == null || txtMiktar == null || txtMinStok == null || 
                    txtKritikStok == null || txtAlisFiyati == null || txtSatisFiyati == null || 
                    cmbBirim == null || cmbKategori == null)
                {
                    MessageBox.Show("Gerekli form alanları bulunamadı!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Kategori seçimi kontrolü
                var secilenKategori = cmbKategori.SelectedItem as ComboBoxItem;
                if (secilenKategori == null || secilenKategori.Id == 0)
                {
                    MessageBox.Show("Lütfen bir kategori seçiniz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Zorunlu alanları kontrol et
                var eksikAlanlar = new List<string>();
                var zorunluAlanlar = new Dictionary<TextBox, string>
                {
                    { txtUrunAdi, "Ürün Adı" },
                    { txtMiktar, "Miktar" },
                    { txtAlisFiyati, "Alış Fiyatı" },
                    { txtSatisFiyati, "Satış Fiyatı" }
                };

                foreach (var alan in zorunluAlanlar)
                {
                    var info = alan.Key.Tag as TextBoxInfo;
                    if (string.IsNullOrWhiteSpace(alan.Key.Text) || 
                        alan.Key.Text == info?.Placeholder)
                    {
                        eksikAlanlar.Add(alan.Value);
                    }
                }

                if (eksikAlanlar.Any())
                {
                    MessageBox.Show($"Lütfen aşağıdaki alanları doldurunuz:\n\n{string.Join("\n", eksikAlanlar)}", 
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Sayısal değerleri kontrol et
                if (!int.TryParse(txtMiktar.Text.Trim(), out int miktar))
                {
                    MessageBox.Show("Miktar için geçerli bir tam sayı giriniz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtAlisFiyati.Text.Trim().Replace(".", ","), out decimal alisFiyati))
                {
                    MessageBox.Show("Alış Fiyatı için geçerli bir sayı giriniz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtSatisFiyati.Text.Trim().Replace(".", ","), out decimal satisFiyati))
                {
                    MessageBox.Show("Satış Fiyatı için geçerli bir sayı giriniz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int minStok = 0;
                if (!string.IsNullOrWhiteSpace(txtMinStok.Text))
                {
                    if (!int.TryParse(txtMinStok.Text.Trim(), out minStok))
                    {
                        MessageBox.Show("Min. Stok için geçerli bir tam sayı giriniz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                int kritikStok = 0;
                if (!string.IsNullOrWhiteSpace(txtKritikStok.Text))
                {
                    if (!int.TryParse(txtKritikStok.Text.Trim(), out kritikStok))
                    {
                        MessageBox.Show("Kritik Stok için geçerli bir tam sayı giriniz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Veritabanı işlemleri
                string projeKlasoru = Application.StartupPath;
                string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                {
                    baglanti.Open();
                    using (var transaction = baglanti.BeginTransaction())
                    {
                        try
                        {
                            string sorgu = duzenlenecekUrunId.HasValue
                                ? @"UPDATE Urunler SET 
                                    UrunAdi = @UrunAdi,
                                    KategoriId = @KategoriId,
                                    Aciklama = @Aciklama,
                                    Miktar = @Miktar,
                                    Birim = @Birim,
                                    Tedarikci = @Tedarikci,
                                    MinStok = @MinStok,
                                    KritikStok = @KritikStok,
                                    RafKodu = @RafKodu,
                                    AlisFiyati = @AlisFiyati,
                                    SatisFiyati = @SatisFiyati,
                                    UrunResmi = @UrunResmi,
                                    Renk = @Renk
                                    WHERE Id = @Id"
                                : @"INSERT INTO Urunler (
                                    UrunAdi, KategoriId, Aciklama, Miktar, Birim, 
                                    Tedarikci, MinStok, KritikStok, RafKodu, 
                                    AlisFiyati, SatisFiyati, UrunResmi, Renk
                                ) VALUES (
                                    @UrunAdi, @KategoriId, @Aciklama, @Miktar, @Birim,
                                    @Tedarikci, @MinStok, @KritikStok, @RafKodu,
                                    @AlisFiyati, @SatisFiyati, @UrunResmi, @Renk
                                )";

                            using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                            {
                                cmd.Transaction = transaction;

                                // Parametreleri ekle
                                cmd.Parameters.AddWithValue("@UrunAdi", txtUrunAdi.Text.Trim());
                                cmd.Parameters.AddWithValue("@KategoriId", secilenKategori.Id);
                                cmd.Parameters.AddWithValue("@Aciklama", txtAciklama.Text.Trim());
                                cmd.Parameters.AddWithValue("@Miktar", miktar);
                                cmd.Parameters.AddWithValue("@Birim", cmbBirim.SelectedItem?.ToString() ?? "Adet");
                                cmd.Parameters.AddWithValue("@Tedarikci", txtTedarikci.Text.Trim());
                                cmd.Parameters.AddWithValue("@MinStok", minStok);
                                cmd.Parameters.AddWithValue("@KritikStok", kritikStok);
                                cmd.Parameters.AddWithValue("@RafKodu", txtRafKodu.Text.Trim());
                                cmd.Parameters.AddWithValue("@AlisFiyati", alisFiyati);
                                cmd.Parameters.AddWithValue("@SatisFiyati", satisFiyati);
                                cmd.Parameters.AddWithValue("@UrunResmi", (object?)resimBytes ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Renk", secilenRenk);

                                if (duzenlenecekUrunId.HasValue)
                                {
                                    cmd.Parameters.AddWithValue("@Id", duzenlenecekUrunId.Value);
                                }

                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            MessageBox.Show("Ürün başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Ana formdaki ürün listesini güncelle
                            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                            if (mainForm != null)
                            {
                                mainForm.YenileUrunListesi();
                            }
                            
                            // Formu kapat
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Kayıt işlemi sırasında bir hata oluştu: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class TextBoxInfo
    {
        public string Placeholder { get; set; } = string.Empty;
        public bool IsNumeric { get; set; }
        public bool IsRequired { get; set; }

        public TextBoxInfo()
        {
        }

        public TextBoxInfo(string placeholder, bool isNumeric, bool isRequired)
        {
            Placeholder = placeholder;
            IsNumeric = isNumeric;
            IsRequired = isRequired;
        }
    }

    public class ComboBoxItem
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";

        public override string ToString()
        {
            return Text;
        }
    }
} 