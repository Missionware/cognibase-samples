namespace PingerWinFormsApp
{
    partial class DeviceEditForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            txtName = new TextBox();
            bsDevice = new BindingSource(components);
            label2 = new Label();
            lblId = new Label();
            txtId = new TextBox();
            txtHost = new TextBox();
            label3 = new Label();
            label4 = new Label();
            numInterval = new NumericUpDown();
            btnSave = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)bsDevice).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
            SuspendLayout();
            // 
            // txtName
            // 
            txtName.DataBindings.Add(new Binding("Text", bsDevice, "Name", true, DataSourceUpdateMode.OnPropertyChanged));
            txtName.Location = new Point(135, 60);
            txtName.Margin = new Padding(2);
            txtName.Name = "txtName";
            txtName.Size = new Size(196, 23);
            txtName.TabIndex = 9;
            // 
            // bsDevice
            // 
            bsDevice.DataSource = typeof(PingerDomain.Entities.Device);
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(25, 63);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(39, 15);
            label2.TabIndex = 11;
            label2.Text = "Name";
            // 
            // lblId
            // 
            lblId.AutoSize = true;
            lblId.Location = new Point(25, 24);
            lblId.Margin = new Padding(2, 0, 2, 0);
            lblId.Name = "lblId";
            lblId.Size = new Size(17, 15);
            lblId.TabIndex = 10;
            lblId.Text = "Id";
            // 
            // txtId
            // 
            txtId.DataBindings.Add(new Binding("Text", bsDevice, "Id", true, DataSourceUpdateMode.OnPropertyChanged));
            txtId.Location = new Point(135, 21);
            txtId.Margin = new Padding(2);
            txtId.Name = "txtId";
            txtId.Size = new Size(83, 23);
            txtId.TabIndex = 12;
            // 
            // txtHost
            // 
            txtHost.DataBindings.Add(new Binding("Text", bsDevice, "Host", true, DataSourceUpdateMode.OnPropertyChanged));
            txtHost.Location = new Point(135, 99);
            txtHost.Margin = new Padding(2);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(196, 23);
            txtHost.TabIndex = 13;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(25, 102);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(32, 15);
            label3.TabIndex = 14;
            label3.Text = "Host";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(25, 141);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(74, 15);
            label4.TabIndex = 15;
            label4.Text = "Interval (sec)";
            // 
            // numInterval
            // 
            numInterval.DataBindings.Add(new Binding("Value", bsDevice, "PingInterval", true, DataSourceUpdateMode.OnPropertyChanged));
            numInterval.Location = new Point(135, 139);
            numInterval.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            numInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numInterval.Name = "numInterval";
            numInterval.Size = new Size(75, 23);
            numInterval.TabIndex = 16;
            numInterval.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(262, 194);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(88, 27);
            btnSave.TabIndex = 17;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(152, 194);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 27);
            btnCancel.TabIndex = 18;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // DeviceEditForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(363, 233);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            Controls.Add(numInterval);
            Controls.Add(label4);
            Controls.Add(txtHost);
            Controls.Add(label3);
            Controls.Add(txtId);
            Controls.Add(txtName);
            Controls.Add(label2);
            Controls.Add(lblId);
            Name = "DeviceEditForm";
            Text = "Device Edit";
            FormClosing += DeviceEditForm_FormClosing;
            ((System.ComponentModel.ISupportInitialize)bsDevice).EndInit();
            ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtName;
        private Label label2;
        private Label lblId;
        private TextBox txtId;
        private TextBox txtHost;
        private Label label3;
        private Label label4;
        private NumericUpDown numInterval;
        private BindingSource bsDevice;
        private Button btnSave;
        private Button btnCancel;
    }
}