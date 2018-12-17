
[![Coverage Status](https://coveralls.io/repos/github/aseduto/sqlscript/badge.svg?branch=master)](https://coveralls.io/github/aseduto/sqlscript?branch=master) [![Build status](https://ci.appveyor.com/api/projects/status/6qbt7d22pwut27t6?svg=true)](https://ci.appveyor.com/project/aseduto/sqlscript)


# Sql Scripter

Sql Scripter is an opinionated tool to implement a complex CI/CD pipeline on sql server while being source control friendly.

## Install latast version

```bash
dotnet tool install sqlscripter
```

or update:

```bash
dotnet tool update sqlscripter
```


## Create a Full script of a database respecting dependency

```
sqlscripter dbindex -S <database> -U <user> -P <password> -d <dbtarget> -i <index-file> 
```

```
sqlscripter script -S <database> -U <user> -P <password> -d <dbtarget> -i <index-file> -o <output-directory>
```

```
sqlscripter build -i <index-file> -o <output-file>
```

The first step will create and index indicating the correct order to build your database.
In case there should be any invalid or external dependency it will insert in the file a ## comment.s


The second step will generate script for all objects indicated in the index file. You can use more than one index file. For instance you could have some script to add data after the database is created.

The files generated or re-generated in the first two steps should be put under source control.

Whenever you modify an object you can just re-run the second step.

If you add objects you may re-run the first step or add the new objects manually to the index.

Finally run the third step and you will get the full script to create or update your database. This step can usually be performed on your build server or process.

## 

