using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;

namespace StokTakip
{
    public partial class BorcTakipForm : Form
    {
        private DataGridView dgvBorclar;
        private TextBox txtMusteriAdi;
        private NumericUpDown nudBorcMiktari;
        private TextBox txtAciklama;
        private ComboBox cmbDurum;
        private NumericUpDown nudOdenenMiktar;
        private Button btnEkle;
        private Button btnGuncelle;
        private Button btnSil;
        private DateTimePicker dtpOdemeTarihi;

        public BorcTakipForm()
        {
            InitializeComponent();
            BorclariListele();
        }

        private void InitializeComponent()
        {
            this.Text = "Borç Takip";
            this.Size = new Size(1000, 600);
            this.BackColor = Color.FromArgb(18, 18, 18);
            this.ForeColor = Color.White;
            this.Padding = new Padding(10);

            TableLayoutPanel pnlUst = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = Color.FromArgb(24, 24, 24),
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 4,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            pnlUst.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlUst.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlUst.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // Label stilleri
            var labelStyle = new Action<Label>((label) => {
                label.Dock = DockStyle.Fill;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.ForeColor = Color.White;
                label.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            });

            // Kontrol stilleri
            var controlStyle = new Action<Control>((control) => {
                control.Dock = DockStyle.Fill;
                control.BackColor = Color.FromArgb(30, 30, 30);
                control.ForeColor = Color.White;
                control.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                
                if (control is TextBox txt)
                {
                    txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is NumericUpDown nud)
                {
                    nud.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ComboBox cmb)
                {
                    cmb.FlatStyle = FlatStyle.Flat;
                }
                else if (control is DateTimePicker dtp)
                {
                    dtp.Format = DateTimePickerFormat.Short;
                }
            });

            // Müşteri Adı
            var lblMusteriAdi = new Label { Text = "Müşteri Adı:" };
            labelStyle(lblMusteriAdi);
            txtMusteriAdi = new TextBox();
            controlStyle(txtMusteriAdi);

            // Borç Miktarı
            var lblBorcMiktari = new Label { Text = "Borç Miktarı:" };
            labelStyle(lblBorcMiktari);
            nudBorcMiktari = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Increment = 0.5M
            };
            controlStyle(nudBorcMiktari);

            // Açıklama
            var lblAciklama = new Label { Text = "Açıklama:" };
            labelStyle(lblAciklama);
            txtAciklama = new TextBox();
            controlStyle(txtAciklama);

            // Ödeme Tarihi
            var lblOdemeTarihi = new Label { Text = "Tarih:" };
            labelStyle(lblOdemeTarihi);
            dtpOdemeTarihi = new DateTimePicker();
            controlStyle(dtpOdemeTarihi);

            // Durum
            var lblDurum = new Label { Text = "Durum:" };
            labelStyle(lblDurum);
            cmbDurum = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDurum.Items.AddRange(new object[] { "Ödenmedi", "Kısmi Ödeme", "Ödendi" });
            cmbDurum.SelectedIndex = 0;
            controlStyle(cmbDurum);

            // Ödenen Miktar
            var lblOdenenMiktar = new Label { Text = "Ödenen Miktar:" };
            labelStyle(lblOdenenMiktar);
            nudOdenenMiktar = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Increment = 0.5M
            };
            controlStyle(nudOdenenMiktar);

            // Butonlar için panel
            var pnlButonlar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlButonlar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            // Buton stilleri
            var buttonStyle = new Action<Button, Color>((btn, color) => {
                btn.Dock = DockStyle.Fill;
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = color;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                btn.Margin = new Padding(5);
                btn.Cursor = Cursors.Hand;
                btn.FlatAppearance.BorderSize = 0;
            });

            btnEkle = new Button { Text = "EKLE" };
            buttonStyle(btnEkle, Color.FromArgb(0, 150, 0));
            btnEkle.Click += BtnEkle_Click;

            btnGuncelle = new Button { Text = "GÜNCELLE" };
            buttonStyle(btnGuncelle, Color.FromArgb(0, 0, 150));
            btnGuncelle.Click += BtnGuncelle_Click;

            btnSil = new Button { Text = "SİL" };
            buttonStyle(btnSil, Color.FromArgb(150, 0, 0));
            btnSil.Click += BtnSil_Click;

