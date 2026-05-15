using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

class SimpleUdpRadio
{
    static async Task Main()
    {
        string serverIp = "127.0.0.1"; 
        int port = 12345;
        
        using TcpClient client = new TcpClient();
        /*
        client.Connect(serverIp, port);
        
        using NetworkStream stream = client.GetStream();

        
        while(true)
        {
            Console.WriteLine("Введіть відповідь: ");
            string message = Console.ReadLine();

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"[Server]: {response}");

            if (message.ToLower().Equals("бувай") || response.ToLower().Equals("бувай"))
            {
                client.GetStream().Close();
                stream.Close();
            }
        }
        */

        try
        {
            Console.WriteLine("[Client] Підключення до сервера хрестиків-нуликів...");
            await client.ConnectAsync(serverIp, port);
            Console.WriteLine("[Client] Підключено успішно!");

            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            Console.WriteLine("[Client] Гра почалася! Ви граєте за 'X'.");
            Console.WriteLine("Формат ходу: рядок,стовпець (наприклад: 1,1 або 0,2)");

            while (true)
            {

                Console.Write("\nВаш хід (x,y): ");
                string move = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(move)) continue;

                byte[] moveBytes = Encoding.UTF8.GetBytes(move);
                await stream.WriteAsync(moveBytes, 0, moveBytes.Length);

                if (move.ToLower().Trim() == "бувай")
                {
                    Console.WriteLine("[Client] Ви вийшли з гри.");
                    break;
                }

                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("[Client] Сервер розірвав з'єднання (можливо, гра закінчилась).");
                    break;
                }

                string serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (serverResponse.Contains("Виграв") || serverResponse.Contains("Нічия"))
                {
                    Console.WriteLine($"\n[КІНЕЦЬ ГРИ] Фінал від сервера: {serverResponse}");
                    break;
                }

                if (serverResponse.Contains("Клітинка вже зайнята"))
                {
                    Console.WriteLine($"[Попередження]: {serverResponse}");
                    continue;
                }

                Console.WriteLine("\nПоточна карта від сервера:");
                Console.WriteLine(serverResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Помилка: {ex.Message}");
        }

        Console.WriteLine("\nНатисніть Enter для виходу...");
        Console.ReadLine();
    }
}