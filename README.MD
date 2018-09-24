
[![Coverage Status](https://coveralls.io/repos/github/aseduto/sqlscript/badge.svg?branch=master)](https://coveralls.io/github/aseduto/sqlscript?branch=master)

[![Build status](https://ci.appveyor.com/api/projects/status/6qbt7d22pwut27t6?svg=true)](https://ci.appveyor.com/project/aseduto/sqlscript)



# Sql Scripter

Sql Scripter is an opinionated tool to implement a complex CI/CD pipeline on sql server while being source control freandly.

## Create a Full script of a database respecting dependency

```
sqlscripter dbindex -S <database> -U <user> -P <password> -d <dbtarget> -i <index-file> --query-mode
```

```
sqlscripter script -S <database> -U <user> -P <password> -d <dbtarget> -i <index-file> -o <output-directory>
```

```
sqlscripter build -S <database> -U <user> -P <password> -d <dbtarget> -i <index-file> -o <output-file>
```



