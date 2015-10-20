using System;
using System.Collections.Generic;

namespace RoomAliveToolkit
{
    public class CameraMath
    {
        public static void Project(Matrix cameraMatrix, Matrix distCoeffs, double x, double y, double z, out double u, out double v)
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

        public static void Undistort(Matrix cameraMatrix, Matrix distCoeffs, double xin, double yin, out double xout, out double yout)
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

                int ii = 2 * j;
                A[ii, 4] = -X[0];
                A[ii, 5] = -X[1];
                A[ii, 6] = -X[2];
                A[ii, 7] = -1;

                A[ii, 8] = y * X[0];
                A[ii, 9] = y * X[1];
                A[ii, 10] = y * X[2];
                A[ii, 11] = y;

                ii++; // next row
                A[ii, 0] = X[0];
                A[ii, 1] = X[1];
                A[ii, 2] = X[2];
                A[ii, 3] = 1;

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
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    R[i, j] = P[i, j];

            if (R.Det3x3() < 0)
            {
                R.Scale(-1);
                P.Scale(-1);
            }

            // orthogonalize R
            {
                var U = new Matrix(3, 3);
                var V = new Matrix(3, 3);
                var ww = new Matrix(3, 1);
                R.SVD(U, ww, V);
                R.MultAAT(U, V);
            }

            // determine scale factor
            var RP = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    RP[i, j] = P[i, j];
            double s = RP.Norm() / R.Norm();

            t = new Matrix(3, 1);
            for (int i = 0; i < 3; i++)
                t[i] = P[i, 3];
            t.Scale(1.0 / s);
        }

        public static void PlanarDLT(Matrix cameraMatrix, Matrix distCoeffs, List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, Matrix Rplane, Matrix Tplane, out Matrix R, out Matrix t)
        {
            // transform world points to plane
            int n = worldPoints.Count;
            var worldPlanePoints = new List<Matrix>();
            for (int i = 0; i < n; i++)
            {
                var planePoint = new Matrix(3, 1);
                planePoint.Mult(Rplane, worldPoints[i]);
                planePoint.Add(Tplane);
                worldPlanePoints.Add(planePoint);
            }

            var Rdlt = new Matrix(3, 3);
            var Tdlt = new Matrix(3, 1);

            PlanarDLT(cameraMatrix, distCoeffs, worldPlanePoints, imagePoints, out Rdlt, out Tdlt);

            R = new Matrix(3, 3);
            t = new Matrix(3, 1);

            t.Mult(Rdlt, Tplane);
            t.Add(Tdlt);

            R.Mult(Rdlt, Rplane);
        }

        public static void PlanarDLT(Matrix cameraMatrix, Matrix distCoeffs, List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, out Matrix R, out Matrix t)
        {
            int n = worldPoints.Count;
            var undistortedImagePoints = new List<System.Drawing.PointF>();
            for (int i = 0; i < n; i++)
            {
                var imagePoint = imagePoints[i];
                double x, y;
                Undistort(cameraMatrix, distCoeffs, imagePoint.X, imagePoint.Y, out x, out y);
                var undistorted = new System.Drawing.PointF();
                undistorted.X = (float)x;
                undistorted.Y = (float)y;
                undistortedImagePoints.Add(undistorted);
            }

            var H = Homography(worldPoints, undistortedImagePoints);
            H.Scale(1.0 / H[2, 2]);

            var r1 = new Matrix(3, 1);
            r1.CopyCol(H, 0);

            var r2 = new Matrix(3, 1);
            r2.CopyCol(H, 1);

            t = new Matrix(3, 1);
            t.CopyCol(H, 2);
            t.Scale(1 / ((r1.Norm() + r2.Norm()) / 2.0));
            r1.Scale(1 / r1.Norm());
            r2.Scale(1 / r2.Norm());

            var r3 = new Matrix(3, 1);
            r3.Cross(r1, r2);

            R = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
            {
                R[i, 0] = r1[i];
                R[i, 1] = r2[i];
                R[i, 2] = r3[i];
            }
        }

