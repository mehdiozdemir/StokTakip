using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;

namespace StokTakip
{
    public partial class UrunListeForm : Form
    {
        private readonly string dbDosyasi = "StokTakip.db";
        private FlowLayoutPanel? flowPanel;
        private Panel? pnlUst;
        private IContainer? components;
        private bool kritikStokAktif = false;
        private ComboBox? kategoriComboBox;
        private TextBox? txtArama;
        private List<Image> loadedImages = new List<Image>();

        public UrunListeForm()
        {
            InitializeComponent();
            FormTasarimOlustur();

            // Form yüklendiğinde başlık çubuğunu siyah yap
            this.HandleCreated += (s, e) => 
            {
                DarkModeHelper.UseImmersiveDarkMode(this.Handle, true);
            };
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await Task.Delay(100); // Kısa bir gecikme ekle
            UrunleriListele();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UrunleriListele();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ürünler listelenirken hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            }
        }

        private void UrunListeForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Kategorileri yükle
                KategorileriYukle();
                
                // Form boyutunu ayarla
                if (this.ParentForm != null)
                {
                    this.Size = this.ParentForm.ClientSize;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Yüklü resimleri temizle
                foreach (var img in loadedImages)
                {
                    img?.Dispose();
                }
                loadedImages.Clear();

                // Kontrolleri temizle
                if (flowPanel != null)
                {
                    foreach (Control control in flowPanel.Controls)
                    {
                        if (control is Panel panel)
                        {
                            foreach (Control innerControl in panel.Controls)
                            {
                                if (innerControl is PictureBox pb && pb.Image != null && pb.Image != DefaultImages.NoImage)
                                {
                                    pb.Image.Dispose();
                                    pb.Image = null;
                                }
                                innerControl.Dispose();
                            }
                        }
                        control.Dispose();
                    }
                    flowPanel.Controls.Clear();
                    flowPanel.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Text = "Ürünler";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += UrunListeForm_FormClosing;
        }

        private void UrunListeForm_FormClosing(object sender, FormClosingEventArgs e)
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
            
            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                Padding = new Padding(20)
            };

