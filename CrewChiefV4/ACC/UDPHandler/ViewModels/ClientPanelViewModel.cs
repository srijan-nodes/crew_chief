using ksBroadcastingNetwork;
using System;

namespace ksBroadcastingTestClient.ClientConnections
{
    internal class ClientPanelViewModel
    {
        public string IP { get; private set; }
        public int Port { get; private set; }
        public string DisplayName { get; private set; }
        public string ConnectionPw { get; private set; }
        public string CommandPw { get; private set; }
        public int RealtimeUpdateIntervalMS { get; private set; }
        public ClientConnectionViewModel ClientConnection { get; private set; }
        public Action<ACCUdpRemoteClient> OnClientConnectedCallback { get; }

        public ClientPanelViewModel(string udpIpAddress, Action<ACCUdpRemoteClient> onClientConnectedCallback)
        {
            IP = udpIpAddress;
            Port = 9000;
            DisplayName = "Your name";
            ConnectionPw = "asd";
            CommandPw = "";
            RealtimeUpdateIntervalMS = 250;
            OnClientConnectedCallback = onClientConnectedCallback;

            var c = new ACCUdpRemoteClient(IP, Port, DisplayName, ConnectionPw, CommandPw, RealtimeUpdateIntervalMS);

            ClientConnection = new ClientConnectionViewModel(c, OnClientConnectedCallback);
        }

        internal void RequestEntryList()
        {
            ClientConnection.Client.RequestEntryList();
        }

        internal void Shutdown()
        {
            if (ClientConnection != null)
            {
                ClientConnection.Client.Shutdown();
                ClientConnection = null;
            }
        }
    }
}
