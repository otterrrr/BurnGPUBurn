/*

Copyright (c) 2020 Taesik Yoon

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using OpenHardwareMonitor.Hardware;

namespace Cloo.Sensor
{
    internal sealed class ADLService
    {
        private static readonly Lazy<ADLService> lazyInstance = new Lazy<ADLService>(() => new ADLService());

        private const int ATI_VENDOR_ID = 0x1002;
        private const int ADL_MAX_PATH = 256;
        private const int ADL_PMLOG_MAX_SENSORS = 256;
        
        private readonly IntPtr context = IntPtr.Zero;
        private readonly ADLAdapterInfo[] adapterInfoList;
        private readonly List<int> overdriveVersionList = new List<int>();
        private int deviceSubscriptionCount = 0;

        public static ADLService Instance { get { return lazyInstance.Value; } }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLAdapterInfo : IEquatable<ADLAdapterInfo>
        {
            public int Size;
            public int AdapterIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string UDID;
            public int BusNumber;
            public int DeviceNumber;
            public int FunctionNumber;
            public int VendorID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string AdapterName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DisplayName;
            public int Present;
            public int Exist;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPath;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string DriverPathExt;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ADL_MAX_PATH)]
            public string PNPString;
            public int OSDisplayIndex;

            public bool Equals(ADLAdapterInfo other)
            {
                return BusNumber == other.BusNumber && DeviceNumber == other.DeviceNumber;
            }
        }

        private readonly int[] PMLogSensorMap = { 19, 8, 30 };
        private readonly int[] ODNSensorMap = { -1, 1, 0 };

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLSingleSensorData
        {
            public bool Supported;
            public int Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLPMLogDataOutput
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ADL_PMLOG_MAX_SENSORS)]
            public ADLSingleSensorData[] Sensors;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLTemperature
        {
            public int Size;
            public int Temperature;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ADLPMActivity
        {
            public int Size;
            public int EngineClock;
            public int MemoryClock;
            public int Vddc;
            public int ActivityPercent;
            public int CurrentPerformanceLevel;
            public int CurrentBusSpeed;
            public int CurrentBusLanes;
            public int MaximumBusLanes;
            public int Reserved;
        }

        private delegate IntPtr ADL_MemAlloc_Delegate(int size);

        private delegate int ADL_Main_Control_CreateDelegate(ADL_MemAlloc_Delegate callback, int enumConnectedAdapters);
        private readonly ADL_Main_Control_CreateDelegate ADL_Main_Control_Create;
        
        private delegate int ADL2_Main_Control_CreateDelegate(ADL_MemAlloc_Delegate callback, int enumConnectedAdapters, out IntPtr context);
        private readonly ADL2_Main_Control_CreateDelegate ADL2_Main_Control_Create;
        
        private delegate int ADL_Main_Control_DestroyDelegate();
        private readonly ADL_Main_Control_DestroyDelegate ADL_Main_Control_Destroy;

        private delegate int ADL2_Main_Control_DestroyDelegate(IntPtr context);
        private readonly ADL2_Main_Control_DestroyDelegate ADL2_Main_Control_Destroy;

        private delegate int ADL_Adapter_AdapterInfo_GetDelegate(IntPtr info, int size);
        private readonly ADL_Adapter_AdapterInfo_GetDelegate ADL_Adapter_AdapterInfo_Get;

        private delegate int ADL_Adapter_NumberOfAdapters_GetDelegate(ref int numAdapters);
        private readonly ADL_Adapter_NumberOfAdapters_GetDelegate ADL_Adapter_NumberOfAdapters_Get;

        private delegate int ADL_Overdrive_CapsDelegate(int adapterIndex, out int supported, out int enabled, out int version);
        private readonly ADL_Overdrive_CapsDelegate ADL_Overdrive_Caps;

        private delegate int ADL2_New_QueryPMLogData_GetDelegate(IntPtr context, int adapterIndex, out ADLPMLogDataOutput aDLPMLogDataOutput);
        private readonly ADL2_New_QueryPMLogData_GetDelegate ADL2_New_QueryPMLogData_Get;

        private delegate int ADL2_OverdriveN_Temperature_GetDelegate(IntPtr context, int adapterIndex, int temperatureType, out int temperature);
        private readonly ADL2_OverdriveN_Temperature_GetDelegate ADL2_OverdriveN_Temperature_Get;

        private delegate int ADL_Overdrive5_Temperature_GetDelegate(int adapterIndex, int thermalControllerIndex, ref ADLTemperature temperature);
        private readonly ADL_Overdrive5_Temperature_GetDelegate ADL_Overdrive5_Temperature_Get;

        private delegate int ADL2_Overdrive6_CurrentPower_GetDelegate(IntPtr context, int adapterIndex, int powerType, out int currentValue);
        private readonly ADL2_Overdrive6_CurrentPower_GetDelegate ADL2_Overdrive6_CurrentPower_Get;

        private delegate int ADL_Overdrive5_CurrentActivity_GetDelegate(int adapterIndex, ref ADLPMActivity activity);
        private readonly ADL_Overdrive5_CurrentActivity_GetDelegate ADL_Overdrive5_CurrentActivity_Get;

        private static string getLibraryName() { return IntPtr.Size == 4 ? "atiadlxy" : "atiadlxx"; }

        private static void GetDelegate<T>(string entryPoint, out T newDelegate) where T : class
        {
            DllImportAttribute attribute = new DllImportAttribute(getLibraryName());
            attribute.CallingConvention = CallingConvention.Cdecl;
            attribute.PreserveSig = true;
            attribute.EntryPoint = entryPoint;
            PInvokeDelegateFactory.CreateDelegate(attribute, out newDelegate);
        }

        private ADLService()
        {
            GetDelegate("ADL_Main_Control_Create", out ADL_Main_Control_Create);
            GetDelegate("ADL2_Main_Control_Create", out ADL2_Main_Control_Create);
            GetDelegate("ADL_Main_Control_Destroy", out ADL_Main_Control_Destroy);
            GetDelegate("ADL2_Main_Control_Destroy", out ADL2_Main_Control_Destroy);
            GetDelegate("ADL_Adapter_NumberOfAdapters_Get", out ADL_Adapter_NumberOfAdapters_Get);
            GetDelegate("ADL_Adapter_AdapterInfo_Get", out ADL_Adapter_AdapterInfo_Get);
            GetDelegate("ADL_Overdrive_Caps", out ADL_Overdrive_Caps);
            GetDelegate("ADL2_New_QueryPMLogData_Get", out ADL2_New_QueryPMLogData_Get);
            GetDelegate("ADL2_OverdriveN_Temperature_Get", out ADL2_OverdriveN_Temperature_Get);
            GetDelegate("ADL_Overdrive5_Temperature_Get", out ADL_Overdrive5_Temperature_Get);
            GetDelegate("ADL2_Overdrive6_CurrentPower_Get", out ADL2_Overdrive6_CurrentPower_Get);
            GetDelegate("ADL_Overdrive5_CurrentActivity_Get", out ADL_Overdrive5_CurrentActivity_Get);

            int status = 0;
            status = ADL_Main_Control_Create(Marshal.AllocHGlobal, 1);
            status = ADL2_Main_Control_Create(Marshal.AllocHGlobal, 1, out context);

            int numberOfAdapters = 0;
            ADL_Adapter_NumberOfAdapters_Get(ref numberOfAdapters);
            if (numberOfAdapters > 0)
            {
                ADLAdapterInfo[] adapterInfo = new ADLAdapterInfo[numberOfAdapters];
                int elemSize = Marshal.SizeOf(typeof(ADLAdapterInfo));
                int byteSize = adapterInfo.Length * elemSize;
                IntPtr ptr = Marshal.AllocHGlobal(byteSize);
                status = ADL_Adapter_AdapterInfo_Get(ptr, byteSize);
                for (int i = 0; i < adapterInfo.Length; ++i)
                {
                    adapterInfo[i] = (ADLAdapterInfo)Marshal.PtrToStructure((IntPtr)((long)ptr + i * elemSize), typeof(ADLAdapterInfo));
                    Match m = Regex.Match(adapterInfo[i].UDID, "PCI_VEN_([\\w\\d]{1,4})");
                    if (m.Success && m.Groups.Count == 2)
                        adapterInfo[i].VendorID = Convert.ToInt32(m.Groups[1].Value, 16);
                }
                Marshal.FreeHGlobal(ptr);
                adapterInfoList = new HashSet<ADLAdapterInfo>(adapterInfo.Where(info => info.VendorID == ATI_VENDOR_ID)).ToArray();
                overdriveVersionList = adapterInfoList.Select(info => { ADL_Overdrive_Caps(info.AdapterIndex, out _, out _, out int overdriveVersion); return overdriveVersion; }).ToList();
            }
        }

        ~ADLService()
        {
            int status = 0;
            status = ADL2_Main_Control_Destroy(context);
            status = ADL_Main_Control_Destroy();
        }

        public int SubscribeDevice()
        {
            return deviceSubscriptionCount < adapterInfoList.Length ? deviceSubscriptionCount++ : -1;
        }

        public void UnsubscribeDevice()
        {
            if (deviceSubscriptionCount > 0)
                --deviceSubscriptionCount;
        }

        public float? GetSensorValue(int deviceIndex, SensorType sensorType)
        {
            int overdriveVersion = overdriveVersionList[deviceIndex];

            if (overdriveVersion >= 8)
            {
                int sensorIndex = (int)PMLogSensorMap[(int)sensorType];
                if (ADL2_New_QueryPMLogData_Get(context, adapterInfoList[deviceIndex].AdapterIndex, out var data) < 0
                    || sensorIndex >= data.Sensors.Length
                    || !data.Sensors[sensorIndex].Supported)
                {
                    return null;
                }
                return data.Sensors[sensorIndex].Value;
            }

            switch (sensorType)
            {
                case SensorType.GFX_LOAD:
                    ADLPMActivity ADLPMact = new ADLPMActivity();
                    if (overdriveVersion >= 5 && ADL_Overdrive5_CurrentActivity_Get(deviceIndex, ref ADLPMact) == 0)
                        return ADLPMact.ActivityPercent/100.0f;
                    break;
                case SensorType.GFX_TEMPERATURE:
                    if (overdriveVersion >= 7 && ADL2_OverdriveN_Temperature_Get(context, deviceIndex, PMLogSensorMap[(int)sensorType], out int temperature) == 0)
                        return temperature * 0.001f;
                    ADLTemperature ADLTemp = new ADLTemperature();
                    if (ADL_Overdrive5_Temperature_Get(deviceIndex, 0, ref ADLTemp) == 0)
                        return  ADLTemp.Temperature * 0.001f;
                    break;
                case SensorType.GFX_POWER:
                    if (overdriveVersion >= 6 && ADL2_Overdrive6_CurrentPower_Get(context, deviceIndex, ODNSensorMap[(int)sensorType], out int power) == 0)
                        return power * (1.0f / 0xff);
                    break;
            }

            return null;
        }
    }
}
