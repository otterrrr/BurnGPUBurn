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
using System.Runtime.InteropServices;
using System.Text;
using OpenHardwareMonitor.Hardware;

namespace Cloo.Sensor
{
    internal sealed class NVMLService
    {
        private static readonly Lazy<NVMLService> lazyInstance = new Lazy<NVMLService>(() => new NVMLService());

        public static NVMLService Instance { get { return lazyInstance.Value; } }

        public bool Initialized { get; }
        private readonly List<NvmlDevice> nvmlDevices = new List<NvmlDevice>();

        private static string GetLibraryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvml");
            return "libnvidia-ml";
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NvmlDevice { private readonly IntPtr ptr; }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlInitDelegate();
        private readonly nvmlInitDelegate nvmlInit;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlInit_v2Delegate();
        private readonly nvmlInit_v2Delegate nvmlInit_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlShutdownDelegate();
        private readonly nvmlShutdownDelegate NvmlShutdown;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlDeviceGetHandleByPciBusIdDelegate(string pciBusId, out NvmlDevice device);
        private readonly nvmlDeviceGetHandleByPciBusIdDelegate NvmlDeviceGetHandleByPciBusId;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlDeviceGetHandleByIndexDelegate(uint index, out NvmlDevice device);
        private readonly nvmlDeviceGetHandleByIndexDelegate NvmlDeviceGetHandleByIndex;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int nvmlDeviceGetPowerUsageDelegate(NvmlDevice device, out int power);
        private readonly nvmlDeviceGetPowerUsageDelegate NvmlDeviceGetPowerUsage;

        private static T CreateDelegate<T>(string entryPoint) where T : Delegate
        {
            var attribute = new DllImportAttribute(GetLibraryName())
            {
                CallingConvention = CallingConvention.Cdecl,
                PreserveSig = true,
                EntryPoint = entryPoint
            };
            PInvokeDelegateFactory.CreateDelegate(attribute, out T newDelegate);
            return newDelegate;
        }

        private NVMLService()
        {
            nvmlInit = CreateDelegate<nvmlInitDelegate>("nvmlInit");
            nvmlInit_v2 = CreateDelegate<nvmlInit_v2Delegate>("nvmlInit_v2");
            NvmlShutdown = CreateDelegate<nvmlShutdownDelegate>("nvmlShutdown");
            NvmlDeviceGetHandleByPciBusId = CreateDelegate<nvmlDeviceGetHandleByPciBusIdDelegate>("nvmlDeviceGetHandleByPciBusId_v2");
            NvmlDeviceGetHandleByIndex = CreateDelegate<nvmlDeviceGetHandleByIndexDelegate>("nvmlDeviceGetHandleByIndex_v2");
            NvmlDeviceGetPowerUsage = CreateDelegate<nvmlDeviceGetPowerUsageDelegate>("nvmlDeviceGetPowerUsage");

            Initialized =
                nvmlInit_v2() == 0 ? true :
                nvmlInit() == 0 ? true : false;

            if (Initialized && NvmlDeviceGetHandleByPciBusId != null)
            {
                for (int deviceIndex = 0; NvmlDeviceGetHandleByPciBusId(NVAPIService.Instance.GetPciBusId(deviceIndex), out NvmlDevice device) == 0; ++deviceIndex)
                {
                    nvmlDevices.Add(device);
                }
            }
        }

        ~NVMLService()
        {
            NvmlShutdown();
        }

        public float? GetSensorValue(int deviceIndex, SensorType sensorType)
        {
            NvmlDevice device = nvmlDevices[deviceIndex];
            switch (sensorType)
            {
                case SensorType.GFX_LOAD:
                    throw new NotImplementedException();
                case SensorType.GFX_TEMPERATURE:
                    throw new NotImplementedException();
                case SensorType.GFX_POWER:
                    if (NvmlDeviceGetPowerUsage != null && NvmlDeviceGetPowerUsage(device, out int power) == 0)
                        return  power*0.001f;
                    break;
            }
            return null;
        }
    }
}
