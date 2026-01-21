using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace PomReport.Config.Security
{
   public static class DpapiSecretStore
   {
       private const string AppFolderName = "PomReport";
       private const string FileName = "db.conn";
       // Roaming AppData per-user (works for everyone, no admin)
       private static readonly string FilePath =
           Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
               AppFolderName,
               FileName
           );
       public static string GetFilePath() => FilePath;
       public static bool Exists() => File.Exists(FilePath);
       public static bool TryLoadConnectionString(out string connectionString)
       {
           connectionString = string.Empty;
           if (!File.Exists(FilePath))
               return false;
           var encrypted = File.ReadAllBytes(FilePath);
           var decrypted = ProtectedData.Unprotect(
               encrypted,
               optionalEntropy: null,
               scope: DataProtectionScope.CurrentUser
           );
           connectionString = Encoding.UTF8.GetString(decrypted);
           return !string.IsNullOrWhiteSpace(connectionString);
       }
       public static string LoadConnectionString()
       {
           if (!TryLoadConnectionString(out var cs))
               throw new InvalidOperationException(
                   $"Connection string not configured. Missing: {FilePath}"
               );
           return cs;
       }
       public static void SaveConnectionString(string connectionString)
       {
           if (string.IsNullOrWhiteSpace(connectionString))
               throw new ArgumentException("Connection string is empty.", nameof(connectionString));
           var folder = Path.GetDirectoryName(FilePath);
           if (!string.IsNullOrWhiteSpace(folder))
               Directory.CreateDirectory(folder);
           var bytes = Encoding.UTF8.GetBytes(connectionString.Trim());
           var encrypted = ProtectedData.Protect(
               bytes,
               optionalEntropy: null,
               scope: DataProtectionScope.CurrentUser
           );
           File.WriteAllBytes(FilePath, encrypted);
       }
   }
}