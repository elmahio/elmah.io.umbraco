# elmah.io.umbraco
Integrates elmah.io with Umbraco CMS.

## Installation
That's easy! elmah.io.umbraco installs through NuGet:

```
PS> Install-Package elmah.io.umbraco
```

## Details
The elmah.io.umbraco package basically installs and configured three things:

* The elmah.io log4net appender (Warn and above)
* An Umbraco content finder for logging 404's
* ELMAH with elmah.io as error log

All unhandled exceptions as well as 404's are logged automatically. Warnings, errors and fatal messages logged through log4net are send to elmah.io as well.