        public static void TestDLT()
        {
            var cameraMatrix = Matrix.Identity(3, 3);
            cameraMatrix[0, 0] = 700;
            cameraMatrix[1, 1] = 700;
            cameraMatrix[0, 2] = 250;
            cameraMatrix[1, 2] = 220;

            var distCoeffs = new Matrix(5, 1);
            distCoeffs[0] = 0.05;
            distCoeffs[1] = -0.1;

            // generate a bunch of points in a volume
            // project under some other camera (view)

            var R = new Matrix(3, 3);
            R.RotEuler2Matrix(0.2, 0.3, 0.3);

            var t = new Matrix(3, 1);
            t[0] = 2;
            t[1] = 0;
            t[2] = -4;

            var modelPoints = new List<Matrix>();
            var imagePoints = new List<System.Drawing.PointF>();
            var zero3 = Matrix.Zero(3, 1);

            for (float z = 1f; z <= 3.0f; z += 0.4f)
                for (float y = -1f; y <= 1.0f; y += 0.4f)
                    for (float x = -1f; x <= 1.0f; x += 0.4f)
                    {
                        var model = new Matrix(3, 1);
                        model[0] = x;
                        model[1] = y;
                        model[2] = z;
                        modelPoints.Add(model);

                        // under our camera:
                        var transformedPoint = new Matrix(3, 1);
                        transformedPoint.Mult(R, model);
                        transformedPoint.Add(t);

                        var noise = GaussianSample(zero3, 0.1 * 0.1);
                        transformedPoint.Add(noise);

                        double u, v;
                        Project(cameraMatrix, distCoeffs, transformedPoint[0], transformedPoint[1], transformedPoint[2], out u, out v);

                        var image = new System.Drawing.PointF();
                        image.X = (float)u;
                        image.Y = (float)v;
                        imagePoints.Add(image);
                    }

            Console.WriteLine("x = [");
            for (int i = 0; i < imagePoints.Count; i++)
                Console.WriteLine("{0} {1}", imagePoints[i].X, imagePoints[i].Y);
            Console.WriteLine("]';");

            Console.WriteLine("X = [");
            for (int i = 0; i < modelPoints.Count; i++)
                Console.WriteLine("{0} {1} {2}", modelPoints[i][0], modelPoints[i][1], modelPoints[i][2]);
            Console.WriteLine("]';");

            Console.WriteLine("fc = [{0} {1}];", cameraMatrix[0, 0], cameraMatrix[1, 1]);
            Console.WriteLine("cc = [{0} {1}];", cameraMatrix[0, 2], cameraMatrix[1, 2]);
            Console.WriteLine("kc = [{0} {1} 0 0 0];", distCoeffs[0], distCoeffs[1]);
            Console.WriteLine();

            Console.WriteLine("R\n" + R);
            Console.WriteLine("t\n" + t);

            var Rest = new Matrix(3, 3);
            var test = new Matrix(3, 1);

            DLT(cameraMatrix, distCoeffs, modelPoints, imagePoints, out Rest, out test);

            Console.WriteLine("Rest\n" + Rest);
            Console.WriteLine("test\n" + test);
        }

        public static void TestPlanarDLT()
        {
            var cameraMatrix = Matrix.Identity(3, 3);
            cameraMatrix[0, 0] = 300;
            cameraMatrix[1, 1] = 300;
            cameraMatrix[0, 2] = 250;
            cameraMatrix[1, 2] = 220;

            var distCoeffs = new Matrix(5, 1);
            distCoeffs[0] = 0.05;
            distCoeffs[1] = -0.1;

            // generate a bunch of points in a plane
            // project under some other camera (view)

            var R = new Matrix(3, 3);
            R.RotEuler2Matrix(0.3, -0.2, 0.6);

            var t = new Matrix(3, 1);
            t[0] = 0.2;
            t[1] = 0.3;
            t[2] = 2;

            var modelR = new Matrix(3, 3);
            modelR.RotEuler2Matrix(-0.6, 0.2, 0.3);

            var modelT = new Matrix(3, 1);
            modelT[0] = -0.1;
            modelT[1] = 1.0;
            modelT[2] = 1.5;

            var worldPoints = new List<Matrix>();
            var worldTransformedPoints = new List<Matrix>();
            var imagePoints = new List<System.Drawing.PointF>();
            var zero3 = Matrix.Zero(3, 1);

            for (float y = -1f; y <= 1.0f; y += 0.2f)
                for (float x = -1f; x <= 1.0f; x += 0.2f)
                {
                    var model = new Matrix(3, 1);
                    model[0] = x;
                    model[1] = y;
                    model[2] = 0;

                    var noise = GaussianSample(zero3, 0.1 * 0.1);

                    var world = new Matrix(3, 1);
                    world.Mult(modelR, model);
                    world.Add(modelT);
                    world.Add(noise);
                    worldPoints.Add(world);

                    // under some camera:
                    var worldTransformed = new Matrix(3, 1);
                    worldTransformed.Mult(R, world);
                    worldTransformed.Add(t);
                    worldTransformedPoints.Add(worldTransformed);

                    double u, v;
                    Project(cameraMatrix, distCoeffs, worldTransformed[0], worldTransformed[1], worldTransformed[2], out u, out v);

                    var image = new System.Drawing.PointF();
                    image.X = (float)u;
                    image.Y = (float)v;
                    imagePoints.Add(image);
                }

            Console.WriteLine("R\n" + R);
            Console.WriteLine("t\n" + t);

            var Rplane = new Matrix(3, 1);
            var Tplane = new Matrix(3, 1);

            PlaneFit(worldPoints, out Rplane, out Tplane);

            var Rest = new Matrix(3, 3);
            var test = new Matrix(3, 1);

            PlanarDLT(cameraMatrix, distCoeffs, worldPoints, imagePoints, Rplane, Tplane, out Rest, out test);

            Console.WriteLine("Rest\n" + Rest);
            Console.WriteLine("test\n" + test);
        }

