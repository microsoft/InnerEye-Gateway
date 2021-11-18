# Set-ExecutionPolicy -ExecutionPolicy RemoteSigned
Invoke-WebRequest -Uri "https://dicom.offis.de/download/dcmtk/dcmtk365/bin/dcmtk-3.6.5-win64-dynamic.zip" -OutFile ".\dcmtk.zip"
Expand-Archive -Path ".\dcmtk.zip" -DestinationPath ".\"

Invoke-WebRequest -Uri "https://www.dclunie.com/dicom3tools/workinprogress/winexe/dicom3tools_winexe_1.00.snapshot.20211009110822.zip" -OutFile ".\dicom3tools.zip"
Expand-Archive -Path ".\dicom3tools.zip" -DestinationPath ".\dicom3tools"
