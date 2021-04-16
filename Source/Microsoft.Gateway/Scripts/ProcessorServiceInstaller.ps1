Enable-WindowsOptionalFeature -Online -FeatureName MSMQ-Server -All

Stop-Service -DisplayName "ProcessorService"

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe Source\Microsoft.InnerEye.Listener\Microsoft.InnerEye.Listener.Processor\bin\x64\Release\Microsoft.InnerEye.Listener.Processor.exe /u

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe Source\Microsoft.InnerEye.Listener\Microsoft.InnerEye.Listener.Processor\bin\x64\Release\Microsoft.InnerEye.Listener.Processor.exe

Start-Service -DisplayName "ProcessorService"