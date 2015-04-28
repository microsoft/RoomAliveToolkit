using System;
using System.Windows.Forms;
using SharpDX;

namespace Kinect2ShaderDemo
{
    public class Manipulator
    {
        public Manipulator(Control panel, Matrix view, Matrix projection, Viewport viewport)
        {
            this.panel = panel;
            this.projection = projection;
            this.viewport = viewport;

            SetView(view);
            this.originalView = view;

            panel.MouseEnter += panel_MouseEnter;
            panel.MouseLeave += panel_MouseLeave;
            panel.MouseMove += panel_MouseMove;
            panel.MouseHover += panel_MouseHover;
            panel.Parent.MouseWheel += Parent_MouseWheel;

            //panel.Focus();

            renderStopWatch.Start();
        }

        void Parent_MouseWheel(object sender, MouseEventArgs e)
        {
            mouseWheel += e.Delta;
        }


        void panel_MouseHover(object sender, EventArgs e)
        {
            mouseOver = true;
        }

        void panel_MouseMove(object sender, MouseEventArgs e)
        {
            mousePosition = e.Location;
        }

        void panel_MouseEnter(object sender, EventArgs e)
        {
            mouseOver = true;
        }

        void panel_MouseLeave(object sender, EventArgs e)
        {
            mouseOver = false;
        }


        bool KeyDown(char key)
        {
            return (RoomAliveToolkit.Win32.GetAsyncKeyState((int)key) != 0);
        }

        bool KeyDown(int key)
        {
            return (RoomAliveToolkit.Win32.GetAsyncKeyState(key) != 0);
        }

        public void SetView(Matrix view, bool setAsOriginalView = true)
        {
            //e.g. view = Matrix.LookAtRH(position, target, up);

            // set orientation and position
            orientation = view;
            orientation.TranslationVector = Vector3.Zero;

            var invR = view;
            invR.TranslationVector = Vector3.Zero;
            invR.Transpose();
            position = -(view * invR).TranslationVector;

            if (setAsOriginalView)
                this.originalView = view;
            UpdateViewMatrix();
        }

        public Matrix MyLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            var T = Matrix.Translation(-eye);

            var z = eye - target;
            z.Normalize();

            var x = Vector3.Cross(up, z);
            x.Normalize();

            var y = Vector3.Cross(z, x);

            var R = new Matrix();
            R.Column1 = new SharpDX.Vector4(x, 0);
            R.Column2 = new SharpDX.Vector4(y, 0);
            R.Column3 = new SharpDX.Vector4(z, 0);
            R.M44 = 1;

            var view = T * R;


            // now recover eye and R from view matrix
            var invR = view;
            invR.TranslationVector = Vector3.Zero;
            invR.Transpose();

            var eye2 = -(view * invR).TranslationVector;

            Console.WriteLine("eye " + eye);
            Console.WriteLine(eye2);

            return view;
        }

        public void UpdateViewMatrix()
        {
            view = Matrix.Translation(-position) * orientation;
            if (ViewMatrixChanged != null)
                ViewMatrixChanged(this, new ViewMatrixChangedEventArgs(view));
        }



        public void Update()
        {
            long now = renderStopWatch.ElapsedTicks;
            float dt = (float)(now - lastTime) / (float)System.Diagnostics.Stopwatch.Frequency;
            lastTime = now;

            //Console.WriteLine(mouseOver);

            //// a way to get mouse position without event
            //mousePosition = Cursor.Position; // screen coords
            //// be careful re: deadlock with Kinect using GUI thread; might be best to put Kinect in another thread
            //// also ensure that call to Update() here is not under a lock
            //panel.Invoke(new Action(() => mousePosition = panel.PointToClient(mousePosition))); 

            //bool mouseOver = panel.ClientRectangle.Contains(mousePosition);
            if (mouseOver)
                Update(dt);

            lastMousePosition = mousePosition;
        }

