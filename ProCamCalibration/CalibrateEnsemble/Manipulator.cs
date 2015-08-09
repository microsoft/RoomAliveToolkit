using SharpDX;
using System;
using System.Windows.Forms;

namespace RoomAliveToolkit
{
    /// <summary>
    /// Interactively modifies the provided view matrix based on the mouse and keyboard input of the associated control.
    /// Usage: after calling creator with the associate control, set View, Projection and Viewport properties.
    /// Call Update() once per game loop to obtain updated view matrix.
    /// A right handed coordinate system, with +X left, +Y up and +Z forward is assumed.
    /// W, A, S, D, E, and C keys translate in X, Y and Z.
    /// Mouse click and drag rotates around X and Y.
    /// Holding down the ctrl key while dragging rotates around Z.
    /// Holding down the shift key while dragging translates in X and Y. 
    /// Mouse wheel translates along Z.
    /// R key resets to a designated 'original view'.
    /// </summary>
    public class Manipulator
    {
        /// <summary>
        /// Create a Manipulator with an associated control.
        /// </summary>
        /// <param name="control"></param>
        public Manipulator(Control control)
        {
            this.control = control;
            control.MouseEnter += panel_MouseEnter;
            control.MouseLeave += panel_MouseLeave;
            control.MouseMove += panel_MouseMove;
            control.MouseHover += panel_MouseHover;
            control.Parent.MouseWheel += Parent_MouseWheel;
            stopwatch.Start();
        }

        /// <summary>
        /// Post-multiply view matrix.
        /// </summary>
        public SharpDX.Matrix View
        {
            get { return view; }
            set
            {
                // get orientation from view matrix
                orientation = value;
                orientation.TranslationVector = Vector3.Zero;

                // get position from view matrix
                var invR = value;
                invR.TranslationVector = Vector3.Zero;
                invR.Transpose();
                position = -(value * invR).TranslationVector;

                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Post-multiply view matrix which is set when user hits R key.
        /// </summary>
        public SharpDX.Matrix OriginalView;

        /// <summary>
        /// Post-multiply projection matrix.
        /// </summary>
        public SharpDX.Matrix Projection;

        /// <summary>
        /// Viewport associated with the control.
        /// </summary>
        public Viewport Viewport;

        /// <summary>
        /// Speed of translation using W, A, S, D, E and C keys. Graphics units/s.
        /// </summary>
        public float TranslationSpeed = 3;

        /// <summary>
        /// Translation speed scale while using Shift key.
        /// </summary>
        public float ShiftTranslationSpeed = 2;

        /// <summary>
        /// Converts mouse wheel units to translation graphics units.
        /// </summary>
        public float MouseWheelStep = 1 / 1000f;

        /// <summary>
        /// Game loop update.
        /// </summary>
        /// <returns>Updated view matrix.</returns>
        public SharpDX.Matrix Update()
        {
            long now = stopwatch.ElapsedTicks;
            if (mouseOver)
            {
                float dt = (float)(now - lastTime) / (float)System.Diagnostics.Stopwatch.Frequency;
                Update(dt);
            }
            lastTime = now;
            lastMousePosition = mousePosition;
            return view;
        }

        void UpdateViewMatrix()
        {
            view = SharpDX.Matrix.Translation(-position) * orientation;
        }

        void Translate(Vector3 dir, float step)
        {
            var invR = orientation;
            invR.Transpose();
            position += step * Vector3.TransformCoordinate(dir, invR);
            UpdateViewMatrix();
        }

        void Update(float dt)
        {
            // translation
            float step = TranslationSpeed * dt;

            // Z
            if (KeyDown('W'))
                Translate(Vector3.UnitZ, step);
            else if (KeyDown('S'))
                Translate(-Vector3.UnitZ, step);

            // X
            if (KeyDown('A'))
                Translate(Vector3.UnitX, step);
            else if (KeyDown('D'))
                Translate(-Vector3.UnitX, step);

            // Y
            if (KeyDown('E'))
                Translate(Vector3.UnitY, step);
            else if (KeyDown('C'))
                Translate(-Vector3.UnitY, step);

            if (KeyDown('R'))
                View = OriginalView;

            if (mouseWheel != 0)
            {
                Translate(-Vector3.UnitZ, mouseWheel * MouseWheelStep);
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

                    var centerRay = Viewport.Unproject(new Vector3((float)Viewport.Width / 2f, (float)Viewport.Height / 2f, 0), Projection, orientation, SharpDX.Matrix.Identity);
                    centerRay.Normalize();

                    var startRay = Viewport.Unproject(new Vector3(startMousePosition.X, startMousePosition.Y, 0), Projection, orientation, SharpDX.Matrix.Identity);
                    startRay.Normalize();

                    var endRay = Viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0), Projection, orientation, SharpDX.Matrix.Identity);
                    endRay.Normalize();

                    float startScale = ShiftTranslationSpeed / Vector3.Dot(centerRay, startRay);
                    float endScale = ShiftTranslationSpeed / Vector3.Dot(centerRay, endRay);

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

                    SharpDX.Matrix dR;

                    if (rotateXY)
                    {
                        var startRay = Viewport.Unproject(new Vector3(startMousePosition.X, startMousePosition.Y, 0), Projection, startOrientation, SharpDX.Matrix.Identity);
                        startRay.Normalize();

                        var endRay = Viewport.Unproject(new Vector3(mousePosition.X, mousePosition.Y, 0), Projection, startOrientation, SharpDX.Matrix.Identity);
                        endRay.Normalize();

                        float angle = (float)Math.Acos(Vector3.Dot(startRay, endRay));
                        var axis = Vector3.Cross(startRay, endRay);
                        axis.Normalize();
                        dR = SharpDX.Matrix.RotationAxis(axis, angle);
                    }
                    else // rotate around Z
                    {
                        var center = new Vector2((float)control.ClientSize.Width / 2f, (float)control.ClientSize.Height / 2f);

                        var startRay2D = new Vector2(startMousePosition.X, startMousePosition.Y);
                        var endRay = new Vector2(mousePosition.X, mousePosition.Y);
                        startRay2D -= center;
                        endRay -= center;

                        startRay2D.Normalize();
                        endRay.Normalize();

                        float angle = (float)Math.Atan2(endRay.Y, endRay.X) - (float)Math.Atan2(startRay2D.Y, startRay2D.X);
                        var axis = Viewport.Unproject(new Vector3(center.X, center.Y, 0), Projection, startOrientation, SharpDX.Matrix.Identity);
                        axis.Normalize();
                        dR = SharpDX.Matrix.RotationAxis(axis, angle);
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
            return KeyDown((int)key);
        }

        bool KeyDown(int key)
        {
            return (RoomAliveToolkit.Win32.GetAsyncKeyState(key) >> 15 != 0);
        }

        SharpDX.Matrix view;
        Vector3 position;
        SharpDX.Matrix orientation;
        Control control;
        bool rotating = false;
        bool translating = false;
        bool rotateXY;
        SharpDX.Matrix startOrientation;
        System.Drawing.Point startMousePosition, mousePosition, lastMousePosition;
        Vector3 startPosition;
        bool mouseOver;
        int mouseWheel;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        long lastTime;
    }
}
