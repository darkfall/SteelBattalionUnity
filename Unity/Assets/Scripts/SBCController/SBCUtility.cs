﻿ using System;using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.Reflection;

internal sealed class SBCDeserializeBinder : SerializationBinder
{
	static Dictionary<string, Type> deserializedTypes = new Dictionary<string, Type>();
	
	public override Type BindToType(string assemblyName, string typeName)
	{
		Type ttd = null;
		if (deserializedTypes.TryGetValue (typeName, out ttd)) {
			return ttd;
		}
		
		try
		{
			Assembly[] asmblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly ass in asmblies)
			{
				try
				{
					Type t = ass.GetType(typeName);
					if (t != null)
					{
						ttd = t;
						deserializedTypes[typeName] = t;
						Console.WriteLine(ass.FullName);
						break;
					}
				}
				catch
				{
					continue;
				}
			}
		}
		catch 
		{
		}
		return ttd;
	}
}

// this file can also be used in Unity
// to serialize and deserialize things!
public enum SBCPacketType
{
	// client will receive the following automatically each refresh period
	JoystickState,
	ButtonState,
	LedState,
	
	// client need send to server to do the following
	FlashLed,
	SetLedState,
};

public enum SBCButtonState
{
	Down = 1 << 0,
	Up = 1 << 1,
	Changed = 1 << 2,
};

#region SBC enums

public enum SBCPOVDirection
{
	Center,
	Down,
	Left,
	Right,
	Up,
};

public enum SBCLed
{
	EmergencyEject = 4,
	CockpitHatch = 5,
	Ignition = 6,
	Start = 7,
	OpenClose = 8,
	MapZoomInOut = 9,
	ModeSelect = 10,
	SubMonitorModeSelect = 11,
	MainMonitorZoomIn = 12,
	MainMonitorZoomOut = 13,
	ForecastShootingSystem = 14,
	Manipulator = 15,
	LineColorChange = 16,
	Washing = 17,
	Extinguisher = 18,
	Chaff = 19,
	TankDetach = 20,
	Override = 21,
	NightScope = 22,
	F1 = 23,
	F2 = 24,
	F3 = 25,
	MainWeaponControl = 26,
	SubWeaponControl = 27,
	MagazineChange = 28,
	Comm1 = 29,
	Comm2 = 30,
	Comm3 = 31,
	Comm4 = 32,
	Comm5 = 33,
	//not sure what is missing here
	GearR = 35,
	GearN = 36,
	Gear1 = 37,
	Gear2 = 38,
	Gear3 = 39,
	Gear4 = 40,
	Gear5 = 41,
}

public enum SBCButton
{
	RightJoyMainWeapon,
	RightJoyFire,
	RightJoyLockOn,
	Eject,
	CockpitHatch,
	Ignition,
	Start,
	MultiMonOpenClose,
	MultiMonMapZoomInOut,
	MultiMonModeSelect,
	MultiMonSubMonitor,
	MainMonZoomIn,
	MainMonZoomOut,
	FunctionFSS,
	FunctionManipulator,
	FunctionLineColorChange,
	Washing,
	Extinguisher,
	Chaff,
	FunctionTankDetach,
	FunctionOverride,
	FunctionNightScope,
	FunctionF1,
	FunctionF2,
	FunctionF3,
	WeaponConMain,
	WeaponConSub,
	WeaponConMagazine,
	Comm1,
	Comm2,
	Comm3,
	Comm4,
	Comm5,
	LeftJoySightChange,
	ToggleFilterControl,
	ToggleOxygenSupply,
	ToggleFuelFlowRate,
	ToggleBufferMaterial,
	ToggleVTLocation,
	TunerDialStateChange,
	GearLeverStateChange,

	_Max,
}

#endregion

[Serializable]
public class SBCPacketJoystick
{
	public int AimingX;
	public int AimingY;
	public int GearLever;
	public int LeftPedal;
	public int MiddlePedal;
	public int RightPedal;
	public int RotationLevel;
	public int SightChangeX;
	public int SightChangeY;
	public int TunerDial;
	public int POVhat;
}

[Serializable]
public class SBCPacketButton
{
	public byte[] buttons = new byte[(int)SBCButton._Max];

	public bool GetButtonDown(SBCButton button)
	{
		return buttons[(int)button] == 5;
	}
	
	public bool GetButtonUp(SBCButton button)
	{
		return buttons[(int)button] == 6;
	}
	
	public bool GetButton(SBCButton button)
	{
		return (buttons[(int)button] & (int)SBCButtonState.Down) > 0;
	}

}

[Serializable]
public class SBCPacketSetLedState
{
	public int ledButton;
	public int intensity = 3;
}

[Serializable]
public class SBCPacketFlashLed
{
	public int ledButton;
	public int numberOfTime = 1;
}

public class SBCUtility
{
	public static byte[] Serialize(SBCPacketType type, object data)
	{
		BinaryFormatter formatter = new BinaryFormatter();
		MemoryStream dataStream = new MemoryStream();
		
		try
		{
			formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
			formatter.Serialize(dataStream, data);
			byte[] bytesToWrite = new byte[1 + dataStream.Length];
			bytesToWrite[0] = (byte)type;
			Array.Copy(dataStream.GetBuffer(), 0, bytesToWrite, 1, dataStream.Length);
			
			return bytesToWrite;
		}
		catch
		{
			return null;
		}
	}
	
	public static object Deserialize(SBCPacketType type, byte[] data)
	{
		BinaryFormatter formatter = new BinaryFormatter();
		MemoryStream dataStream = new MemoryStream(data);
		
		formatter.Binder = new SBCDeserializeBinder();
		try
		{
			return formatter.Deserialize(dataStream);
		}
		catch(System.Exception ex)
		{
			#if UNITY_EDITOR
			UnityEngine.Debug.Log(ex);
			#endif
			return null;
		}
	}

}