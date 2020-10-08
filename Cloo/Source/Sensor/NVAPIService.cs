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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenHardwareMonitor.Hardware;

namespace Cloo.Sensor
{
    internal sealed class NVAPIService
    {
        private static readonly Lazy<NVAPIService> lazyInstance = new Lazy<NVAPIService>(() => new NVAPIService());

        private const int MAX_PHYSICAL_GPUS = 64;
        private const int MAX_THERMAL_SENSORS_PER_GPU = 3;
        private const int NVAPI_MAX_GPU_UTILIZATIONS = 8;
        private readonly uint GPU_THERMAL_SETTINGS_VER = (uint)Marshal.SizeOf(typeof(NvGPUThermalSettings)) | 0x10000;
        private readonly uint GPU_DYNAMIC_PSTATES_INFO_EX_VER = (uint)Marshal.SizeOf(typeof(NvDynamicPstatesInfoEx)) | 0x10000;
        private readonly uint GPU_DYNAMIC_PSTATES_INFO_VER = (uint)Marshal.SizeOf(typeof(NvDynamicPstatesInfo)) | 0x10000;
        private readonly List<NvPhysicalGpuHandle> nvPhysicalGpuHandles = new List<NvPhysicalGpuHandle>();
        private int deviceSubscriptionCount = 0;

        public static NVAPIService Instance { get { return lazyInstance.Value; } }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NvDisplayHandle
        {
            private readonly IntPtr ptr;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NvPhysicalGpuHandle
        {
            public bool IsValid { get { return ptr != IntPtr.Zero; } }
            private readonly IntPtr ptr;
        }

        internal enum NvThermalController
        {
            NONE = 0,
            GPU_INTERNAL,
            ADM1032,
            MAX6649,
            MAX1617,
            LM99,
            LM89,
            LM64,
            ADT7473,
            SBMAX6649,
            VBIOSEVT,
            OS,
            UNKNOWN = -1,
        }

        internal enum NvThermalTarget
        {
            NONE = 0,
            GPU = 1,
            MEMORY = 2,
            POWER_SUPPLY = 4,
            BOARD = 8,
            ALL = 15,
            UNKNOWN = -1
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvSensor
        {
            public NvThermalController Controller;
            public uint DefaultMinTemp;
            public uint DefaultMaxTemp;
            public uint CurrentTemp;
            public NvThermalTarget Target;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvGPUThermalSettings
        {
            public uint Version;
            public uint Count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_THERMAL_SENSORS_PER_GPU)]
            public NvSensor[] Sensor;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvUtilizationDomainEx
        {
            public bool Present;
            public int Percentage;
        }

        public enum UtilizationDomain
        {
            GPU,
            FrameBuffer,
            VideoEngine,
            BusInterface
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvDynamicPstatesInfoEx
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NVAPI_MAX_GPU_UTILIZATIONS)]
            public NvUtilizationDomainEx[] UtilizationDomains;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvUtilizationDomain
        {
            public bool Present;
            public int Percentage;
            public ulong Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct NvDynamicPstatesInfo
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NVAPI_MAX_GPU_UTILIZATIONS)]
            public NvUtilizationDomain[] UtilizationDomains;
        }

        private delegate IntPtr nvapi_QueryInterfaceDelegate(uint id);
        private readonly nvapi_QueryInterfaceDelegate nvapi_QueryInterface;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumPhysicalGPUsDelegate([Out] NvPhysicalGpuHandle[] gpuHandles, out int gpuCount);
        private readonly NvAPI_EnumPhysicalGPUsDelegate NvAPI_EnumPhysicalGPUs;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumTCCPhysicalGPUsDelegate([Out] NvPhysicalGpuHandle[] gpuHandles, out int gpuCount);
        private readonly NvAPI_EnumTCCPhysicalGPUsDelegate NvAPI_EnumTCCPhysicalGPUs;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_EnumNvidiaDisplayHandleDelegate(int thisEnum, ref NvDisplayHandle displayHandle);
        private readonly NvAPI_EnumNvidiaDisplayHandleDelegate NvAPI_EnumNvidiaDisplayHandle;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GetPhysicalGPUsFromDisplayDelegate(NvDisplayHandle displayHandle, [Out] NvPhysicalGpuHandle[] gpuHandles, out uint gpuCount);
        private readonly NvAPI_GetPhysicalGPUsFromDisplayDelegate NvAPI_GetPhysicalGPUsFromDisplay;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetBusIdDelegate(NvPhysicalGpuHandle gpuHandle, out uint busId);
        private readonly NvAPI_GPU_GetBusIdDelegate NvAPI_GPU_GetBusId;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_InitializeDelegate();
        private readonly NvAPI_InitializeDelegate NvAPI_Initialize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetThermalSettingsDelegate(NvPhysicalGpuHandle gpuHandle, int sensorIndex, ref NvGPUThermalSettings gpuThermalSettings);
        private readonly NvAPI_GPU_GetThermalSettingsDelegate NvAPI_GPU_GetThermalSettings;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetDynamicPstatesInfoExDelegate(NvPhysicalGpuHandle gpuHandle, ref NvDynamicPstatesInfoEx dynamicPstatesInfoEx);
        private readonly NvAPI_GPU_GetDynamicPstatesInfoExDelegate NvAPI_GPU_GetDynamicPstatesInfoEx;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NvAPI_GPU_GetDynamicPstatesInfoDelegate(NvPhysicalGpuHandle gpuHandle, ref NvDynamicPstatesInfo dynamicPstatesInfo);
        private readonly NvAPI_GPU_GetDynamicPstatesInfoDelegate NvAPI_GPU_GetDynamicPstatesInfo;

        private static string GetLibraryName() { return IntPtr.Size == 4 ? "nvapi" : "nvapi64"; }

