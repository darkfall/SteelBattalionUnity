### SBC Unity

SBCUnity is a simple plugin to use the amazing SteelBattalion Controller within Unity3D. Its based on part of the nice [SteelBattalion.NET](http://steelbattalionnet.codeplex.com/) library written by Joseph Coutcher and the Loom script from [UnityGems](http://unitygems.com/threads/).

The plugin is developed as part of the #mechgic project, which is a MOBA-like mech game based on the SteelBattalion Controller and Oculus Rift VR. 

The plugin is consist of two parts - the server that will grab data from the Steelbattalion controller and the Unity3D client that will receive data from the server. 

Its not a native plugin because I want to make it more universal than Unity Pro only.

### Special Requirements

On OS X, mono runtime is required (assumed /usr/bin/mono, but you can change it in the source code [Unity/SBCController.cs]).

On Windows, please follow the [tutorials](http://steelbattalionnet.codeplex.com/SourceControl/latest#readme.txt) from SteelBattalion.Net to install the requried drivers.

### How to 

* Compile the server, a pre compiled binary for Mac OS X and Windows is provided in _Server/bin_. 

* Add the scripts to Unity and also add a SBCController component to one of your gameobject. The SBCController component is a singleton object so make sure you only have one copy of it in your scene.

* Put the binaries under your Assets folder (you can make your own folder) in Unity3D, and change the _Server Binary Dir_ option in the SBCController component to the folder where you put the server binaries. (For example, if the server binaries are put in Assets/SBCController/, then set _Server Binary Dir_ to "SBCController/").

* Use SBCController.instance.RunSBCServer/StopSBCServer to run/stop the SBCServer. When the server is running, it will automatically looking for SBCControllers and grabbing data from it.

* Use SBCController.instance.buttonStates/joystickStates to access the controller states. 

* A editor script is also provided for testing.

### How to light the LEDs

	SBCPacketSetLedState packet = new SBCPacketSetLedState ();
    // which LED
    packet.ledButton = (int)SBCLed.NightScope;
    // 0 - 15
    packet.intensity = 15;
    SBCController.instance.Send (SBCUtility.Serialize (SBCPacketType.SetLedState, packet));


### Notes

* When unity crashes, the SBC server may keep running in your system. You have to kill the SBCController process manually using the Windows TaskManager or OS X activily monitor.

* Currenly there are limitations dealing with button state changes. The changed event may not be captured correctly. 

### License

SBC Unity. Copyright (C) 2014 Ruiwei Bu.

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.



__Part of the source code come from SteelBattalion.Net.__

SteelBattalion.NET - A library to access the Steel Battalion XBox controller.  Written by Joseph Coutcher.

SteelBattalion.NET is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.  

SteelBattalion.NET is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.  

You should have received a copy of the GNU General Public License along with SteelBattalion.NET.  If not, see <http://www.gnu.org/licenses/>. 
