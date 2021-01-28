namespace FastDB.NET_Browser
{
    partial class Create_Table
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Create_Table));
            this.tbTableName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dgFields = new System.Windows.Forms.DataGridView();
            this.FieldName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Type = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Default = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCreateTable = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tbTableName
            // 
            this.tbTableName.Location = new System.Drawing.Point(117, 14);
            this.tbTableName.Name = "tbTableName";
            this.tbTableName.Size = new System.Drawing.Size(520, 23);
            this.tbTableName.TabIndex = 0;
            this.tbTableName.TextChanged += new System.EventHandler(this.tbTableName_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(44, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Table name";
            // 
            // dgFields
            // 
            this.dgFields.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgFields.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            this.dgFields.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgFields.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FieldName,
            this.Type,
            this.Default});
            this.dgFields.Location = new System.Drawing.Point(13, 43);
            this.dgFields.MultiSelect = false;
            this.dgFields.Name = "dgFields";
            this.dgFields.Size = new System.Drawing.Size(624, 432);
            this.dgFields.TabIndex = 8;
            this.dgFields.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgFields_CellEndEdit);
            this.dgFields.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgFields_RowsAdded);
            // 
            // FieldName
            // 
            this.FieldName.HeaderText = "Name";
            this.FieldName.Name = "FieldName";
            this.FieldName.Width = 200;
            // 
            // Type
            // 
            this.Type.HeaderText = "Type";
            this.Type.Items.AddRange(new object[] {
            "String",
            "Float",
            "Double",
            "Integer",
            "UnsignedInteger",
            "Bool",
            "ByteArray",
            "Date",
            "DateTime"});
            this.Type.Name = "Type";
            this.Type.Width = 180;
            // 
            // Default
            // 
            this.Default.HeaderText = "Default";
            this.Default.Name = "Default";
            this.Default.Width = 200;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Drive Generic-01.png");
            this.imageList1.Images.SetKeyName(1, "Button Remove-01.png");
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::FastDB.NET_Browser.Properties.Resources.iconfinder_table_edit_44399;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(13, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.ImageIndex = 1;
            this.btnCancel.ImageList = this.imageList1;
            this.btnCancel.Location = new System.Drawing.Point(461, 484);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(84, 32);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnCreateTable
            // 
            this.btnCreateTable.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCreateTable.ImageIndex = 0;
            this.btnCreateTable.ImageList = this.imageList1;
            this.btnCreateTable.Location = new System.Drawing.Point(551, 484);
            this.btnCreateTable.Name = "btnCreateTable";
            this.btnCreateTable.Size = new System.Drawing.Size(84, 32);
            this.btnCreateTable.TabIndex = 4;
            this.btnCreateTable.Text = "OK";
            this.btnCreateTable.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCreateTable.UseVisualStyleBackColor = true;
            this.btnCreateTable.Click += new System.EventHandler(this.btnCreateTable_Click);
            // 
            // Create_Table
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(647, 529);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.dgFields);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnCreateTable);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbTableName);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(663, 567);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(663, 567);
            this.Name = "Create_Table";
            this.Text = "FastDB.NET - Create new Table";
            this.Load += new System.EventHandler(this.Create_Table_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgFields)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbTableName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnCreateTable;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.DataGridView dgFields;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.DataGridViewTextBoxColumn FieldName;
        private System.Windows.Forms.DataGridViewComboBoxColumn Type;
        private System.Windows.Forms.DataGridViewTextBoxColumn Default;
    }
}