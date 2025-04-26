// See https://aka.ms/new-console-template for more information

using CsvHelper;
using CsvHelper.Configuration;
using OpenQA.Selenium;
using StudentContact_PS_Sync;
using System.Globalization;

string[] argsArray = args; // For clarity in top-level statements

// Default values
bool isMockRun = true;
bool useReprocess = false;
bool showDebugStatements = false;
string basePath = Directory.GetCurrentDirectory(); // Default to current directory
string studentContactFileName = "StudentContact.csv"; // Default CSV file

bool forceUserAccountCheck = true;

// Track which parameters are defaulted
var defaultsUsed = new Dictionary<string, bool>
{
    { "mockRun", true },
    { "useReprocess", true },
    { "showDebugStatements", true },
    { "basePath", true },
    { "studentContactFileName", true }
};

// Parse command-line arguments (e.g., --param=value or --flag)
var argDict = ParseArguments(argsArray);
foreach (var arg in argDict)
{
    switch (arg.Key.ToLower())
    {
        case "mockrun":
            isMockRun = bool.Parse(arg.Value ?? "true");
            defaultsUsed["mockRun"] = false;
            break;
        case "usereprocess":
            useReprocess = bool.Parse(arg.Value ?? "false");
            defaultsUsed["useReprocess"] = false;
            break;
        case "showdebugstatements":
            showDebugStatements = bool.Parse(arg.Value ?? "false");
            defaultsUsed["showDebugStatements"] = false;
            break;
        case "basepath":
            basePath = arg.Value ?? throw new ArgumentException("basePath requires a value.");
            if (!Directory.Exists(basePath))
                throw new DirectoryNotFoundException($"Base path does not exist: {basePath}");
            defaultsUsed["basePath"] = false;
            break;
        case "studentcontactfilename":
            studentContactFileName = arg.Value ?? throw new ArgumentException("studentContactFileName requires a value.");
            if (!studentContactFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Student contact file must have a .csv extension.");
            defaultsUsed["studentContactFileName"] = false;
            break;
        default:
            Console.WriteLine($"Warning: Ignoring unknown parameter '{arg.Key}'");
            break;
    }
}

// Log parameters and indicate defaults (before webDriver is instantiated)
Console.WriteLine($" Your run parameters: you provided {argDict.Count} parameters");
Console.WriteLine($"  mockRun: {isMockRun} {(defaultsUsed["mockRun"] ? "(default)" : "")}");
Console.WriteLine($"  useReprocess: {useReprocess} {(defaultsUsed["useReprocess"] ? "(default)" : "")}");
Console.WriteLine($"  showDebugStatements: {showDebugStatements} {(defaultsUsed["showDebugStatements"] ? "(default)" : "")}");
Console.WriteLine($"  basePath: {basePath} {(defaultsUsed["basePath"] ? "(default)" : "")}");
Console.WriteLine($"  studentContactFileName: {studentContactFileName} {(defaultsUsed["studentContactFileName"] ? "(default)" : "")}");

// Construct file paths
string studentContactFilePath = Path.Combine(basePath, studentContactFileName);
string reprocessFilePath = Path.Combine(basePath, "reprocess.csv");
string checkpointFilePath = Path.Combine(basePath, "checkpoint.txt");

// Validate student contact file existence
if (!useReprocess && !File.Exists(studentContactFilePath))
    throw new FileNotFoundException($"Student contact file not found at: {studentContactFilePath}");

if (!File.Exists(checkpointFilePath))
{
    File.WriteAllText(checkpointFilePath, "");
    Console.WriteLine($" Checkpoint File: Empty");
}
else
{
    var checkPointId = File.ReadAllText(checkpointFilePath).Trim();
    if (string.IsNullOrWhiteSpace(checkPointId))
    {
        Console.WriteLine($" Checkpoint File: Empty");
    }
    else
    {
        Console.WriteLine($" Checkpoint File exists. Run will start with last successful student: {checkPointId}");
    }

}
Console.WriteLine("If you agree with these parameters, enter 'Y' and press Enter");
var response = Console.ReadLine()?.Trim().ToUpper();
if (response != "Y")
{
    Console.WriteLine("Program terminated.");
    Environment.Exit(0);
}

// Continue with the rest of the program
Console.WriteLine("Proceeding with the program...");

string csvHeader = ""; // For store the csv header record
var webDriver = new PsWebDriver(basePath, isMockRun, showDebugStatements);
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
        webDriver.Log($"# Using renamed reprocess file: {renamedReprocessFilePath}");
    }
    else
    {
        Console.WriteLine("### Reprocess.csv not found; All records should have been processed successfully then. Ending process.");
        return;
    }

    if (File.Exists(checkpointFilePath))
    {
        // Clear checkpoint.txt (overwrite with "") when using reprocess
        File.WriteAllText(checkpointFilePath, "");
        webDriver.Log("# Cleared checkpoint.txt for reprocess run.");
    }
}
else
{
    // Check if the studentContact file exists
    if (!File.Exists(filePathToProcess))
    {
        webDriver.Log($"### The {filePathToProcess} file does not exist");
        return;
    }
}
// Read last checkpoint (StateStudentNumber)
string lastProcessedId = "";
if (File.Exists(filePathToProcess))
{
    lastProcessedId = File.ReadAllText(checkpointFilePath).Trim();
    if (lastProcessedId != "")
        webDriver.Log($"#++ Run resuming from last successful student: {lastProcessedId}");
}
else
{
    webDriver.Log($"#++ Run starting with first student");
}

