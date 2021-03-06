﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WarlockTCPServer.NetworkClasses;

namespace WarlockTCPServer.Managers
{
    public static class NetworkManager
    {
        public static List<Client> Clients { get; set; }
        public static List<Packet> Packets { get; set; }

        private static int _bufferSize = 1024 * 4;
        private static int _maxPlayers = 2;
        private static int _port = 28852;
        private static IPAddress _ipAddress = IPAddress.Any;
        private static TcpListener _listener;

        private static int _timeStep = 30;
        public static bool Running { get; set; } = false;

        public static void Start()
        {
            Clients = new List<Client>();
            Packets = new List<Packet>();

            _listener = new TcpListener(_ipAddress, _port);

            _listener.Start();
            GameManager.Setup(); // Might break
            FindClients();
            Run();
        }

        public static void FindClients()
        {
            Console.WriteLine("Waiting for clients...");

            while (Clients.Count != _maxPlayers)
            {

                if (_listener.Pending())
                {
                    var client = _listener.AcceptTcpClient();
                    client.SendBufferSize = _bufferSize;
                    client.ReceiveBufferSize = _bufferSize;
                    var newClient = new Client(Clients.Count.ToString(), client);
                    Clients.Add(newClient);
                    SendHello(newClient.PlayerId, client);
                }

                Thread.Sleep(_timeStep);
            }

            if (Clients.Count == _maxPlayers)
            {
                GameManager.SetupNewGame();
            }
        }

        public static void SendHello(string playerId, TcpClient client)
        {
            Packet helloPacket = new Packet
            {
                PlayerId = playerId,
                CommandId = (short)CommandId.hello
            };

            Console.WriteLine($"Sending Hello to {playerId}");

            SendPacket(client, helloPacket);
        }

        public static void Run()
        {
            Running = true;

            while (Running)
            {
                ReceivePackets();
                CheckForDisconnects();
                GameManager.RunGameLoop();
                Thread.Sleep(_timeStep);
            }

            OnShutdown();
        }

        private static void OnShutdown()
        {
            DisconnectClientAll();
            _listener.Stop();
        }

        public static void ReceivePackets()
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                var client = Clients[i].TcpClient;
                if (client.Available > 0)
                {
                    byte[] incoming = new byte[client.Available];
                    client.GetStream().Read(incoming, 0, incoming.Length);

                    string incomingStr = Encoding.UTF8.GetString(incoming);

                    Packet packet = JsonConvert.DeserializeObject<Packet>(incomingStr);
                    Console.WriteLine(packet.POCOJson);

                    Packets.Add(packet);
                }
            }
        }

        public static void SendPacket(TcpClient tcpClient, Packet packet)
        {
            string jsonStr = JsonConvert.SerializeObject(packet);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonStr);
            tcpClient.GetStream().Write(buffer, 0, buffer.Length);
            Console.WriteLine("Packet Was Sent");
        }

        public static void SendPacketsAll(Packet packet)
        {
            foreach (var client in Clients)
            {
                SendPacket(client.TcpClient, packet);
            }
        }

        private static void CheckForDisconnects()
        {
            foreach (var client in Clients)
            {
                if (!client.TcpClient.Connected)
                {
                    DisconnectClient(client);
                }
            }
        }
        private static void DisconnectClientAll()
        {
            foreach (var client in Clients)
            {
                DisconnectClient(client);
            }
        }

        private static void DisconnectClient(Client client)
        {
            Clients.Remove(client);
            var tcpClient = client.TcpClient;
            tcpClient.GetStream().Close();
            tcpClient.Close();
        }

    }
}
