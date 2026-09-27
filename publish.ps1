param(
    [ValidateSet('win-x64', 'win-arm64', 'win-x86')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot

try {
    dotnet publish ImePhraseTool.csproj `
        -c Release `
        -r $Runtime `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o publish

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed (exit $LASTEXITCODE)."
    }

    Write-Host ''
    Write-Host 'Publish succeeded.'
    Write-Host 'Output: publish/'
    Write-Host "Runtime: $Runtime"
    Write-Host 'Requirement: .NET 8 Desktop Runtime'
}
finally {
    Pop-Location
}