int numberOfRecords = 0;
int numberOfRecordsUpdated = 0;

// dispay run parameters
webDriver.Log($"# Run Parameters:");
webDriver.Log($"# Mock run is {(isMockRun ? "On" : "Off")}");
webDriver.Log($"# Base file path: {basePath}");
webDriver.Log($"# Running with Reprocessing file? {(useReprocess ? "Yes" : "No")}");
webDriver.Log($"# Source file being processed: {filePathToProcess}");
webDriver.Log($"# Checkpoint file: {checkpointFilePath}");
webDriver.Log($"# Reprocess file: {reprocessFilePath}");


webDriver.LoginToPS();
ProcessStudentContactFile();
webDriver.Quit();


void ProcessStudentContactFile()
{
    try
    {
        webDriver.InitializeDriver();
        // Read all lines from the CSV file
        List<StudentContact> contacts = LoadStudentContactsFromFile(filePathToProcess, lastProcessedId);
        numberOfRecords = contacts.Count();
        if (lastProcessedId != "")
            webDriver.Log($"#++ Resuming from StateStudentNumber: \"{lastProcessedId}\". Records remaining: {numberOfRecords}");
        else
            webDriver.Log($"#++ Starting from first StateStudentNumber: \"{contacts[0].StudentId}\". Records: {numberOfRecords}");

        int index = 0;
        // Process each line
        while (index < contacts.Count)
        {
            StudentContact contact = contacts[index];
            bool atStudentProfilePage = false;
            try
            {
                string stateStudentNumber = contact.StudentId;
                string campus = contact.Campus;
                if (StringUtil.RecordhasChanges(contact))
                {
                    webDriver.Log($"#++ Starting update process for: \"{contact.StudentId}\", \"{contact.FirstName} {contact.LastName}\". Record {index+1} of {numberOfRecords}"  );
                    if (webDriver.FindStudentOnStudentSearchPage(campus, stateStudentNumber))
                    {
                        if (webDriver.GoToStudentPage())
                        {
                            // Student address update
                            string currentAddress = contact.Q4StuCurrAdd;
                            if (StringUtil.RecordhasChanges(contact, "Student"))
                            {
                                webDriver.Log($"#+# started processing student only portion of demographic for: {stateStudentNumber}");
                                webDriver.GoToStudentCompliantPage();
                                ProcessStudentAddress(contact, webDriver);
                                // Brief pause to allow the dialog to appear (e.g., 5 seconds timeout)
                                // Check for the sibling dialog and click Submit if present
                                if (!isMockRun)
                                    webDriver.HandleSiblingAddressDialog(campus, stateStudentNumber);

                                webDriver.Log($"#-# completed processing student only portion of demographic for: {stateStudentNumber} \r\n");
                            }

                            // Student First Contact
                            if (StringUtil.RecordhasChanges(contact, "Contact1"))
                            {
                                string relationship = contact.Q13Relationship;
                                webDriver.Log($"#+# started processing 1st parent for: {stateStudentNumber}, {relationship}");
                                webDriver.GoToStudenProfilePage();
                                atStudentProfilePage = true;
                                if (ProcessContact(campus, stateStudentNumber, relationship, contact.Q14PG1FN, contact.Q16PG1LN))
                                {
                                    // address
                                    var address = contact.Q25NewPG1Add;
                                    if (contact.Q22PG1NoUpdate == "FALSE" && !string.IsNullOrWhiteSpace(address))
                                        ProcessContactAddress(campus, stateStudentNumber, relationship, contact.Q21PG1Add, contact.Q24NewPG1Unit, address, contact.Q26PG1City, contact.Q27PG1Prov, contact.Q28PG1PC);
                                    // email
                                    var email = EmailValidator.ValidateAndFormatEmail(contact.Q29PG1Email, webDriver.Log, out bool isValid);
                                    if (!string.IsNullOrWhiteSpace(email))
                                    {
                                        var isPrimary = contact.Q32Primary.Trim().ToUpper() == "TRUE";
                                        ProcessStudentContactEmail(stateStudentNumber, relationship, email, true, "Current");
                                    }
                                    // first phone
                                    var phone = contact.Q33PG1Ph1;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q34Cell, contact.Q35Work, contact.Q36Home);
                                        bool acceptsText = (contact.Q37PG1Txt1.Trim().ToUpper() == "TRUE");
                                        var isPreferred = true;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                    // second phone
                                    phone = contact.Q38PG1Ph2;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q39Cell2, contact.Q40Work2, contact.Q41Home2);
                                        bool acceptsText = (contact.Q42PG1Txt2.Trim().ToUpper() == "TRUE");
                                        bool isPreferred = false;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                    // third phone
                                    phone = contact.Q43PG1Ph3;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q44Cell3, contact.Q45Work3, contact.Q46Home3);
                                        bool acceptsText = (contact.Q47PG1Txt3.Trim().ToUpper() == "TRUE");
                                        bool isPreferred = false;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                }

                                webDriver.Log($"#-# completed processing 1st parent for: {stateStudentNumber}, {relationship} \r\n");
                                //need to close window
                                webDriver.CloseContactDetailsTab();
                            }

                            // Student Second Contact
                            //TODO - need to ask Phoebe if there is something like Q22 - PG1no Update for PG2 
                            // answer: if Q56SameAddress is true, skip the address, if Q57
                            if (StringUtil.RecordhasChanges(contact, "Contact2"))
                            {
                                string relationship = contact.Q48RelationshipToStudent;
                                webDriver.Log($"#+# started processing 2nd parent for: {stateStudentNumber}, {relationship}");

                                if (!atStudentProfilePage)
                                {
                                    webDriver.GoToStudenProfilePage();
                                    atStudentProfilePage = true;
                                }
                                if (ProcessContact(campus, stateStudentNumber, relationship, contact.Q49PG2FN, contact.Q51PG2LN))
                                {
                                    // address
                                    var address = contact.Q59PG2Add;
                                    if (contact.Q56SameAddress == "FALSE" && !string.IsNullOrWhiteSpace(address))
                                        ProcessContactAddress(campus, stateStudentNumber, relationship, address, contact.Q58PG2Apt, address, contact.Q60PG2City, contact.Q61PG2Prov, contact.Q62PG2PC);

                                    // email
                                    var email = EmailValidator.ValidateAndFormatEmail(contact.Q63PG2Email, webDriver.Log, out bool isValid);
                                    if (!string.IsNullOrWhiteSpace(email))
                                    {
                                        // hard code to primary as only 1 email allowed. 
                                        ProcessStudentContactEmail(stateStudentNumber, relationship, email, true, "Current");
                                    }
                                    // first phone
                                    string phone = contact.Q67PG2Ph1;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q68CellPG2, contact.Q69WorkPG2, contact.Q70HomePG2);
                                        bool acceptsText = (contact.Q71PG2Ph1Txt.Trim().ToUpper() == "TRUE");
                                        var isPreferred = true;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                    // second phone
                                    phone = contact.Q72PG2Ph2;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q73Cell2PG2, contact.Q74Work2PG2, contact.Q75Home2PG2);
                                        bool acceptsText = (contact.Q76PG2Ph2Txt.Trim().ToUpper() == "TRUE");
                                        bool isPreferred = false;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                    // third phone
                                    phone = contact.Q77PG2Ph3;
                                    if (!StringUtil.isPhoneNull(phone))
                                    {
                                        string phoneType = DeterminePhoneType(contact.Q78Cell3PG2, contact.Q79Work3PG2, contact.Q80Home3PG2);
                                        bool acceptsText = (contact.Q81PG2Ph3Txt.Trim().ToUpper() == "TRUE");
                                        bool isPreferred = false;
                                        ProcessStudentContactPhone(campus, stateStudentNumber, relationship, phone, acceptsText, isPreferred, phoneType);
                                    }
                                    webDriver.Log($"#-# completed processing 2nd parent for: {stateStudentNumber}, {relationship} \r\n");
                                    //need to close window
                                    webDriver.CloseContactDetailsTab();
                                }
                                else
                                {
                                    webDriver.Log($"#-# completed processing 2nd parent for: {stateStudentNumber}, {relationship} \r\n");
                                }
                            }

                            File.WriteAllText(checkpointFilePath, stateStudentNumber);
                            numberOfRecordsUpdated++;
                            webDriver.Log($"#-- Checkpoint updated to: {stateStudentNumber}. {numberOfRecordsUpdated} record updated, {(index + 1)} records processed, out of {numberOfRecords}\r\n");
                        }
                        else
                        {
                            throw new Exception($"### After finding student, Unable to load student page for ASN: {stateStudentNumber}");
                        }
                    }
                    else
                    {
                        webDriver.Log($"## Campus: {campus}, Unable to find student in PowerSchool with ASN: {stateStudentNumber}");
                    }
                    atStudentProfilePage = false;
                    webDriver.GoToStudentSearchPage();
                }// else no changes to pickup
            }
            catch (WebDriverException ex) when (ex.Message.Contains("invalid session id"))
            {
                webDriver.Log($"### Session lost: {ex.Message}. Restarting WebDriver...");
                webDriver.Log(ex);
                AddReprocessRecord(contacts, contact, reprocessFilePath, msg => webDriver.Log(msg));
                webDriver.CleanupDriver();
                webDriver.InitializeDriver();
                webDriver.LoginToPS();
            }
            catch (Exception ex)
            {
                webDriver.Log($"### An error occurred: {ex.Message}, {ex.GetType().FullName}");
                webDriver.Log(ex);
                AddReprocessRecord(contacts, contact, reprocessFilePath, msg => webDriver.Log(msg));
                webDriver.CloseContactDetailsTab();
                webDriver.GoToStudentSearchPage();
                Thread.Sleep(2000);
            }
            index++;
        }
        webDriver.Log($"#-- Run ended. Records processed: {index--}");
    }
    catch (Exception ex)
    {
        webDriver.Log(ex);
    }

}

