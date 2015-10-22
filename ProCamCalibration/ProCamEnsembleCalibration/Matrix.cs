using System;
using System.Xml.Serialization;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Threading.Tasks;

namespace RoomAliveToolkit
{

	public class Matrix
	{
		protected int m, n, mn;
		protected double[] data;

		protected Matrix squareWorkMatrix1, workColumn1, mbymWorkMatrix1;
		protected int[] workIndx1;

		public Matrix() {}

		public Matrix(int m, int n)
		{
			this.m = m;
			this.n = n;
			mn = m*n;
			data = new double[mn];
		}

		public Matrix(Matrix A)
		{
			m = A.m;
			n = A.n;
			mn = m*n;
			data = new double[mn];
			Copy(A);
		}

        public static Matrix Identity(int m, int n)
        {
            var A = new Matrix(m, n);
            A.Identity();
            return A;
        }

        public static Matrix Zero(int m, int n)
        {
            var A = new Matrix(m, n);
            A.Zero();
            return A;
        }

		// properties
		// ValuesByColumn will be serialized to XML
		public double[][] ValuesByColumn
		{
			get 
			{
				double[][] A = new double[n][];

				for (int j = 0; j < n; j++)
					A[j] = new double[m];

				for (int i = 0; i < m; i++)
					for (int j = 0; j < n; j++)
						A[j][i] = this[i,j];
				return A;
			}
			set 
			{ 
				double[][] A = value;
				n = A.Length;
				m = A[0].Length;
				mn = m*n;
				data = new double[mn];
				for (int i = 0; i < m; i++)
					for (int j = 0; j < n; j++)
						this[i,j] = A[j][i];
				// should verify that each column is same length...
				squareWorkMatrix1 = null;
				workColumn1 = null;
				mbymWorkMatrix1 = null;
				workIndx1 = null;
			}
		}

		public int Rows
		{
			get { return m; }
		}

		public int Cols
		{
			get { return n; }
		}

		public int Size
		{
			get { return mn; }
		}


		
		// indexers
		public double this[int i, int j]
		{
			get { return data[i*n + j]; }
			set	{ data[i*n + j] = value; }
		}

		public double this[int i]
		{
			get { return data[i]; }
			set	{ data[i] = value; }
		}

        public float[] AsFloatArray()
        {
            float[] array = new float[mn];
            for (int i = 0; i < mn; i++)
            {
                array[i] = (float)data[i];
            }
            return array;
        }
        // copy
        public void Copy(Matrix A)
		{
			for (int i = 0; i < m; i++)
				for (int j = 0; j < n; j++)
					this[i,j] = A[i,j];
		}
		public static void CopyRange(Matrix A, int ai, int aj, int m, int n, Matrix B, int bi, int bj)
		{
			for (int i = 0; i < m; i++)
				for (int j = 0; j < n; j++)
					B[bi + i,bj + j] = A[ai + i,aj + j];
		}
		public void Copy(int bi, int bj, Matrix A) { CopyRange(A, 0, 0, A.Rows, A.Cols, this, bi, bj); }
		public void Copy(int bi, int bj, Matrix A, int ai, int aj, int rows, int cols) { CopyRange(A, ai, aj, rows, cols, this, bi, bj); }
		public static void CopyRow(Matrix A, int row, Matrix B)
		{
			for (int j = 0; j < A.n; j++)
				B.data[j] = A[row,j];
		}
		public void CopyRow(Matrix A, int row) { CopyRow(A, row, this); }
		public static void CopyCol(Matrix A, int col, Matrix B)
		{
			for (int i = 0; i < A.m; i++)
				B.data[i] = A[i,col];
		}
		public void CopyCol(Matrix A, int col) { CopyCol(A, col, this); }
		public static void CopyDiag(Matrix A, Matrix B)
		{
			int maxd = (A.m > A.n) ? A.m : A.n;
			for (int i = 0; i < maxd; i++)
				B.data[i] = A[i,i];
		}
		public void CopyDiag(Matrix A) { CopyDiag(A, this); }


