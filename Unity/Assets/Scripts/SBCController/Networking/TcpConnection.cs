using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum ConnectionStatus
{
    Disconnected,
    ConnectionFailed,
    WaitingForConnection,
    Connected,
}

public class TcpConnection
{

    public static TcpListener serverListener = null;
    public static TcpClient client = null;

    public static ConnectionStatus currentStatus = ConnectionStatus.Disconnected;
    public static bool isMaster = false;

    public static string lastConnectionStatus = "Not connected";

    static TcpConnectionHandler handler = null;
    static IPAddress ip = null;


    public static IPAddress IP
    {
        get
        {
            if (ip == null)
            {
                ip = (
                    from entry in Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    where entry.AddressFamily == AddressFamily.InterNetwork
                    select entry
                    ).FirstOrDefault();
            }

            return ip;
        }
    }

    public static void Connect(string serverAddr, int port, TcpConnectionHandler h)
    {
        serverListener = null;
        client = new TcpClient();
        handler = h;
        currentStatus = ConnectionStatus.WaitingForConnection;
        isMaster = false;

        client.BeginConnect(serverAddr, port, new System.AsyncCallback(OnServerConnect), client);
    }

	public static void Disconnect() {
		try {
			client.Close();
			client = null;
			currentStatus = ConnectionStatus.Disconnected;
			isMaster = false;
		}
		catch {

		}
	}

    public static void CreateRoom(int port, int speculatorPort, TcpConnectionHandler h)
    {
        serverListener = new TcpListener(IPAddress.Any, port);
        serverListener.Start();
        handler = h;
        isMaster = true;

        currentStatus = ConnectionStatus.WaitingForConnection;
        serverListener.BeginAcceptTcpClient(new System.AsyncCallback(TcpConnection.OnClientConnect), serverListener);
    }

    public static void EndConnection(string reason)
    {
        handler = null;
        currentStatus = ConnectionStatus.Disconnected;
        try
        {
            if (client != null)
            {
                client.Close();
            }
        }
        catch
        {

        }
        try
        {
            if (serverListener != null)
            {
                serverListener.Stop();
            }
        }
        catch
        {

        }
        finally
        {
            client = null;
            serverListener = null;
            currentStatus = ConnectionStatus.Disconnected;
            lastConnectionStatus = reason;
        }
    }

    public static void OnClientConnect(System.IAsyncResult result)
    {
        try
        {
            client = serverListener.EndAcceptTcpClient(result);
        }
        catch
        {
            currentStatus = ConnectionStatus.ConnectionFailed;
            lastConnectionStatus = "Connection failed";
            handler.OnConnectionFailed(lastConnectionStatus);
            return;
        }
        currentStatus = ConnectionStatus.Connected;
        lastConnectionStatus = "Client Connected";
        handler.Connected(client);
    }

    public static void OnServerConnect(System.IAsyncResult result)
    {
        try
        {
            client.EndConnect(result);
        }
        catch
        {
            currentStatus = ConnectionStatus.ConnectionFailed;
            lastConnectionStatus = "Connection failed";
            handler.OnConnectionFailed(lastConnectionStatus);
            return;
        }
        currentStatus = ConnectionStatus.Connected;
        lastConnectionStatus = "Server Connected";
        handler.Connected(client);
    }

}