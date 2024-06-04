using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Data.SqlClient;
using System.Diagnostics;


class Program
{
    static string connectionString = "Server=cnddosdodev03;Database=JobTracker;Trusted_Connection=Yes; TrustServerCertificate=True"; // Update this with your database connection string
    
    static void Main(string[] args)
    {

        string scriptDirectory = Directory.Exists(GetDirectoryName()) ? GetDirectoryName() : Directory.GetCurrentDirectory();
        string[] logFiles = Directory.GetFiles(scriptDirectory, "*.log");
        foreach (var item in logFiles)
        {
            DbMethods.ParseLogFile(item, connectionString);
        }
        var watcher = watch();

        while (true)
        {
            Console.WriteLine($"Currently monitoring directory: {scriptDirectory}");

            Console.WriteLine("1. Press 'c' to change target directory...\n2. Press 'q' to quit...");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            ConsoleKey key = keyInfo.Key;

            switch (key)
            {
                case ConsoleKey.Q:
                    watcher.Dispose();
                    return;

                case ConsoleKey.C:
                    Console.WriteLine("\nEnter new directory to watch: ");
                    String newDirectory = Console.ReadLine();
                    if (Directory.Exists($@"{newDirectory}"))
                    {
                        UpdateDirectoryName(newDirectory);
                        watcher.Dispose();
                        Main(new string[] { });
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"The directory is not valid");
                    }
                    break;
                    
                default:
                    break;
            }
        }
    }

    static FileSystemWatcher watch()
    {
        string scriptDirectory = Directory.Exists(GetDirectoryName()) ? GetDirectoryName() : Directory.GetCurrentDirectory();
        FileSystemWatcher watcher = new FileSystemWatcher(scriptDirectory);
        watcher.Filter = "*.log";
        watcher.IncludeSubdirectories = false;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += new FileSystemEventHandler(OnLogFileChanged);
        watcher.Created += new FileSystemEventHandler(OnLogFileChanged); ; // Subscribe to the Created event
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    static void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[WATCHER] {DateTime.Now}: DETECTED CHANGE IN: {e.FullPath}. PARSING LOGS...");
            Console.ResetColor();
            JobDTO job = DbMethods.ParseLogFile(e.FullPath, connectionString);

            if (job.Name != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"[SUCCESS] {DateTime.Now}: {Path.GetFileName(e.FullPath)} was successfully processed");
                Console.ResetColor();
                DbMethods.UpdateJobStatus(job, connectionString);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {DateTime.Now}: AN ERROR OCCURRED WHILE PROCESSING {Path.GetFileName(e.FullPath)} Jobs name is NULL {job.Name}");
                Console.ResetColor();
            }
        }
    }

    static string GetDirectoryName()
    {
        string filePath = @"TargetDirectory.txt";
        string directoryName = "";

        if (!File.Exists(filePath))
        {
            try
            {
                FileStream fs = File.Create(filePath);
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        try
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "") continue;
                    directoryName = line;
                }
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        return $@"{directoryName}";
    }

    static void UpdateDirectoryName(String newDirectory)
    {
        string filePath = @"TargetDirectory.txt";

        try
        {
            File.WriteAllText(filePath, newDirectory);
            Console.WriteLine("File overwritten successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }
}
