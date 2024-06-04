using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Data.SqlClient;
using System.Globalization;
public static class DbMethods
{
    public static int DataSourceId { get; set; } = 1;
    public static JobDTO ParseLogFile(string logFilePath, string connectionString)
    {
        //String para guardar el log de error
        List<string> currentErrorMessage = new List<string>();
        JobDTO job = new JobDTO
        {
            RunStatusId = 1
        };
        List<DateTime> datetimeList = new List<DateTime>();


        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[PROCESS] {DateTime.Now}: Processing log file: |{Path.GetFileName(logFilePath)}|");
        Console.ResetColor();

        try
        {
            using (FileStream stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream))
            {
                DateTime _lastRun = DateTime.Now;
                string line;
                job.LastOutcomeMessage = $"Log File Name: |{Path.GetFileName(logFilePath)}| ";
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line == "") continue;

                    Match match = Regex.Match(line, @"^(\w+):");
                    if (line.ToLower().Contains("error"))
                    {
                        job.RunStatusId = 0;
                    }

                    currentErrorMessage.Add(line);
                    //Regex processName
                    match = Regex.Match(line, @"/job:""(.*?)""");
                    if (match.Success)
                    {
                        string processName = match.Groups[1].Value;
                        job.Name = processName;
                    }
                    if (job.RunStatusId == 0)
                    {
                        string logMessage = string.Join(Environment.NewLine, line);
                        job.LastOutcomeMessage = job.LastOutcomeMessage + " | " + logMessage;
                    }
                    //Regex para obtener fechas y agregarlas a la lista de todas las fechas
                    match = Regex.Match(line, @"\b(\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\b");
                    if (match.Success)
                    {
                        datetimeList.Add(DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd HH:mm:ss", null));
                    }
                }
                if (job.RunStatusId == 1)
                {
                    job.LastOutcomeMessage = job.LastOutcomeMessage + "Success. ";
                }
                datetimeList.Sort();
                job.LastExecution = datetimeList.FirstOrDefault();
                job.EndDateTime = datetimeList.LastOrDefault();
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now}: {e.Message}");
            Console.ResetColor();
        }
        SaveLogDataToJson(logFilePath, job);
        return job;
    }

    public static bool UpdateJobStatus(JobDTO job, string connectionString)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Jobs SET RunStatusId = @RunStatusId, LastExecution = @LastExecution, LastOutcomeMessage = @LastOutcomeMessage, LastUpdate = @LastUpdate, EndDateTime = @EndDateTime, DataSourceId = @DataSourceId WHERE Name = @Name";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", job.Name);
                command.Parameters.AddWithValue("@RunStatusId", job.RunStatusId + 1);
                command.Parameters.AddWithValue("@EndDateTime", job.EndDateTime == null ? null : job.EndDateTime);
                command.Parameters.AddWithValue("@LastExecution", job.LastExecution == null ? null : job.LastExecution);
                command.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                command.Parameters.AddWithValue("@DataSourceID", DataSourceId);
                command.Parameters.AddWithValue("@LastOutcomeMessage", job.LastOutcomeMessage.Length > 700 ? job.LastOutcomeMessage.Substring(0, 700) : job.LastOutcomeMessage);
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    SaveLogDataToDatabase(job, connectionString);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[SUCCESS] {DateTime.Now}: {job.Name} was successfully updated on the database");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now}: {e.Message} \n {e.StackTrace}");
            Console.ResetColor();
            return false;
        }
        return true;
    }

    static void SaveLogDataToDatabase(JobDTO job, string connectionString)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Jobs (Name, RunStatusId, LastExecution, LastOutcomeMessage, Enabled, PriorityId, LastUpdate, DataSourceId, EndDateTime) VALUES (@Name, @RunStatusId, @LastExecution, @LastOutcomeMessage, @Enabled, @PriorityId, @LastUpdate, @DataSourceId, @EndDateTime)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", job.Name);
                command.Parameters.AddWithValue("@RunStatusId", job.RunStatusId + 1);
                command.Parameters.AddWithValue("@LastExecution", job.LastExecution);
                command.Parameters.AddWithValue("@EndDateTime", job.EndDateTime);
                //command.Parameters.AddWithValue("@Duration", job.Duration);
                command.Parameters.AddWithValue("@LastUpdate", DateTime.Now);
                command.Parameters.AddWithValue("@LastOutcomeMessage", job.LastOutcomeMessage.Length > 500 ? job.LastOutcomeMessage.Substring(0, 500) : job.LastOutcomeMessage);
                command.Parameters.AddWithValue("@Enabled", 1);
                command.Parameters.AddWithValue("@PriorityId", 2);
                command.Parameters.AddWithValue("@DataSourceId", DataSourceId);
                int rowsAffected = command.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCCESS] {DateTime.Now}: {job.Name} was successfully added to the database");
                Console.ResetColor();
            }

        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now}: {e.Message} \n {e.StackTrace}");
            Console.ResetColor();
        }
    }

    //Delete this, this is for testing purposes. 
    static void SaveLogDataToJson(string logFilePath, JobDTO job)
    {
        try
        {
            string jsonFilePath = Path.ChangeExtension(logFilePath, ".json");
            using (StreamWriter writer = new StreamWriter(jsonFilePath))
            {
                string jsonData = JsonConvert.SerializeObject(job, Formatting.Indented);
                writer.Write(jsonData);
                //Console.WriteLine($"Saved JSON file: {jsonFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now}: Failed to save JSON file: {ex.Message} \n {ex.StackTrace}");
        }
    }
}