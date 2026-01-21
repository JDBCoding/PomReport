# Test-SqlConnection.ps1
# Reusable SQL connectivity + identity test (safe / minimal)
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\Test-SqlConnection.ps1
# or
#   pwsh -File .\Test-SqlConnection.ps1
$ErrorActionPreference = "Stop"
function Read-ConnectionString {
   Write-Host ""
   Write-Host "Paste your SQL connection string, then press Enter." -ForegroundColor Cyan
   Write-Host "Tip: If it contains a password, avoid saving it in files." -ForegroundColor DarkGray
   $cs = Read-Host "ConnectionString"
   if ([string]::IsNullOrWhiteSpace($cs)) {
       throw "Connection string was empty."
   }
   return $cs.Trim()
}
function Test-SqlConnection {
   param(
       [Parameter(Mandatory=$true)]
       [string]$ConnectionString
   )
   # Try to load System.Data.SqlClient (usually available on Windows/.NET)
   try {
       Add-Type -AssemblyName "System.Data" | Out-Null
   } catch {
       # Not fatal; PowerShell often already has it.
   }
   $conn = $null
   $cmd  = $null
   $rdr  = $null
   try {
       $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
       $conn.Open()
       $cmd = $conn.CreateCommand()
       $cmd.CommandTimeout = 15
       $cmd.CommandText = @"
SELECT
 DB_NAME() AS CurrentDb,
 SYSTEM_USER AS SystemUser,
 SUSER_SNAME() AS LoginName,
 @@SERVERNAME AS ServerName
"@
       $rdr = $cmd.ExecuteReader()
       while ($rdr.Read()) {
           $currentDb  = $rdr["CurrentDb"]
           $systemUser = $rdr["SystemUser"]
           $loginName  = $rdr["LoginName"]
           $serverName = $rdr["ServerName"]
           Write-Host ""
           Write-Host "CONNECTED ✅" -ForegroundColor Green
           Write-Host ("Server   : {0}" -f $serverName)
           Write-Host ("Database : {0}" -f $currentDb)
           Write-Host ("SYSTEM_USER  : {0}" -f $systemUser)
           Write-Host ("SUSER_SNAME(): {0}" -f $loginName)
       }
       return $true
   }
   catch {
       Write-Host ""
       Write-Host "FAILED ❌" -ForegroundColor Red
       Write-Host ("Type   : {0}" -f $_.Exception.GetType().FullName)
       # Most useful message is often InnerException
       Write-Host ("Message: {0}" -f $_.Exception.Message)
       if ($_.Exception.InnerException) {
           Write-Host ("Inner  : {0}" -f $_.Exception.InnerException.Message)
       }
       # Extra hinting for common failures
       $msg = ($_.Exception.Message + " " + ($_.Exception.InnerException?.Message))
       if ($msg -match "Login failed") {
           Write-Host ""
           Write-Host "Hint: This is an AUTH failure (bad user/pw, disabled account, not allowed, wrong auth mode)." -ForegroundColor Yellow
       }
       elseif ($msg -match "certificate chain" -or $msg -match "not trusted") {
           Write-Host ""
           Write-Host "Hint: This is TLS/certificate trust. Try adding: Encrypt=True;TrustServerCertificate=True;" -ForegroundColor Yellow
       }
       elseif ($msg -match "server was not found" -or $msg -match "error: 40" -or $msg -match "Named Pipes Provider") {
           Write-Host ""
           Write-Host "Hint: This is NETWORK/DNS/FIREWALL/INSTANCE name. Verify server/instance and port access." -ForegroundColor Yellow
       }
       return $false
   }
   finally {
       if ($rdr) { try { $rdr.Close() } catch {} }
       if ($cmd) { try { $cmd.Dispose() } catch {} }
       if ($conn) { try { $conn.Close() } catch {} }
   }
}
# ---- main ----
try {
   $cs = Read-ConnectionString
   $ok = Test-SqlConnection -ConnectionString $cs
   Write-Host ""
   if ($ok) {
       Write-Host "DONE ✅" -ForegroundColor Green
       exit 0
   } else {
       Write-Host "DONE (with errors) ❌" -ForegroundColor Red
       exit 1
   }
}
catch {
   Write-Host ""
   Write-Host "SCRIPT ERROR ❌" -ForegroundColor Red
   Write-Host $_.Exception.Message
   exit 2
}