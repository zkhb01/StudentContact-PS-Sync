// See https://aka.ms/new-console-template for more information

using CsvHelper;
using CsvHelper.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using StudentContact_PS_Sync;
using System.Globalization;


bool isMockRun = args.Any(a => a.Equals("mockRun", StringComparison.OrdinalIgnoreCase));
bool useReprocess = args.Any(a => a.Equals("useReprocess", StringComparison.OrdinalIgnoreCase));

string basePath = "D:\\Foundation\\RycorData\\";

string csvHeader = ""; // Store the header
var webDriver = new PsWebDriver(basePath, isMockRun);
webDriver.LoginToPS();
ProcessStudentContactFile();
webDriver.Quit();


void ProcessStudentContactFile()
{
    string studentContactFilePath = basePath + "ContactUpdateFormSubmitted(Feb23).csv";
    string reprocessFilePath = basePath + "reprocess.csv"; // New file for failed records
    string checkpointFilePath = basePath + "checkpoint.txt";

    // Determine which file to process
    string filePathToProcess = studentContactFilePath;
    if (useReprocess)
    {
        // Need to turn off useReporcess after it's been run it the process dies and you want to restart where you left off
        if (File.Exists(reprocessFilePath))
        {
            // Rename Reprocess.csv with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string renamedReprocessFilePath = basePath + $"reprocess_{timestamp}.csv";
            File.Move(reprocessFilePath, renamedReprocessFilePath);
            filePathToProcess = renamedReprocessFilePath;
            webDriver.Log($"Using renamed reprocess file: {renamedReprocessFilePath}");
        }
        else
        {
            Console.WriteLine("Reprocess.csv not found; All records should have been processed successfully then. Ending process.");
            return;
        }

        if (File.Exists(checkpointFilePath))
        {
            // Clear checkpoint.txt (overwrite with "") when using reprocess
            File.WriteAllText(checkpointFilePath, "");
            webDriver.Log("Cleared checkpoint.txt for reprocess run.");
        }
    }
    else
    {
        // Check if the studentContact file exists
        if (!File.Exists(filePathToProcess))
        {
            webDriver.Log("The file does not exist: " + studentContactFilePath);
            return;
        }
    }
    // Read last checkpoint (StateStudentNumber)
    string lastProcessedId = "";
    if (File.Exists(filePathToProcess))
    {
        lastProcessedId = File.ReadAllText(checkpointFilePath).Trim();
        if (lastProcessedId != "")
            webDriver.Log($"Run resuming from last successful student: {lastProcessedId}");
    }
    else
    {
        webDriver.Log($"Run starting with first student");
    }

    int numberOfRecords = 0;
    int numberOfRecordsProcessed = 0;
    int numberOfRecordsUpdated = 0;

    try
    {
        InitializeDriver();
        // Read all lines from the CSV file
        List<StudentContact> contacts = LoadStudentContactsFromFile(filePathToProcess, lastProcessedId);
        numberOfRecords = contacts.Count();
        if (lastProcessedId != "")
            webDriver.Log($"Resuming from StateStudentNumber: \"{lastProcessedId}\". Records remaining: {numberOfRecords}");
        else
            webDriver.Log($"Starting from first StateStudentNumber: \"{contacts[0].StudentId}\". Records: {numberOfRecords}");

        // Process each line
        foreach (var contact in contacts)
        {
            bool atStudentProfilePage = false;
            try
            {
                string stateStudentNumber = contact.StudentId;
                if (contact.Q5NoUpdate == "FALSE" || contact.Q22PG1NoUpdate == "FALSE")
                {
                    if (webDriver.FindStudentOnStudentSearchPage(stateStudentNumber) )
                    { 
                        if (webDriver.GoToStudentPage())
                        {
                            // Student address update
                            string currentAddress = contact.Q4StuCurrAdd;
                            if (contact.Q5NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(currentAddress))
                            {
                                try
                                {
                                    webDriver.GoToStudentCompliantPage();
                                    ProcessStudentAddress(contact, webDriver);
                                    // Brief pause to allow the dialog to appear (e.g., 5 seconds timeout)
                                    // Check for the sibling dialog and click Submit if present
                                    if (!isMockRun)
                                        webDriver.HandleSiblingAddressDialog();
                                }
                                catch (Exception)
                                {
                                    string csvRow = ReconstituteCsvRow(contact);
                                    WriteToReprocessFile(csvRow, reprocessFilePath);
                                }
                                webDriver.Log($"completed processing student only portion of demographic for: {stateStudentNumber}");
                            }

                            // Student First Contact
                            currentAddress = contact.Q25NewPG1Add;
                            if (contact.Q22PG1NoUpdate == "FALSE")
                            {
                                webDriver.GoToStudenProfilePage();
                                atStudentProfilePage = true;
                                string relationship = contact.Q13Relationship;
                                if (ProcessContact(stateStudentNumber, relationship, contact.Q14PG1FN, contact.Q16PG1LN))
                                {
                                    try 
                                    { 
                                        // address
                                        if (!string.IsNullOrWhiteSpace(currentAddress))
                                            ProcessContactAddress(stateStudentNumber, relationship, contact.Q21PG1Add, contact.Q24NewPG1Unit, contact.Q25NewPG1Add, contact.Q26PG1City, contact.Q27PG1Prov, contact.Q28PG1PC);
                                        // email
                                        var email = contact.Q29PG1Email;
                                        if (!string.IsNullOrWhiteSpace(email))
                                        {
                                            var isPrimary = contact.Q32Primary.Trim().ToUpper() == "TRUE";
                                            ProcessStudentContactEmail(stateStudentNumber, relationship, email, isPrimary, "Current");
                                        }
                                        // first phone
                                        var phone = contact.Q33PG1Ph1;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q34Cell, contact.Q35Work, contact.Q36Home);
                                            bool acceptsText = (contact.Q37PG1Txt1.Trim().ToUpper() == "TRUE");
                                            var isPreferred = true;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                        }
                                        // second phone
                                        phone = contact.Q38PG1Ph2;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q39Cell2, contact.Q40Work2, contact.Q41Home2);
                                            bool acceptsText = (contact.Q42PG1Txt2.Trim().ToUpper() == "TRUE");
                                            bool isPreferred = false;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText,isPreferred, phoneType);
                                        }
                                        // third phone
                                        phone = contact.Q43PG1Ph3;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q44Cell3, contact.Q45Work3, contact.Q46Home3);
                                            bool acceptsText = (contact.Q47PG1Txt3.Trim().ToUpper() == "TRUE");
                                            bool isPreferred = false;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                        }
                                    }

                                    catch (Exception)
                                    {
                                        string csvRow = ReconstituteCsvRow(contact);
                                        WriteToReprocessFile(csvRow, reprocessFilePath);
                                    }
                                    webDriver.Log($"completed processing 1st parent for: {stateStudentNumber}, {relationship}");
                                    //need to close window
                                    webDriver.CloseContactTab();
                                }
                            }

                            // Student Second Contact
                            currentAddress = contact.Q59PG2Add;
                            if (contact.Q56SameAddress == "FALSE")
                            {
                                if (!atStudentProfilePage)
                                  webDriver.GoToStudenProfilePage();

                                string relationship = contact.Q48RelationshipToStudent;
                                if (ProcessContact(stateStudentNumber, relationship, contact.Q49PG2FN, contact.Q51PG2LN))
                                {
                                    try
                                    {
                                        // address
                                        if (!string.IsNullOrWhiteSpace(currentAddress))
                                            ProcessContactAddress(stateStudentNumber, relationship, contact.Q59PG2Add, contact.Q58PG2Apt, contact.Q59PG2Add, contact.Q60PG2City, contact.Q61PG2Prov, contact.Q62PG2PC);

                                        // email
                                        var email = contact.Q63PG2Email;
                                        if (!string.IsNullOrWhiteSpace(email))
                                        {
                                            ProcessStudentContactEmail(stateStudentNumber, relationship, email, true, "Current");
                                        }

                                        // first phone
                                        string phone = contact.Q67PG2Ph1;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q68CellPG2, contact.Q69WorkPG2, contact.Q70HomePG2);
                                            bool acceptsText = (contact.Q71PG2Ph1Txt.Trim().ToUpper() == "TRUE");
                                            var isPreferred = true;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                        }
                                        // second phone
                                        phone = contact.Q72PG2Ph2;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q73Cell2PG2, contact.Q74Work2PG2, contact.Q75Home2PG2);
                                            bool acceptsText = (contact.Q76PG2Ph2Txt.Trim().ToUpper() == "TRUE");
                                            bool isPreferred = false;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                        }
                                        // third phone
                                        phone = contact.Q77PG2Ph3;
                                        if (!string.IsNullOrWhiteSpace(phone))
                                        {
                                            string phoneType = DeterminePhoneType(contact.Q78Cell3PG2, contact.Q79Work3PG2, contact.Q80Home3PG2);
                                            bool acceptsText = (contact.Q81PG2Ph3Txt.Trim().ToUpper() == "TRUE");
                                            bool isPreferred = false;
                                            ProcessStudentContactPhone(stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        string csvRow = ReconstituteCsvRow(contact);
                                        WriteToReprocessFile(csvRow, reprocessFilePath);
                                    }
                                    webDriver.Log($"completed processing 2nd parent for: {stateStudentNumber}, {relationship}");
                                    webDriver.CloseContactTab();
                                }
                            }

                            File.WriteAllText(checkpointFilePath, stateStudentNumber);
                            numberOfRecordsUpdated++;
                            webDriver.Log($"Checkpoint updated to: {stateStudentNumber}. {numberOfRecordsUpdated} record updated, {(numberOfRecordsProcessed + 1)} records processed, out of {numberOfRecords}");
                        }
                        else
                        {
                            webDriver.Log($"Unable to load student with ASN: {stateStudentNumber}", false);
                        }
                    }
                    else
                    {
                        webDriver.Log($"Unable to find student with ASN: {stateStudentNumber}",false);
                    }
                    atStudentProfilePage = false;
                    webDriver.GoToStudentSearchPage();
                }// else no changes to pickup
            }
            catch (WebDriverException ex) when (ex.Message.Contains("invalid session id"))
            {
                throw;
            }
            catch (Exception ex)
            {
                webDriver.Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                webDriver.Log(ex.StackTrace);
            }
            numberOfRecordsProcessed++;
        }
    }
    // need to put this in a better spot to pick up after session drops
    catch (WebDriverException ex) when (ex.Message.Contains("invalid session id"))
    {
        webDriver.Log($"Session lost: {ex.Message}. Restarting WebDriver...");
        CleanupDriver();
    }
    catch (Exception ex)
    {
        webDriver.Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
        webDriver.Log(ex.Message);
        webDriver.Log(ex.StackTrace);
    }

}

