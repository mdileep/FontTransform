using CsPotrace;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;

namespace FontTransformers
{
	class Runner
	{
		static string debugPath = "~";
		const string SVGTempalte = "svg.xml";
		static bool genDebugFiles = true;
		public const int Precision = 1;
		public static int Size = 1;

		public static void ProcessUFO(string srcDirectory, string targetDirectory, string transDefFile, bool includeOriginal, int turdSize)
		{
			var srcDirectoryInfo = new DirectoryInfo(srcDirectory);
			targetDirectory = new DirectoryInfo(targetDirectory).FullName;
			Directory.CreateDirectory(targetDirectory);

			foreach (FileInfo FI in srcDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
			{
				DateTime start = DateTime.Now;
				Console.Write(FI.Name);

				string relativePath = FI.FullName.Substring(srcDirectoryInfo.FullName.Length + 1);
				string targetFile = System.IO.Path.Combine(targetDirectory, relativePath);

				var targetFileInfo = new FileInfo(targetFile);
				if (!targetFileInfo.Directory.Exists)
				{
					targetFileInfo.Directory.Create();
				}

				if (FI.Extension == ".glif")
				{
					bool succ = false;
					try
					{
						succ = DoConvert(FI.FullName, targetFile, transDefFile, includeOriginal, turdSize);
					}
					catch
					{
						succ = false;
					}

					if (!succ)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						File.Copy(FI.FullName, targetFile, true);
					}
				}
				else
				{
					File.Copy(FI.FullName, targetFile, true);
				}

				Console.WriteLine(" -" + (DateTime.Now - start).TotalMilliseconds + "ms");
				Console.ResetColor();
			}
		}

		internal static void ProcessSVG(string srcFile, string targetFile, string transDefFile, bool includeOriginal, int turdSize)
		{
			XmlDocument document = new XmlDocument();
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.XmlResolver = null;
			settings.DtdProcessing = DtdProcessing.Ignore;

			Stream s = new FileStream(srcFile, FileMode.Open);
			XmlReader reader = XmlReader.Create(s, settings);
			document.Load(reader);

			string rules = File.ReadAllText(SVGTempalte);
			int cnt = 1;

			XmlNodeList glyphs = document.GetElementsByTagName("glyph");
			foreach (XmlNode glyph in glyphs)
			{
				DateTime start = DateTime.Now;

				var d = glyph.Attributes["d"] == null ? "" : glyph.Attributes["d"].InnerText;
				if (string.IsNullOrEmpty(d))
				{
					continue;
				}

				var name = glyph.Attributes["glyph-name"].InnerText;

				if (name != "Y")
				{
					continue;
				}

				name = cnt + "." + name;
				Console.Write(name);


				var d2 = RelativeToAbsolute(d);

				ViewBox WH = GetViewBox(d2);
				string absoluteSVG = rules
									.Replace("$width", WH.Width.ToString())
									.Replace("$height", WH.Height.ToString())
									.Replace("$minX", WH.MinX.ToString())
									.Replace("$minY", WH.MinY.ToString())
									.Replace("$path", d2);

				//string relativeSVG = rules.Replace("$width", WH.x.ToString()).Replace("$height", WH.y.ToString()).Replace("$path", d);
				//if (genDebugFiles)
				//{
				//	File.WriteAllText(DebugFile("SVG", ".svg", name), relativeSVG);
				//	File.WriteAllText(DebugFile("SVG2", ".svg", name), absoluteSVG);
				//}

				string targetPath = DoConvert2(name, absoluteSVG, transDefFile, includeOriginal, turdSize, WH.MinX, WH.MinY);

				if (Size != 1)
				{
					if (glyph.Attributes["horiz-adv-x"] == null)
					{
						Debugger.Break();
					}
					else
					{
						double horiz = Convert.ToDouble(glyph.Attributes["horiz-adv-x"].InnerText);
						glyph.Attributes["horiz-adv-x"].InnerText = (horiz * Size).ToString("0");
					}
				}

				glyph.Attributes["d"].InnerText = targetPath;

				cnt++;

				Console.WriteLine(" -" + (DateTime.Now - start).TotalMilliseconds + "ms");
			}
			document.Save(targetFile);
		}

