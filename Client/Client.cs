using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public static class Client
    {
        static TcpClient client;
        static NetworkStream stream;

        public static void Start()
        {
            Console.WriteLine("===CHAT-CLIENT===");
            Console.Write("WRITE A SERVER IP: ");
            string serverIP = Console.ReadLine();

            client = new TcpClient();
            try
            {
                client.Connect(serverIP, 8888);
                stream = client.GetStream();
                Console.WriteLine("CONNECTED TO THE SERVER");

                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                SendMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        public static void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (client.Connected)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\n{message}");
                    Console.Write("[YOU]: ");
                }
                catch
                {
                    Console.WriteLine("\nDisconnected from server.");
                    break;
                }
            }
        }

        public static void SendMessages()
        {
            while (client.Connected)
            {
                Console.Write("[YOU]: ");
                string message = Console.ReadLine();

                if (string.IsNullOrEmpty(message))
                    continue;

                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                }
                catch
                {
                    Console.WriteLine("Failed to send message.");
                    break;
                }
            }
        }
    }
}