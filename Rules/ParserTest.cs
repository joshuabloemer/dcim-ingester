using System;
using System.IO;

namespace DcimIngester.Rules
{
    public static class ParserTest
    {
        public static void TestParser()
        {
            var source = File.ReadAllText("Rules/rules");
            var parser = new Parser();
            var syntax = parser.Parse(source);
            var evaluator = new Evaluator("test");
            Console.WriteLine("==== SYNTAX ====");
            Console.WriteLine(syntax);
            Console.WriteLine(syntax.PrettyPrint());
            Console.WriteLine("==== OUTPUT ====");
            Console.WriteLine(evaluator.Evaluate(syntax));
        }
    }
}
