using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using System.IO;

namespace StokTakip
{
    public partial class Form1 : Form
    {
        private SQLiteConnection baglanti;
        private string dbDosyasi = "StokTakip.db";
        
        public Form1()
        {
            InitializeComponent();
            VeritabaniBaglantisiOlustur();
            ControllerOlustur();
            TabloOlustur();
        }

        private void VeritabaniBaglantisiOlustur()
        {
            string projeKlasoru = Application.StartupPath;
            string dbYolu = Path.Combine(projeKlasoru, dbDosyasi);

            if (!File.Exists(dbYolu))
            {
                SQLiteConnection.CreateFile(dbYolu);
            }
            baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;");
        }

        private void ControllerOlustur()
        {
            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(pnlAna);

            // Üst Panel
            Panel pnlUst = new Panel
            {
                Height = 150,
                Dock = DockStyle.Top
            };
            pnlAna.Controls.Add(pnlUst);

            // TextBox'lar
            TextBox txtUrunAdi = new TextBox
            {
                Location = new Point(120, 50),
                Name = "txtUrunAdi"
            };
            TextBox txtFiyat = new TextBox
            {
                Location = new Point(120, 80),
                Name = "txtFiyat"
            };
            TextBox txtMiktar = new TextBox
            {
                Location = new Point(120, 110),
                Name = "txtMiktar"
            };

            // Label'lar
            Label lblUrunAdi = new Label
            {
                Text = "Ürün Adı:",
                Location = new Point(20, 53)
            };
            Label lblFiyat = new Label
            {
                Text = "Fiyat:",
                Location = new Point(20, 83)
            };
            Label lblMiktar = new Label
            {
                Text = "Miktar:",
                Location = new Point(20, 113)
            };

            // Butonlar
            Button btnEkle = new Button
            {
                Text = "Ekle",
                Location = new Point(300, 20),
                Size = new Size(100, 30)
            };
            Button btnGuncelle = new Button
            {
                Text = "Güncelle",
                Location = new Point(300, 60),
                Size = new Size(100, 30)
            };
            Button btnSil = new Button
            {
                Text = "Sil",
                Location = new Point(300, 100),
                Size = new Size(100, 30)
            };

            // DataGridView
            DataGridView dgvUrunler = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 300,
                Name = "dgvUrunler",
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Kontrolleri panele ekleme
            pnlUst.Controls.AddRange(new Control[] { 
                txtUrunAdi, txtFiyat, txtMiktar,
                lblUrunAdi, lblFiyat, lblMiktar,
                btnEkle, btnGuncelle, btnSil
            });
            pnlAna.Controls.Add(dgvUrunler);

            // Event handler'ları
            btnEkle.Click += (s, e) => UrunEkle();
            btnGuncelle.Click += (s, e) => UrunGuncelle();
            btnSil.Click += (s, e) => UrunSil();
            dgvUrunler.CellClick += (s, e) => DataGridViewSecim(e);
        }

        private void TabloOlustur()
        {
            try
            {
                // Önce eski tabloyu silelim
                baglanti.Open();
                string dropTable = "DROP TABLE IF EXISTS Urunler";
                SQLiteCommand dropCmd = new SQLiteCommand(dropTable, baglanti);
                dropCmd.ExecuteNonQuery();
                baglanti.Close();

                // Yeni tabloyu oluşturalım
                baglanti.Open();
                string sorgu = @"
                    CREATE TABLE IF NOT EXISTS Urunler (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UrunAdi TEXT NOT NULL,
                        Aciklama TEXT,
                        Miktar INTEGER NOT NULL,
                        Birim TEXT NOT NULL DEFAULT 'Adet',
                        Tedarikci TEXT,
                        MinStok INTEGER DEFAULT 0,
                        KritikStok INTEGER DEFAULT 0,
                        RafKodu TEXT,
                        AlisFiyati DECIMAL(10,2) DEFAULT 0,
                        SatisFiyati DECIMAL(10,2) DEFAULT 0
                    )";
                SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tablo oluşturma hatası: " + ex.Message);
            }
            finally
            {
                if (baglanti.State == ConnectionState.Open)
                    baglanti.Close();
            }
            VerileriGetir();
        }

        private void VerileriGetir()
        {
            SQLiteConnection yeniBaglanti = null;
            try
            {
                yeniBaglanti = new SQLiteConnection($"Data Source={dbDosyasi};Version=3;");
                yeniBaglanti.Open();
                string sorgu = "SELECT * FROM Urunler";
                SQLiteDataAdapter da = new SQLiteDataAdapter(sorgu, yeniBaglanti);
                DataTable dt = new DataTable();
                da.Fill(dt);
                DataGridView dgv = (DataGridView)Controls.Find("dgvUrunler", true)[0];
                dgv.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri getirme hatası: " + ex.Message);
            }
            finally
            {
                if (yeniBaglanti != null)
                {
                    yeniBaglanti.Close();
                    yeniBaglanti.Dispose();
                }
            }
        }