        static public double PlaneFit(IList<Matrix> X, out Matrix R, out Matrix t)
        {
            int n = X.Count;

            var mu = new Matrix(3, 1);
            for (int i = 0; i < n; i++)
                mu.Add(X[i]);
            mu.Scale(1f / (float)n);

            var A = new Matrix(3, 3);
            var xc = new Matrix(3, 1);
            var M = new Matrix(3, 3);
            for (int i = 0; i < X.Count; i++)
            {
                var x = X[i];
                xc.Sub(x, mu);
                M.Outer(xc, xc);
                A.Add(M);
            }
            var V = new Matrix(3, 3);
            var d = new Matrix(3, 1);
            A.Eig(V, d); // eigenvalues in ascending order

            // arrange in descending order so that z = 0
            var V2 = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
            {
                V2[i, 2] = V[i, 0];
                V2[i, 1] = V[i, 1];
                V2[i, 0] = V[i, 2];
            }

            R = new Matrix(3, 3);
            R.Transpose(V2);

            if (R.Det3x3() < 0)
                R.Scale(-1);

            t = new Matrix(3, 1);
            t.Mult(R, mu);
            t.Scale(-1);

            // min eigenvalue is the sum of squared distances to the plane
            return d[0];
        }

        public static Matrix Homography(List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints)
        {
            int n = worldPoints.Count;

            // normalize image coordinates
            var mu = new Matrix(2, 1);
            for (int i = 0; i < n; i++)
            {
                mu[0] += imagePoints[i].X;
                mu[1] += imagePoints[i].Y;
            }
            mu.Scale(1.0 / n);
            var muAbs = new Matrix(2, 1);
            for (int i = 0; i < n; i++)
            {
                muAbs[0] += Math.Abs(imagePoints[i].X - mu[0]);
                muAbs[1] += Math.Abs(imagePoints[i].Y - mu[1]);
            }
            muAbs.Scale(1.0 / n);

            var Hnorm = Matrix.Identity(3, 3);
            Hnorm[0, 0] = 1 / muAbs[0];
            Hnorm[1, 1] = 1 / muAbs[1];
            Hnorm[0, 2] = -mu[0] / muAbs[0];
            Hnorm[1, 2] = -mu[1] / muAbs[1];

            var invHnorm = Matrix.Identity(3, 3);
            invHnorm[0, 0] = muAbs[0];
            invHnorm[1, 1] = muAbs[1];
            invHnorm[0, 2] = mu[0];
            invHnorm[1, 2] = mu[1];


            var A = Matrix.Zero(2 * n, 9);
            for (int i = 0; i < n; i++)
            {
                var X = worldPoints[i];
                var imagePoint = imagePoints[i];

                var x = new Matrix(3, 1);
                x[0] = imagePoint.X;
                x[1] = imagePoint.Y;
                x[2] = 1;

                var xn = new Matrix(3, 1);
                xn.Mult(Hnorm, x);
 
                // Zhang's formulation; Hartley's is similar
                int ii = 2 * i;
                A[ii, 0] = X[0];
                A[ii, 1] = X[1];
                A[ii, 2] = 1;

                A[ii, 6] = -xn[0] * X[0];
                A[ii, 7] = -xn[0] * X[1];
                A[ii, 8] = -xn[0];

                ii++; // next row
                A[ii, 3] = X[0];
                A[ii, 4] = X[1];
                A[ii, 5] = 1;

                A[ii, 6] = -xn[1] * X[0];
                A[ii, 7] = -xn[1] * X[1];
                A[ii, 8] = -xn[1];
            }

            // h is the eigenvector of ATA with the smallest eignvalue
            var h = new Matrix(9, 1);
            {
                var ATA = new Matrix(9, 9);
                ATA.MultATA(A, A);

                var V = new Matrix(9, 9);
                var ww = new Matrix(9, 1);
                ATA.Eig(V, ww);

                h.CopyCol(V, 0);
            }

            var Hn = new Matrix(3, 3);
            Hn.Reshape(h);

            var H = new Matrix(3, 3);
            H.Mult(invHnorm, Hn);

            return H;
        }

        public static Matrix GaussianSample(int m, int n)
        {
            var A = new Matrix(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i,j] = NextGaussianSample(0, 1);
            return A;
        }

        public static Matrix GaussianSample(Matrix mu, double sigma)
        {
            int m = mu.Rows;
            int n = mu.Cols;

            var A = new Matrix(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i, j] = NextGaussianSample(mu[i,j], sigma);
            return A;
        }

        static double z0, z1;
        static bool generate = false;
        static Random random = new Random();

        public static double NextGaussianSample(double mu, double sigma)
        {
            const double epsilon = double.MinValue;
            const double tau = 2.0 * Math.PI;

            generate = !generate;
            if (!generate)
                return z1 * sigma + mu;

            double u1, u2;
            do
            {
                u1 = random.NextDouble();
                u2 = random.NextDouble();
            }
            while (u1 <= epsilon);

            z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(tau * u2);
            z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(tau * u2);
            return z0 * sigma + mu;
        }
    }
}
