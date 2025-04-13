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
    public partial class SubjectEntry : Form
    {
        public SubjectEntry()
        {
            InitializeComponent();
        }

        private void SubjectCodeTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (Char)Keys.Enter)
            {
                String connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\63915\Documents\Goco.mdf;Integrated Security=True;Connect Timeout=30";
                SqlConnection myConnection = new SqlConnection(connectionString);
                myConnection.Open();

                String commandText = "SELECT * FROM SUBJECTFILE";
                SqlCommand thisCommand = myConnection.CreateCommand();
                thisCommand.CommandText = commandText;

                SqlDataReader thisReader = thisCommand.ExecuteReader();

                bool flag = false;
                while (thisReader.Read())
                {
                    if (thisReader["SFSUBJCODE"].ToString().Trim() == SubjectCodeTextBox.Text.Trim())
                    {
                        flag = true;
                        DescriptionLabel2.Text = thisReader["SFSUBJDESC"].ToString();
                        break;
                    }//end if
                }//end while
            }
        }
    }
}
