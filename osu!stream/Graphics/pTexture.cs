#if iOS || ANDROID
using OpenTK.Graphics.ES11;
#if ANDROID
using System.Drawing.Imaging;
#endif
#if iOS
using Foundation;
using ObjCRuntime;
using OpenGLES;
#endif

using TextureTarget = OpenTK.Graphics.ES11.All;
using TextureParameterName = OpenTK.Graphics.ES11.All;
using EnableCap = OpenTK.Graphics.ES11.All;
using BlendingFactorSrc = OpenTK.Graphics.ES11.All;
using BlendingFactorDest = OpenTK.Graphics.ES11.All;
using PixelStoreParameter = OpenTK.Graphics.ES11.All;
using VertexPointerType = OpenTK.Graphics.ES11.All;
using ColorPointerType = OpenTK.Graphics.ES11.All;
using ClearBufferMask = OpenTK.Graphics.ES11.All;
using TexCoordPointerType = OpenTK.Graphics.ES11.All;
using BeginMode = OpenTK.Graphics.ES11.All;
using MatrixMode = OpenTK.Graphics.ES11.All;
using PixelInternalFormat = OpenTK.Graphics.ES11.All;
using PixelFormat = OpenTK.Graphics.ES11.All;
using PixelType = OpenTK.Graphics.ES11.All;
using ShaderType = OpenTK.Graphics.ES11.All;
using VertexAttribPointerType = OpenTK.Graphics.ES11.All;
using ProgramParameter = OpenTK.Graphics.ES11.All;
using ShaderParameter = OpenTK.Graphics.ES11.All;
#if iOS
using UIKit;
using CoreGraphics;
#endif
#else
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
#endif
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using osum.AssetManager;
using Android.Util;
using Android.Graphics;

namespace osum.Graphics
{
    public class pTexture : IDisposable, IComparable<pTexture>
    {
        public string assetName;
        public bool fromResourceStore;
        internal int Width;
        internal int Height;
        internal int X;
        internal int Y;

#if DEBUG
        internal int id;
        internal static int staticid = 1;
#endif
        //public SkinSource Source;

        public override string ToString()
        {
            return assetName ?? "unknown texture " + Width + "x" + Height;
        }

        public pTexture(TextureGl textureGl, int width, int height)
        {
            TextureGl = textureGl;
            Width = width;
            Height = height;

#if DEBUG
            id = staticid++;
#endif
        }

        private pTexture()
        {
#if DEBUG
            id = staticid++;
#endif
        }

        internal TextureGl TextureGl;
        internal OsuTexture OsuTextureInfo;

