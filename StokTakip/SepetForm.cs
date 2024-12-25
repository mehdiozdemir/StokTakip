using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace StokTakip
{
    public class SepetForm : Form
    {
        private readonly Sepet sepet;
        private DataGridView? dgvSepet;
        private Label? lblToplamTutar;

        public SepetForm(Sepet sepet)
        {
            this.sepet = sepet;
            InitializeComponent();
            FormTasarimOlustur();
            SepetGuncelle();
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Text = "Sepet";
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

            // Başlık
            Label lblBaslik = new Label
            {
                Text = "SEPET",
                Dock = DockStyle.Top,
                Height = 50,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // DataGridView
            dgvSepet = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 40 },
                GridColor = Color.FromArgb(45, 45, 45),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            dgvSepet.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(45, 45, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            dgvSepet.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(50, 50, 50),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(10, 5, 10, 5)
            };

            // Alt Panel
            Panel pnlAlt = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Toplam Tutar
            lblToplamTutar = new Label
            {
                Text = "Toplam: ₺0,00",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Sepeti Temizle Butonu
            Button btnTemizle = new Button
            {
                Text = "🗑️ Sepeti Temizle",
                Width = 180,
                Height = 45,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTemizle.FlatAppearance.BorderSize = 0;
            btnTemizle.Click += BtnTemizle_Click;

            // Satışı Tamamla Butonu
            Button btnTamamla = new Button
            {
                Text = "💰 Satışı Tamamla",
                Width = 180,
                Height = 45,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            btnTamamla.FlatAppearance.BorderSize = 0;
            btnTamamla.Click += BtnTamamla_Click;

            // Kontrolleri panellere ekle
            pnlAlt.Controls.AddRange(new Control[] { lblToplamTutar, btnTamamla, btnTemizle });
            pnlAna.Controls.AddRange(new Control[] { lblBaslik, dgvSepet, pnlAlt });
            this.Controls.Add(pnlAna);
        }

        public void SepetGuncelle()
        {
            if (dgvSepet == null || lblToplamTutar == null) return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(SepetGuncelle));
                    return;
                }

                // DataGridView'i temizle
                dgvSepet.DataSource = null;
                dgvSepet.Columns.Clear();

                // Kolonları manuel olarak oluştur
                dgvSepet.Columns.AddRange(
                    new DataGridViewTextBoxColumn
                    {
                        Name = "UrunId",
                        HeaderText = "UrunId",
                        Visible = false
                    },
                    new DataGridViewTextBoxColumn
                    {
                        Name = "UrunAdi",
                        HeaderText = "ÜRÜN ADI",
                        Width = 400,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = DataGridViewContentAlignment.MiddleLeft,
                            Padding = new Padding(10, 0, 0, 0)
                        }
                    },
                    new DataGridViewTextBoxColumn
                    {
                        Name = "Adet",
                        HeaderText = "ADET",
                        Width = 150,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = DataGridViewContentAlignment.MiddleCenter
                        }
                    },
                    new DataGridViewTextBoxColumn
                    {
                        Name = "BirimFiyat",
                        HeaderText = "BİRİM FİYAT",
                        Width = 200,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = DataGridViewContentAlignment.MiddleRight,
                            Format = "N2",
                            Padding = new Padding(0, 0, 10, 0)
                        }
                    },
                    new DataGridViewTextBoxColumn
                    {
                        Name = "ToplamFiyat",
                        HeaderText = "TOPLAM FİYAT",
                        Width = 200,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = DataGridViewContentAlignment.MiddleRight,
                            Format = "N2",
                            Padding = new Padding(0, 0, 10, 0)
                        }
                    }
                );

                // DataTable oluştur ve doldur
                var dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[]
                {
                    new DataColumn("UrunId", typeof(int)),
                    new DataColumn("UrunAdi", typeof(string)),
                    new DataColumn("Adet", typeof(int)),
                    new DataColumn("BirimFiyat", typeof(decimal)),
                    new DataColumn("ToplamFiyat", typeof(decimal))
                });

                foreach (var urun in sepet.Urunler)
                {
                    dt.Rows.Add(
                        urun.UrunId,
                        urun.UrunAdi,
                        urun.Adet,
                        urun.BirimFiyat,
                        urun.ToplamFiyat
                    );
                }

                // DataSource'u ayarla
                dgvSepet.DataSource = dt;

                // Toplam tutarı güncelle
                lblToplamTutar.Text = $"Toplam: {sepet.ToplamTutar:C2}";

                // Satır renklerini ayarla
                foreach (DataGridViewRow row in dgvSepet.Rows)
                {
                    row.DefaultCellStyle.BackColor = row.Index % 2 == 0 
                        ? Color.FromArgb(35, 35, 35) 
                        : Color.FromArgb(40, 40, 40);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sepet güncellenirken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTemizle_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Sepeti temizlemek istediğinizden emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                sepet.Temizle();
                SepetGuncelle();
            }
        }

        private void BtnTamamla_Click(object? sender, EventArgs e)
        {
            if (sepet.Urunler.Count == 0)
            {
                MessageBox.Show("Sepet boş!", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var musteriForm = new MusteriForm())
            {
                if (musteriForm.ShowDialog() == DialogResult.OK)
                {
                    string musteriAdi = musteriForm.MusteriAdi;
                    try
                    {
                        string projeKlasoru = Application.StartupPath;
                        string dbYolu = Path.Combine(projeKlasoru, "StokTakip.db");
                        using (SQLiteConnection baglanti = new SQLiteConnection($"Data Source={dbYolu};Version=3;"))
                        {
                            baglanti.Open();
                            using (var transaction = baglanti.BeginTransaction())
                            {
                                try
                                {
                                    foreach (var urun in sepet.Urunler)
                                    {
                                        // Satış kaydı ekle
                                        string satisSorgu = @"
                                            INSERT INTO Satislar (SatisId, MusteriAdi, SatisTarihi)
                                            VALUES ((SELECT COALESCE(MAX(SatisId), 0) + 1 FROM Satislar), @MusteriAdi, datetime('now', 'localtime'));
                                            
                                            INSERT INTO SatisDetaylari (SatisId, UrunId, Miktar, BirimFiyat)
                                            VALUES ((SELECT MAX(SatisId) FROM Satislar), @UrunId, @Miktar, @BirimFiyat)";

                                        using (var cmd = new SQLiteCommand(satisSorgu, baglanti))
                                        {
                                            cmd.Parameters.AddWithValue("@UrunId", urun.UrunId);
                                            cmd.Parameters.AddWithValue("@MusteriAdi", musteriAdi);
                                            cmd.Parameters.AddWithValue("@Miktar", urun.Adet);
                                            cmd.Parameters.AddWithValue("@BirimFiyat", urun.BirimFiyat);
                                            cmd.ExecuteNonQuery();
                                        }

                                        // Stok güncelleme
                                        string stokSorgu = @"
                                            UPDATE Urunler 
                                            SET Miktar = Miktar - @Adet 
                                            WHERE Id = @UrunId";

                                        using (var cmd = new SQLiteCommand(stokSorgu, baglanti))
                                        {
                                            cmd.Parameters.AddWithValue("@Adet", urun.Adet);
                                            cmd.Parameters.AddWithValue("@UrunId", urun.UrunId);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    transaction.Commit();
                                    MessageBox.Show("Satış başarıyla tamamlandı!", "Başarılı",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    sepet.Temizle();
                                    SepetGuncelle();

                                    // Raporlar formunu güncelle
                                    var raporlarForm = Application.OpenForms.OfType<RaporlarForm>().FirstOrDefault();
                                    raporlarForm?.RaporGoster();
                                }
                                catch (Exception)
                                {
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Satış işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private class MusteriForm : Form
        {
            private TextBox txtMusteriAdi;
            public string MusteriAdi => txtMusteriAdi.Text.Trim();

            public MusteriForm()
            {
                Text = "Müşteri Bilgileri";
                Size = new Size(400, 200);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                BackColor = Color.FromArgb(30, 30, 30);

                Panel pnlIcerik = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20)
                };

                Label lblMusteriAdi = new Label
                {
                    Text = "Müşteri Adı:",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10),
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                txtMusteriAdi = new TextBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(340, 30),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White
                };

                Button btnTamam = new Button
                {
                    Text = "TAMAM",
                    Location = new Point(20, 90),
                    Size = new Size(340, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 183, 195),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                btnTamam.FlatAppearance.BorderSize = 0;

                pnlIcerik.Controls.AddRange(new Control[] { lblMusteriAdi, txtMusteriAdi, btnTamam });
                Controls.Add(pnlIcerik);

                AcceptButton = btnTamam;
            }
        }
    }
} 