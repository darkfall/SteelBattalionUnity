/*********************************************************************************
 *   SteelBattalion.NET - A library to access the Steel Battalion XBox           *
 *   controller.  Written by Joseph Coutcher.                                    *
 *                                                                               *
 *   This file is part of SteelBattalion.NET                                     *
 *                                                                               *
 *   SteelBattalion.NET is free software: you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by        *
 *   the Free Software Foundation, either version 3 of the License, or           *
 *   (at your option) any later version.                                         *
 *                                                                               *
 *   SteelBattalion.NET is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of              *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               *
 *   GNU General Public License for more details.                                *
 *                                                                               *
 *   You should have received a copy of the GNU General Public License           *
 *   along with SteelBattalion.NET.  If not, see <http://www.gnu.org/licenses/>. *
 *                                                                               *
 *   While this code is licensed under the LGPL, please let me know whether      *
 *   you're using it.  I'm always interested in hearing about new projects,      *
 *   especially ones that I was able to make a contribution to...in the form of  *
 *   this library.                                                               *
 *                                                                               *
 *   EMail: geekteligence at google mail                                         *
 *                                                                               *
 *   2010-11-26: JC - Initial commit                                             *
 *                                                                               * 
 *********************************************************************************/

using System;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Text;
using System.Text.RegularExpressions;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.Threading;//used for backgroundworker


namespace SBC
{
    public enum POVdirection
    {
        CENTER,
        LEFT,
        RIGHT,
        UP,
        DOWN
    }
        

    public struct LightProperties
    {
        public ControllerLEDEnum LED;
        public bool lightOnHold;//light comes on when button is pressed
        public int intensity;//light intensity
        public LightProperties(ControllerLEDEnum a, bool b,int c)
        {
            LED = a;
            lightOnHold = b;
            intensity = c;
        }
         
    }

    public struct FlashLEDParams
    {
        public ControllerLEDEnum LightId;
        public int numberOfTimes;
        public FlashLEDParams(ControllerLEDEnum a,int b)
        {
            LightId = a;
            numberOfTimes = b;
        }
    }

	/// <summary>
	/// Description of SteelBattalionController.
	/// </summary>
	public class SteelBattalionController {
		#region Public and Private variables
		private DateTime LastDataEventDate = DateTime.Now;
		private UsbDevice MyUsbDevice;
        private Hashtable ButtonLights = new Hashtable();
        private Hashtable ButtonKeys = new Hashtable();
        private bool updateGearLights = true;
        private int gearLightIntensity = 3;
        public ButtonState[] stateChangedArray;

        public delegate void ButtonStateChangedDelegate(SteelBattalionController controller, ButtonState[] arr);
		public event ButtonStateChangedDelegate ButtonStateChanged;

        public delegate void RawDataDelegate(SteelBattalionController controller, byte[] arr);
		public event RawDataDelegate RawData;
        public POVdirection POVhat = POVdirection.CENTER;

		UsbEndpointReader reader = null;
		UsbEndpointWriter writer = null;
		
		System.Timers.Timer pollTimer = new System.Timers.Timer();
		
		// The USB device finder that looks for the Steel Batallion controller
		private UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x0A7B, 0xD000);

		/// <summary>
		/// The byte buffer that the raw control data is stored
		/// </summary>
		public byte[] rawControlData = new Byte[26];
		
		/// <summary>
		/// The byte buffer that the raw LED data is stored
		/// </summary>
		byte[] rawLEDData = new Byte[34];
		#endregion
		
		/// <summary>
		/// Constructor for the controller.  Does nothing at the moment.
		/// </summary>
		public SteelBattalionController() {
		}

        public void setGearLights(bool update,int intensity)
        {
            updateGearLights = update;
            gearLightIntensity = intensity;
        }

