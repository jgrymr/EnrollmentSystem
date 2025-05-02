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
    public partial class StudentEnrollmentEntry: Form
    {
        string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\63915\Documents\Goco.mdf;Integrated Security=True;Connect Timeout=30";

        public StudentEnrollmentEntry()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void IdNumberTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Enter)
            {
                bool found = false;
                SqlConnection myConnection = new SqlConnection(connectionString);
                myConnection.Open();
                SqlCommand studentCommand = myConnection.CreateCommand();

                studentCommand.CommandText = "SELECT * FROM STUDENTFILE";

                SqlDataReader studentDataReader = studentCommand.ExecuteReader();
                while (studentDataReader.Read())
                {
                    if (studentDataReader["STFSTUDID"].ToString().ToUpper() == IdNumberTextBox.Text.ToUpper())
                    {
                        NameTextBox.Text = studentDataReader["STFSTUDLNAME"].ToString() + ", " + studentDataReader["STFSTUDFNAME"].ToString();
                        CourseTextBox.Text = studentDataReader["STFSTUDCOURSE"].ToString();
                        YearTextBox.Text = studentDataReader["STFSTUDYEAR"].ToString();
                        found = true;
                        break;
                    }
                    continue;
                }

                if (!found)
                {
                    MessageBox.Show("ID Number not Found", "No ID Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            MainMenu MM = new MainMenu();
            MM.Show();

            this.Hide();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string studentId = IdNumberTextBox.Text.Trim();
            string encodedBy = EncodedByTextBox.Text.Trim();
            string status = "EN";
            int totalUnits = int.TryParse(TotalUnitsLabel.Text.Trim(), out int result) ? result : 0;
            DateTime dateEnrolled = DateTimePicker.Value;

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(encodedBy) || SubjectChoosedDataGridView.Rows.Count == 0)
            {
                MessageBox.Show("Please fill out all required fields and add at least one subject.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string firstEDPCode = null;
            foreach (DataGridViewRow row in SubjectChoosedDataGridView.Rows)
            {
                if (!row.IsNewRow)
                {
                    firstEDPCode = row.Cells["EDPCodeColumn"].Value?.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(firstEDPCode))
            {
                MessageBox.Show("Cannot determine school year.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string schoolYear = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                bool success = false; 
                try
                {
                    using (SqlCommand getSY = new SqlCommand("SELECT SSFSCHOOLYEAR FROM SubjectSchedFile WHERE SSFEDPCODE = @edp", conn, transaction))
                    {
                        getSY.Parameters.AddWithValue("@edp", firstEDPCode);
                        object resultSY = getSY.ExecuteScalar();
                        if (resultSY != null && int.TryParse(resultSY.ToString(), out int sy))
                        {
                            schoolYear = sy.ToString();
                        }
                        else
                        {
                            MessageBox.Show("Could not retrieve school year for EDP code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    
                    // Insert into EnrollmentHeaderFile
                    string insertHeaderSql = @"INSERT INTO EnrollmentHeaderFile
                        (ENRHFSTUDID, ENRHFSTUDDATEENROLL, ENRHFSTUDSCHLYR, ENRHFSTUDENCODER, ENRHFSTUDTOTALUNITS, ENRHFSTUDSTATUS)
                        VALUES (@StudentId, @DateEnroll, @SchoolYear, @Encoder, @TotalUnits, @Status)";
            
                    using (SqlCommand cmdHeader = new SqlCommand(insertHeaderSql, conn, transaction))
                    {
                        cmdHeader.Parameters.AddWithValue("@StudentId", studentId);
                        cmdHeader.Parameters.AddWithValue("@DateEnroll", dateEnrolled);
                        cmdHeader.Parameters.AddWithValue("@SchoolYear", schoolYear);
                        cmdHeader.Parameters.AddWithValue("@Encoder", encodedBy);
                        cmdHeader.Parameters.AddWithValue("@TotalUnits", totalUnits);
                        cmdHeader.Parameters.AddWithValue("@Status", status);
                        cmdHeader.ExecuteNonQuery();
                    }

                    // Insert into EnrollmentDetailFile
                    foreach (DataGridViewRow row in SubjectChoosedDataGridView.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string edpCode = row.Cells["EDPCodeColumn"].Value?.ToString() ?? "";
                        string subjectCode = row.Cells["SubjectCodeColumn"].Value?.ToString() ?? "";

                        if (string.IsNullOrEmpty(edpCode) || string.IsNullOrEmpty(subjectCode)) continue;

                        string insertDetailSql = @"INSERT INTO EnrollmentDetailFile
                            (ENRDFSTUDID, ENRDFSTUDSUBJCDE, ENRDFSTUDEDPCODE, ENRDFSTUDSTATUS)
                            VALUES (@StudentId, @SubjectCode, @EDPCode, @Status)";

                        using (SqlCommand cmdDetail = new SqlCommand(insertDetailSql, conn, transaction))
                        {
                            cmdDetail.Parameters.AddWithValue("@StudentId", studentId);
                            cmdDetail.Parameters.AddWithValue("@SubjectCode", subjectCode);
                            cmdDetail.Parameters.AddWithValue("@EDPCode", edpCode);
                            cmdDetail.Parameters.AddWithValue("@Status", status);
                            cmdDetail.ExecuteNonQuery();
                        }
                    }
                    success = true;
                    transaction.Commit();
                    MessageBox.Show("Enrollment saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving enrollment: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (!success)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        private void EDPCodeTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Prevent the 'ding' sound
                string edpCodeInput = EDPCodeTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(edpCodeInput))
                {
                    return; // Do nothing if input is empty
                }

                // --- 1. Check if EDP code already in DataGridView ---
                bool alreadyAdded = false;
                foreach (DataGridViewRow rw in SubjectChoosedDataGridView.Rows)
                {
                    if (rw.IsNewRow) continue;
                    if (rw.Cells["EDPCodeColumn"].Value?.ToString().Equals(edpCodeInput, StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                {
                    MessageBox.Show("Subject with this EDP Code is already added.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Added Icon
                    EDPCodeTextBox.SelectAll();
                    return;
                }
                string subjectCode = null;
                DateTime startTime = DateTime.MinValue;
                DateTime endTime = DateTime.MinValue;
                string days = null;
                string room = null;
                double units = 0.0;
                int maxSize = 0;
                int classSize = -1;
                bool scheduleFound = false;

                string combinedSql = @"
                    SELECT
                        ss.SSFSUBJCODE, ss.SSFSTARTTIME, ss.SSFENDTIME, ss.SSFDAYS,
                        ss.SSFROOM, ss.SSFMAXSIZE, ss.SSFCLASSSIZE, sf.SFSUBJUNITS
                    FROM SUBJECTSCHEDFILE ss
                    LEFT JOIN SUBJECTFILE sf ON ss.SSFSUBJCODE = sf.SFSUBJCODE
                    WHERE ss.SSFEDPCODE = @EdpCode";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(combinedSql, conn))
                {
                    cmd.Parameters.AddWithValue("@EdpCode", edpCodeInput);
                    SqlDataReader reader = null;

                    try
                    {
                        conn.Open();
                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            scheduleFound = true;
                            subjectCode = reader["SSFSUBJCODE"] as string;
                            startTime = reader["SSFSTARTTIME"] as DateTime? ?? DateTime.MinValue;
                            endTime = reader["SSFENDTIME"] as DateTime? ?? DateTime.MinValue;

                            days = reader["SSFDAYS"] as string;
                            room = reader["SSFROOM"] as string;
                            maxSize = reader["SSFMAXSIZE"] != DBNull.Value ? Convert.ToInt32(reader["SSFMAXSIZE"]) : 0;
                            classSize = reader["SSFCLASSSIZE"] != DBNull.Value ? Convert.ToInt32(reader["SSFCLASSSIZE"]) : 0;
                            units = reader["SFSUBJUNITS"] != DBNull.Value ? Convert.ToDouble(reader["SFSUBJUNITS"]) : 0.0;

                            if (string.IsNullOrWhiteSpace(subjectCode))
                            {
                                MessageBox.Show($"Schedule found for EDP Code '{edpCodeInput}', but linked Subject Code is missing.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                scheduleFound = false;
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show($"SQL Database Error: {sqlEx.Message}\nError Code: {sqlEx.Number}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while fetching data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    finally
                    {
                        reader?.Close();
                    }
                }


                if (!scheduleFound)
                {
                    MessageBox.Show("EDP Code Not Found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    EDPCodeTextBox.SelectAll();
                    return;
                }

                if (classSize >= maxSize)
                {
                    MessageBox.Show("Subject for this EDP Code is already closed (Class Full).", "Class Full", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    EDPCodeTextBox.SelectAll();
                    return;
                }

                double currentTotalUnits = 0.0;
                double.TryParse(TotalUnitsLabel.Text, out currentTotalUnits);
                if ((currentTotalUnits + units) > 24.0)
                {
                    MessageBox.Show($"Adding this subject ({units} units) would exceed the 24 unit limit.", "Unit Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    EDPCodeTextBox.SelectAll();
                    return;
                }
                bool conflictFound = false;
                foreach (DataGridViewRow rw in SubjectChoosedDataGridView.Rows)
                {
                    if (rw.IsNewRow) continue;

                    try
                    {
                        string existingDays = rw.Cells["DaysColumn"].Value?.ToString() ?? "";

                        DateTime existingStartTime = DateTime.MinValue;
                        DateTime existingEndTime = DateTime.MinValue;


                        if (rw.Cells["StartTimeColumn"].Value is DateTime dtStart)
                            existingStartTime = dtStart;
                        else if (DateTime.TryParse(rw.Cells["StartTimeColumn"].Value?.ToString(), out DateTime parsedDtStart))
                            existingStartTime = parsedDtStart;


                        if (rw.Cells["EndTimeColumn"].Value is DateTime dtEnd)
                            existingEndTime = dtEnd;
                        else if (DateTime.TryParse(rw.Cells["EndTimeColumn"].Value?.ToString(), out DateTime parsedDtEnd))
                            existingEndTime = parsedDtEnd;


                        if (HasDaysConflict(days, existingDays))
                        {
                            if (HasTimeConflict(startTime, endTime, existingStartTime, existingEndTime))
                            {
                                conflictFound = true;
                                break;
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error parsing grid data for conflict check: {parseEx.Message}");
                    }
                }

                if (conflictFound)
                {
                    MessageBox.Show("Schedule conflict detected with an already added subject.", "Schedule Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Added Icon
                    EDPCodeTextBox.SelectAll();
                    return;
                }

                int index = SubjectChoosedDataGridView.Rows.Add();
                DataGridViewRow newRow = SubjectChoosedDataGridView.Rows[index];

                newRow.Cells["EDPCodeColumn"].Value = edpCodeInput;
                newRow.Cells["SubjectCodeColumn"].Value = subjectCode;
                newRow.Cells["StartTimeColumn"].Value = startTime;
                newRow.Cells["EndTimeColumn"].Value = endTime;
                newRow.Cells["DaysColumn"].Value = days;
                newRow.Cells["RoomColumn"].Value = room;
                newRow.Cells["UnitsColumn"].Value = units;

                EDPCodeTextBox.Clear();
                EDPCodeTextBox.Focus();
            }
        }


        bool HasTimeConflict(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {

            return (start1.TimeOfDay < end2.TimeOfDay && end1.TimeOfDay > start2.TimeOfDay);
        }

        bool HasDaysConflict(string days, string dayList)
        {
            bool daysConflict = false;
            if (string.IsNullOrWhiteSpace(days) || string.IsNullOrWhiteSpace(dayList)) return false; // Handle empty strings
            string daysUpper = days.ToUpper().Trim();
            string dayListUpper = dayList.ToUpper().Trim();
            for (int i = 0; i < dayListUpper.Length; i++)
            {
                char day = dayListUpper[i];
                if (dayListUpper.Contains(day)) // Problematic check
                {
                    daysConflict = true;
                    break;
                }
            }
            return daysConflict;
        }

    }
}
