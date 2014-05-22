/*********************************************************************************
 *   SBC Unity based on SteelBattalion.NET.
 *
 * 	By Ruiwei Bu
 *   darkfall3@gmail.com
 *                                                                               *
 *********************************************************************************/

﻿using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Linq;
using System.Collections;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class TcpConnectionHandler : MonoBehaviour
{

    protected TcpClient client = null;
    protected NetworkStream nstream = null;
    protected NetworkReader nreader = null;

    private System.Object thisLock = new System.Object();

    public virtual void OnConnectionFailed(string reason)
    {

    }

    public virtual void OnConnected(System.Net.Sockets.TcpClient client)
    {

    }

    public virtual void OnDataReceived(SBCPacketType t, byte[] data)
    {

    }

    public virtual void OnDataWritten()
    {

    }

    public virtual void OnNetworkError(System.Exception exp)
    {

    }

    public void Connected(TcpClient c)
    {
        Loom.QueueOnMainThread(() =>
        {
            client = c;
            nstream = c.GetStream();

            nreader = NetworkReader.Begin(nstream, OnReaderReceive, OnReaderError);
            OnConnected(c);
        });
    }

    public void Send(byte[] data)
    {
        if (client == null)
        {
            Debug.Log("No client available");
            return;
        }
        try
        {
            // lock until actually written to prevent deadlock
            System.Threading.Monitor.Enter(thisLock);
            nstream.BeginWrite(data, 0, data.Length, onDataWritten, client);

        }
        catch
        {
            this.OnNetworkError(new Exception("Disconnected"));
        }
    }

    void OnReaderReceive(NetworkReader reader, byte[] data)
    {
        Loom.QueueOnMainThread(() =>
        {
            SBCPacketType t = (SBCPacketType)data[0];
            OnDataReceived(t, data.Skip(1).Take(data.Length).ToArray());
        });
    }

    void OnReaderError(NetworkReader reader, System.Exception exception)
    {
        if (TcpConnection.currentStatus == ConnectionStatus.Disconnected)
            return;

        Loom.QueueOnMainThread(() =>
        {
			OnNetworkError(new Exception("Disconnected"));
			StopAllCoroutines();
        });
    }

    void onDataWritten(System.IAsyncResult result)
    {
        try
        {
            nstream.EndWrite(result);
            Loom.QueueOnMainThread(() =>
            {
                OnDataWritten();
            });
        }
        catch
        {
            Loom.QueueOnMainThread(() =>
            {
                OnNetworkError(new Exception("Disconnected"));
            });
            return;
        }
        finally
        {
            System.Threading.Monitor.Exit(thisLock);
        }
    }

    public IEnumerator Heartbeat()
    {
        while (client != null && client.Connected)
        {
            //this.Send(MechgicSBC.SBCPacketType.Heartbeat, null);
            // wait one sec
            yield return new WaitForSeconds(1.0f);
        }
    }

	public void Disconnect() {
		TcpConnection.Disconnect();
		client = null;
	}

}
