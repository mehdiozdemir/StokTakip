using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Drawing.Drawing2D;

namespace StokTakip
{
    public partial class UrunDuzenleForm : Form
    {
        private readonly int urunId;
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
        private PictureBox? picUrunResmi;
        private Panel? pnlSecilenRenk;
        private TableLayoutPanel pnlAna;
        private string secilenRenk = "";
        private byte[]? resimBytes = null;

        public UrunDuzenleForm(int urunId)
        {
            this.urunId = urunId;
            InitializeComponent();
            UrunBilgileriniYukle();
        }

        private void InitializeComponent()
        {
            this.Text = "Ürün Düzenle";
            this.Size = new Size(500, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            pnlAna = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(30, 30, 30),
                ColumnCount = 2,
                RowCount = 13
            };

            // Sol taraf için genişlik ayarı
            pnlAna.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            // Sağ taraf için genişlik ayarı
            pnlAna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Form kontrollerini oluştur
            txtUrunAdi = new TextBox { Dock = DockStyle.Fill };
            txtAciklama = new TextBox { Dock = DockStyle.Fill };
            txtTedarikci = new TextBox { Dock = DockStyle.Fill };
            txtRafKodu = new TextBox { Dock = DockStyle.Fill };
            txtMiktar = new TextBox { Dock = DockStyle.Fill };
            txtMinStok = new TextBox { Dock = DockStyle.Fill };
            txtKritikStok = new TextBox { Dock = DockStyle.Fill };
            txtAlisFiyati = new TextBox { Dock = DockStyle.Fill };
            txtSatisFiyati = new TextBox { Dock = DockStyle.Fill };
            cmbBirim = new ComboBox { Dock = DockStyle.Fill };
            cmbKategori = new ComboBox { Dock = DockStyle.Fill };

            // Birim seçeneklerini ekle
            cmbBirim.Items.AddRange(new object[] { "Adet", "Kg", "Lt", "Mt" });

            // Kategorileri yükle
            KategorileriYukle();

            // Resim Seçme Paneli
            Panel pnlResim = new Panel
            {
                Height = 150,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10)
            };

            picUrunResmi = new PictureBox
            {
                Size = new Size(150, 150),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(45, 45, 45),
                Cursor = Cursors.Hand
            };

