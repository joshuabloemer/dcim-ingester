using System;

namespace DcimIngester.Rules
{
    public static class ParserTest
    {
        public static void TestParser()
        {
            var parser = new Parser();
            var result = parser.Parse("123as");
            Console.WriteLine(result);
        }
    }
}
