using System;
using System.IO;
using System.Collections.Generic;
using static DcimIngester.Utilities;

namespace DcimIngester.Rules
{
    public static class ParserTest
    {
        public static void TestParser()
        {
            var source = File.ReadAllText("Rules/rules");
            var parser = new Parser();
            var syntax = parser.Parse(source);
            // var evaluator = new Evaluator("D:/Downloads/Neuer Ordner/Neuer Ordner/DSC_0063.NEF");
            var evaluator = new Evaluator("D:/Videos/SD Card Dump/2022/08/20/DJI_0123.MP4");
            foreach (KeyValuePair<String,Dictionary<String,String>> entry in evaluator.Metadata){
                foreach(KeyValuePair<string, string> subEntry in entry.Value)
                    {
                        Console.WriteLine($"{entry.Key} - {subEntry.Key} = {subEntry.Value}");
                    } 
            }
            Console.WriteLine("==== SYNTAX ====");
            Console.WriteLine(syntax);
            Console.WriteLine(syntax.PrettyPrint());
            Console.WriteLine("==== OUTPUT ====");
            Console.WriteLine(evaluator.Evaluate(syntax));
            // Console.WriteLine(GetDateTaken("D:/Downloads/Neuer Ordner/Neuer Ordner/DSC_0063.NEF"));
            Console.WriteLine(GetDateTaken("D:/Videos/SD Card Dump/2022/08/20/DJI_0123.MP4"));
        }
    }
}