        public void Diag(Matrix A, Matrix d)
        {
            A.Zero();
            for (int i = 0; i < A.m; i++)
                A[i, i] = d[i];
        }
        public void Diag(Matrix d)
        {
            Diag(this, d);
        }

		// equals
		public static bool Equals(Matrix A, Matrix B)
		{
			for (int i = 0; i < A.mn; i++)
				if (A.data[i] != B.data[i]) return false;
			return true;
		}
		public bool Equals(Matrix A) { return Equals(A, this); }
		
		// change shape
		public static void Transpose(Matrix A, Matrix B)
		{
			if (A != B)
			{
				for (int i = 0; i < A.m; i++)
					for (int j = 0; j < A.n; j++)
						B[j,i] = A[i,j];
			}
			else // must be square
			{
				double s;
				for (int i = 0; i < A.m; i++)
					for (int j = 0; j < i; j++)
					{
						s = A[i,j];
						A[i,j] = A[j,i];
						A[j,i] = s;
					}
				A.squareWorkMatrix1 = null;
				A.workColumn1 = null;
				A.workIndx1 = null;
			}

		}
		public void Transpose(Matrix A) { Transpose(A, this); }
		public void Transpose() { Transpose(this, this); }		
		public static void Reshape(Matrix A, Matrix B)
		{
			int k = 0;
			for (int i = 0; i < A.m; i++)
				for (int j = 0; j < A.n; j++)
					B.data[k++] = A[i,j];
		}
		public void Reshape(Matrix A) { Reshape(A, this); }
		
		// matrix-scalar ops
		public static void Identity(Matrix A)
		{
			for (int i = 0; i < A.m; i++)
				for (int j = 0; j < A.n; j++)
					if (i == j)
						A[i,j] = 1.0;
					else
						A[i,j] = 0.0;
		}
		public void Identity() { Identity(this); }
		public static void Set(Matrix A, double c)
		{
			for (int i = 0; i < A.mn; i++)
				A.data[i] = c;
		}

		public void Set(double c) { Set(this, c); }
		public void Zero() { Set(0.0); }
		public void Randomize()
		{
			System.Random rnd = new System.Random();
			for (int i = 0; i < mn; i++)
				data[i] = rnd.NextDouble();

		}
		public void Linspace(double x0, double x1)
		{
			double dx = (x1 - x0) / (double)(mn - 1);
			for (int i = 0; i < mn; i++)
				data[i] = x0 + dx * i;
		}

