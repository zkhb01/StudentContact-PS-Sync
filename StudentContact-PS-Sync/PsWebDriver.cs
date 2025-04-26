using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.IO;
using System.Text;

namespace StudentContact_PS_Sync
{
    public class PsWebDriver
    {
        public string logFilePath;
        public bool isMockRun;
        public bool showDebugStatements;
        public IWebDriver driver;
        private string originalWindow { get; set; }
        private int waitSeconds = 2;
        private string adminUser = "srv_psro@ffca-calgary.com";
        private string adminPassword = "Pj&^MTAE(bR#$B=7:/?6CU\"Hk";
        private string adminWriteUser = "srv_identity5309@ffca-calgary.com";
        private string adminWritePassword = "e?Cn%YPQcE=Ek7qv!qVzm%hCLz";
        private string psUrl = "https://ffca.powerschool.com/admin/";
        private string psHomeUrl = "home.html?searchtype=students";
        private bool goReadOnly = false;
        private bool first = true;

        public PsWebDriver(string basePath,  bool isMockRun, bool showDebugStatements) {
            driver = new ChromeDriver();
            originalWindow = "";
            this.logFilePath = basePath + $"log_{DateTime.Now.ToString("yyyy-MM-dd_HHmmss")}.txt";
            this.isMockRun = isMockRun;
            this.showDebugStatements = showDebugStatements;
        }


        public string Title {  get { return driver.Title;  } }


        public void LoginToPS()
        {
            string ps_user = adminWriteUser;
            string ps_pw = adminWritePassword;
            if (goReadOnly)
            {
                ps_user = adminUser;
                ps_pw = adminPassword;
            }

            try
            {
                driver.Navigate().GoToUrl(psUrl);

                // Wait for the page to load (adjust timeout as needed)
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitSeconds));

                // Wait for the login page to stabilize (e.g., redirect to Microsoft login)
                wait.Until(drv => drv.Url.Contains("login.microsoftonline.com") || drv.FindElements(By.Name("loginfmt")).Count > 0);

