using System;
using System.IO;
using MetadataExtractor;
using System.Collections.Generic;

namespace DcimIngester.Rules
{
    public static class ParserTest
    {
        public static void TestParser()
        {
            // Dictionary<String,Dictionary<String,String>> metadata = new Dictionary<String,Dictionary<String,String>>();
            // IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata("D:/Photos/SD Card Dump/RAW/2022/08/23/DJI_0158.DNG");
            // foreach (var directory in directories){
            //     metadata[directory.Name] = new Dictionary<String, String>();
            //     foreach (var tag in directory.Tags){
            //         metadata[directory.Name][tag.Name] = tag.Description;
            //         // Console.WriteLine($"{directory.Name} - {directory.Name} = {tag.Description}");
            //     }
                
            // }



            var source = File.ReadAllText("Rules/rules");
            var parser = new Parser();
            var syntax = parser.Parse(source);
            var evaluator = new Evaluator("D:/Downloads/Neuer Ordner/Neuer Ordner/DSC_0063.NEF");// D:/Photos/SD Card Dump/RAW/2022/08/23/DJI_0158.DNG");
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
        }
    }
}
