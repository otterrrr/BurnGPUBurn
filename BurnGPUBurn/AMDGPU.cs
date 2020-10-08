using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BurnGPUBurn
{
    class AMDGPU
    {
        protected const string libName = "atiadlxx";
        public const int ADL_PMLOG_MAX_SENSORS = 256;
        private IntPtr context = IntPtr.Zero;

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

        private delegate IntPtr ADL_MemAlloc_Delegate(int size);

        [DllImport(libName, EntryPoint = "ADL_Main_Control_Create")]
        private static extern int ADL_Main_Control_Create(ADL_MemAlloc_Delegate alloc, int enumConnectedAdapters);

        [DllImport(libName, EntryPoint = "ADL2_Main_Control_Create")]
        private static extern int ADL2_Main_Control_Create(ADL_MemAlloc_Delegate alloc, int enumConnectedAdapters, out IntPtr context);

        [DllImport(libName, EntryPoint = "ADL_Main_Control_Destroy")]
        private static extern int ADL_Main_Control_Destroy();

        [DllImport(libName, EntryPoint = "ADL2_Main_Control_Destroy")]
        private static extern int ADL2_Main_Control_Destroy(IntPtr context);

        [DllImport(libName, EntryPoint = "ADL_Adapter_NumberOfAdapters_Get")]
        private static extern int ADL_Adapter_NumberOfAdapters_Get(ref int num_adapters);

        [DllImport(libName, EntryPoint = "ADL_Adapter_AdapterInfo_Get")]
        private static extern int ADL_Adapter_AdapterInfo_Get(IntPtr info, int size);

        [DllImport(libName, EntryPoint = "ADL2_New_QueryPMLogData_Get")]
        public static extern int ADL2_New_QueryPMLogData_Get(IntPtr context, int adapterIndex, ref ADLPMLogDataOutput aDLPMLogDataOutput);

        public AMDGPU()
        {
            int status = 0;
            status = ADL_Main_Control_Create(Marshal.AllocHGlobal, 1);
            status = ADL2_Main_Control_Create(Marshal.AllocHGlobal, 1, out context);
        }

        ~AMDGPU()
        {
            int status = 0;
            status = ADL_Main_Control_Destroy();
            status = ADL2_Main_Control_Destroy(context);
        }

        
    }
}
