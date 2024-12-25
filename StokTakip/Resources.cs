using System.Drawing;
using System.IO;

namespace StokTakip
{
    public static class DefaultImages
    {
        private static Image _noImage;
        
        public static Image NoImage
        {
            get
            {
                if (_noImage == null)
                {
                    // 100x100 boyutunda gri bir resim oluştur
                    _noImage = new Bitmap(100, 100);
                    using (Graphics g = Graphics.FromImage(_noImage))
                    {
                        g.Clear(Color.FromArgb(45, 45, 45));
                        using (Font font = new Font("Segoe UI", 10))
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            g.DrawString("Resim Yok", font, Brushes.White, new RectangleF(0, 0, 100, 100), sf);
                        }
                    }
                }
                return _noImage;
            }
        }
    }
} 