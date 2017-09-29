namespace FontTransformers
{
	class Program
	{
		static void Main(string[] args)
		{
			ProcessSVG(args);
		}

		static void ProcessSVG(string[] args)
		{
#if RELEASE
			if (args.Length < 5)
			{
				ShowHelp();
				return;
			}

			string srcFile = args[0];
			string targetFile = args[1];
			string transDefFile = args[2];
			bool includeOriginal = (args[3] == "1");
			int turdSize = Convert.ToInt32(args[4]);
#else
			string srcFile = @"_Ramaraja-Regular.svg";
			string targetFile = @"Ramaraja-Regular.svg";
			string transDefFile = @"Textures\Fills\Topography.xml";
			bool includeOriginal = false;
			int turdSize = 25;
#endif
			Runner.ProcessSVG(srcFile, targetFile, transDefFile, includeOriginal, turdSize);
		}

		static void ProcessUFO(string[] args)
		{
#if RELEASE
			if (args.Length < 5)
			{
				ShowHelp();
				return;
			}

			string srcDirectory = args[0];
			string targetDirectory = args[1];
			string transDefFile = args[2];
			bool includeOriginal = (args[3] == "1");
			int turdSize = Convert.ToInt32(args[4]);
#else
			string srcDirectory = @"_Ramaraja-Regular.ufo";
			string targetDirectory = @"Ramaraja-Regular.ufo";
			string transDefFile = @"Textures\Fills\Topography.xml";
			bool includeOriginal = false;
			int turdSize = 25;
#endif
			Runner.ProcessUFO(srcDirectory, targetDirectory, transDefFile, includeOriginal, turdSize);
		}


		/// <summary>
		/// Help
		/// </summary>
		static void ShowHelp()
		{
			//TODO..
		}

	}
}