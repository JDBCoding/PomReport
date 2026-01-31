using System;
using System.Windows.Forms;
using PomReport.Config;
namespace PomReport.App
{
   internal static class Program
   {
       [STAThread]
       static void Main()
       {
           ApplicationConfiguration.Initialize();
           // First-run setup: if config.json doesn't exist next to EXE, run SetupForm
           if (!ConfigStore.Exists())
           {
               using var setup = new SetupForm();
               var result = setup.ShowDialog();
               if (result != DialogResult.OK)
               {
                   // User cancelled setup
                   return;
               }
           }

           Application.Run( new Form1());
       }
   }
}