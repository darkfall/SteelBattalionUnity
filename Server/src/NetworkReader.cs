/*********************************************************************************
 *   SBC Unity based on SteelBattalion.NET.
 *
 * 	By Ruiwei Bu
 *   darkfall3@gmail.com
 *                                                                               *
 *********************************************************************************/

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MechgicSBC
{
    public class NetworkReader
    {
        public static NetworkReader Begin(NetworkStream stream, IncomingReadHandler readHandler, IncomingReadErrorHandler errorHandler = null)
        {
            return new NetworkReader(stream, readHandler, errorHandler);
        }

        // 4kb
        public const int kBufferSize = 4096;

        public delegate void IncomingReadHandler(NetworkReader read, byte[] data);
        public delegate void IncomingReadErrorHandler(NetworkReader read, Exception exception);

        NetworkStream stream;
        IncomingReadHandler readHandler;
        IncomingReadErrorHandler errorHandler;
        byte[] buffer = new byte[kBufferSize];

        public NetworkStream Stream
        {
            get
            {
                return stream;
            }
        }


        NetworkReader(NetworkStream stream, IncomingReadHandler readHandler, IncomingReadErrorHandler errorHandler = null)
        {
            this.stream = stream;
            this.readHandler = readHandler;
            this.errorHandler = errorHandler;

            BeginReceive();
        }

        void BeginReceive()
        {
            stream.BeginRead(buffer, 0, kBufferSize, OnReceive, stream);
        }

        void OnReceive(IAsyncResult result)
        {
            try
            {
                if (result.IsCompleted)
                {
                    int bytesRead = stream.EndRead(result);
                    if (bytesRead > 0)
                    {
                        byte[] read = new byte[bytesRead];
                        Array.Copy(buffer, 0, read, 0, bytesRead);

                        readHandler(this, read);
                        BeginReceive();
                    }
                    else
                    {
                        errorHandler(this, null);
                    }
                }
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                {
                    errorHandler(this, e);
                }
            }
        }
    }
}