void ProcessStudentAddress(StudentContact contact, PsWebDriver webDriver)
{
    try
    {
        //webDriver.Log($"StudentId: {contact.StudentId}, Student: {contact.LastName}, {contact.FirstName}, CurrentAddr: {contact.Q4StuCurrAdd}, NewStreet: {contact.Q8NewStuAdd}, NewCity: {contact.Q9City}, NewProv: {contact.Q10Prov}, NewZip: {contact.Q11PC}, NewEffDt: {contact.Q12NewAddrEffDt}");
        string currentAddress = contact.Q4StuCurrAdd;
        string stateStudentNumber = contact.StudentId;
        string street = contact.Q8NewStuAdd;
        string city = contact.Q9City;
        string state = contact.Q10Prov;
        string postalcode = contact.Q11PC;
        DateTime? effectiveDate = contact.Q12NewAddrEffDt;
        //webDriver.Log($"StudentId: {stateStudentNumber}, Student: {contact.LastName}, {contact.FirstName}, CurrentAddr: {currentAddress}, NewStreet: {street}, NewCity: {city}, NewProv: {state}, NewZip: {postalcode}, NewEffDt: {effectiveDate}");

        if (webDriver.GoToStudentCompliantAddressPage(stateStudentNumber, currentAddress, street, city, state, postalcode, effectiveDate))
        {
            webDriver.EnterEditAddress(street, city, state, postalcode, effectiveDate);
            webDriver.Log($"Address for student with ASN: {stateStudentNumber} Saved");
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Address for {contact.StudentId}:\r\n  {ex.Message}, {ex.GetType().FullName}",false);
        throw;
    }
}

bool ProcessContact(string studentId, string relationship, string contactFirstName, string contactLastName)
{
    try
    {
        return webDriver.GoToContactDetailsPage(contactFirstName, contactLastName, relationship);
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Contact Details page for {studentId}, {relationship}:\r\n  {ex.Message}, {ex.GetType().FullName}", false);
        return false;
    }
}

void ProcessContactAddress(string stateStudentNumber, string relationship, string currentAddress, string unit, string street, string city, string state, string zip)
{
    try
    {
        if (webDriver.GoToContactDetailsAddressPage(stateStudentNumber, relationship, currentAddress, unit, street, city, state, zip))
        {
            webDriver.EnterContactAddress(stateStudentNumber + ", " + relationship, street, "", unit, city, state, zip);
            webDriver.Log($"Address for student with ASN: {stateStudentNumber} and contact relationship : {relationship} Saved");
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Contact Address for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}",false);
        throw;
    }
}


void ProcessStudentContactEmail(string stateStudentNumber, string relationship, string email, bool isPrimary, string emailType)
{
    try
    {
        if (webDriver.GoToContactDetailsEmailPage(stateStudentNumber, relationship, emailType))
        {
            webDriver.EnterContactEmail(stateStudentNumber + ", " + relationship, email, isPrimary, emailType);
            webDriver.Log($"Email for student with ASN: {stateStudentNumber} and contact relationship : {relationship} Saved");
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Contact Email for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}", false);
        throw;
    }
}

void ProcessStudentContactPhone(string stateStudentNumber, string relationship, string phone, bool acceptsTexts, bool isPreferred, string phoneType)
{
    string pType = phoneType;
    try
    {
        if (string.IsNullOrWhiteSpace(phoneType))
        {
            pType = "Mobile";
            webDriver.Log($"Student {stateStudentNumber} {relationship}, has no phonetype for {phone}, defaulting to {pType}");
        }

        if (webDriver.GoToContactDetailsPhonePage(stateStudentNumber, relationship, phone, phoneType))
        {
            if (webDriver.EnterContactPhone(stateStudentNumber + ", " + relationship, phone, acceptsTexts, isPreferred, phoneType))
                webDriver.Log($"Phone for student with ASN: {stateStudentNumber} and contact relationship : {relationship}, Type: {pType} Saved");
            else // retry
            {
                if (webDriver.GoToContactDetailsPhonePage(stateStudentNumber, relationship, phone, phoneType))
                {
                    if (!webDriver.EnterContactPhone(stateStudentNumber + ", " + relationship, phone, acceptsTexts, isPreferred, phoneType))
                        throw new Exception($"After retry still not able to enter student {stateStudentNumber} {relationship}, phone for {pType}");
                }
                else
                {
                    webDriver.Log($"After retry still not able to enter student {stateStudentNumber} {relationship}, phone for {pType}");
                }
            }
        }
    }
    catch(ElementClickInterceptedException e)
    {
        webDriver.Log($"Retrying after {e.Message}");
        if (webDriver.GoToContactDetailsPhonePage(stateStudentNumber, relationship, phone, phoneType))
        {
            webDriver.EnterContactPhone(stateStudentNumber + ", " + relationship, phone, acceptsTexts, isPreferred, phoneType);
            webDriver.Log($"Phone for student with ASN: {stateStudentNumber} and contact relationship : {relationship}, Type: {pType} Saved");
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Contact Phone for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}", false);
        throw;
    }
}

// Reconstitute CSV row from StudentContact
string ReconstituteCsvRow(StudentContact contact)
{
    // Match CSV column order (assumed from property comments)
    var fields = new[]
    {
        Quote(contact.Campus),
        Quote(contact.Status),
        FormatDateTime(contact.DateModified),
        FormatDateTime(contact.DateCompleted),
        Quote(contact.StudentId),
        Quote(contact.LastName),
        Quote(contact.FirstName),
        Quote(contact.Grade),
        Quote(contact.HR),
        Quote(contact.Q1StuFN),
        Quote(contact.Q2StuMN),
        Quote(contact.Q3StuLN),
        Quote(contact.Q4StuCurrAdd),
        Quote(contact.Q5NoUpdate),
        Quote(contact.Q6UpdateAdd),
        Quote(contact.Q7NewStuUnit),
        Quote(contact.Q8NewStuAdd),
        Quote(contact.Q9City),
        Quote(contact.Q10Prov),
        Quote(contact.Q11PC),
        FormatDate(contact.Q12NewAddrEffDt),
        Quote(contact.Q13Relationship),
        Quote(contact.Q14PG1FN),
        Quote(contact.Q15PG1MidNm),
        Quote(contact.Q16PG1LN),
        Quote(contact.Q17Custody),
        Quote(contact.Q18LegDocName),
        Quote(contact.Q19NoLegDoc),
        Quote(contact.Q20LivesWith),
        Quote(contact.Q21PG1Add),
        Quote(contact.Q22PG1NoUpdate),
        Quote(contact.Q23PG1UpdateAddr),
        Quote(contact.Q24NewPG1Unit),
        Quote(contact.Q25NewPG1Add),
        Quote(contact.Q26PG1City),
        Quote(contact.Q27PG1Prov),
        Quote(contact.Q28PG1PC),
        Quote(contact.Q29PG1Email),
        Quote(contact.Q30PG1AccAllEm),
        Quote(contact.Q31PG1AccLtdEm),
        Quote(contact.Q32Primary),
        Quote(contact.Q33PG1Ph1),
        Quote(contact.Q34Cell),
        Quote(contact.Q35Work),
        Quote(contact.Q36Home),
        Quote(contact.Q37PG1Txt1),
        Quote(contact.Q38PG1Ph2),
        Quote(contact.Q39Cell2),
        Quote(contact.Q40Work2),
        Quote(contact.Q41Home2),
        Quote(contact.Q42PG1Txt2),
        Quote(contact.Q43PG1Ph3),
        Quote(contact.Q44Cell3),
        Quote(contact.Q45Work3),
        Quote(contact.Q46Home3),
        Quote(contact.Q47PG1Txt3),
        Quote(contact.Q48RelationshipToStudent),
        Quote(contact.Q49PG2FN),
        Quote(contact.Q50PG2MN),
        Quote(contact.Q51PG2LN),
        Quote(contact.Q52Custody2),
        Quote(contact.Q53LegalDocName),
        Quote(contact.Q54NoLegalDoc),
        Quote(contact.Q55PG2LiveWith),
        Quote(contact.Q56SameAddress),
        Quote(contact.Q57DifferentAddress),
        Quote(contact.Q58PG2Apt),
        Quote(contact.Q59PG2Add),
        Quote(contact.Q60PG2City),
        Quote(contact.Q61PG2Prov),
        Quote(contact.Q62PG2PC),
        Quote(contact.Q63PG2Email),
        Quote(contact.Q64AcceptAllEmails),
        Quote(contact.Q65PG2AccLtdEm),
        Quote(contact.Q66Primary2),
        Quote(contact.Q67PG2Ph1),
        Quote(contact.Q68CellPG2),
        Quote(contact.Q69WorkPG2),
        Quote(contact.Q70HomePG2),
        Quote(contact.Q71PG2Ph1Txt),
        Quote(contact.Q72PG2Ph2),
        Quote(contact.Q73Cell2PG2),
        Quote(contact.Q74Work2PG2),
        Quote(contact.Q75Home2PG2),
        Quote(contact.Q76PG2Ph2Txt),
        Quote(contact.Q77PG2Ph3),
        Quote(contact.Q78Cell3PG2),
        Quote(contact.Q79Work3PG2),
        Quote(contact.Q80Home3PG2),
        Quote(contact.Q81PG2Ph3Txt),
        Quote(contact.Q82Confirm)
    };
    return string.Join(",", fields);
}

// Helper methods
string Quote(string value) => $"\"{value?.Replace("\"", "\"\"") ?? ""}\"";
string FormatDateTime(DateTime? date) => date.HasValue ? $"\"{date.Value.ToString("M/dd/yyyy HH:mm", CultureInfo.InvariantCulture)}\"" : "\"\"";
string FormatDate(DateTime? date) => date.HasValue ? $"\"{date.Value.ToString("dd-MMM-yy", CultureInfo.InvariantCulture)}\"" : "\"\"";


