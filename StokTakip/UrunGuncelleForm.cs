using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;

namespace StokTakip
{
    public partial class UrunGuncelleForm : Form
    {
        private SQLiteConnection baglanti;
        private string dbDosyasi = "StokTakip.db";
        private int urunId;

        public UrunGuncelleForm(int urunId)
        {
            this.urunId = urunId;
            VeritabaniBaglantisiOlustur();
            FormTasarimOlustur();
            UrunBilgileriniGetir();
        }

        private void VeritabaniBaglantisiOlustur()
        {
            try
            {
                string projeKlasoru = Application.StartupPath;
                string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);
                baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veritabanı bağlantısı oluşturulamadı: " + ex.Message);
            }
        }

        private void FormTasarimOlustur()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Size = new Size(400, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;

            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Başlık
            Label lblBaslik = new Label
            {
                Text = "ÜRÜN GÜNCELLE",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };

            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 20, 0, 0)
            };

            // Form Elemanları
            TextBox txtUrunAdi = YeniTextBox("Ürün Adı", "UrunAdi");
            TextBox txtAciklama = YeniTextBox("Açıklama", "Aciklama", 100);
            TextBox txtMiktar = YeniTextBox("Miktar", "Miktar");
            TextBox txtTedarikci = YeniTextBox("Tedarikçi", "Tedarikci");
            TextBox txtMinStok = YeniTextBox("Min. Stok", "MinStok");
            TextBox txtKritikStok = YeniTextBox("Kritik Stok", "KritikStok");
            TextBox txtRafKodu = YeniTextBox("Raf Kodu", "RafKodu");
            TextBox txtAlisFiyati = YeniTextBox("Alış Fiyatı", "AlisFiyati");
            TextBox txtSatisFiyati = YeniTextBox("Satış Fiyatı", "SatisFiyati");

            // Birim ComboBox
            ComboBox cmbBirim = new ComboBox
            {
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = "Birim"
            };
            cmbBirim.Items.AddRange(new string[] { "Adet", "Kg", "Lt", "Mt" });

            // Miktar Panel
            Panel pnlMiktar = new Panel
            {
                Size = new Size(350, 35),
                Margin = new Padding(0, 10, 0, 10)
            };
            txtMiktar.Width = 240;
            cmbBirim.Width = 100;
            pnlMiktar.Controls.AddRange(new Control[] { txtMiktar, cmbBirim });

            // Min ve Kritik Stok Panel
            Panel pnlStok = new Panel
            {
                Size = new Size(350, 35),
                Margin = new Padding(0, 10, 0, 10)
            };
            txtMinStok.Width = 170;
            txtKritikStok.Width = 170;
            txtMinStok.Dock = DockStyle.Left;
            txtKritikStok.Dock = DockStyle.Right;
            pnlStok.Controls.AddRange(new Control[] { txtMinStok, txtKritikStok });

            // Alış ve Satış Fiyatı Panel
            Panel pnlFiyat = new Panel
            {
                Size = new Size(350, 35),
                Margin = new Padding(0, 10, 0, 10)
            };
            txtAlisFiyati.Width = 170;
            txtSatisFiyati.Width = 170;
            txtAlisFiyati.Dock = DockStyle.Left;
            txtSatisFiyati.Dock = DockStyle.Right;
            pnlFiyat.Controls.AddRange(new Control[] { txtAlisFiyati, txtSatisFiyati });

            // Butonlar
            Button btnGuncelle = new Button
            {
                Text = "GÜNCELLE",
                Size = new Size(350, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 151, 230),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Margin = new Padding(0, 20, 0, 0)
            };
            btnGuncelle.FlatAppearance.BorderSize = 0;

            Button btnIptal = new Button
            {
                Text = "İPTAL",
                Size = new Size(350, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.Silver,
                Font = new Font("Segoe UI", 11),
                Margin = new Padding(0, 10, 0, 0)
            };
            btnIptal.FlatAppearance.BorderColor = Color.DimGray;

            // Kontrolleri Flow Panel'e ekleme
            flowPanel.Controls.AddRange(new Control[] {
                txtUrunAdi,
                txtAciklama,
                pnlMiktar,
                txtTedarikci,
                pnlStok,
                txtRafKodu,
                pnlFiyat,
                btnGuncelle,
                btnIptal
            });

            btnGuncelle.Click += (s, e) => UrunGuncelle(new Control[] {
                txtUrunAdi, txtAciklama, txtMiktar, cmbBirim, txtTedarikci,
                txtMinStok, txtKritikStok, txtRafKodu, txtAlisFiyati, txtSatisFiyati
            });
            btnIptal.Click += (s, e) => this.Close();

            pnlAna.Controls.AddRange(new Control[] { lblBaslik, flowPanel });
            this.Controls.Add(pnlAna);

            // Form taşıma için mouse events
            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { this.Capture = false; Message m = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero); this.WndProc(ref m); } };
        }

        private TextBox YeniTextBox(string placeholder, string tag, int height = 35)
        {
            TextBox txt = new TextBox
            {
                Size = new Size(350, height),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Multiline = height > 35,
                Margin = new Padding(0, 10, 0, 10),
                Tag = tag
            };

            return txt;
        }

        private void UrunBilgileriniGetir()
        {
            try
            {
                baglanti.Open();
                string sorgu = "SELECT * FROM Urunler WHERE Id = @UrunId";
                using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                {
                    cmd.Parameters.AddWithValue("@UrunId", urunId);
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // Form kontrollerini bul ve değerleri ata
                            foreach (Control ctrl in this.Controls[0].Controls[0].Controls)
                            {
                                if (ctrl is TextBox txt)
                                {
                                    string kolonAdi = txt.Tag?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(kolonAdi))
                                    {
                                        txt.Text = dr[kolonAdi].ToString();
                                        txt.ForeColor = Color.White;
                                    }
                                }
                                else if (ctrl is ComboBox cmb)
                                {
                                    cmb.SelectedItem = dr["Birim"].ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ürün bilgileri getirme hatası: " + ex.Message);
            }
            finally
            {
                baglanti.Close();
            }
        }

        private void UrunGuncelle(Control[] controls)
        {
            try
            {
                baglanti.Open();
                string sorgu = @"UPDATE Urunler SET 
                    UrunAdi = @UrunAdi,
                    Aciklama = @Aciklama,
                    Miktar = @Miktar,
                    Birim = @Birim,
                    Tedarikci = @Tedarikci,
                    MinStok = @MinStok,
                    KritikStok = @KritikStok,
                    RafKodu = @RafKodu,
                    AlisFiyati = @AlisFiyati,
                    SatisFiyati = @SatisFiyati,
                    Beden = @Beden,
                    Renk = @Renk
                    WHERE Id = @UrunId";

                using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                {
                    cmd.Parameters.AddWithValue("@UrunId", urunId);
                    foreach (Control ctrl in controls)
                    {
                        string paramName = "@" + ctrl.Tag.ToString();
                        if (ctrl is TextBox txt)
                        {
                            if (decimal.TryParse(txt.Text, out decimal decimalValue))
                                cmd.Parameters.AddWithValue(paramName, decimalValue);
                            else if (int.TryParse(txt.Text, out int intValue))
                                cmd.Parameters.AddWithValue(paramName, intValue);
                            else
                                cmd.Parameters.AddWithValue(paramName, txt.Text);
                        }
                        else if (ctrl is ComboBox cmb)
                        {
                            cmd.Parameters.AddWithValue(paramName, cmb.SelectedItem?.ToString() ?? "Adet");
                        }
                    }
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Ürün başarıyla güncellendi!", "Başarılı");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güncelleme hatası: " + ex.Message);
            }
            finally
            {
                baglanti.Close();
            }
        }
    }
} 