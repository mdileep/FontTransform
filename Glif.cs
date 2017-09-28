using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace FontTransformers
{
	public class glyph
	{
		[XmlAttribute()]
		public string format
		{
			get;
			set;
		}


		[XmlAttribute()]
		public string name
		{
			get;
			set;
		}
		public Advance advance
		{
			get;
			set;
		}

		public Unicode unicode
		{
			get;
			set;
		}

		[XmlElement()]
		public Outline outline
		{
			get;
			set;
		}


	}

	public class Advance
	{
		[XmlAttribute()]
		public int width
		{
			get;
			set;
		}
	}

	public class Unicode
	{
		[XmlAttribute()]
		public string hex
		{
			get;
			set;
		}
	}

	public class Outline
	{
		[XmlElement()]
		public Contour[] contour
		{
			get;
			set;
		}

		[XmlElement()]
		public component[] component
		{
			get;
			set;
		}


	}

	public class component
	{
		[XmlElement("base")]
		public string Base
		{
			get;
			set;
		}

		public int xOffset
		{
			get;
			set;
		}
	}

	public class Contour
	{
		[XmlElement()]
		public Point[] point
		{
			get;
			set;
		}
	}

	public class Point
	{
		[XmlAttribute()]
		public decimal x
		{
			get;
			set;
		}

		[XmlAttribute()]
		public decimal y
		{
			get;
			set;
		}

		[XmlAttribute()]
		public string type
		{
			get;
			set;
		}

		[XmlAttribute()]
		public string smooth
		{
			get;
			set;
		}

		public string ToPath()
		{
			string s = "";
			s = string.Format("");
			return s;
		}

		public override string ToString()
		{
			return x + " , " + y;
		}
	}

	public static partial class Export
	{
		public static glyph Deserialise(string glifFile)
		{
			glyph g = new glyph();
			XmlSerializer x = new XmlSerializer(g.GetType());
			using (FileStream fileStream = new FileStream(glifFile, FileMode.Open))
			{
				g = (glyph)x.Deserialize(fileStream);
			}
			return g;
		}
		static Point MaxPoint(Outline outline)
		{
			Point M = new Point
			{
				x = 0,
				y = 0
			};
			if (outline.contour != null)
			{
				foreach (Contour C in outline.contour)
				{
					Point P = MaxPoint(C);

					if (P.x > M.x)
					{
						M.x = P.x;
					}

					if (P.y > M.y)
					{
						M.y = P.y;
					}
				}
			}
			return M;
		}
		static Point MinPoint(Outline outline)
		{
			Point M = new Point
			{
				x = 0,
				y = 0
			};
			if (outline.contour != null)
			{
				foreach (Contour C in outline.contour)
				{
					Point P = MinPoint(C);

					if (P.x < M.x)
					{
						M.x = P.x;
					}

					if (P.y < M.y)
					{
						M.y = P.y;
					}
				}
			}
			return M;
		}
		static Point MaxPoint(Contour C)
		{
			Point M = new Point
			{
				x = 0,
				y = 0
			};

			foreach (Point P in C.point)
			{
				if (P.x > M.x)
				{
					M.x = P.x;
				}

				if (P.y > M.y)
				{
					M.y = P.y;
				}
			}
			return M;
		}
		static Point MinPoint(Contour C)
		{
			Point M = new Point
			{
				x = 0,
				y = 0
			};

			foreach (Point P in C.point)
			{
				if (P.x < M.x)
				{
					M.x = P.x;
				}

				if (P.y < M.y)
				{
					M.y = P.y;
				}
			}
			return M;
		}
	}

	public static partial class Export
	{
		public static string ToSVG(glyph g, bool showPoints, bool showPolygon, int Precision)
		{
			if (g == null)
			{
				return "";
			}

			string contours = "";
			string points = "";
			string polygon = "";

			Point MaxP = new Point
			{
				x = 0,
				y = 0
			};
			Point MinP = new Point
			{
				x = 0,
				y = 0
			};

			if (g.outline != null && g.outline.contour != null)
			{
				foreach (Contour contour in g.outline.contour)
				{
					contours = contours + ToPath(contour);
				}
				contours = contours.TrimEnd('\r', '\n');

				if (showPoints)
				{
					foreach (Contour contour in g.outline.contour)
					{
						points = points + ListPoints(contour);
					}
					points = points.TrimEnd('\r', '\n');
				}

				if (showPolygon)
				{
					foreach (Contour contour in g.outline.contour)
					{
						polygon = polygon + PolygonPoints(contour);
					}
					polygon = polygon.TrimEnd('\r', '\n');
				}

				MaxP = MaxPoint(g.outline);
				MinP = MinPoint(g.outline);
			}


			int w_max = (int)Math.Ceiling((double)MaxP.x / Precision) * Precision;
			int h_max = (int)Math.Ceiling((double)MaxP.y / Precision) * Precision;

			int w_min = (int)Math.Floor((double)MinP.x / Precision) * Precision;
			int h_min = (int)Math.Floor((double)MinP.y / Precision) * Precision;

			int w = w_max - w_min;
			int h = h_max - h_min;

			int vx1 = w_min;
			int vy1 = h_min;

			int vx2 = w_max;
			int vy2 = h_max;

			//vx1 = w_min;
			//vy1 = Math.Min(h_min, -1 * h_max);

			vx2 = w;
			vy2 = h;

			int a = g.advance.width;

			string s = string.Format(svg_template,
								w, h,
								contours,
								points,
								polygon, 
								vx1, vy1);
			return s;
		}
		
		static string ToPath(Contour contour)
		{
			string lastCurve = "";
			Point prev = null;
			List<Point> List = new List<Point>();
			List<Contour> Groups = new List<Contour>();
			foreach (Point point_ in contour.point)
			{
				if (string.IsNullOrEmpty(point_.type))
				{
					//Part of Last Curve
				}
				else
				{
					if (prev == null)
					{
						//Nothing to do
					}
					else
					{
						//New Curve started Hence add the current list of points to Group
						//Clear the List
						Contour C = new Contour();
						C.point = List.ToArray();
						Groups.Add(C);
						List = new List<Point>();
					}

					//Start a new Group
					lastCurve = point_.type;
				}

				//Add this point to current group
				List.Add(point_);
				prev = new Point
				{
					x = point_.x,
					y = point_.y
				};
			}
			{
				//Process Pending List
				Contour C = new Contour();
				C.point = List.ToArray();
				Groups.Add(C);

				//
				Contour C2 = Groups[0];
				Contour C3 = new Contour();
				C3.point = new Point[]
				{
					C2.point[0]
				};
				Groups.Add(C3);
			}
			string s = ProcessGroups(Groups);
			return s;
		}
		static string ProcessGroups(List<Contour> Groups)
		{
			string s = "";
			bool first = true;
			bool QCurve = false;
			foreach (Contour C in Groups)
			{
				if (C.point.Length == 0)
				{
					continue;
				}
				string groupType = C.point[0].type;
				string smooth = C.point[0].smooth;
				Point P0 = C.point[0];
				if (first)
				{
					s = string.Format("M{0} {1} \r\n", P0.x, P0.y);
					if (C.point.Length == 1)
					{
						first = false;
						continue;
					}
				}

				if (C.point.Length == 1)
				{
					if (groupType == "line")
					{
						s = s + string.Format("L{0} {1} \r\n", P0.x, P0.y);
					}
					if (groupType == "qcurve" || groupType == "curve")
					{
						if (QCurve)
						{
							s = s + string.Format("{0} {1} \r\n", P0.x, P0.y);
							QCurve = false;
						}
						else
						{
							s = s + string.Format("T{0} {1} \r\n", P0.x, P0.y);
						}
					}
					continue;
				}

				int cnt = 0;
				Point Prev = null;

				if (C.point.Length == 2 && groupType == "qcurve")
				{
					Point P1 = C.point[0];
					Point P2 = C.point[1];

					Point Mid = null;

					Mid = new Point
					{
						x = (P2.x + P1.x) / 2,
						y = (P2.y + P1.y) / 2,
					};

					if (first)
					{
						s = s + string.Format("Q{0} {1} ", P2.x, P2.y);
						first = false;
					}
					else
					{
						s = s + string.Format("Q{0} {1} {2} {3}\r\n", P1.x, P1.y, Mid.x, Mid.y);
					}

					QCurve = true;
				}

				if (C.point.Length > 2 || (C.point.Length == 2 && groupType == "line"))
				{
					foreach (Point P in C.point)
					{
						Point Mid = null;
						if (Prev != null)
						{
							Mid = new Point
							{
								x = (Prev.x + P.x) / 2,
								y = (Prev.y + P.y) / 2,
							};
						}

						if (first && cnt == 0)
						{
							Prev = P;
							cnt++;
							first = false;
							continue;
						}
						if (!first && cnt == 0)
						{
							if (smooth == "yes")
							{
								if (QCurve)
								{
									s = s + string.Format("{0} {1} \r\n", P.x, P.y);
									QCurve = false;
								}
								else
								{
									s = s + string.Format("T{0} {1} \r\n", P.x, P.y);
								}
							}
							else if (groupType == "line")
							{
								s = s + string.Format("L{0} {1} \r\n", P.x, P.y);
							}
							else if (groupType == "qcurve")
							{
								if (QCurve)
								{
									s = s + string.Format("{0} {1} \r\n", P.x, P.y);
									QCurve = false;
								}
								else
								{
									s = s + string.Format("T{0} {1} \r\n", P.x, P.y);
								}
							}
							Prev = P;
							cnt++;
							continue;
						}
						if (cnt == 1)
						{
							s = s + string.Format("Q{0} {1} ", P.x, P.y);
							QCurve = true;
						}
						else if (cnt == 2)
						{
							s = s + string.Format("{0} {1} \r\n", Mid.x, Mid.y);
							QCurve = false;
						}
						else
						{
							if (QCurve)
							{
								s = s + string.Format("{0} {1} \r\n", Mid.x, Mid.y);
								QCurve = false;
							}
							else
							{
								s = s + string.Format("T{0} {1} \r\n", Mid.x, Mid.y);
							}
						}
						cnt++;
						Prev = P;
					}
				}
			}

			s = s + "Z";
			return s;
		}

		static string ListPoints(Contour contour)
		{
			string s = "";
			bool first = true;
			foreach (Point point_ in contour.point)
			{
				s = s + string.Format(@"<circle cx=""{0}"" cy=""{1}"" r=""5""  type=""{2}"" class=""circle{3}"" />", point_.x, point_.y, point_.type, first ? " first" : "");
				s = s + Environment.NewLine;

				if (first)
				{
					first = false;
				}
			}
			return s;
		}

		static string PolygonPoints(Contour contour)
		{
			string s = "";
			foreach (Point point_ in contour.point)
			{
				s = s + string.Format(@"{0},{1} ", point_.x, point_.y);
			}
			{
				Point point_ = contour.point[0];
				s = s + string.Format(@"{0},{1} ", point_.x, point_.y);
			}
			s = string.Format(@"<polyline points=""{0}"" class=""ploygon""  />", s);
			s = s + Environment.NewLine;
			return s;
		}

		const string svg_template = @"<svg
width=""{0}"" height=""{1}"" viewBox=""{5},{6} {0},{1}""
version=""1.0"" xmlns=""http://www.w3.org/2000/svg"" >
<defs />
<g>
<path 
d=""{2}"" />
{3}
{4}
</g>
</svg>";

	}

	public static class NoiseUtil
	{
		public static glyph AddNoise(glyph g, int noise)
		{
			glyph g2 = new glyph
			{
				advance = g.advance,
				format = g.format,
				name = g.name,
				outline = AddNoise(g.outline, noise),
				unicode = g.unicode
			};

			return g2;
		}
		static Outline AddNoise(Outline outline, int noise)
		{
			if (outline == null)
			{
				return null;
			}
			Outline O = new Outline
			{
				component = outline.component,
				contour = AddNoise(outline.contour, noise)
			};
			return O;
		}
		static Contour[] AddNoise(Contour[] contour, int noise)
		{
			Contour[] C = new Contour[contour.Length];
			for (int i = 0; i < contour.Length; i++)
			{
				C[i] = AddNoise(contour[i], noise);
			}
			return C;
		}
		static Contour AddNoise(Contour contour, int noise)
		{
			Contour C = new Contour
			{
				point = AddNoise(contour.point, noise)
			};
			return C;
		}
		static Point[] AddNoise(Point[] point, int noise)
		{
			Point[] C = new Point[point.Length];
			for (int i = 0; i < point.Length; i++)
			{
				C[i] = AddNoise(point[i], noise);
			}
			return C;
		}
		static Point AddNoise(Point point, int noise)
		{
			Point P = new Point
			{
				smooth = point.smooth,
				type = point.type,
				x = AddNoise(point.x, noise),
				y = AddNoise(point.y, noise)
			};
			return P;
		}
		static int AddNoise(decimal p, int noise)
		{
			double d = R.NextDouble() - 0.5;
			int next = R.Next(-1 * noise, noise);
			return (int)(p + next);
		}

		static Random R = new Random();
	}
}
