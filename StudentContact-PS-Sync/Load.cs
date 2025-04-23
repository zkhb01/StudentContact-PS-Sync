using System;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class Load
{
    public static void Main()
    {
        // Connection string for SQL Server
        //string connectionString = "Server=.;Database=ADH2;User Id=SA;Password=Carl0011sept;TrustServerCertificate=True";
        string connectionString = "Server=.;Database=ADH;User Id=SA;Password=Carl0011??;TrustServerCertificate=True";
        //string connectionString = "Server=tcp:dbe9542.database.windows.net,1433;Database=ADH;Authentication=Active Directory Password;User ID=db7542@ffca.onmicrosoft.com; Password=M!ST#Rb0J@NGI3s5; Encrypt=False; TrustServerCertificate = False; Connection Timeout = 30;";
        //string connectionString = "Server=tcp:sqldb-ccsdapi.database.windows.net,1433;Database=CCA_DDH;Persist Security Info=False;User ID=syncuser;Password=lyVvZksUxrf60Mfm1yTyqF7gJ47bEvTfVS59RDKnhzw=;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",

        // Initialize WebDriver
        //IWebDriver driver = new ChromeDriver();
        //driver.Manage().Window.Maximize();

        try
        {
            // Navigate to PowerSchool
            //driver.Navigate().GoToUrl("URL_OF_POWERSCHOOL_LOGIN_PAGETrustServerCertificate=True

            // Login to PowerSchool (This would be specific to your implementation)
            //            LoginToPowerSchool(driver);

            // Fetch data from SQL View
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM ffc.Entity"; // Replace with your view name
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Assuming the view contains data for one student or one record at a time
  //                          UpdatePowerSchool(driver, reader);
                        }
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        finally
        {
            // Close the browser
           // driver.Quit();
        }
    }

    static void LoginToPowerSchool(IWebDriver driver)
    {
        // Example of how you might log in
        driver.FindElement(By.Id("login")).SendKeys("your_username");
        driver.FindElement(By.Id("password")).SendKeys("your_password");
        driver.FindElement(By.Id("loginButton")).Click();

        // Wait for page to load after login
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.Id("mainContent")));
    }

    static void UpdatePowerSchool(IWebDriver driver, SqlDataReader reader)
    {
        // Example: Assuming you're updating a student's contact information
        // Note: You need to know the exact HTML elements' IDs or class names
        driver.FindElement(By.Id("studentName")).SendKeys(reader["StudentName"].ToString());
        driver.FindElement(By.Id("phoneNumber")).SendKeys(reader["PhoneNumber"].ToString());

        // If there's a save or update button
        driver.FindElement(By.Id("updateButton")).Click();

        // Wait for update confirmation or next action
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.Id("updateConfirmation"))); // This should be replaced with actual confirmation method
    }
}
