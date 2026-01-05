param(
  [string]$Protoc = ".\protoc.exe",                # expects in PATH or give full path
  [string]$ProtosDir   = "ESPHomeServerApi\Protos",
  [string]$OutDir   = "."
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$cmdArgs = @(
  "--csharp_out=$OutDir",
  'api.proto'
)

Write-Host "Running protoc with args: $cmdArgs"
cd $ProtosDir
& $Protoc $cmdArgs

(Get-Content (Join-Path $outDir 'Api.cs')).Replace('global::ApiOptionsReflection.', '') | Set-Content (Join-Path $outDir 'Api.cs')

Write-Host "Created Api.cs into $outDir"