            // Kontrolleri panele ekle
            pnlUst.Controls.Add(lblMusteriAdi, 0, 0);
            pnlUst.Controls.Add(txtMusteriAdi, 0, 1);
            pnlUst.Controls.Add(lblBorcMiktari, 1, 0);
            pnlUst.Controls.Add(nudBorcMiktari, 1, 1);
            pnlUst.Controls.Add(lblAciklama, 2, 0);
            pnlUst.Controls.Add(txtAciklama, 2, 1);
            pnlUst.Controls.Add(lblOdemeTarihi, 0, 2);
            pnlUst.Controls.Add(dtpOdemeTarihi, 0, 3);
            pnlUst.Controls.Add(lblDurum, 1, 2);
            pnlUst.Controls.Add(cmbDurum, 1, 3);
            pnlUst.Controls.Add(lblOdenenMiktar, 2, 2);
            pnlUst.Controls.Add(nudOdenenMiktar, 2, 3);

            pnlButonlar.Controls.Add(btnEkle, 0, 0);
            pnlButonlar.Controls.Add(btnGuncelle, 1, 0);
            pnlButonlar.Controls.Add(btnSil, 2, 0);

            var pnlButonlarContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = Color.FromArgb(24, 24, 24)
            };
            pnlButonlarContainer.Controls.Add(pnlButonlar);

            // DataGridView
            dgvBorclar = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(40, 40, 40),
                EnableHeadersVisualStyles = false
            };

            // DataGridView stil ayarları
            dgvBorclar.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            dgvBorclar.DefaultCellStyle.ForeColor = Color.White;
            dgvBorclar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
            dgvBorclar.DefaultCellStyle.SelectionForeColor = Color.White;
            
            dgvBorclar.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvBorclar.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBorclar.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 35, 35);
            dgvBorclar.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvBorclar.ColumnHeadersHeight = 40;
            dgvBorclar.RowTemplate.Height = 35;

            dgvBorclar.CellClick += DgvBorclar_CellClick;

            // Form'a kontrolleri ekle
            this.Controls.Add(dgvBorclar);
            this.Controls.Add(pnlButonlarContainer);
            this.Controls.Add(pnlUst);
        }

        private void BorclariListele()
        {
            try
            {
                var dt = DatabaseManager.ExecuteQuery(@"
                    SELECT 
                        BorcId, 
                        MusteriAdi, 
                        BorcMiktari, 
                        Aciklama, 
                        BorcTarihi,
                        OdemeTarihi,
                        Durumu, 
                        OdenenMiktar,
                        ROUND(BorcMiktari - OdenenMiktar, 2) as KalanBorc
                    FROM Borclar
                    ORDER BY BorcTarihi DESC");

                dgvBorclar.DataSource = dt;
                
                if (dgvBorclar.Columns["BorcId"] != null)
                    dgvBorclar.Columns["BorcId"].Visible = false;

                // Kolon başlıklarını düzenle
                if (dgvBorclar.Columns["MusteriAdi"] != null)
                    dgvBorclar.Columns["MusteriAdi"].HeaderText = "Müşteri Adı";
                if (dgvBorclar.Columns["BorcMiktari"] != null)
                    dgvBorclar.Columns["BorcMiktari"].HeaderText = "Borç Miktarı";
                if (dgvBorclar.Columns["Aciklama"] != null)
                    dgvBorclar.Columns["Aciklama"].HeaderText = "Açıklama";
                if (dgvBorclar.Columns["BorcTarihi"] != null)
                    dgvBorclar.Columns["BorcTarihi"].HeaderText = "Borç Tarihi";
                if (dgvBorclar.Columns["OdemeTarihi"] != null)
                    dgvBorclar.Columns["OdemeTarihi"].HeaderText = "Ödeme Tarihi";
                if (dgvBorclar.Columns["Durumu"] != null)
                    dgvBorclar.Columns["Durumu"].HeaderText = "Durum";
                if (dgvBorclar.Columns["OdenenMiktar"] != null)
                    dgvBorclar.Columns["OdenenMiktar"].HeaderText = "Ödenen Miktar";
                if (dgvBorclar.Columns["KalanBorc"] != null)
                    dgvBorclar.Columns["KalanBorc"].HeaderText = "Kalan Borç";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borçlar listelenirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEkle_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtMusteriAdi.Text))
                {
                    MessageBox.Show("Lütfen müşteri adını giriniz.", "Uyarı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    using var cmd = new SQLiteCommand(@"
                        INSERT INTO Borclar 
                            (MusteriAdi, BorcMiktari, Aciklama, OdemeTarihi, Durumu, OdenenMiktar)
                        VALUES 
                            (@musteriAdi, @borcMiktari, @aciklama, @odemeTarihi, @durumu, @odenenMiktar)", 
                        connection, transaction);

                    cmd.Parameters.AddWithValue("@musteriAdi", txtMusteriAdi.Text);
                    cmd.Parameters.AddWithValue("@borcMiktari", nudBorcMiktari.Value);
                    cmd.Parameters.AddWithValue("@aciklama", txtAciklama.Text);
                    cmd.Parameters.AddWithValue("@odemeTarihi", dtpOdemeTarihi.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@durumu", cmbDurum.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@odenenMiktar", nudOdenenMiktar.Value);

                    cmd.ExecuteNonQuery();
                });

                FormuTemizle();
                BorclariListele();
                MessageBox.Show("Borç kaydı başarıyla eklendi.", "Bilgi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borç eklenirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            if (dgvBorclar.SelectedRows.Count == 0) return;

            try
            {
                var borcId = Convert.ToInt32(dgvBorclar.SelectedRows[0].Cells["BorcID"].Value);

                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    using var cmd = new SQLiteCommand(@"
                        UPDATE Borclar 
                        SET 
                            MusteriAdi = @musteriAdi, 
                            BorcMiktari = @borcMiktari, 
                            Aciklama = @aciklama,
                            OdemeTarihi = @odemeTarihi,
                            Durumu = @durumu,
                            OdenenMiktar = @odenenMiktar
                        WHERE BorcID = @borcId", connection, transaction);

                    cmd.Parameters.AddWithValue("@borcId", borcId);
                    cmd.Parameters.AddWithValue("@musteriAdi", txtMusteriAdi.Text);
                    cmd.Parameters.AddWithValue("@borcMiktari", nudBorcMiktari.Value);
                    cmd.Parameters.AddWithValue("@aciklama", txtAciklama.Text);
                    cmd.Parameters.AddWithValue("@odemeTarihi", dtpOdemeTarihi.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@durumu", cmbDurum.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@odenenMiktar", nudOdenenMiktar.Value);

                    cmd.ExecuteNonQuery();
                });

                FormuTemizle();
                BorclariListele();
                MessageBox.Show("Borç kaydı başarıyla güncellendi.", "Bilgi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borç güncellenirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (dgvBorclar.SelectedRows.Count == 0) return;

            if (MessageBox.Show("Seçili borç kaydını silmek istediğinizden emin misiniz?", "Onay", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var borcId = Convert.ToInt32(dgvBorclar.SelectedRows[0].Cells["BorcID"].Value);

                DatabaseManager.ExecuteTransaction((connection, transaction) =>
                {
                    using var cmd = new SQLiteCommand("DELETE FROM Borclar WHERE BorcID = @borcId", 
                        connection, transaction);
                    cmd.Parameters.AddWithValue("@borcId", borcId);
                    cmd.ExecuteNonQuery();
                });

                FormuTemizle();
                BorclariListele();
                MessageBox.Show("Borç kaydı başarıyla silindi.", "Bilgi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Borç silinirken hata oluştu: {ex.Message}", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvBorclar_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvBorclar.Rows[e.RowIndex];
            txtMusteriAdi.Text = row.Cells["MusteriAdi"].Value.ToString();
            nudBorcMiktari.Value = Convert.ToDecimal(row.Cells["BorcMiktari"].Value);
            txtAciklama.Text = row.Cells["Aciklama"].Value?.ToString();
            dtpOdemeTarihi.Value = Convert.ToDateTime(row.Cells["OdemeTarihi"].Value);
            cmbDurum.SelectedItem = row.Cells["Durumu"].Value.ToString();
            nudOdenenMiktar.Value = Convert.ToDecimal(row.Cells["OdenenMiktar"].Value);
        }

        private void FormuTemizle()
        {
            txtMusteriAdi.Clear();
            nudBorcMiktari.Value = 0;
            txtAciklama.Clear();
            dtpOdemeTarihi.Value = DateTime.Now;
            cmbDurum.SelectedIndex = 0;
            nudOdenenMiktar.Value = 0;
        }
    }
} 