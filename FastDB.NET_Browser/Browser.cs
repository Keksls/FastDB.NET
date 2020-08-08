using FastDB.NET;
using System;
using System.IO;
using System.Windows.Forms;

namespace FastDB.NET_Browser
{
    public partial class Browser : Form
    {
        FastDatabase Database = null;
        Table table = null;
        Table definitionTable = null;
        int currentPageIndex = 0;
        int nbPerPage = 100;

        public Browser()
        {
            InitializeComponent();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        void BindFromDB()
        {
            cbTable.Items.Clear();
            lbTables.Items.Clear();
            foreach (var Table in Database.Tables)
            {
                cbTable.Items.Add(Table.Key);
                lbTables.Items.Add(Table.Key);
            }
            cbTable.SelectedIndex = 0;
            lbTables.SelectedIndex = 0;
        }

        private void cbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbTable.SelectedIndex != -1 && Database != null)
            {
                table = Database.GetTable(cbTable.Text);
                dataGrid.Columns.Clear();
                dataGrid.ColumnCount = table.NbFields;
                int i = 0;
                foreach (var field in table.Fields)
                {
                    dataGrid.Columns[i].Name = field.Key;
                    i++;
                }
                currentPageIndex = 1;
                LoadPage(0);
            }
        }

        private void LoadPage(int lastPage)
        {
            if (table == null)
                return;
            dataGrid.Rows.Clear();
            int max = Math.Min(table.NbRows, currentPageIndex * nbPerPage);
            for (int i = lastPage * nbPerPage; i < max; i++)
                dataGrid.Rows.Add(table.Rows[i].GetCells());
            lbPageIndex.Text = currentPageIndex.ToString();
            lbNbRows.Text = "NbRows : " + table.NbRows.ToString();
        }

        public void NextPage()
        {
            int nbPages = table.NbRows / nbPerPage;
            if (currentPageIndex + 1 < nbPages)
            {
                currentPageIndex++;
                LoadPage(currentPageIndex - 1);
            }
        }

        public void LastPage()
        {
            if (currentPageIndex - 1 >= 1)
            {
                currentPageIndex--;
                LoadPage(currentPageIndex - 1);
            }
        }

        private void openDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "FastDB.NET Database (*.fastdb)|*.fastdb";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Database = new FastDatabase(Path.GetFileNameWithoutExtension(ofd.FileName),
                    ofd.FileName.Replace(Path.GetFileName(ofd.FileName), ""));
                Database.Connect();
                Text = "FastDB.NET Browser - " + Database.DatabaseName;
                BindFromDB();
                menuSave.Enabled = true;
                tabs.Enabled = true;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            NextPage();
        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            LastPage();
        }

        private void lbTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbTables.SelectedIndex != -1 && Database != null)
            {
                definitionTable = Database.GetTable(lbTables.Text);
                rtbDefinition.Text = definitionTable.ToString();
            }
        }

        private void btnDeleteTable_Click(object sender, EventArgs e)
        {
            if (lbTables.SelectedIndex == -1 || definitionTable == null)
                return;
            Database.Tables.Remove(definitionTable.Name);
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            Database.Save();
            BindFromDB();
        }

        private void dataGrid_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            int index = currentPageIndex * nbPerPage + e.RowIndex;
            table.Rows.RemoveAt(index);
            lbNbRows.Text = "NbRows : " + table.NbRows.ToString();
        }
    }
}