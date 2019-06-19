function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

exec { & dotnet restore  }

if($env:APPVEYOR_REPO_TAG_NAME -eq $NULL)
{
    $revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL]
    $suffix = "dev{0:D4}" -f [convert]::ToInt32($revision, 10)
    echo "build: Development build - no commit tag. Package version suffix is $suffix"
    
    exec { & dotnet pack .\FFMediaToolkit\FFMediaToolkit.csproj -c Debug -o .\artifacts --include-symbols --version-suffix=$suffix }
}
else
{
    echo "build: Release build - tagged commit detected. No version suffix"

    exec { & dotnet pack .\FFMediaToolkit\FFMediaToolkit.csproj -c Release -o .\artifacts --include-symbols }
}