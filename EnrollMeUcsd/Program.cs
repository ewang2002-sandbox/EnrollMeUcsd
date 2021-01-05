using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

static void Wait(IWebDriver driver, Func<IWebDriver, bool> func, TimeSpan time)
{
	var wait = new WebDriverWait(driver, time);
	wait.Until(func);
}

var sectionsToWatch = new List<string>
{
	"029808"
};

const int maximumClasses = 1;
Console.WriteLine("[INFO] Enter UCSD Username.");
var username = Console.ReadLine() ?? string.Empty;
Console.WriteLine("[INFO] Enter UCSD Password.");
var password = Console.ReadLine() ?? string.Empty;
Console.Clear();
if (username.Length == 0 || password.Length == 0)
{
	Console.WriteLine("[ERROR] Empty Username or Password. Try Again.");
	return;
}

var chromeOptions = new ChromeOptions();
#if !DEBUG
chromeOptions.AddArgument("--headless");
#endif
chromeOptions.AddArgument("log-level=3");
var chromeService = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
chromeService.SuppressInitialDiagnosticInformation = true;
using var driver = new ChromeDriver(chromeService, chromeOptions);
#if DEBUG
driver.Manage().Window.Maximize();
#endif

// Go to WebReg and login. 
driver.Navigate().GoToUrl("https://act.ucsd.edu/webreg2/start");
driver.FindElementByName("urn:mace:ucsd.edu:sso:username")
	.SendKeys(username);
driver.FindElementByName("urn:mace:ucsd.edu:sso:password")
	.SendKeys(password + Keys.Enter);

// Invalid password.
if (driver.FindElementsById("_login_error_message").Any())
{
	Console.WriteLine("[ERROR] Invalid Password. Try Again.");
	driver.Close();
	return;
}

try
{
	Console.WriteLine("[INFO] Please authenticate this session with Duo 2FA.");
	Wait(driver, x => x.Url.Contains("start"), TimeSpan.FromMinutes(2));
}
catch (Exception)
{
	Console.WriteLine("[ERROR] Failed to authenticate. Try again.");
	driver.Close();
	return;
}

Wait(driver, x => x.FindElements(By.Id("startpage-button-go")).Count != 0,
	TimeSpan.FromSeconds(5));
Console.WriteLine("[INFO] Logged In!");
driver.FindElementById("startpage-button-go").Click();
await Task.Delay(TimeSpan.FromSeconds(1));
// click on "Advanced Search" 
driver.FindElementById("advanced-search").Click();
var classesEnrolledIn = new List<string>();
while (true)
{
	foreach (var section in sectionsToWatch)
	{
		// click "Reset" button
		driver.FindElementById("search-div-t-reset").Click();
		// find the "Section ID" search box
		driver.FindElementById("search-div-t-t3-i4").SendKeys(section + Keys.Enter);
		// delay so the table can properly load
		// We know the table is fully loaded when the spinner is gone
		Wait(driver, x => x.FindElements(By.ClassName("wr-spinner-loading")).Count == 0, 
			TimeSpan.FromSeconds(5));
		// Account for minor delay. 
		await Task.Delay(50); 
		// check to see if there even are rows to check 
		if (driver.FindElementsById("search-group-header-id").Count == 0)
		{
			Console.WriteLine($"[INFO] Section ID {section} not found.");
			continue;
		}

		// click on the first row (there will only be one row)
		driver.FindElementById("search-group-header-id").Click();
		// check if the enroll button exists. If it does, click. 
		var enrollButton = driver.FindElementsById($"search-enroll-id-{section}");
		if (enrollButton.Count == 0)
		{
			Console.WriteLine($"[INFO] Unable to enroll in {section}. Enroll button doesn't exist.");
			continue;
		}

		// click on the enroll button 
		enrollButton[0].Click();
		// a popup should now appear
		// TODO find specific button id
		var allPossibleButtons = driver.FindElementsByClassName("ui-button-text");
		if (allPossibleButtons.All(x => x.Text != "Confirm"))
		{
			Console.WriteLine($"[INFO] Unable to enroll in {section}. Confirm button not found.");
			continue;
		}

		allPossibleButtons.First(x => x.Text == "Confirm").Click();
		Wait(driver, x => x.FindElements(By.Id("dialog-after-action")).Count != 0,
			TimeSpan.FromSeconds(10));
		classesEnrolledIn.Add(section);
		Console.WriteLine($"[INFO] Successfully added section ID {section}!");
		var popupBox = driver.FindElementsById("dialog-after-action");
		if (popupBox.Any())
		{
			try
			{
				var emailButton = driver.FindElementById("dialog-after-action-email");
				emailButton.Click();
				Wait(driver, x => x.FindElements(By.Id("dialog-msg-close")).Count != 0,
					TimeSpan.FromSeconds(10));
				var closeEmailConfirmButton = driver.FindElementById("dialog-msg-close");
				closeEmailConfirmButton.Click();
			}
			catch (Exception)
			{
				try
				{
					driver.FindElementById("dialog-after-action-close").Click();
				}
				catch (Exception)
				{
					// ignore it 
				}
			}
		}

		if (classesEnrolledIn.Count >= maximumClasses)
			goto outLoop;
	}
}

outLoop:
Console.WriteLine($"Successfully Enrolled In {classesEnrolledIn.Count} Classes.");
Console.WriteLine($"Classes: {string.Join(", ", classesEnrolledIn)}");