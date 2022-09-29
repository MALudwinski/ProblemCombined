using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using ProblemCombined.Models;
using System.Diagnostics;

namespace ProblemCombined.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string workFileName = "mine.dat";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            string[] allfiles = Directory.GetFiles(".\\", "Mine*.dat", SearchOption.AllDirectories);
            foreach (var file in allfiles)
            {
                try { System.IO.File.Delete(file); } catch { }
            }  //  foreach 
            return View();
        }  //  Index() 

        public ActionResult Temperature()
        {
            UploadViewModel model = new UploadViewModel();
            model.screen = ScreenType.Temperature;
            model.Title = "Temperature";
            model.Label = "Find min temperature difference";
            model.explanation = "Select and upload a file to read and find minimum temerature differance.";
            model.format1 = "The file format is the first row of:";
            model.format2 = "  Dy MxT   MnT   AvT   HDDay  AvDP 1HrP TPcpn WxType PDir AvSp Dir MxS SkyC MxR MnR AvSLP";
            model.format3 += "followed by rows of each days data.";

            return View("FindMinDiff-Goals", model);
        }  //  Temperature() 

        public ActionResult Goals()
        {
            UploadViewModel model = new UploadViewModel();
            model.screen = ScreenType.Goals;
            model.Title = "Goals";
            model.Label = "Find min goals difference";
            model.explanation = "Select and upload a file to read and find minimum goals differance from for and against columns.";
            model.format1 = "The file format is the first row of:";
            model.format2 = "     Team            P     W    L   D    F      A     Pts";
            model.format3 = "followed by rows of the teams data.";
            return View("FindMinDiff-Goals", model);
        }  //  Goals() 

        [HttpPost]
        public ActionResult Upload(ScreenType screen, IFormFile postedFile)
        {
            workFileName = CreateWorkFileName();
            try { 
                if (System.IO.File.Exists(workFileName)) System.IO.File.Delete(workFileName); 
                using (FileStream stream = new FileStream(workFileName, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }  //  using 
            }  //  try 
            catch { }

            ProcessStatus theStatus = new ProcessStatus();
            if (screen == ScreenType.Goals) theStatus = ProcessGoalsFile();
            else theStatus = ProcessTemperatureFile();

            if (theStatus.success)
                return PartialView("UploadSuccess", theStatus);

            return PartialView("UploadFailure", theStatus);
        }  //  Upload() 

        public ProcessStatus ProcessGoalsFile()
        {
            ProcessStatus theStatus = new ProcessStatus();
            theStatus.success = false;
            int Count = 0;
            StreamReader sr;
            string? fileLine;

            string fileName = workFileName;
            try
            {
                sr = System.IO.File.OpenText(fileName);
            }
            catch
            {
                theStatus.result = "Failure opening file: " + fileName;
                return theStatus;
            }
            fileLine = sr.ReadLine();
            if (fileLine == null)
            {
                theStatus.result = "Failure reading file: " + fileName;
                return theStatus;
            }  //  if 
            int Floc = fileLine.IndexOf(" F ");
            int Aloc = fileLine.IndexOf(" A ");
            int Teamloc = fileLine.IndexOf(" Team ");

            if ((Floc == -1) || (Aloc == -1) || (Teamloc == -1))
            {
                theStatus.result = "Problem with file format.";
                return theStatus;
            }

            int goalDiffSmallest = 99999;
            string thisTeam, TeamSmallest = "Team";
            string workLine;
            int SpaceLoc, goalsFor, goalsAg, goalsDiff;

            if (fileLine != null)
            {
                Count = 1;
                while ((fileLine = sr.ReadLine()) != null)
                {
                    Count++;
                    workLine = fileLine.Substring(Teamloc).Trim();
                    SpaceLoc = workLine.IndexOf(" ");
                    if (SpaceLoc == -1) continue;
                    thisTeam = workLine.Substring(0, SpaceLoc);

                    workLine = fileLine.Substring(Floc).Trim(); ;
                    SpaceLoc = workLine.IndexOf(" ");
                    workLine = workLine.Substring(0, SpaceLoc).Trim(); ;
                    if (Int32.TryParse(workLine, out goalsFor) == false) continue;

                    workLine = fileLine.Substring(Aloc).Trim(); ;
                    SpaceLoc = workLine.IndexOf(" ");
                    workLine = workLine.Substring(0, SpaceLoc).Trim(); ;
                    if (Int32.TryParse(workLine, out goalsAg) == false) continue;

                    goalsDiff = Math.Abs(goalsFor - goalsAg);
                    if (goalsDiff < goalDiffSmallest)
                    {
                        goalDiffSmallest = goalsDiff;
                        TeamSmallest = thisTeam;
                    }  //  if 
                }  //  while 
            }  //  if fileLine 

            sr.Close();
            sr.Dispose();
            try { System.IO.File.Delete(fileName); } catch { }
            theStatus.result = TeamSmallest;
            theStatus.success = true;
            return theStatus;
        }  //  ProcessGoalsFile() 

        public ProcessStatus ProcessTemperatureFile()
        {
            ProcessStatus theStatus = new ProcessStatus();
            theStatus.success = false;

            string? fileLine;
            string linePart, thisNumber;
            int Count = 0;
            StreamReader sr;

                int MxT, MnT, diff, Dy, minDiff, minDy, nLoc;
                minDiff = 9999;
                minDy = 0;
                try
                {
                    sr = System.IO.File.OpenText(workFileName);
                }
                catch
                {
                    theStatus.result = "Failure opening file: " + workFileName;
                    return theStatus;
                }

            fileLine = sr.ReadLine();
            if (fileLine == null)
            {
                theStatus.result = "Failure reading file: " + workFileName;
                return theStatus;
            }  //  if 

            if (fileLine.Trim().Substring(0,6) != "Dy MxT")
            {
                theStatus.result = "Problem with file format.";
                return theStatus;
            }

            while ((fileLine = sr.ReadLine()) != null)
                {
                    linePart = fileLine.Trim();
                    nLoc = linePart.IndexOf(" ");
                    if (nLoc == -1) continue;
                    thisNumber = linePart.Substring(0, nLoc);
                    if (Int32.TryParse(thisNumber, out Dy) == false) continue;
                    linePart = linePart.Substring(nLoc).Trim();
                    nLoc = linePart.IndexOf(" ");
                    if (nLoc == -1) continue;
                    thisNumber = linePart.Substring(0, nLoc);
                    if (Int32.TryParse(thisNumber, out MxT) == false) continue;
                    linePart = linePart.Substring(nLoc).Trim();
                    nLoc = linePart.IndexOf(" ");
                    if (nLoc == -1) continue;
                    thisNumber = linePart.Substring(0, nLoc);
                    if (Int32.TryParse(thisNumber, out MnT) == false) continue;
                    diff = MxT - MnT;


                    ////Console.WriteLine(Dy.ToString() + " " + MxT.ToString() + " " + MnT.ToString() + " " + diff.ToString());
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        minDy = Dy;
                    }
                    Count++;
                }  //  while 

            sr.Close();
            sr.Dispose();
            try { System.IO.File.Delete(workFileName); } catch { }
            theStatus.result = "Day number: " + minDy.ToString() + " smallest spread: " + minDiff.ToString();
            theStatus.success = true;
            return theStatus;
        }  //  ProcessTemperatureFile() 

        public string CreateWorkFileName()
        {
            string FileName = "Mine";
            DateTime dateT = DateTime.Now;
            int nMonth = dateT.Month + 1;
            string month = nMonth.ToString();
            if (month.Length == 1) month = "0" + month;
            string day = dateT.Day.ToString();
            if (day.Length == 1) day = "0" + day;
            string Hour = dateT.Hour.ToString();
            if (Hour.Length == 1) Hour= "0" + Hour;
            string Min = dateT.Minute.ToString();
            if (Min.Length == 1) Min = "0" + Min;
            string Second = dateT.Second.ToString();
            if (Second.Length == 1) Second = "0" + Second;
            string mils = dateT.Millisecond.ToString();
            while (mils.Length < 3) mils = "0" + mils;

            FileName += dateT.Year.ToString() + month + day + Hour + Min + Second + mils + ".dat";
            return FileName;
        }  //  CreateWorkFileName() 

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}