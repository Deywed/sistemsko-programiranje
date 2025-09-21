namespace NyTimesListServer.Services
{
    public class SimpleLogger
    {
        private readonly string logFilePath;
        private readonly object fileLock = new object();

        public SimpleLogger(string path)
        {
            logFilePath = path;
            var dir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public void Info(string message)
        {
            string line = $"INFO: {message}";
            lock (fileLock)
            {
                Console.WriteLine(line);
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }

        public void Error(string message)
        {
            string line = $"ERROR: {message}";
            lock (fileLock)
            {
                Console.Error.WriteLine(line);
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
        public void Warning(string message)
        {
            string line = $"WARNING: {message}";
            lock (fileLock)
            {
                Console.WriteLine(line);
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
    }
}