        void Update(float dt)
        {
            // translation
            float step = 3 * dt;

            if (KeyDown('W'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(Vector3.UnitZ, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('S'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(-Vector3.UnitZ, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('A'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(Vector3.UnitX, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('D'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(-Vector3.UnitX, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('E'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(Vector3.UnitY, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('C'))
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(-Vector3.UnitY, invR);
                position += x * step;
                UpdateViewMatrix();
            }
            else if (KeyDown('R'))
            {
                SetView(originalView);
            }

            if (mouseWheel != 0)
            {
                Matrix invR = orientation;
                invR.Transpose();
                var x = Vector3.TransformCoordinate(-Vector3.UnitZ, invR);
                position += x * mouseWheel / 1000f;
                UpdateViewMatrix();
                mouseWheel = 0;
            }


            bool mouseDown = (Control.MouseButtons & MouseButtons.Left) != 0;
            bool ctrlDown = KeyDown(0x11);
            bool shiftDown = KeyDown(0x10);

            if (mouseDown && (mousePosition != lastMousePosition))
            {
                if (shiftDown) // translate
                {
                    if (!translating)
                    {
                        translating = true;
                        startPosition = position;
                        startMousePosition = lastMousePosition;
                    }


                    var centerRay = viewport.Unproject(new Vector3((float)viewport.Width/2f, (float)viewport.Height/2f, 0), projection, orientation, Matrix.Identity);
                    centerRay.Normalize();

                    var startRay = viewport.Unproject(new Vector3(startMousePosition.X, startMousePosition.Y, 0), projection, orientation, Matrix.Identity);
                    startRay.Normalize();

                    var endRay = viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0), projection, orientation, Matrix.Identity);
                    endRay.Normalize();

                    const float s = 2f; // an overall scale of motion that should probably be combined with keyboard translation step
                    float startScale = s / Vector3.Dot(centerRay, startRay);
                    float endScale = s / Vector3.Dot(centerRay, endRay);

                    var translation = endScale * endRay - startScale * startRay;

                    position = startPosition - translation;
                    UpdateViewMatrix();
                }
                else // rotate
                {
                    if (!rotating)
                    {
                        rotating = true;
                        rotateXY = !ctrlDown;
                        startOrientation = orientation;
                        startMousePosition = lastMousePosition;
                    }

                    var dR = Matrix.Identity;

                    if (rotateXY)
                    {
                        var startRay = viewport.Unproject(new Vector3(startMousePosition.X, startMousePosition.Y, 0), projection, startOrientation, Matrix.Identity);
                        startRay.Normalize();

                        var endRay = viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0), projection, startOrientation, Matrix.Identity);
                        endRay.Normalize();

                        float angle = (float)Math.Acos(Vector3.Dot(startRay, endRay));
                        var axis = Vector3.Cross(startRay, endRay);
                        axis.Normalize();
                        dR = Matrix.RotationAxis(axis, angle);
                    }
                    else // rotate around Z
                    {
                        var center = new Vector2((float)panel.ClientSize.Width / 2f, (float)panel.ClientSize.Height / 2f);

                        var startRay2D = new Vector2(startMousePosition.X, startMousePosition.Y);
                        var endRay = new Vector2(mousePosition.X, mousePosition.Y);
                        startRay2D -= center;
                        endRay -= center;

                        startRay2D.Normalize();
                        endRay.Normalize();

                        float angle = (float)Math.Atan2(endRay.Y, endRay.X) - (float)Math.Atan2(startRay2D.Y, startRay2D.X);
                        var axis = viewport.Unproject(new Vector3(center.X, center.Y, 0), projection, startOrientation, Matrix.Identity);
                        axis.Normalize();
                        dR = Matrix.RotationAxis(axis, angle);
                    }

                    orientation = dR * startOrientation;
                    orientation.Orthonormalize();

                    UpdateViewMatrix();
                }
            }
            else if (!mouseDown)
            {
                translating = false;
                rotating = false;
            }
        }

        public class ViewMatrixChangedEventArgs : EventArgs
        {
            public ViewMatrixChangedEventArgs(Matrix view) { this.view = view; }
            public Matrix view;
        }
        public delegate void ViewMatrixChangedEventHandler(object sender, ViewMatrixChangedEventArgs e);
        public event ViewMatrixChangedEventHandler ViewMatrixChanged;

        public Viewport viewport;
        Matrix view;
        public Matrix projection;
        Vector3 position = Vector3.Zero;
        Matrix orientation = Matrix.Identity;


        Control panel;
        bool rotating = false;
        bool translating = false;
        bool rotateXY;
        Matrix startOrientation;
        System.Drawing.Point startMousePosition;
        Vector3 startPosition;
        bool mouseOver;
        int mouseWheel;
        Matrix originalView;
        System.Diagnostics.Stopwatch renderStopWatch = new System.Diagnostics.Stopwatch();
        System.Drawing.Point mousePosition, lastMousePosition;
        long lastTime;

    }
}
