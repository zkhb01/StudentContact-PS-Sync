using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Text;

namespace StudentContact_PS_Sync
{
    public class PsWebDriver
    {
        private string originalWindow { get; set; }
        public string logFilePath;
        public bool isMockRun;
        public PsWebDriver(string basePath,  bool isMockRun) {
            driver = new ChromeDriver();
            // Remove implicit wait (optional, since we'll use explicit waits)
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            originalWindow = "";
            this.logFilePath = basePath + $"log_{DateTime.Now.ToString("yyyy-MM-dd_HHmmss")}.txt";
            this.isMockRun = isMockRun;
        }

        private int waitSeconds = 2;
        private string adminUser = "srv_psro@ffca-calgary.com";
        private string adminPassword = "Pj&^MTAE(bR#$B=7:/?6CU\"Hk";
        private string adminWriteUser = "srv_identity5309@ffca-calgary.com";
        private string adminWritePassword = "e?Cn%YPQcE=Ek7qv!qVzm%hCLz";
        private string psUrl = "https://ffca.powerschool.com/admin/";
        private string psHomeUrl = "home.html?searchtype=students";
        public IWebDriver driver;
        private bool first = true;
        private bool goReadOnly = false;

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
                Log("Error: " + ex.Message);
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

        public bool FindStudentOnStudentSearchPage(string studentNumber)
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
                Console.WriteLine($"Class attribute: '{classAttribute}'");

                // Check if showFilters is present
                bool isVisible = classAttribute.Contains("showFilters");
                Console.WriteLine(isVisible ? "Buttons are visible (showFilters present)." : "Buttons are hidden (showFilters absent).");

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
                //    var feedbackElement = wait.Until(drv =>
                //    {
                //        var elements = drv.FindElements(By.Id("searchFeedbackStudent"));
                //        if (elements.Count > 0 && elements[0].Displayed)
                //        {
                //            string text = elements[0].Text.Trim();
                //            Log($"Feedback found: '{text}'");
                //            return elements[0];
                //        }
                //        return null; // Keep waiting
                //    });
                //    noResultsFound = feedbackElement.Text.Trim().Contains("There are no search results");
                //}
                //catch (NoSuchElementException)
                //{
                //    Log("No 'searchFeedbackStudent' found - assuming results exist.");
                //    noResultsFound = false;
                //}
                //catch (WebDriverTimeoutException)
                //{
                //    Log("No 'searchFeedbackStudent' found - assuming results exist.");
                //    noResultsFound = false;
                //}
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
                    Log("Neither element found - unexpected state.");
                    noResultsFound = true;
                }


