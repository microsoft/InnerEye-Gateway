& ".\dcmtk-3.6.5-win64-dynamic\bin\storescu.exe" `
	--log-level trace                      <# log level #> `
	--scan-directories                     <# scan directories for input files #> `
	--scan-pattern "*.dcm"                 <# pattern for filename matching (wildcards) #> `
	--recurse                              <# recurse within specified directories #> `
	--aetitle RADIOMICS_APP                       <# set my calling AE title #> `
	--call PassThroughModel                      <# set called AE title of peer #> `
	--config-file ".\Microsoft.InnerEye.Listener.Tests.Common\Assets\SCU.cfg" LEExplicitCT <# use profile LEExplicitiCT from config file #> `
	127.0.0.1                              <# peer #> `
	111                                    <# port #> `
	"..\..\Images\LargeSeriesWithContour\" <# dcmfile-in #>
