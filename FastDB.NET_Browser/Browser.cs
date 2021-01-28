using FastDB.NET;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace FastDB.NET_Browser
{
    public partial class Browser : Form
    {
        public FastDatabase Database = null;
        Table table = null;
        Table definitionTable = null;
        int currentPageIndex = 0;
        int nbPerPage = 100;
        bool removingRow = false;

        public Browser()
        {
            InitializeComponent();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void BindFromDB()
        {
            removingRow = true;
            cbTable.Items.Clear();
            tvTables.Nodes.Clear();
            TreeNode root = new TreeNode("Database");
            root.ImageIndex = 0;
            root.SelectedImageIndex = 0;
            tvTables.Nodes.Add(root);
            foreach (var Table in Database.Tables)
            {
                cbTable.Items.Add(Table.Key);
                TreeNode tn = new TreeNode(Table.Key);
                tn.ImageIndex = 1;
                tn.SelectedImageIndex = 1;
                foreach (var field in Table.Value.Fields)
                {
                    TreeNode t = new TreeNode(field.Key + " (" + field.Value.Type.ToString().ToUpper() + ")");
                    t.ImageIndex = 2;
                    t.SelectedImageIndex = 2;
                    tn.Nodes.Add(t);
                }
                root.Nodes.Add(tn);
            }
            root.Expand();
            if (cbTable.Items.Count > 0)
            {
                cbTable.SelectedIndex = 0;
                btnLast.Enabled = true;
                btnNext.Enabled = true;
            }
            else
            {
                btnLast.Enabled = false;
                btnNext.Enabled = false;
                cbTable.Text = "";
                table = null;
                dataGrid.Columns.Clear();
                dataGrid.Rows.Clear();
                GC.Collect();
            }
            removingRow = false;
        }

        private void cbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbTable.SelectedIndex != -1 && Database != null)
            {
                removingRow = true;
                table = Database.GetTable(cbTable.Text);
                dataGrid.Columns.Clear();
                int i = 0;
                foreach (var field in Database.GetTable(table.Name).Fields)
                {
                    switch (field.Value.Type)
                    {
                        case FastDBType.String:
                            DataGridViewTextBoxColumn colString = new DataGridViewTextBoxColumn();
                            colString.ValueType = typeof(string);
                            colString.Name = field.Key;
                            colString.HeaderText = field.Key;
                            dataGrid.Columns.Add(colString);
                            break;
                        case FastDBType.Integer:
                            DataGridViewTextBoxColumn colInt = new DataGridViewTextBoxColumn();
                            colInt.ValueType = typeof(int);
                            colInt.Name = field.Key;
                            colInt.HeaderText = field.Key;
                            dataGrid.Columns.Add(colInt);
                            break;
                        case FastDBType.UnsignedInteger:
                            DataGridViewTextBoxColumn colUInt = new DataGridViewTextBoxColumn();
                            colUInt.ValueType = typeof(uint);
                            colUInt.Name = field.Key;
                            colUInt.HeaderText = field.Key;
                            dataGrid.Columns.Add(colUInt);
                            break;
                        case FastDBType.Float:
                            DataGridViewTextBoxColumn colFloat = new DataGridViewTextBoxColumn();
                            colFloat.ValueType = typeof(float);
                            colFloat.Name = field.Key;
                            colFloat.HeaderText = field.Key;
                            dataGrid.Columns.Add(colFloat);
                            break;
                        case FastDBType.Double:
                            DataGridViewTextBoxColumn colDouble = new DataGridViewTextBoxColumn();
                            colDouble.ValueType = typeof(double);
                            colDouble.Name = field.Key;
                            colDouble.HeaderText = field.Key;
                            dataGrid.Columns.Add(colDouble);
                            break;
                        case FastDBType.Bool:
                            DataGridViewCheckBoxColumn colBool = new DataGridViewCheckBoxColumn();
                            colBool.ValueType = typeof(bool);
                            colBool.Name = field.Key;
                            colBool.HeaderText = field.Key;
                            dataGrid.Columns.Add(colBool);
                            break;
                        case FastDBType.ByteArray:
                            DataGridViewTextBoxColumn colBA = new DataGridViewTextBoxColumn();
                            colBA.ValueType = typeof(byte[]);
                            colBA.Name = field.Key;
                            colBA.HeaderText = field.Key;
                            dataGrid.Columns.Add(colBA);
                            break;
                        case FastDBType.Date:
                        case FastDBType.DateTime:
                            DataGridViewTextBoxColumn coldate = new DataGridViewTextBoxColumn();
                            coldate.ValueType = typeof(DateTime);
                            coldate.Name = field.Key;
                            coldate.HeaderText = field.Key;
                            coldate.DefaultCellStyle.Format = "MM/dd/yyyy HH:mm:ss";
                            dataGrid.Columns.Add(coldate);
                            break;
                        default:
                            break;
                    }
                    i++;
                }
                currentPageIndex = 1;
                LoadPage();
                removingRow = false;
            }
        }

        private void LoadPage()
        {
            removingRow = true;
            if ((int)Math.Ceiling((double)Database.GetTable(table.Name).NbRows / (double)nbPerPage) == currentPageIndex)
                dataGrid.AllowUserToAddRows = true;
            else
                dataGrid.AllowUserToAddRows = false;

            if (table == null)
                return;
            dataGrid.Rows.Clear();
            int max = Math.Min(Database.GetTable(table.Name).NbRows, currentPageIndex * nbPerPage);
            int min = Math.Max(0, currentPageIndex - 1);
            for (int i = min * nbPerPage; i < max; i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                int j = 0;
                foreach (var field in Database.GetTable(table.Name).Fields)
                {
                    DataGridViewCell cell = null;
                    switch (field.Value.Type)
                    {
                        case FastDBType.String:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<string>(j);
                            break;
                        case FastDBType.Integer:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<int>(j);
                            break;
                        case FastDBType.UnsignedInteger:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<uint>(j);
                            break;
                        case FastDBType.Float:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<float>(j);
                            break;
                        case FastDBType.Double:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<double>(j);
                            break;
                        case FastDBType.Bool:
                            cell = new DataGridViewCheckBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<bool>(j);
                            break;
                        case FastDBType.ByteArray:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = "byte[" +  Database.GetTable(table.Name).Rows[i].GetByteArray(j).Length + "]";
                            break;
                        case FastDBType.Date:
                        case FastDBType.DateTime:
                            cell = new DataGridViewTextBoxCell();
                            if (!Database.GetTable(table.Name).Rows[i].isNull(j))
                                cell.Value = Database.GetTable(table.Name).Rows[i].Get<DateTime>(j);
                            break;
                        default:
                            break;
                    }
                    row.Cells.Add(cell);
                    j++;
                }
                dataGrid.Rows.Add(row);
            }
            lbPageIndex.Text = currentPageIndex.ToString();
            lbNbRows.Text = "NbRows : " + Database.GetTable(table.Name).NbRows.ToString();
            removingRow = false;
        }

        public void NextPage()
        {
            int nbPages = (int)Math.Ceiling((double)Database.GetTable(table.Name).NbRows / (double)nbPerPage);
            if (currentPageIndex + 1 <= nbPages)
            {
                currentPageIndex++;
                LoadPage();
            }
        }

        public void FirstPage()
        {
            currentPageIndex = 1;
            LoadPage();
        }

        public void LastPage()
        {
            if (currentPageIndex - 1 >= 1)
            {
                currentPageIndex--;
                LoadPage();
            }
        }

        public void LastOnePage()
        {
            int nbPages = (int)Math.Ceiling((double)Database.GetTable(table.Name).NbRows / (double)nbPerPage);
            currentPageIndex = nbPages;
            LoadPage();
        }

        private void openDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDatabase();
        }

        private void OpenDatabase()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "FastDB.NET Database (*.fastdb)|*.fastdb";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Database = new FastDatabase(Path.GetFileNameWithoutExtension(ofd.FileName),
                    ofd.FileName.Replace(Path.GetFileName(ofd.FileName), ""));
                tabs.Visible = false;
                btnOpenDatabase.Visible = false;
                btnCreateDatabase.Visible = false;
                btnSave.Visible = false;
                rtbOut.AppendText("\n ============== Openning Database " + Database.DatabaseName + " ==============");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Thread t = new Thread(() =>
                {
                    Database.Connect();

                    this.BeginInvoke((Action)(() =>
                    {
                        Text = "FastDB.NET Browser - " + ofd.FileName;
                        BindFromDB();
                        menuSave.Enabled = true;
                        btnSave.Enabled = menuSave.Enabled;
                        menuCreateTable.Enabled = true;
                        tabs.Enabled = true;
                        tabs.Visible = true;
                        btnOpenDatabase.Visible = true;
                        btnCreateDatabase.Visible = true;
                        btnSave.Visible = true;
                        rtbOut.AppendText("\n > Database opened! (" + sw.ElapsedMilliseconds + " ms)");
                        DisplayDBInfos();
                    }));
                });
                t.Start();
            }
        }

        void DisplayDBInfos()
        {
            int nbRows = 0;
            foreach (var table in Database.Tables)
                nbRows += table.Value.NbRows;
            rtbOut.AppendText("\n= DBName   : " + Database.DatabaseName);
            rtbOut.AppendText("\n= DBPath   : " + Database.FilePath);
            rtbOut.AppendText("\n= NbTables : " + Database.Tables.Count);
            rtbOut.AppendText("\n= NbRows   : " + nbRows);
            rtbOut.AppendText("\n= DBSize   : " + new FileInfo(Path.Combine(Database.FilePath, Database.DatabaseName + ".FastDB")).Length.ToPrettySize());
            rtbOut.AppendText("\n= RAM      : " + Process.GetCurrentProcess().PrivateMemorySize64.ToPrettySize());
            rtbOut.AppendText("\n===================================================");
        }

        public void DisplayTableInfos(string tableName)
        {
            rtbOut.AppendText("\n=========== TABLE " + tableName + " ==============");
            rtbOut.AppendText("\n= Name   : " + tableName);
            rtbOut.AppendText("\n= NbFields   : " + Database.GetTable(tableName).NbFields);
            foreach (var field in Database.GetTable(tableName).Fields)
                rtbOut.AppendText("\n=  > " + field.Key + " (" + field.Value.Type.ToString().ToUpper() + ")");
            rtbOut.AppendText("\n= NbRows : " + Database.GetTable(tableName).NbRows);
            rtbOut.AppendText("\n===================================================");
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            NextPage();
        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            LastPage();
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            ExecuteinThread(Save, BindFromDB);
        }

        void Save() => Database.Save();

        private void dataGrid_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (table == null)
            {
                lbNbRows.Text = "No table available";
                return;
            }
            if (removingRow)
                return;
            int index = (currentPageIndex - 1) * nbPerPage + e.RowIndex;
            Database.GetTable(table.Name).Rows.RemoveAt(index);
            lbNbRows.Text = "NbRows : " + Database.GetTable(table.Name).NbRows.ToString();
        }

        private void bnCreateTable_Click(object sender, EventArgs e)
        {
            Create_Table ct = new Create_Table(this);
            ct.ShowDialog();
        }

        public void ExecuteinThread(Action InThread, Action MainThread = null, bool feedback = true)
        {
            if (feedback)
            {
                tabs.Visible = false;
                btnOpenDatabase.Visible = false;
                btnCreateDatabase.Visible = false;
                btnSave.Visible = false;
            }
            Thread t = new Thread(() =>
            {
                InThread?.Invoke();
                if (MainThread != null)
                    this.BeginInvoke((Action)(() =>
                    {
                        MainThread();
                        if (feedback)
                        {
                            tabs.Visible = true;
                            btnOpenDatabase.Visible = true;
                            btnCreateDatabase.Visible = true;
                            btnSave.Visible = true;
                        }
                    }));
            });
            t.Start();
        }

        public FastDBType TypeFromString(string value)
        {
            foreach (FastDBType type in Enum.GetValues(typeof(FastDBType)))
                if (type.ToString() == value)
                    return type;
            return FastDBType.String;
        }

        void CloseDatabase()
        {
            cbTable.Items.Clear();
            tvTables.Nodes.Clear();
            if (Database != null)
                Database.Close();
            Database = null;
            GC.Collect();
            tabs.Enabled = false;
            menuSave.Enabled = false;
            btnSave.Enabled = menuSave.Enabled;
            menuCreateTable.Enabled = false;
            rtbOut.AppendText("\n> Database Closed\n");
        }

        private void createTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Create_Table ct = new Create_Table(this);
            ct.ShowDialog();
        }

        private void tvTables_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.ImageIndex == 1) // a table is selected
            {
                btnEditTable.Enabled = true;
                btnDeleteTable.Enabled = true;
                definitionTable = Database.GetTable(e.Node.Text.Split('(')[0].Trim());
            }
            else
            {
                btnEditTable.Enabled = false;
                btnDeleteTable.Enabled = false;
                definitionTable = null;
            }
        }

        private void btnEditTable_Click(object sender, EventArgs e)
        {
            if (definitionTable == null) return;
            Create_Table ct = new Create_Table(this, definitionTable);
            ct.ShowDialog();
        }

        private void btnDeleteTable_Click(object sender, EventArgs e)
        {
            if (definitionTable == null) return;
            Database.RemoveTable(definitionTable.Name);
            definitionTable = null;
            GC.Collect();
            BindFromDB();
            btnEditTable.Enabled = false;
            btnDeleteTable.Enabled = false;
        }

        private void createNewDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewDatabase();
        }

        private void CreateNewDatabase()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "FastDB.NET Database (*.fastdb)|*.fastdb";
            sfd.DefaultExt = ".fastdb";
            sfd.AddExtension = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                CloseDatabase();
                Database = new FastDatabase(Path.GetFileNameWithoutExtension(sfd.FileName),
                    sfd.FileName.Replace(Path.GetFileName(sfd.FileName), ""));
                Database.Save();
                Text = "FastDB.NET Browser - " + sfd.FileName;
                rtbOut.AppendText(" > Database Created! \n\r > " + sfd.FileName + "");
                BindFromDB();
                menuSave.Enabled = true;
                btnSave.Enabled = menuSave.Enabled;
                menuCreateTable.Enabled = true;
                tabs.Enabled = true;
                tabs.Visible = true;
                btnOpenDatabase.Visible = true;
                btnCreateDatabase.Visible = true;
                btnSave.Visible = true;
            }
        }

        private void rtbOut_TextChanged(object sender, EventArgs e)
        {
            rtbOut.SelectionStart = rtbOut.Text.Length;
            rtbOut.ScrollToCaret();
        }

        private void dataGrid_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void dataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            FastDBType type = Database.GetTable(table.Name).Fields.ElementAt(e.ColumnIndex).Value.Type;

            if (Database.GetTable(table.Name).NbRows <= e.RowIndex + ((currentPageIndex - 1) * nbPerPage)) // Insert row
            {
                object[] cells = new object[Database.GetTable(table.Name).NbFields];
                int i = 0;
                foreach (DataGridViewCell cell in dataGrid.Rows[e.RowIndex].Cells)
                {
                    cells[i] = cell.Value;
                    i++;
                }
                Database.GetTable(table.Name).Insert(cells);
                lbNbRows.Text = "NbRows : " + Database.GetTable(table.Name).NbRows.ToString();
            }
            else // Update row
            {
                object val = Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Get(e.ColumnIndex);
                switch (type)
                {
                    default:
                    case FastDBType.String:
                        string dgValue = (string)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        string dbValue = val == null ? default(string) : (string)val; ;
                        if (!dgValue.Equals(dbValue))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValue);
                        break;
                    case FastDBType.Integer:
                        int dgValueI = (int)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        int dbValueI = val == null ? default(int) : (int)val;
                        if (!dgValueI.Equals(dbValueI))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueI);
                        break;
                    case FastDBType.UnsignedInteger:
                        uint dgValueUI = (uint)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        uint dbValueUI = val == null ? default(uint) : (uint)val;
                        if (!dgValueUI.Equals(dbValueUI))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueUI);
                        break;
                    case FastDBType.Float:
                        float dgValueF = (float)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        float dbValueF = val == null ? default(float) : (float)val;
                        if (!dgValueF.Equals(dbValueF))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueF);
                        break;
                    case FastDBType.Double:
                        double dgValueD = (double)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        double dbValueD = val == null ? default(double) : (double)val;
                        if (!dgValueD.Equals(dbValueD))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueD);
                        break;
                    case FastDBType.Bool:
                        bool dgValueB = (bool)((DataGridViewCheckBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        bool dbValueB = val == null ? default(bool) : (bool)val;
                        if (!dgValueB.Equals(dbValueB))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueB);
                        break;
                    case FastDBType.ByteArray:
                        byte[] dgValueBa = (byte[])((DataGridViewCheckBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        byte[] dbValueBa = val == null ? default(byte[]) : (byte[])val;
                        if (!dgValueBa.Equals(dbValueBa))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueBa);
                        break;
                    case FastDBType.Date:
                    case FastDBType.DateTime:
                        DateTime dgValueDT = (DateTime)((DataGridViewTextBoxCell)dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value;
                        DateTime dbValueDT = val == null ? default(DateTime) : (DateTime)val;
                        if (!dgValueDT.Equals(dbValueDT))
                            Database.GetTable(table.Name).Rows[e.RowIndex + ((currentPageIndex - 1) * nbPerPage)].Set(e.ColumnIndex, dgValueDT);
                        break;
                }
            }
        }

        private void btnCreateDatabase_Click(object sender, EventArgs e)
        {
            CreateNewDatabase();
        }

        private void btnOpenDatabase_Click(object sender, EventArgs e)
        {
            OpenDatabase();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ExecuteinThread(Save, BindFromDB);
        }

        private void closeDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseDatabase();
        }

        private void btnFirstPage_Click(object sender, EventArgs e)
        {
            FirstPage();
        }

        private void btnLastPage_Click(object sender, EventArgs e)
        {
            LastOnePage();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadPage();
        }

        private void Browser_Load(object sender, EventArgs e)
        {

        }

        private void dataGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1) return;
            var cell = dataGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell is DataGridViewTextBoxCell)
            {
                if (((DataGridViewTextBoxCell)cell).Value == null)
                    rtbCellData.Text = "null";
                else
                    rtbCellData.Text = ((DataGridViewTextBoxCell)cell).Value.ToString();
            }
            else
                rtbCellData.Text = "";
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
    public static class Ext
    {
        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        public static string ToPrettySize(this int value, int decimalPlaces = 2)
        {
            return ((long)value).ToPrettySize(decimalPlaces);
        }

        public static string ToPrettySize(this long value, int decimalPlaces = 2)
        {
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}Tb", asTb)
                : asGb > 1 ? string.Format("{0}Gb", asGb)
                : asMb > 1 ? string.Format("{0}Mb", asMb)
                : asKb > 1 ? string.Format("{0}Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }
    }
}