		public static void Pow(Matrix A, double c, Matrix B)
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = Math.Pow(A.data[i], c); 
		}
		public void Pow(Matrix A, double c) { Pow(A, c, this);}
		public void Pow(double c) { Pow(this, c, this); }
		public static void Exp(Matrix A, Matrix B)
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = Math.Exp(A.data[i]);
		}

		public void Exp(Matrix A) { Exp(A, this);}
		public void Exp() { Exp(this, this); }
		public static void Log(Matrix A, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = Math.Log(A.data[i]);
		}

		public void Log(Matrix A) { Log(A, this);}
		public void Log() { Log(this, this); }
		public static void Abs(Matrix A, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = Math.Abs(A.data[i]);
		}

		public void Abs(Matrix A) { Abs(A, this);}
		public void Abs() { Abs(this, this); }
		public static void Add(Matrix A, double c, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = c + A.data[i];
		}

		public void Add(Matrix A, double c) { Add(A, c, this);}
		public void Add(double c) { Add(this, c, this); }
		public static void Scale(Matrix A, double c, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = c * A.data[i];
		}

		public void Scale(Matrix A, double c) { Scale(A, c, this);}
		public void Scale(double c) { Scale(this, c, this); }
		public static void ScaleAdd(Matrix A, double c, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] += c * A.data[i];
		}

		public void ScaleAdd(Matrix A, double c) { ScaleAdd(A, c, this);}
		public void ScaleAdd(double c) { ScaleAdd(this, c, this); }
		public static void Reciprocal(Matrix A, Matrix B)		
		{
			for (int i = 0; i < A.mn; i++)
				B.data[i] = 1.0 / A.data[i];
		}

		public void Reciprocal(Matrix A) { Reciprocal(A, this); }
		public void Reciprocal() { Reciprocal(this, this); }
		public static void Bound(Matrix A, Matrix B, Matrix C)
		{
			for (int i = 0; i < A.mn; i++)
			{
				if (C.data[i] < A.data[i])
					C.data[i] = A.data[i];
				if (C.data[i] > B.data[i])
					C.data[i] = B.data[i];
			}
		}
		// limits data between elements of A and B
		public void Bound(Matrix A, Matrix B) { Bound(A, B, this); }
		
		// matrix-matrix elementwise ops
		public static void Add(Matrix A, Matrix B, Matrix C)
		{
			for (int i = 0; i < A.Size; i++)
				C.data[i] = A.data[i] + B.data[i];
		}
		public void Add(Matrix A, Matrix B) { Add(A, B, this); }		
		public void Add(Matrix B) { Add(this, B, this); }
		public static void Sub(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.mn; i++)
				C.data[i] = A.data[i] - B.data[i];
		}

		public void Sub(Matrix A, Matrix B) { Sub(A, B, this);}
		public void Sub(Matrix B) { Sub(this, B, this); }
		public static void ElemMult(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.mn; i++)
				C.data[i] = A.data[i] * B.data[i];
		}

		public void ElemMult(Matrix A, Matrix B) { ElemMult(A, B, this);}
		public void ElemMult(Matrix B) { ElemMult(this, B, this); }
		public static void Divide(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.mn; i++)
				C.data[i] = A.data[i] / B.data[i];
		}

		public void Divide(Matrix A, Matrix B) { Divide(A, B, this);}
		public void Divide(Matrix B) { Divide(this, B, this); }
					
		// vector ops
		public static double Dot(Matrix A, Matrix B)		
		{
			double sum = 0.0;
			for (int i = 0; i < A.mn; i++)
				sum += A.data[i] * B.data[i];
			return sum;
		}

		public double Dot(Matrix B) { return Dot(this, B); }
		public static void Outer(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < C.m; i++)
				for (int j = 0; j < C.n; j++)
					C[i,j] = A.data[i] * B.data[j];
		}

		public void Outer(Matrix A, Matrix B) { Outer(A, B, this); }
		public static void Cross(Matrix A, Matrix B, Matrix C)		
		{
			C.data[0] =  A.data[1]*B.data[2] - A.data[2]*B.data[1];
			C.data[1] =  A.data[2]*B.data[0] - A.data[0]*B.data[2];
			C.data[2] =  A.data[0]*B.data[1] - A.data[1]*B.data[0];
		}

		public void Cross(Matrix A, Matrix B) { Cross(A, B, this); }
		
		// matrix-matrix ops
		public static void Mult(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.m; i++)
				for (int j = 0; j < B.n; j++)
				{
                    double sum = 0;
                    for (int k = 0; k < A.n; k++)
						sum += A[i,k] * B[k,j];
                    C[i, j] = sum;
				}
		}

		public void Mult(Matrix A, Matrix B) { Mult(A, B, this); }
		public static void MultAAT(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.m; i++)
				for (int j = 0; j < B.m; j++) // B.n
				{
                    double sum = 0;
                    for (int k = 0; k < A.n; k++)
						sum += A[i,k] * B[j,k];
                    C[i, j] = sum;
				}
		}

		public void MultAAT(Matrix A, Matrix B) { MultAAT(A, B, this); }

		public static void MultATA(Matrix A, Matrix B, Matrix C)		
		{
			for (int i = 0; i < A.n; i++)      // A.m
				for (int j = 0; j < B.n; j++)
				{
                    double sum = 0;  
					for (int k = 0; k < A.m; k++)  // A.n
						sum += A[k,i] * B[k,j];
                    C[i, j] = sum;
				}
		}

		public void MultATA(Matrix A, Matrix B) { MultATA(A, B, this); }


        public static void MultATAParallel(Matrix A, Matrix B, Matrix C)		
		{
            Parallel.For(0, A.n, i =>
            {
                for (int j = 0; j < B.n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < A.m; k++)  // A.n
                        sum += A[k, i] * B[k, j];
                    C[i, j] = sum;
                }
            }
            );
        }

        public void MultATAParallel(Matrix A, Matrix B) { MultATAParallel(A, B, this); }



        public static Matrix<double> ToMathNet(Matrix A)
        {
            var B = Matrix<double>.Build.Dense(A.Rows, A.Cols);
            for (int i = 0; i < A.Rows; i++)
                for (int j = 0; j < A.Cols; j++)
                    B[i, j] = A[i, j];
            return B;
        }

        public static void FromMathNet(Matrix<double> A, Matrix B)
        {
            for (int i = 0; i < A.RowCount; i++)
                for (int j = 0; j < A.ColumnCount; j++)
                    B[i, j] = A[i, j];
        }

        public static void FromMathNet(Vector<double> a, Matrix B)
        {
            for (int i = 0; i < a.Count; i++)
                B[i] = a[i];
        }



        public void Inverse(Matrix A)
        {
            // invert A and store in this
            var Anet = ToMathNet(A);
            var inverse = Anet.Inverse();
            FromMathNet(inverse, this);
        }

   
        public static double Det3x3(Matrix A)
        {
            double a = A[0, 0];
            double b = A[0, 1];
            double c = A[0, 2];
            double d = A[1, 0];
            double e = A[1, 1];
            double f = A[1, 2];
            double g = A[2, 0];
            double h = A[2, 1];
            double i = A[2, 2];

            return (a * e * i + b * f * g + c * d * h) - (c * e * g + b * d * i + a * f * h);
        }

        public double Det3x3()
        {
            return Det3x3(this);
        }


		public void Eig(Matrix v, Matrix d) 
        { 
            //Eig(this, v, d);

            var evd = ToMathNet(this).Evd();
            FromMathNet(evd.EigenVectors, v);
            for (int i = 0; i < this.Rows; i++)
                d[i] = evd.D[i, i];
        }


        public void Eig2x2(Matrix A, Matrix v, Matrix D)
        {
            double a = A[0, 0];
            double b = A[0, 1];
            double c = A[1, 0];
            double d = A[1, 1];

            // solve det(A - l*I) = 0 for eigenvalues l
            double s = Math.Sqrt((a + d) * (a + d) + 4 * (b * c - a * d));
            D[0] = (a + d + s) / 2;
            D[1] = (a + d - s) / 2;

            // solve for eigenvectors v in (A - l*I)*v = 0 for each eigenvalue
            // set v1 = 1.0
            double v0, n;

            // first eigenvector
            v0 = (D[0] - d) / c;
            n = Math.Sqrt(v0 * v0 + 1);

            v[0, 0] = v0 / n;
            v[1, 0] = 1.0 / n;

            // second eigenvector
            v0 = (D[1] - d) / c;
            n = Math.Sqrt(v0 * v0 + 1);

            v[0, 1] = v0 / n;
            v[1, 1] = 1.0 / n;
        }
        public void Eig2x2(Matrix v, Matrix d) { Eig2x2(this, v, d); }




        public void SVD(Matrix U, Matrix w, Matrix V)
        {
            //SVD(this, U, w, V);

            var svd = ToMathNet(this).Svd();
            FromMathNet(svd.U, U);
            FromMathNet(svd.S, w);
            FromMathNet(svd.VT.Transpose(), V);
        }


        public static void LeastSquares(Matrix x, Matrix A, Matrix b)
        {
            // use svd
            // for overdetermined systems A*x = b
            // x = V * diag(1/wj) * U T * b
            // NRC p. 66

            int m = A.m;
            int n = A.n;

            Matrix U = new Matrix(m, n), V = new Matrix(n, n), w = new Matrix(n, 1), W = new Matrix(n, n);
            A.SVD(U, w, V);
            w.Reciprocal();
            W.Diag(w);

            Matrix M = new Matrix(n, n);
            M.Mult(V, W);

            Matrix N = new Matrix(n, m);
            N.MultAAT(M, U);

            x.Mult(N, b);
        }

        public void LeastSquares(Matrix A, Matrix b)
        {
            LeastSquares(this, A, b);
        }






		// rotation conversions
        public static void Rot2D(Matrix A, double theta)
        {
            // clockwise rotation
            double s = Math.Sin(theta);
            double c = Math.Cos(theta);
            A[0, 0] = c;
            A[1, 0] = s;
            A[0, 1] = -s;
            A[1, 1] = c;
        }

        public void Rot2D(double theta)
        {
            Rot2D(this, theta);
        }

		public static void RotEuler2Matrix(double x, double y, double z, Matrix A)		
		{
			double s1 = Math.Sin(x);
			double s2 = Math.Sin(y);
			double s3 = Math.Sin(z);
			double c1 = Math.Cos(x);
			double c2 = Math.Cos(y);
			double c3 = Math.Cos(z);

			A[0,0] = c3*c2;
			A[0,1] = -s3*c1 + c3*s2*s1;
			A[0,2] = s3*s1 + c3*s2*c1;
			A[1,0] = s3*c2;
			A[1,1] = c3*c1 + s3*s2*s1;
			A[1,2] = -c3*s1 + s3*s2*c1;
			A[2,0] = -s2;
			A[2,1] = c2*s1;
			A[2,2] = c2*c1;		
		}

		public void RotEuler2Matrix(double x, double y, double z) { RotEuler2Matrix(x, y, z, this); }
		public static void RotFromTo2Quat(Matrix x, Matrix y, Matrix q)		
		{
			Matrix axis = new Matrix(3,1);
			axis.Cross(y, x);
			axis.Normalize();

			double angle = Math.Acos(x.Dot(y));
			double s = Math.Sin(angle/2.0);
		
			q[0] = axis[0]*s;
			q[1] = axis[1]*s;
			q[2] = axis[2]*s;
			q[3] = Math.Cos(angle/2.0);
		}

		public void RotFromTo2Quat(Matrix x, Matrix y) { RotFromTo2Quat(x, y, this); }
		public static void RotQuat2Matrix(Matrix q, Matrix A)		
		{
			double X = q[0];
			double Y = q[1];
			double Z = q[2];
			double W = q[3];
		
			// Watt and Watt p. 363
			
			double s = 2.0/Math.Sqrt(X*X + Y*Y + Z*Z + W*W);
		
			double xs = X*s;   double ys = Y*s;   double zs = Z*s;
			double wx = W*xs;  double wy = W*ys;  double wz = W*zs;
			double xx = X*xs;  double xy = X*ys;  double xz = X*zs;
			double yy = Y*ys;  double yz = Y*zs;  double zz = Z*zs;
		
		
			A[0,0] = 1 - (yy + zz);
			A[0,1] = xy + wz;
			A[0,2] = xz - wy;
		
			A[1,0] = xy - wz;
			A[1,1] = 1 - (xx + zz);
			A[1,2] = yz + wx;
		
			A[2,0] = xz + wy;
			A[2,1] = yz - wx;
			A[2,2] = 1 - (xx + yy);
		}

		public void RotQuat2Matrix(Matrix q) { RotQuat2Matrix(q, this); }
		public static void RotAxisAngle2Quat(Matrix axis, double angle, Matrix q)		
		{
			q[0] = axis[0] * Math.Sin(angle/2.0);
			q[1] = axis[1] * Math.Sin(angle/2.0);
			q[2] = axis[2] * Math.Sin(angle/2.0);
			q[3] = Math.Cos(angle/2.0);

		}

		public void RotAxisAngle2Quat(Matrix axis, double angle) { RotAxisAngle2Quat(axis, angle, this); }
		public static void RotMatrix2Quat(Matrix A, Matrix q)		
		{
			// Watt and Watt p. 362
			double trace = A[0,0] + A[1,1] + A[2,2] + 1.0;
			q[3] = Math.Sqrt(trace);

			q[0] = (A[2,1] - A[1,2])/(4*q[3]);
			q[1] = (A[0,2] - A[2,0])/(4*q[3]);
			q[2] = (A[1,0] - A[0,1])/(4*q[3]);

			// not tested
		}

		public void RotMatrix2Quat(Matrix A) { RotMatrix2Quat(A, this); }

        public void RotMatrix2Euler(ref double x, ref double y, ref double z)
        {
            RotMatrix2Euler(this, ref x, ref y, ref z);
        }
		public static void RotMatrix2Euler(Matrix A, ref double x, ref double y, ref double z)		
		{
			y = -Math.Asin(A[2,0]);
			double C = Math.Cos(y);

			double cost3 = A[0,0] / C;
			double sint3 = A[1,0] / C;
			z = Math.Atan2(sint3, cost3);

			double sint1 = A[2,1] / C;
			double cost1 = A[2,2] / C;
			x = Math.Atan2(sint1, cost1);
		}

		
		// quaternion ops; quat is ((X, Y, Z), W)
		public static void QuatMult(Matrix a, Matrix b, Matrix c)		
		{
			Matrix v1 = new Matrix(3,1);
			Matrix v2 = new Matrix(3,1);
			Matrix v3 = new Matrix(3,1);

			v1[0] = a[0];
			v1[1] = a[1];
			v1[2] = a[2];
			double s1 = a[3];

			v2[0] = b[0];
			v2[1] = b[1];
			v2[2] = b[2];
			double s2 = b[3];

			v3.Cross(v1, v2);

			c[0] = s1*v2[0] + s2*v1[0] + v3[0];
			c[1] = s1*v2[1] + s2*v1[1] + v3[1];
			c[2] = s1*v2[2] + s2*v1[2] + v3[2];
			c[3] = s1*s2 - v1.Dot(v2);
		}

		public void QuatMult(Matrix a, Matrix b) { QuatMult(a, b, this); }
		public static void QuatInvert(Matrix a, Matrix b)		
		{
			b[0] = -a[0];
			b[1] = -a[1];
			b[2] = -a[2];
			b[3] = a[3]; // w
		}

		public void QuatInvert(Matrix a) { QuatInvert(a, this); }
		public void QuatInvert() { QuatInvert(this, this); }
		public static void QuatRot(Matrix q, Matrix x, Matrix y)		
		{
			// p. 361 Watt and Watt

			Matrix p = new Matrix(4,1);
			p[0] = x[0];
			p[1] = x[1];
			p[2] = x[2];
			p[3] = 0.0;

			Matrix q1 = new Matrix(4,1);
			Matrix q2 = new Matrix(4,1);
			Matrix qi = new Matrix(4,1);

			qi.QuatInvert(q);

			q1.QuatMult(q, p);
			q2.QuatMult(q1, qi);

			y[0] = q2[0];
			y[1] = q2[1];
			y[2] = q2[2];
		}

		public void QuatRot(Matrix q, Matrix x) { QuatRot(q, x, this); }
		
		// norms
		public double Minimum(ref int argmin)		
		{
			double min = data[0];
			int mini = 0;
			for (int i = 1; i < mn; i++)
				if (data[i] < min)
				{
					min = data[i];
					mini = i;
				}
			argmin = mini;
			return min;
		}

		public double Maximum(ref int argmax)		
		{
			double max = data[0];
			int maxi = 0;
			for (int i = 1; i < mn; i++)
				if (data[i] > max)
				{
					max = data[i];
					maxi = i;
				}
			argmax = maxi;
			return max;
		}

		public double Norm()		
		{
			double sum = 0;
			for (int i = 0; i < mn; i++)
				sum += data[i]*data[i];
			return Math.Sqrt(sum);
		}

		public double Sum()		
		{
			double sum = 0;
			for (int i = 0; i < mn; i++)
				sum += data[i];
			return sum;
		}

        public double SumSquares()
        {
            double sum = 0;
            for (int i = 0; i < mn; i++)
                sum += data[i]*data[i];
            return sum;
        }

		public double Product()		
		{
			double product = 1.0;
			for (int i = 0; i < mn; i++)
				product *= data[i];
			return product;
		}

		public static double L1distance(Matrix a, Matrix b)		
		{
			double s = 0.0;
			double d;
			for (int i = 0; i < a.mn; i++)
			{
				d = a.data[i] - b.data[i];
				s += Math.Abs(d);
			}
			return s;
		}

		public double L1distance(Matrix a) { return L1distance(a, this); }
		public static double L2distance(Matrix a, Matrix b)		
		{
			double s = 0.0;
			double d;
			for (int i = 0; i < a.mn; i++)
			{
				d = a.data[i] - b.data[i];
				s += d*d;
			}
			return Math.Sqrt(s);
		}

		public double L2distance(Matrix a) { return L2distance(a, this); }
		public static void Normalize(Matrix A, Matrix B)		
		{
			B.Scale(A, 1.0/A.Norm());
		}

		public void Normalize(Matrix A) { Normalize(A, this); }
		public void Normalize() { Normalize(this, this); }

        public double Magnitude()
        {
            double sum = 0;
            for (int i = 0; i < mn; i++)
            {
                sum += data[i] * data[i];
            }
            return sum;
        }


        public class SingularMatrixException : Exception { }

		public class EigException : Exception {}

		public void NormalizeRows() 
		{
			double sum;
			for (int i = 0; i < m; i++)
			{
				sum = 0;
				for (int j = 0; j < n; j++)
				{
					sum += this[i,j];
				}
				for (int j = 0; j < n; j++)
				{
					this[i,j] = this[i,j] / sum;
				}
			}
		}

        public static Matrix GaussianSample(int m, int n)
        {
            var A = new Matrix(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i, j] = NextGaussianSample(0, 1);
            return A;
        }

        public static Matrix GaussianSample(Matrix mu, double sigma)
        {
            int m = mu.Rows;
            int n = mu.Cols;

            var A = new Matrix(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    A[i, j] = NextGaussianSample(mu[i, j], sigma);
            return A;
        }

        static double z0, z1;
        static bool generate = false;
        static Random random = new Random();

        public static double NextGaussianSample(double mu, double sigma)
        {
            // Box-Muller transform
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

        public override string ToString()
		{
			string s = "";

			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					s += this[i,j].ToString();
					if (j < n-1)
						s += ", \t";
				}
				s += " \r\n";
			}
			return s;
		}



		public static void Test()
		{
			Matrix A = new Matrix(2,2);
			A[0,0] = 1.0;  A[0,1] = 2.0;
			A[1,0] = 3.0;  A[1,1] = 4.0;
 
			Console.WriteLine(A.ToString());


			// test serialization
			
			XmlSerializer serializer = new XmlSerializer(typeof(Matrix));
			TextWriter writer = new StreamWriter("test.xml");

			serializer.Serialize(writer, A);
			writer.Close();


			XmlSerializer deserializer = new XmlSerializer(typeof(Matrix));
			TextReader reader = new StreamReader("test.xml");
			Matrix A2 = (Matrix) deserializer.Deserialize(reader);

			Console.WriteLine(A2);

		}



	}

}
