using NLog;

namespace Equipment2;

internal class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Logger.Info("テスト");
        Logger.Warn("テスト");
        Logger.Error("テスト");
    }
}