		static bool DoConvert(string glifFile, string targetGlif, string transDefFile, bool includeOriginal, int turdSize)
		{
			//1.Convert glifFile to RawSVG Stream
			//2.Apply target Transformations SVG
			//3.Use SVG File to CreateBitmap Object
			//4.Generate SVG File with Transformations
			//5.Translate Origin based on ViewBox of RawSVG
			//6.Generate glifFile from SVGFile.

			//Step#1
			string rawSVG = ConvertToSVG(glifFile);
			if (string.IsNullOrEmpty(rawSVG))
			{
				return false;
			}

			//Step#2&3
			string svgWithTrans = ApplyTransformations(rawSVG, transDefFile);

			//Step#3
			Bitmap bitmap = LoadAsBitmap(svgWithTrans);

			{
				string name = new FileInfo(glifFile).Name;
				bitmap.Save(DebugFile("svgWithTrans", ".bmp", name), ImageFormat.Bmp);
			}

			bool targtDirection = TargetDirection(glifFile);

			//Step#4
			string svg = PotraceWrapper.AsSVG(bitmap, Size, false, false, turdSize, false);
			string svg2 = PotraceWrapper.AsSVG(bitmap, Size, false, true, turdSize, false);

			//Step#5
			ViewBox VB = ExtractViewBox(rawSVG); //Can be made part of ConvertToSVG of Step#1
			string path = GetPath(svg);
			path = Translate(path, VB.MinX, VB.MinY, " ", " ");

			//Step#6
			string targetGlifContent = ConvertToGilf(path, glifFile, includeOriginal);
			File.WriteAllText(targetGlif, targetGlifContent);


			if (genDebugFiles)
			{
				string name = new FileInfo(glifFile).Name;

				File.WriteAllText(DebugFile("rawSVG", ".svg", name), rawSVG);
				File.WriteAllText(DebugFile("svgWithTrans", ".svg", name), svgWithTrans);
				bitmap.Save(DebugFile("svgWithTrans", ".bmp", name), ImageFormat.Bmp);

				File.WriteAllText(DebugFile("targetSVG", ".svg", name), svg);
				File.WriteAllText(DebugFile("targetSVG2", ".svg", name), svg2);
				File.WriteAllText(DebugFile("targetglif", ".glif", name), targetGlifContent);

				string targetglifSVG = ConvertToSVG(DebugFile("targetglif", ".glif", name));
				File.WriteAllText(DebugFile("targetglifSVG", ".svg", name), targetglifSVG);
			}

			return true;
		}

		private static ViewBox ExtractViewBox(string svg)
		{
			XmlDocument document = new XmlDocument();
			document.LoadXml(svg);

			var svgElems = document.GetElementsByTagName("svg");
			var svgElem = svgElems[0];
			var viewBox = svgElem.Attributes["viewBox"].InnerText;
			string[] Coors = viewBox.Split(' ');
			string[] xy1 = Coors[0].Split(',');
			string[] xy2 = Coors[1].Split(',');

			decimal x1 = Convert.ToDecimal(xy1[0]);
			decimal y1 = Convert.ToDecimal(xy1[1]);

			decimal x2 = Convert.ToDecimal(xy2[0]);
			decimal y2 = Convert.ToDecimal(xy2[1]);

			return new ViewBox
			{
				MinX = x1,
				MinY = y1,
				Width = x2,
				Height = y2
			};
		}

		static string GetPath(string svg)
		{
			XmlDocument document = new XmlDocument();
			document.LoadXml(svg);

			var paths = document.GetElementsByTagName("path");
			var path = paths[0];
			var d = path.Attributes["d"].InnerText;
			return d;
		}

		static string DoConvert2(string name, string rawSVG, string transDefFile, bool includeOriginal, int turdSize, decimal minX, decimal minY)
		{
			//1.Apply target Transformations SVG on the source SVG
			//2.Use SVG File to CreateBitmap Object
			//3.Generate SVG File with Transformations
			//4.Return target SVG path

			//Step#1
			string svgWithTrans = ApplyTransformations(rawSVG, transDefFile);

			//Step#2
			Bitmap bitmap = LoadAsBitmap(svgWithTrans);
			bool targtDirection = TargetSVGDirection(rawSVG);

			//Step#3
			string targetSVG = PotraceWrapper.AsSVG(bitmap, Size, false, false, turdSize, false);
			string targetSVG2 = PotraceWrapper.AsSVG(bitmap, Size, false, true, turdSize, false);

			//Step#4
			string targetPath = ExtractPath(targetSVG);

			targetPath = Translate(targetPath, minX, minY, ",", "");

			if (genDebugFiles)
			{
				File.WriteAllText(DebugFile("rawSVG", ".svg", name), rawSVG);
				File.WriteAllText(DebugFile("svgWithTrans", ".svg", name), svgWithTrans);
				bitmap.Save(DebugFile("svgWithTrans", ".bmp", name));

				File.WriteAllText(DebugFile("targetSVG", ".svg", name), targetSVG);
				File.WriteAllText(DebugFile("targetSVG2", ".svg", name), targetSVG2);
			}

			return targetPath;
		}

