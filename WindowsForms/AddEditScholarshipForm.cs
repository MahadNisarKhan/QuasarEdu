using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Window_Forms
{
    public partial class AddEditScholarshipForm : Form
    {
        private readonly int? editId;

        public AddEditScholarshipForm(int? id = null)
        {
            InitializeComponent();

            // Max-Length Constraints
            txtTitle.MaxLength = 100;
            txtDesc.MaxLength = 1000;
            txtEligibility.MaxLength = 1000;
            txtRequiredDocs.MaxLength = 500;
            txtDegreeProgram.MaxLength = 100;
            txtSemesterYear.MaxLength = 50;

            editId = id;

            if (id.HasValue)
                LoadScholarshipData(id.Value);
            chkNeedBased.CheckedChanged += ChkNeedBased_CheckedChanged;
        }

        private void LoadScholarshipData(int id)
        {
            try
            {
                using SqlConnection conn = Database.GetConnection();
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_GetScholarshipById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return;

                txtTitle.Text = reader["Title"].ToString();
                txtDesc.Text = reader["Description"].ToString();
                txtEligibility.Text = reader["Eligibility"].ToString();
                txtAmount.Text = reader["Amount"].ToString();

                if (reader["Deadline"] != DBNull.Value)
                    dtpDeadline.Value = Convert.ToDateTime(reader["Deadline"]);

                chkActive.Checked = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
                txtRequiredDocs.Text = reader["RequiredDocuments"].ToString();
                txtMinCgpa.Text = reader["MinimumCGPA"] == DBNull.Value ? string.Empty : reader["MinimumCGPA"].ToString();
                txtMaxIncome.Text = reader["MaxFamilyIncome"] == DBNull.Value ? string.Empty : reader["MaxFamilyIncome"].ToString();
                txtDegreeProgram.Text = reader["DegreeProgram"].ToString();
                txtSemesterYear.Text = reader["SemesterYear"].ToString();
                chkNeedBased.Checked = reader["NeedBased"] != DBNull.Value && Convert.ToBoolean(reader["NeedBased"]);
            }
            catch (Exception ex)
            {
                ToastForm.ShowError("Scholarship could not be loaded.\n\n" + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Checks/Validations

            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                ToastForm.ShowWarning("Title is required.");
                return;
            }

            if (!TryReadDecimal(txtAmount.Text, "Amount", out decimal amount)) return;

            // Range check: Amount
            if (amount <= 0)
            {
                ToastForm.ShowWarning("Amount must be greater than zero.");
                return;
            }

            if (!TryReadNullableDecimal(txtMinCgpa.Text, "Minimum CGPA", out decimal? minCgpa)) return;

            // Range check: CGPA
            if (minCgpa.HasValue && (minCgpa.Value < 0m || minCgpa.Value > 4.0m))
            {
                ToastForm.ShowWarning("CGPA must be between 0.0 and 4.0.");
                return;
            }

            if (!TryReadNullableDecimal(txtMaxIncome.Text, "Max family income", out decimal? maxIncome)) return;

            // Range check: Income
            if (maxIncome.HasValue && maxIncome.Value < 0)
            {
                ToastForm.ShowWarning("Family Income cannot be negative.");
                return;
            }

            // Date validation for new scholarships
            if (!editId.HasValue && dtpDeadline.Value.Date < DateTime.Today)
            {
                ToastForm.ShowWarning("New scholarships cannot have a deadline in the past.");
                return;
            }

            // DB Save

            try
            {
                using SqlConnection conn = Database.GetConnection();
                conn.Open();

                if (editId.HasValue)
                {
                    using SqlCommand cmd = new SqlCommand("sp_UpdateScholarship", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Id", editId.Value);
                    cmd.Parameters.AddWithValue("@Title", txtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@Description", Database.ValueOrDbNull(txtDesc.Text.Trim()));
                    cmd.Parameters.AddWithValue("@Eligibility", Database.ValueOrDbNull(txtEligibility.Text.Trim()));
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@Deadline", dtpDeadline.Value.Date);
                    cmd.Parameters.AddWithValue("@IsActive", chkActive.Checked);
                    cmd.Parameters.AddWithValue("@ReqDocs", Database.ValueOrDbNull(txtRequiredDocs.Text.Trim()));
                    cmd.Parameters.AddWithValue("@MinCGPA", minCgpa.HasValue ? (object)minCgpa.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaxIncome", maxIncome.HasValue ? (object)maxIncome.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DegreeProgram", Database.ValueOrDbNull(txtDegreeProgram.Text.Trim()));
                    cmd.Parameters.AddWithValue("@SemesterYear", Database.ValueOrDbNull(txtSemesterYear.Text.Trim()));
                    cmd.Parameters.AddWithValue("@NeedBased", chkNeedBased.Checked);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using SqlCommand cmd = new SqlCommand("sp_CreateScholarship", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Title", txtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@Description", Database.ValueOrDbNull(txtDesc.Text.Trim()));
                    cmd.Parameters.AddWithValue("@Eligibility", Database.ValueOrDbNull(txtEligibility.Text.Trim()));
                    cmd.Parameters.AddWithValue("@Deadline", dtpDeadline.Value.Date);
                    cmd.Parameters.AddWithValue("@RequiredDocuments", Database.ValueOrDbNull(txtRequiredDocs.Text.Trim()));
                    cmd.Parameters.AddWithValue("@MinimumCGPA", minCgpa.HasValue ? (object)minCgpa.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaxFamilyIncome", maxIncome.HasValue ? (object)maxIncome.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DegreeProgram", Database.ValueOrDbNull(txtDegreeProgram.Text.Trim()));
                    cmd.Parameters.AddWithValue("@SemesterYear", Database.ValueOrDbNull(txtSemesterYear.Text.Trim()));
                    cmd.Parameters.AddWithValue("@NeedBased", chkNeedBased.Checked);

                    cmd.ExecuteNonQuery();
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ToastForm.ShowError("Scholarship could not be saved.\n\n" + ex.Message);
            }
        }

        private static bool TryReadDecimal(string text, string label, out decimal value)
        {
            if (decimal.TryParse(text, out value))
                return true;

            ToastForm.ShowWarning(label + " must be a number.");
            return false;
        }

        private static bool TryReadNullableDecimal(string text, string label, out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (decimal.TryParse(text, out decimal parsed))
            {
                value = parsed;
                return true;
            }

            ToastForm.ShowWarning(label + " must be a number.");
            return false;
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        private void ChkNeedBased_CheckedChanged(object? sender, EventArgs e)
        {
            const string slipDoc = "Income/Salary Proof";
            var docs = txtRequiredDocs.Text
                .Split(',')
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            if (chkNeedBased.Checked)
            {
                if (!docs.Any(d => d.Equals(slipDoc, StringComparison.OrdinalIgnoreCase)))
                {
                    docs.Add(slipDoc);
                    txtRequiredDocs.Text = string.Join(",", docs);
                }
            }
            else
            {
                docs.RemoveAll(d => d.Equals(slipDoc, StringComparison.OrdinalIgnoreCase));
                txtRequiredDocs.Text = string.Join(",", docs);
            }
        }
    }
}