

#$url = 'https://github.com/Microsoft/sqltoolsservice/blob/master/bin/nuget/Microsoft.SqlServer.Smo.140.2.9.nupkg';
#$url = 'https://raw.github.com/Microsoft/sqltoolsservice/master/bin/nuget/Microsoft.SqlServer.Smo.140.2.9.nupkg';

#$url = 'https://cdn.rawgit.com/Microsoft/sqltoolsservice/v1.1.0/bin/nuget/Microsoft.SqlServer.Smo.140.2.2.nupkg'

$url = gc ./deps.url

if(-Not (test-path './bin/nuget'))
{
     [System.IO.Directory]::CreateDirectory('./bin/nuget');
}


if(-Not (test-path './bin/nuget/Microsoft.SqlServer.Smo.140.2.9.nupkg'))
{

    Invoke-WebRequest $url -OutFile './bin/nuget/Microsoft.SqlServer.Smo.140.2.9.nupkg';

}
