using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EvaluationSystem
{
    public partial class StudentEntryForm: Form
    {
        public StudentEntryForm()
        {
            InitializeComponent();
            RemarksComboBox.Items.Add("Shiftee");
            RemarksComboBox.Items.Add("Transferee");
            RemarksComboBox.Items.Add("New");
            RemarksComboBox.Items.Add("Old");
            RemarksComboBox.Items.Add("Cross-Ennrollee");
            RemarksComboBox.Items.Add("Returnee");
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            String connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\63915\Documents\Goco.mdf;Integrated Security=True;Connect Timeout=30";
            SqlConnection myConnection = new SqlConnection(connectionString);

            String sql = "SELECT * FROM STUDENTFILE";

            SqlDataAdapter thisAdapter = new SqlDataAdapter(sql, myConnection);

            SqlCommandBuilder thisBuilder = new SqlCommandBuilder(thisAdapter);

            DataSet thisDataSet = new DataSet();
            thisAdapter.Fill(thisDataSet, "StudentFile");

            DataRow thisRow = thisDataSet.Tables["StudentFile"].NewRow();

            try
            {
                if (IdNumberTextBox.Text.Equals("") ||
                   LastNameTextBox.Text.Equals("") ||
                   FirstNameTextBox.Text.Equals("") ||
                   MiddleInitialTextBox.Text.Equals("") ||
                   CourseTextBox.Text.Equals("") ||
                   YearTextBox.Text.Equals(""))
                {
                    MessageBox.Show("FILL UP THE REMAINING FIELDS");
                }
                else
                {
                    thisRow["STFSTUDID"] = Convert.ToInt64(IdNumberTextBox.Text);
                    thisRow["STFSTUDLNAME"] = LastNameTextBox.Text;
                    thisRow["STFSTUDFNAME"] = FirstNameTextBox.Text;
                    thisRow["STFSTUDMNAME"] = MiddleInitialTextBox.Text;
                    thisRow["STFSTUDCOURSE"] = CourseTextBox.Text;
                    thisRow["STFSTUDYEAR"] = Convert.ToInt64(YearTextBox.Text);
                    thisRow["STFSTUDREMARKS"] = RemarksComboBox.SelectedItem.ToString();
                    //thisRow["STFSTUDSTATUS"] = ??

                    thisDataSet.Tables["StudentFile"].Rows.Add(thisRow);
                    thisAdapter.Update(thisDataSet, "StudentFile");

                    MessageBox.Show("Entries Recorded");
                }
            }
            catch
            {

            }
        }
    }
}
