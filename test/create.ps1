param([string] $psw = $null
    , [string] $user = "sa"
    , [string] $database = "testdb"
    , [string] $server = "."
    , [string] $script = "create.sql"
)

$ErrorActionPreference = "Stop";

$my_dir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

if(('' -eq $psw) -and ('' -ne $user))
{
    Write-Error 'Password not specified'
}

#"[$psw] [$($null -eq $psw)]" | oh

$target = [IO.Path]::GetFullPath([IO.Path]::Combine($my_dir, "../src/sqlscripter"));
$script = [IO.Path]::GetFullPath([IO.Path]::Combine($my_dir, $script));

$current = Get-Location;

Set-Location $target

"export index" | out-host
dotnet run -- dbindex -S $server -d $database -U $user -P $psw -i index.txt --query-mode
"export objects" | out-host
dotnet run -- script -S $server -d $database -U $user -P $psw '-o' './sql' '-i' 'index.txt' --sql-version 'Version100'
"build final script" | out-host
dotnet run -- build -S $server -d $database -U $user -P $psw '-b' './sql' '-i' 'index.txt' -o $script

Set-Location $current

