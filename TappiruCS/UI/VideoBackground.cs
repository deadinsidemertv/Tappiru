using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using TappiruCS.Core.GameObject;

namespace TappiruCS.UI
{
    public class VideoBackground : GameObject
    {
        private LibVLC _libVLC = null!;
        private MediaPlayer _mediaPlayer = null!;
        private Media _media = null!;

        private int _videoTextureId = 0;
        private bool _textureCreated = false;

        private Bitmap? _frontBitmap;     // для рендера
        private Bitmap? _backBitmap;      // для записи из VLC
        private readonly object _lock = new object();

        public string VideoPath { get; private set; } = string.Empty;
        public float Opacity { get; set; } = 1f;

        private bool _initialized = false;
        private int _videoWidth = 0;
        private int _videoHeight = 0;
        private bool _hasFrame = false;

        public VideoBackground(string videoPath)
        {
            VideoPath = videoPath ?? string.Empty;
            Active = !string.IsNullOrEmpty(VideoPath) && File.Exists(VideoPath);
        }

        public void LoadVideo()
        {
            if (_initialized || !Active) return;

            try
            {
                _libVLC = new LibVLC("--quiet", "--no-audio", "--no-video-title-show", "--no-xlib", "--avcodec-hw=none");

                _mediaPlayer = new MediaPlayer(_libVLC);
                _media = new Media(_libVLC, VideoPath, FromType.FromPath);

                // === Новый подход ===
                _mediaPlayer.SetVideoFormatCallbacks(VideoFormatCallback, null);
                _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);

                _mediaPlayer.Media = _media;
                _mediaPlayer.Play();

                Console.WriteLine($"[VideoBackground] ✅ Запущено с SetVideoFormatCallbacks: {Path.GetFileName(VideoPath)}");
                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VideoBackground] Ошибка: {ex.Message}");
                Active = false;
            }
        }

        // Вызывается VLC, чтобы узнать формат
        private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
        {
            // Просим RGBA
            unsafe { *(uint*)chroma = BitConverter.ToUInt32("RV32"u8); }   // RV32 = RGBA

            pitches = width * 4;
            lines = height;

            _videoWidth = (int)width;
            _videoHeight = (int)height;

            lock (_lock)
            {
                _backBitmap?.Dispose();
                _frontBitmap?.Dispose();

                _backBitmap = new Bitmap(_videoWidth, _videoHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                _frontBitmap = new Bitmap(_videoWidth, _videoHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                Console.WriteLine($"[VideoBackground] Формат получен: {_videoWidth}×{_videoHeight}");
            }

            return 1; // успех
        }

        private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
        {
            lock (_lock)
            {
                if (_backBitmap == null) return IntPtr.Zero;

                var data = _backBitmap.LockBits(new Rectangle(0, 0, _videoWidth, _videoHeight),
                    ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                unsafe { *(IntPtr*)planes = data.Scan0; }
                return data.Scan0;
            }
        }

        private void DisplayCallback(IntPtr opaque, IntPtr picture)
        {
            lock (_lock)
            {
                if (_backBitmap != null)
                {
                    try { _backBitmap.UnlockBits(new BitmapData()); } catch { }
                }

                // Меняем буферы
                (_frontBitmap, _backBitmap) = (_backBitmap, _frontBitmap);
                _hasFrame = true;
            }
        }

        public override void Draw(Matrix4 projection)
        {
            if (!Active || SB == null)
            {
                SB?.DrawRect(0, 0, Game.ClientSize.X, Game.ClientSize.Y,
                    new Color4(0.02f, 0.02f, 0.06f, Opacity), projection);
                return;
            }

            UpdateVideoTexture();

            if (_videoTextureId != 0 && _hasFrame && _frontBitmap != null)
            {
                SB.Draw(_videoTextureId, 0, 0, Game.ClientSize.X, Game.ClientSize.Y,
                        0f, 0f, 1f, 1f, 1f, 1f, 1f, Opacity, projection);
            }
            else
            {
                SB.DrawRect(0, 0, Game.ClientSize.X, Game.ClientSize.Y,
                    new Color4(0.02f, 0.02f, 0.06f, Opacity), projection);
            }
        }

        private void UpdateVideoTexture()
        {
            if (_frontBitmap == null) return;

            lock (_lock)
            {
                if (!_textureCreated)
                {
                    _videoTextureId = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, _videoTextureId);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                    _textureCreated = true;
                }

                GL.BindTexture(TextureTarget.Texture2D, _videoTextureId);

                var data = _frontBitmap.LockBits(new Rectangle(0, 0, _videoWidth, _videoHeight),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              _videoWidth, _videoHeight, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                              PixelType.UnsignedByte, data.Scan0);

                _frontBitmap.UnlockBits(data);
            }
        }

        public void DisposeVideo()
        {
            _mediaPlayer?.Stop();
            _media?.Dispose();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            lock (_lock)
            {
                _frontBitmap?.Dispose();
                _backBitmap?.Dispose();
            }

            if (_videoTextureId != 0)
                GL.DeleteTexture(_videoTextureId);
        }
    }
}