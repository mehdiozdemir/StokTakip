using System;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;

namespace StokTakip
{
    internal static class Program
    {
        public static string DbPath = Path.Combine(Application.StartupPath, "StokTakip.db");

        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Renk kolonunu ekle
                DatabaseManager.AddRenkColumn();

                // Veritabanı bağlantısını kontrol et ve gerekli tabloları oluştur
                if (!File.Exists(DbPath))
                {
                    SQLiteConnection.CreateFile(DbPath);
                    VeritabaniniOlustur();
                }
                else
                {
                    // Veritabanı var ama tablolar eksik olabilir
                    VeritabaniniKontrolEt();
                }

                // Giriş formunu göster
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        Application.Run(new MainForm());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama başlatılırken bir hata oluştu: {ex.Message}", "Kritik Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void VeritabaniniOlustur()
        {
            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Sifre tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS Sifre (
                                ID INTEGER PRIMARY KEY,
                                Sifre TEXT NOT NULL
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Varsayılan şifreyi ekle
                        using (var cmd = new SQLiteCommand(@"
                            INSERT OR IGNORE INTO Sifre (ID, Sifre) VALUES (1, 'admin')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Kullanıcılar tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS Kullanicilar (
                                KullaniciId INTEGER PRIMARY KEY AUTOINCREMENT,
                                KullaniciAdi TEXT NOT NULL UNIQUE,
                                Sifre TEXT NOT NULL,
                                Ad TEXT,
                                Soyad TEXT,
                                Rol TEXT DEFAULT 'Kullanici',
                                OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Admin kullanıcısı oluştur
                        using (var cmd = new SQLiteCommand(@"
                            INSERT OR IGNORE INTO Kullanicilar (KullaniciAdi, Sifre, Ad, Soyad, Rol)
                            VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Admin', 'User', 'Admin')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Kategoriler tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS Kategoriler (
                                KategoriId INTEGER PRIMARY KEY AUTOINCREMENT,
                                KategoriAdi TEXT NOT NULL UNIQUE,
                                Aciklama TEXT,
                                OlusturmaTarihi DATETIME DEFAULT CURRENT_TIMESTAMP
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Örnek kategoriler ekle
                        using (var cmd = new SQLiteCommand(@"
                            INSERT OR IGNORE INTO Kategoriler (KategoriAdi, Aciklama) VALUES 
                            ('Pantolon', 'Kot ve Kumaş Pantolonlar'),
                            ('Gömlek', 'Erkek Gömlekleri'),
                            ('T-Shirt', 'Yazlık T-Shirtler'),
                            ('Ceket', 'Erkek Ceketleri'),
                            ('Aksesuar', 'Kemer, Cüzdan vb.')", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Ürünler tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS Urunler (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UrunAdi TEXT NOT NULL,
                                KategoriId INTEGER,
                                Aciklama TEXT,
                                Miktar INTEGER NOT NULL DEFAULT 0,
                                Birim TEXT NOT NULL DEFAULT 'Adet',
                                Tedarikci TEXT,
                                MinStok INTEGER DEFAULT 0,
                                KritikStok INTEGER DEFAULT 0,
                                RafKodu TEXT,
                                AlisFiyati DECIMAL(10,2) DEFAULT 0,
                                SatisFiyati DECIMAL(10,2) DEFAULT 0,
                                UrunResmi BLOB,
                                Renk TEXT,
                                FOREIGN KEY(KategoriId) REFERENCES Kategoriler(KategoriId) ON DELETE SET NULL
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Satışlar tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS Satislar (
                                SatisId INTEGER PRIMARY KEY AUTOINCREMENT,
                                MusteriAdi TEXT,
                                ToplamTutar DECIMAL(10,2),
                                OdemeTipi TEXT,
                                SatisTarihi DATETIME DEFAULT CURRENT_TIMESTAMP
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Satış Detayları tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS SatisDetaylari (
                                SatisDetayId INTEGER PRIMARY KEY AUTOINCREMENT,
                                SatisId INTEGER,
                                UrunId INTEGER,
                                Miktar INTEGER,
                                BirimFiyat DECIMAL(10,2),
                                ToplamFiyat DECIMAL(10,2),
                                FOREIGN KEY(SatisId) REFERENCES Satislar(SatisId) ON DELETE CASCADE,
                                FOREIGN KEY(UrunId) REFERENCES Urunler(UrunId) ON DELETE SET NULL
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Borç Takip tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS BorcTakip (
                                BorcId INTEGER PRIMARY KEY AUTOINCREMENT,
                                MusteriAdi TEXT NOT NULL,
                                BorcMiktari DECIMAL(10,2),
                                OdenenMiktar DECIMAL(10,2) DEFAULT 0,
                                KalanBorc DECIMAL(10,2),
                                BorcTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
                                SonOdemeTarihi DATETIME,
                                Durum TEXT DEFAULT 'Aktif'
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // Borç Ödemeleri tablosu
                        using (var cmd = new SQLiteCommand(@"
                            CREATE TABLE IF NOT EXISTS BorcOdemeleri (
                                OdemeId INTEGER PRIMARY KEY AUTOINCREMENT,
                                BorcId INTEGER,
                                OdemeMiktari DECIMAL(10,2),
                                OdemeTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
                                Aciklama TEXT,
                                FOREIGN KEY(BorcId) REFERENCES BorcTakip(BorcId) ON DELETE CASCADE
                            )", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Veritabanı oluşturma hatası: {ex.Message}");
                    }
                }
            }
        }

        private static void VeritabaniniKontrolEt()
        {
            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Tabloların varlığını kontrol et
                        var tablolar = new[] { "Kullanicilar", "Kategoriler", "Urunler", "Satislar", "SatisDetaylari", "BorcTakip", "BorcOdemeleri" };
                        bool tablolarEksik = false;

                        foreach (var tablo in tablolar)
                        {
                            using (var cmd = new SQLiteCommand($"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{tablo}'", conn))
                            {
                                var sonuc = Convert.ToInt32(cmd.ExecuteScalar());
                                if (sonuc == 0)
                                {
                                    tablolarEksik = true;
                                    break;
                                }
                            }
                        }

                        if (tablolarEksik)
                        {
                            // Veritabanını yeniden oluştur
                            conn.Close();
                            File.Delete(DbPath);
                            SQLiteConnection.CreateFile(DbPath);
                            VeritabaniniOlustur();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}