void ProcessStudentAddress(StudentContact contact, PsWebDriver webDriver)
{
    //webDriver.Log($"StudentId: {contact.StudentId}, Student: {contact.LastName}, {contact.FirstName}, CurrentAddr: {contact.Q4StuCurrAdd}, NewStreet: {contact.Q8NewStuAdd}, NewCity: {contact.Q9City}, NewProv: {contact.Q10Prov}, NewZip: {contact.Q11PC}, NewEffDt: {contact.Q12NewAddrEffDt}");
    string campus = contact.Campus;
    string currentAddress = contact.Q4StuCurrAdd;
    string stateStudentNumber = contact.StudentId;
    string street = contact.Q8NewStuAdd;
    string city = contact.Q9City;
    string state = contact.Q10Prov;
    string postalcode = contact.Q11PC;
    DateTime? effectiveDate = contact.Q12NewAddrEffDt;
    //webDriver.Log($"StudentId: {stateStudentNumber}, Student: {contact.LastName}, {contact.FirstName}, CurrentAddr: {currentAddress}, NewStreet: {street}, NewCity: {city}, NewProv: {state}, NewZip: {postalcode}, NewEffDt: {effectiveDate}");
    try
    {
        webDriver.Log($"# Looking for Student Address for {stateStudentNumber}, street: {street}");
        if (string.IsNullOrWhiteSpace(street))
        {
            webDriver.Log($"## Student Address for {stateStudentNumber} is missing the street");
            return;
        }
        if (webDriver.GoToStudentCompliantAddressPage(campus, stateStudentNumber, currentAddress, street, city, state, postalcode, effectiveDate))
        {
             if (webDriver.EnterEditAddress(street, city, state, postalcode, effectiveDate))
            {
                webDriver.Log($"# Student Address for {stateStudentNumber} Saved");
                return;
            }
        }
        //throw new Exception($"Error Processing Student Address for {stateStudentNumber}, attempting retry.");
    }
    catch (Exception ex)
    {
        webDriver.Log($"# Retrying after {ex.Message}");
        if (webDriver.GoToStudentCompliantAddressPage(campus, stateStudentNumber, currentAddress, street, city, state, postalcode, effectiveDate))
        {
            if (webDriver.EnterEditAddress(street, city, state, postalcode, effectiveDate))
            {
                webDriver.Log($"# Student Address for {stateStudentNumber} Saved");
                return;
            }
        }
        webDriver.Log($"### Error Processing retry on Student Address for ASN: {stateStudentNumber}:\r\n  {ex.Message}, {ex.GetType().FullName}");
        throw;
    }
}

