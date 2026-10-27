using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DshPet
{
    /// <summary>分层窗口的绘制表面：托管 Bitmap -> DIB Section -> UpdateLayeredWindow（逐像素 alpha）。</summary>
    public class LayeredSurface : IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        static extern void CopyMemory(IntPtr dest, IntPtr src, UIntPtr count);

        Bitmap _bmp;
        IntPtr _hdcMem = IntPtr.Zero;
        IntPtr _hDib = IntPtr.Zero;
        IntPtr _hOld = IntPtr.Zero;
        IntPtr _bits = IntPtr.Zero;
        int _stride;
        public bool LastOk;
        public int LastError;

        public Bitmap Bitmap { get { return _bmp; } }
        public int Width { get { return _bmp == null ? 0 : _bmp.Width; } }
        public int Height { get { return _bmp == null ? 0 : _bmp.Height; } }

        public void EnsureSize(int w, int h)
        {
            if (w <= 0 || h <= 0) return;
            if (_bmp != null && _bmp.Width == w && _bmp.Height == h) return;
            ReleaseGdi();
            if (_bmp != null) { _bmp.Dispose(); _bmp = null; }

            _bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            _stride = w * 4;

            IntPtr screen = Native.GetDC(IntPtr.Zero);
            _hdcMem = Native.CreateCompatibleDC(screen);
            Native.ReleaseDC(IntPtr.Zero, screen);

            Native.BITMAPINFO bmi = new Native.BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = w;
            bmi.bmiHeader.biHeight = -h; // 负值 = 自上而下
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB
            bmi.bmiHeader.biSizeImage = (uint)(_stride * h);

            _hDib = Native.CreateDIBSection(_hdcMem, ref bmi, 0, out _bits, IntPtr.Zero, 0);
            _hOld = Native.SelectObject(_hdcMem, _hDib);
        }

        public bool Present(IntPtr hwnd, byte constantAlpha)
        {
            if (_bmp == null || _hdcMem == IntPtr.Zero) return false;

            BitmapData data = _bmp.LockBits(new Rectangle(0, 0, _bmp.Width, _bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                if (data.Stride == _stride)
                {
                    CopyMemory(_bits, data.Scan0, (UIntPtr)(uint)(_stride * _bmp.Height));
                }
                else
                {
                    for (int y = 0; y < _bmp.Height; y++)
                    {
                        IntPtr srcRow = new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride);
                        IntPtr dstRow = new IntPtr(_bits.ToInt64() + (long)y * _stride);
                        CopyMemory(dstRow, srcRow, (UIntPtr)(uint)_stride);
                    }
                }
            }
            finally
            {
                _bmp.UnlockBits(data);
            }

            Native.SIZE size = new Native.SIZE(_bmp.Width, _bmp.Height);
            Native.POINT srcPt = new Native.POINT(0, 0);
            Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
            blend.BlendOp = Native.AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = constantAlpha;
            blend.AlphaFormat = Native.AC_SRC_ALPHA;

            bool ok = Native.UpdateLayeredWindow(hwnd, IntPtr.Zero, IntPtr.Zero, ref size, _hdcMem, ref srcPt, 0, ref blend, Native.ULW_ALPHA);
            LastError = Marshal.GetLastWin32Error();
            LastOk = ok;
            return ok;
        }

        /// <summary>取某点的不透明度（用于鼠标穿透判定）。</summary>
        public int AlphaAt(int x, int y)
        {
            if (_bmp == null || x < 0 || y < 0 || x >= _bmp.Width || y >= _bmp.Height) return 0;
            try { return _bmp.GetPixel(x, y).A; }
            catch { return 0; }
        }

        void ReleaseGdi()
        {
            if (_hdcMem != IntPtr.Zero && _hOld != IntPtr.Zero)
            {
                Native.SelectObject(_hdcMem, _hOld);
                _hOld = IntPtr.Zero;
            }
            if (_hDib != IntPtr.Zero) { Native.DeleteObject(_hDib); _hDib = IntPtr.Zero; _bits = IntPtr.Zero; }
            if (_hdcMem != IntPtr.Zero) { Native.DeleteDC(_hdcMem); _hdcMem = IntPtr.Zero; }
        }

        public void Dispose()
        {
            ReleaseGdi();
            if (_bmp != null) { _bmp.Dispose(); _bmp = null; }
        }
    }
}