        private void UrunEkle()
        {
            try
            {
                TextBox txtUrunAdi = (TextBox)Controls.Find("txtUrunAdi", true)[0];
                TextBox txtFiyat = (TextBox)Controls.Find("txtFiyat", true)[0];
                TextBox txtMiktar = (TextBox)Controls.Find("txtMiktar", true)[0];

                if (string.IsNullOrEmpty(txtUrunAdi.Text))
                {
                    MessageBox.Show("Lütfen gerekli alanları doldurun!");
                    return;
                }

                baglanti.Open();
                string sorgu = @"INSERT INTO Urunler 
                    (UrunAdi, Miktar, Birim, AlisFiyati, SatisFiyati) 
                    VALUES 
                    (@UrunAdi, @Miktar, @Birim, @AlisFiyati, @SatisFiyati)";

                SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti);
                cmd.Parameters.AddWithValue("@UrunAdi", txtUrunAdi.Text);
                cmd.Parameters.AddWithValue("@Miktar", Convert.ToInt32(txtMiktar.Text));
                cmd.Parameters.AddWithValue("@Birim", "Adet");
                cmd.Parameters.AddWithValue("@AlisFiyati", Convert.ToDecimal(txtFiyat.Text));
                cmd.Parameters.AddWithValue("@SatisFiyati", Convert.ToDecimal(txtFiyat.Text));
                cmd.ExecuteNonQuery();
                MessageBox.Show("Ürün başarıyla eklendi!");
                
                FormuTemizle();
                VerileriGetir();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ürün ekleme hatası: " + ex.Message);
            }
            finally
            {
                baglanti.Close();
            }
        }

        private void UrunGuncelle()
        {
            try
            {
                DataGridView dgv = (DataGridView)Controls.Find("dgvUrunler", true)[0];
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen güncellenecek ürünü seçin!");
                    return;
                }

                TextBox txtUrunAdi = (TextBox)Controls.Find("txtUrunAdi", true)[0];
                TextBox txtFiyat = (TextBox)Controls.Find("txtFiyat", true)[0];
                TextBox txtMiktar = (TextBox)Controls.Find("txtMiktar", true)[0];

                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["Id"].Value);

                baglanti.Open();
                string sorgu = @"UPDATE Urunler 
                    SET UrunAdi=@UrunAdi, Miktar=@Miktar, AlisFiyati=@AlisFiyati, SatisFiyati=@SatisFiyati 
                    WHERE Id=@Id";

                SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@UrunAdi", txtUrunAdi.Text);
                cmd.Parameters.AddWithValue("@Miktar", Convert.ToInt32(txtMiktar.Text));
                cmd.Parameters.AddWithValue("@AlisFiyati", Convert.ToDecimal(txtFiyat.Text));
                cmd.Parameters.AddWithValue("@SatisFiyati", Convert.ToDecimal(txtFiyat.Text));
                cmd.ExecuteNonQuery();
                MessageBox.Show("Ürün başarıyla güncellendi!");

                FormuTemizle();
                VerileriGetir();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ürün güncelleme hatası: " + ex.Message);
            }
            finally
            {
                baglanti.Close();
            }
        }

        private void UrunSil()
        {
            try
            {
                DataGridView dgv = (DataGridView)Controls.Find("dgvUrunler", true)[0];
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Lütfen silinecek ürünü seçin!");
                    return;
                }

                if (MessageBox.Show("Seçili ürünü silmek istediğinizden emin misiniz?", "Silme Onayı", 
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["Id"].Value);

                    baglanti.Open();
                    string sorgu = "DELETE FROM Urunler WHERE Id=@Id";
                    SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Ürün başarıyla silindi!");

                    FormuTemizle();
                    VerileriGetir();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ürün silme hatası: " + ex.Message);
            }
            finally
            {
                baglanti.Close();
            }
        }

        private void DataGridViewSecim(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridView dgv = (DataGridView)Controls.Find("dgvUrunler", true)[0];
                TextBox txtUrunAdi = (TextBox)Controls.Find("txtUrunAdi", true)[0];
                TextBox txtFiyat = (TextBox)Controls.Find("txtFiyat", true)[0];
                TextBox txtMiktar = (TextBox)Controls.Find("txtMiktar", true)[0];

                txtUrunAdi.Text = dgv.Rows[e.RowIndex].Cells["UrunAdi"].Value.ToString();
                txtFiyat.Text = dgv.Rows[e.RowIndex].Cells["AlisFiyati"].Value.ToString();
                txtMiktar.Text = dgv.Rows[e.RowIndex].Cells["Miktar"].Value.ToString();
            }
        }

        private void FormuTemizle()
        {
            TextBox txtUrunAdi = (TextBox)Controls.Find("txtUrunAdi", true)[0];
            TextBox txtFiyat = (TextBox)Controls.Find("txtFiyat", true)[0];
            TextBox txtMiktar = (TextBox)Controls.Find("txtMiktar", true)[0];

            txtUrunAdi.Clear();
            txtFiyat.Clear();
            txtMiktar.Clear();
        }
    }
}
