$ErrorActionPreference = 'Stop'
$projDir  = Split-Path -Parent $PSScriptRoot
$protoDir = Join-Path $projDir 'Protos'
New-Item -ItemType Directory -Force -Path $protoDir | Out-Null

$apiUrl        = 'https://raw.githubusercontent.com/esphome/esphome/dev/esphome/components/api/api.proto'
$apiOptionsUrl = 'https://raw.githubusercontent.com/esphome/esphome/dev/esphome/components/api/api_options.proto'
$apiDescriptorUrl = 'https://raw.githubusercontent.com/protocolbuffers/protobuf/refs/heads/main/src/google/protobuf/descriptor.proto'
$apiEmptyUrl = 'https://raw.githubusercontent.com/protocolbuffers/protobuf/refs/heads/main/src/google/protobuf/empty.proto'

Invoke-WebRequest -Uri $apiUrl        -OutFile (Join-Path $protoDir 'api.proto')
Invoke-WebRequest -Uri $apiOptionsUrl -OutFile (Join-Path $protoDir 'api_options.proto')
Invoke-WebRequest -Uri $apiDescriptorUrl -OutFile (Join-Path $protoDir 'descriptor.proto')
Invoke-WebRequest -Uri $apiEmptyUrl -OutFile (Join-Path $protoDir 'empty.proto')

(Get-Content (Join-Path $protoDir 'api_options.proto')).Replace('google/protobuf/descriptor.proto', 'descriptor.proto') | Set-Content (Join-Path $protoDir 'api_options.proto')

Write-Host "Fetched api.proto into $protoDir"
