using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Window_Forms.Repositories;
namespace Window_Forms
{
    public partial class AllDocumentsForm : Form
    {
        // Made readonly since it shouldn't change after the form opens
        private readonly string? _email;
        private readonly int? _applicationId;
        public AllDocumentsForm(string? email = null, int? applicationId = null)
        {
            InitializeComponent();
            _email = email;
            _applicationId = applicationId;

            if (_applicationId.HasValue)
                lblHeaderTitle.Text = $"Documents - Application #{_applicationId}";
            else if (!string.IsNullOrWhiteSpace(_email))
                lblHeaderTitle.Text = $"Documents - {_email}";
        }

        private void AllDocumentsForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Lock down the data grid
                dgvDocuments.ReadOnly = true;
                dgvDocuments.AllowUserToAddRows = false;
                dgvDocuments.AllowUserToDeleteRows = false;
                dgvDocuments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvDocuments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                DataTable dt;

                if (_applicationId.HasValue)
                {
                    dt = new ApplicationRepository().GetApplicationDocuments(_applicationId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(_email))
                {
                    dt = Database.QueryProcedure("sp_GetDocumentsByEmail",
                        new SqlParameter("@Email", _email));
                }
                else
                {
                    dt = Database.QueryProcedure("sp_GetAllDocuments");
                }

                dgvDocuments.DataSource = dt;

                // Hide system columns
                if (dgvDocuments.Columns["FilePath"] is DataGridViewColumn filePathColumn)
                    filePathColumn.Visible = false;

                if (dgvDocuments.Columns["Id"] is DataGridViewColumn idColumn)
                    idColumn.Visible = false;

                // hide the "Student" column if they are viewing their own documents
                if (!string.IsNullOrWhiteSpace(_email) && dgvDocuments.Columns["Student"] is DataGridViewColumn studentCol)
                    studentCol.Visible = false;

                if (dgvDocuments.Columns["UploadDate"] is DataGridViewColumn uploadDateColumn)
                {
                    uploadDateColumn.HeaderText = "Upload Date";
                    uploadDateColumn.DefaultCellStyle.Format = "dd MMM yyyy";
                }

                FormatColumnHeader("DocumentType", "Document Type");
                FormatColumnHeader("FileName", "File Name");
                FormatColumnHeader("UploadDate", "Upload Date");
            }
            catch (Exception ex)
            {
                ToastForm.ShowError("Documents could not be loaded.\n\n" + ex.Message);
                this.Close();
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            string? filePath = GetSelectedFilePath();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // Check if file actually exists on the disk
                if (!File.Exists(filePath))
                {
                    ToastForm.ShowError("File not found. It may have been moved or deleted.");
                    return;
                }

                DocumentPrintService.OpenDocument(filePath);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            string? filePath = GetSelectedFilePath();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                // Check if file actually exists on the disk
                if (!File.Exists(filePath))
                {
                    ToastForm.ShowError("File not found. It may have been moved or deleted.");
                    return;
                }

                DocumentPrintService.PrintDocument(filePath);
            }
        }

        private string? GetSelectedFilePath()
        {
            if (dgvDocuments.SelectedRows.Count == 0)
            {
                ToastForm.ShowInfo("Select a document first.");
                return null;
            }

            return dgvDocuments.SelectedRows[0].Cells["FilePath"].Value?.ToString();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormatColumnHeader(string columnName, string headerText)
        {
            if (dgvDocuments.Columns[columnName] is DataGridViewColumn col)
            {
                col.HeaderText = headerText;
            }
        }
    }
}