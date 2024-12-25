using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Collections.Generic;

namespace StokTakip
{
    public partial class KategoriYonetimForm : Form
    {
        private DataGridView? dgvKategoriler;
        private TextBox? txtKategoriAdi;
        private TextBox? txtAciklama;
        private Button? btnKaydet;
        private int? duzenlenecekKategoriId = null;

        public KategoriYonetimForm()
        {
            InitializeComponent();
            FormTasarimOlustur();
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Kategori Yönetimi";
        }

        private void FormTasarimOlustur()
        {
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.Padding = new Padding(10);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Padding = new Padding(10);

            // Sol Panel (Form)
            Panel pnlSol = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Form başlığı
            Label lblBaslik = new Label
            {
                Text = "KATEGORİ BİLGİLERİ",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Kategori Adı
            Label lblKategoriAdi = new Label
            {
                Text = "Kategori Adı *",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(0, 60)
            };

            txtKategoriAdi = new TextBox
            {
                Width = 260,
                Height = 30,
                Location = new Point(0, 85),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Açıklama
            Label lblAciklama = new Label
            {
                Text = "Açıklama",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(0, 130)
            };

            txtAciklama = new TextBox
            {
                Width = 260,
                Height = 100,
                Location = new Point(0, 155),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true
            };

            // Kaydet Butonu
            btnKaydet = new Button
            {
                Text = "💾 KAYDET",
                Width = 260,
                Height = 45,
                Location = new Point(0, 275),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.Click += BtnKaydet_Click;

            // Sağ Panel (Liste)
            Panel pnlSag = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Liste Başlığı
            Label lblListeBaslik = new Label
            {
                Text = "KATEGORİ LİSTESİ",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // DataGridView
            dgvKategoriler = new DataGridView
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
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 40 }
            };

            dgvKategoriler.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgvKategoriler.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvKategoriler.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 45, 45);
            dgvKategoriler.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            dgvKategoriler.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgvKategoriler.DefaultCellStyle.ForeColor = Color.White;
            dgvKategoriler.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 183, 195);
            dgvKategoriler.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvKategoriler.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgvKategoriler.DefaultCellStyle.Padding = new Padding(5);

            dgvKategoriler.CellClick += DgvKategoriler_CellClick;

            // Kontrolleri panellere ekle
            pnlSol.Controls.AddRange(new Control[] { 
                lblBaslik, 
                lblKategoriAdi, txtKategoriAdi,
                lblAciklama, txtAciklama, 
                btnKaydet 
            });

            Panel gridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };
            gridContainer.Controls.Add(dgvKategoriler);
            
            pnlSag.Controls.AddRange(new Control[] {
                lblListeBaslik,
                gridContainer
            });

            mainLayout.Controls.Add(pnlSol, 0, 0);
            mainLayout.Controls.Add(pnlSag, 1, 0);
            
            this.Controls.Add(mainLayout);

            // İlk yüklemede kategorileri listele
            KategorileriListele();
        }

