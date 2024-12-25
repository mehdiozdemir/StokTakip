using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace StokTakip
{
    public partial class SatisForm : Form
    {
        private SQLiteConnection baglanti;
        private string dbDosyasi = "StokTakip.db";
        private int urunId;
        private string urunAdi;
        private int mevcutStok;
        private decimal satisFiyati;

        public SatisForm(int urunId, string urunAdi, int mevcutStok, decimal satisFiyati)
        {
            this.urunId = urunId;
            this.urunAdi = urunAdi;
            this.mevcutStok = mevcutStok;
            this.satisFiyati = satisFiyati;
            
            VeritabaniBaglantisiOlustur();
            FormTasarimOlustur();
        }

        private void VeritabaniBaglantisiOlustur()
        {
            string projeKlasoru = Application.StartupPath;
            string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
            baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;");
        }

        private void FormTasarimOlustur()
        {
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(24, 28, 63);

            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25)
            };

            // Başlık Paneli
            Panel pnlBaslik = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(28, 33, 75),
                Padding = new Padding(10, 0, 10, 0)
            };

            Label lblBaslik = new Label
            {
                Text = "SATIŞ İŞLEMİ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button btnKapat = new Button
            {
                Text = "✕",
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btnKapat.FlatAppearance.BorderSize = 0;
            btnKapat.Click += (s, e) => this.Close();

            pnlBaslik.Controls.AddRange(new Control[] { btnKapat, lblBaslik });

            // Ürün Bilgi Kartı
            Panel pnlUrunBilgi = new Panel
            {
                BackColor = Color.FromArgb(34, 39, 85),
                Location = new Point(25, 60),
                Size = new Size(350, 100),
                Padding = new Padding(15)
            };

            Label lblUrunBilgi = new Label
            {
                Text = $"📦 {urunAdi}\n📊 Mevcut Stok: {mevcutStok}\n💰 Satış Fiyatı: ₺{satisFiyati:N2}",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 11),
                Location = new Point(15, 15),
                AutoSize = true
            };

            pnlUrunBilgi.Controls.Add(lblUrunBilgi);

            // Form Elemanları
            TextBox txtMusteriAdi = new TextBox
            {
                Size = new Size(350, 40),
                Location = new Point(25, 180),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(34, 39, 85),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "👤 Müşteri Adı"
            };

            Panel pnlMusteriAdiLine = new Panel
            {
                Size = new Size(350, 2),
                Location = new Point(25, 220),
                BackColor = Color.FromArgb(0, 183, 195)
            };

            // Miktar ve Fiyat için Panel
            Panel pnlMiktarFiyat = new Panel
            {
                Size = new Size(350, 45),
                Location = new Point(25, 240),
                BackColor = Color.FromArgb(34, 39, 85)
            };

            NumericUpDown numAdet = new NumericUpDown
            {
                Size = new Size(170, 40),
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(34, 39, 85),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Minimum = 1,
                Maximum = mevcutStok,
                Value = 1
            };

            TextBox txtSatisFiyati = new TextBox
            {
                Size = new Size(150, 40),
                Location = new Point(190, 10),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(34, 39, 85),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Text = satisFiyati.ToString("N2"),
                PlaceholderText = "Satış Fiyatı"
            };

            Label lblToplam = new Label
            {
                Text = $"Toplam: ₺{satisFiyati:N2}",
                ForeColor = Color.FromArgb(0, 183, 195),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(25, 300),
                AutoSize = true
            };

            // Fiyat ve adet değiştiğinde toplamı güncelle
            EventHandler toplamGuncelle = (s, e) =>
            {
                if (decimal.TryParse(txtSatisFiyati.Text, out decimal fiyat))
                {
                    decimal toplam = fiyat * numAdet.Value;
                    lblToplam.Text = $"Toplam: ₺{toplam:N2}";
                }
            };

            numAdet.ValueChanged += toplamGuncelle;
            txtSatisFiyati.TextChanged += toplamGuncelle;

            // Butonlar
            Button btnSatisYap = new Button
            {
                Text = "SATIŞI TAMAMLA",
                Size = new Size(350, 45),
                Location = new Point(25, 350),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSatisYap.FlatAppearance.BorderSize = 0;

            Button btnIptal = new Button
            {
                Text = "İPTAL",
                Size = new Size(350, 35),
                Location = new Point(25, 405),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 28, 63),
                ForeColor = Color.FromArgb(0, 183, 195),
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand
            };
            btnIptal.FlatAppearance.BorderColor = Color.FromArgb(0, 183, 195);

            btnSatisYap.Click += (s, e) => SatisYap(txtMusteriAdi.Text, (int)numAdet.Value, 
                decimal.Parse(txtSatisFiyati.Text));
            btnIptal.Click += (s, e) => this.Close();

            // Kontrolleri forma ekle
            pnlAna.Controls.AddRange(new Control[] {
                pnlBaslik,
                pnlUrunBilgi,
                txtMusteriAdi,
                pnlMusteriAdiLine,
                pnlMiktarFiyat,
                lblToplam,
                btnSatisYap,
                btnIptal
            });

            pnlMiktarFiyat.Controls.AddRange(new Control[] { numAdet, txtSatisFiyati });
            this.Controls.Add(pnlAna);

            // Form taşıma için mouse events
            pnlBaslik.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { this.Capture = false; Message m = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero); this.WndProc(ref m); } };
        }

        private void SatisYap(string musteriAdi, int adet, decimal satisFiyati)
        {
            if (string.IsNullOrWhiteSpace(musteriAdi))
            {
                MessageBox.Show("Lütfen müşteri adını girin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                baglanti.Open();
                using (var transaction = baglanti.BeginTransaction())
                {
                    try
                    {
                        decimal toplamTutar = adet * satisFiyati;
                        
                        // Önce Satislar tablosuna ana satış kaydını ekle
                        string satisSorgu = @"
                            INSERT INTO Satislar (MusteriAdi, ToplamTutar, OdemeTipi, SatisTarihi) 
                            VALUES (@MusteriAdi, @ToplamTutar, 'Nakit', @SatisTarihi);
                            SELECT last_insert_rowid();";

                        int satisId;
                        using (SQLiteCommand cmd = new SQLiteCommand(satisSorgu, baglanti))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddWithValue("@MusteriAdi", musteriAdi);
                            cmd.Parameters.AddWithValue("@ToplamTutar", toplamTutar);
                            cmd.Parameters.AddWithValue("@SatisTarihi", DateTime.Now);
                            satisId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Sonra SatisDetaylari tablosuna detay kaydını ekle
                        string detaySorgu = @"
                            INSERT INTO SatisDetaylari (SatisId, UrunId, Miktar, BirimFiyat, ToplamFiyat) 
                            VALUES (@SatisId, @UrunId, @Miktar, @BirimFiyat, @ToplamFiyat)";

                        using (SQLiteCommand cmd = new SQLiteCommand(detaySorgu, baglanti))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddWithValue("@SatisId", satisId);
                            cmd.Parameters.AddWithValue("@UrunId", urunId);
                            cmd.Parameters.AddWithValue("@Miktar", adet);
                            cmd.Parameters.AddWithValue("@BirimFiyat", satisFiyati);
                            cmd.Parameters.AddWithValue("@ToplamFiyat", toplamTutar);
                            cmd.ExecuteNonQuery();
                        }

                        // Stok miktarını güncelle
                        string stokSorgu = "UPDATE Urunler SET Miktar = Miktar - @Adet WHERE Id = @UrunId";
                        using (SQLiteCommand cmd = new SQLiteCommand(stokSorgu, baglanti))
                        {
                            cmd.Transaction = transaction;
                            cmd.Parameters.AddWithValue("@Adet", adet);
                            cmd.Parameters.AddWithValue("@UrunId", urunId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("Satış başarıyla kaydedildi.", "Başarılı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Satış işlemi sırasında bir hata oluştu: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Satış işlemi sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (baglanti.State == ConnectionState.Open)
                    baglanti.Close();
            }
        }
    }
} 