        //this only sets the gearLights, you still have to refreshlights
        private void GearLightsRefresh(int gearValue)
        {
         /*	LED VALUES
	        Gear5 = 41,
	        Gear4 = 40,
	        Gear3 = 39,
	        Gear2 = 38,
	        Gear1 = 37,
	        GearN = 36,
	        GearR = 35,*/
        	
	        for(int i=35;i<=41;i++)
		        SetLEDState((ControllerLEDEnum)(i),0,false);//turn all off

	        //int gearValue = GearLever;//returns values -2,-1,1,2,3,4,5
	        if (gearValue < 0)
		        SetLEDState((ControllerLEDEnum)((int) ControllerLEDEnum.Gear1+gearValue),gearLightIntensity,false);
	        else
                SetLEDState((ControllerLEDEnum)((int)ControllerLEDEnum.GearN + gearValue), gearLightIntensity, false);
        }


        public void AddButtonLightMapping(ButtonEnum button, bool lightOnHold, int intensity)
        {
            int buttonEquivalent = (int)button;
            ControllerLEDEnum light = (ControllerLEDEnum)GetLightForButton(button);
            AddButtonLightMapping(button, light, lightOnHold, intensity);
        }

        public void AddButtonLightMapping(ButtonEnum button, ControllerLEDEnum LED,bool lightOnHold,int intensity)
        {
            /*if (!ButtonLights.ContainsKey(button))
                ButtonLights.Add((int)button, new LightProperties(LED, lightOnHold, intensity));
            else*/
            if (ButtonLights.Contains((int)button))
                ButtonLights.Remove((int)button);//to save on later garbage collection
            ButtonLights[(int)button] = new LightProperties(LED, lightOnHold, intensity);
            
        }

        private ControllerLEDEnum GetLightForButton(ButtonEnum button)
        {
            int buttonNumber =(int)button;
            if( buttonNumber >= 3 && buttonNumber <33)
            {
                return (ControllerLEDEnum)(int)button + 1;
            }
            else
                throw new System.ArgumentException("Button not a lit button");
                
        }

		
		/// <summary>
		/// Sets the intensity of the specified LED in the buffer, and sends the buffer to the controller.
		/// </summary>
		/// <param name="LightId">A ControllerLEDEnum value that specifies which LED to modify</param>
		/// <param name="Intensity">The intensity of the LED, ranging from 0 to 15</param>
		public void SetLEDState(ControllerLEDEnum LightId, int Intensity) {
			SetLEDState(LightId, Intensity, true);
		}

		/// <summary>
		/// Sets the intensity of the specified LED in the buffer, but gives the option on whether you want
		/// to send the buffer to the controller.  This can be useful for updating multiple LED's at the
		/// same time, but not waiting for the LED buffer to transfer to the device after each call.
		/// </summary>
		/// <param name="LightId">A ControllerLEDEnum value that specifies which LED to modify</param>
		/// <param name="Intensity">The intensity of the LED, ranging from 0 to 15</param>
		/// <param name="refreshState">A boolean value indicating whether to refresh the buffer on the device.</param>
		public void SetLEDState(ControllerLEDEnum LightId, int Intensity, bool refreshState) {
			int hexPos = ((int) LightId) % 2;
			int bytePos = (((int) LightId) - hexPos) / 2;
			
			if (Intensity > 0x0f) Intensity = 0x0f;
			
			// Erase the byte position, and set the light intensity
			rawLEDData[bytePos] &= (byte) ((hexPos == 1)?0x0F:0xF0);
			rawLEDData[bytePos] += (byte) (Intensity * ((hexPos == 1)?0x10:0x01));
			
			if (refreshState) {
				RefreshLEDState();
			}
		}
		
		/// <summary>
		/// Refreshes the LED buffer/state on the controller
		/// </summary>
		public void RefreshLEDState() {
			ErrorCode ec = ErrorCode.None;
			int bytesWritten;
			
			ec = writer.Write(rawLEDData, 1000, out bytesWritten);
			if (ec != ErrorCode.None) throw new Exception(UsbDevice.LastErrorString);
		}

