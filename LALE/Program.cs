using System;
using System.Collections;
using System.Windows.Forms;
using System.Threading;

namespace LALE;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        AELogger.Prepare();
        try
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += ThreadExceptionHandler.ApplicationThreadException;
            Application.ThreadException += ThreadExceptionHandler.ApplicationThreadException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LALEForm());
        }
        catch (Exception e)
        {
            AELogger.Log("Exception: " + e.Message);

            AELogger.Log("Exception: " + e.StackTrace);

            var i = 1;
            while (e.InnerException != null)
            {
                e = e.InnerException;
                AELogger.Log("InnerException " + i + ": " + e.Message);

                AELogger.Log("InnerException " + i + ": " + e.StackTrace);
                i++;
            }
            Console.WriteLine(e.Message);
            MessageBox.Show(@"UNHAPPY ERROR :(\nhey, you should save the logfile.txt and give it to the developers of this tool \n--------------\n " +
                            e.Message + @"\n" + e.StackTrace, @"Exception!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        AELogger.WriteLog();
    }

    private static class ThreadExceptionHandler
    {
        private static void HandleException(object sender, Exception e)
        {
            var exceptionString = "UNHAPPY ERROR :(\nyou should save the logfile.txt and give it to the developers of this tool \n--------------\n ";
            if (e == null)
            {
                AELogger.Log("BIG PROBLEM, EXCEPTION IS NULL");
                exceptionString += "BIG PROBLEM, EXCEPTION IS NULL\n";
            }
            else
            {
                AELogger.Log("Exception: " + e.Message);

                AELogger.Log("Exception: " + e.StackTrace);

                if (e.Data.Count > 0)
                {
                    AELogger.Log("Exception: additional data:");
                    foreach (DictionaryEntry d in e.Data)
                    {
                        AELogger.Log("             " + d.Key + ": " + d.Value);
                    }
                }

                var i = 1;
                var a = e;
                while (a.InnerException != null)
                {
                    a = a.InnerException;
                    AELogger.Log("InnerException " + i + ": " + a.Message);

                    AELogger.Log("InnerException " + i + ": " + a.StackTrace);

                    if (a.Data.Count > 0)
                    {
                        AELogger.Log("InnerException " + i + ": additional data:");
                        foreach (DictionaryEntry d in a.Data)
                        {
                            AELogger.Log("             " + d.Key + ": " + d.Value);
                        }
                    }

                    i++;
                }
                Console.WriteLine(e.Message);
                exceptionString += e.Message + "\n" + e.StackTrace + "\n";
            }

            if (sender != null)
            {
                AELogger.Log("sender is " + sender);
                exceptionString += "sender is " + sender;
            }
            else
            {
                AELogger.Log("sender is null");
                exceptionString += "sender is null";
            }


            MessageBox.Show(exceptionString, @"Exception!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            AELogger.WriteLog();
            Application.Exit();
        }

        public static void ApplicationThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            AELogger.Log("unhandled\ne.IsTerminating = " + e.IsTerminating);
            HandleException(sender, (Exception)e.ExceptionObject);
        }

        public static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AELogger.Log("ThreadException");
            HandleException(sender, e.Exception);
        }
    }

}