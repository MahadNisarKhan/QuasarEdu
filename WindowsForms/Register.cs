using System;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;

namespace Window_Forms
{
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Login login = new Login();
            login.Show();
            this.Hide();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            // CHECK 1: Prevent double-clicks / spamming the SMTP server
            if (sender is Control btn) btn.Enabled = false;

            try
            {
                // CHECK 2: Email Normalization (treat uppercase/lowercase the same)
                string email = txtEmail.Text.Trim().ToLower();

                // CHECK 3: Safe Password Handling (Don't trim passwords, spaces might be intentional)
                string password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ToastForm.ShowWarning("Fill all fields.");
                    return;
                }

                // CHECK 4: Strict Email Format Validation
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ToastForm.ShowWarning("Please enter a valid email address.");
                    return;
                }

                // CHECK 5: Minimum Password Complexity Enforcement
                if (password.Length < 6)
                {
                    ToastForm.ShowWarning("Password must be at least 6 characters long.");
                    return;
                }

                DataTable existing = Database.QueryProcedure("sp_GetUserVerificationStatus",
                new SqlParameter("@Email", email));

                if (existing.Rows.Count > 0 && Convert.ToBoolean(existing.Rows[0]["IsVerified"]))
                {
                    ToastForm.ShowWarning("User already exists and is verified. Please log in.");
                    return;
                }

                // SECURITY NOTE: The passwords here are saved in plain-text. 
                // In a production environment, you must hash these (e.g., using BCrypt) before saving.
                // (Left as plain-text here so your existing Login form doesn't break).

                if (existing.Rows.Count == 0)
                {
                    using SqlConnection regConn = Database.GetConnection();
                    regConn.Open();
                    using SqlCommand regCmd = new SqlCommand("sp_RegisterUser", regConn);
                    regCmd.CommandType = System.Data.CommandType.StoredProcedure;
                    regCmd.Parameters.AddWithValue("@Email", email);
                    regCmd.Parameters.AddWithValue("@Password", password);
                    regCmd.Parameters.AddWithValue("@Role", "Student");
                    regCmd.ExecuteNonQuery();
                }
                else
                {
                    Database.ExecuteProcedure("sp_ResetUnverifiedUser",
                    new SqlParameter("@Email", email),
                    new SqlParameter("@Password", password));
                }

                string otp = Utils.GenerateOtp();

                // Note: If SendOtpEmail is a slow synchronous method, the UI will freeze briefly here.
                Utils.SendOtpEmail(email, otp);

                OTP otpForm = new OTP(email, otp);
                otpForm.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                ToastForm.ShowError("Registration could not be completed.\n\n" + ex.Message);
            }
            finally
            {
                // Re-enable the button in case the registration failed or returned early
                if (sender is Control btnn) btnn.Enabled = true;
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            System.Windows.Forms.Application.Exit();
        }
    }
}