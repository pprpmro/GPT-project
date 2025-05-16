namespace GPTProject.ConsoleUI
{
	public static class TxtFileHelper
	{
		public static List<string> GetListTxtFilePaths(string folderPath)
		{
			if (!Directory.Exists(folderPath))
			{
				Console.WriteLine($"Папка {folderPath} не найдена.");
				return new List<string>();
			}
			return Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly).ToList();
		}

		public static List<string> GetListTxtFileText(List<string> segmentFiles)
		{
			var segments = new List<string>();

			foreach (var filePath in segmentFiles)
			{
				if (File.Exists(filePath))
				{
					segments.Add(File.ReadAllText(filePath));
				}
			}

			return segments;
		}
	}
}
