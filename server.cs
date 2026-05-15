using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Server
{
    static async Task Main(string[] args)
    {
        int port = 12345;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine("[Server] Програму запущено");

        using TcpClient client = await server.AcceptTcpClientAsync();
        Console.WriteLine("[Server] Клієнт підключився!");

        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        // 1 
        /*
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"[Server] Отримано повідомлення: {message}");

            if (message.ToLower() == "бувай")
            {
                server.Stop();
                break;
            }

            Console.WriteLine("[Server] Введіть відповідь: ");
            string response = Console.ReadLine();

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

            if (response.ToLower() == "бувай")
            {
                server.Stop();
                break;
            }
        }
        */ 

        // 2
        char[,] map = new char[3, 3] {
            { '-', '-', '-' },
            { '-', '-', '-' },
            { '-', '-', '-' }
        };

        List<(int, int)> availableMoves = new List<(int, int)> {
            (0, 0), (0, 1), (0, 2),
            (1, 0), (1, 1), (1, 2),
            (2, 0), (2, 1), (2, 2)
        };

        Console.WriteLine("[Server] Введіть хто буде грати: людина(1) чи робот(2)");
        int playerType = int.Parse(Console.ReadLine() ?? "1");

        Console.WriteLine("[Server] Клієнт грає за X...");
        Console.WriteLine("[Server] Чекаємо на ходи клієнта...");

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("[Server] Клієнт розірвав з'єднання.");
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                if (message.Equals("бувай", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Клієнт сказав 'бувай'. Завершення гри.");
                    break;
                }

                List<int> tmp = message.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                int x = tmp[0];
                int y = tmp[1];

                if (map[x, y] == '-')
                {
                    map[x, y] = 'X';
                    availableMoves.Remove((x, y));
                }
                else
                {
                    byte[] responseBytes2 = Encoding.UTF8.GetBytes("Клітинка вже зайнята\n");
                    await stream.WriteAsync(responseBytes2, 0, responseBytes2.Length);
                    continue; 
                }

                ShowMap(map);

                await CheckWinner(map, availableMoves, stream, server);

                int robotX, robotY;
                while (true)
                {
                    List<int> choice = RPChoice(availableMoves, playerType);
                    robotX = choice[0];
                    robotY = choice[1];

                    if (map[robotX, robotY] == '-')
                    {
                        map[robotX, robotY] = 'O';
                        availableMoves.Remove((robotX, robotY));
                        break;
                    }
                    Console.WriteLine("[Server] Ця клітинка вже зайнята! Виберіть іншу.");
                }

                ShowMap(map);
                await CheckWinner(map, availableMoves, stream, server);

                string response = MapToString(map);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Помилка під час гри: {ex.Message}");
        }
        finally
        {
            server.Stop();
            Console.WriteLine("[Server] Роботу сервера успішно завершено. Порт вільний.");
        }
    }

    public static void ShowMap(char[,] map)
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                Console.Write(map[i, j] + " ");
            }
            Console.WriteLine();
        }
    }

    public static List<int> RPChoice(List<(int, int)> availableMoves, int playerType)
    {
        if (playerType == 1)
        {
            Console.WriteLine("[Server] Введіть ваш хід (x,y): ");
            string input = Console.ReadLine() ?? "";
            return input.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        }
        else
        {
            Random rand = new Random();
            var choice = availableMoves[rand.Next(availableMoves.Count)];
            Console.WriteLine($"[Server] Робот вибрав: {choice.Item1},{choice.Item2}");
            return new List<int> { choice.Item1, choice.Item2 };
        }
    }

    public static string MapToString(char[,] map)
    {
        string line = "";
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                line += map[i, j] + " ";
            }
            line += "\n";
        }
        return line;
    }

    public static async Task CheckWinner(char[,] board, List<(int, int)> availableMoves, NetworkStream stream, TcpListener server)
    {
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] != '-' && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
            {
                string res = board[i, 0] == 'X' ? "Виграв клієнт!" : "Виграв сервер!";
                Console.WriteLine(res);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(res));
                server.Stop(); 
                Environment.Exit(0);
            }

            if (board[0, i] != '-' && board[0, i] == board[1, i] && board[1, i] == board[2, i])
            {
                string res = board[0, i] == 'X' ? "Виграв клієнт!" : "Виграв сервер!";
                Console.WriteLine(res);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(res));
                server.Stop();
                Environment.Exit(0);
            }
        }

        if (board[0, 0] != '-' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
        {
            string res = board[0, 0] == 'X' ? "Виграв клієнт!" : "Виграв сервер!";
            Console.WriteLine(res);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(res));
            server.Stop();
            Environment.Exit(0);
        }

        if (board[0, 2] != '-' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
        {
            string res = board[0, 2] == 'X' ? "Виграв клієнт!" : "Виграв сервер!";
            Console.WriteLine(res);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(res));
            server.Stop();
            Environment.Exit(0);
        }

        if (availableMoves.Count == 0)
        {
            Console.WriteLine("Нічия!");
            await stream.WriteAsync(Encoding.UTF8.GetBytes("Нічия"));
            server.Stop();
            Environment.Exit(0);
        }
    }
}