string DeterminePhoneType(string mobile, string work, string home)
{
    string phoneType = "";
    if (mobile.Trim().ToUpper() == "TRUE") { phoneType = "Mobile"; }
    else if (work.Trim().ToUpper() == "TRUE") { phoneType = "Work"; }
    else if (home.Trim().ToUpper() == "TRUE") { phoneType = "Home"; }
    return phoneType;
}
List<StudentContact> LoadStudentContactsFromFile(string filePath, string lastProcessedId)
{
    try
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // Ignore header validation (e.g., if some headers don’t match exactly)
            HeaderValidated = null,
            // Ignore missing fields in the CSV (won’t throw if a column is missing)
            MissingFieldFound = null

        }))
        {
            csv.Read();
            csv.ReadHeader();
            csvHeader = csv.Context.Reader.HeaderRecord is null ? "" : string.Join(",", csv.Context.Reader.HeaderRecord.Select(Quote)); // Capture header

            // Register the class map to map CSV headers to properties
            csv.Context.RegisterClassMap<StudentContactMap>();
            // Load all records into a list
            var records = csv.GetRecords<StudentContact>().GetEnumerator();
            if (!string.IsNullOrWhiteSpace(lastProcessedId))
            {
                // Skip to last processed record
                while (records.MoveNext())
                {
                    var student = records.Current;
                    if (student.StudentId == lastProcessedId) // e.g., "306050683"
                    {
                        break; // Found the last processed record
                    }
                }
            };

            var remainingList = new List<StudentContact>();

            while (records.MoveNext())
            {
                remainingList.Add(records.Current);
            }
            return remainingList;
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error loading CSV: {ex.Message}", false);
        throw;
    }
}


