/*********************************************************************************
 *   SBC Unity based on SteelBattalion.NET.
 *
 * 	By Ruiwei Bu
 *   darkfall3@gmail.com
 *                                                                               *
 *********************************************************************************/

﻿using System;
using System.Runtime;
using System.Reflection;
using System.IO;

namespace SBCController
{
	class MainClass
	{
		static System.Timers.Timer refreshTimer = new System.Timers.Timer();
        static MechgicSBC.SBCController controller = new MechgicSBC.SBCController();
        static int serverPort = 8080;
        static int refreshRate = 33;

        public static void Main (string[] args)
		{
            AppDomain.CurrentDomain.AssemblyResolve += (sender, a) =>
            {
                string dllName = a.Name.Contains(",") ? a.Name.Substring(0, a.Name.IndexOf(',')) : a.Name.Replace(".dll", "");
                dllName = dllName.Replace(".", "_");

                if (dllName.EndsWith("_resources")) return null;
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager("SBCController.Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());

                byte[] bytes = (byte[])rm.GetObject(dllName);

                return System.Reflection.Assembly.Load(bytes);
            };

			if (args.Length >= 1)
			{
				refreshRate = Int32.Parse(args[0]);
			}
			if (args.Length >= 2)
			{
				serverPort = Int32.Parse(args[1]);
			}

			Console.WriteLine (string.Format("Preparing SBC controller server, refresh rate = {0}, port = {1}", refreshRate, serverPort));

            // start server first
            controller.StartServer(serverPort);

			if (!controller.TryConnectSBC (refreshRate)) {
				refreshTimer.Interval = 1000;
				refreshTimer.Elapsed += new System.Timers.ElapsedEventHandler (testTimer_Elapsed);
				refreshTimer.Start ();
			}

            while (true) { }
		}

		static void testTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			refreshTimer.Stop ();
            if (!controller.TryConnectSBC(refreshRate))
            {
				refreshTimer.Start ();
			}
		}
	}
}