            // Flow Panel
            flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(10)
            };

            // Üst Panel (Başlık ve Butonlar)
            pnlUst = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20, 10, 20, 10)
            };

            // Başlık
            Label lblBaslik = new Label
            {
                Text = "ÜRÜN LİSTESİ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // Arama Kutusu
            txtArama = new TextBox
            {
                Name = "txtArama",
                Size = new Size(200, 35),
                Location = new Point(200, 15),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.Gray,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "🔍 Ürün Ara..."
            };

            txtArama.Enter += (s, e) => 
            {
                if (txtArama.Text == "🔍 Ürün Ara...")
                {
                    txtArama.Text = "";
                    txtArama.ForeColor = Color.White;
                }
            };

            txtArama.Leave += (s, e) => 
            {
                if (string.IsNullOrWhiteSpace(txtArama.Text))
                {
                    txtArama.Text = "🔍 Ürün Ara...";
                    txtArama.ForeColor = Color.Gray;
                }
            };

            txtArama.TextChanged += (s, e) => 
            {
                if (txtArama.Text != "🔍 Ürün Ara...")
                {
                    UrunAra(txtArama.Text);
                }
                else
                {
                    UrunleriListele();
                }
            };

            // Kategori ComboBox
            kategoriComboBox = new ComboBox
            {
                Name = "cmbKategori",
                Size = new Size(200, 40),
                Location = new Point(420, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Kategorileri yükle
            KategorileriYukle();

            // ComboBox değiştiğinde ürünleri filtrele
            kategoriComboBox.SelectedIndexChanged += (s, e) => 
            {
                if (kategoriComboBox.SelectedItem is KeyValuePair<int, string> seciliKategori)
                {
                    UrunleriListele(seciliKategori.Key);
                }
            };

            // Kritik Stok Butonu
            Button btnKritikStok = new Button
            {
                Text = "⚠️ KRİTİK STOK",
                Size = new Size(150, 35),
                Location = new Point(640, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            btnKritikStok.FlatAppearance.BorderSize = 0;
            btnKritikStok.Click += BtnKritikStok_Click;

            // Yeni Ürün Butonu
            Button btnYeniUrun = new Button
            {
                Text = "➕ YENİ ÜRÜN",
                Size = new Size(150, 35),
                Location = new Point(810, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            btnYeniUrun.FlatAppearance.BorderSize = 0;
            btnYeniUrun.Click += (s, e) =>
            {
                try
                {
                    var mainForm = this.ParentForm as MainForm;
                    if (mainForm != null)
                    {
                        mainForm.YeniUrunEkle();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Yeni ürün ekleme formu açılırken hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Kontrolleri panele ekle
            pnlUst.Controls.AddRange(new Control[] { lblBaslik, txtArama, kategoriComboBox, btnKritikStok, btnYeniUrun });
            pnlAna.Controls.AddRange(new Control[] { pnlUst, flowPanel });
            this.Controls.Add(pnlAna);
        }

        private void KategorileriYukle()
        {
            try
            {
                if (kategoriComboBox == null) return;

                kategoriComboBox.Items.Clear();
                
                // Tüm Kategoriler seçeneği
                kategoriComboBox.Items.Add(new KeyValuePair<int, string>(0, "Tüm Kategoriler"));

                string sorgu = "SELECT KategoriId, KategoriAdi FROM Kategoriler ORDER BY KategoriAdi";
                var dt = DatabaseManager.ExecuteQuery(sorgu);

                foreach (DataRow dr in dt.Rows)
                {
                    int id = Convert.ToInt32(dr["KategoriId"]);
                    string ad = dr["KategoriAdi"].ToString() ?? "";
                    kategoriComboBox.Items.Add(new KeyValuePair<int, string>(id, ad));
                }

                kategoriComboBox.DisplayMember = "Value";
                kategoriComboBox.ValueMember = "Key";
                kategoriComboBox.SelectedIndex = 0;  // Tüm Kategoriler seçili olarak başla
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kategoriler yüklenirken hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UrunleriListele(int kategoriId = 0)
        {
            if (flowPanel == null) return;

            try
            {
                flowPanel.SuspendLayout();
                
                // Mevcut kontrolleri temizle
                foreach (Control control in flowPanel.Controls)
                {
                    if (control is Panel panel)
                    {
                        foreach (Control innerControl in panel.Controls)
                        {
                            if (innerControl is PictureBox pb && pb.Image != null && pb.Image != DefaultImages.NoImage)
                            {
                                pb.Image.Dispose();
                            }
                            innerControl.Dispose();
                        }
                    }
                    control.Dispose();
                }
                flowPanel.Controls.Clear();

                string sorgu = @"
                    SELECT u.*, k.KategoriAdi 
                    FROM Urunler u 
                    LEFT JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                    WHERE (@KategoriId = 0 OR u.KategoriId = @KategoriId)
                    ORDER BY u.UrunAdi";

                var parameters = new[] { new SQLiteParameter("@KategoriId", kategoriId) };
                var dt = DatabaseManager.ExecuteQuery(sorgu, parameters);

                foreach (DataRow dr in dt.Rows)
                {
                    Panel pnlUrun = OlusturUrunKarti(dr);
                    flowPanel.Controls.Add(pnlUrun);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürünler listelenirken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                flowPanel.ResumeLayout();
            }
        }

        private Panel OlusturUrunKarti(DataRow dr)
        {
            Panel pnlUrun = new Panel
            {
                Width = 320,
                Height = 500,
                Margin = new Padding(10),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };

            // Stok durumuna göre kart rengini ayarla
            int miktar = Convert.ToInt32(dr["Miktar"]);
            int kritikStok = Convert.ToInt32(dr["KritikStok"]);
            bool kritikStokDurumu = miktar <= kritikStok;
            bool stokBitti = miktar == 0;

            if (stokBitti)
            {
                pnlUrun.BackColor = Color.FromArgb(45, 20, 20);
            }
            else if (kritikStokDurumu)
            {
                pnlUrun.BackColor = Color.FromArgb(45, 35, 20);
            }

            // Resim Paneli
            Panel pnlResim = new Panel
            {
                Height = 300,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 5, 0, 10),
                Padding = new Padding(5),
                BackColor = Color.FromArgb(25, 25, 25)
            };

            PictureBox picUrunResmi = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            // Resmi yükle
            if (dr["UrunResmi"] != DBNull.Value)
            {
                try
                {
                    byte[] resimBytes = (byte[])dr["UrunResmi"];
                    using (var ms = new MemoryStream(resimBytes))
                    {
                        var img = Image.FromStream(ms);
                        picUrunResmi.Image = img;
                        loadedImages.Add(img);
                    }
                }
                catch
                {
                    picUrunResmi.Image = DefaultImages.NoImage;
                }
            }
            else
            {
                picUrunResmi.Image = DefaultImages.NoImage;
            }

            pnlResim.Controls.Add(picUrunResmi);

            // Bilgi Paneli
            Panel pnlBilgi = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            // Ürün Adı
            Label lblUrunAdi = new Label
            {
                Text = dr["UrunAdi"].ToString(),
                Dock = DockStyle.Top,
                Height = 45,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.TopLeft
            };

            // Kategori
            Label lblKategori = new Label
            {
                Text = dr["KategoriAdi"].ToString() ?? "Kategorisiz",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 11)
            };

            // Stok Durumu
            Label lblStok = new Label
            {
                Text = $"Stok: {miktar} {dr["Birim"]}",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = stokBitti ? Color.Red : (kritikStokDurumu ? Color.Orange : Color.White),
                Font = new Font("Segoe UI", 12)
            };

            // Fiyat
            Label lblFiyat = new Label
            {
                Text = $"₺{Convert.ToDecimal(dr["SatisFiyati"]):N2}",
                Dock = DockStyle.Top,
                Height = 35,
                ForeColor = Color.Lime,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };

            // Renk göstergesi ve adı için panel
            if (dr["Renk"] != DBNull.Value && !string.IsNullOrEmpty(dr["Renk"].ToString()))
            {
                try
                {
                    Color urunRengi = ColorTranslator.FromHtml(dr["Renk"].ToString());
                    string renkAdi = RenkAdiniBul(urunRengi);
                    
                    Panel pnlRenkBilgi = new Panel
                    {
                        Width = 80,
                        Height = 50,
                        Location = new Point(220, lblFiyat.Bottom + 5),
                        BackColor = Color.Transparent
                    };

                    Panel pnlRenkKare = new Panel
                    {
                        Width = 25,
                        Height = 25,
                        Location = new Point(27, 0),
                        BackColor = urunRengi,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    Label lblRenkAdi = new Label
                    {
                        Text = renkAdi,
                        Width = 80,
                        Height = 20,
                        Location = new Point(0, 30),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 12),
                        TextAlign = ContentAlignment.TopCenter
                    };

                    pnlRenkBilgi.Controls.Add(pnlRenkKare);
                    pnlRenkBilgi.Controls.Add(lblRenkAdi);
                    pnlBilgi.Controls.Add(pnlRenkBilgi);
                }
                catch { /* Renk dönüştürme hatası olursa gösterge eklenmez */ }
            }

            // Butonlar için panel
            TableLayoutPanel pnlButonlar = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                ColumnCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0, 10, 0, 0)
            };
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            int urunId = Convert.ToInt32(dr["Id"]);

            // Düzenle Butonu
            Button btnDuzenle = new Button
            {
                Text = "✏️",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Tag = urunId
            };
            btnDuzenle.FlatAppearance.BorderSize = 0;
            btnDuzenle.Click += (s, e) =>
            {
                if (s is Button btn)
                {
                    btn.Enabled = false;
                    try
                    {
                        using (var form = new UrunDuzenleForm(urunId))
                        {
                            form.StartPosition = FormStartPosition.CenterParent;
                            form.ShowDialog(this);
                            
                            if (form.DialogResult == DialogResult.OK)
                            {
                                UrunleriListele(kategoriComboBox?.SelectedValue is KeyValuePair<int, string> seciliKategori 
                                    ? seciliKategori.Key : 0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ürün düzenleme işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btn.Enabled = true;
                    }
                }
            };

            // Sepete Ekle Butonu
            Button btnSepeteEkle = new Button
            {
                Text = "🛒",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Enabled = !stokBitti,
                Tag = urunId
            };
            btnSepeteEkle.FlatAppearance.BorderSize = 0;
            btnSepeteEkle.Click += (s, e) =>
            {
                var mainForm = this.ParentForm as MainForm;
                if (mainForm != null)
                {
                    SepeteEkleDialogGoster(
                        urunId,
                        dr["UrunAdi"].ToString() ?? "",
                        Convert.ToDecimal(dr["SatisFiyati"]),
                        miktar
                    );
                }
            };

            // Sil Butonu
            Button btnSil = new Button
            {
                Text = "❌",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13),
                Cursor = Cursors.Hand,
                Tag = urunId
            };
            btnSil.FlatAppearance.BorderSize = 0;
            btnSil.Click += (s, e) => UrunSil(urunId);

            // Butonları panele ekle
            pnlButonlar.Controls.Add(btnDuzenle, 0, 0);
            pnlButonlar.Controls.Add(btnSepeteEkle, 1, 0);
            pnlButonlar.Controls.Add(btnSil, 2, 0);

            // Kontrolleri panellere ekle
            pnlBilgi.Controls.AddRange(new Control[] { pnlButonlar, lblFiyat, lblStok, lblKategori, lblUrunAdi });
            pnlUrun.Controls.AddRange(new Control[] { pnlBilgi, pnlResim });

            return pnlUrun;
        }

        private string RenkAdiniBul(Color renk)
        {
            // En yakın temel rengi bul
            if (renk.R > 200 && renk.G < 100 && renk.B < 100) return "Kırmızı";
            if (renk.R < 100 && renk.G > 200 && renk.B < 100) return "Yeşil";
            if (renk.R < 100 && renk.G < 100 && renk.B > 200) return "Mavi";
            if (renk.R > 200 && renk.G > 200 && renk.B < 100) return "Sarı";
            if (renk.R > 200 && renk.G < 100 && renk.B > 200) return "Mor";
            if (renk.R < 100 && renk.G > 200 && renk.B > 200) return "Turkuaz";
            if (renk.R > 200 && renk.G > 100 && renk.B < 100) return "Turuncu";
            if (renk.R > 200 && renk.G > 200 && renk.B > 200) return "Beyaz";
            if (renk.R < 50 && renk.G < 50 && renk.B < 50) return "Siyah";
            if (Math.Abs(renk.R - renk.G) < 30 && Math.Abs(renk.G - renk.B) < 30) return "Gri";
            if (renk.R > 150 && renk.G > 100 && renk.B > 100) return "Pembe";
            if (renk.R > 100 && renk.G > 50 && renk.B < 50) return "Kahverengi";

            return "Diğer";
        }

        private void BtnYeniUrun_Click(object? sender, EventArgs e)
        {
            try
            {
                var mainForm = this.ParentForm as MainForm;
                if (mainForm != null)
                {
                    mainForm.YeniUrunEkle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yeni ürün ekleme formu açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UrunSil(int urunId)
        {
            try
            {
                if (MessageBox.Show("Bu ürünü silmek istediğinize emin misiniz?", "Silme Onayı",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string sorgu = "DELETE FROM Urunler WHERE Id = @UrunId";
                    var parameters = new[] { new SQLiteParameter("@UrunId", urunId) };

                    int etkilenenSatir = DatabaseManager.ExecuteNonQuery(sorgu, parameters);

                    if (etkilenenSatir > 0)
                    {
                        MessageBox.Show("Ürün başarıyla silindi.", "Bilgi", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        this.Invoke((MethodInvoker)delegate
                        {
                            UrunleriListele(kategoriComboBox?.SelectedValue is KeyValuePair<int, string> seciliKategori 
                                ? seciliKategori.Key : 0);
                        });
                    }
                    else
                    {
                        MessageBox.Show("Ürün silinirken bir hata oluştu.", "Hata", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün silinirken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UrunGuncelle(int? urunId)
        {
            if (!urunId.HasValue) return;

            try
            {
                // Yeni form oluştur
                using (var urunEkleForm = new UrunEkleForm())
                {
                    // Form özelliklerini ayarla
                    urunEkleForm.StartPosition = FormStartPosition.CenterScreen;
                    urunEkleForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    urunEkleForm.MaximizeBox = false;
                    urunEkleForm.MinimizeBox = false;

                    // Ürün bilgilerini getir ve form kontrollerini doldur
                    string projeKlasoru = Application.StartupPath;
                    string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
                    using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                    {
                        baglanti.Open();
                        string sorgu = "SELECT * FROM Urunler WHERE Id = @UrunId";
                        using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                        {
                            cmd.Parameters.AddWithValue("@UrunId", urunId.Value);
                            using (SQLiteDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    // Form kontrollerini bul
                                    var controls = urunEkleForm.Controls[0].Controls[0].Controls;
                                    var txtUrunAdi = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtUrunAdi");
                                    var txtAciklama = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtAciklama");
                                    var txtTedarikci = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtTedarikci");
                                    var txtRafKodu = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtRafKodu");
                                    var txtMiktar = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtMiktar");
                                    var txtMinStok = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtMinStok");
                                    var txtKritikStok = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtKritikStok");
                                    var txtAlisFiyati = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtAlisFiyati");
                                    var txtSatisFiyati = controls.OfType<TextBox>().FirstOrDefault(x => x.Name == "txtSatisFiyati");
                                    var cmbBirim = controls.OfType<ComboBox>().FirstOrDefault();
                                    var pbUrunResmi = controls.OfType<PictureBox>().FirstOrDefault();

                                    // Değerleri doldur
                                    if (txtUrunAdi != null)
                                    {
                                        txtUrunAdi.Text = dr["UrunAdi"].ToString();
                                        txtUrunAdi.ForeColor = Color.White;
                                    }

                                    if (txtAciklama != null)
                                    {
                                        txtAciklama.Text = dr["Aciklama"].ToString();
                                        txtAciklama.ForeColor = Color.White;
                                    }

                                    if (txtTedarikci != null)
                                    {
                                        txtTedarikci.Text = dr["Tedarikci"].ToString();
                                        txtTedarikci.ForeColor = Color.White;
                                    }

                                    if (txtRafKodu != null)
                                    {
                                        txtRafKodu.Text = dr["RafKodu"].ToString();
                                        txtRafKodu.ForeColor = Color.White;
                                    }

                                    if (txtMiktar != null)
                                    {
                                        txtMiktar.Text = dr["Miktar"].ToString();
                                        txtMiktar.ForeColor = Color.White;
                                    }

                                    if (txtMinStok != null)
                                    {
                                        txtMinStok.Text = dr["MinStok"].ToString();
                                        txtMinStok.ForeColor = Color.White;
                                    }

                                    if (txtKritikStok != null)
                                    {
                                        txtKritikStok.Text = dr["KritikStok"].ToString();
                                        txtKritikStok.ForeColor = Color.White;
                                    }

                                    if (txtAlisFiyati != null)
                                    {
                                        txtAlisFiyati.Text = Convert.ToDecimal(dr["AlisFiyati"]).ToString("N2");
                                        txtAlisFiyati.ForeColor = Color.White;
                                    }

                                    if (txtSatisFiyati != null)
                                    {
                                        txtSatisFiyati.Text = Convert.ToDecimal(dr["SatisFiyati"]).ToString("N2");
                                        txtSatisFiyati.ForeColor = Color.White;
                                    }

                                    if (cmbBirim != null)
                                    {
                                        cmbBirim.SelectedItem = dr["Birim"].ToString();
                                    }

                                    // Resmi yükle
                                    if (pbUrunResmi != null && !dr.IsDBNull(dr.GetOrdinal("UrunResmi")))
                                    {
                                        byte[] resimBytes = (byte[])dr["UrunResmi"];
                                        using (MemoryStream ms = new MemoryStream(resimBytes))
                                        {
                                            pbUrunResmi.Image = Image.FromStream(ms);
                                        }
                                    }

                                    // Kaydet butonunu güncelle
                                    var btnKaydet = urunEkleForm.Controls[0].Controls[0].Controls.OfType<Button>()
                                        .FirstOrDefault(x => x.Text.Contains("KAYDET"));
                                    if (btnKaydet != null)
                                    {
                                        btnKaydet.Text = "💾 GÜNCELLE";
                                        btnKaydet.Tag = urunId;
                                    }
                                }
                            }
                        }
                    }

                    // Formu modal olarak göster
                    if (urunEkleForm.ShowDialog() == DialogResult.OK)
                    {
                        // Form kapandığında listeyi yenile
                        UrunleriListele();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün güncelleme formunu açarken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SepeteEkleDialogGoster(int urunId, string urunAdi, decimal onerilenfiyat, int maksimumAdet)
        {
            try
            {
                var sepetForm = new Form
                {
                    Text = "Sepete Ekle",
                    Size = new Size(350, 300),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    Owner = this.ParentForm
                };

                Panel pnlIcerik = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20)
                };

                Label lblUrunAdi = new Label
                {
                    Text = urunAdi,
                    Dock = DockStyle.Top,
                    Height = 30,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                Panel pnlAdet = new Panel
                {
                    Height = 60,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 10, 0, 0)
                };

                Label lblAdet = new Label
                {
                    Text = "Adet:",
                    AutoSize = true,
                    Location = new Point(0, 5),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.White
                };

                NumericUpDown numAdet = new NumericUpDown
                {
                    Location = new Point(0, 25),
                    Size = new Size(290, 30),
                    Minimum = 1,
                    Maximum = maksimumAdet,
                    Value = 1,
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White
                };

                Panel pnlFiyat = new Panel
                {
                    Height = 60,
                    Dock = DockStyle.Top,
                    Padding = new Padding(0, 10, 0, 0)
                };

                Label lblFiyat = new Label
                {
                    Text = "Birim Fiyat (₺):",
                    AutoSize = true,
                    Location = new Point(0, 5),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.White
                };

                TextBox txtFiyat = new TextBox
                {
                    Location = new Point(0, 25),
                    Size = new Size(290, 30),
                    Text = onerilenfiyat.ToString("N2"),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lblToplam = new Label
                {
                    Text = $"Toplam: ₺{onerilenfiyat:N2}",
                    Height = 30,
                    Dock = DockStyle.Top,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 183, 195),
                    TextAlign = ContentAlignment.MiddleRight
                };

                // Toplam tutarı güncelle
                void ToplamGuncelle()
                {
                    if (decimal.TryParse(txtFiyat.Text, out decimal fiyat))
                    {
                        decimal toplam = fiyat * numAdet.Value;
                        lblToplam.Text = $"Toplam: ₺{toplam:N2}";
                    }
                }

                numAdet.ValueChanged += (s, e) => ToplamGuncelle();
                txtFiyat.TextChanged += (s, e) => ToplamGuncelle();

                Button btnEkle = new Button
                {
                    Text = "SEPETE EKLE",
                    Dock = DockStyle.Bottom,
                    Height = 45,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 183, 195),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnEkle.FlatAppearance.BorderSize = 0;
                btnEkle.Click += (s, e) =>
                {
                    try
                    {
                        if (!decimal.TryParse(txtFiyat.Text, out decimal fiyat))
                        {
                            MessageBox.Show("Lütfen geçerli bir fiyat girin!", "Uyarı",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var mainForm = this.ParentForm as MainForm;
                        if (mainForm != null)
                        {
                            mainForm.SepeteUrunEkle(urunId, urunAdi, (int)numAdet.Value, fiyat);
                            MessageBox.Show("Ürün sepete eklendi!", "Başarılı",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            sepetForm.DialogResult = DialogResult.OK;
                            sepetForm.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ürün sepete eklenirken hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                pnlAdet.Controls.AddRange(new Control[] { lblAdet, numAdet });
                pnlFiyat.Controls.AddRange(new Control[] { lblFiyat, txtFiyat });
                pnlIcerik.Controls.AddRange(new Control[] { lblUrunAdi, pnlAdet, pnlFiyat, lblToplam });
                sepetForm.Controls.AddRange(new Control[] { pnlIcerik, btnEkle });

                sepetForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sepete ekleme penceresi açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDuzenle_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Enabled = false;
                try
                {
                    int urunId = Convert.ToInt32(btn.Tag);
                    using (var form = new UrunDuzenleForm(urunId))
                    {
                        form.StartPosition = FormStartPosition.CenterParent;
                        form.ShowDialog(this);
                        
                        // Form kapandıktan sonra listeyi yenile
                        if (form.DialogResult == DialogResult.OK)
                        {
                            UrunleriListele(kategoriComboBox?.SelectedValue is KeyValuePair<int, string> seciliKategori 
                                ? seciliKategori.Key : 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ürün düzenleme işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btn.Enabled = true;
                }
            }
        }

        private void BtnKritikStok_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                kritikStokAktif = !kritikStokAktif;
                btn.BackColor = kritikStokAktif ? Color.FromArgb(0, 183, 195) : Color.FromArgb(45, 45, 45);

                if (kritikStokAktif)
                {
                    // Kritik stok ürünlerini göster
                    try
                    {
                        if (flowPanel == null) return;
                        flowPanel.SuspendLayout();
                        flowPanel.Controls.Clear();

                        string sorgu = @"
                            SELECT 
                                u.Id,
                                u.UrunAdi,
                                u.KategoriId,
                                u.Aciklama,
                                COALESCE(u.Miktar, 0) as Miktar,
                                COALESCE(u.Birim, 'Adet') as Birim,
                                u.Tedarikci,
                                COALESCE(u.MinStok, 0) as MinStok,
                                COALESCE(u.KritikStok, 0) as KritikStok,
                                u.RafKodu,
                                COALESCE(u.AlisFiyati, 0) as AlisFiyati,
                                COALESCE(u.SatisFiyati, 0) as SatisFiyati,
                                u.UrunResmi,
                                u.Renk,
                                k.KategoriAdi 
                            FROM Urunler u 
                            LEFT JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                            WHERE u.Miktar <= u.KritikStok AND u.KritikStok > 0
                            ORDER BY u.Miktar ASC";

                        var dt = DatabaseManager.ExecuteQuery(sorgu);

                        if (dt.Rows.Count == 0)
                        {
                            Label lblBilgi = new Label
                            {
                                Text = "Kritik stok seviyesinde ürün bulunmamaktadır.",
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 12),
                                AutoSize = true,
                                Padding = new Padding(20)
                            };
                            flowPanel.Controls.Add(lblBilgi);
                            return;
                        }

                        foreach (DataRow dr in dt.Rows)
                        {
                            Panel pnlUrun = OlusturUrunKarti(dr);
                            flowPanel.Controls.Add(pnlUrun);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Kritik stok ürünleri listelenirken bir hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        flowPanel.ResumeLayout();
                    }
                }
                else
                {
                    // Tüm ürünleri göster
                    UrunleriListele();
                }
            }
        }

        public void RefreshList()
        {
            UrunleriListele();
        }

        public void YenileKategoriListesi()
        {
            if (kategoriComboBox != null)
            {
                var seciliDeger = kategoriComboBox.SelectedValue;
                KategorileriYukle();
                if (seciliDeger != null)
                {
                    kategoriComboBox.SelectedValue = seciliDeger;
                }
            }
        }

        private void UrunAra(string aramaMetni)
        {
            if (flowPanel == null) return;

            try
            {
                // Önceki kontrolleri temizle
                flowPanel.SuspendLayout();
                flowPanel.Controls.Clear();

                string sorgu = @"
                    SELECT 
                        u.Id,
                        u.UrunAdi,
                        u.KategoriId,
                        u.Aciklama,
                        COALESCE(u.Miktar, 0) as Miktar,
                        COALESCE(u.Birim, 'Adet') as Birim,
                        u.Tedarikci,
                        COALESCE(u.MinStok, 0) as MinStok,
                        COALESCE(u.KritikStok, 0) as KritikStok,
                        u.RafKodu,
                        COALESCE(u.AlisFiyati, 0) as AlisFiyati,
                        COALESCE(u.SatisFiyati, 0) as SatisFiyati,
                        u.UrunResmi,
                        u.Renk,
                        k.KategoriAdi 
                    FROM Urunler u 
                    LEFT JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                    WHERE (@KategoriId = 0 OR u.KategoriId = @KategoriId) AND 
                          (u.UrunAdi LIKE @AramaMetni || '%' OR 
                           u.Tedarikci LIKE @AramaMetni || '%' OR 
                           u.RafKodu LIKE @AramaMetni || '%' OR
                           k.KategoriAdi LIKE @AramaMetni || '%')
                    ORDER BY u.UrunAdi";

                var parameters = new[] { 
                    new SQLiteParameter("@KategoriId", kategoriComboBox?.SelectedValue is KeyValuePair<int, string> seciliKategori ? seciliKategori.Key : 0),
                    new SQLiteParameter("@AramaMetni", aramaMetni)
                };

                var dt = DatabaseManager.ExecuteQuery(sorgu, parameters);

                if (dt.Rows.Count == 0)
                {
                    Label lblBilgi = new Label
                    {
                        Text = "Arama kriterlerine uygun ürün bulunamadı.",
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 12),
                        AutoSize = true,
                        Padding = new Padding(20)
                    };
                    flowPanel.Controls.Add(lblBilgi);
                    return;
                }

                foreach (DataRow dr in dt.Rows)
                {
                    Panel pnlUrun = OlusturUrunKarti(dr);
                    flowPanel.Controls.Add(pnlUrun);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürünler aranırken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                flowPanel.ResumeLayout();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Form kapatılmadan önce tüm kontrolleri temizle
                foreach (Control ctrl in this.Controls)
                {
                    if (!ctrl.IsDisposed)
                    {
                        if (ctrl is Panel panel)
                        {
                            foreach (Control panelCtrl in panel.Controls)
                            {
                                if (!panelCtrl.IsDisposed)
                                {
                                    panelCtrl.Dispose();
                                }
                            }
                        }
                        ctrl.Dispose();
                    }
                }

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form kapatılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtArama_TextChanged(object sender, EventArgs e)
        {
            if (txtArama == null) return;

            string aramaMetni = txtArama.Text.Trim();
            if (aramaMetni == "🔍 Ürün Ara...") return;

            if (string.IsNullOrEmpty(aramaMetni))
            {
                UrunleriListele();
            }
            else
            {
                UrunAra(aramaMetni);
            }
        }
    }
} 