        private void GetNvAPIDelegate<T>(uint id, out T newDelegate) where T : class
        {
            IntPtr ptr = nvapi_QueryInterface(id);
            newDelegate = ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)) as T : null;
        }

        private NVAPIService()
        {
            DllImportAttribute attribute = new DllImportAttribute(GetLibraryName());
            attribute.CallingConvention = CallingConvention.Cdecl;
            attribute.PreserveSig = true;
            attribute.EntryPoint = "nvapi_QueryInterface";
            PInvokeDelegateFactory.CreateDelegate(attribute, out nvapi_QueryInterface);

            GetNvAPIDelegate(0x0150E828, out NvAPI_Initialize);
            NvAPI_Initialize();
            GetNvAPIDelegate(0xE5AC921F, out NvAPI_EnumPhysicalGPUs);
            GetNvAPIDelegate(0xD9930B07, out NvAPI_EnumTCCPhysicalGPUs);
            GetNvAPIDelegate(0x9ABDD40D, out NvAPI_EnumNvidiaDisplayHandle);
            GetNvAPIDelegate(0x34EF9506, out NvAPI_GetPhysicalGPUsFromDisplay);
            GetNvAPIDelegate(0x1BE0B8E5, out NvAPI_GPU_GetBusId);
            GetNvAPIDelegate(0xE3640A56, out NvAPI_GPU_GetThermalSettings);
            GetNvAPIDelegate(0x60DED2ED, out NvAPI_GPU_GetDynamicPstatesInfoEx);
            GetNvAPIDelegate(0x189A1FDF, out NvAPI_GPU_GetDynamicPstatesInfo);

            {
                NvPhysicalGpuHandle[] physicalGpuHandles = new NvPhysicalGpuHandle[MAX_PHYSICAL_GPUS];
                if (NvAPI_EnumPhysicalGPUs(physicalGpuHandles, out int physicalGpuCount) == 0)
                {
                    nvPhysicalGpuHandles.AddRange(physicalGpuHandles.Where((handle,i) => i < physicalGpuCount && handle.IsValid).ToList());
                }

                NvPhysicalGpuHandle[] TCCPhysicalGpuHandles = new NvPhysicalGpuHandle[MAX_PHYSICAL_GPUS];
                if (NvAPI_EnumTCCPhysicalGPUs(TCCPhysicalGpuHandles, out int TCCPhysicalGpuCount) == 0)
                {
                    nvPhysicalGpuHandles.AddRange(TCCPhysicalGpuHandles.Where((handle, i) => i < TCCPhysicalGpuCount && handle.IsValid).ToList());
                }
            }
        }

        ~NVAPIService()
        {
        }

        public int SubscribeDevice()
        {
            return deviceSubscriptionCount < nvPhysicalGpuHandles.Count ? deviceSubscriptionCount++ : -1;
        }

        public void UnsubscribeDevice()
        {
            if (deviceSubscriptionCount > 0)
                --deviceSubscriptionCount;
        }

        public string GetPciBusId(int deviceIndex)
        {
            if (deviceIndex >= nvPhysicalGpuHandles.Count)
                return null;

            NvAPI_GPU_GetBusId(nvPhysicalGpuHandles[deviceIndex], out uint busId);
            return "0000:" + busId.ToString("X2") + ":00.0";
        }

        public float GetSensorValue(int deviceIndex, SensorType sensorType)
        {
            NvPhysicalGpuHandle physicalHandle = nvPhysicalGpuHandles[deviceIndex];
            switch (sensorType)
            {
                case SensorType.GFX_LOAD:
                    if (NvAPI_GPU_GetDynamicPstatesInfoEx != null)
                    {
                        NvDynamicPstatesInfoEx infoEx = new NvDynamicPstatesInfoEx();
                        infoEx.Version = GPU_DYNAMIC_PSTATES_INFO_EX_VER;
                        infoEx.UtilizationDomains = new NvUtilizationDomainEx[NVAPI_MAX_GPU_UTILIZATIONS];
                        return NvAPI_GPU_GetDynamicPstatesInfoEx(physicalHandle, ref infoEx) == 0 && infoEx.UtilizationDomains[0].Present ? infoEx.UtilizationDomains[0].Percentage/100.0f : 0;
                    }
                    if (NvAPI_GPU_GetDynamicPstatesInfo != null)
                    {
                        NvDynamicPstatesInfo info = new NvDynamicPstatesInfo();
                        info.Version = GPU_DYNAMIC_PSTATES_INFO_VER;
                        info.UtilizationDomains = new NvUtilizationDomain[NVAPI_MAX_GPU_UTILIZATIONS];
                        return NvAPI_GPU_GetDynamicPstatesInfo(physicalHandle, ref info) == 0 && info.UtilizationDomains[0].Present ? info.UtilizationDomains[0].Percentage : 0;
                    }
                    break;
                case SensorType.GFX_TEMPERATURE:
                    if (NvAPI_GPU_GetThermalSettings != null)
                    {
                        NvGPUThermalSettings thermalSettings = new NvGPUThermalSettings();
                        thermalSettings.Version = GPU_THERMAL_SETTINGS_VER;
                        thermalSettings.Count = MAX_THERMAL_SENSORS_PER_GPU;
                        thermalSettings.Sensor = new NvSensor[MAX_THERMAL_SENSORS_PER_GPU];
                        
                        return NvAPI_GPU_GetThermalSettings(physicalHandle, (int)NvThermalTarget.ALL, ref thermalSettings) == 0 ?
                            Array.Find(thermalSettings.Sensor, sensor => sensor.Target == NvThermalTarget.GPU).CurrentTemp : 0;
                    }
                    break;
                case SensorType.GFX_POWER:
                    break;
            }
            return 0;
        }
    }
}
