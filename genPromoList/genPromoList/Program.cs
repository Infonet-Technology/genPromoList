using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;

namespace genPromoList
{
    class Program
    {
        static void Main(string[] args)
        {
            string inifolder = "C:\\C-StoreCommander\\";
            string configFile = "DataPath.ini";

            string OutDir= "C:\\C-StoreCommander\\";
            string OutFile = "output.csv";
            string OutputFile;

            string connectionString = "";
            string procedureName = "SProc_CurrentPriceList";
            //string query = "SELECT * FROM [CSCMaster].[dbo].[PromoHeader] WHERE GetDate() between startdate and enddate"; // Update with your SQL query

            string sectionName = "[DATABASE]";
            Dictionary<string, string> section = GetSectionFromConfig(inifolder, configFile, sectionName);
            if (section != null)
            {
                foreach (var kvp in section)
                {
                    //Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    if (kvp.Key.Substring(0, 2) != "//")
                    {
                        if ((kvp.Key.Trim().ToUpper() == "DATASOURCE") || (kvp.Key.Trim().ToUpper() == "DATA SOURCE"))
                        {
                            if (kvp.Value.IndexOf(";") > 0)
                                connectionString = "Data Source = " + kvp.Value.Substring(0, kvp.Value.IndexOf(";"));
                            else
                                connectionString = "Data Source = " + kvp.Value;
                        }   
                        //if (kvp.Key.ToUpper() == "USERID")
                        //    connectionString += "User ID=" + kvp.Value + ";";
                        //if (kvp.Key.ToUpper() == "PASSWORD")
                        //    connectionString += "Password=" + kvp.Value + ";MultipleActiveResultSets=True";
                    }
                }
            }
            else
            {
                Console.WriteLine("Section [{sectionName}] not found in the configuration file.");
            }

            connectionString += "; Initial Catalog = CSCMaster; Persist Security Info = True; Connection Timeout = 0; User ID = tpsuser; Password = It$*2010";

            sectionName = "[PRICEFILE]";
            section = GetSectionFromConfig(inifolder, configFile, sectionName);
            if (section != null)
            {
                foreach (var kvp in section)
                {
                    //Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    if (kvp.Key.Substring(0, 2) != "//")
                    {
                        if (kvp.Key.ToUpper() == "OUTDIR")
                            OutDir = kvp.Value;
                        if (kvp.Key.ToUpper() == "OUTFILE")
                            OutFile = kvp.Value;
                    }
                }
            }
            else
            {
                Console.WriteLine("Section [{sectionName}] not found in the configuration file.");
                Console.WriteLine("");
                Console.WriteLine("example:");
                Console.WriteLine("");
                Console.WriteLine("[{sectionName}] ");
                Console.WriteLine("OUTDIR=C:\\C-StoreCommander\\");
                Console.WriteLine("OUTFILE=Output.csv");
                Console.WriteLine("");
                Console.WriteLine("Press any key to close this window.");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            OutputFile = OutDir + OutFile;

            try
            {
                // Display running massage
                Console.WriteLine("Please wait while data is generating");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    //using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlCommand command = new SqlCommand(procedureName, connection))
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                // Create a StreamWriter to write to a CSV file
                                using (StreamWriter sw = new StreamWriter(OutputFile))
                                {
                                    // Write column headers
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        sw.Write(reader.GetName(i));
                                        if (i < reader.FieldCount - 1)
                                            sw.Write(",");
                                    }
                                    sw.WriteLine();

                                    // Write data rows
                                    while (reader.Read())
                                    {
                                        Console.Write(".");
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            if (!reader.IsDBNull(i))
                                            {
                                                if (i == 2 || i == 4)
                                                {
                                                    decimal decimalValue;
                                                    decimal parsedDecimal;
                                                    if (Decimal.TryParse(reader.GetValue(i).ToString(), out parsedDecimal))
                                                    {
                                                        decimalValue = parsedDecimal;
                                                    }
                                                    else
                                                    {
                                                        // Handle parsing error or default value as needed
                                                        decimalValue = 0; // Default value if conversion fails
                                                    }
                                                    sw.Write(decimalValue.ToString("0.00"));
                                                }
                                                else
                                                {
                                                    sw.Write(reader.GetValue(i).ToString());
                                                }
                                            }
                                            if (i < reader.FieldCount - 1)
                                                sw.Write(",");
                                        }

                                        sw.WriteLine();
                                    }
                                }
                                Console.WriteLine("");
                                Console.WriteLine("Data exported to output.csv succesffuly!");
                                Console.WriteLine("");
                                var waitTime = 5;
                                Console.WriteLine("Press any key to close this window, otherwise it will automatically close in {0} seconds.", waitTime);
                                
                                //Console.ReadKey(true);

                                var original = DateTime.Now;
                                var newTime = original;

                                var remainingWaitTime = waitTime;
                                var lastWaitTime = waitTime.ToString();
                                var keyRead = false;
                                do
                                {
                                    keyRead = Console.KeyAvailable;
                                    if (!keyRead)
                                    {
                                        newTime = DateTime.Now;
                                        remainingWaitTime = waitTime - (int)(newTime - original).TotalSeconds;
                                        var newWaitTime = remainingWaitTime.ToString();
                                        if (newWaitTime != lastWaitTime)
                                        {
                                            var backSpaces = new string('\b', lastWaitTime.Length);
                                            var spaces = new string(' ', lastWaitTime.Length);
                                            Console.Write(backSpaces + spaces + backSpaces);
                                            lastWaitTime = newWaitTime;
                                            Console.Write(lastWaitTime);
                                            System.Threading.Thread.Sleep(25);
                                        }
                                    }
                                } while (remainingWaitTime > 0 && !keyRead);
                                Environment.Exit(0);
                            }
                            else
                            {
                                Console.WriteLine("No rows found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static Dictionary<string, string> GetSectionFromConfig(string directory, string filename, string sectionName)
        {
            string filePath;
            Dictionary<string, string> section = new Dictionary<string, string>();

            bool foundSection = false;

            filePath = directory + filename;
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith(sectionName))
                        {
                            foundSection = true;
                            continue;
                        }

                        if (foundSection && line.Trim().StartsWith("["))
                        {
                            // Reached another section, so break
                            break;
                        }

                        if (foundSection && !string.IsNullOrWhiteSpace(line))
                        {
                            string[] keyValue = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (keyValue.Length == 2)
                            {
                                section[keyValue[0].Trim()] = keyValue[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while reading the file: {ex.Message}");
                return null;
            }

            return section.Count > 0 ? section : null;
        }
    }
}