		static string Translate(string inputPath, decimal minX, decimal minY, string pointSeperator, string commandSperator)
		{
			StringBuilder outputPath = new StringBuilder();
			foreach (string s in inputPath.Split(' ', '\r', '\n'))
			{
				if (s == "")
				{
					continue;
				}
				switch (s)
				{
					case "M":
					case "L":
					case "Q":
					case "C":
						outputPath.Append("\r\n");
						outputPath.Append(s);
						outputPath.Append(commandSperator);
						break;
					case "S":
					case "T":
					case "V":
					case "H":
					case "Z":
						//These are unexpeted 
						outputPath.Append(s);
						outputPath.Append(commandSperator);
						break;
					default:
						string[] xy = s.Split(',');
						decimal x = Convert.ToDecimal(xy[0]);
						decimal y = Convert.ToDecimal(xy[1]);
						outputPath.Append(minX + x);
						outputPath.Append(pointSeperator);
						outputPath.Append(minY + y);
						outputPath.Append(" ");
						break;

				}

			}
			return outputPath.ToString();
		}

		static string ExtractPath(string rawSVG)
		{
			XmlDocument document = new XmlDocument();
			document.LoadXml(rawSVG);

			var paths = document.GetElementsByTagName("path");
			var path = paths[0];

			var d = path.Attributes["d"].InnerText;
			d = d.Replace("\n", " ");
			return d;
		}

		static ViewBox GetViewBox(string input)
		{
			//To be improved.
			input = input.Replace("\r\n", "")
						.Replace(" ", ",")
						.Replace("M", " M ")
						.Replace("L", " L ")
						.Replace("Q", " Q ")
						.Replace("C", " C ")
						.Replace("S", " S ")
						.Replace("T", " T ")
						.Replace("V", " V ")
						.Replace("H", " H ")
						.Replace("Z", " Z ");
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

			Point last = new Point
			{
				x = 0,
				y = 0
			};
			string lastCommand = "";
			List<Point> List = new List<Point>();
			foreach (string s in input.Split(' '))
			{
				if (s == "")
				{
					continue;
				}
				switch (s)
				{
					case "M":
					case "L":
					case "Q":
					case "C":
					case "T":
					case "S":
					case "V":
					case "H":
					case "Z":
						lastCommand = s;
						break;

					default:
						string[] coors = s.TrimEnd(',').Split(',');
						if (coors.Length == 1)
						{
							if (string.IsNullOrEmpty(coors[0]))
							{
								continue;
							}

							decimal val = decimal.Parse(coors[0]);
							if (lastCommand == "V")
							{
								last = new Point
								{
									x = last.x,
									y = val
								};
							}

							if (lastCommand == "H")
							{
								last = new Point
								{
									x = val,
									y = last.y
								};
							}

						}
						else if (coors.Length == 2)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);

							last = new Point
							{
								x = val1,
								y = val2
							};
						}
						else if (coors.Length == 4)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);
							decimal val3 = decimal.Parse(coors[2]);
							decimal val4 = decimal.Parse(coors[3]);

							List.Add(new Point
							{
								x = val1,
								y = val2
							});