		/// <summary>
		/// Retrieves the LED state from the internal buffer.  This does not return the actual
		/// intensity from the controller itself.  But, if this is the only library accessing
		/// the device, you can assume that the LED state is the same as what's in the buffer.
		/// </summary>
		/// <param name="LightId"></param>
		/// <returns></returns>
		public int GetLEDState(ControllerLEDEnum LightId) {
			int hexPos = ((int) LightId) % 2;
			int bytePos = (((int) LightId) - hexPos) / 2;
			int returnValue = (((int) rawLEDData[bytePos]) & ((hexPos == 1)?0xF0:0x0F)) / ((hexPos == 1)?0x10:0x01);
            return returnValue;
		}

		/// <summary>
		/// Initializes the device via LibUSB, and sets the refresh interval of the controller data
		/// </summary>
		/// <param name="Interval">The interval that the device is polled for information.</param>
		public void Init(int Interval) {
			//ErrorCode ec = ErrorCode.None;

			// Find and open the usb device.
			MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
			if (MyUsbDevice == null) throw new Exception("Device Not Found.");

			IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
			if (!ReferenceEquals(wholeUsbDevice, null)) {
				wholeUsbDevice.SetConfiguration(1);
				wholeUsbDevice.ClaimInterface(0);
			}
			
			reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
			writer = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

            int readByteCount = 0;
            byte[] buf = new byte[64];
            reader.Read(buf, 0, 64, 1000, out readByteCount);


			ButtonMasks.InitializeMasks();

//			SetPollingInterval(Interval);
//			pollTimer.Elapsed += new ElapsedEventHandler(pollTimer_Elapsed);
//			pollTimer.Start();

			TestLEDs();
            if (updateGearLights)
                GearLightsRefresh((int)unchecked((sbyte)buf[25]));
			RefreshLEDState();

		}

        private void flashLED_helper(ControllerLEDEnum LightId, int numberOfTimes)
        {
            for (int j = 0; j < numberOfTimes; j++)
            {
                for (int intensity = 0; intensity <= 0x0f; intensity++)
                    SetLEDState(LightId, intensity, true);
                for (int intensity = 0x0f; intensity >= 0; intensity--)
                    SetLEDState(LightId, intensity, true);
            }
            

        }
        //used when creating thread
        private void flashLED_worker(object input)
        {
            FlashLEDParams parameters = (FlashLEDParams) input;
            int numberOfTimes = parameters.numberOfTimes;
            ControllerLEDEnum LightId = parameters.LightId;
            flashLED_helper(LightId, numberOfTimes);
        }

        //trying to make this method multi-threaded, will return to this later.
        public void flashLED(ControllerLEDEnum LightId,int numberOfTimes)
        {
            FlashLEDParams inputParams = new FlashLEDParams(LightId,numberOfTimes);
            Thread workerThread = new Thread(this.flashLED_worker);
            //workerThread.Start(inputParams);
            flashLED_helper(LightId, numberOfTimes);
		}

        public void TestLEDs()
        {
            TestLEDs(5);
        }
		
		/// <summary>
		/// lights 5 times...just as a sanity check to make sure I coded all of the enumerator values for the LED's :-)
		/// </summary>
		public void TestLEDs(int frequency) 
        {
			for (int j = 0; j < frequency; j++) {
				for (int intensity = 0; intensity <= 0x0f; intensity++) {
					foreach(string value in Enum.GetNames(typeof(ControllerLEDEnum))) {
						ControllerLEDEnum LightId = (ControllerLEDEnum) Enum.Parse(typeof(ControllerLEDEnum), value);
						
						int hexPos = ((int) LightId) % 2;
						int bytePos = (((int) LightId) - hexPos) / 2;
						
						if (intensity > 0x0f) intensity = 0x0f;
						
						rawLEDData[bytePos] &= (byte) ((hexPos == 1)?0x0F:0xF0);
						rawLEDData[bytePos] += (byte) (intensity * ((hexPos == 1)?0x10:0x01));
					}
					
					RefreshLEDState();
				}
				for (int intensity = 0x0f; intensity >= 0; intensity--) {
					foreach(string value in Enum.GetNames(typeof(ControllerLEDEnum))) {
						ControllerLEDEnum LightId = (ControllerLEDEnum) Enum.Parse(typeof(ControllerLEDEnum), value);
						
						int hexPos = ((int) LightId) % 2;
						int bytePos = (((int) LightId) - hexPos) / 2;
						
						if (intensity > 0x0f) intensity = 0x0f;
						
						rawLEDData[bytePos] &= (byte) ((hexPos == 1)?0x0F:0xF0);
						rawLEDData[bytePos] += (byte) (intensity * ((hexPos == 1)?0x10:0x01));
					}
					
					RefreshLEDState();
				}
			}
            if (updateGearLights)
                GearLightsRefresh(GearLever);
            RefreshLEDState();
		}

