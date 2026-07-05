namespace Window_Forms
{
    // Bridges GeminiService with the ProfileForm UI.
    // Call ScanAndFillAsync() after a user uploads a document to auto-fill their profile.
    public class DocumentExtractorService
    {
        private readonly Dictionary<string, Control> _fields;
        private readonly Action<string>? _onStatus;  // optional status callback (e.g. update a label)

        // Pass in the same `fields` dictionary that ProfileForm uses
        // and an optional status callback to show progress messages.
        public DocumentExtractorService(
            Dictionary<string, Control> profileFields,
            Action<string>? statusCallback = null)
        {
            _fields   = profileFields;
            _onStatus = statusCallback;
        }

        // MAIN ENTRY POINT
        // Call this right after a document has been saved to disk.
        // filePath     = full path to the saved PDF / image
        // documentType = e.g. "CNIC", "Transcript", "Degree Certificate"
        //
        // Returns the number of fields that were filled.
        public async Task<int> ScanAndFillAsync(string filePath, string documentType)
        {
            _onStatus?.Invoke("🔍 Scanning document with AI...");

            Dictionary<string, string> extracted;

            try
            {
                extracted = await GeminiService.ExtractProfileDataAsync(filePath);
            }
            catch (Exception ex)
            {
                _onStatus?.Invoke("⚠️ Scan failed.");
                throw new Exception($"Document scan failed: {ex.Message}", ex);
            }

            if (extracted.Count == 0)
            {
                _onStatus?.Invoke("ℹ️ No data could be extracted.");
                return 0;
            }

            // Fields to prioritise based on document type
            var priorityFields = GetPriorityFields(documentType);

            int filled = 0;

            // Fill on the UI thread
            if (_fields.Values.FirstOrDefault()?.InvokeRequired == true)
            {
                _fields.Values.First().Invoke(() => filled = FillFields(extracted, priorityFields));
            }
            else
            {
                filled = FillFields(extracted, priorityFields);
            }

            _onStatus?.Invoke(filled > 0
                ? $"✅ {filled} field(s) auto-filled from document."
                : "ℹ️ Document scanned — no matching fields found.");

            return filled;
        }

  
        // Fills form controls with extracted values.
        // Only fills fields that are currently EMPTY to avoid overwriting
        // data the student has already typed.
        // Returns the count of fields actually filled.
  
        private int FillFields(
            Dictionary<string, string> extracted,
            HashSet<string>? priorityFields)
        {
            int count = 0;

            // Build the ordered set: priority fields first, then the rest
            IEnumerable<string> orderedKeys = priorityFields != null
                ? priorityFields.Concat(extracted.Keys.Except(priorityFields))
                : extracted.Keys;

            foreach (string key in orderedKeys)
            {
                if (!extracted.TryGetValue(key, out string? value)) continue;
                if (!_fields.TryGetValue(key, out Control? control))  continue;

                string cleaned = value.Trim();
                if (string.IsNullOrEmpty(cleaned)) continue;

                switch (control)
                {
                    case TextBox tb:
                        // Only fill if empty
                        if (string.IsNullOrWhiteSpace(tb.Text))
                        {
                            tb.Text = cleaned;
                            HighlightControl(tb);
                            count++;
                        }
                        break;

                    case ComboBox cb:
                        if (string.IsNullOrWhiteSpace(cb.Text))
                        {
                            // Try exact match first
                            int idx = cb.FindStringExact(cleaned);
                            if (idx >= 0)
                            {
                                cb.SelectedIndex = idx;
                                HighlightControl(cb);
                                count++;
                            }
                            else
                            {
                                // Case-insensitive partial match
                                for (int i = 0; i < cb.Items.Count; i++)
                                {
                                    if (cb.Items[i]?.ToString()?.Equals(cleaned,
                                        StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        cb.SelectedIndex = i;
                                        HighlightControl(cb);
                                        count++;
                                        break;
                                    }
                                }
                            }
                        }
                        break;

                    case DateTimePicker dtp:
                        // Only fill if at default/min value
                        if (dtp.Value == dtp.MinDate || dtp.Value.Date == DateTime.Today)
                        {
                            if (DateTime.TryParseExact(cleaned, "dd/MM/yyyy",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out DateTime dt))
                            {
                                dtp.Value = dt;
                                count++;
                            }
                            else if (DateTime.TryParse(cleaned, out DateTime dt2))
                            {
                                dtp.Value = dt2;
                                count++;
                            }
                        }
                        break;

                    // Guna2TextBox inherits from TextBox, so the TextBox case above
                    // will catch it. If you have Guna2-specific controls that don't
                    // inherit TextBox, handle them here.
                }
            }

            return count;
        }

   
        // Briefly highlights a control that was auto-filled so the user
        // can quickly see what changed.
        private static void HighlightControl(Control control)
        {
            Color originalColor = control.BackColor;
            control.BackColor = Color.FromArgb(220, 252, 231);  // soft green

            var timer = new System.Windows.Forms.Timer { Interval = 2500 };
            timer.Tick += (_, _) =>
            {
                control.BackColor = originalColor;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        // ---------------------------------------------------------------
        // Returns the most relevant fields for each document type so they
        // are filled first (and empty-check ordering feels natural).
        // ---------------------------------------------------------------
        private static HashSet<string>? GetPriorityFields(string documentType)
        {
            return documentType.ToLower() switch
            {
                var d when d.Contains("cnic") || d.Contains("national") => new HashSet<string>
                {
                    "FullName", "FatherName", "CNIC", "DateOfBirth",
                    "Gender", "PermanentAddress", "District", "Province"
                },

                var d when d.Contains("transcript") || d.Contains("result") || d.Contains("mark") => new HashSet<string>
                {
                    "UniversityName", "Department", "DegreeProgram",
                    "RegistrationNumber", "RollNumber", "CGPA", "SemesterYear",
                    "SSCBoard", "SSCRollNo", "SSCYear", "SSCMarks", "SSCPercentage", "SSCInstitute",
                    "HSSCBoard", "HSSCRollNo", "HSSCYear", "HSSCMarks", "HSSCPercentage", "HSSCInstitute"
                },

                var d when d.Contains("domicile") => new HashSet<string>
                {
                    "FullName", "FatherName", "Province", "District",
                    "DomicileDistrict", "TownVillage", "PermanentAddress"
                },

                var d when d.Contains("income") || d.Contains("salary") => new HashSet<string>
                {
                    "FullName", "FatherName", "FamilyIncome"
                },

                var d when d.Contains("degree") || d.Contains("certificate") => new HashSet<string>
                {
                    "FullName", "FatherName", "UniversityName", "Department",
                    "DegreeProgram", "RegistrationNumber"
                },

                _ => null  // no priority — fill everything we can find
            };
        }
    }
}