        private void KategorileriListele()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={Program.DbPath};Version=3;"))
                {
                    baglanti.Open();

                    string sorgu = "SELECT KategoriId, KategoriAdi, Aciklama, OlusturmaTarihi FROM Kategoriler ORDER BY KategoriAdi";

                    using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        da.Fill(dt);

                        if (dgvKategoriler != null)
                        {
                            dgvKategoriler.DataSource = null;
                            dgvKategoriler.Columns.Clear();
                            dgvKategoriler.DataSource = dt;
                            
                            // Sütunları düzenle
                            if (dgvKategoriler.Columns["KategoriId"] != null)
                                dgvKategoriler.Columns["KategoriId"].Visible = false;

                            if (dgvKategoriler.Columns["KategoriAdi"] != null)
                                dgvKategoriler.Columns["KategoriAdi"].HeaderText = "Kategori Adı";

                            if (dgvKategoriler.Columns["Aciklama"] != null)
                                dgvKategoriler.Columns["Aciklama"].HeaderText = "Açıklama";

                            if (dgvKategoriler.Columns["OlusturmaTarihi"] != null)
                            {
                                dgvKategoriler.Columns["OlusturmaTarihi"].HeaderText = "Oluşturma Tarihi";
                                dgvKategoriler.Columns["OlusturmaTarihi"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                            }

                            // Sil butonu ekle
                            DataGridViewButtonColumn btnSil = new DataGridViewButtonColumn
                            {
                                Name = "Sil",
                                Text = "🗑️ Sil",
                                UseColumnTextForButtonValue = true,
                                FlatStyle = FlatStyle.Flat
                            };
                            dgvKategoriler.Columns.Add(btnSil);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kategoriler listelenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvKategoriler_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvKategoriler?.CurrentRow == null) return;

            // Sil butonuna tıklandıysa
            if (e.ColumnIndex == dgvKategoriler.Columns["Sil"].Index)
            {
                try
                {
                    int kategoriId = Convert.ToInt32(dgvKategoriler.CurrentRow.Cells["KategoriId"].Value);
                    string kategoriAdi = dgvKategoriler.CurrentRow.Cells["KategoriAdi"].Value.ToString() ?? "";

                    var result = MessageBox.Show(
                        $"{kategoriAdi} kategorisini silmek istediğinize emin misiniz?",
                        "Kategori Silme Onayı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={Program.DbPath};Version=3;"))
                        {
                            baglanti.Open();

                            // Önce bu kategoriye bağlı ürünleri kontrol et
                            string kontrolSorgu = "SELECT COUNT(*) FROM Urunler WHERE KategoriId = @KategoriId";
                            using (SQLiteCommand cmd = new SQLiteCommand(kontrolSorgu, baglanti))
                            {
                                cmd.Parameters.AddWithValue("@KategoriId", kategoriId);
                                int urunSayisi = Convert.ToInt32(cmd.ExecuteScalar());
                                
                                if (urunSayisi > 0)
                                {
                                    MessageBox.Show(
                                        "Bu kategoriye bağlı ürünler bulunmaktadır. Önce bu ürünleri başka bir kategoriye taşıyın veya silin.", 
                                        "Uyarı", 
                                        MessageBoxButtons.OK, 
                                        MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // Kategoriyi sil
                            string silSorgu = "DELETE FROM Kategoriler WHERE KategoriId = @KategoriId";
                            using (SQLiteCommand cmd = new SQLiteCommand(silSorgu, baglanti))
                            {
                                cmd.Parameters.AddWithValue("@KategoriId", kategoriId);
                                cmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Kategori başarıyla silindi.", "Bilgi",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Listeyi güncelle
                            KategorileriListele();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kategori silinirken hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Düzenleme için verileri form alanlarına doldur
                if (txtKategoriAdi != null && txtAciklama != null && btnKaydet != null)
                {
                    duzenlenecekKategoriId = Convert.ToInt32(dgvKategoriler.CurrentRow.Cells["KategoriId"].Value);
                    txtKategoriAdi.Text = dgvKategoriler.CurrentRow.Cells["KategoriAdi"].Value.ToString();
                    txtAciklama.Text = dgvKategoriler.CurrentRow.Cells["Aciklama"].Value?.ToString() ?? "";
                    btnKaydet.Text = "💾 GÜNCELLE";
                }
            }
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            try
            {
                if (txtKategoriAdi == null)
                {
                    MessageBox.Show("Form alanları bulunamadı!", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Kategori adı kontrolü
                if (string.IsNullOrWhiteSpace(txtKategoriAdi.Text))
                {
                    MessageBox.Show("Lütfen kategori adını giriniz!", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={Program.DbPath};Version=3;"))
                {
                    baglanti.Open();

                    // Aynı isimde kategori var mı kontrol et
                    string kontrolSorgu = "SELECT COUNT(*) FROM Kategoriler WHERE KategoriAdi = @KategoriAdi" +
                        (duzenlenecekKategoriId.HasValue ? " AND KategoriId != @KategoriId" : "");
                    
                    using (SQLiteCommand cmd = new SQLiteCommand(kontrolSorgu, baglanti))
                    {
                        cmd.Parameters.AddWithValue("@KategoriAdi", txtKategoriAdi.Text.Trim());
                        if (duzenlenecekKategoriId.HasValue)
                            cmd.Parameters.AddWithValue("@KategoriId", duzenlenecekKategoriId.Value);

                        int kategoriSayisi = Convert.ToInt32(cmd.ExecuteScalar());
                        if (kategoriSayisi > 0)
                        {
                            MessageBox.Show("Bu isimde bir kategori zaten mevcut!", "Uyarı",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    string sorgu;
                    if (duzenlenecekKategoriId.HasValue)
                    {
                        // Güncelleme
                        sorgu = @"
                            UPDATE Kategoriler 
                            SET KategoriAdi = @KategoriAdi,
                                Aciklama = @Aciklama
                            WHERE KategoriId = @KategoriId";
                    }
                    else
                    {
                        // Yeni kayıt
                        sorgu = @"
                            INSERT INTO Kategoriler (KategoriAdi, Aciklama)
                            VALUES (@KategoriAdi, @Aciklama)";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(sorgu, baglanti))
                    {
                        cmd.Parameters.AddWithValue("@KategoriAdi", txtKategoriAdi.Text.Trim());
                        cmd.Parameters.AddWithValue("@Aciklama", txtAciklama?.Text?.Trim() ?? "");
                        
                        if (duzenlenecekKategoriId.HasValue)
                            cmd.Parameters.AddWithValue("@KategoriId", duzenlenecekKategoriId.Value);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Kategori başarıyla kaydedildi.", "Bilgi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Formu temizle
                    FormuSifirla();
                    // Listeyi güncelle
                    KategorileriListele();

                    // Ana formdaki ürün listesini güncelle
                    var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainForm != null)
                    {
                        mainForm.YenileUrunListesi();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kategori kaydedilirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormuSifirla()
        {
            if (txtKategoriAdi != null) txtKategoriAdi.Text = "";
            if (txtAciklama != null) txtAciklama.Text = "";
            if (btnKaydet != null) btnKaydet.Text = "💾 KAYDET";
            duzenlenecekKategoriId = null;
        }
    }
} 