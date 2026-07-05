using BCrypt.Net; // Requires BCrypt.Net-Next
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace Window_Forms
{
    public partial class ForgotPasswordForm : Form
    {
        private string _generatedOtp = "";
        private DateTime _otpExpiryTime;
        private int _otpAttempts = 0;
        private const int MAX_OTP_ATTEMPTS = 3;

        public ForgotPasswordForm()
        {
            InitializeComponent();
        }

        private void BtnSendOtp_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                ToastForm.ShowWarning("Enter your email address.");
                return;
            }

            // Email format validation
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$"))
            {
                ToastForm.ShowWarning("Please enter a valid email address.");
                return;
            }

           // Block admin account from being reset
            if (email.Equals("admin@sms.local", StringComparison.OrdinalIgnoreCase))
            {
                ToastForm.ShowWarning("This account cannot be reset.");
                return;
            }

            try
            {
                DataTable table = Database.QueryProcedure("sp_CheckEmailExists",
                new SqlParameter("@Email", email));

                if (table.Rows.Count == 0)
                {
                    ToastForm.ShowWarning("Email not found.");
                    return;
                }

                _generatedOtp = Utils.GenerateOtp();
                Utils.SendOtpEmail(email, _generatedOtp, isPasswordReset: true);

                // Set OTP expiry to 5 minutes from now
                _otpExpiryTime = DateTime.Now.AddMinutes(5);
                _otpAttempts = 0; // Reset attempts for the new OTP

                lblOtpSent.Visible = true;
                lblOtp.Visible = true;
                txtOtp.Visible = true;
                btnSendOtp.Visible = false;

                // Disable email textbox to prevent Email Swap attacks
                txtEmail.Enabled = false;

                txtOtp.Focus();

                ToastForm.ShowInfo("OTP sent to your email. It will expire in 5 minutes.");
            }
            catch (Exception ex)
            {
                ToastForm.ShowError($"Could not send OTP.\n\n{ex.Message}");
            }
        }

        private void BtnResetPassword_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string otp = txtOtp.Text.Trim();
            string newPwd = txtNewPassword.Text.Trim();
            string confirmPwd = txtConfirmPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPwd) || string.IsNullOrWhiteSpace(confirmPwd))
            {
                ToastForm.ShowWarning("Fill in all fields.");
                return;
            }

            // Limit OTP guess attempts
            if (_otpAttempts >= MAX_OTP_ATTEMPTS)
            {
                ToastForm.ShowError("Too many failed attempts. Please request a new OTP.");
                ResetFormState();
                return;
            }

            // Check if OTP has expired
            if (DateTime.Now > _otpExpiryTime)
            {
                ToastForm.ShowWarning("OTP has expired. Please request a new one.");
                ResetFormState();
                return;
            }

            // Verify OTP
            if (otp != _generatedOtp)
            {
                _otpAttempts++;
                int attemptsLeft = MAX_OTP_ATTEMPTS - _otpAttempts;
                ToastForm.ShowWarning($"Invalid OTP. You have {attemptsLeft} attempts left.");
                return;
            }

            if (newPwd != confirmPwd)
            {
                ToastForm.ShowWarning("Passwords do not match.");
                return;
            }

            if (newPwd.Length < 6)
            {
                ToastForm.ShowWarning("Password must be at least 6 characters.");
                return;
            }

            try
            {
                // Hash the new password before saving it to the database
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPwd);

                Database.ExecuteProcedure("sp_ResetPassword",
                new SqlParameter("@Email", email),
                new SqlParameter("@Password", hashedPassword));

                ToastForm.ShowInfo("Password reset successfully.");
                Close();
            }
            catch (Exception ex)
            {
                ToastForm.ShowError($"Password reset failed.\n\n{ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Helper method to reset the UI if OTP fails too many times or expires
        private void ResetFormState()
        {
            _generatedOtp = "";
            txtOtp.Text = "";
            txtEmail.Enabled = true;
            btnSendOtp.Visible = true;
            lblOtpSent.Visible = false;
        }
    }
}