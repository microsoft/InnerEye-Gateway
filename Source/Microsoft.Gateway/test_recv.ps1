$ReceiveFolder = "TestReceived"
$AETitle = "PACS"
$Port = 104

if (-not(Test-Path $ReceiveFolder))
{
	New-Item $ReceiveFolder -ItemType Directory
}

& ".\dcmtk-3.6.5-win64-dynamic\bin\storescp.exe" `
	--log-level trace                 <# log level #> `
	--aetitle $AETitle                <# set my AE title #> `
	--output-directory $ReceiveFolder <# write received objects to existing directory TestReceived #> `
	$Port                             <# port #>
