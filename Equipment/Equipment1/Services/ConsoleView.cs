using Equipment1.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Equipment1.Services;

public class ConsoleView
{
    private static readonly object _lock = new object();

    public void ShowAlarm(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine("==================================================");
        }
    }


    public void ShowSuccess(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine("==================================================");
        }
    }

    public void ShowInfo(string message)
    {
        lock (_lock)
        {
            Console.WriteLine(message);
            Console.WriteLine("==================================================");
        }
    }

    public void ShowCommand(Command command)
    {
        lock (_lock)
        {
            Console.WriteLine($"搬送指示ID：{command.CommandId}");
            Console.WriteLine($"CarrierID：{command.CarrierId}");
            Console.WriteLine($"搬送指示タイプ：{(command.CommandType == 1 ? "入庫" : "出庫")}");
            Console.WriteLine($"棚：{command.Location}");
            Console.WriteLine("==================================================");
        }
    }

    public void ShowSelectCompleteInput()
    {
        lock (_lock)
        {
            Console.WriteLine("結果を選択してください");
            Console.WriteLine("1 : 正常完了");
            Console.WriteLine("2 : 異常完了");
            Console.WriteLine("==================================================");
        }
    }


}
