# Test program to exercuse sql from linux

## Build
dotnet build

## Run
`SQLTEST_CONNECTIONSTRING='<CONNECTIONSTRING>' dotnet run`

or 

```
export SQLTEST_CONNECTIONSTRING='<CONNECTIONSTRING>'
dotnet run
```

## Configuration
Optionally set environment variable SQLTEST_DAPPER to `True` to use Dapper.