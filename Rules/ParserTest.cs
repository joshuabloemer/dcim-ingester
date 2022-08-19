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
            var result = parser.Parse(source);
            Console.WriteLine(result);
        }
    }
}
