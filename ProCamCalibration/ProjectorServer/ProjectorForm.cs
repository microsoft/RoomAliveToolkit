using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using RoomAliveToolkit;

namespace RoomAliveToolkit
{
    public partial class ProjectorForm : Form
    {
        SharpDX.Direct2D1.Factory factory = new SharpDX.Direct2D1.Factory();
        SharpDX.DirectWrite.Factory directWriteFacrtory = new SharpDX.DirectWrite.Factory();
        RenderTarget renderTarget;
        GrayCode grayCode;
        ARGBImage[] grayCodeImages;
        SharpDX.Direct2D1.Bitmap bitmap;
        int screenIndex;
        System.Drawing.Rectangle bounds;
        SharpDX.DirectWrite.TextFormat textFormat;
        SolidColorBrush solidColorBrush;

        public ProjectorForm(int screenIndex)
        {
            InitializeComponent();
            this.screenIndex = screenIndex;
            ShowInTaskbar = false;

            FormBorderStyle = FormBorderStyle.None;


            // assumes that taskbar is not displayed on every display

            bounds = Screen.AllScreens[screenIndex].Bounds;
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(bounds.X, bounds.Y);
            Size = new Size(bounds.Width, bounds.Height);

            // Gray code 
            grayCode = new GrayCode(bounds.Width, bounds.Height);
            grayCodeImages = grayCode.Generate();


        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
 
            // Direct2D
            var renderTargetProperties = new RenderTargetProperties()
            {
                PixelFormat = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Ignore)
            };
            var hwndRenderTargetProperties = new HwndRenderTargetProperties()
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(bounds.Width, bounds.Height),
                PresentOptions = PresentOptions.Immediately,
            };
            renderTarget = new WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);

            var bitmapProperties = new BitmapProperties()
            {
                PixelFormat = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Ignore)
            };
            bitmap = new SharpDX.Direct2D1.Bitmap(renderTarget, new Size2(bounds.Width, bounds.Height), bitmapProperties);

            textFormat = new TextFormat(directWriteFacrtory, "Arial", FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, 96.0f);
            textFormat.ParagraphAlignment = ParagraphAlignment.Center;
            textFormat.TextAlignment = TextAlignment.Center;

            solidColorBrush = new SolidColorBrush(renderTarget, Color4.White);
        }

        public int NumberOfGrayCodeImages
        {
            get { return grayCodeImages.Length; }
        }

        public void DisplayGrayCode(int i)
        {
            var image = grayCodeImages[i];
            bitmap.CopyFromMemory(image.DataIntPtr, image.Width * 4);
            renderTarget.BeginDraw();
            renderTarget.DrawBitmap(bitmap, 1.0f, BitmapInterpolationMode.Linear);
            renderTarget.EndDraw();
            //Console.WriteLine("displaying Gray code " + i);
        }
        
        public void DisplayName(string name)
        {
            var brush = new SolidColorBrush(renderTarget, Color4.White);
            var layoutRect = new SharpDX.RectangleF(0, 0, bounds.Width, bounds.Height);

            renderTarget.BeginDraw();
            renderTarget.Clear(Color4.Black);
            renderTarget.DrawRectangle(new SharpDX.RectangleF(0, 0, bounds.Width, bounds.Height), brush, 10f);
            renderTarget.DrawText(name, textFormat, layoutRect, solidColorBrush);

            //int nx = 4;
            //int ny = 4;
            //for (int i = 0; i < nx - 1; i++)
            //    for (int j = 0; j < ny - 1; j++)
            //    {
            //        float x = (float)bounds.Width / (float)nx * (i + 1);
            //        float y = (float)bounds.Height / (float)ny * (j + 1);

            //        const int w = 10;
            //        renderTarget.DrawLine(new Vector2(x - w, y), new Vector2(x + w, y), brush, 1);
            //        renderTarget.DrawLine(new Vector2(x, y - w), new Vector2(x, y + w), brush, 1);
            //    }

            int nx = 4;
            int ny = 4;
            for (int i = 0; i < nx - 1; i++)
            {
                float x = (float)bounds.Width / (float)nx * (i + 1);
                renderTarget.DrawLine(new Vector2(x, 0), new Vector2(x, bounds.Height), brush, 1);
            }
            for (int i = 0; i < ny - 1; i++)
            {
                float y = (float)bounds.Height / (float)ny * (i + 1);
                renderTarget.DrawLine(new Vector2(0, y), new Vector2(bounds.Width, y), brush, 1);
            }
            renderTarget.EndDraw();

            brush.Dispose();
        }

        public void SetColor(float r, float g, float b)
        {
            renderTarget.BeginDraw();
            renderTarget.Clear(new Color4(r, g, b, 1.0f));
            renderTarget.EndDraw();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            bitmap.Dispose();
            renderTarget.Dispose();
            for (int i = 0; i < grayCodeImages.Length; i++)
                grayCodeImages[i].Dispose();
        }

    }
}
