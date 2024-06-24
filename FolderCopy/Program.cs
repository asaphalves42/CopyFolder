using System;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSync <sourceFolder> <replicaFolder> <intervalInSeconds> <logFilePath>");
            return;
        }

        string sourceFolder = args[0];
        string replicaFolder = args[1];
        int interval;
        if (!int.TryParse(args[2], out interval))
        {
            Console.WriteLine("The interval must be a valid integer.");
            return;
        }
        string logFilePath = args[3];

        // Start the synchronization process
        SyncFolders(sourceFolder, replicaFolder, interval, logFilePath);
    }

    static void SyncFolders(string sourceFolder, string replicaFolder, int interval, string logFilePath)
    {
        while (true)
        {
            try
            {
                SyncDirectories(sourceFolder, replicaFolder, logFilePath);
            }
            catch (Exception ex)
            {
                Log(ex.Message, logFilePath);
            }

            Thread.Sleep(interval * 1000);
        }
    }

    static void SyncDirectories(string sourceDir, string replicaDir, string logFilePath)
    {
        // Ensure replica directory exists
        Directory.CreateDirectory(replicaDir);

        // Copy new and updated files from source to replica
        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            string destFile = Path.Combine(replicaDir, fileName);

            if (!File.Exists(destFile) || !FilesAreEqual(filePath, destFile))
            {
                File.Copy(filePath, destFile, true);
                Log($"Copied: {filePath} to {destFile}", logFilePath);
            }
        }

        // Delete files from replica that are not in source
        foreach (string filePath in Directory.GetFiles(replicaDir))
        {
            string fileName = Path.GetFileName(filePath);
            string sourceFile = Path.Combine(sourceDir, fileName);

            if (!File.Exists(sourceFile))
            {
                File.Delete(filePath);
                Log($"Deleted: {filePath}", logFilePath);
            }
        }

        // Repeat for subdirectories
        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(directory);
            string destDir = Path.Combine(replicaDir, dirName);

            SyncDirectories(directory, destDir, logFilePath);
        }

        foreach (string directory in Directory.GetDirectories(replicaDir))
        {
            string dirName = Path.GetFileName(directory);
            string sourceSubDir = Path.Combine(sourceDir, dirName);

            if (!Directory.Exists(sourceSubDir))
            {
                Directory.Delete(directory, true);
                Log($"Deleted directory: {directory}", logFilePath);
            }
        }
    }

    static bool FilesAreEqual(string filePath1, string filePath2)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream1 = File.OpenRead(filePath1))
            using (var stream2 = File.OpenRead(filePath2))
            {
                var hash1 = md5.ComputeHash(stream1);
                var hash2 = md5.ComputeHash(stream2);

                for (int i = 0; i < hash1.Length; i++)
                {
                    if (hash1[i] != hash2[i])
                        return false;
                }

                return true;
            }
        }
    }

    static void Log(string message, string logFilePath)
    {
        Console.WriteLine(message);
        File.AppendAllText(logFilePath, message + Environment.NewLine);
    }
}
