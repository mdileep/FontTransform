/*
Copyright (C) 2001-2016 Peter Selinger
Copyright (C) 2009-2016 Wolfgang Nagl
Copyright (C) 2017 Dileep Miriyala :(Reorganised)
This program is free software; you can redistribute it and/or modify  it under the terms of the GNU General Public License as published by  the Free Software Foundation; either version 2 of the License, or (at  your option) any later version.
This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU  General Public License for more details.
You should have received a copy of the GNU General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;


namespace CsPotrace
{
	/// <summary>
	/// 
	/// </summary>
	struct ViewBox
	{
		public decimal MinX;
		public decimal MinY;

		public decimal Width;
		public decimal Height;
	}

	/// <summary>
	/// Kind of Curve : Line or Bezier
	/// </summary>
	enum CurveKind
	{
		Line,
		Bezier
	}
	/// <summary>
	/// Curve tags
	/// </summary>
	enum Tags
	{
		Corner = 1,
		CurveTo = 2
	}

	/// <summary>
	/// Holds the rounded coordinates of a Point
	/// </summary>
	struct Point
	{
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public Point(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public int X;

		public int Y;
	}

	/// <summary>
	/// Holds the precision coordinates of a Point
	/// </summary>
	class dPoint
	{
		/// <summary>
		/// x-coordinate
		/// </summary>
		public double x;

		/// <summary>
		/// y-coordinate
		/// </summary>
		public double y;

		/// <summary>
		/// Creates a point
		/// </summary>
		/// <param name="x">x-coordinate</param>
		/// <param name="y">y-coordinate</param>
		public dPoint(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public dPoint Copy()
		{
			return new dPoint(x, y);
		}

		/// <summary>
		/// ctor
		/// </summary>
		public dPoint()
		{

		}
	}

	/// <summary>
	/// Holds the information about der produced curves
	/// </summary>
	struct Curve
	{
		/// <summary>
		/// Bezier or Line
		/// </summary>
		public CurveKind Kind;

		/// <summary>
		/// Startpoint
		/// </summary>
		public dPoint A;

		/// <summary>
		/// ControlPoint
		/// </summary>
		public dPoint ControlPointA;

		/// <summary>
		/// ControlPoint
		/// </summary>
		public dPoint ControlPointB;

		/// <summary>
		/// Endpoint
		/// </summary>
		public dPoint B;

		/// <summary>
		/// Creates a curve
		/// </summary>
		/// <param name="Kind"></param>
		/// <param name="A">Startpoint</param>
		/// <param name="ControlPointA">Controlpoint</param>
		/// <param name="ControlPointB">Controlpoint</param>
		/// <param name="B">Endpoint</param>
		public Curve(CurveKind Kind, dPoint A, dPoint ControlPointA, dPoint ControlPointB, dPoint B)
		{

			this.Kind = Kind;
			this.A = A;
			this.B = B;
			this.ControlPointA = ControlPointA;
			this.ControlPointB = ControlPointB;
		}

		public override string ToString()
		{
			return this.Kind.ToString();
		}
	}

	/// <summary>
	/// Turn Policy : Line or Bezier
	/// </summary>
	enum TurnPolicy
	{
		Minority,
		Majority,
		Right,
		Black,
		White
	}

	/// <summary>
	/// Quad
	/// </summary>
	class Quad
	{
		/// <summary>
		/// ctor
		/// </summary>
		public Quad()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public double At(int x, int y)
		{
			return this.Data[x * 3 + y];
		}

		/// <summary>
		/// 
		/// </summary>
		public double[] Data = new double[9];
	}

	/// <summary>
	/// Holds the binary bitmap
	/// </summary>
	class BinaryMatrix
	{
		public byte[] data = null;

		public BinaryMatrix(int w, int h)
		{
			this.w = w;
			this.h = h;
			data = new byte[Size];
		}

		public int w = 0;
		public int h = 0;

		public int Size
		{
			get
			{
				return w * h;
			}
		}

		public bool At(int x, int y)
		{
			return ((x >= 0) && (x < this.w) && (y >= 0) && (y < this.h) && (this.data[this.w * y + x] == 1));
		}

		public Point Index(int i)
		{
			int y = i / w;
			return new Point(i - y * w, y);
		}

		public void Flip(int x, int y)
		{
			if (this.At(x, y))
			{
				this.data[this.w * y + x] = 0;
			}
			else
			{
				this.data[this.w * y + x] = 1;
			}
		}

		public BinaryMatrix Copy()
		{
			BinaryMatrix Result = new BinaryMatrix(w, h);
			for (int i = 0; i < Size; i++)
			{
				Result.data[i] = data[i];

			}
			return Result;
		}

	}

	/// <summary>
	/// Optimization Penalty
	/// </summary>
	class Optimization
	{
		public double Pen = 0;
		public dPoint[] C = new dPoint[2];
		public double T = 0;
		public double S = 0;
		public double Alpha = 0;
	}

	/// <summary>
	/// Path
	/// </summary>
	class Path
	{
		public int m = 0;
		public int Area = 0;
		public int Length = 0;
		public string Sign = "?";
		public List<Point> Points = new List<Point>();
		public int minX = 100000;
		public int minY = 100000;
		public int maxX = -1;
		public int maxY = -1;
		public double x0;
		public double y0;
		public int[] po;
		public int[] lon = null;
		public List<Sum> Sums = new List<Sum>();
		public PrimitiveCurve Curve = null;

		public override string ToString()
		{
			return Sign;
		}
	}

	/// <summary>
	/// Primitive Curve
	/// </summary>
	class PrimitiveCurve
	{

		/// <summary>
		/// Number of segments
		/// </summary>
		public int N;

		/// <summary>
		/// Tag[n]: POTRACE_CORNER or POTRACE_CURVETO 
		/// </summary>
		public Tags[] Tag;

		/// <summary>
		/// For POTRACE_CORNER, this equals c[1]
		/// </summary>
		public dPoint[] Vertex;

		/// <summary>
		/// Help Point
		/// </summary>
		public dPoint[] C = null;

		/// <summary>
		/// Applicable for POTRACE_CURVETO
		/// </summary>
		public double[] Alpha;

		/// <summary>
		/// Uncropped alpha parameter
		/// </summary>
		public double[] Alpha0;

		/// <summary>
		/// Beta
		/// </summary>
		public double[] Beta;

		/// <summary>
		/// Alpha Curve
		/// </summary>
		public int AlphaCurve = 0;

		public PrimitiveCurve(int Count)
		{
			N = Count;
			Tag = new Tags[N];
			Vertex = new dPoint[N];
			Alpha = new double[N];
			Alpha0 = new double[N];
			Beta = new double[N];
			C = new dPoint[N * 3];
		}
	}

	struct Sum
	{
		public Sum(double x, double y, double xy, double x2, double y2)
		{
			this.x = x;
			this.y = y;
			this.xy = xy;
			this.x2 = x2;
			this.y2 = y2;
		}
		public double x, y, xy, x2, y2;
	}

	static class MathUtil
	{
		/// <summary>
		/// Range over the straight line segment [a,b] when lambda ranges over [0,1]
		/// </summary>
		/// <param name="lambda"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static dPoint Interval(double lambda, dPoint a, dPoint b)
		{
			var res = new dPoint();
			res.x = a.x + lambda * (b.x - a.x);
			res.y = a.y + lambda * (b.y - a.y);
			return res;
		}

		/// <summary>
		///  Direction that is 90 degrees counterclockwise from p2-p0 But then restricted to one of the major wind directions (n, nw, w, etc) 
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p2"></param>
		/// <returns>Direction that is 90 degrees counterclockwise from p2-p0</returns>
		public static dPoint dorth_infty(dPoint p0, dPoint p2)
		{
			var r = new dPoint();

			r.y = MathUtil.Sign(p2.x - p0.x);
			r.x = -MathUtil.Sign(p2.y - p0.y);

			return r;
		}

		/// <summary>
		/// ddenom/dpara have the property that the square of radius 1 centered
		/// at p1 intersects the line p0p2 iff |MathUtil.dpara(p0,p1,p2)| <= MathUtil.ddenom(p0,p2)
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static double ddenom(dPoint p0, dPoint p2)
		{
			var r = dorth_infty(p0, p2);
			return r.y * (p2.x - p0.x) - r.x * (p2.y - p0.y);
		}

		/// <summary>
		/// Area of the parallelogram
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns>(p1-p0)x(p2-p0), the area of the parallelogram </returns>
		public static double dpara(dPoint p0, dPoint p1, dPoint p2)
		{
			double x1, y1, x2, y2;

			x1 = p1.x - p0.x;
			y1 = p1.y - p0.y;
			x2 = p2.x - p0.x;
			y2 = p2.y - p0.y;

			return x1 * y2 - x2 * y1;
		}

		/// <summary>
		/// Calculates (p1-p0)x(p3-p2) 
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <returns></returns>
		public static double cprod(dPoint p0, dPoint p1, dPoint p2, dPoint p3)
		{
			double x1, y1, x2, y2;

			x1 = p1.x - p0.x;
			y1 = p1.y - p0.y;
			x2 = p3.x - p2.x;
			y2 = p3.y - p2.y;

			return x1 * y2 - x2 * y1;
		}

		/// <summary>
		/// Calculates (p1-p0)*(p2-p0
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static double iprod(dPoint p0, dPoint p1, dPoint p2)
		{
			double x1, y1, x2, y2;

			x1 = p1.x - p0.x;
			y1 = p1.y - p0.y;
			x2 = p2.x - p0.x;
			y2 = p2.y - p0.y;

			return x1 * x2 + y1 * y2;
		}

		/// <summary>
		/// Calculates (p1-p0)*(p3-p2) 
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <returns></returns>
		public static double iprod(dPoint p0, dPoint p1, dPoint p2, dPoint p3)
		{
			double x1, y1, x2, y2;

			x1 = p1.x - p0.x;
			y1 = p1.y - p0.y;
			x2 = p3.x - p2.x;
			y2 = p3.y - p2.y;

			return x1 * x2 + y1 * y2;
		}

		/// <summary>
		/// Calculate distance between two points
		/// </summary>
		/// <param name="p"></param>
		/// <param name="q"></param>
		/// <returns></returns>
		public static double Distance(dPoint p, dPoint q)
		{
			return Math.Sqrt((p.x - q.x) * (p.x - q.x) + (p.y - q.y) * (p.y - q.y));
		}


		/// <summary>
		/// Calculates p1 x p2 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static double Product(dPoint p1, dPoint p2)
		{
			return p1.x * p2.y - p1.y * p2.x;
		}

		/// <summary>
		/// Calculates p1 x p2 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static int Product(Point p1, Point p2)
		{
			return p1.X * p2.Y - p1.Y * p2.X;
		}


		/// <summary>
		/// return 1 if a <= b < c < a, in a cyclic sense (MathUtil.mod n)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static bool Cyclic(double a, double b, double c)
		{
			if (a <= c)
			{
				return (a <= b && b < c);
			}
			else
			{
				return (a <= b || b < c);
			}
		}

		/// <summary>
		/// Sign 
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static int Sign(double i)
		{
			return i > 0 ? 1 : i < 0 ? -1 : 0;
		}

		/// <summary>
		/// Apply quadratic form Q to vector w = (w.x,w.y) 
		/// </summary>
		/// <param name="Q"></param>
		/// <param name="w"></param>
		/// <returns></returns>
		public static double quadform(Quad Q, dPoint w)
		{
			double sum = 0;
			double[] v = new double[3];
			v[0] = w.x;
			v[1] = w.y;
			v[2] = 1;
			sum = 0.0;

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					sum += v[i] * Q.At(i, j) * v[j];
				}
			}
			return sum;
		}

		/// <summary>
		/// Calculates point of a bezier curve
		/// </summary>
		/// <param name="t"></param>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <returns></returns>
		public static dPoint Bezier(double t, dPoint p0, dPoint p1, dPoint p2, dPoint p3)
		{
			double s = 1 - t;
			dPoint res = new dPoint();
			/* 
			* Note: 
			* A good optimizing compiler (such as gcc-3) reduces the 
			* following to 16 multiplications, using common subexpression elimination. 
			*/
			res.x = s * s * s * p0.x + 3 * (s * s * t) * p1.x + 3 * (t * t * s) * p2.x + t * t * t * p3.x;
			res.y = s * s * s * p0.y + 3 * (s * s * t) * p1.y + 3 * (t * t * s) * p2.y + t * t * t * p3.y;
			return res;
		}

		/// <summary>
		/// Calculates the point t in [0..1] on the (convex) bezier curve
		/// (p0,p1,p2,p3) which is MathUtil.tangent to q1-q0. Return -1.0 if there is no solution in [0..1].
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="q0"></param>
		/// <param name="q1"></param>
		/// <returns></returns>
		public static double Tangent(dPoint p0, dPoint p1, dPoint p2, dPoint p3, dPoint q0, dPoint q1)
		{
			double A, B, C;               /* (1-t)^2 A + 2(1-t)t B + t^2 C = 0 */

			double a, b, c;   /* a t^2 + b t + c = 0 */
			double d, s, r1, r2;
			A = MathUtil.cprod(p0, p1, q0, q1);
			B = MathUtil.cprod(p1, p2, q0, q1);
			C = MathUtil.cprod(p2, p3, q0, q1);

			a = A - 2 * B + C;
			b = -2 * A + 2 * B;
			c = A;

			d = b * b - 4 * a * c;

			if (a == 0 || d < 0)
			{
				return -1.0;
			}

			s = Math.Sqrt(d);

			r1 = (-b + s) / (2 * a);
			r2 = (-b - s) / (2 * a);

			if (r1 >= 0 && r1 <= 1)
			{
				return r1;
			}
			else if (r2 >= 0 && r2 <= 1)
			{
				return r2;
			}
			else
			{
				return -1.0;
			}
		}


		/// <summary>
		/// Determine the center and slope of the line i..j. Assume i<j. Needs "sum" components of p to be set
		/// </summary>
		/// <param name="path"></param>
		/// <param name="i"></param>
		/// <param name="j"></param>
		/// <param name="ctr"></param>
		/// <param name="dir"></param>
		public static void Slope(Path path, int i, int j, dPoint ctr, dPoint dir)
		{

			int n = path.Length;
			List<Sum> sums = path.Sums;
			double x, y, x2, xy, y2;
			double a, b, c, lambda2, l;
			int k = 0;
			/* assume i<j */
			int r = 0; /* rotations from i to j */

			while (j >= n)
			{
				j -= n;
				r += 1;
			}
			while (i >= n)
			{
				i -= n;
				r -= 1;
			}
			while (j < 0)
			{
				j += n;
				r -= 1;
			}
			while (i < 0)
			{
				i += n;
				r += 1;
			}

			x = sums[j + 1].x - sums[i].x + r * sums[n].x;
			y = sums[j + 1].y - sums[i].y + r * sums[n].y;
			x2 = sums[j + 1].x2 - sums[i].x2 + r * sums[n].x2;
			xy = sums[j + 1].xy - sums[i].xy + r * sums[n].xy;
			y2 = sums[j + 1].y2 - sums[i].y2 + r * sums[n].y2;
			k = j + 1 - i + r * n;

			ctr.x = x / k;
			ctr.y = y / k;

			a = (x2 - x * x / k) / k;
			b = (xy - x * y / k) / k;
			c = (y2 - y * y / k) / k;

			lambda2 = (a + c + Math.Sqrt((a - c) * (a - c) + 4 * b * b)) / 2;  /* larger e.value */

			a -= lambda2;
			c -= lambda2;

			if (Math.Abs(a) >= Math.Abs(c))
			{
				l = Math.Sqrt(a * a + b * b);
				if (l != 0)
				{
					dir.x = -b / l;
					dir.y = a / l;
				}
			}
			else
			{
				l = Math.Sqrt(c * c + b * b);
				if (l != 0)
				{
					dir.x = -c / l;
					dir.y = b / l;
				}
			}
			if (l == 0)
			{
				dir.x = dir.y = 0;  /* sometimes this can happen when k=4:
			      the two eigenvalues coincide */
			}
		}


		/// <summary>
		/// Absolute of the given value
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static int Abs(int a)
		{
			return ((a) > 0 ? (a) : -(a));
		}

		/// <summary>
		/// Minimum of the given values
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static int Min(int a, int b)
		{
			return ((a) < (b) ? (a) : (b));
		}

		/// <summary>
		/// Maximum of the given values
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static int Max(int a, int b)
		{
			return ((a) > (b) ? (a) : (b));
		}

		/// <summary>
		/// Square 
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static int Square(int a)
		{
			return ((a) * (a));
		}

		/// <summary>
		/// Cube
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static int Cube(int a)
		{
			return ((a) * (a) * (a));
		}

		/// <summary>
		/// Mod
		/// </summary>
		/// <param name="a"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static int Mod(int a, int n)
		{
			return a >= n ? a % n : a >= 0 ? a : n - 1 - (-1 - a) % n;
		}

		/// <summary>
		/// Floor Division
		/// </summary>
		/// <param name="a"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static int FloorDivision(int a, int n)
		{
			return a >= 0 ? a / n : -1 - (-1 - a) / n;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static double Slope(dPoint p1, dPoint p2)
		{
			return (p2.y - p1.y) / (p2.x - p1.x);
		}
	}

	class Configurations
	{
		/// <summary>
		/// Area of largest path to be ignored
		/// </summary>
		public int TurdSize = 2;

		/// <summary>
		///  Corner threshold
		/// </summary>
		public double AlphaMax = 1.0;

		/// <summary>
		///  Use curve optimization
		///  optimize the path p, replacing sequences of Bezier segments by a
		///  single segment when possible.
		/// </summary>
		public bool CurveOptimizing = true;

		/// <summary>
		/// Curve optimization tolerance
		/// </summary>
		public double OptTolerance = 0.2;

		/// <summary>
		/// Threshold
		/// </summary>
		public double Treshold = 0.5;

		/// <summary>
		/// Turn Policy
		/// </summary>
		public TurnPolicy TurnPolicy = TurnPolicy.Minority;

		/// <summary>
		/// Rotate directions: Left to Right
		/// </summary>
		public bool Clockwise = true;
	}

	internal class Potrace
	{
		/// <summary>
		/// Produces a binary Matrix with Dimensions
		/// For the threshold, we take the sum of  weighted R,g,b value. The sum of weights must be 1.
		/// The result fills the field bm;
		/// </summary>
		/// <param name="bitmap"> A Bitmap, which will be transformed to a binary Matrix</param>
		/// <returns>Returns a binary boolean Matrix </returns>
		static BinaryMatrix ConvertBitmap(Bitmap bitmap, double Treshold)
		{
			byte[] Result = new byte[bitmap.Width * bitmap.Height];
			BitmapData SourceData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			unsafe
			{
				byte* SourcePtr = (byte*)(void*)SourceData.Scan0;

				int l = Result.Length;
				for (int i = 0; i < l; i++)
				{
					//  if ((0.2126 * (double)SourcePtr[4 * i + 2] + 0.7153 * (double)SourcePtr[4 * i + 1] + 0.0721 * (double)SourcePtr[4 * i]) < Treshold*255)
					if (((double)SourcePtr[4 * i + 2] + (double)SourcePtr[4 * i + 1] + (double)SourcePtr[4 * i]) < Treshold * 255 * 3)
						Result[i] = 1;
					else
						Result[i] = 0;
				}
			}

			bitmap.UnlockBits(SourceData);
			BinaryMatrix bm = null;
			bm = new BinaryMatrix(bitmap.Width, bitmap.Height);
			bm.data = Result;
			return bm;
		}

		/// <summary>
		/// Compute a path in the binary matrix.
		/// Start path at the point (x0,x1), which must be an upper left corner
		/// of the path. Also compute the area enclosed by the path. Return a
		/// new path_t object, or NULL on error (note that a legitimate path
		/// cannot have length 0). 
		/// </summary>
		/// <param name="copy"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		static Path FindPath(BinaryMatrix original, BinaryMatrix copy, Point point, TurnPolicy turnPolicy)
		{
			Path path = new Path();
			int x = point.X;
			int y = point.Y;
			int dirx = 0;
			int diry = 1;
			int tmp = -1;

			path.Sign = original.At(point.X, point.Y) ? "+" : "-";

			while (true)
			{
				path.Points.Add(new Point(x, y));
				if (x > path.maxX)
					path.maxX = x;
				if (x < path.minX)
					path.minX = x;
				if (y > path.maxY)
					path.maxY = y;
				if (y < path.minY)
					path.minY = y;
				path.Length++;

				x += dirx;
				y += diry;
				path.Area -= x * diry;

				if (x == point.X && y == point.Y)
					break;

				var l = copy.At(x + (dirx + diry - 1) / 2, y + (diry - dirx - 1) / 2);
				var r = copy.At(x + (dirx - diry - 1) / 2, y + (diry + dirx - 1) / 2);

				if (r && !l)
				{
					if ((turnPolicy == TurnPolicy.Right) ||
					(((turnPolicy == TurnPolicy.Black) && (path.Sign == "+"))) ||
					(((turnPolicy == TurnPolicy.White) && (path.Sign == "-"))) ||
					(((turnPolicy == TurnPolicy.Majority) && (Majority(copy, x, y)))) ||
					((turnPolicy == TurnPolicy.Minority && !Majority(copy, x, y))))
					{
						tmp = dirx;
						dirx = -diry;
						diry = tmp;
					}
					else
					{
						tmp = dirx;
						dirx = diry;
						diry = -tmp;
					}
				}
				else if (r)
				{
					tmp = dirx;
					dirx = -diry;
					diry = tmp;
				}
				else if (!l)
				{
					tmp = dirx;
					dirx = diry;
					diry = -tmp;
				}
			}
			return path;
		}

		/// <summary>
		/// Searches a x and a y such that source[x,y] = 1 and source[x+1,y] 0.
		/// If this not exists, false will be returned else the result is true. 
		/// </summary>
		/// <param name="bm"></param>
		/// <param name="point"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		static bool FindNext(BinaryMatrix bm, Point point, ref Point result)
		{
			var i = bm.w * point.Y + point.X;
			while ((i < bm.Size) && (bm.data[i] != 1))
			{
				i++;
			}
			if (i >= bm.Size)
				return false;
			result = bm.Index(i);
			return true;
		}

		static void XorPath(BinaryMatrix bm, Path path)
		{
			int y1 = path.Points[0].Y,
			  len = path.Length,
			  x, y, maxX, minY, i, j;
			for (i = 1; i < len; i++)
			{
				x = path.Points[i].X;
				y = path.Points[i].Y;

				if (y != y1)
				{
					minY = y1 < y ? y1 : y;
					maxX = path.maxX;
					for (j = x; j < maxX; j++)
					{
						bm.Flip(j, minY);
					}
					y1 = y;
				}
			}
		}

		/// <summary>
		/// Decompose the given bitmap into paths. Returns a linked list of
		/// Path objects with the fields len, pt, area filled
		/// </summary>
		/// <param name="original">A binary bitmap which holds the imageinformations.</param>
		/// <param name="plistp">List of Path objects</param>
		static Path ToPathList(BinaryMatrix original, List<Path> pathlist, TurnPolicy turnPolicy, int TurdSize)
		{
			BinaryMatrix copy = original.Copy();
			Point currentPoint = new Point(0, 0);
			Path path = new Path();

			bool weiter = FindNext(copy, currentPoint, ref currentPoint);
			while (weiter)
			{
				path = FindPath(original, copy, currentPoint, turnPolicy);
				XorPath(copy, path);

				if (path.Area > TurdSize)
				{
					pathlist.Add(path);
				}
				weiter = FindNext(copy, currentPoint, ref currentPoint);
			}
			return path;
		}

		/// <summary>
		/// Stage#1
		/// Preparation: fill in the sum* fields of a path (used for later rapid summing). 
		/// </summary>
		/// <param name="path">Path for which the preparation will be done</param>
		/// <returns></returns>
		static void CalcSums(Path path)
		{
			double x, y;
			// origin 
			path.x0 = path.Points[0].X;
			path.y0 = path.Points[0].Y;


			List<Sum> s = path.Sums;
			s.Add(new Sum(0, 0, 0, 0, 0));
			for (int i = 0; i < path.Length; i++)
			{
				x = path.Points[i].X - path.x0;
				y = path.Points[i].Y - path.y0;
				s.Add(new Sum(s[i].x + x, s[i].y + y, s[i].xy + x * y, s[i].x2 + x * x, s[i].y2 + y * y));
			}
		}

		/// <summary>
		/// Stage 2: calculate the optimal polygon (Sec. 2.2.2-2.2.4). 
		/// Auxiliary function: calculate the penalty of an edge from i to j in
		/// the given path. This needs the "lon" and "sum*" data. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="i"></param>
		/// <param name="j"></param>
		/// <returns></returns>
		static double Penalty3(Path path, int i, int j)
		{

			int n = path.Length;
			List<Point> pt = path.Points;
			List<Sum> sums = path.Sums;

			double x, y, xy, x2, y2;
			double a, b, c, s,
			  px, py, ex, ey;
			int r = 0;
			int k = 0;

			if (j >= n)
			{
				j -= n;
				r = 1;
			}

			if (r == 0)
			{
				x = sums[j + 1].x - sums[i].x;
				y = sums[j + 1].y - sums[i].y;
				x2 = sums[j + 1].x2 - sums[i].x2;
				xy = sums[j + 1].xy - sums[i].xy;
				y2 = sums[j + 1].y2 - sums[i].y2;
				k = j + 1 - i;
			}
			else
			{
				x = sums[j + 1].x - sums[i].x + sums[n].x;
				y = sums[j + 1].y - sums[i].y + sums[n].y;
				x2 = sums[j + 1].x2 - sums[i].x2 + sums[n].x2;
				xy = sums[j + 1].xy - sums[i].xy + sums[n].xy;
				y2 = sums[j + 1].y2 - sums[i].y2 + sums[n].y2;
				k = j + 1 - i + n;
			}

			px = (pt[i].X + pt[j].X) / 2.0 - pt[0].X;
			py = (pt[i].Y + pt[j].Y) / 2.0 - pt[0].Y;
			ey = (pt[j].X - pt[i].X);
			ex = -(pt[j].Y - pt[i].Y);

			a = ((x2 - 2 * x * px) / k + px * px);
			b = ((xy - x * py - y * px) / k + px * py);
			c = ((y2 - 2 * y * py) / k + py * py);

			s = ex * ex * a + 2 * ex * ey * b + ey * ey * c;

			return Math.Sqrt(s);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		static void CalcLon(Path path)
		{

			int n = path.Length;
			List<Point> pt = path.Points;

			int dir;
			int[] pivk = new int[n];
			int[] nc = new int[n];

			int[] ct = new int[4];

			path.lon = new int[n];

			Point[] constraint = new Point[2];
			Point cur = new Point();
			Point off = new Point();
			Point dk = new Point();
			int foundk;

			int j, k1;
			int a, b, c, d;
			/* initialize the nc data structure. Point from each point to the
			furthest future point to which it is connected by a vertical or
			horizontal segment. We take advantage of the fact that there is
			always a direction change at 0 (due to the path decomposition
			algorithm). But even if this were not so, there is no harm, as
			in practice, correctness does not depend on the word "furthest"
			above.  */
			int k = 0;
			/* determine pivot points: for each i, let pivk[i] be the furthest k
			such that all j with i<j<k lie on a line connecting i,k. */

			for (int i = n - 1; i >= 0; i--)
			{
				if (pt[i].X != pt[k].X && pt[i].Y != pt[k].Y)
				{
					k = i + 1;
				}
				nc[i] = k;
			}

			for (int i = n - 1; i >= 0; i--)
			{
				ct[0] = ct[1] = ct[2] = ct[3] = 0;
				dir = (3 + 3 * (pt[MathUtil.Mod(i + 1, n)].X - pt[i].X) +
					(pt[MathUtil.Mod(i + 1, n)].Y - pt[i].Y)) / 2;
				ct[dir]++;

				constraint[0].X = 0;
				constraint[0].Y = 0;
				constraint[1].X = 0;
				constraint[1].Y = 0;

				k = nc[i];
				k1 = i;
				while (true)
				{
					foundk = 0;
					dir = (3 + 3 * MathUtil.Sign(pt[k].X - pt[k1].X) +
						MathUtil.Sign(pt[k].Y - pt[k1].Y)) / 2;
					ct[dir]++;

					if ((ct[0] == 1) && (ct[1] == 1) && (ct[2] == 1) && (ct[3] == 1))
					{
						pivk[i] = k1;
						foundk = 1;
						break;
					}

					cur.X = pt[k].X - pt[i].X;
					cur.Y = pt[k].Y - pt[i].Y;

					if (MathUtil.Product(constraint[0], cur) < 0 || MathUtil.Product(constraint[1], cur) > 0)
					{
						break;
					}

					if (Math.Abs(cur.X) <= 1 && Math.Abs(cur.Y) <= 1)
					{

					}
					else
					{
						off.X = cur.X + ((cur.Y >= 0 && (cur.Y > 0 || cur.X < 0)) ? 1 : -1);
						off.Y = cur.Y + ((cur.X <= 0 && (cur.X < 0 || cur.Y < 0)) ? 1 : -1);
						if (MathUtil.Product(constraint[0], off) >= 0)
						{
							constraint[0].X = off.X;
							constraint[0].Y = off.Y;
						}
						off.X = cur.X + ((cur.Y <= 0 && (cur.Y < 0 || cur.X < 0)) ? 1 : -1);
						off.Y = cur.Y + ((cur.X >= 0 && (cur.X > 0 || cur.Y < 0)) ? 1 : -1);
						if (MathUtil.Product(constraint[1], off) <= 0)
						{
							constraint[1].X = off.X;
							constraint[1].Y = off.Y;
						}
					}
					k1 = k;
					k = nc[k1];
					if (!MathUtil.Cyclic(k, i, k1))
					{
						break;
					}
				}
				if (foundk == 0)
				{
					dk.X = MathUtil.Sign(pt[k].X - pt[k1].X);
					dk.Y = MathUtil.Sign(pt[k].Y - pt[k1].Y);
					cur.X = pt[k1].X - pt[i].X;
					cur.Y = pt[k1].Y - pt[i].Y;

					a = MathUtil.Product(constraint[0], cur);
					b = MathUtil.Product(constraint[0], dk);
					c = MathUtil.Product(constraint[1], cur);
					d = MathUtil.Product(constraint[1], dk);

					j = 10000000;
					if (b < 0)
					{
						j = a / -b;
					}
					if (d > 0)
					{
						j = Math.Min(j, -c / d);
					}
					pivk[i] = MathUtil.Mod(k1 + j, n);
				}
			}

			j = pivk[n - 1];
			path.lon[n - 1] = j;
			for (int i = n - 2; i >= 0; i--)
			{
				if (MathUtil.Cyclic(i + 1, pivk[i], j))
				{
					j = pivk[i];
				}
				path.lon[i] = j;
			}

			for (int i = n - 1; MathUtil.Cyclic(MathUtil.Mod(i + 1, n), j, path.lon[i]); i--)
			{
				path.lon[i] = j;
			}
		}

		/// <summary>
		/// Finds the optimal polygon. Fill in the m and po components. Return 1
		/// on failure with errno set, else 0. Non-cyclic version: assumes i=0
		/// is in the polygon. Fixme: ### implement cyclic version.
		/// </summary>
		/// <param name="path"></param>
		static void BestPolygon(Path path)
		{

			double thispen, best;
			int i, j, m, k;
			int n = path.Length;
			int c;
			int[] clip0 = new int[n];
			double[] pen = new double[n + 1];
			int[] prev = new int[n + 1];
			int[] clip1 = new int[n + 1];
			int[] seg0 = new int[n + 1];
			int[] seg1 = new int[n + 1];



			for (i = 0; i < n; i++)
			{
				c = MathUtil.Mod(path.lon[MathUtil.Mod(i - 1, n)] - 1, n);
				if (c == i)
				{
					c = MathUtil.Mod(i + 1, n);
				}
				if (c < i)
				{
					clip0[i] = n;
				}
				else
				{
					clip0[i] = c;
				}
			}

			j = 1;
			for (i = 0; i < n; i++)
			{
				while (j <= clip0[i])
				{
					clip1[j] = i;
					j++;
				}
			}

			i = 0;
			for (j = 0; i < n; j++)
			{
				seg0[j] = i;
				i = clip0[i];
			}
			seg0[j] = n;
			m = j;

			i = n;
			for (j = m; j > 0; j--)
			{
				seg1[j] = i;
				i = clip1[i];
			}
			seg1[0] = 0;

			pen[0] = 0;
			/* now find the shortest path with m segments, based on penalty3 */
			/* note: the outer 2 loops jointly have at most n interations, thus
			the worst-case behavior here is quadratic. In practice, it is
			close to linear since the inner loop tends to be short. */
			for (j = 1; j <= m; j++)
			{
				for (i = seg1[j]; i <= seg0[j]; i++)
				{
					best = -1;
					for (k = seg0[j - 1]; k >= clip1[i]; k--)
					{
						thispen = Penalty3(path, k, i) + pen[k];
						if (best < 0 || thispen < best)
						{
							prev[i] = k;
							best = thispen;
						}
					}
					pen[i] = best;
				}
			}
			path.m = m;
			path.po = new int[m];
			/* read off shortest path */
			for (i = n, j = m - 1; i > 0; j--)
			{
				i = prev[i];
				path.po[j] = i;
			}
		}

		/// <summary>
		/// Stage 3: vertex adjustment (Sec. 2.3.1).
		/// Adjust vertices of optimal polygon: calculate the intersection of
		/// the two "optimal" line segments, then move it into the unit square
		/// if it lies outside. Return 1 with errno set on error; 0 on success. 
		/// Calculate "optimal" point-slope representation for each line segment 
		/// </summary>
		/// <param name="path"></param>
		static void AdjustVertices(Path path)
		{


			int m = path.m;
			int[] po = path.po;
			int n = path.Length;
			List<Point> pt = path.Points;
			double x0 = path.x0;
			double y0 = path.y0;
			dPoint[] ctr = new dPoint[m];
			dPoint[] dir = new dPoint[m];
			Quad[] q = new Quad[m];
			int i, j, k, l;
			double[] v = new double[3];

			dPoint s = new dPoint();
			double d;
			path.Curve = new PrimitiveCurve(m);
			/* calculate "optimal" point-slope representation for each line
					segment */
			for (i = 0; i < m; i++)
			{
				j = po[MathUtil.Mod(i + 1, m)];
				j = MathUtil.Mod(j - po[i], n) + po[i];
				ctr[i] = new dPoint();
				dir[i] = new dPoint();
				MathUtil.Slope(path, po[i], j, ctr[i], dir[i]);
			}
			/* represent each line segment as a singular quadratic form; the
						 distance of a point (x,y) from the line segment will be
						 (x,y,1)Q(x,y,1)^t, where Q=q[i]. */
			for (i = 0; i < m; i++)
			{
				q[i] = new Quad();
				d = dir[i].x * dir[i].x + dir[i].y * dir[i].y;
				if (d == 0.0)
				{
					for (j = 0; j < 3; j++)
					{
						for (k = 0; k < 3; k++)
						{
							q[i].Data[j * 3 + k] = 0;
						}
					}
				}
				else
				{
					v[0] = dir[i].y;
					v[1] = -dir[i].x;
					v[2] = -v[1] * ctr[i].y - v[0] * ctr[i].x;
					for (l = 0; l < 3; l++)
					{
						for (k = 0; k < 3; k++)
						{
							q[i].Data[l * 3 + k] = v[l] * v[k] / d;
						}
					}
				}
			}

			double dx, dy, det;
			int z;
			double xmin, ymin; /* coordinates of minimum */
			double min, cand; /* minimum and candidate for minimum of quad. form */
			/* now calculate the "intersections" of consecutive segments.
			   Instead of using the actual intersection, we find the point
			   within a given unit square which minimizes the square distance to
			   the two lines. */
			for (i = 0; i < m; i++)
			{
				Quad Q = new Quad();
				dPoint w = new dPoint();
				/* let s be the vertex, in coordinates relative to x0/y0 */
				s.x = pt[po[i]].X - x0;
				s.y = pt[po[i]].Y - y0;
				/* intersect segments i-1 and i */
				j = MathUtil.Mod(i - 1, m);
				/* add quadratic forms */
				for (l = 0; l < 3; l++)
				{
					for (k = 0; k < 3; k++)
					{
						Q.Data[l * 3 + k] = q[j].At(l, k) + q[i].At(l, k);
					}
				}

				while (true)
				{
					/* minimize the quadratic form Q on the unit square */
					/* find intersection */
					det = Q.At(0, 0) * Q.At(1, 1) - Q.At(0, 1) * Q.At(1, 0);
					if (det != 0.0)
					{
						w.x = (-Q.At(0, 2) * Q.At(1, 1) + Q.At(1, 2) * Q.At(0, 1)) / det;
						w.y = (Q.At(0, 2) * Q.At(1, 0) - Q.At(1, 2) * Q.At(0, 0)) / det;
						break;
					}
					/* matrix is singular - lines are parallel. Add another,
								  orthogonal axis, through the center of the unit square */
					if (Q.At(0, 0) > Q.At(1, 1))
					{
						v[0] = -Q.At(0, 1);
						v[1] = Q.At(0, 0);
					}
					else if (Q.At(1, 1) != 0.0)
					{
						v[0] = -Q.At(1, 1);
						v[1] = Q.At(1, 0);
					}
					else
					{
						v[0] = 1;
						v[1] = 0;
					}
					d = v[0] * v[0] + v[1] * v[1];
					v[2] = -v[1] * s.y - v[0] * s.x;
					for (l = 0; l < 3; l++)
					{
						for (k = 0; k < 3; k++)
						{
							Q.Data[l * 3 + k] += v[l] * v[k] / d;
						}
					}
				}
				dx = Math.Abs(w.x - s.x);
				dy = Math.Abs(w.y - s.y);
				if (dx <= 0.5 && dy <= 0.5)
				{

					path.Curve.Vertex[i] = new dPoint(w.x + x0, w.y + y0);
					continue;
				}
				/* the minimum was not in the unit square; now minimize quadratic
				   on boundary of square */
				min = MathUtil.quadform(Q, s);
				xmin = s.x;
				ymin = s.y;

				if (Q.At(0, 0) != 0.0)
				{
					for (z = 0; z < 2; z++)
					{
						/* value of the y-coordinate */
						w.y = s.y - 0.5 + z;
						w.x = -(Q.At(0, 1) * w.y + Q.At(0, 2)) / Q.At(0, 0);
						dx = Math.Abs(w.x - s.x);
						cand = MathUtil.quadform(Q, w);
						if (dx <= 0.5 && cand < min)
						{
							min = cand;
							xmin = w.x;
							ymin = w.y;
						}
					}
				}

				if (Q.At(1, 1) != 0.0)
				{
					for (z = 0; z < 2; z++)
					{
						/* value of the x-coordinate */
						w.x = s.x - 0.5 + z;
						w.y = -(Q.At(1, 0) * w.x + Q.At(1, 2)) / Q.At(1, 1);
						dy = Math.Abs(w.y - s.y);
						cand = MathUtil.quadform(Q, w);
						if (dy <= 0.5 && cand < min)
						{
							min = cand;
							xmin = w.x;
							ymin = w.y;
						}
					}
				}
				/* check four corners */
				for (l = 0; l < 2; l++)
				{
					for (k = 0; k < 2; k++)
					{
						w.x = s.x - 0.5 + l;
						w.y = s.y - 0.5 + k;
						cand = MathUtil.quadform(Q, w);
						if (cand < min)
						{
							min = cand;
							xmin = w.x;
							ymin = w.y;
						}
					}
				}

				path.Curve.Vertex[i] = new dPoint(xmin + x0, ymin + y0);
			}
		}

		/// <summary>
		/// Stage 4: smoothing and corner analysis (Sec. 2.3.3) 
		/// </summary>
		/// <param name="path"></param>
		static void Reverse(Path path)
		{
			PrimitiveCurve curve = path.Curve;
			int m = curve.N;
			dPoint[] v = curve.Vertex;
			int i, j;
			dPoint tmp;

			for (i = 0, j = m - 1; i < j; i++, j--)
			{
				tmp = v[i];
				v[i] = v[j];
				v[j] = tmp;
			}

		}

		/// <summary>
		/// Always succeeds and returns 0
		/// </summary>
		/// <param name="path"></param>
		static void Smooth(Path path, double AlphaMax, bool Clockwise)
		{
			int m = path.Curve.N;
			PrimitiveCurve curve = path.Curve;

			if ((Clockwise && path.Sign == "-") || (!Clockwise && path.Sign == "+"))
			{
				Reverse(path);
			}

			int i, j, k;
			double dd, denom, alpha;
			dPoint p2, p3, p4;
			/* examine each vertex and find its best fit */
			for (i = 0; i < m; i++)
			{
				j = MathUtil.Mod(i + 1, m);
				k = MathUtil.Mod(i + 2, m);
				p4 = MathUtil.Interval(1 / 2.0, curve.Vertex[k], curve.Vertex[j]);

				denom = MathUtil.ddenom(curve.Vertex[i], curve.Vertex[k]);
				if (denom != 0.0)
				{
					dd = MathUtil.dpara(curve.Vertex[i], curve.Vertex[j], curve.Vertex[k]) / denom;
					dd = Math.Abs(dd);
					alpha = dd > 1 ? (1 - 1.0 / dd) : 0;
					alpha = alpha / 0.75;
				}
				else
				{
					alpha = 4 / 3.0;
				}
				curve.Alpha0[j] = alpha;   /* remember "original" value of alpha */

				if (alpha >= AlphaMax)
				{
					curve.Tag[j] = Tags.Corner;
					curve.C[3 * j + 1] = curve.Vertex[j];
					curve.C[3 * j + 2] = p4;
				}
				else
				{
					if (alpha < 0.55)
					{
						alpha = 0.55;
					}
					else if (alpha > 1)
					{
						alpha = 1;
					}
					p2 = MathUtil.Interval(0.5 + 0.5 * alpha, curve.Vertex[i], curve.Vertex[j]);
					p3 = MathUtil.Interval(0.5 + 0.5 * alpha, curve.Vertex[k], curve.Vertex[j]);
					curve.Tag[j] = Tags.CurveTo;
					curve.C[3 * j + 0] = p2;
					curve.C[3 * j + 1] = p3;
					curve.C[3 * j + 2] = p4;
				}
				curve.Alpha[j] = alpha;  /* store the "cropped" value of alpha */
				curve.Beta[j] = 0.5;
			}
			curve.AlphaCurve = 1;
		}

		/// <summary>
		/// Stage 5: Curve optimization (Sec. 2.4)
		/// calculate best fit from i+.5 to j+.5.  Assume i<j (cyclically).
		/// Return 0 and set badness and parameters (alpha, beta), if
		/// possible. Return 1 if impossible. 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="i"></param>
		/// <param name="j"></param>
		/// <param name="res"></param>
		/// <param name="opttolerance"></param>
		/// <param name="convc"></param>
		/// <param name="areac"></param>
		/// <returns></returns>
		static int OptimizationPenatly(Path path, int i, int j, Optimization res, double optTolerance, int[] convc, double[] areac)
		{
			int m = path.Curve.N;
			PrimitiveCurve curve = path.Curve;
			dPoint[] vertex = curve.Vertex;
			int k, k1, k2, conv, i1;
			double area, alpha, d, d1, d2;
			dPoint p0, p1, p2, p3, pt;
			double A, R, A1, A2, A3, A4,
			  s, t;
			/* check convexity, corner-freeness, and maximum bend < 179 degrees */
			if (i == j)
			{ /* sanity - a full loop can never be an opticurve */
				return 1;
			}

			k = i;
			i1 = MathUtil.Mod(i + 1, m);
			k1 = MathUtil.Mod(k + 1, m);
			conv = convc[k1];
			if (conv == 0)
			{
				return 1;
			}
			d = MathUtil.Distance(vertex[i], vertex[i1]);
			for (k = k1; k != j; k = k1)
			{
				k1 = MathUtil.Mod(k + 1, m);
				k2 = MathUtil.Mod(k + 2, m);
				if (convc[k1] != conv)
				{
					return 1;
				}
				if (MathUtil.Sign(MathUtil.cprod(vertex[i], vertex[i1], vertex[k1], vertex[k2])) !=
					conv)
				{
					return 1;
				}
				if (MathUtil.iprod(vertex[i], vertex[i1], vertex[k1], vertex[k2]) <
					d * MathUtil.Distance(vertex[k1], vertex[k2]) * -0.999847695156)
				{
					return 1;
				}
			}
			/* the curve we're working in: */
			p0 = curve.C[MathUtil.Mod(i, m) * 3 + 2].Copy();
			p1 = vertex[MathUtil.Mod(i + 1, m)].Copy();
			p2 = vertex[MathUtil.Mod(j, m)].Copy();
			p3 = curve.C[MathUtil.Mod(j, m) * 3 + 2].Copy();
			/* determine its area */
			area = areac[j] - areac[i];
			area -= MathUtil.dpara(vertex[0], curve.C[i * 3 + 2], curve.C[j * 3 + 2]) / 2;
			if (i >= j)
			{
				area += areac[m];
			}
			/* find intersection o of p0p1 and p2p3. Let t,s such that o =
					 MathUtil.interval(t,p0,p1) = MathUtil.interval(s,p3,p2). Let A be the area of the
					 triangle (p0,o,p3). */
			A1 = MathUtil.dpara(p0, p1, p2);
			A2 = MathUtil.dpara(p0, p1, p3);
			A3 = MathUtil.dpara(p0, p2, p3);

			A4 = A1 + A3 - A2;

			if (A2 == A1)
			{/* this should never happen */
				return 1;
			}

			t = A3 / (A3 - A4);
			s = A2 / (A2 - A1);
			A = A2 * t / 2.0;

			if (A == 0.0)
			{
				/* this should never happen */
				return 1;
			}

			R = area / A; /* relative area */
			alpha = 2 - Math.Sqrt(4 - R / 0.3); /* overall alpha for p0-o-p3 curve */

			res.C[0] = MathUtil.Interval(t * alpha, p0, p1);
			res.C[1] = MathUtil.Interval(s * alpha, p3, p2);
			res.Alpha = alpha;
			res.T = t;
			res.S = s;

			p1 = res.C[0].Copy();
			p2 = res.C[1].Copy(); /* the proposed curve is now (p0,p1,p2,p3) */

			res.Pen = 0;
			/* calculate penalty */
			/* check tangency with edges */
			for (k = MathUtil.Mod(i + 1, m); k != j; k = k1)
			{
				k1 = MathUtil.Mod(k + 1, m);
				t = MathUtil.Tangent(p0, p1, p2, p3, vertex[k], vertex[k1]);
				if (t < -0.5)
				{
					return 1;
				}
				pt = MathUtil.Bezier(t, p0, p1, p2, p3);
				d = MathUtil.Distance(vertex[k], vertex[k1]);
				if (d == 0.0)
				{
					/* this should never happen */
					return 1;
				}
				d1 = MathUtil.dpara(vertex[k], vertex[k1], pt) / d;
				if (Math.Abs(d1) > optTolerance)
				{
					return 1;
				}
				if (MathUtil.iprod(vertex[k], vertex[k1], pt) < 0 ||
					MathUtil.iprod(vertex[k1], vertex[k], pt) < 0)
				{
					return 1;
				}
				res.Pen += d1 * d1;
			}
			/* check corners */
			for (k = i; k != j; k = k1)
			{
				k1 = MathUtil.Mod(k + 1, m);
				t = MathUtil.Tangent(p0, p1, p2, p3, curve.C[k * 3 + 2], curve.C[k1 * 3 + 2]);
				if (t < -0.5)
				{
					return 1;
				}
				pt = MathUtil.Bezier(t, p0, p1, p2, p3);
				d = MathUtil.Distance(curve.C[k * 3 + 2], curve.C[k1 * 3 + 2]);
				if (d == 0.0)
				{
					/* this should never happen */
					return 1;
				}
				d1 = MathUtil.dpara(curve.C[k * 3 + 2], curve.C[k1 * 3 + 2], pt) / d;
				d2 = MathUtil.dpara(curve.C[k * 3 + 2], curve.C[k1 * 3 + 2], vertex[k1]) / d;
				d2 *= 0.75 * curve.Alpha[k1];
				if (d2 < 0)
				{
					d1 = -d1;
					d2 = -d2;
				}
				if (d1 < d2 - optTolerance)
				{
					return 1;
				}
				if (d1 < d2)
				{
					res.Pen += (d1 - d2) * (d1 - d2);
				}
			}

			return 0;
		}

		/// <summary>
		/// Optimize the path p, replacing sequences of Bezier segments by a
		/// single segment when possible. Return 0 on success, 1 with errno set on failure.
		/// </summary>
		/// <param name="path"></param>
		static void OptimizedCurve(Path path, double optTolerance)
		{
			PrimitiveCurve curve = path.Curve;
			int m = curve.N;
			dPoint[] vert = curve.Vertex;
			int[] pt = new int[m + 1];
			double[] pen = new double[m + 1];
			int[] len = new int[m + 1];
			Optimization[] opt = new Optimization[m + 1];
			Optimization o = new Optimization();
			int om, i, j, r;
			dPoint p0;
			int i1;
			double area, alpha;
			PrimitiveCurve ocurve;
			int[] convc = new int[m];  /* conv[m]: pre-computed convexities */
			double[] areac = new double[m + 1];
			/* pre-calculate convexity: +1 = right turn, -1 = left turn, 0 = corner */
			for (i = 0; i < m; i++)
			{
				if (curve.Tag[i] == Tags.CurveTo)
				{
					convc[i] = MathUtil.Sign(MathUtil.dpara(vert[MathUtil.Mod(i - 1, m)], vert[i], vert[MathUtil.Mod(i + 1, m)]));
				}
				else
				{
					convc[i] = 0;
				}
			}
			/* pre-calculate areas */
			area = 0.0;
			areac[0] = 0.0;
			p0 = curve.Vertex[0];
			for (i = 0; i < m; i++)
			{
				i1 = MathUtil.Mod(i + 1, m);
				if (curve.Tag[i1] == Tags.CurveTo)
				{
					alpha = curve.Alpha[i1];
					area += 0.3 * alpha * (4 - alpha) *
						MathUtil.dpara(curve.C[i * 3 + 2], vert[i1], curve.C[i1 * 3 + 2]) / 2;
					area += MathUtil.dpara(p0, curve.C[i * 3 + 2], curve.C[i1 * 3 + 2]) / 2;
				}
				areac[i + 1] = area;
			}

			pt[0] = -1;
			pen[0] = 0;
			len[0] = 0;

			/* Fixme: we always start from a fixed point -- should find the best
					  curve cyclically ### */
			for (j = 1; j <= m; j++)
			{
				/* calculate best path from 0 to j */
				pt[j] = j - 1;
				pen[j] = pen[j - 1];
				len[j] = len[j - 1] + 1;

				for (i = j - 2; i >= 0; i--)
				{
					r = OptimizationPenatly(path, i, MathUtil.Mod(j, m), o, optTolerance, convc, areac);
					if (r == 1)
					{
						break;
					}
					if (len[j] > len[i] + 1 ||
						(len[j] == len[i] + 1 && pen[j] > pen[i] + o.Pen))
					{
						pt[j] = i;
						pen[j] = pen[i] + o.Pen;
						len[j] = len[i] + 1;
						opt[j] = o;
						o = new Optimization();
					}
				}
			}
			om = len[m];
			ocurve = new PrimitiveCurve(om);
			double[] s = new double[om];
			double[] t = new double[om];

			j = m;
			for (i = om - 1; i >= 0; i--)
			{
				if (pt[j] == j - 1)
				{
					ocurve.Tag[i] = curve.Tag[MathUtil.Mod(j, m)];
					ocurve.C[i * 3 + 0] = curve.C[MathUtil.Mod(j, m) * 3 + 0];
					ocurve.C[i * 3 + 1] = curve.C[MathUtil.Mod(j, m) * 3 + 1];
					ocurve.C[i * 3 + 2] = curve.C[MathUtil.Mod(j, m) * 3 + 2];
					ocurve.Vertex[i] = curve.Vertex[MathUtil.Mod(j, m)];
					ocurve.Alpha[i] = curve.Alpha[MathUtil.Mod(j, m)];
					ocurve.Alpha0[i] = curve.Alpha0[MathUtil.Mod(j, m)];
					ocurve.Beta[i] = curve.Beta[MathUtil.Mod(j, m)];
					s[i] = t[i] = 1.0;
				}
				else
				{
					ocurve.Tag[i] = Tags.CurveTo;
					ocurve.C[i * 3 + 0] = opt[j].C[0];
					ocurve.C[i * 3 + 1] = opt[j].C[1];
					ocurve.C[i * 3 + 2] = curve.C[MathUtil.Mod(j, m) * 3 + 2];
					ocurve.Vertex[i] = MathUtil.Interval(opt[j].S, curve.C[MathUtil.Mod(j, m) * 3 + 2],
												 vert[MathUtil.Mod(j, m)]);
					ocurve.Alpha[i] = opt[j].Alpha;
					ocurve.Alpha0[i] = opt[j].Alpha;
					s[i] = opt[j].S;
					t[i] = opt[j].T;
				}
				j = pt[j];
			}
			/* calculate beta parameters */
			for (i = 0; i < om; i++)
			{
				i1 = MathUtil.Mod(i + 1, om);
				ocurve.Beta[i] = s[i] / (s[i] + t[i1]);
			}
			ocurve.AlphaCurve = 1;
			path.Curve = ocurve;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bm"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		static bool Majority(BinaryMatrix bm, int x, int y)
		{
			int i;
			int a;
			int ct;
			for (i = 2; i < 5; i++)
			{
				ct = 0;
				for (a = -i + 1; a <= i - 1; a++)
				{
					ct += bm.At(x + a, y + i - 1) ? 1 : -1;
					ct += bm.At(x + i - 1, y + a - 1) ? 1 : -1;
					ct += bm.At(x + a - 1, y - i) ? 1 : -1;
					ct += bm.At(x - i, y + a) ? 1 : -1;
				}
				if (ct > 0)
				{
					return true;
				}
				else if (ct < 0)
				{
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ListOfPathes"></param>
		static void TraceToList(List<List<Curve>> ListOfPathes, List<Path> pathlist)
		{
			if (ListOfPathes == null)
			{
				return;
			}

			for (int i = 0; i < pathlist.Count; i++)
			{
				Path P = pathlist[i];
				List<Curve> CurveList = new List<Curve>();
				ListOfPathes.Add(CurveList);
				dPoint LastPoint = P.Curve.C[(P.Curve.N - 1) * 3 + 2];
				for (int j = 0; j < P.Curve.N; j++)
				{
					if (P.Curve.Tag[j] == Tags.Corner)
					{
						Curve C = new Curve(CurveKind.Line, P.Curve.C[j * 3 + 1], P.Curve.C[j * 3 + 1], P.Curve.C[j * 3 + 2], P.Curve.C[j * 3 + 2]);
						CurveList.Add(C);
					}
					else
					{
						Curve C = new Curve(CurveKind.Bezier, LastPoint, P.Curve.C[j * 3], P.Curve.C[j * 3 + 1], P.Curve.C[j * 3 + 2]);
						CurveList.Add(C);
					}
					LastPoint = P.Curve.C[j * 3 + 2];
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Bitmap"></param>
		/// <param name="ListOfCurveArrays"></param>
		internal static void Trace(Bitmap Bitmap, List<List<Curve>> ListOfCurveArrays, List<Path> pathList, Configurations config)
		{
			BinaryMatrix bm = ConvertBitmap(Bitmap, config.Treshold);
			Path Path = ToPathList(bm, pathList, config.TurnPolicy, config.TurdSize);
			for (int i = 0; i < pathList.Count; i++)
			{
				CalcSums(pathList[i]);
				CalcLon(pathList[i]);
				BestPolygon(pathList[i]);
				AdjustVertices(pathList[i]);
				Smooth(pathList[i], config.AlphaMax, config.Clockwise);
				if (config.CurveOptimizing)
				{
					OptimizedCurve(pathList[i], config.OptTolerance);
				}
			}
			TraceToList(ListOfCurveArrays, pathList);
		}
	}

	public class PotraceWrapper
	{
		static decimal radius = 1;

		/// <summary>
		/// Show Points on SVG
		/// </summary>
		internal static bool ShowPoints = false;

		/// <summary>
		/// Use Cubic Curves for SVG rendering
		/// </summary>
		static bool UseCubicCurves = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		static string ToString(double value)
		{
			return string.Format(System.Globalization.CultureInfo.GetCultureInfo("en-US"), "{0:0.0}", value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="i"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string Segment(PrimitiveCurve curve, int i, double size)
		{
			var s = string.Format("\nL {0},{1} {2},{3} ",
					ToString(curve.C[i * 3 + 1].x * size),
					ToString(curve.C[i * 3 + 1].y * size),
					ToString(curve.C[i * 3 + 2].x * size),
					ToString(curve.C[i * 3 + 2].y * size));
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="i"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string BezierPath(PrimitiveCurve curve, int i, double size)
		{
			var b = "";
			if (UseCubicCurves)
			{
				var mx = (curve.C[i * 3 + 0].x + curve.C[i * 3 + 1].x) / 2;
				var my = (curve.C[i * 3 + 0].y + curve.C[i * 3 + 1].y) / 2;

				b = string.Format("\r\nQ {0},{1} {2},{3} ",
					ToString(mx * size),
					ToString(my * size),
					ToString(curve.C[i * 3 + 2].x * size),
					ToString(curve.C[i * 3 + 2].y * size));
			}
			else
			{
				b = string.Format("\r\nC {0},{1} {2},{3} {4},{5} ",
					ToString(curve.C[i * 3 + 0].x * size),
					ToString(curve.C[i * 3 + 0].y * size),
					ToString(curve.C[i * 3 + 1].x * size),
					ToString(curve.C[i * 3 + 1].y * size),
					ToString(curve.C[i * 3 + 2].x * size),
					ToString(curve.C[i * 3 + 2].y * size));
			}
			return b;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string Path(PrimitiveCurve curve, double size)
		{
			int n = curve.N, i;

			var p = string.Format("\r\nM {0},{1} ",
					ToString(curve.C[(n - 1) * 3 + 2].x * size),
					ToString(curve.C[(n - 1) * 3 + 2].y * size));

			for (i = 0; i < n; i++)
			{
				if (curve.Tag[i] == Tags.CurveTo)
				{
					p += BezierPath(curve, i, size);
				}
				else if (curve.Tag[i] == Tags.Corner)
				{
					p += Segment(curve, i, size);
				}
			}
			return p;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string Points(PrimitiveCurve curve, double size)
		{
			int n = curve.N, i;
			radius = 1;
			var p = "";
			for (i = 0; i < n; i++)
			{
				if (curve.Tag[i] == Tags.CurveTo)
				{
					p += BezierPoints(curve, i, size);
				}
				else if (curve.Tag[i] == Tags.Corner)
				{
					p += SegmentPoints(curve, i, size);
				}
			}
			radius = 1;
			p = p + ToCircle(curve.C[(n - 1) * 3 + 2].x * size, curve.C[(n - 1) * 3 + 2].y * size, "blue");
			return p;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="i"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string SegmentPoints(PrimitiveCurve curve, int i, double size)
		{
			var s = "";
			s += ToCircle(curve.C[i * 3 + 1].x * size, curve.C[i * 3 + 1].y * size);
			s += ToCircle(curve.C[i * 3 + 2].x * size, curve.C[i * 3 + 2].y * size);
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curve"></param>
		/// <param name="i"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		static string BezierPoints(PrimitiveCurve curve, int i, double size)
		{
			var b = "";
			if (UseCubicCurves)
			{
				var mx = (curve.C[i * 3 + 0].x + curve.C[i * 3 + 1].x) / 2;
				var my = (curve.C[i * 3 + 0].y + curve.C[i * 3 + 1].y) / 2;

				b += ToCircle(mx * size, my * size);
				b += ToCircle(curve.C[i * 3 + 2].x * size, curve.C[i * 3 + 2].y * size);
			}
			else
			{
				b += ToCircle(curve.C[i * 3 + 0].x * size, curve.C[i * 3 + 0].y * size);
				b += ToCircle(curve.C[i * 3 + 1].x * size, curve.C[i * 3 + 1].y * size);
				b += ToCircle(curve.C[i * 3 + 2].x * size, curve.C[i * 3 + 2].y * size);
			}
			return b;
		}

		/// <summary>
		/// Draw SVG Element of a Circle
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		static string ToCircle(double x, double y)
		{
			return ToCircle(x, y, "red");
		}

		/// <summary>
		/// Draw SVG Element of a Circle
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		static string ToCircle(double x, double y, string color)
		{
			string s = string.Format(@"<circle cx=""{0}"" cy=""{1}"" r=""{2}"" fill=""{3}"" />", x.ToString("0.0"), y.ToString("0.0"), radius.ToString("0.0"), color);
			radius = radius + (decimal)0.1;
			return s;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		static string DrawSVG(int w, int h, double size, bool useOddEven, List<Path> pathList)
		{
			PrimitiveCurve c;
			w = (int)(w * size);
			h = (int)(h * size);
			int len = pathList.Count;

			string d = "";
			for (int i = 0; i < len; i++)
			{
				c = pathList[i].Curve;
				d += Path(c, size);
			}

			string path = useOddEven ? @"<path stroke=""black"" fill=""black""  fill-rule=""evenodd"" d=""{0}"" />" : @"<path  d=""{0}"" />";
			path = string.Format(path, d);
			path = "\r\n" + path;

			string points = "";
			if (ShowPoints)
			{
				for (int i = 0; i < len; i++)
				{
					c = pathList[i].Curve;
					points += Points(c, size);
				}
			}

			string svg = @"<svg id=""svg"" version=""1.1"" width=""{0}"" height=""{1}""  xmlns=""http://www.w3.org/2000/svg"">{2}{3}</svg>";
			svg = string.Format(svg,
				w.ToString(),
				h.ToString(),
				path,
				points);
			return svg;
		}

		public static string AsSVG(Bitmap bitmap,double size, bool clockwise, bool useCubicCurves, int turdSize, bool useOddEven)
		{
			var ListOfPaths = new List<List<Curve>>();
			var pathList = new List<Path>();

			UseCubicCurves = useCubicCurves;
			Potrace.Trace(bitmap, ListOfPaths, pathList, new Configurations
			{
				Clockwise = clockwise,
				TurdSize = turdSize
			});

			string svg = DrawSVG(bitmap.Width, bitmap.Height, size, useOddEven, pathList);

			ListOfPaths = null;
			return svg;
		}
	}
}

