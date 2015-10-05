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
                        if (config.InputFormats.HasHeaders)
                            csv.Configuration.HasHeaderRecord = true;
                        using (var csvW = new CsvWriter(File.CreateText(o.Object.OutputFile)))
                        {
                            // Itterate each row in input file
                            while (csv.Read())
                            {
                                // get the match column (the one that contains the patter to match against a class)
                                var matchPattern = csv.GetField(config.InputFormats.MatchColumnId);
                                if (!string.IsNullOrEmpty(config.InputFormats.MatchPrefix))
                                    matchPattern = config.InputFormats.MatchPrefix + matchPattern;

                                // Match to a class
                                var matchClasses =
                                    config.Classes.Where(x => Regex.Match(matchPattern, x.Pattern).Success);
                                var matchClass = matchClasses.OrderByDescending(x => x.Priority).FirstOrDefault();

                                if (matchClass == null)
                                {
                                    Console.WriteLine($"Failed to find class for {matchPattern}");
                                    continue;
                                }

                                // loop through the input columns in the order they are to be output
                                foreach (var col in config.InputFormats.Columns.OrderBy(x => x.Output.Id))
                                {
                                    if (col.Rate && col.Id.HasValue)
                                    {
                                        var val = csv.GetField<decimal>(col.Id.Value);
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
                                        csvW.WriteField(outputVal);
                                    }
                                    else
                                    {
                                        if (col.Id != null)
                                        {
                                            csvW.WriteField(csv.GetField(col.Id.Value));
                                        }
                                        else if(!string.IsNullOrEmpty(col.Output.DefaultValue))
                                        {
                                            csvW.WriteField(col.Output.DefaultValue);
                                        }
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