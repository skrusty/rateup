using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using Fclp;
using Newtonsoft.Json;

namespace rateup
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Setup args
            var o = new FluentCommandLineParser<CmdArgs>();
            o.Setup(arg => arg.InputFile)
                .As('i')
                .Required();
            o.Setup(arg => arg.OutputFile)
                .As('o')
                .Required();
            o.Setup(arg => arg.Config)
                .As('c')
                .Required();
            o.Setup(arg => arg.Verbose)
                .As('v');

            var result = o.Parse(args);
            if (result.Errors.Any())
            {
            }
            else
            {
                // Read in configuration file
                var config = JsonConvert.DeserializeObject<RateConfiguration>(File.ReadAllText(o.Object.Config));

                // Open inputfile
                using (TextReader reader = File.OpenText(o.Object.InputFile))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        // Does input file has headers
                        csv.Configuration.HasHeaderRecord = config.InputFormats.HasHeaders;
                        using (var csvW = new CsvWriter(File.CreateText(o.Object.OutputFile)))
                        {
                            // Do we need to write headers to the output file
                            if (config.OutputFormat.HasHeaders)
                            {
                                foreach (var col in config.OutputFormat.Columns.OrderBy(x => x.Id))
                                {
                                    csvW.WriteField(col.Header);
                                }
                                csvW.NextRecord();
                            }

                            // Itterate each row in input file
                            while (csv.Read())
                            {
                                RateCofigurationClass matchClass = null;
                                string matchPattern = string.Empty;
                                // get the match column (the one that contains the patter to match against a class)
                                for (int i = 0; i < config.InputFormats.MatchColumnId.Count(); i++)
                                {
                                    matchPattern = csv.GetField(config.InputFormats.MatchColumnId[i]);
                                    if (!string.IsNullOrEmpty(config.InputFormats.MatchPrefix))
                                        matchPattern = config.InputFormats.MatchPrefix + matchPattern;

                                    // Match to a class
                                    var matchClasses =
                                        config.Classes.Where(x => Regex.Match(matchPattern, x.Pattern).Success);
                                    matchClass = matchClasses.OrderByDescending(x => x.Priority).FirstOrDefault();
                                    if (matchClass != null)
                                    {
                                        // match found, break out of loop
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Failed to find class for {matchPattern}");
                                        continue;
                                    }
                                }

                                // loop through the input columns in the order they are to be output
                                foreach (var col in config.OutputFormat.Columns.OrderBy(x => x.Id))
                                {
                                    object outputValue = null;
                                    
                                    // If the column is set to rate, and has an input column id specified
                                    if (col.Rate && col.InputId.HasValue)
                                    {

                                        decimal val = 0;
                                        if(!decimal.TryParse(csv.GetField<string>(col.InputId.Value), out val))
                                        {
                                            Console.WriteLine($"Input value couldn't be converted to a decimal. Defaulting '{csv.GetField<string>(col.InputId.Value)}' to 0 for match '{matchPattern}'");
                                        }
                                        decimal outputVal = 0;
                                        // Get the expressions defined for this class and if restrictions are defined, apply them
                                        var useExpressions = matchClass.Expressions;
                                        if (col.Restrict != null && col.Restrict.Any())
                                            useExpressions =
                                                useExpressions.Where(x => col.Restrict.Contains(x.Name)).ToList();
                                        foreach (var exp in useExpressions)
                                        {
                                            var expression = config.Expressions.Single(x => x.Name == exp.Name);
                                            outputVal += expression.Evaluate(val, exp.Param);
                                        }
                                        if (o.Object.Verbose)
                                            Console.WriteLine(
                                                $"{matchPattern} rerated with class {matchClass.Name} from {val} to {outputVal}");
                                        outputValue = outputVal;
                                    }
                                    else
                                    {
                                        if (col.InputId != null)
                                        {
                                            // Not to set rate, but input column id has been specified
                                            outputValue = csv.GetField(col.InputId.Value);
                                        }
                                        else if(!string.IsNullOrEmpty(col.DefaultValue))
                                        {
                                            // no input specified, but a default value has been given
                                            outputValue = col.DefaultValue;
                                        }
                                    }

                                    if (outputValue != null)
                                    {

                                        csvW.WriteField(outputValue);
                                    }
                                }
                                // move to the next row for output
                                csvW.NextRecord();
                            }
                        }
                    }
                }
            }
        }

        public class CmdArgs
        {
            public string InputFile { get; set; }
            public string OutputFile { get; set; }
            public string Config { get; set; }
            public bool Verbose { get; set; }
        }
    }
}