void WriteToReprocessFile(string rawRecord, string reprocessFilePath)
{
    try
    {
        // Check if file doesn't exist or is empty
        bool isFileEmpty = !File.Exists(reprocessFilePath) || new FileInfo(reprocessFilePath).Length == 0;
        if (isFileEmpty && !string.IsNullOrEmpty(csvHeader))
        {
            // Write header and first row
            File.WriteAllText(reprocessFilePath, csvHeader + Environment.NewLine + rawRecord + Environment.NewLine);
        }
        else
        {
            // Append the raw record to the reprocess file with a newline
            File.AppendAllText(reprocessFilePath, rawRecord + Environment.NewLine);
        }
    }
    catch (Exception ex)
    {
        webDriver.Log($"Failed to write to reprocess file: {ex.Message}");
    }
}
void InitializeDriver()
{
    if (webDriver.driver == null)
    {
        var options = new ChromeOptions();
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        webDriver.driver = new ChromeDriver(options);
        //wait = new WebDriverWait(webDriver.driver, TimeSpan.FromSeconds(20));
        webDriver.driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
        webDriver.driver.Navigate().GoToUrl("your_powerschool_url");
        webDriver.Log("WebDriver initialized.");
    }
}

void CleanupDriver()
{
    if (webDriver.driver != null)
    {
        try { webDriver.driver.Quit(); } 
        catch { }
        webDriver.driver = null;
        //var wait = null;
        webDriver.Log("WebDriver cleaned up.");
    }
}