using System;
using System.IO;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            string report = MaskedValuesVerificationSuite.Run();
            Console.WriteLine(report);
            if (args.Length == 2 && args[0] == "--report")
                File.WriteAllText(args[1], report);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