bool ProcessContact(string campus, string stateStudentNumber, string relationship, string contactFirstName, string contactLastName)
{
    try
    {
        return webDriver.GoToContactDetailsPage(campus, stateStudentNumber, relationship, contactFirstName, contactLastName);
    }
    catch (Exception ex)
    {
        webDriver.Log($"Error Processing Student Contact Details page for {stateStudentNumber}, {relationship}:\r\n  {ex.Message}, {ex.GetType().FullName}");
        return false;
    }
}

void ProcessContactAddress(string campus, string stateStudentNumber, string relationship, string currentAddress, string unit, string street, string city, string state, string zip)
{
    try
    {
        webDriver.Log($"# Looking for Student Contact Address for {stateStudentNumber}, {relationship}, street: {street}");
        if (webDriver.GoToContactDetailsAddressPage(campus, stateStudentNumber, relationship, currentAddress, unit, street, city, state, zip))
        {
            if (webDriver.EnterContactAddress(stateStudentNumber + ", " + relationship, street, "", unit, city, state, zip))
            {
                webDriver.Log($"# Student Contact Address for {stateStudentNumber}, {relationship} Saved");
                return;
            }
        }
    }
    catch (Exception ex)
    {
        try
        {
            webDriver.Log($"# Retrying after {ex.Message}");
            if (webDriver.GoToContactDetailsAddressPage(campus, stateStudentNumber, relationship, currentAddress, unit, street, city, state, zip))
            {
                if (webDriver.EnterContactAddress(stateStudentNumber + ", " + relationship, street, "", unit, city, state, zip))
                {
                    webDriver.Log($"# Student Contact Address for {stateStudentNumber}, {relationship} Saved");
                    return;
                }
            }
        }
        catch (Exception)
        {
            webDriver.Log($"### Error Processing retry on Student Contact Address for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}");
            throw;
        }
    }
}


