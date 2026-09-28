# Helper compartido: ejecuta T-SQL con System.Data.SqlClient (incluido en Windows PowerShell 5.1; sin módulos extra).
function Invoke-Sa {
    param([string]$Server, [string]$Database = 'master', [string]$Sql, [System.Management.Automation.PSCredential]$Cred)
    $cs = "Server=$Server;Database=$Database;User Id=$($Cred.UserName);Password=$($Cred.GetNetworkCredential().Password);TrustServerCertificate=True;Connect Timeout=15"
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $c.Open()
    try { $cmd = $c.CreateCommand(); $cmd.CommandText = $Sql; $cmd.CommandTimeout = 0; $cmd.ExecuteNonQuery() | Out-Null }
    finally { $c.Dispose() }
}
