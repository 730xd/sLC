using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
   public static class Client
    {
        static TcpClient client;
        static NetworkStream stream;
        
        public static void Start()
        {
            Console.WriteLine("===CHAT-CLIENT===");
            Console.WriteLine("WRITE A SERVER IP:");
            string serverIP = Console.ReadLine();

            client = new TcpClient();
            try
            {
                client.Connect(serverIP, 8888);
                stream = client.GetStream();
                Console.WriteLine("CONNECTED TO THE SERVER");
                Thread thread = new Thread(ReceiveMessages);
                thread.Start();
                SendMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR:{ex.Message}");

            }
        }
        public static void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int btread = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, btread);
                    Console.WriteLine($"[CLIENT]:{message}");
                    Console.WriteLine("[YOU]:");
                }
                catch { break; }
            }
        }
        public static void SendMessages()
        {
            while (true)
            {
                Console.Write("[YOU]:");
                string message = Console.ReadLine();
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }
    }
}
