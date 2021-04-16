Enable-WindowsOptionalFeature -Online -FeatureName MSMQ-Server -All

Stop-Service -DisplayName "ReceiveService"

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe Source\Microsoft.InnerEye.Listener\Microsoft.InnerEye.Listener.Receiver\bin\x64\Release\Microsoft.InnerEye.Listener.Receiver.exe /u

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe Source\Microsoft.InnerEye.Listener\Microsoft.InnerEye.Listener.Receiver\bin\x64\Release\Microsoft.InnerEye.Listener.Receiver.exe

Start-Service -DisplayName "ReceiveService"