void ProcessStudentContactEmail(string stateStudentNumber, string relationship, string email, bool isPrimary, string emailType)
{
    var emailChanged = false;
    try
    {
        webDriver.Log($"# Looking for Student Contact Email for {stateStudentNumber}, {relationship}, email: {email}");
        if (webDriver.GoToContactDetailsEmailPage(stateStudentNumber, relationship, emailType))
        {
            if (webDriver.EnterContactEmail(stateStudentNumber + ", " + relationship, email, isPrimary, emailType, ref emailChanged))
            {
                webDriver.Log($"# Student Contact Email for {stateStudentNumber}, {relationship} Saved");
                if (forceUserAccountCheck || emailChanged)
                {
                    webDriver.UpdateUserNameIfNeeded(stateStudentNumber + ", " + relationship, email);
                }
                return;
            }
        }
    }
    catch (Exception ex)
    {
        try
        {
            webDriver.Log($"# Retrying after {ex.Message}");
            if (webDriver.GoToContactDetailsEmailPage(stateStudentNumber, relationship, emailType))
            {
                if (webDriver.EnterContactEmail(stateStudentNumber + ", " + relationship, email, isPrimary, emailType, ref emailChanged))
                {
                    webDriver.Log($"# Student Contact Email for {stateStudentNumber}, {relationship} Saved");
                    if (forceUserAccountCheck || emailChanged)
                    {
                        webDriver.UpdateUserNameIfNeeded(stateStudentNumber + ", " + relationship, email);
                    }
                    return;
                }
            }
        }
        catch (Exception )
        {
            webDriver.Log($"### Error Processing retry on Student Contact Email for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}");
            throw;
        }
    }
}

