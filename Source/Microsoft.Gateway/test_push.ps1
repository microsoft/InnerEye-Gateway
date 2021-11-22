$AETitle = "RADIOMICS_APP"
$Call = "PassThroughModel"
$Port = 111
$SendFolder = "..\..\Images\HN\"

& ".\dcmtk-3.6.5-win64-dynamic\bin\storescu.exe" `
	--log-level trace                      <# log level #> `
	--scan-directories                     <# scan directories for input files #> `
	--scan-pattern "*.dcm"                 <# pattern for filename matching (wildcards) #> `
	--aetitle $AETitle                     <# set my calling AE title #> `
	--call $Call                           <# set called AE title of peer #> `
	127.0.0.1                              <# peer #> `
	$Port                                  <# port #> `
	$SendFolder                           <# dcmfile-in #>