                first = false;
                if (noResultsFound)
                {
                    Log($"No Student exists with ASN: {studentNumber}");
                    return false;
                }
                return true;
            }
            catch (StaleElementReferenceException ex)
            {
                Log($"Element stale: {ex.Message}");
//                Log("Page source at failure: " + driver.PageSource);
                return false;
            }
            catch (NoSuchElementException ex)
            {
                Log($"Element not found: {ex.Message}");
                return false;
            }
            catch (WebDriverTimeoutException ex)
            {
                Log("Timed out waiting for search outcome.");
                return false;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
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
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                return false;
            }
        }

        // SHould be at the page with the various student data including the address section
        public bool GoToStudentCompliantAddressPage(string stateStudentId, string currAddr, string street, string city, string state, string zip, DateTime? effectiveDate)
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
                        string currStreet = currAddr.Split(',')[0].Trim();
                        // Wait for the address table to appear
                        wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                        // Find all rows in the address table (skip header)
                        var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);
                        StringBuilder allStreetText = new StringBuilder();

                        var currStreetText = StringUtil.AbbreviateStreet(currStreet.Trim()).ToLower();
                        foreach (var row in trs)
                        {
                            // Find the Street <td> (3rd column based on header order)
                            var streetTd = row.FindElements(By.TagName("td"))[2]; // Index 2 for "Street"
                            string streetText = StringUtil.AbbreviateStreet(streetTd.Text.Trim()).ToLower();

                            if (streetText == currStreetText)
                            {
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {streetText}");
                                return true;
                            }
                            allStreetText.Append($"\r\n\"{streetText}\"");
                        }
                        Log($"Unmatched CurrentAddress: StateStudentId: {stateStudentId}, CurrentAddress: {currAddr}, NewAddress: {street}, {city}, {state}, {zip}, \r\nList of Streets compared to \"{currStreetText}\": {allStreetText.ToString()}", false);
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
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                Log(ex.StackTrace);
                return false;
            }
        }

        // From the Contact Details page, find the address that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsAddressPage(string stateStudentId, string relationship, string currAddr, string unit, string street, string city, string state, string zip)
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
                        string currStreet = currAddr.Split(',')[0].Trim();
                        // Wait for the address table to appear
                        wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                          // Find all rows in the address table (skip header)
                        var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);

                        foreach (var row in trs)
                        {
                            // Find the Street <td> (3rd column based on header order)
                            var streetTd = row.FindElements(By.TagName("td"))[2]; // Index 2 for "Street"
                            string streetText = streetTd.Text.Trim();

                            if (streetText == currStreet)
                            {
                                // Find and click EditAddress in this row
                                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                                    row.FindElement(By.Id("EditAddress"))
                                ));
                                editAddressLink.Click();
                                Log($"Clicked EditAddress for street: {streetText}");
                                return true;
                            }
                        }
                        Log($"UnmatchCurrentAddress: StateStudentId: {stateStudentId}, CurrentAddress: {currAddr}, NewAddress: {street}, {city}, {state}, {zip}, {unit}", false);
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
                    {
                        Log($"No Powerschool addresses for student: {stateStudentId} {relationship} ");
                        return false;
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
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}", false);
                Log(ex.StackTrace);
                throw;
            }
        }

        // From the Contact Details page, find the address that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsEmailPage(string stateStudentId, string relationship, string current)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Then wait for it to be visible
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='email-address-table']")));

                IWebElement expandBtn = null;  // uncomment if multiple emails exist and table needs to be expanded via expandBtn
                //try
                //{
                //    expandBtn = driver.FindElement(By.XPath("//a[@ng-show='addressList.length > 1']"));
                //    if ((expandBtn.Displayed && expandBtn.Enabled ? expandBtn : null) == null)
                //        expandBtn = null;
                //    else
                //    {
                //        expandBtn.Click();
                //        // in expanded table find row with current address
                //        string currStreet = currAddr.Split(',')[0].Trim();
                //        // Wait for the address table to appear
                //        wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                //        // Find all rows in the address table (skip header)
                //        var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);

                //        foreach (var row in trs)
                //        {
                //            // Find the Street <td> (3rd column based on header order)
                //            var streetTd = row.FindElements(By.TagName("td"))[2]; // Index 2 for "Street"
                //            string streetText = streetTd.Text.Trim();

                //            if (streetText == currStreet)
                //            {
                //                // Find and click EditAddress in this row
                //                IWebElement editAddressLink = wait.Until(ExpectedConditions.ElementToBeClickable(
                //                    row.FindElement(By.Id("EditAddress"))
                //                ));
                //                editAddressLink.Click();
                //                Log($"Clicked EditAddress for street: {streetText}");
                //                return true;
                //            }
                //        }
                //        Log($"UnmatchCurrentAddress: StateStudentId: {stateStudentId}, CurrentAddress: {currAddr}, NewAddress: {street}, {city}, {state}, {zip}, {unit}", false);
                //        return false;
                //    }
                //}
                //catch (OpenQA.Selenium.NoSuchElementException)
                //{
                //    Log("no expand button");
                //}

                if (expandBtn == null)
                {
                    int maxRetries = 3; // Number of retries
                    int retryDelayMs = 1000; // 1 second delay between retries
                    IWebElement editEmailBtn = null;

                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            // Locate the button (no need to wait for clickability)
                            editEmailBtn = wait.Until(drv =>
                            {
                                try
                                {
                                    var elem = drv.FindElement(By.Id("edit-email-button-0"));
                                    return elem.Displayed && elem.Enabled ? elem : null;
                                }
                                catch (NoSuchElementException)
                                {
                                    return null;
                                }
                            });

                            // Click via JavaScript
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", editEmailBtn);
                            Console.WriteLine("Clicked Edit Email button via JavaScript.");
                            return true;

                            //-----------------------
                            editEmailBtn = wait.Until(drv =>
                            {
                                try
                                {
                                    var elem = drv.FindElement(By.Id("edit-email-button-0"));
                                    return elem.Displayed && elem.Enabled ? elem : null;
                                }
                                catch (NoSuchElementException)
                                {
                                    return null; // Keep retrying within wait.Until
                                }
                            });
                            //---------------------
                            break; // Success, exit loop
                        }
                        catch (WebDriverTimeoutException ex)
                        {
                            Log($"Attempt {attempt + 1}/{maxRetries} failed: {ex.Message}");
                            if (attempt == maxRetries - 1)
                            {
                                throw new Exception("Failed to find edit-email-button-0 after all retries", ex);
                            }
                            Thread.Sleep(retryDelayMs); // Wait before next attempt
                        }
                    }

                    if (editEmailBtn == null)
                    {
                        editEmailBtn = wait.Until(drv =>
                        {
                            var elem = drv.FindElement(By.Id("add-email-button")); //KHB - not sure on Id or if this button exists
                            return elem.Displayed && elem.Enabled ? elem : null;
                        });
                    }
                    editEmailBtn.Click();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}", false);
                Log(ex.StackTrace);
                throw;
            }
        }

        // From the Contact Details page, find the phone that needs to change in the list and click the button to edit it
        public bool GoToContactDetailsPhonePage(string stateStudentId, string relationship, string phone, string phoneType)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Then wait for it to be visible
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//form[@id='contactform']//table[@id='phone-number-table']")));

                // Wait for the address table to appear
                //wait.Until(d => d.FindElement(By.XPath("//table[@ng-show='addressList.length > 0']")).Displayed);

                // Find all rows in the address table (skip header)
                //var trs = driver.FindElements(By.XPath("//table[@ng-show='addressList.length > 0']/tbody/tr")).Skip(1);

                //----
                // Get all rows within tbody (exclude header row with <th>)
                var table = wait.Until(ExpectedConditions.ElementExists(By.Id("phone-number-table")));
                var rows = table.FindElements(By.XPath(".//tbody/tr"));
                if (rows.Count() < 2  )
                {
                    Log($"no phones in PowerSchool for student {stateStudentId} {relationship}", false);
                    return false;
                }
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
                                    Log($"Found '{phone}' in row: {row.GetAttribute("id")}");
                                    // Click Edit button in 6th <td> (index 5)
                                    var actionCell = cells[5];
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
                                    Log($"Edit button clicked for {phone} row!");
                                    return true; // Stop after clicking the first match
                                }
                            }
                        }
                        else 
                        {
                            var typeCell = cells[1]; // Second <td> for Type
                            var spans = typeCell.FindElements(By.TagName("span"));
                            foreach (var span in spans)
                            {
                                if (span.Text.Trim() == phoneType)
                                {
                                    Log($"Found '{phoneType}' in row: {row.GetAttribute("id")}");
                                    // Click Edit button in 6th <td> (index 5)
                                    var actionCell = cells[5];
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
                                    Log($"Edit button clicked for {phoneType} row!");
                                    return true; // Stop after clicking the first match
                                }
                            }

                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(phoneType))
                    Log($"No '{phoneType}' phone type found in the phone table. Trying to add it");
                else
                    Log($"No '{phone}' with no phone type found in the phone table. trying to add as Mobile");
                // add this phone
                // Locate the Add Phone button
                IWebElement addPhoneButton = wait.Until(ExpectedConditions.ElementExists(
                    By.Id("add-phone-button")
                ));

                // Click via JavaScript
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].click();", addPhoneButton);
                Console.WriteLine("Clicked the Add Phone button via JavaScript.");


                return true;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}", false);
                Log(ex.StackTrace);
                throw;
            }
        }

        // bring up the student's contact in studentContactsTable using First & Last name & Relationship
        //    if First & Last name match, but PS has no Relationship, use it.
        public bool GoToContactDetailsPage(string contactFirstName, string contactLastName, string relationship)
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
                    Log($"No link found for {contactFirstName} {contactLastName} with relationship: \"{relationship}\" \r\nCompared {contactName} with: {allNames}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                Log(ex.StackTrace);
                throw;
            }
        }


        public void EnterContactAddress(string recordId, string streetLine1, string streetLine2, string unit, string city, string state, string zip)
        {
            string validAddress = "not validating yet";
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                IWebElement addressType = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-type-input"));  //id="physical-address-type-input"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });

                // Use SelectElement to interact with the dropdown
                SelectElement selectat = new SelectElement(addressType);
                selectat.SelectByText("Mailing");


                IWebElement streeta = wait.Until(drv =>
                {
                    // Locate the textarea
                    var elem = drv.FindElement(By.Id("physical-address-line1-input")); // id = "physical-address-line1-input" and id="physical-address-line2-input" and id="physical-address-unit-input" 
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                streeta.Clear();
                streeta.SendKeys(StringUtil.AbbreviateWholeString(StripOutStreet(streetLine1)));

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
                //  id="physical-address-country-input" <option label="Canada (CA)" value="CA" selected="selected">Canada (CA)</option>
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

                /*
                // address validator popups up here
                var validateButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[text()='Validate']")));
                validateButton.Click();

                // Wait for dialog to appear

                wait.Until(ExpectedConditions.ElementExists(By.XPath("//div[@role='dialog' and contains(@class, 'MuiDialog-paper')]")));
                var dialog = driver.FindElement(By.XPath("//div[@role='dialog' and contains(@class, 'MuiDialog-paper')]"));
                Log("Dialog HTML: " + dialog.GetAttribute("outerHTML"));


                var acceptButtons = wait.Until(drv =>
                {
                    var elem = drv.FindElements(By.XPath("//div[@role='dialog' and contains(@class, 'MuiDialog-paper')]//button[text()='Accept']"));
                    return elem;
                });

                //var acceptButtons = driver.FindElements(By.XPath("//div[@role='dialog' and contains(@class, 'MuiDialog-paper')]//button[text()='Accept']"));
                Log($"total accept buttons: {acceptButtons.Count.ToString()}");
                if (acceptButtons.Count > 0 && acceptButtons[0].Enabled)
                {
                    acceptButtons[0].Click();
                    Log("Accept clicked!");
                    validAddress = "valid";
                }
                else
                {
                    var cancelButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//div[@role='dialog' and contains(@class, 'MuiDialog-paper')]//button[text()='Cancel']")));
                    Log($"Invalid address for: {recordId}, {streetLine1}, {city}, {state}, {zip}");
                    cancelButton.Click();
                    Log("Cancel clicked!");
                    validAddress = "invalid";
                }
                */

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
                    return;
                }
                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("physical-address-panel-save-button")); //id="physical-address-panel-save-button"
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
                return;
 


            }
            catch (Exception ex)
            {
                Log($"validation ended with {validAddress}");
                Log(ex.Message);
                Log(ex.StackTrace);
                throw;
            }
        }

        public void EnterContactEmail(string recordId, string email, bool isPrimary, string type)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                //Thread.Sleep(1000);
                // Switch to new tab
                wait.Until(d => d.WindowHandles.Count > 1);
                string originalWindow = driver.CurrentWindowHandle;
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                Log("Switched to new tab.");

                // Wait for basic page load
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                Log("Tab DOM loaded.");

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
                        return null; // Retry if not found
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
                emailtd.Clear();
                emailtd.SendKeys(email.Trim().ToLower());

                // Locate and click the checkbox
                var checkbox = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("web-account-email-input")));
                // Optional: Verify state
                bool isChecked = checkbox.Selected;
                if (isPrimary != isChecked)
                    checkbox.Click();
                Console.WriteLine("Checkbox clicked!");


                if (isMockRun)
                {
                    IWebElement cancel = wait.Until(drv =>
                    {
                        // Locate the dropdown
                        var elem = drv.FindElement(By.Id("email-panel-cancel-button"));
                        return elem.Displayed && elem.Enabled ? elem : null;
                    });
                    cancel.Click();
                    return;
                }

                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("email-panel-save-button"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Log(ex.StackTrace);
                throw;
            }
        }

        public bool EnterContactPhone(string recordId, string phone, bool acceptsText, bool isPreferred, string type)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
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
                Console.WriteLine("Checkbox clicked!");


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

                IWebElement sub = wait.Until(drv =>
                {
                    // Locate the dropdown
                    var elem = drv.FindElement(By.Id("phonel-panel-save-button"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
                return true;
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                IWebElement closeButton = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.ClassName("ui-dialog-titlebar-close")
                ));

                // Click the button
                closeButton.Click();
                Console.WriteLine("Clicked the Close Dialog button to facilitate retry.");
                return false;
            }
        }


        public void EnterEditAddress(string street, string city, string state, string zip, DateTime? effectiveDate)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

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

                IWebElement streeta = wait.Until(drv =>
                {
                    // Locate the textarea
                    var elem = drv.FindElement(By.XPath("//textarea[@ng-model='addressData.street']")); // id = "physical-address-line1-input" and id="physical-address-line2-input" and id="physical-address-unit-input" 
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                streeta.Clear();
                streeta.SendKeys(StringUtil.AbbreviateStreet(StripOutStreet(street)));

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
                    return; 
                }

                IWebElement sub = wait.Until(drv =>
                {
                    var elem = drv.FindElement(By.Id("addressDrawerSubmit"));
                    return elem.Displayed && elem.Enabled ? elem : null;
                });
                sub.Click();
            }
            catch (Exception ex)
            {
                Log($"An error occurred: {ex.Message}, {ex.GetType().FullName}");
                Log(ex.StackTrace);
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
        public void GoToStudentCompliantPage()
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

            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Log(ex.StackTrace);
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
                Log(ex.Message);
                Log(ex.StackTrace);
            }
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

        public void Log(string message, bool consoleOnly = true)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string logEntry = $"{timestamp} - {message}";
            Console.WriteLine(logEntry);
            try
            {
                if (!consoleOnly)
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Log failure: {ex.Message}");
            }
        }

        public void HandleSiblingAddressDialog()
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
                Console.WriteLine("Sibling address dialog found and Submit clicked.");
            }
            catch (WebDriverTimeoutException)
            {
                // Dialog didn’t appear within 5 seconds, assume no siblings
                Console.WriteLine("No sibling address dialog detected.");
            }
        }
    }
}
