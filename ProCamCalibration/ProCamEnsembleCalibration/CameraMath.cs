using System;
using System.Collections.Generic;

namespace RoomAliveToolkit
{
    public class CameraMath
    {
        public static void Project(RoomAliveToolkit.Matrix cameraMatrix, RoomAliveToolkit.Matrix distCoeffs, double x, double y, double z, out double u, out double v)
        {
            double xp = x / z;
            double yp = y / z;

            double fx = cameraMatrix[0, 0];
            double fy = cameraMatrix[1, 1];
            double cx = cameraMatrix[0, 2];
            double cy = cameraMatrix[1, 2];
            double k1 = distCoeffs[0];
            double k2 = distCoeffs[1];

            // compute f(xp, yp)
            double rSquared = xp * xp + yp * yp;
            double xpp = xp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
            double ypp = yp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
            u = fx * xpp + cx;
            v = fy * ypp + cy;
        }

        public static void Undistort(RoomAliveToolkit.Matrix cameraMatrix, RoomAliveToolkit.Matrix distCoeffs, double xin, double yin, out double xout, out double yout)
        {
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];
            float[] kappa = new float[] { (float)distCoeffs[0], (float)distCoeffs[1] };
            Undistort(fx, fy, cx, cy, kappa, xin, yin, out xout, out yout);
        }

        public static void Undistort(float fx, float fy, float cx, float cy, float[] kappa, double xin, double yin, out double xout, out double yout)
        {
            // maps coords in undistorted image (xin, yin) to coords in distorted image (xout, yout)
            double x = (xin - cx) / fx;
            double y = (yin - cy) / fy; // chances are you will need to flip y before passing in: imageHeight - yin

            // Newton Raphson
            double ru = Math.Sqrt(x * x + y * y);
            double rdest = ru;
            double factor = 1.0;

            bool converged = false;
            for (int j = 0; (j < 100) && !converged; j++)
            {
                double rdest2 = rdest * rdest;
                double num = 1.0, denom = 1.0;
                double rk = 1.0;

                factor = 1.0;
                for (int k = 0; k < 2; k++)
                {
                    rk *= rdest2;
                    factor += kappa[k] * rk;
                    denom += (2.0 * k + 3.0) * kappa[k] * rk;
                }
                num = rdest * factor - ru;
                rdest -= (num / denom);

                converged = (num / denom) < 0.0001;
            }
            xout = x / factor;
            yout = y / factor;
        }

        // Use DLT to obtain estimate of calibration rig pose; in our case this is the pose of the Kinect camera.
        // This pose estimate will provide a good initial estimate for subsequent projector calibration.
        // Note for a full PnP solution we should probably refine with Levenberg-Marquardt.
        // DLT is described in Hartley and Zisserman p. 178
        public static void DLT(Matrix cameraMatrix, Matrix distCoeffs, List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, out Matrix R, out Matrix t)
        {
            int n = worldPoints.Count;

            var A = Matrix.Zero(2 * n, 12);

            for (int j = 0; j < n; j++)
            {
                var X = worldPoints[j];
                var imagePoint = imagePoints[j];

                double x, y;
                Undistort(cameraMatrix, distCoeffs, imagePoint.X, imagePoint.Y, out x, out y);

                double w = 1;

                int ii = 2 * j;
                A[ii, 4] = -w * X[0];
                A[ii, 5] = -w * X[1];
                A[ii, 6] = -w * X[2];
                A[ii, 7] = -w;

                A[ii, 8] = y * X[0];
                A[ii, 9] = y * X[1];
                A[ii, 10] = y * X[2];
                A[ii, 11] = y;

                ii++; // next row
                A[ii, 0] = w * X[0];
                A[ii, 1] = w * X[1];
                A[ii, 2] = w * X[2];
                A[ii, 3] = w;

                A[ii, 8] = -x * X[0];
                A[ii, 9] = -x * X[1];
                A[ii, 10] = -x * X[2];
                A[ii, 11] = -x;
            }

            // Pcolumn is the eigenvector of ATA with the smallest eignvalue
            var Pcolumn = new Matrix(12, 1);
            {
                var ATA = new Matrix(12, 12);
                ATA.MultATA(A, A);

                var V = new Matrix(12, 12);
                var ww = new Matrix(12, 1);
                ATA.Eig(V, ww);

                Pcolumn.CopyCol(V, 0);
            }

            // reshape into 3x4 projection matrix
            var P = new Matrix(3, 4);
            P.Reshape(Pcolumn);

            R = new Matrix(3, 3);
            t = new Matrix(3, 1);

            for (int ii = 0; ii < 3; ii++)
            {
                t[ii] = P[ii, 3];
                for (int jj = 0; jj < 3; jj++)
                    R[ii, jj] = P[ii, jj];
            }

            if (R.Det3x3() < 0)
            {
                R.Scale(-1);
                t.Scale(-1);
            }

            // orthogonalize R
            {
                var U = new Matrix(3, 3);
                var Vt = new Matrix(3, 3);
                var V = new Matrix(3, 3);
                var ww = new Matrix(3, 1);

                R.SVD(U, ww, V);
                Vt.Transpose(V);

                R.Mult(U, Vt);
                double s = ww.Sum() / 3.0;
                t.Scale(1.0 / s);
            }
        }

    }
}
