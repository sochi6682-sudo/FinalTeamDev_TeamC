using NLog;

namespace Equipment1;

internal class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        Console.WriteLine("保管設備１起動");
        Logger.Info("保管設備１起動");

        //イニシャル処理

        //IDLE処理

        //RUN処理

        //ALARM処理
    }
}
