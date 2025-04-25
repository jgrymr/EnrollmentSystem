using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace EvaluationSystem
{
    public partial class SubjectScheduleEntry: Form
    {
        public SubjectScheduleEntry()
        {
            InitializeComponent();
            AMPMComboBox.Items.Add("AM");
            AMPMComboBox.Items.Add("PM");
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\63915\Documents\Goco.mdf;Integrated Security=True;Connect Timeout=30";
            SqlConnection myConnection = new SqlConnection(connectionString);
            
            String sql = "SELECT * FROM SubjectSchedFile";
            
            SqlDataAdapter thisAdapter = new SqlDataAdapter(sql, myConnection);
            
            SqlCommandBuilder thisBuilder = new SqlCommandBuilder(thisAdapter);
            
            DataSet thisDataSet = new DataSet();
            thisAdapter.Fill(thisDataSet, "SubjectSchedFile");
            DataRow thisRow = thisDataSet.Tables["SubjectSchedFile"].NewRow();

            try
            {
                if (SubjectEDPCodeTextBox.Text.Equals("") ||
                    SubjectCodeTextBox.Text.Equals("") ||
                    DescriptionTextBox.Text.Equals("") ||
                    TimeStartTextBox.Text.Equals("") ||
                    TimeEndTextBox.Text.Equals("") ||
                    DaysTextBox.Text.Equals("") ||
                    SectionTextBox.Text.Equals("") ||
                    RoomTextBox.Text.Equals("") ||
                    SchoolYearTextBox.Text.Equals(""))
                {
                    MessageBox.Show("FILL UP THE REMAINING FIELDS");
                }
                else
                {
                    // Next step: assign values to thisRow
                    thisRow["SSFEDPCODE"] = SubjectEDPCodeTextBox.Text;
                    thisRow["SSFSUBJCODE"] = SubjectCodeTextBox.Text;
                    thisRow["SSFTIMESTART"] = Convert.ToDateTime(TimeStartTextBox.Text);
                    thisRow["SSFENDTIME"] = Convert.ToDateTime(TimeEndTextBox.Text);
                    thisRow["SSFDAYS"] = DaysTextBox.Text;
                    thisRow["SSFSECTION"] = SectionTextBox.Text;
                    thisRow["SSFROOM"] = RoomTextBox.Text;
                    thisRow["SSFSCHOOLYEAR"] = Convert.ToInt32(SchoolYearTextBox.Text);

                    thisDataSet.Tables["SubjectSchedFile"].Rows.Add(thisRow);
                    thisAdapter.Update(thisDataSet, "SubjectSchedFile");

                    MessageBox.Show("Entries Recorded");

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }
    }
}
