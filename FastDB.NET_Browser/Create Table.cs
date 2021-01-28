using FastDB.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FastDB.NET_Browser
{
    public partial class Create_Table : Form
    {
        string tableName = null;
        Browser browser;

        public Create_Table(Browser _browser)
        {
            InitializeComponent();
            browser = _browser;
        }

        public Create_Table(Browser _browser, Table _table)
        {
            InitializeComponent();
            browser = _browser;
            tableName = _table.Name;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Create_Table_Load(object sender, EventArgs e)
        {
            dgFields.Rows.Clear();

            if (tableName == null)
            {
                tableName = "new table";
                Text = "FastDB.NET - Create new table";
                Initialize(tableName);
                tbTableName.Enabled = true;
            }
            else
            {
                Text = "FastDB.NET - Edit " + tableName;
                Initialize(tableName);
                tbTableName.Enabled = false;
            }
        }

        public void Initialize(string _tableName)
        {
            tableName = _tableName;
            dgFields.Rows.Clear();
            tbTableName.Text = tableName;

            if (browser.Database.TableExists(tableName))
            {
                if (browser.Database.GetTable(tableName).Fields != null)
                {
                    foreach (var field in browser.Database.GetTable(tableName).Fields)
                        dgFields.Rows.Add(field.Key, field.Value.Type.ToString(), field.Value.DefaultValue);
                }
            }
        }

        private bool validateRow(DataGridViewRow row)
        {
            return ((row.Cells[0].Value != null && row.Cells[1].Value != null) ||
                row.Index >= dgFields.Rows.Count - 1);
        }

        private void btnCreateTable_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgFields.Rows)
                if (!validateRow(row))
                {
                    MessageBox.Show("One or more field definition is not valide");
                    return;
                }

            browser.ExecuteinThread(() =>
            {
                HashSet<string> Fields = new HashSet<string>();
                if (browser.Database.TableExists(tableName))
                {
                    // save table
                    foreach (DataGridViewRow row in dgFields.Rows)
                    {
                        if (row.Cells[0].Value == null)
                            continue;
                        string name = row.Cells[0].Value.ToString();
                        FastDBType type = browser.TypeFromString(row.Cells[1].Value.ToString());
                        IComparable defVal = (IComparable)row.Cells[2].Value;
                        if (!browser.Database.GetTable(tableName).Fields.ContainsKey(name))
                            browser.Database.GetTable(tableName).AddField(name, type, defVal);
                    }
                }
                else // add table
                {
                    browser.Database.CreateTable(tableName);
                    foreach (DataGridViewRow row in dgFields.Rows)
                    {
                        if (row.Cells[0].Value == null)
                            continue;
                        string name = row.Cells[0].Value.ToString();
                        FastDBType type = browser.TypeFromString(row.Cells[1].Value.ToString());
                        IComparable defVal = (IComparable)row.Cells[2].Value;
                        browser.Database.GetTable(tableName).AddField(name, type, defVal);

                    }
                }

                // remove fields that not exist anymore
                HashSet<string> FieldsToRem = new HashSet<string>();
                foreach (var field in browser.Database.GetTable(tableName).Fields)
                {
                    bool contained = false;
                    foreach (DataGridViewRow row in dgFields.Rows)
                        if ((string)row.Cells[0].Value == field.Key)
                        {
                            contained = true;
                            break;
                        }
                    if (!contained)
                        FieldsToRem.Add(field.Key);
                }
                foreach (var field in FieldsToRem)
                    browser.Database.GetTable(tableName).RemoveField(field);
            }, () => { Close(); browser.BindFromDB(); browser.DisplayTableInfos(tableName); });
        }

        private void dgFields_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dgFields.Rows[e.RowIndex].Cells[1].Value == null)
                dgFields.Rows[e.RowIndex].Cells[1].Value = "String";
        }

        private void dgFields_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                string typeString = (string)dgFields.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                FastDBType type = browser.TypeFromString(typeString);
                if (!browser.Database.TableExists(tableName))
                    return;
                Field field = browser.Database.GetTable(tableName).Fields.ElementAt(e.ColumnIndex).Value;
                switch (type)
                {
                    case FastDBType.String:
                        DataGridViewTextBoxCell colString = new DataGridViewTextBoxCell();
                        colString.ValueType = typeof(string);
                        field.DefaultValue = "";
                        colString.Value = (string)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = colString;
                        break;
                    case FastDBType.Integer:
                        DataGridViewTextBoxCell colint = new DataGridViewTextBoxCell();
                        colint.ValueType = typeof(int);
                        field.DefaultValue = default(int);
                        colint.Value = (int)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = colint;
                        break;
                    case FastDBType.UnsignedInteger:
                        DataGridViewTextBoxCell coluint = new DataGridViewTextBoxCell();
                        coluint.ValueType = typeof(uint);
                        field.DefaultValue = default(uint);
                        coluint.Value = (uint)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = coluint;
                        break;
                    case FastDBType.Double:
                        DataGridViewTextBoxCell coldouble = new DataGridViewTextBoxCell();
                        coldouble.ValueType = typeof(double);
                        field.DefaultValue = default(double);
                        coldouble.Value = (double)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = coldouble;
                        break;
                    case FastDBType.Float:
                        DataGridViewTextBoxCell colfloat = new DataGridViewTextBoxCell();
                        colfloat.ValueType = typeof(float);
                        field.DefaultValue = default(float);
                        colfloat.Value = (float)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = colfloat;
                        break;
                    case FastDBType.Bool:
                        DataGridViewCheckBoxCell colbool = new DataGridViewCheckBoxCell();
                        colbool.ValueType = typeof(bool);
                        field.DefaultValue = default(bool);
                        colbool.Value = (bool)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = colbool;
                        break;
                    case FastDBType.ByteArray:
                        DataGridViewCheckBoxCell colba = new DataGridViewCheckBoxCell();
                        colba.ValueType = typeof(byte[]);
                        field.DefaultValue = "";
                        colba.Value = (byte[])field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = colba;
                        break;
                    case FastDBType.DateTime:
                    case FastDBType.Date:
                        DataGridViewTextBoxCell coldt = new DataGridViewTextBoxCell();
                        coldt.ValueType = typeof(DateTime);
                        field.DefaultValue = default(DateTime);
                        coldt.Style.Format = "MM/dd/yyyy HH:mm:ss";
                        coldt.Value = (DateTime)field.DefaultValue;
                        dgFields.Rows[e.RowIndex].Cells[2] = coldt;
                        break;
                }
            }
        }

        private void tbTableName_TextChanged(object sender, EventArgs e)
        {
            tableName = tbTableName.Text;
        }
    }
}