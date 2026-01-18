using System;
using System.IO;

namespace Execor.Services
{
    public static class PathManager
    {
        public static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Execor");

        static PathManager()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
        }
    }
}