							last = new Point
							{
								x = val3,
								y = val4
							};
						}
						else if (coors.Length == 6)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);
							decimal val3 = decimal.Parse(coors[2]);
							decimal val4 = decimal.Parse(coors[3]);
							decimal val5 = decimal.Parse(coors[4]);
							decimal val6 = decimal.Parse(coors[5]);

							List.Add(new Point
							{
								x = val1,
								y = val2
							});


							List.Add(new Point
							{
								x = val3,
								y = val4
							});

							last = new Point
							{
								x = val5,
								y = val6
							};

						}
						else
						{
							Debugger.Break();
						}
						List.Add(last);
						break;
				}
			}

			foreach (Point P in List)
			{
				if (P.x > MaxP.x)
				{
					MaxP.x = P.x;
				}

				if (P.y > MaxP.y)
				{
					MaxP.y = P.y;
				}

				if (P.x < MinP.x)
				{
					MinP.x = P.x;
				}

				if (P.y < MinP.y)
				{
					MinP.y = P.y;
				}
			}

			int w_max = (int)Math.Ceiling((double)MaxP.x / Precision) * Precision;
			int h_max = (int)Math.Ceiling((double)MaxP.y / Precision) * Precision;

			int w_min = (int)Math.Floor((double)MinP.x / Precision) * Precision;
			int h_min = (int)Math.Floor((double)MinP.y / Precision) * Precision;

			int w = w_max - w_min;
			int h = h_max - h_min;

			return new ViewBox
			{
				MinX = w_min,
				MinY = h_min,
				Width = w,
				Height = h
			};
		}

		static string RelativeToAbsolute(string input)
		{
			//To be improved.
			input = input.Replace("\r\n", "")
			.Replace(" ", ",")
			.Replace("M", " M ")
			.Replace("l", " l ")
			.Replace("q", " q ")
			.Replace("c", " c ")
			.Replace("s", " s ")
			.Replace("t", " t ")
			.Replace("v", " v ")
			.Replace("h", " h ")
			.Replace("z", " z ");

			string output = "";
			string last = "";
			decimal X = 0;
			decimal Y = 0;
			foreach (string s in input.Split(' '))
			{
				if (s == "")
				{
					continue;
				}

				switch (s)
				{
					case "M":
						last = s;
						output = output + "\r\n" + s.ToUpper();
						X = 0;
						Y = 0;
						break;

					case "L":
					case "Q":
					case "C":
					case "T":
					case "S":
					case "V":
					case "H":
					case "Z":
						throw new Exception("Currently not supported mixed relative and absolute paths");

					case "l":
					case "q":
					case "c":
					case "t":
					case "s":
					case "v":
					case "h":
					case "z":
						last = s;
						output = output + "\r\n" + s.ToUpper();
						break;

					default:
						string[] coors = s.TrimEnd(',').Split(',');
						//Use Switch
						if (coors.Length == 1)
						{
							if (string.IsNullOrEmpty(coors[0]))
							{
								continue;
							}

							decimal val = decimal.Parse(coors[0]);
							if (last == "v")
							{
								val = val + Y;
								Y = val;
							}

							if (last == "h")
							{
								val = val + X;
								X = val;
							}

							output = output + val.ToString("0");
						}
						else if (coors.Length == 2)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);


							val1 = val1 + X;
							val2 = val2 + Y;

							output = output + val1.ToString("0") + "," + val2.ToString("0") + " ";

							X = val1;
							Y = val2;

						}
						else if (coors.Length == 4)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);
							decimal val3 = decimal.Parse(coors[2]);
							decimal val4 = decimal.Parse(coors[3]);

							val1 = val1 + X;
							val2 = val2 + Y;

							val3 = val3 + X;
							val4 = val4 + Y;

							output = output + val1.ToString("0") + "," + val2.ToString("0") + " ";
							output = output + val3.ToString("0") + "," + val4.ToString("0") + " ";

							X = val3;
							Y = val4;
						}
						else if (coors.Length == 6)
						{
							decimal val1 = decimal.Parse(coors[0]);
							decimal val2 = decimal.Parse(coors[1]);
							decimal val3 = decimal.Parse(coors[2]);
							decimal val4 = decimal.Parse(coors[3]);
							decimal val5 = decimal.Parse(coors[4]);
							decimal val6 = decimal.Parse(coors[5]);

							val1 = val1 + X;
							val2 = val2 + Y;

							val3 = val3 + X;
							val4 = val4 + Y;

							val5 = val5 + X;
							val6 = val6 + Y;

							output = output + val1.ToString("0") + "," + val2.ToString("0") + " ";
							output = output + val3.ToString("0") + "," + val4.ToString("0") + " ";
							output = output + val5.ToString("0") + "," + val6.ToString("0") + " ";

							X = val5;
							Y = val6;
						}
						else
						{
							Debugger.Break();
						}
						break;
				}
			}
			return output;
		}

		static bool TargetSVGDirection(string rawSVG)
		{
			//TODO:
			return true;
		}

		static bool TargetDirection(string glifFile)
		{
			glyph g = Export.Deserialise(glifFile);
			int cnt = 0;
			int maxContour = 0;
			double maxArea = 0;
			foreach (Contour c in g.outline.contour)
			{
				double area = PolygonArea(c.point);
				if (area > maxArea)
				{
					maxArea = area;
					maxContour = cnt;
				}
				cnt++;
			}

			Contour c2 = g.outline.contour[maxContour];
			if (c2.point.Length < 3)
			{
				return false;
			}

			bool currDirection = GetCurrentDirection(c2);
			return !currDirection;
		}

		static bool GetCurrentDirection(Contour c2)
		{
			double s1 = 0;
			double s2 = 0;
			int index = 0;
			while (s1 == 0 || s2 == 0)
			{
				if (index + 2 >= c2.point.Length)
				{
					break;
				}

				dPoint p1 = new dPoint((double)c2.point[index + 0].x, (double)c2.point[index + 0].y);
				dPoint p2 = new dPoint((double)c2.point[index + 1].x, (double)c2.point[index + 1].y);
				dPoint p3 = new dPoint((double)c2.point[index + 2].x, (double)c2.point[index + 2].y);

				s1 = MathUtil.Slope(p1, p2);
				s2 = MathUtil.Slope(p2, p3);

				//If  σ < τ, the orientation is counterclockwise (left turn)
				//If  σ = τ, the orientation is collinear
				//If  σ > τ, the orientation is clockwise (right turn)
				if (s1 == 0 || s2 == 0)
				{
					index = index + 1;
					continue;
				}

				if (s1 < s2)
				{
					return false;
				}

				if (s1 == s2)
				{
					index = index + 1;
					continue;
				}

				return true;
			}

			return true;
		}

		static double PolygonArea(Point[] point)
		{
			double area = 0;
			int j = point.Length - 1;

			for (int i = 0; i < point.Length; i++)
			{
				area = area + (double)((point[j].x + point[i].x) * (point[j].y - point[i].y));
				j = i;  //j is previous vertex to i
			}
			return Math.Abs(area / 2);
		}

		static string DebugFile(string prefix, string ext, string name)
		{
			string dir = System.IO.Path.Combine(debugPath, prefix);
			Directory.CreateDirectory(dir);
			string s = System.IO.Path.Combine(dir, name + ext);
			return s;
		}

		static Bitmap LoadAsBitmap(string svg)
		{
			XmlDocument document = new XmlDocument();
			document.LoadXml(svg);

			SvgDocument svgDoc = SvgDocument.Open(document);
			Bitmap bitmap = svgDoc.Draw();
			bitmap = ProcessColors(bitmap);
			return bitmap;
		}

		static Bitmap ProcessColors(Bitmap OriginalImage)
		{
			Bitmap bmap = (Bitmap)OriginalImage.Clone();
			Color col;
			for (int i = 0; i < bmap.Width; i++)
			{
				for (int j = 0; j < bmap.Height; j++)
				{
					col = bmap.GetPixel(i, j);
					int rgb = (col.R + col.G + col.B) / 3;
					if (rgb == 0)
					{
						rgb = 255 - col.A;
						bmap.SetPixel(i, j, Color.FromArgb(rgb, rgb, rgb));
						continue;
					}

					bmap.SetPixel(i, j, Color.FromArgb(255 - col.R, 255 - col.G, 255 - col.B));
				}
			}
			return bmap;
		}

		static string ConvertToGilf(string path, string srcGlifFile, bool includeOriginal)
		{
			XmlDocument g = new XmlDocument();
			g.Load(srcGlifFile);

			string s = "";
			string[] lines = path.Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();

				if (string.IsNullOrEmpty(line))
				{
					continue;
				}

				string[] arr = line.Split(' ');
				switch (arr[0])
				{
					case "M":
						if (i + 1 < lines.Length)
						{
							var x1 = arr[1];
							var y1 = arr[2];
							if (s != "")
							{
								s = s + "</contour>";
								s = s + Environment.NewLine;
							}
							s = s + "<contour>";
							s = s + Environment.NewLine;
							s = s + string.Format(@"<point x=""{0}"" y=""{1}""  />", x1, y1);
							s = s + Environment.NewLine;
						}
						continue;

					case "L":
						s = s + BuildLine(arr);
						break;

					case "Q":
						s = s + BuildQubic(arr);
						break;

					case "C":
						s = s + BuildCurve(arr);
						break;

					case "T":
						s = s + BuildSmooth(arr);
						break;
				}
				s = s + Environment.NewLine;
			}

			if (s != "")
			{
				s = s + "</contour>";
				s = s + Environment.NewLine;
			}

			var outline = g.GetElementsByTagName("outline")[0];
			if (includeOriginal)
			{
				outline.InnerXml = outline.InnerXml + s;
			}
			else
			{
				outline.InnerXml = s;
			}

			if (Size != 1)
			{
				var advances = g.GetElementsByTagName("advance");
				if (advances != null && advances.Count != 0)
				{
					var advance = advances[0];
					if (advance != null)
					{
						var width = advance.Attributes["width"].InnerText;
						advance.Attributes["width"].InnerText = (Convert.ToDouble(width) * Size).ToString("0");
					}
				}
			}

			return g.InnerXml;
		}

		private static string BuildCurve(string[] arr)
		{
			var x1 = Convert.ToDecimal(arr[1]);
			var y1 = Convert.ToDecimal(arr[2]);

			var x2 = Convert.ToDecimal(arr[3]);
			var y2 = Convert.ToDecimal(arr[4]);

			var x3 = Convert.ToDecimal(arr[5]);
			var y3 = Convert.ToDecimal(arr[6]);

			var retVal = string.Format(@"<point x=""{0}"" y=""{1}"" type=""curve"" />", x1, y1);
			retVal = retVal + Environment.NewLine;
			retVal = retVal + string.Format(@"<point x=""{0}"" y=""{1}""  />", x2, y2);
			retVal = retVal + Environment.NewLine;
			retVal = retVal + string.Format(@"<point x=""{0}"" y=""{1}""  />", x3, y3);
			return retVal;
		}

		static string BuildQubic(string[] arr)
		{
			var x1 = Convert.ToDecimal(arr[1]);
			var x2 = Convert.ToDecimal(arr[3]);

			var y1 = Convert.ToDecimal(arr[2]);
			var y2 = Convert.ToDecimal(arr[4]);

			var x = x2 - ((x1 - x2));
			var y = y2 - ((y1 - y2));

			var retVal = string.Format(@"<point x=""{0}"" y=""{1}"" type=""qcurve"" />", x1, y1);
			retVal = retVal + Environment.NewLine;
			retVal = retVal + string.Format(@"<point x=""{0}"" y=""{1}""  />", x, y);
			return retVal;
		}

		static string BuildSmooth(string[] arr)
		{
			//TODO...
			return "";
		}

		static string BuildLine(string[] arr)
		{
			var x1 = arr[1];
			var y1 = arr[2];
			var x2 = arr[3];
			var y2 = arr[4];

			var retVal = string.Format(@"<point x=""{0}"" y=""{1}"" type=""line""/>", x1, y1);
			retVal = retVal + Environment.NewLine;
			retVal = retVal + string.Format(@"<point x=""{0}"" y=""{1}"" type=""line""/>", x2, y2);
			return retVal;
		}

		static string ApplyTransformations(string rawSVG, string transDefFile)
		{
			string transformations = File.ReadAllText(transDefFile);

			XmlDocument document = new XmlDocument();
			document.LoadXml(rawSVG);

			var paths = document.GetElementsByTagName("path");
			string path = paths[0].Attributes["d"].InnerText;

			var svg = document.GetElementsByTagName("svg");
			string w = svg[0].Attributes["width"].InnerText;
			string h = svg[0].Attributes["height"].InnerText;
			string viewBox = svg[0].Attributes["viewBox"].InnerText;

			return transformations
				.Replace("$width", w)
				.Replace("$height", h)
				.Replace("$viewBox", viewBox)
				.Replace("$path", path);
		}

		static string ConvertToSVG(string glifFile)
		{
			glyph g = Export.Deserialise(glifFile);
			if (g.outline == null || g.outline.contour == null || g.outline.component != null)
			{
				return "";
			}
			string svgContents = Export.ToSVG(g, false, false, Precision);
			return svgContents;
		}
	}
}
