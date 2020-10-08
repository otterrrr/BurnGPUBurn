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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cloo;

namespace BurnGPUBurn
{
    public partial class MainForm : Form
    {
        internal struct InfoPerDevice
        {
            public InfoPerDevice(Cloo.ComputePlatform inPlatform, Cloo.ComputeDevice inDevice, BlockedMatMulThread inThreadObject)
            {
                platform = inPlatform;
                device = inDevice;
                threadObject = inThreadObject;
                thread = null;
            }

            public ComputePlatform platform;
            public ComputeDevice device;
            public BlockedMatMulThread threadObject;
            public Thread thread;
        }

        private List<InfoPerDevice> devices = new List<InfoPerDevice>();

        private void UpdateListViewSubItems(ListViewItem item, float mflops = 0, float? load = 0, float? temperature = 0, float? power = 0)
        {
            while (item.SubItems.Count < 5)
            {
                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, ""));
            }
            item.SubItems[1].Text = mflops.ToString("n0");
            item.SubItems[2].Text = load != null ? load?.ToString("p1") : "N/A";
            item.SubItems[3].Text = temperature != null ? temperature?.ToString("f1") + (" °C") : "N/A";
            item.SubItems[4].Text = power != null ? power?.ToString("f0") + " W" : "N/A";
        }

        private void InitializeListItems()
        {
            foreach (var info in devices)
            {
                listView_Devices.Items.Add(new ListViewItem(info.device.Name));
            }
        }

        public MainForm()
        {
            const int N = 1024;
            const int count = N * N;
            float[] A = new float[count];
            float[] B = new float[count];
            Random rand = new Random();
            for (int i = 0; i < count; ++i)
            {
                A[i] = (float)(rand.NextDouble() * 100);
                B[i] = (float)(rand.NextDouble() * 100);
            }

            InitializeComponent();
            foreach (Cloo.ComputePlatform platform in Cloo.ComputePlatform.Platforms)
                foreach (Cloo.ComputeDevice device in platform.Devices)
                    if (device.Type == ComputeDeviceTypes.Gpu)
                        devices.Add(new InfoPerDevice(platform, device, new BlockedMatMulThread(platform, device, N, A, B)));

            listView_Devices.Items.Clear();
            InitializeListItems();
            PeriodicSensorTimer.Start();
            labelCaution.Image = (new System.Drawing.Icon(System.Drawing.SystemIcons.Exclamation, 16, 16)).ToBitmap();
        }

        private void ListView_Devices_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listView_Devices.Columns[e.ColumnIndex].Width;
        }

        private void PeriodicSensorTimer_Tick(object sender, EventArgs e)
        {
            int itemIndex = 0;
            foreach (var info in devices)
            {
                var sensor = info.device.Sensor;
                UpdateListViewSubItems(listView_Devices.Items[itemIndex], info.threadObject.MFLOPS, sensor.Load, sensor.Temperature, sensor.Power);
                ++itemIndex;
            }
        }

        private void listView_Devices_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Checked)
            {
                var info = devices[e.Item.Index];
                info.thread = new Thread(new ThreadStart(info.threadObject.Run));
                info.thread.Start();
            }
            else
            {
                var info = devices[e.Item.Index];
                info.threadObject.HaltRequested = true;
                if (info.thread != null)
                {
                    info.thread.Join();
                    info.thread = null;
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < devices.Count; ++i)
            {
                var info = devices[i];
                info.threadObject.HaltRequested = true;
                if (info.thread != null)
                {
                    info.thread.Join();
                    info.thread = null;
                }
                info.threadObject.Dispose();
            }
            devices.Clear();
        }
    }
}
