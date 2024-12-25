using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace StokTakip
{
    public partial class IadeForm : Form
    {
        private readonly int satisId;
        private readonly int urunId;
        private readonly decimal birimFiyat;
        private int maxIadeAdet;
        private int kalanIadeHakki;

        public IadeForm(int satisId, int urunId, string urunAdi, int satisAdet, decimal birimFiyat)
        {
            this.satisId = satisId;
            this.urunId = urunId;
            this.maxIadeAdet = satisAdet;
            this.birimFiyat = birimFiyat;
            this.kalanIadeHakki = satisAdet;

            KalanIadeHakkiniHesapla();
            FormTasarimOlustur(urunAdi ?? "Bilinmeyen Ürün", satisAdet, birimFiyat);
        }

        private void KalanIadeHakkiniHesapla()
        {
            try
            {
                string kontrolSql = @"
                    SELECT 
                        sd.Miktar as SatisMiktari,
                        COALESCE((SELECT SUM(Miktar) FROM Iadeler WHERE SatisId = s.SatisId), 0) as ToplamIadeEdilen
                    FROM Satislar s
                    JOIN SatisDetaylari sd ON s.SatisId = sd.SatisId
                    WHERE s.SatisId = @SatisId";

                var kontrolParams = new[] { new SQLiteParameter("@SatisId", satisId) };
                var kontrolDt = DatabaseManager.ExecuteQuery(kontrolSql, kontrolParams);

                if (kontrolDt.Rows.Count > 0)
                {
                    int satisMiktari = Convert.ToInt32(kontrolDt.Rows[0]["SatisMiktari"]);
                    int toplamIadeEdilen = Convert.ToInt32(kontrolDt.Rows[0]["ToplamIadeEdilen"]);
                    kalanIadeHakki = satisMiktari - toplamIadeEdilen;
                    maxIadeAdet = kalanIadeHakki;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İade hakkı hesaplanırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormTasarimOlustur(string urunAdi, int satisAdet, decimal birimFiyat)
        {
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(18, 18, 18);

            // Ana Panel
            Panel pnlAna = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(18, 18, 18)
            };

            // Başlık Paneli
            Panel pnlBaslik = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20, 0, 15, 0)
            };

            Label lblBaslik = new Label
            {
                Text = "İade İşlemi",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button btnKapat = new Button
            {
                Text = "✕",
                Size = new Size(40, 60),
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
                BackColor = Color.FromArgb(30, 30, 30),
                Location = new Point(25, 85),
                Size = new Size(450, 180),
                Padding = new Padding(25)
            };

            TableLayoutPanel tblUrunBilgi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                RowStyles = {
                    new RowStyle(SizeType.Percent, 25),
                    new RowStyle(SizeType.Percent, 25),
                    new RowStyle(SizeType.Percent, 25),
                    new RowStyle(SizeType.Percent, 25)
                },
                ColumnStyles = {
                    new ColumnStyle(SizeType.Absolute, 150),
                    new ColumnStyle(SizeType.Percent, 100)
                }
            };
            tblUrunBilgi.Padding = new Padding(0, 5, 0, 5);
            tblUrunBilgi.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

            Label[] labels = new Label[]
            {
                new Label { Text = "Ürün:", ForeColor = Color.FromArgb(180, 180, 180), Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 10, 0) },
                new Label { Text = urunAdi, ForeColor = Color.White, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
                new Label { Text = "Satış Adedi:", ForeColor = Color.FromArgb(180, 180, 180), Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 10, 0) },
                new Label { Text = satisAdet.ToString(), ForeColor = Color.White, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
                new Label { Text = "Birim Fiyat:", ForeColor = Color.FromArgb(180, 180, 180), Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 10, 0) },
                new Label { Text = $"₺{birimFiyat:N2}", ForeColor = Color.White, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft },
                new Label { Text = "Kalan İade:", ForeColor = Color.FromArgb(180, 180, 180), Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 10, 0) },
                new Label { Text = $"{kalanIadeHakki} adet", ForeColor = Color.Lime, Font = new Font("Segoe UI", 11, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }
            };

            for (int i = 0; i < labels.Length; i++)
            {
                tblUrunBilgi.Controls.Add(labels[i], i % 2, i / 2);
            }

            pnlUrunBilgi.Controls.Add(tblUrunBilgi);

            // İade Adedi için Panel
            Panel pnlIadeAdet = new Panel
            {
                Size = new Size(450, 60),
                Location = new Point(25, 285),
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(25, 0, 25, 0)
            };

            Label lblIadeAdet = new Label
            {
                Text = "İade Adedi:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                Location = new Point(25, 20),
                AutoSize = true
            };

            if (kalanIadeHakki <= 0)
            {
                MessageBox.Show("Bu satış için iade hakkı kalmamıştır.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            NumericUpDown numIadeAdet = new NumericUpDown
            {
                Size = new Size(120, 40),
                Location = new Point(280, 15),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Value = 1,
                Minimum = 1,
                Maximum = kalanIadeHakki
            };

            Button btnIadeYap = new Button
            {
                Text = "İade Et",
                Size = new Size(450, 45),
                Location = new Point(25, 365),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 183, 195),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIadeYap.FlatAppearance.BorderSize = 0;
            btnIadeYap.Click += (s, e) => IadeYap((int)numIadeAdet.Value);

            pnlIadeAdet.Controls.AddRange(new Control[] { lblIadeAdet, numIadeAdet });

            pnlAna.Controls.AddRange(new Control[] {
                pnlBaslik,
                pnlUrunBilgi,
                pnlIadeAdet,
                btnIadeYap
            });

            this.Controls.Add(pnlAna);

            // Form taşıma için mouse events
            pnlBaslik.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { this.Capture = false; Message m = Message.Create(this.Handle, 0xa1, new IntPtr(2), IntPtr.Zero); this.WndProc(ref m); } };
        }

        private void IadeYap(int iadeAdet)
        {
            try
            {
                if (iadeAdet > kalanIadeHakki)
                {
                    MessageBox.Show($"En fazla {kalanIadeHakki} adet ürün iade edebilirsiniz!", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    // İade kaydını ekle
                    string iadeKayit = @"
                        INSERT INTO Iadeler (SatisId, UrunId, Miktar, BirimFiyat, ToplamTutar, IadeTarihi)
                        VALUES (@SatisId, @UrunId, @Miktar, @BirimFiyat, @ToplamTutar, datetime('now', 'localtime'))";

                    decimal toplamIadeTutari = birimFiyat * iadeAdet;

                    using (var cmd = new SQLiteCommand(iadeKayit, connection))
                    {
                        cmd.Transaction = transaction;
                        cmd.Parameters.AddWithValue("@SatisId", satisId);
                        cmd.Parameters.AddWithValue("@UrunId", urunId);
                        cmd.Parameters.AddWithValue("@Miktar", iadeAdet);
                        cmd.Parameters.AddWithValue("@BirimFiyat", birimFiyat);
                        cmd.Parameters.AddWithValue("@ToplamTutar", toplamIadeTutari);
                        cmd.ExecuteNonQuery();
                    }

                    // Ürün stoğunu güncelle
                    string stokGuncelle = @"
                        UPDATE Urunler 
                        SET Miktar = Miktar + @IadeAdet 
                        WHERE Id = @UrunId";

                    using (var cmd = new SQLiteCommand(stokGuncelle, connection))
                    {
                        cmd.Transaction = transaction;
                        cmd.Parameters.AddWithValue("@IadeAdet", iadeAdet);
                        cmd.Parameters.AddWithValue("@UrunId", urunId);
                        cmd.ExecuteNonQuery();
                    }
                });

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İade işlemi sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 