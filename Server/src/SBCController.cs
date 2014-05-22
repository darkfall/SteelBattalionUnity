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

using System.Threading.Tasks;

using System.Diagnostics;

namespace MechgicSBC
{
    public class SBCController
    {
        SBC.SteelBattalionController controller = null;
        System.Timers.Timer refreshTimer = new System.Timers.Timer();

        TcpListener listener;
        TcpClient client;
        NetworkStream nstream;

        SBCPacketButton buttonState = new SBCPacketButton();

        public int serverPort = 8080;
		public bool started = false;

        public SBC.SteelBattalionController Controller
        {
            get
            {
                return controller;
            }
        }

		public bool TryConnectSBC(int refreshRate)
        {
            try
            {
                controller = new SBC.SteelBattalionController();

                controller.ButtonStateChanged += this.OnSBCButtonStateChanged;
                controller.Init(refreshRate);

                Console.WriteLine("Initialzied SBC");

                refreshTimer.Interval = refreshRate;
                refreshTimer.Elapsed += this.OnRefreshTimeout;
                refreshTimer.Start();
				started = true;
				return true;
            }
            catch (Exception ex)
            {
				Console.WriteLine("SBC: " + ex.Message);
				started = false;
				return false;
            }
        }

		public void Stop()
		{
			if(controller != null)
				controller.UnInit();
			controller = null;
			refreshTimer.Stop ();
			started = false;
		}

        public void StartServer(int port)
        {
            serverPort = port;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, serverPort);
                    listener.Start();

                    Console.WriteLine("Server started..., listening " + serverPort.ToString());

                    while (true)
                    {
                        client = listener.AcceptTcpClient();
                        nstream = client.GetStream();
                        NetworkReader.Begin(nstream, OnReadData, OnReadDataError);

						Console.WriteLine(String.Format("SBC: Received connection from {0}", client.Client.RemoteEndPoint.ToString()));

						while (client != null && client.Connected)
                        {
                        }

                        try
                        {
                            client.Close();
                        }
                        catch
                        {

                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Unable to listen to 127.0.0.1:" + serverPort.ToString() + ", " + ex.ToString());

                }
            });
        }

        byte GetButtonState(bool state, bool changed)
        {
            byte result = state ? (byte)SBCButtonState.Down : (byte)SBCButtonState.Up;
            if (changed)
                result |= (byte)SBCButtonState.Changed;
            return result;
        }

        void OnReadData(NetworkReader reader, byte[] data)
        {
            SBCPacketType t = (SBCPacketType)data[0];
            byte[] packetData = data.Skip(1).Take(data.Length - 1).ToArray();
			try {
	            switch (t) {
	                case SBCPacketType.FlashLed:
	                    SBCPacketFlashLed pfl = SBCUtility.Deserialize(SBCPacketType.FlashLed, packetData) as SBCPacketFlashLed;
	                    controller.flashLED((SBC.ControllerLEDEnum)pfl.ledButton, pfl.numberOfTime);
	                    break;

	                case SBCPacketType.SetLedState:
	                    SBCPacketSetLedState psl = SBCUtility.Deserialize(SBCPacketType.SetLedState, packetData) as SBCPacketSetLedState;
	                    controller.SetLEDState((SBC.ControllerLEDEnum)psl.ledButton, psl.intensity);
	                    break;

	                default:
	                    break;
	            }
			} catch(Exception ex) {
				Console.WriteLine ("SBC Error: " + ex.Message);
			}
        }

        void OnReadDataError(NetworkReader reader, Exception ex)
        {

        }

        void OnSBCButtonStateChanged(SBC.SteelBattalionController controller, SBC.ButtonState[] states)
        {

            if (states == null)
                return;

            foreach (SBC.ButtonState state in states)
            {
                if (state == null)
                    continue;

				buttonState.buttons[(int)state.button] = GetButtonState(state.currentState, state.changed);
            }
        }

        void Send(byte[] packet)
        {
			try
			{
				if (client != null && client.Connected)
    	        {
        	        nstream.Write(packet, 0, packet.Length);
	            }
				else
				{
					client = null;
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine ("Error sending data: " + ex.Message);
				client = null;
			}
        }

        void OnRefreshTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {
			refreshTimer.Stop ();

			controller.CheckStatesChangedPub ();
            if (client != null && client.Connected)
            {
                SBCPacketJoystick js = new SBCPacketJoystick();
                js.AimingX = controller.AimingX;
                js.AimingY = controller.AimingY;
                js.GearLever = controller.GearLever;
                js.LeftPedal = controller.LeftPedal;
                js.MiddlePedal = controller.MiddlePedal;
                js.RightPedal = controller.RightPedal;
                js.RotationLevel = controller.RotationLever;
                js.SightChangeX = controller.SightChangeX;
                js.SightChangeY = controller.SightChangeY;
                js.TunerDial = controller.TunerDial;
                js.POVhat = (int)controller.POVhat;

                byte[] packet = SBCUtility.Serialize(SBCPacketType.JoystickState, js);
                Send(packet);

                packet = SBCUtility.Serialize(SBCPacketType.ButtonState, buttonState);
                Send(packet);
            }

			refreshTimer.Start ();

        }

    }
}