                // Check for iframes and switch to the correct one (if necessary)
                try
                {
                    wait.Until(drv => drv.FindElements(By.TagName("iframe")).Count > 0);
                    var iframes = driver.FindElements(By.TagName("iframe"));
                    foreach (var iframe in iframes)
                    {
                        Log($"Switching to iframe: {iframe.GetAttribute("id")} or {iframe.GetAttribute("name")}");
                        driver.SwitchTo().Frame(iframe);

                        // Try to find the login input field in this iframe
                        try
                        {
                            IWebElement user = wait.Until(drv =>
                            {
                                var elem = drv.FindElement(By.Name("loginfmt"));
                                return elem.Displayed && elem.Enabled ? elem : null;
                            });
                            Log("Found login field in iframe.");
                            break; // Exit loop if found
                        }
                        catch (NoSuchElementException)
                        {
                            driver.SwitchTo().DefaultContent(); // Switch back if not found
                        }
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    Log("No iframes found or login field not in iframe.");
                }

                // Wait for the login input field to be visible and interactable
                IWebElement userField = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Name("loginfmt"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Ensure the element is in view and focused
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", userField);
                userField.Click(); // Click to focus
                userField.SendKeys(ps_user);

                // Wait for the submit button to be clickable
                IWebElement submitButton = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("idSIButton9"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                submitButton.Click();

                // Continue with password entry (to be added after fixing username entry)
                // Wait for the password page to load (check for URL change or element presence)
                wait.Until(drv => drv.Url.Contains("login.microsoftonline.com") || drv.FindElements(By.Name("passwd")).Count > 0);

                // Switch to iframe if necessary (inspect the page to confirm)
                try
                {
                    wait.Until(drv => drv.FindElements(By.TagName("iframe")).Count > 0);
                    var iframes = driver.FindElements(By.TagName("iframe"));
                    foreach (var iframe in iframes)
                    {
                        Log($"Switching to iframe: {iframe.GetAttribute("id")} or {iframe.GetAttribute("name")}");
                        driver.SwitchTo().Frame(iframe);

                        // Try to find the password input field in this iframe
                        try
                        {
                            IWebElement passwordField = wait.Until(drv =>
                            {
                                var elem = drv.FindElement(By.Name("passwd"));
                                return elem.Displayed && elem.Enabled ? elem : null;
                            });
                            Log("Found password field in iframe.");
                            break; // Exit loop if found
                        }
                        catch (NoSuchElementException)
                        {
                            driver.SwitchTo().DefaultContent(); // Switch back if not found
                        }
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    Log("No iframes found or password field not in iframe.");
                }

                // Wait for the password input field to be visible and interactable
                IWebElement passwordFieldFinal = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Name("passwd"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Enter password
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", passwordFieldFinal);
                passwordFieldFinal.Click();
                passwordFieldFinal.SendKeys(ps_pw);

                // Wait for the submit button to be clickable
                IWebElement passwordSubmitButton = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("idSIButton9"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                passwordSubmitButton.Click();

            }
            catch (Exception ex)
            {
                Log("### Error: " + ex.Message);
                Log("Current URL: " + driver.Url);
                Log("Page Source: " + driver.PageSource);
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                screenshot.SaveAsFile("error.png");
            }
        }

        public void GoToStudentSearchPage()
        {
            driver.Navigate().GoToUrl(psUrl + psHomeUrl);
            WebDriverWait wait8 = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
        }

        public bool FindStudentOnStudentSearchPage(string campus, string studentNumber)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                // Expand the serach by dropdown and select 'State Student Number' as the serach ccriteria

                // Wait for the dropdown button to be present and clickable
                IWebElement dropdownButton = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("dropDownButton__studentsSearchFields"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Locate the filter-buttons div (assuming it’s unique by class)
                var filterDiv = wait.Until(ExpectedConditions.ElementExists(By.ClassName("filter-buttons")));

                // Get the class attribute
                string classAttribute = filterDiv.GetAttribute("class");
                //Console.WriteLine($"Class attribute: '{classAttribute}'");

                // Check if showFilters is present
                bool isVisible = classAttribute.Contains("showFilters");
                //Console.WriteLine(isVisible ? "Buttons are visible (showFilters present)." : "Buttons are hidden (showFilters absent).");

                if (isVisible)
                {
                    IWebElement clearButton = wait.Until(drv =>
                    {
                        var elem = drv.FindElement(By.Id("clearAllFiltersstudents"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    // Clear prior search results
                    clearButton.Click();
                }



                // Click the dropdown button to expand the list
                dropdownButton.Click();

                // Wait for the specific item to be visible and clickable
                IWebElement targetItem = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("dropDownButton_studentsSearchFields_4"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                // Click the target item
                targetItem.Click();


                // provide the State Student number for the search criteria
                var searchButton = driver.FindElement(By.Id("add_search_filter_btn_students"));
                IWebElement? searchField = driver.FindElement(By.Id("studentSearchInput"));

                searchField.SendKeys(studentNumber);
                searchButton.Click();

                ///////////////////////////////////
                bool noResultsFound = false;
                try
                {
                    Thread.Sleep(500); // Small delay to ensure it's in view (use sparingly)

                    var headerElement = wait.Until(drv =>
                    {
                        var elements = drv.FindElements(By.XPath("//h2[contains(text(), 'Current Student Selection')]"));
                        return elements.Count > 0 && elements[0].Displayed ? elements[0] : null;
                    });
                    Log($"Results confirmed: '{headerElement.Text}'");
                    if (headerElement.Text == "Current Student Selection (0)")
                        noResultsFound = true;
                    else
                        noResultsFound = false;
                }
                catch (WebDriverTimeoutException)
                {
                    Log("### Neither element found - unexpected state.");
                    noResultsFound = true;
                }


                first = false;
                if (noResultsFound)
                {
                    Log($"Campus: {campus}, No Student exists with ASN: {studentNumber}");
                    return false;
                }
                return true;
            }
            catch (StaleElementReferenceException ex)
            {
                Log($"### Element stale: {ex.Message}");
//                Log("Page source at failure: " + driver.PageSource);
                return false;
            }
            catch (NoSuchElementException ex)
            {
                Log($"### Element not found: {ex.Message}");
                return false;
            }
            catch (WebDriverTimeoutException ex)
            {
                Log("### Timed out waiting for search outcome.");
                return false;
            }
            catch (Exception ex)
            {
                Log($"### An error occurred: {ex.Message}, {ex.GetType().FullName}");
                return false;
            }

        }

        public bool GoToStudentPage()
        {
            try
            {
                // Wait for document to be fully loaded
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(drv => ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").Equals("complete"));

                // Wait for the search results to load (you might need to identify a unique element that appears only after search results are loaded)
                wait.Until(drv => drv.FindElements(By.CssSelector("table.district.students_8")).Count > 0); // Adjust selector to match your table's ID or class

                // Wait for the specific student link to be both present and interactable
                IWebElement studentLink = wait.Until(drv =>
                {
                    //var elem = drv.FindElement(By.Id("student_selection_lnk4213"));
                    var elem = drv.FindElement(By.XPath("//*[starts-with(@id, 'student_selection_lnk')]"));
                    // Use JavaScript to check if the element is visible and not obscured
                    bool isVisible = (bool)((IJavaScriptExecutor)drv).ExecuteScript(
                        @"return arguments[0].offsetParent !== null && 
                    (function(el) {
                        var style = window.getComputedStyle(el);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    })(arguments[0]);", elem);
                    return isVisible ? elem : null;
                });

                // Attempt to scroll it into view and click it
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", studentLink);
                Thread.Sleep(500); // Small delay to ensure it's in view (use sparingly)
                studentLink.Click();
                return true;
            }
            catch (Exception ex)
            {
                Log($"### An error occurred: {ex.Message}, {ex.GetType().FullName}");
                return false;
            }
        }



        // SHould be at the page with the various student data including the address section
        public bool GoToStudentCompliantAddressPage(string campus, string stateStudentNumber, string currAddr, string street, string city, string state, string zip, DateTime? effectiveDate)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
                IWebElement expandBtn = null;
                try
                {
                    expandBtn = wait.Until(drv =>
                    {
                        var elements = drv.FindElements(By.XPath("//a[@ng-show='addressList.length > 1']"));
                        return elements.Count > 0 && elements[0].Displayed ? elements[0] : null;
                    });

                    if ((expandBtn.Displayed && expandBtn.Enabled ? expandBtn : null) == null)
                        expandBtn = null;
                    else
                    {
                        expandBtn.Click();
                        // in expanded table find row with current address
                        string prevCsvStreet = currAddr.Split(',')[0].Trim();
                        // Wait for the address table to appear
                        wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                        // Find all rows in the address table (skip header)
                        var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);
                        StringBuilder allStreetText = new StringBuilder();

                        var currStreetText = StringUtil.AbbreviateStreet(prevCsvStreet.Trim()).ToLower();
                        var updatedCsvStreet = StringUtil.AbbreviateStreet(street.Trim()).ToLower();
                        IWebElement? firstRow = null;
                        string psStreet = "";
                        foreach (var row in trs)
                        {
                            if (firstRow == null) firstRow = row;
                            // Find the Street <td> (3rd column based on header order)
                            var streetTd = row.FindElements(By.TagName("td"))[2]; // Index 2 for "Street"
                            psStreet = StringUtil.AbbreviateStreet(streetTd.Text.Trim()).ToLower();
                            if (psStreet == updatedCsvStreet)  // PS street = new csv street then update this row
                            {
                                Log($"# New street: '{updatedCsvStreet}' already matched a PS street: '{psStreet}'");
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {psStreet}");
                                return true;
                            }
                            if (psStreet == currStreetText)  // ps street = original street in csv then update this row
                            {
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {psStreet}");
                                return true;
                            }
                            allStreetText.Append($"\r\n\"{psStreet}\"");
                        }
                        if (trs.Count() == 2 && firstRow != null && psStreet != "")   // replace existing address to new csv address
                        {
                            // Find and click EditAddress in this row
                            IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                firstRow.FindElement(By.Id("EditAddress"))
                            ));
                            editAddressLink.Click();
                            Log($"Clicked EditAddress for street: {psStreet}");
                            return true;

                        }
                        Log($"## Campus: {campus}, Unmatched CurrentAddress: StateStudentNumber: {stateStudentNumber}, CurrentAddress: {currAddr}, NewAddress: {street}, {city}, {state}, {zip}, \r\nList of Streets compared to \"{currStreetText}\": {allStreetText.ToString()}");
                        return false;
                    }
                }
                catch (OpenQA.Selenium.NoSuchElementException)
                {
                    Log("no expand button");
                }
                catch (OpenQA.Selenium.WebDriverTimeoutException)
                {
                    Log("no expand button within time");
                }

                if (expandBtn == null)
                {

                    IWebElement addressBtn = wait.Until(drv =>
                    {
                        var elem = drv.FindElement(By.Id("EditAddress"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    if (addressBtn == null)
                    {
                        addressBtn = wait.Until(drv =>
                        {
                            var elem = drv.FindElement(By.Id("AddAddress"));
                            return elem.Displayed && elem.Enabled ? elem : null;
                        });
                    }
                    addressBtn.Click();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                return false;
            }
        }

        // From the Contact Details page, find the address that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsAddressPage(string campus, string stateStudentNumber, string relationship, string currAddr, string unit, string street, string city, string state, string zip)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                //var table = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("physical-address-table")));
                // Wait for the table to exist in the DOM
                wait.Until(ExpectedConditions.ElementExists(By.Id("physical-address-table")));
                //var table = wait.Until(ExpectedConditions.ElementExists(By.XPath("//form[@id='contactform']//table[@id='physi//cal-address-table']")));
                //var table = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("physical-address-table")));
                //Log("Table found in DOM: " + table.Displayed); // Check if it’s visible

                // Then wait for it to be visible
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='physical-address-table']")));


                // Wait for the address table to appear
                //wait.Until(d => d.FindElement(By.XPath("//form[@id='contactform']//table[@id='physical-address-table']")).Displayed);

                IWebElement expandBtn = null;
                try
                {
                    expandBtn = driver.FindElement(By.XPath("//a[@ng-show='addressList.length > 1']"));
                    if ((expandBtn.Displayed && expandBtn.Enabled ? expandBtn : null) == null)
                        expandBtn = null;
                    else
                    {
                        expandBtn.Click();
                        // in expanded table find row with current address
                        string prevCsvStreet = currAddr.Split(',')[0].Trim();
                        // Wait for the address table to appear
                        wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                          // Find all rows in the address table (skip header)
                        var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);
                        StringBuilder allStreetText = new StringBuilder();

                        var currStreetText = StringUtil.AbbreviateStreet(prevCsvStreet.Trim()).ToLower();
                        var updatedCsvStreet = StringUtil.AbbreviateStreet(street.Trim()).ToLower();

                        IWebElement? firstRow = null;
                        string psStreet = "";
                        foreach (var row in trs)
                        {
                            if (firstRow == null) firstRow = row;
                            // Find the Street <td> (3rd column based on header order)
                            var streetTd = row.FindElements(By.TagName("td"))[2]; // Index 2 for "Street"
                            string streetText = streetTd.Text.Trim();
                            psStreet = StringUtil.AbbreviateStreet(streetTd.Text.Trim()).ToLower();
                            if (psStreet == updatedCsvStreet)  // PS street = new csv street then update this row
                            {
                                Log($"# New street: '{updatedCsvStreet}' already matched a PS street: '{psStreet}'");
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {psStreet}");
                                return true;
                            }
                            if (psStreet == currStreetText)  // ps street = original street in csv then update this row
                            {
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {psStreet}");
                                return true;
                            }
                            allStreetText.Append($"\r\n\"{psStreet}\"");

                            //if (streetText == currStreet)
                            //{
                            //    // Find and click EditAddress in this row
                            //    IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                            //        row.FindElement(By.Id("EditAddress"))
                            //    ));
                            //    editAddressLink.Click();
                            //    Log($"Clicked EditAddress for street: {streetText}");
                            //    return true;
                            //}
                        }
                        if (trs.Count() == 2 && firstRow != null && psStreet != "")   // replace existing address to new csv address
                        {
                            // Find and click EditAddress in this row
                            IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                firstRow.FindElement(By.Id("EditAddress"))
                            ));
                            editAddressLink.Click();
                            Log($"Clicked EditAddress for street: {psStreet}");
                            return true;
                        }
                        Log($"## Campus: {campus}, Unmatched CurrentAddress: StateStudentNumber: {stateStudentNumber}, CurrentAddress: {currAddr}, NewAddress: {street}, {city}, {state}, {zip}, \r\nList of Streets compared to \"{currStreetText}\": {allStreetText.ToString()}");
                        return false;
                    }
                }
                catch (OpenQA.Selenium.NoSuchElementException )
                {
                    Log("no expand button");
                }
                
                if (expandBtn == null)
                {

                    var table = wait.Until(ExpectedConditions.ElementExists(By.Id("physical-address-table")));
                    Log("Physical address table found.");

                    // Get all <tr> elements within tbody
                    var rows = table.FindElements(By.XPath(".//tbody/tr"));
                    int rowCount = rows.Count;


                    if (rowCount < 2)
                    {  // try again
                        table = wait.Until(ExpectedConditions.ElementExists(By.Id("physical-address-table")));
                        Log("Physical address table found again.");
                        rows = table.FindElements(By.XPath(".//tbody/tr"));
                        rowCount = rows.Count;
                        if (rowCount < 2)
                        {
                            Log($"# Campus: {campus}, Seleniun found only {rowCount} rows into Powerschool address table after 1 retry for student: {stateStudentNumber} {relationship}");
                            return false;
                        }
                    }
                    int maxRetries = 3; // Number of retries
                    int retryDelayMs = 1000; // 1 second delay between retries
                    IWebElement addressBtn = null;
                    // var elem = drv.FindElement(By.Id("edit-address-button-0"));
                    // return elem.Displayed && elem.Enabled ? elem : null;                    // Get all <td> elements in the row
                    var cells = rows[1].FindElements(By.TagName("td"));
                    if (cells.Count >= 2) // Ensure there’s at least 2 <td>s
                    {
                        var actionCell = cells[10];
                        var editButton = actionCell.FindElement(By.XPath(".//button[@aria-label='Edit']"));

                        wait.Until(ExpectedConditions.ElementToBeClickable(editButton));

                        // Scroll into view (optional, helps with visibility)
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", editButton);

                        // Attempt regular click
                        editButton.Click();
                        Log("Edit button clicked for Address row!");
                        return true; 
                    }
                    throw new Exception("Less than 3 columns on address table");
                }
                throw new Exception("Expand button found but not preocessed");
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        // From the Contact Details page, find the address that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsEmailPage(string stateStudentNumber, string relationship, string current)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Then wait for it to be visible
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='email-address-table']")));

                // Get all rows within tbody (exclude header row with <th>)
                var table = wait.Until(ExpectedConditions.ElementExists(By.Id("email-address-table")));
                var rows = table.FindElements(By.XPath(".//tbody/tr"));
                if (rows.Count() < 2)
                {
                    Log($"# No emails in PowerSchool for student {stateStudentNumber} {relationship}, attempting to click on Add Email button");

                    var addEmailBtn = wait.Until(drv =>
                    {
                        var elem = drv.FindElement(By.Id("add-email-button"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    addEmailBtn.Click();
                    return true;
                }

                foreach (var row in rows.Skip(1)) // skip header row
                {
                    // Get all <td> elements in the row
                    var cells = row.FindElements(By.TagName("td"));
                    if (cells.Count >= 2) // Ensure there’s at least 2 <td>s
                    {
                        var actionCell = cells[3];
                        var editButton = actionCell.FindElement(By.XPath(".//button[@aria-label='Edit']"));

                        // Wait for the overlay to disappear
                        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.XPath("//img[@class='stoplight-symbol']")));
                        Log("Overlay cleared.");

                        // Ensure button is clickable
                        wait.Until(ExpectedConditions.ElementToBeClickable(editButton));
                        Log("Edit button is clickable.");

                        // Scroll into view (optional)
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", editButton);

                        // Attempt regular click
                        try
                        {
                            editButton.Click();
                            Log("Edit button clicked normally!");
                        }
                        catch (ElementClickInterceptedException)
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
                            Log("Edit button clicked via JavaScript!");
                        }
                        Log($"Edit button clicked for email row!");
                        return true; // Stop after clicking the first match
                    }
                }
                throw new Exception("### Unable to find an email row with an Edit button to click");
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        // From the Contact Details page, find the phone that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsPhonePage(string campus, string stateStudentNumber, string relationship, string phone, string phoneType)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Then wait for it to be visible
                //wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='phone-number-table']")));

                //// Get all rows within tbody (exclude header row with <th>)
                //var table = wait.Until(ExpectedConditions.ElementExists(By.Id("phone-number-table")));
                //var rows = table.FindElements(By.XPath(".//tbody/tr"));
                // Wait for table to be visible

                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='phone-number-table']")));
                Log("Phone table visible.");

                // Wait for data rows to load (at least one <tr> with <td>)
                var table = wait.Until(drv =>
                {
                    var tbl = drv.FindElement(By.Id("phone-number-table"));
                    var dataRows = tbl.FindElements(By.XPath(".//tbody/tr[td]"));
                    return dataRows.Any() ? tbl : null; // Retry until data rows appear
                });
                Log("Table has data rows.");


                // Wait for row count to stabilize (Angular rendering complete)
                int previousCount = 0;
                var rows = wait.Until(drv =>
                {
                    var currentRows = table.FindElements(By.XPath(".//tbody/tr"));
                    int currentCount = currentRows.Count;
                    if (currentCount > previousCount && currentCount > 1) // More than just header
                    {
                        previousCount = currentCount;
                        Thread.Sleep(500); // Brief pause to check if more rows load
                        var newRows = table.FindElements(By.XPath(".//tbody/tr"));
                        return newRows.Count == currentCount ? newRows : null; // Stable if count doesn’t change
                    }
                    return null; // Retry if not enough rows or still growing
                });
                Log($"Table fully loaded with {rows.Count} rows (including header).");

                foreach (var row in rows.Skip(1)) // skip header row
                {
                    // Get all <td> elements in the row
                    var cells = row.FindElements(By.TagName("td"));
                    if (cells.Count >= 2) // Ensure there’s at least 2 <td>s
                    {
                        if (string.IsNullOrWhiteSpace(phoneType))
                            {
                            var typeCell = cells[2]; // Third <td> for Phone Number
                            var spans = typeCell.FindElements(By.TagName("span"));
                            foreach (var span in spans)
                            {
                                if (FormatPhoneNumber(span.Text.Trim()) == FormatPhoneNumber(phone))
                                {
                                    Log($"# Found '{phone}' in row: {row.GetAttribute("id")}");
                                    // Click Edit button in 6th <td> (index 5)
                                    var actionCell = cells[5];
                                    var editButton = actionCell.FindElement(By.XPath(".//button[@aria-label='Edit']"));

                                    // Wait for the overlay to disappear
                                    wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.XPath("//img[@class='stoplight-symbol']")));
                                    Log("Overlay cleared.");

                                    // Ensure button is clickable
                                    wait.Until(ExpectedConditions.ElementToBeClickable(editButton));
                                    Log("# Edit button is clickable.");

                                    // Scroll into view (optional)
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", editButton);

                                    // Attempt regular click
                                    try
                                    {
                                        editButton.Click();
                                        Log("# Edit button clicked normally!");
                                    }
                                    catch (ElementClickInterceptedException)
                                    {
                                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
                                        Log("# Edit button clicked via JavaScript!");
                                    }
                                    Log($"Edit button clicked for {phone} row!");
                                    return true; // Stop after clicking the first match
                                }
                            }
                        }
                        else 
                        {
                            var typeCell = cells[1]; // Second <td> for Type
                            var spans = wait.Until(drv =>
                            {
                                var spanElements = typeCell.FindElements(By.TagName("span"));
                                return spanElements.Any() ? spanElements : null; // Wait.Until will retry if no spans
                            });


                            //var spans = typeCell.FindElements(By.TagName("span"));
                            foreach (var span in spans)
                            {
                                if (span.Text.Trim() == phoneType)
                                {
                                    Log($"# Found '{phoneType}' in row: {row.GetAttribute("id")}");
                                    // Click Edit button in 6th <td> (index 5)
                                    var actionCell = cells[5];
                                    var editButton = actionCell.FindElement(By.XPath(".//button[@aria-label='Edit']"));

                                    // Wait for the overlay to disappear
                                    wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.XPath("//img[@class='stoplight-symbol']")));
                                    Log("Overlay cleared.");

                                    // Ensure button is clickable
                                    wait.Until(ExpectedConditions.ElementToBeClickable(editButton));
                                    Log("# Edit button is clickable.");

                                    // Scroll into view (optional)
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", editButton);

                                    // Attempt regular click
                                    try
                                    {
                                        editButton.Click();
                                        Log("# Edit button clicked normally!");
                                    }
                                    catch (ElementClickInterceptedException)
                                    {
                                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
                                        Log("# Edit button clicked via JavaScript!");
                                    }
                                    Log($"Edit button clicked for {phoneType} row!");
                                    return true; // Stop after clicking the first match
                                }
                            }

                        }
                    }
                }

                if (rows.Count() < 2)
                    Log($"# Campus: {campus}, No phones in PowerSchool for student {stateStudentNumber} {relationship}. Adding it.");
                else if (string.IsNullOrWhiteSpace(phoneType))
                    Log($"No '{phoneType}' phone type found in the phone table. Trying to add it");
                else
                    Log($"No '{phone}' with no phone type found in the phone table. trying to add as {phoneType}");

                // add this phone
                // Locate the Add Phone button
                IWebElement addPhoneButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.Id("add-phone-button")
                ));

