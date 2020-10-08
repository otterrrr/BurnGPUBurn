namespace BurnGPUBurn
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "NVIDIA RTX 2070",
            "8230",
            "20.0 %",
            "46 °C",
            "55 W"}, -1);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "AMD Radeon RX 570",
            "1023",
            "3.0 %",
            "38 °C",
            "23 W"}, -1);
            this.listView_Devices = new System.Windows.Forms.ListView();
            this.listViewColumnHeader_Device = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listViewColumnHeader_MFLOPS = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listViewColumnHeader_Load = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listViewColumnHeader_Temperature = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listViewColumnHeader_Power = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PeriodicSensorTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelCaution = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView_Devices
            // 
            this.listView_Devices.CheckBoxes = true;
            this.listView_Devices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.listViewColumnHeader_Device,
            this.listViewColumnHeader_MFLOPS,
            this.listViewColumnHeader_Load,
            this.listViewColumnHeader_Temperature,
            this.listViewColumnHeader_Power});
            this.listView_Devices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Devices.FullRowSelect = true;
            this.listView_Devices.GridLines = true;
            this.listView_Devices.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView_Devices.HideSelection = false;
            listViewItem1.StateImageIndex = 0;
            listViewItem2.StateImageIndex = 0;
            this.listView_Devices.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
            this.listView_Devices.Location = new System.Drawing.Point(3, 28);
            this.listView_Devices.MultiSelect = false;
            this.listView_Devices.Name = "listView_Devices";
            this.listView_Devices.ShowGroups = false;
            this.listView_Devices.Size = new System.Drawing.Size(510, 136);
            this.listView_Devices.TabIndex = 0;
            this.listView_Devices.TabStop = false;
            this.listView_Devices.UseCompatibleStateImageBehavior = false;
            this.listView_Devices.View = System.Windows.Forms.View.Details;
            this.listView_Devices.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.ListView_Devices_ColumnWidthChanging);
            this.listView_Devices.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listView_Devices_ItemChecked);
            // 
            // listViewColumnHeader_Device
            // 
            this.listViewColumnHeader_Device.Text = "Device";
            this.listViewColumnHeader_Device.Width = 252;
            // 
            // listViewColumnHeader_MFLOPS
            // 
            this.listViewColumnHeader_MFLOPS.Text = "MFLOPS";
            this.listViewColumnHeader_MFLOPS.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.listViewColumnHeader_MFLOPS.Width = 70;
            // 
            // listViewColumnHeader_Load
            // 
            this.listViewColumnHeader_Load.Text = "Load";
            this.listViewColumnHeader_Load.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // listViewColumnHeader_Temperature
            // 
            this.listViewColumnHeader_Temperature.Text = "Temperature";
            this.listViewColumnHeader_Temperature.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // listViewColumnHeader_Power
            // 
            this.listViewColumnHeader_Power.Text = "Power";
            this.listViewColumnHeader_Power.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // PeriodicSensorTimer
            // 
            this.PeriodicSensorTimer.Interval = 1000;
            this.PeriodicSensorTimer.Tick += new System.EventHandler(this.PeriodicSensorTimer_Tick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.listView_Devices, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelCaution, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(516, 167);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // labelCaution
            // 
            this.labelCaution.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCaution.Font = new System.Drawing.Font("Gulim", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelCaution.ForeColor = System.Drawing.Color.Crimson;
            this.labelCaution.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelCaution.Location = new System.Drawing.Point(3, 0);
            this.labelCaution.Name = "labelCaution";
            this.labelCaution.Size = new System.Drawing.Size(510, 25);
            this.labelCaution.TabIndex = 1;
            this.labelCaution.Text = "Please make sure your PSU can supply all the GPUs\' TDP";
            this.labelCaution.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(516, 167);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MainForm";
            this.Text = "Burn! GPU! Burn!";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView_Devices;
        private System.Windows.Forms.ColumnHeader listViewColumnHeader_Device;
        private System.Windows.Forms.ColumnHeader listViewColumnHeader_Load;
        private System.Windows.Forms.ColumnHeader listViewColumnHeader_Temperature;
        private System.Windows.Forms.ColumnHeader listViewColumnHeader_Power;
        private System.Windows.Forms.ColumnHeader listViewColumnHeader_MFLOPS;
        private System.Windows.Forms.Timer PeriodicSensorTimer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelCaution;
    }
}