		/// <summary>
		/// When the poll timer elapses, this function retrieves data from the controller, and passes
		/// the raw data to both the raw data event (so applications can analyze the raw data if needed),
		/// and passes the data to the private CheckStateChanged function.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>
		/// Please note - the poll timer is stopped until all events have been processed...so
		/// keep your event handlers optimized for analyzing the controller data.
		/// </remarks>
		private void pollTimer_Elapsed(object sender, ElapsedEventArgs e) {
			pollTimer.Stop();
			int readByteCount = 0;
			byte[] buf = new byte[64];
			reader.Read(buf, 0, 64, 1000, out readByteCount);
			
			if (this.RawData != null) {
				RawData(this,buf);
			}
			
			CheckStateChanged(buf);
            Array.Copy(buf, 0, rawControlData, 0, readByteCount);

            //moved this section out of CheckStateChanged since we want this to go off after the data from buf has been copied over to rawControlData
			if ((Enum.GetValues(typeof(ButtonEnum)).Length > 0) && (this.ButtonStateChanged != null))
				ButtonStateChanged(this,stateChangedArray);
			

			//Console.WriteLine(ConvertToHex(rawControlData, rawControlData.Length));
			pollTimer.Start();
		}

		public void CheckStatesChangedPub()
        {
            int readByteCount = 0;
            byte[] buf = new byte[64];
            reader.Read(buf, 0, 64, 1000, out readByteCount);

            if (this.RawData != null)
            {
                RawData(this, buf);
            }

            try
            {
                CheckStateChanged(buf);
            }
            catch
            {

            }
          
            Array.Copy(buf, 0, rawControlData, 0, readByteCount);

            //moved this section out of CheckStateChanged since we want this to go off after the data from buf has been copied over to rawControlData
            if ((Enum.GetValues(typeof(ButtonEnum)).Length > 0) && (this.ButtonStateChanged != null))
                ButtonStateChanged(this, stateChangedArray);

        }
        /// <summary>
        /// Checks the individual button state
        /// </summary>
        /// <param name="buf">Int value of button enum</param>
        public bool GetButtonState(int button)
        {
            ButtonMasks.ButtonMask mask = ButtonMasks.MaskList[button];

            return ((rawControlData[mask.bytePos] & mask.maskValue) > 0);
        }

        /// <summary>
        /// Checks the individual button state
        /// </summary>
        /// <param name="buf">Int value of button enum</param>
        public bool GetButtonState(SBC.ButtonEnum button)
        {
            return GetButtonState((int)button);
        }

        /// <summary>
        /// Checks if individual button state has changed
        /// </summary>
        /// <param name="buf">Int value of button enum</param>
        public bool GetButtonStateChanged(int button)
        {
            ButtonMasks.ButtonMask mask = ButtonMasks.MaskList[button];

            return isStateChanged(rawControlData, mask.bytePos, mask.maskValue);
        }


