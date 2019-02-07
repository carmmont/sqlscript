param([string] $psw = $null
    , [string] $user = "sa"
    , [string] $database = "testdb"
    , [string] $server = "."
    , [string] $script = "create.sql"
    , [string] $action = "export"
    , [string] $dbversion = "1.0.0.0"
)

$ErrorActionPreference = "Stop";

function  export() {
    
"export index" | out-host
dotnet run -- dbindex -S $server -d $database -U $user -P $psw -i index.txt --query-mode
"export objects" | out-host
dotnet run -- script -S $server -d $database -U $user -P $psw '-o' './sql' '-i' 'index.txt' --sql-version 'Version100' --file-version
"build final script" | out-host
dotnet run -- build -S $server -d $database -U $user -P $psw '-b' './sql' '-i' 'index.txt' -o $script --database-version $dbversion
    
}

function coverage() {
    "run test coverage" | Out-Host
    dotnet run -- coverage -S $server -d $database -U $user -P $psw --save "test.xml" -f -s "exec test_coverage 0" 
}



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

Invoke-Expression $action

Set-Location $current

