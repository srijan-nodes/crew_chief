using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace CrewChiefV4
{
    public static class JsonFiles
    {
        /// <summary>
        /// Whether to strip lines starting with # from the file.
        /// </summary>
        public enum Comments
        {
            ThereAreNone,
            Strip
        }

        public static T ReadFile<T>(string filePath, Comments stripComments = Comments.ThereAreNone, bool dontCatch = false)
        {
            T result = default(T);
            string json = readFile(filePath, stripComments);
            try
            {
                result = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonReaderException ex)
            {
                if (dontCatch)
                {
                    throw ex;
                }
                Log.Error($"Error reading JSON file '{filePath}' : {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Read the JSON file, calling the onException callback if an exception occurs.
        /// </summary>
        /// <param name="onException"></param>
        /// <returns></returns>
        public static T TryReadFile<T>(this FileInfo file, Action<Exception> onException = null) where T : class
        {
            try
            {
                return ReadFile<T>(file.FullName, Comments.ThereAreNone, true);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                return default(T);
            }
        }

        public static bool WriteFile(string filePath, object data)
        {
            bool result = false;
            if (filePath != null)
            {
                try
                {
                    using (StreamWriter file = File.CreateText(filePath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                        serializer.Serialize(file, data);
                        result = true;
                    }
                }
                catch (Exception e)
                {
                    // use C.W instead of Log.Error as writing to log file may not be set up yet
                    Console.WriteLine($"Error writing JSON file '{Path.GetFullPath(filePath)}' : {e.Message}");
                }
            }
            return result;
        }

        private static string readFile(string filePath, Comments stripComments = Comments.ThereAreNone)
        {
            if (filePath == null)
            {
                Log.Error("File path is null");
                return null;
            }
            if (!File.Exists(filePath))
            {
                // use C.W instead of Log.Error as writing to log file may not be set up yet
                Console.WriteLine($"File '{Path.GetFullPath(filePath)}' does not exist");
                return null;
            }
            try
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    StringBuilder sb = new StringBuilder();
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (stripComments == Comments.Strip && line.Trim().StartsWith("#"))
                        {
                            continue;
                        }
                        sb.AppendLine(line);
                    }
                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error reading text file " + filePath + ": ");
                return null;
            }
        }
    }
}