		/// <summary>
		/// Checks the button state based on the raw data returned, and if it has, the ButtonStateChanged event is raised
		/// </summary>
		/// <param name="buf">Raw data buffer retrieved from the controller</param>
		private void CheckStateChanged(byte[] buf) {
			ButtonEnum[] values = (ButtonEnum[]) Enum.GetValues(typeof(ButtonEnum));
			stateChangedArray = new ButtonState[values.Length];
            bool updateLights = false;

            for (int i = 0; i < values.Length; i++)
            {
                ButtonMasks.ButtonMask mask = ButtonMasks.MaskList[(int)values[i]];

                ButtonState state = new ButtonState();
                state.button = (ButtonEnum)values[i];
                state.currentState = ((buf[mask.bytePos] & mask.maskValue) > 0);
                state.changed = isStateChanged(buf, mask.bytePos, mask.maskValue);
                ButtonEnum currentButton = (ButtonEnum)(i);

                //only do something if button changed, and button was pressed and button is in hashtable
                if (state.changed)
                {
                    if (updateGearLights && state.button == ButtonEnum.GearLeverStateChange)
                    {
                        GearLightsRefresh((int)unchecked((sbyte)buf[25]));//copied this code from GearLever accessor, changed it since we need ot
                        updateLights = true;
                    }
                    //check button - light mapping
                    if (ButtonLights.ContainsKey(i))
                    {
                        updateLights = true;
                        LightProperties currentLightProperties = (LightProperties)(ButtonLights[i]);

                        if (currentLightProperties.lightOnHold)
                            if (state.currentState)
                                SetLEDState(currentLightProperties.LED, currentLightProperties.intensity, false);
                            else
                                SetLEDState(currentLightProperties.LED, 0, false);
                        else
                            if (state.currentState)//only switch when button is pressed
                            {
                                int result = GetLEDState(currentLightProperties.LED);
                                if (result > 0)//light is on
                                    SetLEDState(currentLightProperties.LED, 0);//turn off
                                else
                                    SetLEDState(currentLightProperties.LED, currentLightProperties.intensity);
                            }
                    }
                }


                stateChangedArray[(int)values[i]] = state;
            }

            if (updateLights)
                RefreshLEDState();
		}
		
		/// <summary>
		/// Checks to see if the buton state has changed
		/// </summary>
		/// <param name="buf">The raw data buffer</param>
		/// <param name="bytePos">The byte position to check</param>
		/// <param name="maskValue">The mask value for that button/switch</param>
		/// <returns></returns>
		private bool isStateChanged(byte[] buf, int bytePos, int maskValue) {
			return ((buf[bytePos] & maskValue) != (rawControlData[bytePos] & maskValue));
		}

		// De-initialize the controller
		public void UnInit() {
			// Always disable and unhook event when done.
			//reader.DataReceivedEnabled = false;
			//reader.DataReceived -= (OnRxEndPointData);
			
			pollTimer.Stop();
			pollTimer.Elapsed -= (pollTimer_Elapsed);

			if (MyUsbDevice != null) {
				if (MyUsbDevice.IsOpen) {
					IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
					if (!ReferenceEquals(wholeUsbDevice, null)) {
						// Release interface #0.
						wholeUsbDevice.ReleaseInterface(0);
					}
					MyUsbDevice.Close();
				}
			}
			
			MyUsbDevice = null;
		}
		
		/// <summary>
		/// Allows you to set the polling interval while the controller is initialized
		/// </summary>
		/// <param name="Interval"></param>
		public void SetPollingInterval(int Interval) {
			pollTimer.Interval = Interval;
		}
		
		/// <summary>
		/// Corresponds to the "Rotation Lever" joystick on the left.
		/// </summary>
		public int RotationLever {
            get { return getSignedAxisValue(13, 14); }
        }

		/// <summary>
		/// Corresponds to the "Sight Change" analog stick on the "Rotation Lever" joystick.  X Axis value.
		/// </summary>
		public int SightChangeX {
            get { return getSignedAxisValue(15, 16); }
        }

		/// <summary>
		/// Corresponds to the "Sight Change" analog stick on the "Rotation Lever" joystick.  Y Axis value.
		/// </summary>
		public int SightChangeY {
            get { return getSignedAxisValue(17, 18); }
        }

		/// <summary>
		/// Corresponds to the "Aiming Lever" joystick on the right.  X Axis value.
		/// </summary>
		public int AimingX {
			get {return getAxisValue(9,10);}
		}

