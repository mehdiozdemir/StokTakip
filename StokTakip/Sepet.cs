using System;
using System.Collections.Generic;
using System.Linq;

namespace StokTakip
{
    public class Sepet
    {
        public List<SepetUrunu> Urunler { get; private set; } = new List<SepetUrunu>();
        public decimal ToplamTutar => Urunler.Sum(u => u.ToplamFiyat);

        public void UrunEkle(int urunId, string urunAdi, int adet, decimal birimFiyat)
        {
            var urun = Urunler.FirstOrDefault(u => u.UrunId == urunId);
            if (urun != null)
            {
                urun.Adet += adet;
            }
            else
            {
                Urunler.Add(new SepetUrunu(urunId, urunAdi, adet, birimFiyat));
            }
        }

        public void Temizle()
        {
            Urunler.Clear();
        }
    }

    public class SepetUrunu
    {
        public int UrunId { get; set; }
        public string UrunAdi { get; set; }
        public int Adet { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamFiyat => Adet * BirimFiyat;

        public SepetUrunu(int urunId, string urunAdi, int adet, decimal birimFiyat)
        {
            UrunId = urunId;
            UrunAdi = urunAdi;
            Adet = adet;
            BirimFiyat = birimFiyat;
        }
    }
}
