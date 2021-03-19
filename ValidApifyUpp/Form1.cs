using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sqlconnection;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace ValidApifyUpp
{
    public partial class Form1 : Form
    {

        public string _retailerId { get { return textBox2.Text; } set { } }

        public static DataTable _dt { get; set; }
        public static DataTable _dts { get; set; }
        public static List<string> address { get; set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        public void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            textBox1.Text = openFileDialog1.FileName;
            BindData(textBox1.Text);
        }

        public void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog
            {
                Title = "Open File",
                InitialDirectory = @"c:\",
                Filter = "All files (*.*)|*.*|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fdlg.FileName;
            }
        }

        public void BindData(string filePath)
        {

            _dt = new DataTable();
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length > 0)
            {
                //first line to create header
                string firstLine = lines[0];
                string[] headerLabels = firstLine.Split(',');
                foreach (string headerWord in headerLabels)
                {
                    _dt.Columns.Add(new DataColumn(headerWord));
                }
                //For Data
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] dataWords = lines[i].Split(',');
                    DataRow dr = _dt.NewRow();
                    int columnIndex = 0;
                    foreach (string headerWord in headerLabels)
                    {
                        dr[headerWord] = dataWords[columnIndex++];
                    }
                    _dt.Rows.Add(dr);
                }
            }
            if (_dt.Rows.Count > 0)
            {
                dataGridView1.DataSource = _dt;
            }
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            GetData();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GetDiff();
        }

        public void GetData()
        {
            _dts = new DataTable();
            _dts.Columns.Add("ProductUrl");
            _dts.Columns.Add("ProductId");
            address = new List<string>();
            for (int i = 1; i <= 150; i++)
            {
                if (_dt.Rows.Count > 0)
                {
                    if (_dt.Rows[i][4].ToString().Length > 0)
                    {
                        address.Add(_dt.Rows[i][4].ToString().Replace("\"", ""));
                    }
                }
            }

            address = address.Select(x => "'" + x + "'").ToList();
            var adressesString = string.Join(",", address);


            var sqlManager = new SqlServerAccessManager();

            sqlManager.Connect();
            var scriptRP = $@"select Address,IDs from RetailerProductInfo RPI where RPI.RetailerId ='{_retailerId}'
                              AND (RPI.Deleted = 0 or RPI.Deleted IS NULL)
                              AND (RPI.LinkIsValid = 1 OR RPI.LinkIsValid IS NULL)
                              AND RPI.Address in ({adressesString})";
            var rpi = sqlManager.ExecuteSQL(scriptRP).ToList();
            foreach (var item in rpi)
            {
                var row = _dts.NewRow();

                row["ProductUrl"] = item.Address;
                row["ProductId"] = item.IDs;
                _dts.Rows.Add(row);
            }
        }

        public void GetDiff()
        {
            CompareValues(_dt, _dts);
            dataGridView2.DataSource = CompareValues(_dt, _dts);
        }

        public static DataTable CompareValues(DataTable _dt, DataTable _dts)
        {
            _dt.TableName = "Apify";
            _dts.TableName = "Database";

            var dur = _dt.Select(_dt.Columns[4].ColumnName).CopyToDataTable();
            var str = _dt.Select(_dt.Columns[2].ColumnName).CopyToDataTable();

            var unmatched = _dts.AsEnumerable().Except(_dt.AsEnumerable(), DataRowComparer.Default);
            var three = unmatched.Any() ? unmatched.CopyToDataTable() : _dt.Clone();

            return three;
        }
    }
}