        public bool IsDisposed { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of textures but leaves the GC finalizer in place.
        /// This is used to temporarily unload sprites which are not accessed in a while.
        /// </summary>
        internal void TemporalDispose()
        {
            if (TextureGl != null)
            {
                TextureGl.Dispose();
                TextureGl = null;
            }
        }

        private void Dispose(bool isDisposing)
        {
            if (TextureGl != null)
            {
//                if (fboDepthBuffer >= 0)
//                {
//#if iOS
//                    GL.Oes.DeleteRenderbuffers(1, ref fboDepthBuffer);
//#else
//                    GL.DeleteRenderbuffers(1, ref fboDepthBuffer);
//#endif
//                    fboDepthBuffer = -1;
//                }

                if (fboId >= 0)
                {
#if iOS || ANDROID
                    GL.Oes.DeleteFramebuffers(1,ref fboId);
                    fboId = -1;

#else
                    GL.DeleteFramebuffers(1, ref fboId);
                    fboId = -1;
#endif
                    fboSingleton = -1;
                }


                TextureGl.Dispose();
                TextureGl = null;
            }

            IsDisposed = true;
        }

        /// <summary>
        /// Unloads texture without fully disposing. It may be able to be restored by calling ReloadIfPossible
        /// </summary>
        internal void UnloadTexture()
        {
            if (TextureGl != null)
                TextureGl.Dispose();
        }

        internal bool ReloadIfPossible()
        {
            if (TextureGl == null || TextureGl.Id == -1)
            {
                if (assetName != null || OsuTextureInfo != OsuTexture.None)
                {
                    pTexture reloadedTexture = OsuTextureInfo != OsuTexture.None ? TextureManager.Load(OsuTextureInfo) : FromFile(assetName);
                    if (TextureGl == null)
                        TextureGl = reloadedTexture.TextureGl;
                    else
                        TextureGl.Id = reloadedTexture.TextureGl.Id;

                    reloadedTexture.TextureGl = null; //deassociate with temporary pTexture to avoid disposal.

                    return true;
                }
            }

            return false;
        }

        #endregion

        public void SetData(byte[] data)
        {
            SetData(data, 0, 0);
        }

        public void SetData(byte[] data, int level, PixelFormat format)
        {
            if (TextureGl != null)
            {
                if ((int)format != 0)
                    TextureGl.SetData(data, level, format);
                else
                    TextureGl.SetData(data, level);
            }
        }

        public static pTexture FromFile(string filename)
        {
            return FromFile(filename, false);
        }

        /// <summary>
        /// Read a pTexture from an arbritrary file.
        /// </summary>
        public static pTexture FromFile(string filename, bool mipmap)
        {
            //load base texture first...
            if (!NativeAssetManager.Instance.FileExists(filename))
            {
                System.Diagnostics.Debug.WriteLine("BOOO! WE FAILED TO LOAD");
                return null;
            }

            pTexture tex = null;

            try
            {
#if BITMAP_CACHING
                string bitmapFilename = GameBase.Instance.PathConfig + Path.GetFileName(filename.Replace(".png",".raw"));
                string infoFilename = GameBase.Instance.PathConfig + Path.GetFileName(filename.Replace(".png", ".info"));

                if (!NativeAssetManager.Instance.FileExists(bitmapFilename))
                {
#if iOS
                    using (UIImage image = UIImage.FromFile(filename))
                    {
                        if (image == null)
                            return null;

                        int width = (int)image.Size.Width;
                        int height = (int)image.Size.Height;

                        byte[] buffer = new byte[width * height * 4];
                        fixed (byte* p = buffer)
                        {
                            IntPtr data = (IntPtr)p;

                            using (CGBitmapContext textureContext = new CGBitmapContext(data,
                                        width, height, 8, width * 4,
                                        image.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                            {
                                textureContext.DrawImage(new RectangleF (0, 0, width, height), image.CGImage);
                            }

                            File.WriteAllBytes(bitmapFilename, buffer);

                            tex = FromRawBytes(data, width, height);
                        }
                    }
#else
                    using (Stream stream = NativeAssetManager.Instance.GetFileStream(filename))
                    using (Bitmap b = (Bitmap)Image.FromStream(stream, false, false))
                    {
                        BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly,
                                                     System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        byte[] bitmap = new byte[b.Width * b.Height * 4];
                        Marshal.Copy(data.Scan0, bitmap, 0, bitmap.Length);
                        File.WriteAllBytes(bitmapFilename, bitmap);

                        tex = FromRawBytes(data.Scan0, b.Width, b.Height);
                        b.UnlockBits(data);
                    }
#endif

                    if (tex != null)
                    {
                        string info = tex.Width + "x" + tex.Height;
                        File.WriteAllText(infoFilename, info);
                    }
                }
                else
                {
                    byte[] buffer = File.ReadAllBytes(bitmapFilename);
                    string info = File.ReadAllText(infoFilename);
                    string[] split = info.Split('x');

                    int width = Int32.Parse(split[0]);
                    int height = Int32.Parse(split[1]);

                    fixed (byte* p = buffer)
                    {
                        IntPtr location = (IntPtr)p;
                        tex = FromRawBytes(location, width, height);
                    }
                }
#else
#if iOS
                using (UIImage image = UIImage.FromFile(filename))
                    tex = FromUIImage(image,filename);
#else
                using (Stream stream = NativeAssetManager.Instance.GetFileStream(filename))
                    tex = FromStream(stream, filename);
#endif
#endif

                if (tex == null)
                {
                    System.Diagnostics.Debug.WriteLine("BOOO! WE FAILED TO LOAD 2");
                    return null;
                }

                //This makes sure we are always at the correct sprite resolution.
                //Fucking hack, or fucking hax?
                tex.assetName = filename;
                tex.Width = (int)(tex.Width * 960f / GameBase.SpriteSheetResolution);
                tex.Height = (int)(tex.Height * 960f / GameBase.SpriteSheetResolution);
                tex.TextureGl.TextureWidth = tex.Width;
                tex.TextureGl.TextureHeight = tex.Height;

                return tex;
            }
            catch
            {
                // ignored
            }

            return null;
        }

#if iOS
        public unsafe static pTexture FromUIImage(UIImage image, string assetname, bool requireClear = false)
        {
            if (image == null)
                return null;

            int width = (int)image.Size.Width;
            int height = (int)image.Size.Height;

            IntPtr data = Marshal.AllocHGlobal(width * height * 4);
            byte* bytes = (byte*)data;
            for (int i = width * height * 4 - 1; i >= 0; i--) bytes[i] = 0;

            using (CGBitmapContext textureContext = new CGBitmapContext(data,
                        width, height, 8, width * 4, image.CGImage.ColorSpace, CGImageAlphaInfo.PremultipliedLast))
                textureContext.DrawImage(new RectangleF (0, 0, width, height), image.CGImage);

            pTexture tex = FromRawBytes(data, width, height);

            Marshal.FreeHGlobal(data);

            tex.assetName = assetname;
            return tex;
        }
#endif

        public static pTexture FromStream(Stream stream, string assetname)
        {
            return FromStream(stream, assetname, false);
        }

        public static byte[] ReadAllBytes(BinaryReader reader)
        {
            const int bufferSize = 4096*8;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Read a pTexture from an arbritrary file.
        /// </summary>
        public static pTexture FromStream(Stream stream, string assetname, bool saveToFile)
        {
            try
            {
                pTexture pt;
#if iOS
                pt = FromUIImage(UIImage.LoadFromData(NSData.FromStream(stream)),assetname);
#else

                BinaryReader binreader = new BinaryReader(stream);
                var allData = ReadAllBytes(binreader);
                Android.Graphics.Bitmap bitmap = BitmapFactory.DecodeByteArray(allData, 0, allData.Length);

                pt = FromRawBytes(bitmap.LockPixels(), bitmap.Width, bitmap.Height);
                pt.assetName = assetname;
                bitmap.UnlockPixels();

                /*System.Diagnostics.Debug.WriteLine("PART-0");
                using (System.Drawing.Bitmap b = new System.Drawing.Bitmap(stream))
                {
                    System.Diagnostics.Debug.WriteLine("PART0");
                    BitmapData data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    System.Diagnostics.Debug.WriteLine("PART1");

                    if (saveToFile)
                    {
                        byte[] bitmap = new byte[b.Width * b.Height * 4];
                        Marshal.Copy(data.Scan0, bitmap, 0, bitmap.Length);
                        File.WriteAllBytes(assetname, bitmap);
                    }

                    System.Diagnostics.Debug.WriteLine("PART2");

                    pt = FromRawBytes(data.Scan0, b.Width, b.Height);
                    System.Diagnostics.Debug.WriteLine("PART3");
                    pt.assetName = assetname;
                    System.Diagnostics.Debug.WriteLine("PART4");
                    b.UnlockBits(data);

                    System.Diagnostics.Debug.WriteLine("PART5");
                }*/
#endif
                return pt;
            }
            catch (Exception e)
            {
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                return null;
            }
        }

        public static pTexture FromBytes(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
                return FromStream(ms, "");
        }

        public static pTexture FromBytesSaveRawToFile(byte[] data, string filename)
        {
            using (MemoryStream ms = new MemoryStream(data))
                return FromStream(ms, filename, true);
        }

        public static pTexture FromRawBytes(IntPtr location, int width, int height)
        {
            pTexture pt = new pTexture();

            pt.Width = width;
            pt.Height = height;

            try
            {
                pt.TextureGl = new TextureGl(pt.Width, pt.Height);
                pt.TextureGl.SetData(location, 0, 0);
            }
            catch
            {
                // ignored
            }

            return pt;
        }

        public static pTexture FromRawBytes(byte[] bitmap, int width, int height)
        {
            pTexture pt = new pTexture();
            pt.Width = width;
            pt.Height = height;

            try
            {
                pt.TextureGl = new TextureGl(pt.Width, pt.Height);
                pt.SetData(bitmap);
            }
            catch
            {
                // ignored
            }

            return pt;
        }

        private static int fboSingleton = -1;

        internal int fboId = -1;
        //internal int fboDepthBuffer = -1;

        internal int BindFramebuffer()
        {
            if (fboSingleton >= 0)
                fboId = fboSingleton;

#if iOS || ANDROID
            System.Diagnostics.Debug.WriteLine("VERB432 " + GL.GetError().ToString());
            int oldFBO = 0;

            GL.GetInteger(All.FramebufferBindingOes, out oldFBO);

            if (fboId < 0)
                GL.Oes.GenFramebuffers(1, out fboId);

            GL.Oes.BindFramebuffer(All.FramebufferOes, fboId);
            GL.Oes.FramebufferTexture2D(All.FramebufferOes, All.ColorAttachment0Oes, All.Texture2D, TextureGl.Id, 0);
            GL.Oes.BindFramebuffer(All.FramebufferOes, oldFBO);
            System.Diagnostics.Debug.WriteLine("VERB2324" + GL.GetError().ToString());
#else
            try
            {
                if (fboId < 0)
                    GL.GenFramebuffers(1, out fboId);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureGl.SURFACE_TYPE, TextureGl.Id, 0);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
            catch
            {
                return fboId;
            }
#endif

            fboSingleton = fboId;
            return fboId;
        }

        internal pTexture Clone()
        {
            return (pTexture)MemberwiseClone();
        }

        #region IComparable<pTexture> Members

        public int CompareTo(pTexture other)
        {
            return String.Compare(assetName, other.assetName, StringComparison.Ordinal);
        }

        #endregion
    }
}