		/// <summary>
		/// Corresponds to the "Aiming Lever" joystick on the right.  Y Axis value.
		/// </summary>
		public int AimingY {
            get { return getAxisValue(11, 12); }
		}
        /// <summary>
        /// Used to bitshift array and actually return proper 10-bit value for axis.
        /// </summary>
        private int getAxisValue(int firstIndex, int SecondIndex)
        {
            int temp = rawControlData[firstIndex];
            int temp2 = rawControlData[SecondIndex];
            temp = temp << 2;
            temp2 = temp2 >> 6;
            temp = temp | temp2;
            return temp;
        }

        /// <summary>
        /// Used to bitshift array and actually return proper 10-bit value for axis.
        /// </summary>
        private int getSignedAxisValue(int firstIndex, int SecondIndex)
        {
            uint temp = rawControlData[firstIndex];
            uint temp2 = rawControlData[SecondIndex];
            short result;
            temp = temp << 2;
            temp2 = temp2 >> 6;
            temp = temp | temp2;
            if (rawControlData[firstIndex] >= 128)//we need to pad on some 1's so that we can use 16-bit 2's complement
                temp |= 0xFC00;//0b1111110000000000
            result = unchecked((short)temp);
            return (int)result;
        }

		/// <summary>
		/// Corresponds to the left pedal on the pedal block
		/// </summary>
		public int LeftPedal {
            get { return getAxisValue(19, 20); }
        }

		/// <summary>
		/// Corresponds to the middle pedal on the pedal block
		/// </summary>
		public int MiddlePedal {
            get { return getAxisValue(21, 22); }
        }

		/// <summary>
		/// Corresponds to the right pedal on the pedal block
		/// </summary>
		public int RightPedal {
            get { return getAxisValue(23, 24); }
        }

		/// <summary>
		/// Corresponds to the tuner dial position.  The 9 o'clock postion is 0, and the 6 o'clock position is 12.
		/// The blank area between the 6 and 9 o'clock positions is 13, 14, and 15 clockwise.
		/// </summary>
		public int TunerDial {
			get { return (int) rawControlData[24] & 0x0F; }
		}
		
		/// <summary>
		/// Corresponds to the gear lever on the left block.
		/// </summary>
		public int GearLever {
            get { return (int)unchecked((sbyte)rawControlData[25]); }
		}

		/// <summary>
		/// Function to convert a byte array to a hex string
		/// </summary>
		/// <param name="asciiString">Byte array containing the actual byte values to convert</param>
		/// <param name="count">the count of the bytes to convert</param>
		/// <returns></returns>
		public string ConvertToHex(byte[] asciiString, int count) {
			StringBuilder hex = new StringBuilder();
			int i = 0;
			
			foreach (byte c in asciiString) {
				int tmp = c;
				hex.Append(String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString())));
				i++;
				if(i >= count) break;
			}
			
			return hex.ToString();
		}
        private static string GetIntBinaryString(int n)
        {
            char[] b = new char[8];
            int pos = 7;
            int i = 0;

            while (i < 8)
            {
                if ((n & (1 << i)) != 0)
                {
                    b[pos] = '1';
                }
                else
                {
                    b[pos] = '0';
                }
                pos--;
                i++;
            }
            return new string(b);
        }

        /// <summary>
        /// Function to convert a byte array to a binary representation string
        /// </summary>
        /// <param name="asciiString">Byte array containing the actual byte values to convert</param>
        /// <param name="start">the starting position of the bytes to convert</param>
        /// <param name="end">the end position of the bytes to convert</param>
        /// <returns></returns>
        public string GetBinaryBuffer(int start, int end)
        {
            StringBuilder bin = new StringBuilder();
            int startIndex = start;
            int endIndex    = end;
            if (startIndex < 0)
                startIndex  =   0;
            if (end >= rawControlData.Length)
                endIndex    =   rawControlData.Length;
            for (int i = startIndex; i <= endIndex; i++)
            {
                bin.Append(GetIntBinaryString(rawControlData[i]));
                bin.Append(" ");
            }
            bin.Append("\n");
            return bin.ToString();
        }
    }
}
