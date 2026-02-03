namespace PomReport.Config.Models
{
   public sealed class AirplanePair
   {
       // Shop-only identifier (never sent to SQL)
       public string? LineNumber { get; set; } 
       // SQL identifiiers
       public string? Vh { get; set; } = "";
       public string? Vz { get; set; } = "";
       // Display / context
       public string? Location { get; set; } = "";
   }
}