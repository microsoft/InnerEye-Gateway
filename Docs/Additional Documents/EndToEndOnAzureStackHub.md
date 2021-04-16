# How to run end to end demo on Azure Stack hub?

Here are the steps:

1. TBD - share image over DICOM
2. Then, open the image (test H&N volume is located in *<TBD>*) and ensure you can see it in the app
3. Select '...' button that is shown when hovering over image in the explorer panel, select "Export", choose destination, check "Send Series", make sure that "Send Structure Set" is unchecked, and click "Export".
4. Observe the console output from solution components to ensure that the process is underway. Once process is complete you should see a yellow "new image received" icon in the left sidebar of the app.
5. Go into the Incoming tab on the app and press "Open" next to the latest segmentation received.

