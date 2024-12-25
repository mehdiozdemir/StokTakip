using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace StokTakip
{
    public static class DatabaseManager
    {
        private static readonly string connectionString;
        private static readonly object lockObject = new object();

        static DatabaseManager()
        {
            string dbPath = Path.Combine(Application.StartupPath, "StokTakip.db");
            connectionString = $"Data Source={dbPath};Version=3;";

            bool yeniVeritabani = !File.Exists(dbPath);
            if (yeniVeritabani)
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            InitializeDatabase();
        }

        public static string ConnectionString => connectionString;

        private static bool InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        // Kategoriler tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Kategoriler (
                                KategoriId INTEGER PRIMARY KEY AUTOINCREMENT,
                                KategoriAdi TEXT NOT NULL
                            )";
                        command.ExecuteNonQuery();

                        // Urunler tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Urunler (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UrunAdi TEXT NOT NULL,
                                KategoriId INTEGER,
                                Aciklama TEXT,
                                Miktar INTEGER DEFAULT 0,
                                Birim TEXT,
                                Tedarikci TEXT,
                                MinStok INTEGER DEFAULT 0,
                                KritikStok INTEGER DEFAULT 0,
                                RafKodu TEXT,
                                AlisFiyati DECIMAL(10,2) DEFAULT 0,
                                SatisFiyati DECIMAL(10,2) DEFAULT 0,
                                Renk TEXT,
                                FOREIGN KEY(KategoriId) REFERENCES Kategoriler(KategoriId)
                            )";
                        command.ExecuteNonQuery();

                        // Borclar tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Borclar (
                                BorcId INTEGER PRIMARY KEY AUTOINCREMENT,
                                MusteriAdi TEXT NOT NULL,
                                BorcMiktari DECIMAL(10,2) NOT NULL,
                                OdenenMiktar DECIMAL(10,2) DEFAULT 0,
                                Aciklama TEXT,
                                BorcTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
                                OdemeTarihi DATETIME,
                                Durumu TEXT DEFAULT 'Ödenmedi'
                            )";
                        command.ExecuteNonQuery();

                        // Satislar tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Satislar (
                                SatisId INTEGER PRIMARY KEY AUTOINCREMENT,
                                MusteriAdi TEXT,
                                ToplamTutar DECIMAL(10,2),
                                OdemeTipi TEXT,
                                SatisTarihi DATETIME DEFAULT CURRENT_TIMESTAMP
                            )";
                        command.ExecuteNonQuery();

                        // SatisDetaylari tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS SatisDetaylari (
                                SatisDetayId INTEGER PRIMARY KEY AUTOINCREMENT,
                                SatisId INTEGER,
                                UrunId INTEGER,
                                Miktar INTEGER,
                                BirimFiyat DECIMAL(10,2),
                                ToplamFiyat DECIMAL(10,2),
                                FOREIGN KEY(SatisId) REFERENCES Satislar(SatisId) ON DELETE CASCADE,
                                FOREIGN KEY(UrunId) REFERENCES Urunler(UrunId) ON DELETE SET NULL
                            )";
                        command.ExecuteNonQuery();

                        // Iadeler tablosu
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Iadeler (
                                IadeId INTEGER PRIMARY KEY AUTOINCREMENT,
                                SatisId INTEGER,
                                UrunId INTEGER,
                                Miktar INTEGER,
                                BirimFiyat DECIMAL(10,2),
                                ToplamTutar DECIMAL(10,2),
                                IadeTarihi DATETIME DEFAULT CURRENT_TIMESTAMP,
                                Aciklama TEXT,
                                FOREIGN KEY(SatisId) REFERENCES Satislar(SatisId),
                                FOREIGN KEY(UrunId) REFERENCES Urunler(UrunId)
                            )";
                        command.ExecuteNonQuery();

                        // OdenenMiktar sütununu kontrol et ve yoksa ekle
                        command.CommandText = @"
                            SELECT COUNT(*) 
                            FROM pragma_table_info('Borclar') 
                            WHERE name='OdenenMiktar'";
                        var result = command.ExecuteScalar();

                        if (Convert.ToInt32(result) == 0)
                        {
                            command.CommandText = "ALTER TABLE Borclar ADD COLUMN OdenenMiktar DECIMAL(10,2) DEFAULT 0";
                            command.ExecuteNonQuery();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı oluşturulurken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void ExecuteTransaction(Action<SQLiteConnection, SQLiteTransaction> action)
        {
            lock (lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            action(connection, transaction);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
        }

        public static T ExecuteTransaction<T>(Func<SQLiteConnection, SQLiteTransaction, T> action)
        {
            lock (lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            T result = action(connection, transaction);
                            transaction.Commit();
                            return result;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
        }

        public static DataTable ExecuteQuery(string sql, SQLiteParameter[] parameters = null)
        {
            return ExecuteTransaction((connection, transaction) =>
            {
                using (var command = new SQLiteCommand(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            });
        }

        public static int ExecuteNonQuery(string sql, SQLiteParameter[] parameters = null)
        {
            return ExecuteTransaction((connection, transaction) =>
            {
                using (var command = new SQLiteCommand(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            });
        }

        public static object ExecuteScalar(string sql, SQLiteParameter[] parameters = null)
        {
            return ExecuteTransaction((connection, transaction) =>
            {
                using (var command = new SQLiteCommand(sql, connection, transaction))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            });
        }

        public static void AddRenkColumn()
        {
            ExecuteTransaction((connection, transaction) =>
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.Transaction = transaction;
                    
                    // Önce kolonun var olup olmadığını kontrol et
                    command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Urunler') WHERE name='Renk'";
                    var result = command.ExecuteScalar();
                    
                    // Kolon yoksa ekle
                    if (Convert.ToInt32(result) == 0)
                    {
                        command.CommandText = "ALTER TABLE Urunler ADD COLUMN Renk TEXT";
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            });
        }
    }
} 