            Button btnResimSec = new Button
            {
                Text = "🖼️ Resim Seç",
                Size = new Size(150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnResimSec.FlatAppearance.BorderSize = 0;
            btnResimSec.Click += PicUrunResmi_Click;

            Button btnResimKaldir = new Button
            {
                Text = "❌ Resmi Kaldır",
                Size = new Size(150, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnResimKaldir.FlatAppearance.BorderSize = 0;
            btnResimKaldir.Click += BtnResimKaldir_Click;

            FlowLayoutPanel pnlResimButonlar = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Width = 160,
                Height = 150,
                Padding = new Padding(0, 0, 0, 0),
                Margin = new Padding(10, 0, 0, 0)
            };
            pnlResimButonlar.Controls.AddRange(new Control[] { btnResimSec, btnResimKaldir });

            pnlResim.Controls.Add(picUrunResmi);
            pnlResim.Controls.Add(pnlResimButonlar);
            pnlResimButonlar.Location = new Point(picUrunResmi.Right + 10, picUrunResmi.Top);

            // Kontrolleri panele ekle
            Label lblResim = new Label
            {
                Text = "Ürün Resmi:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };

            pnlAna.Controls.Add(lblResim, 0, 0);
            pnlAna.Controls.Add(pnlResim, 1, 0);

            // Diğer kontrolleri ekle
            EkleKontrol(pnlAna, "Ürün Adı:", txtUrunAdi, 1);
            EkleKontrol(pnlAna, "Kategori:", cmbKategori, 2);
            EkleKontrol(pnlAna, "Açıklama:", txtAciklama, 3);
            EkleKontrol(pnlAna, "Tedarikçi:", txtTedarikci, 4);
            EkleKontrol(pnlAna, "Raf Kodu:", txtRafKodu, 5);
            EkleKontrol(pnlAna, "Miktar:", txtMiktar, 6);
            EkleKontrol(pnlAna, "Birim:", cmbBirim, 7);
            EkleKontrol(pnlAna, "Min. Stok:", txtMinStok, 8);
            EkleKontrol(pnlAna, "Kritik Stok:", txtKritikStok, 9);
            EkleKontrol(pnlAna, "Alış Fiyatı:", txtAlisFiyati, 10);
            EkleKontrol(pnlAna, "Satış Fiyatı:", txtSatisFiyati, 11);

            // Renk seçme paneli
            Label lblRenk = new Label
            {
                Text = "Ürün Rengi:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };

            Panel pnlRenkKontrol = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 35,
                Margin = new Padding(0, 25, 0, 10)
            };

            pnlSecilenRenk = new Panel
            {
                Width = 30,
                Height = 30,
                Location = new Point(0, 0),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            Button btnRenkSec = new Button
            {
                Text = "🎨 Renk Seç",
                Width = 150,
                Height = 30,
                Location = new Point(40, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnRenkSec.FlatAppearance.BorderSize = 0;
            btnRenkSec.Click += BtnRenkSec_Click;

            pnlRenkKontrol.Controls.Add(pnlSecilenRenk);
            pnlRenkKontrol.Controls.Add(btnRenkSec);

            pnlAna.Controls.Add(lblRenk, 0, 12);
            pnlAna.Controls.Add(pnlRenkKontrol, 1, 12);

            // Kaydet butonu
            Button btnKaydet = new Button
            {
                Text = "💾 KAYDET",
                Dock = DockStyle.Bottom,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btnKaydet.Click += BtnKaydet_Click;

            this.Controls.AddRange(new Control[] { pnlAna, btnKaydet });
        }

        private void EkleKontrol(TableLayoutPanel panel, string etiket, Control kontrol, int satir)
        {
            Label lbl = new Label
            {
                Text = etiket,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0)
            };

            if (kontrol is TextBox txt)
            {
                txt.BackColor = Color.FromArgb(45, 45, 45);
                txt.ForeColor = Color.White;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.Font = new Font("Segoe UI", 10);
            }
            else if (kontrol is ComboBox cmb)
            {
                cmb.BackColor = Color.FromArgb(45, 45, 45);
                cmb.ForeColor = Color.White;
                cmb.FlatStyle = FlatStyle.Flat;
                cmb.Font = new Font("Segoe UI", 10);
            }

            panel.Controls.Add(lbl, 0, satir);
            panel.Controls.Add(kontrol, 1, satir);
        }

        private void UrunBilgileriniYukle()
        {
            try
            {
                string sorgu = "SELECT * FROM Urunler WHERE Id = @UrunId";
                var parameters = new[] { new SQLiteParameter("@UrunId", urunId) };
                var dt = DatabaseManager.ExecuteQuery(sorgu, parameters);

                if (dt.Rows.Count > 0)
                {
                    var dr = dt.Rows[0];

                    txtUrunAdi!.Text = dr["UrunAdi"].ToString();
                    txtAciklama!.Text = dr["Aciklama"].ToString();
                    txtTedarikci!.Text = dr["Tedarikci"].ToString();
                    txtRafKodu!.Text = dr["RafKodu"].ToString();
                    txtMiktar!.Text = dr["Miktar"].ToString();
                    txtMinStok!.Text = dr["MinStok"].ToString();
                    txtKritikStok!.Text = dr["KritikStok"].ToString();
                    txtAlisFiyati!.Text = Convert.ToDecimal(dr["AlisFiyati"]).ToString("N2");
                    txtSatisFiyati!.Text = Convert.ToDecimal(dr["SatisFiyati"]).ToString("N2");
                    cmbBirim!.Text = dr["Birim"].ToString();

                    // Kategori seçimini ayarla
                    int kategoriId = Convert.ToInt32(dr["KategoriId"]);
                    foreach (KeyValuePair<int, string> item in cmbKategori!.Items)
                    {
                        if (item.Key == kategoriId)
                        {
                            cmbKategori.SelectedItem = item;
                            break;
                        }
                    }

                    // Resmi yükle
                    if (dr["UrunResmi"] != DBNull.Value)
                    {
                        try
                        {
                            resimBytes = (byte[])dr["UrunResmi"];
                            using (MemoryStream ms = new MemoryStream(resimBytes))
                            {
                                picUrunResmi!.Image = Image.FromStream(ms);
                            }
                        }
                        catch
                        {
                            picUrunResmi!.Image = DefaultImages.NoImage;
                            resimBytes = null;
                        }
                    }
                    else
                    {
                        picUrunResmi!.Image = DefaultImages.NoImage;
                        resimBytes = null;
                    }

                    // Renk bilgisini yükle
                    if (dr["Renk"] != DBNull.Value && !string.IsNullOrEmpty(dr["Renk"].ToString()))
                    {
                        secilenRenk = dr["Renk"].ToString();
                        pnlSecilenRenk.BackColor = ColorTranslator.FromHtml(secilenRenk);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün bilgileri yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PicUrunResmi_Click(object sender, EventArgs e)
        {
            BtnResimSec_Click(sender, e);
        }

        private void BtnResimSec_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                ofd.Title = "Ürün Resmi Seç";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Resmi yükle ve boyutlandır
                        using (var img = Image.FromFile(ofd.FileName))
                        {
                            // Maksimum boyutlar
                            int maxWidth = 800;
                            int maxHeight = 800;

                            // Yeni boyutları hesapla
                            var ratioX = (double)maxWidth / img.Width;
                            var ratioY = (double)maxHeight / img.Height;
                            var ratio = Math.Min(ratioX, ratioY);

                            var newWidth = (int)(img.Width * ratio);
                            var newHeight = (int)(img.Height * ratio);

                            // Yeni resmi oluştur
                            var newImage = new Bitmap(newWidth, newHeight);
                            using (var graphics = Graphics.FromImage(newImage))
                            {
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphics.DrawImage(img, 0, 0, newWidth, newHeight);
                            }

                            // Resmi byte dizisine çevir
                            using (var ms = new MemoryStream())
                            {
                                newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                resimBytes = ms.ToArray();
                            }

                            // Eski resmi dispose et
                            if (picUrunResmi.Image != null && picUrunResmi.Image != DefaultImages.NoImage)
                            {
                                picUrunResmi.Image.Dispose();
                            }

                            // Yeni resmi göster
                            using (var ms = new MemoryStream(resimBytes))
                            {
                                picUrunResmi.Image = Image.FromStream(ms);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Resim yüklenirken hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnResimKaldir_Click(object sender, EventArgs e)
        {
            if (picUrunResmi.Image != null && picUrunResmi.Image != DefaultImages.NoImage)
            {
                picUrunResmi.Image.Dispose();
            }
            picUrunResmi.Image = DefaultImages.NoImage;
            resimBytes = null;
        }

        private void BtnRenkSec_Click(object sender, EventArgs e)
        {
            // Renk seçme formu
            Form renkForm = new Form
            {
                Text = "Renk Seç",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };

            FlowLayoutPanel pnlRenkler = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30)
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
                    Size = new Size(80, 100),
                    Margin = new Padding(5),
                    Padding = new Padding(0),
                    BackColor = Color.FromArgb(45, 45, 45)
                };

                Panel pnlRenkGosterge = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 75,
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
                    BackColor = Color.FromArgb(45, 45, 45),
                    Padding = new Padding(0)
                };

                pnlRenkGosterge.Click += (s, ev) =>
                {
                    secilenRenk = renk.Deger;
                    pnlSecilenRenk.BackColor = ColorTranslator.FromHtml(renk.Deger);
                    renkForm.Close();
                };

                pnlRenk.Controls.Add(pnlRenkGosterge);
                pnlRenk.Controls.Add(lblRenkAdi);
                pnlRenkler.Controls.Add(pnlRenk);
            }

            renkForm.Controls.Add(pnlRenkler);
            renkForm.ShowDialog();
        }

        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUrunAdi?.Text))
                {
                    MessageBox.Show("Ürün adı boş olamaz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmbKategori?.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir kategori seçin!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmbBirim?.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir birim seçin!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal alisFiyati = 0;
                if (!string.IsNullOrWhiteSpace(txtAlisFiyati?.Text))
                {
                    string temizFiyat = txtAlisFiyati.Text.Trim();
                    if (!decimal.TryParse(temizFiyat, System.Globalization.NumberStyles.Any, 
                        new System.Globalization.CultureInfo("tr-TR"), out alisFiyati) && 
                        !decimal.TryParse(temizFiyat, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out alisFiyati))
                    {
                        MessageBox.Show("Geçerli bir alış fiyatı giriniz!\nÖrnek: 10,50 veya 10.50", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (alisFiyati < 0)
                    {
                        MessageBox.Show("Alış fiyatı negatif olamaz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                decimal satisFiyati = 0;
                if (!string.IsNullOrWhiteSpace(txtSatisFiyati?.Text))
                {
                    string temizFiyat = txtSatisFiyati.Text.Trim();
                    if (!decimal.TryParse(temizFiyat, System.Globalization.NumberStyles.Any,
                        new System.Globalization.CultureInfo("tr-TR"), out satisFiyati) &&
                        !decimal.TryParse(temizFiyat, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out satisFiyati))
                    {
                        MessageBox.Show("Geçerli bir satış fiyatı giriniz!\nÖrnek: 10,50 veya 10.50", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (satisFiyati < 0)
                    {
                        MessageBox.Show("Satış fiyatı negatif olamaz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                int miktar = 0;
                if (!string.IsNullOrWhiteSpace(txtMiktar?.Text))
                {
                    if (!int.TryParse(txtMiktar.Text, out miktar) || miktar < 0)
                    {
                        MessageBox.Show("Geçerli bir miktar giriniz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                int minStok = 0;
                if (!string.IsNullOrWhiteSpace(txtMinStok?.Text))
                {
                    if (!int.TryParse(txtMinStok.Text, out minStok) || minStok < 0)
                    {
                        MessageBox.Show("Geçerli bir minimum stok değeri giriniz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                int kritikStok = 0;
                if (!string.IsNullOrWhiteSpace(txtKritikStok?.Text))
                {
                    if (!int.TryParse(txtKritikStok.Text, out kritikStok) || kritikStok < 0)
                    {
                        MessageBox.Show("Geçerli bir kritik stok değeri giriniz!", "Uyarı",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                var kategori = (KeyValuePair<int, string>)cmbKategori.SelectedItem;

                string sorgu = @"
                    UPDATE Urunler 
                    SET UrunAdi = @UrunAdi,
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
                    WHERE Id = @UrunId";

                var parameters = new[]
                {
                    new SQLiteParameter("@UrunId", urunId),
                    new SQLiteParameter("@UrunAdi", txtUrunAdi?.Text.Trim()),
                    new SQLiteParameter("@KategoriId", kategori.Key),
                    new SQLiteParameter("@Aciklama", txtAciklama?.Text.Trim()),
                    new SQLiteParameter("@Miktar", miktar),
                    new SQLiteParameter("@Birim", cmbBirim?.SelectedItem.ToString()),
                    new SQLiteParameter("@Tedarikci", txtTedarikci?.Text.Trim()),
                    new SQLiteParameter("@MinStok", minStok),
                    new SQLiteParameter("@KritikStok", kritikStok),
                    new SQLiteParameter("@RafKodu", txtRafKodu?.Text.Trim()),
                    new SQLiteParameter("@AlisFiyati", alisFiyati),
                    new SQLiteParameter("@SatisFiyati", satisFiyati),
                    new SQLiteParameter("@UrunResmi", (object?)resimBytes ?? DBNull.Value),
                    new SQLiteParameter("@Renk", secilenRenk)
                };

                DatabaseManager.ExecuteNonQuery(sorgu, parameters);

                MessageBox.Show("Ürün başarıyla güncellendi!", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün güncellenirken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void KategorileriYukle()
        {
            try
            {
                string sorgu = "SELECT KategoriId, KategoriAdi FROM Kategoriler ORDER BY KategoriAdi";
                var dt = DatabaseManager.ExecuteQuery(sorgu);

                cmbKategori.Items.Clear();
                foreach (DataRow dr in dt.Rows)
                {
                    cmbKategori.Items.Add(new KeyValuePair<int, string>(
                        Convert.ToInt32(dr["KategoriId"]),
                        dr["KategoriAdi"].ToString() ?? ""
                    ));
                }
                cmbKategori.DisplayMember = "Value";
                cmbKategori.ValueMember = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kategoriler yüklenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BirimYukle()
        {
            cmbBirim.Items.Clear();
            cmbBirim.Items.AddRange(new object[] { "Adet", "Kg", "Lt", "Mt" });
            cmbBirim.SelectedIndex = 0;
        }
    }
} 