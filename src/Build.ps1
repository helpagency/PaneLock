$ErrorActionPreference = 'Stop'
$sourceFolder = $PSScriptRoot
$outputFolder = Split-Path -Parent $sourceFolder
$buildFolder = Join-Path ([System.IO.Path]::GetTempPath()) ('PaneLockBuild-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildFolder -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$iconGenerator = Join-Path $buildFolder 'CreateIcon.exe'
$icon = Join-Path $outputFolder 'PaneLock.ico'
& $compiler /nologo /target:exe "/out:$iconGenerator" /reference:System.Drawing.dll (Join-Path $sourceFolder 'CreateIcon.cs')
if ($LASTEXITCODE -ne 0) { throw 'Icon compiler failed' }
& $iconGenerator $icon
$companionZip = Join-Path $buildFolder 'YouTubeCompanion.zip'
Compress-Archive -Path (Join-Path $outputFolder 'YouTubeCompanion\*') -DestinationPath $companionZip -Force
$app = Join-Path $outputFolder 'PaneLock.exe'
$license = Join-Path $outputFolder 'LICENSE'
$manifest = Join-Path $sourceFolder 'app.manifest'
$config = Join-Path $sourceFolder 'app.config'
$references = @('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll')
$sources = Get-ChildItem -LiteralPath $sourceFolder -Filter '*.cs' | Where-Object { $_.Name -notin @('SetupProgram.cs','CreateIcon.cs') } | Select-Object -ExpandProperty FullName
& $compiler /nologo /optimize+ /target:winexe /platform:x64 "/out:$app" "/win32icon:$icon" "/win32manifest:$manifest" "/resource:${companionZip},YouTubeCompanion" "/resource:${config},AppConfig" "/resource:${license},MITLicense" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'App compiler failed' }
Copy-Item -LiteralPath $config -Destination ($app+'.config') -Force
$license=Join-Path $outputFolder 'LICENSE'
if(Test-Path -LiteralPath $license){Write-Output 'MIT license included in repository and release packaging.'}
$setup = Join-Path $outputFolder 'PaneLock-Setup.exe'
& $compiler /nologo /optimize+ /target:winexe /platform:x64 "/out:$setup" "/win32icon:$icon" "/win32manifest:$manifest" "/resource:${app},PaneLockPayload" "/resource:${companionZip},YouTubeCompanion" "/resource:${config},AppConfig" "/resource:${license},MITLicense" @references (Join-Path $sourceFolder 'Installer.cs') (Join-Path $sourceFolder 'SetupProgram.cs') (Join-Path $sourceFolder 'Native.cs') (Join-Path $sourceFolder 'Version.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer compiler failed' }
Write-Output "Built $app and $setup"