void ProcessStudentContactPhone(string campus, string stateStudentNumber, string relationship, string phone, bool acceptsTexts, bool isPreferred, string phoneType)
{
    string pType = phoneType;
    try
    {
        if (string.IsNullOrWhiteSpace(phoneType))
        {
            pType = "Mobile";
            webDriver.Log($"## Campus: {campus}, Student {stateStudentNumber} {relationship}, has no phonetype for {phone}, defaulting to {pType}");
        }

        webDriver.Log($"# Looking for Student Contact Phone for {stateStudentNumber}, {relationship}, Type: {pType}, Phone: {phone}");
        if (webDriver.GoToContactDetailsPhonePage(campus, stateStudentNumber, relationship, phone, phoneType))
        {
            if (webDriver.EnterContactPhone(stateStudentNumber + ", " + relationship, phone, acceptsTexts, isPreferred, phoneType))
            {
                webDriver.Log($"# Student Contact Phone for {stateStudentNumber}, {relationship}, Type: {pType} Saved");
                return;
            }
        }
    }
    catch (Exception ex) //(ElementClickInterceptedException e)
    {
        try
        {
            webDriver.Log($"# Retrying after {ex.Message}");
            if (webDriver.GoToContactDetailsPhonePage(campus, stateStudentNumber, relationship, phone, phoneType))
            {
                if (webDriver.EnterContactPhone(stateStudentNumber + ", " + relationship, phone, acceptsTexts, isPreferred, phoneType))
                {
                    webDriver.Log($"# Student Contact Phone for {stateStudentNumber}, {relationship}, Type: {pType} Saved");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            webDriver.Log($"### Error Processing retry on Student Contact Phone for {stateStudentNumber}, {relationship}:\r\n {ex.Message}, {ex.GetType().FullName}");
            webDriver.Log(e.StackTrace);
            throw;
        }
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
{ 
}
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
        webDriver.Log($"### Error loading CSV: {ex.Message}");
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

void AddReprocessRecord(IList<StudentContact> contacts, StudentContact contact, string reprocessFilePath, Action<string> log)
{
    // reprocess
    if (contact.Reprocessed)
    {
        string csvRow = ReconstituteCsvRow(contact);
        WriteToReprocessFile(csvRow, reprocessFilePath);
        log($"# Failed record has been passed to the Reprocessing file for: {contact.StudentId}");
    }
    else
    {
        contact.Reprocessed = true;
        contacts.Add(contact);
        log($"# Failed record has been readded to the list for current processing for: {contact.StudentId}");
    }
}

// Argument parsing helper
Dictionary<string, string> ParseArguments(string[] args)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i].TrimStart('-'); // Remove leading -- or -
        if (arg.Contains('='))
        {
            var parts = arg.Split('=', 2);
            dict[parts[0]] = parts[1];
        }
        else
        {
            dict[arg] = null; // Flag without value
        }
    }
    return dict;
}
