param($installPath, $toolsPath, $package, $project)

$configFile = $project.ProjectItems.Item("web.config")
$configFileFullPath = $configFile.Properties.Item("FullPath").Value
$fileContent =  Get-Content $configFileFullPath
$xml = [xml]$fileContent
$apiKey = $xml.configuration.elmah.errorLog.apiKey
$logId = $xml.configuration.elmah.errorLog.logId

$logIdTemplate = "ELMAH_IO_LOG_ID"
$apiKeyTemplate = "ELMAH_IO_API_KEY"

$configFolder = $project.ProjectItems.Item("config")
$logConfigFile = $configFolder.ProjectItems.Item("serilog.user.config")
$logConfigFileFullPath = $logConfigFile.Properties.Item("FullPath").Value
$logFileContent =  Get-Content $logConfigFileFullPath

$newInstall = $logFileContent | Select-String $logIdTemplate -Quiet

if ($newInstall) {
	# Update log4net.config
	$logFileContent =  $logFileContent | Foreach-Object {
        $_ -replace $apikeyTemplate, $apiKey `
           -replace $logIdTemplate, $logId
    }
	Set-Content -Value $logFileContent -Path $logConfigFileFullPath
}