                // Click via JavaScript
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].click();", addPhoneButton);
                //Console.WriteLine("Clicked the Add Phone button via JavaScript.");

                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        // bring up the student's contact in studentContactsTable using First & Last name & Relationship
        //    if First & Last name match, but PS has no Relationship, use it.
        public bool GoToContactDetailsPage(string campus, string stateStudentNumber, string relationship, string contactFirstName, string contactLastName)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                //IWebElement contactsLink = wait.Until(drv =>
                //{
                //    var elem = drv.FindElement(By.Id("navStudentContactManagement"));
                //    return elem.Displayed && elem.Enabled ? elem : null;
                //});
                //contactsLink.Click();

                // Wait for the table to exist in the DOM
                wait.Until(ExpectedConditions.ElementExists(By.Id("studentContactsTable")));
                // Then wait for it to be visible
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='studentContactsForm']//table[@id='studentContactsTable']")));

                var rows = driver.FindElements(By.XPath("//form[@id='studentContactsForm']//table[@id='studentContactsTable']/tbody/tr"));

                // Locate the matching row
                IWebElement targetLink = null;
                string relationshipValue = null;
                var contactName = "\"" + contactFirstName.Trim().ToLower() + ", " + contactLastName.Trim().ToLower() + "\"";
                var name = "";
                var allNames = "";
                foreach (var row in rows.Skip(1))
                {
                    // Find the <a> with matching names
                    var links = row.FindElements(By.TagName("a"));
                    foreach (var link in links)
                    {
                        var spans = link.FindElements(By.TagName("span"));
                        if (spans.Count >= 2)
                        {
                            string firstName = spans[0].Text.Trim().ToLower();
                            string lastName = spans[1].Text.Trim().ToLower();
                            name = "\"" + firstName + ", " + lastName + "\"";

                            //if (StringUtil.CompareFirstNames(firstName, contactFirstName) && lastName == contactLastName.Trim().ToLower())
                            //{
                                // Check for "Mother" in the same row
                            var relationshipSpan = row.FindElements(By.TagName("td"))
                                .Select(td => td.FindElements(By.TagName("span")))
                                .SelectMany(spansInTd => spansInTd)
                                .FirstOrDefault(span => span.GetAttribute("id")?.Contains("contact-relationshiptype") == true);

                            if (relationshipSpan != null)
                            {
                                relationshipValue = relationshipSpan.Text.Trim();
                                if (StringUtil.CompareFirstNames(firstName, contactFirstName) && lastName == contactLastName.Trim().ToLower() &&
                                    (relationshipValue == relationship || string.IsNullOrWhiteSpace(relationshipValue) || string.IsNullOrWhiteSpace(relationship)))
                                {
                                    targetLink = link;
                                    break;
                                }
                            }
                        }
                    }
                    if (targetLink != null) break; // Exit outer loop if found
                    else
                    {
                        allNames = allNames + $"\r\n  {name}, Relationship: \"{relationshipValue}\"";
                    }
                }

                if (targetLink != null)
                {
                    // Get the original window handle before clicking
                    originalWindow = driver.CurrentWindowHandle;

                    // Wait until clickable and click
                    wait.Until(ExpectedConditions.ElementToBeClickable(targetLink));
                    targetLink.Click();


                    // Wait for a new tab to open (more than 1 window handle)
                    wait.Until(d => d.WindowHandles.Count > 1);

                    // Switch to the new tab
                    foreach (var window in driver.WindowHandles)
                    {
                        if (window != originalWindow)
                        {
                            driver.SwitchTo().Window(window);
                            break;
                        }
                    }

                    // Now find the table in the new tab
                    //var table = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("physical-address-table")));
                    //Log("Table displayed in new tab: " + table.Displayed);
                    //Log("New tab source: " + driver.PageSource);
                }
                else
                {
                    Log($"## Campus: {campus}, StateStudentNumber: {stateStudentNumber}, No student contacts found for {contactFirstName} {contactLastName} with relationship: \"{relationship}\" \r\nCompared {contactName} with: {allNames}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }


        public bool EnterContactAddress(string recordId, string streetLine1, string streetLine2, string unit, string city, string state, string zip)
        {
            string validAddress = "not validating yet";
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                IWebElement addressType = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-type-input"));  //id="physical-address-type-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectat = new SelectElement(addressType);
                selectat.SelectByText("Mailing");

                var csvStreet = StringUtil.AbbreviateWholeString(StripOutStreet(streetLine1));
                IWebElement streeta = wait.Until(drv =>
                {
                    // Locate the textarea
                    var elem = drv.FindElement(By.Id("physical-address-line1-input")); // id = "physical-address-line1-input" and id="physical-address-line2-input" and id="physical-address-unit-input" 
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                Log($"# Replacing '{streeta.GetAttribute("value")}' with '{csvStreet}'");
                streeta.Clear();
                streeta.SendKeys(csvStreet);

                IWebElement streetb = wait.Until(drv =>
                {
                    // Locate the textarea
                    var elem = drv.FindElement(By.Id("physical-address-line2-input")); // id = "physical-address-line1-input" and id="physical-address-line2-input" and id="physical-address-unit-input" 
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                streetb.Clear();
                streetb.SendKeys(streetLine2);

                IWebElement unittb = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("physical-address-unit-input"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                unittb.Clear();
                unittb.SendKeys(unit);

                IWebElement citytb = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("physical-address-city-input")); //id="physical-address-city-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                citytb.Clear();
                citytb.SendKeys(city);

                IWebElement country = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-country-input"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectcn = new SelectElement(country);
                //selectcn.SelectByValue("string:CA");

                IWebElement stateprov = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-state-input"));  //id="physical-address-state-input" <option label="Alberta (AB)" value="344" selected="selected">Alberta (AB)</option>
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectsp = new SelectElement(stateprov);
                selectsp.SelectByText(state == "AB" ? "Alberta (AB)" : "");

                IWebElement postal = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("physical-address-zip-input"));  //id="physical-address-zip-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                postal.Clear();
                postal.SendKeys(FormatPostalCode(zip));

                string today = DateTime.Today.ToString("MM\\/dd\\/yyyy");
                IWebElement effdt = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("physical-address-startdate-input")); //id="physical-address-startdate-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                effdt.Clear();
                effdt.SendKeys(today);

                if (isMockRun)
                {
                    IWebElement cancel = wait.Until(drv =>
                    {
                        // Locate the dropdown
                        var elem = drv.FindElement(By.Id("physical-address-panel-cancel-button")); //id="physical-address-panel-save-button"
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    cancel.Click();
                    Thread.Sleep(1000);
                    return true;
                }
                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-panel-save-button")); //id="physical-address-panel-save-button"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
                return true;
            }
            catch (Exception ex)
            {
                Log($"validation ended with {validAddress}");
                Log(ex);
                throw ex;
            }
        }

        public bool EnterContactEmail(string recordId, string email, bool isPrimary, string type, ref bool emailChanged)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            try
            {
                wait.Until(d => d.WindowHandles.Count > 1);
                string originalWindow = driver.CurrentWindowHandle;
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                Log("Switched to new tab.");

                // Wait for basic page load
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                Log("New tab fully loaded.");

                // Wait for email-type-input to exist in the DOM
                wait.Until(d => d.FindElements(By.Id("email-type-input")).Count > 0);
                Log("email-type-input exists in DOM.");

                // Now wait for it to be clickable
                IWebElement emailType = wait.Until(drv =>
                {
                    try
                    {
                        var elem = drv.FindElement(By.Id("email-type-input"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    }
                    catch (NoSuchElementException)
                    {
                        return null; // Wait.Until will retry if not found
                    }
                });

                Log("Email type dropdown ready.");

                //IWebElement emailType = wait.Until(drv =>
                //{
                //    // Locate the dropdown
                //    var elem = drv.FindElement(By.Id("email-type-input"));
                //    return elem.Displayed && elem.Enabled ? elem : null;
                //});

                // Use SelectElement to interact with the dropdown
                SelectElement selectat = new SelectElement(emailType);
                selectat.SelectByText(type);



                IWebElement emailtd = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("email-address-input"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                Log($"# Replacing '{emailtd.GetAttribute("value")}' with '{email}'");
                // save the existing value and compare with the Email/UserName in the Web Account Access section (table) and is the same update these as well
                if (email.Trim().ToLower() != emailtd.GetAttribute("value").Trim().ToLower())
                    emailChanged = true;
                emailtd.Clear();
                emailtd.SendKeys(email.Trim().ToLower());

                // Locate and click the checkbox
                var checkbox = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("web-account-email-input")));
                // Optional: Verify state
                bool isChecked = checkbox.Selected;
                if (isPrimary != isChecked)
                    checkbox.Click();
                //Console.WriteLine("Checkbox clicked!");


                if (isMockRun)
                {
                    IWebElement cancel = wait.Until(drv =>
                    {
                        // Locate the dropdown
                        var elem = drv.FindElement(By.Id("email-panel-cancel-button"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    cancel.Click();
                    return true;
                }

                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("email-panel-save-button"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
                return true;
            }
            catch (Exception ex)
            {
                //Log($"### An error occurred: {ex.Message}, {ex.GetType().FullName}");
                Log(ex);
                IWebElement closeButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.ClassName("ui-dialog-titlebar-close")
                ));

                // Click the button
                closeButton.Click();
                //Console.WriteLine("Clicked the Close Dialog button to facilitate retry.");
                throw ex;
            }
        }

        public bool EnterContactPhone(string recordId, string phone, bool acceptsText, bool isPreferred, string type)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            try
            {
                wait.Until(d => d.WindowHandles.Count > 1);
                string originalWindow = driver.CurrentWindowHandle;
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                Log("Switched to new tab.");

                // Wait for the page to load (e.g., ready state or specific element)
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                Log("New tab fully loaded.");
                
                IWebElement phoneType = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("phone-type-input"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                var ptype = string.IsNullOrWhiteSpace(type) ? "Mobile" : type;
                // Use SelectElement to interact with the dropdown
                SelectElement selectat = new SelectElement(phoneType);
                selectat.SelectByText(ptype);


                IWebElement phonetd = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.Id("phone-number-input"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                Log($"# Replacing '{phonetd.GetAttribute("value")}' with '{phone}'");
                phonetd.Clear();
                phonetd.SendKeys(FormatPhoneNumber(phone.Trim()));

                // Locate and click the checkbox
                var acceptsTextCheckbox = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("phone-sms-input")));
                // Optional: Verify state
                bool acceptsTextChecked = acceptsTextCheckbox.Selected;
                if (acceptsText != acceptsTextChecked)
                    acceptsTextCheckbox.Click();

                // Locate and click the checkbox
                var prefrredCheckbox = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("phone-preferred-input")));
                // Optional: Verify state
                bool isPreferredChecked = prefrredCheckbox.Selected;
                if (isPreferred != isPreferredChecked)
                    prefrredCheckbox.Click();
                //Console.WriteLine("Checkbox clicked!");


                if (isMockRun)
                {
                    IWebElement cancel = wait.Until(drv =>
                    {
                        // Locate the dropdown
                        var elem = drv.FindElement(By.Id("phone-panel-cancel-button"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    cancel.Click();
                    return true;
                }

                // Locate and click Submit
                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("phone-panel-save-button"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                IWebElement closeButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.ClassName("ui-dialog-titlebar-close")
                ));

                // Click the button
                closeButton.Click();
                Log("Clicked the Close Dialog button to facilitate retry.");
                Thread.Sleep(3000);
                throw ex;
            }
        }

        public bool UpdateUserNameIfNeeded(string recordId, string email)
        {
            // goto Web Account Access table and pull the Username and Account Email column values
            string username = string.Empty;
            string accountEmail = string.Empty;
            // Find the Username value
            username = driver.FindElement(By.XPath("//td[@id='web-account-user-name']/span")).Text;

            // Find the Account Email value
            accountEmail = driver.FindElement(By.XPath("//td[@id='web-account-email']/span")).Text;



            if (username != email || accountEmail != email)
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                // Wait for the stoplight image to disappear
                try
                {
                    wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector("img.stoplight")));
                    Log("Stoplight overlay disappeared.");
                }
                catch (WebDriverTimeoutException)
                {
                    Log("Warning: Stoplight overlay did not disappear within 20 seconds. Attempting to proceed.");
                }

                // Wait for the Edit Account button to be clickable
                var editButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("edit-account-button")));

                // Log button state for debugging
                Log($"Button displayed: {editButton.Displayed}, enabled: {editButton.Enabled}");

                // Attempt to click the button
                try
                {
                    editButton.Click();
                    Log("Edit Account button clicked successfully.");
                }
                catch (ElementClickInterceptedException ex)
                {
                    Log("Click intercepted: " + ex.Message);
                    // Take a screenshot for debugging
                    var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    screenshot.SaveAsFile("click_intercepted.png");

                    // Fallback: Use JavaScript to click
                    Log("Attempting JavaScript click as fallback.");
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
                    Log("Edit Account button clicked via JavaScript.");
                }

                // click Edit Account button
                if (username != email)
                {
                    var usernameField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("web-account-username-input")));

                    // Clear the existing value and enter a new username
                    usernameField.Clear();
                    usernameField.SendKeys(email);
                    Log($"# Username for {recordId} updated successfully from '{username}' to '{email}'.");
                }

                if (accountEmail != email)
                {
                    var emailField = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("web-account-recovery-email-input")));

                    // Clear the existing value and enter a new recovery email
                    emailField.Clear();
                    emailField.SendKeys(email);
                    Log($"# Account Email for {recordId} updated successfully from '{accountEmail}' to '{email}'.");
                }

                if (isMockRun)
                {
                    // Wait for the Cancel button to be clickable
                    var cancelButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("email-panel-cancel-button")));
                    cancelButton.Click();
                    Log("Cancel button clicked successfully.");
                }
                else
                {
                    // Wait for the Submit button to be clickable
                    var submitButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("email-panel-save-button")));
                    submitButton.Click();
                    Log("Submit button clicked successfully.");
                }


            }
            return true;
        }

        public bool EnterEditAddress(string street, string city, string state, string zip, DateTime? effectiveDate)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                IWebElement addressType = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.XPath("//select[@ng-model='addressData.addressType']"));  //id="physical-address-type-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectat = new SelectElement(addressType);
                selectat.SelectByValue("string:Mailing");

                IWebElement addressFormat = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.XPath("//select[@ng-model='addressData.addressFormat']")); // not present
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectaf = new SelectElement(addressFormat);
                selectaf.SelectByValue("string:Mail-CA");

                var csvStreet = StringUtil.AbbreviateStreet(StripOutStreet(street));
                IWebElement streeta = wait.Until(drv =>
                {
                    // Locate the textarea
                    var elem = drv.FindElement(By.XPath("//textarea[@ng-model='addressData.street']")); // id = "physical-address-line1-input" and id="physical-address-line2-input" and id="physical-address-unit-input" 
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                Log($"# Replacing '{streeta.GetAttribute("value")}' with '{csvStreet}'");
                streeta.Clear();
                streeta.SendKeys(csvStreet);

                IWebElement citytb = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.XPath("//input[@ng-model='addressData.city']")); //id="physical-address-city-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                citytb.Clear();
                citytb.SendKeys(city);

                IWebElement stateprov = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.XPath("//select[@ng-model='addressData.stateProvince']"));  //id="physical-address-state-input" <option label="Alberta (AB)" value="344" selected="selected">Alberta (AB)</option>
                    //  id="physical-address-country-input" <option label="Canada (CA)" value="CA" selected="selected">Canada (CA)</option>
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectsp = new SelectElement(stateprov);
                selectsp.SelectByValue("string:" + state);

                IWebElement postal = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.XPath("//input[@ng-model='addressData.postalCode']"));  //id="physical-address-zip-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                postal.Clear();
                postal.SendKeys(FormatPostalCode(zip));

                string today = DateTime.Today.ToString("MM\\/dd\\/yyyy");
                if (effectiveDate.HasValue) today = effectiveDate.Value.ToString("MM\\/dd\\/yyyy");
                IWebElement effdt = wait.Until(drv =>
                {
                    // Locate the input
                    var elem = drv.FindElement(By.XPath("//input[@ng-model='addressData.effectiveDate']")); //id="physical-address-startdate-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                effdt.Clear();
                effdt.SendKeys(today);
                // address validator popups up here
                //var validateButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[text()='Validate']")));
                //validateButton.Click();

                if (isMockRun)
                {
                    IWebElement cancel = wait.Until(drv =>
                    {
                        var elem = drv.FindElement(By.Id("addressDrawerCancel"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    cancel.Click();
                    Thread.Sleep(1000);
                    return true; 
                }

                IWebElement sub = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("addressDrawerSubmit"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                
                sub.Click();


                // Check for validation error
                try
                {
                    // Short timeout to quickly check for error
                    wait.Timeout = TimeSpan.FromSeconds(5);
                    bool errorPresent = wait.Until(drv =>
                    {
                        var errors = drv.FindElements(By.ClassName("feedback-error"));
                        return errors.Count > 0 && errors[0].Displayed && !string.IsNullOrEmpty(errors[0].Text);
                    });

                    // Error found, click Cancel
                    Log($"# Validation error: {driver.FindElement(By.ClassName("feedback-error")).Text}");
                    IWebElement cancelButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("addressDrawerCancel")));
                    cancelButton.Click();
                    Log("Cancel clicked due to validation error.");
                }
                catch (WebDriverTimeoutException)
                {
                    // No error, assume submit succeeded and dialog closed
                    Log("Submit succeeded, moving to next dialog.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        public void Quit()
        {
            driver.Quit();
        }

        public void ClickEditButtonForContact(string name, string relationship)
        {
            // Construct the XPath to find the row with the specific Name and Relationship
            string rowXPath = $"//table[@id='studentContactsTable']//tr[td//span[contains(@id, 'contact-first-name') and contains(text(), '{name.Split(' ')[0]}')] and td//span[contains(@id, 'contact-last-name') and contains(text(), '{name.Split(' ')[1]}')] and td/span[contains(@id, 'contact-relationshiptype') and text()='{relationship}']]";

            // Locate the row
            IWebElement row = driver.FindElement(By.XPath(rowXPath));

            // Find the Edit button within that row (using its title or class)
            IWebElement editButton = row.FindElement(By.XPath(".//button[@title='Edit' and contains(@class, 'editButton')]"));

            // Click the Edit button
            editButton.Click();
        }

        // Should be at student's menu, selecting Compliance and then Compliance Demographics, clicking it
        public bool GoToStudentCompliantPage()
        {
            try
            {
                // Wait for the "Student Profile" li to be present
                WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                IWebElement studentProfileLink = wait2.Until(drv =>
                {
                    // Assuming the parent UL or some container has an identifiable attribute or class
                    // Here I'm guessing, adjust this XPath based on your actual HTML structure
                    var elem = drv.FindElement(By.XPath("//li[contains(., 'Compliance')]"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Click the "Student Profile" li to open the menu
                studentProfileLink.Click();

                // find and click the Compliance Demographic link
                var complianceDemographicLink = driver.FindElement(By.Id("navABStudentInfo"));
                complianceDemographicLink.Click();
                return true;

            }
            catch (Exception ex)
            {
                Log(ex);
                return false;
            }
        }

        public void GoToStudenProfilePage()
        {
            try
            {
                // Wait for the "Student Profile" li to be present
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                IWebElement studentProfileLi = wait.Until(drv =>
                {
                    // Assuming the parent UL or some container has an identifiable attribute or class
                    // Here I'm guessing, adjust this XPath based on your actual HTML structure
                    var elem = drv.FindElement(By.XPath("//li[contains(., 'Student Profile')]"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Click the "Student Profile" li to open the menu
                studentProfileLi.Click();

                // moved from next method
                IWebElement contactsLink = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("navStudentContactManagement"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                contactsLink.Click();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        public void CloseContactDetailsTab()
        {
            var studentTabs = 0;
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Store the original window handle (assuming you started with one tab)
            string originalWindow = driver.CurrentWindowHandle;

            // Get all window handles (tabs)
            var allWindows = driver.WindowHandles;

            foreach (var window in allWindows)
            {
                // Switch to the tab
                driver.SwitchTo().Window(window);

                // Check the title
                string title = driver.Title;
                Log($"# Current tab title: {title}");
                if (title.Contains("Contact Management"))
                {
                    if (studentTabs == 0)
                        studentTabs++;
                    else
                    {
                        driver.Close();
                        Log($"# Closed '{title}' tab.");
                    }
                }

                else if (title == "Contact Details")
                {
                    // Close the tab
                    driver.Close();
                    Log($"# Closed '{title}' tab.");
                }
            }

            // Switch back to the original tab (or another open tab)
            if (driver.WindowHandles.Count > 0)
            {
                driver.SwitchTo().Window(driver.WindowHandles[0]); // First remaining tab
                Log($"Switched back to tab: {driver.Title}");
            }
            else
            {
                Log("No tabs left open.");
            }
            return; // Stop after closing the target tab

            Log("No tab with title 'Contact Details' found when trying to close Contact Details.");
        }

        public void CloseContactTab()
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            //wait.Until(d => d.WindowHandles.Count > 1);
            //driver.SwitchTo().Window(driver.WindowHandles.Last());

            // Do your processing here (e.g., edit address, validate, accept)
            // ...

            // Close the new tab
            driver.Close();

            // Switch back to the original tab
            driver.SwitchTo().Window(originalWindow);

            Log("New Contact tab closed, back to original tab. Current URL: " + driver.Url);
        }

        public string FormatPostalCode(string postalCode)
        {
            // Remove all spaces and trim
            string cleaned = postalCode.Replace(" ", "").Trim();

            // Check if the cleaned length is 6
            if (cleaned.Length == 6)
            {
                // Take first 3 chars, add space, take last 3 chars
                return cleaned.Substring(0, 3) + " " + cleaned.Substring(3, 3).ToUpper();
            }
            else
            {
                // Return original or throw an error if invalid
                return postalCode.ToUpper(); // Or handle invalid input differently
            }
        }

        public string StripOutStreet(string streetFull)
        {
            int commaIndex = streetFull.IndexOf(",");
            if (commaIndex != -1) // Check if comma exists
            {
                string street = streetFull.Substring(0, commaIndex).Trim();
                return street; // Outputs: "115 Marquis COURT SE"
            }
            else
            {
                return streetFull; // If no comma, return original
            }
        }

        public static string FormatPhoneNumber(string phoneNumber)
        {
            // Guard against null or empty input
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Strip all non-digit characters
            string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Check if we have at least 10 digits
            if (digitsOnly.Length < 10)
                return phoneNumber; // Return original if invalid

            // Take the first 10 digits (ignore extras like country codes for now)
            string areaCode = digitsOnly.Substring(0, 3);
            string prefix = digitsOnly.Substring(3, 3);
            string lineNumber = digitsOnly.Substring(6, 4);

            // Format as (###)###-####
            return $"({areaCode}){prefix}-{lineNumber}";
        }

        public void Log(Exception ex)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logEntry = $"{timestamp} - ";
            var details = ExceptionHelper.GetFullExceptionDetails(ex);
            Console.WriteLine("Full Exception Messages:");
            Console.WriteLine(logEntry + details.FullMessage);
            Console.WriteLine("\nFull Stack Trace:");
            Console.WriteLine(logEntry + details.FullStackTrace);
        }

        public void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logEntry = $"{timestamp} - ";
            string msgType = "";
            var msg = message;
            try
            {
                if (message.Substring(0, 1) == "#")
                {
                    if (message.Substring(0, 3) == "###")
                        msgType = "Error";
                    else if (message.Substring(0, 2) == "##")
                    {
                        msgType = " Warn";
                        File.AppendAllText(logFilePath, logEntry + msg + Environment.NewLine);
                    }
                    else if (message.Substring(0, 3) == "#-#")
                    {
                        msgType = " Info";
                    }
                    else if (message.Substring(0, 3) == "#+#")
                    {
                        msgType = " Info";
                    }
                    else if (message.Substring(0, 3) == "#--")
                    {
                        msgType = " Info";
                        msg = msg + "\r\n";
                    }
                    else if (message.Substring(0, 3) == "#++")
                    {
                        msgType = " Info";
                        Console.WriteLine("\r\n" + $"{timestamp} - {msgType}: {msg}");
                        return;
                    }
                    else if (message.Substring(0, 1) == "#")
                    {
                        msgType = " Info";
                    }
                }
                else  // debug msg
                {
                   if (showDebugStatements)
                    {
                        msgType = "Debug";
                        msg = "    " + message;
                    }
                   else
                    {
                        return;
                    }
                }
                Console.WriteLine($"{timestamp} - {msgType}: {msg}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log failure: {ex.Message}");
            }
        }

        public void HandleSiblingAddressDialog(string campus, string studentNumber)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5)); // 5-second timeout

            try
            {
                // Wait for the Submit button to be present and clickable
                IWebElement submitButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.Id("btnSubmit")
                ));

                // If found, click it
                submitButton.Click();
                Log($"## Campus: {campus}, Sibling address dialog found and Submit clicked for {studentNumber}.");
            }
            catch (WebDriverTimeoutException)
            {
                // Dialog didn’t appear within 5 seconds, assume no siblings
                Log("No sibling address dialog detected.");
            }
        }
        public void InitializeDriver()
        {
            if (driver == null)
            {
                var options = new ChromeOptions();
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                driver = new ChromeDriver(options);
                //wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                driver.Navigate().GoToUrl(psUrl);
                Log("WebDriver initialized.");
            }
            // Remove implicit wait (optional, since we'll use explicit waits)
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
        }

        public void CleanupDriver()
        {
            if (driver != null)
            {
                try { driver.Quit(); }
                catch { }
                driver = null;
                //var wait = null;
                Log("WebDriver cleaned up.");
            }
        }

        public class ExceptionDetails
        {
            public string FullMessage { get; set; }
            public string FullStackTrace { get; set; }
        }

        public static class ExceptionHelper
        {
            public static ExceptionDetails GetFullExceptionDetails(Exception ex)
            {
                var messages = new StringBuilder();
                var stackTraces = new StringBuilder();
                Exception currentException = ex;

                // Traverse all inner exceptions
                while (currentException != null)
                {
                    // Append the current exception's message
                    messages.AppendLine(currentException.Message);

                    // Append the current exception's stack trace (if available)
                    if (!string.IsNullOrEmpty(currentException.StackTrace))
                    {
                        stackTraces.AppendLine(currentException.StackTrace);
                    }

                    // Move to the inner exception
                    currentException = currentException.InnerException;

                    // Add a separator between exceptions if there's more
                    if (currentException != null)
                    {
                        messages.AppendLine("--- Inner Exception ---");
                        stackTraces.AppendLine("--- Inner Exception Stack Trace ---");
                    }
                }

                return new ExceptionDetails
                {
                    FullMessage = messages.ToString().Trim(),
                    FullStackTrace = stackTraces.ToString().Trim()
                };
            }
        }

    }
}
