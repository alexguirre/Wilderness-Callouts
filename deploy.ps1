New-Item -ItemType Directory -Force -Path "bin/Deploy/lspdfr/audio/scanner/WILDERNESS_CALLOUTS_AUDIO"
New-Item -ItemType Directory -Force -Path "bin/Deploy/Plugins/LSPDFR/WildernessCallouts"

Copy-Item -Path "bin/Release/Wilderness Callouts.dll" -Destination "bin/Deploy/Plugins/LSPDFR/Wilderness Callouts.dll"
Copy-Item -Path "resources/Wilderness Callouts Config.ini" -Destination "bin/Deploy/Plugins/LSPDFR/Wilderness Callouts Config.ini"
Copy-Item -Path "resources/BinocularsTexture.png" -Destination "bin/Deploy/Plugins/LSPDFR/WildernessCallouts/BinocularsTexture.png"
Copy-Item -Path "resources/WILDERNESS_CALLOUTS_AUDIO/*.wav" -Destination "bin/Deploy/lspdfr/audio/scanner/WILDERNESS_CALLOUTS_AUDIO"
Copy-Item -Path "dependencies/RAGENativeUI.dll" -Destination "bin/Deploy/RAGENativeUI.dll"
Copy-Item -Path "src\PoliceSmartRadio" -Destination "bin/Deploy/Plugins/LSPDFR" -Recurse

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("bin/Release/Wilderness Callouts.dll")

Compress-Archive -Force -Path "bin/Deploy/*" -DestinationPath "bin/Wilderness Callouts v$($version.ProductMajorPart).$($version.ProductMinorPart).$($version.ProductBuildPart).zip"
Remove-Item -Path "bin/Deploy" -Recurse -Force