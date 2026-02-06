using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SLC.Server
{
    public class Server
    {
        private static TcpListener listener;
        // Список для хранения всех подключенных клиентов
        private static List<TcpClient> connectedClients = new List<TcpClient>();
        // Объект для синхронизации доступа к списку клиентов
        private static readonly object clientsLock = new object();

        public static void Start()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 8888);
                listener.Start();
                Console.WriteLine("SERVER HAS STARTED. Waiting for connections...");

                // Запускаем прием новых клиентов в отдельном потоке
                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        // Метод для принятия новых клиентов
        private static void AcceptClients()
        {
            while (true)
            {
                try
                {
                    // Ждем нового подключения
                    TcpClient client = listener.AcceptTcpClient();

                    // Добавляем клиента в список
                    lock (clientsLock)
                    {
                        connectedClients.Add(client);
                        Console.WriteLine($"New client connected. Total clients: {connectedClients.Count}");
                    }

                    // Запускаем обработку клиента в отдельном потоке
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Accept error: {ex.Message}");
                    break;
                }
            }
        }

        // Метод для обработки отдельного клиента
        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            string clientInfo = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

            Console.WriteLine($"Client {clientInfo} connected.");

            try
            {
                // Поток для отправки сообщений клиенту
                Thread sendThread = new Thread(() => SendMessagesToClient(client, stream));
                sendThread.Start();

                // Получение сообщений от клиента
                while (client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break; // Клиент отключился

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[{clientInfo}]: {message}");

                    // Отправляем сообщение всем остальным клиентам
                    BroadcastMessage($"[{clientInfo}]: {message}", client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientInfo}: {ex.Message}");
            }
            finally
            {
                // Удаляем клиента при отключении
                lock (clientsLock)
                {
                    connectedClients.Remove(client);
                    Console.WriteLine($"Client {clientInfo} disconnected. Total clients: {connectedClients.Count}");
                }
                client.Close();
            }
        }

        // Метод для отправки сообщений клиенту
        private static void SendMessagesToClient(TcpClient client, NetworkStream stream)
        {
            try
            {
                while (client.Connected)
                {
                    // Ввод сообщения от сервера для отправки всем клиентам
                    Console.Write("[SERVER]: ");
                    string message = Console.ReadLine();

                    if (!string.IsNullOrEmpty(message))
                    {
                        byte[] data = Encoding.UTF8.GetBytes($"[SERVER]: {message}");

                        // Отправляем всем клиентам
                        BroadcastMessage($"[SERVER]: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }

        // Отправка сообщения всем клиентам
        private static void BroadcastMessage(string message, TcpClient sender = null)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (clientsLock)
            {
                foreach (var client in connectedClients.ToList())
                {
                    // Не отправляем сообщение отправителю, если он указан
                    if (sender != null && client == sender)
                        continue;

                    try
                    {
                        if (client.Connected)
                        {
                            NetworkStream clientStream = client.GetStream();
                            clientStream.Write(data, 0, data.Length);
                        }
                    }
                    catch
                    {
                        // Клиент может быть отключен
                    }
                }
            }
        }
    }
}