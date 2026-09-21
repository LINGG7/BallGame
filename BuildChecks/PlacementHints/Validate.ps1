param([string]$UnityData = 'E:/Unity Engine/2022.3.62f3c1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outputRoot = [IO.Path]::GetFullPath($PSScriptRoot)
if (!$outputRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar) -or $outputRoot -match '[\\/](Assets|Temp|Library)([\\/]|$)') { throw 'Unexpected validation output directory.' }
# Refuse linked output directories. No clean, delete, move or asset-regeneration operations.
$ancestor = Get-Item -LiteralPath $outputRoot
while ($null -ne $ancestor) {
    if ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked output path: $($ancestor.FullName)" }
    $ancestor = $ancestor.Parent
}
function Get-References([string]$projectFile) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot $projectFile) -Raw
    foreach ($node in $project.SelectNodes("//*[local-name()='Reference']/*[local-name()='HintPath']")) {
        $hintPath = $node.InnerText
        if (![IO.Path]::IsPathRooted($hintPath)) { $hintPath = Join-Path $projectRoot $hintPath }
        if ((Split-Path $hintPath -Leaf) -ne 'Assembly-CSharp.dll') { '/reference:"' + $hintPath + '"' }
    }
}
function Compile([string]$name, [string[]]$lines) {
    $rsp = Join-Path $outputRoot "$name.rsp"
    $log = Join-Path $outputRoot "$name.log"
    $lines | Set-Content -LiteralPath $rsp -Encoding utf8
    & (Join-Path $UnityData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityData 'DotNetSdkRoslyn/csc.dll') "@$rsp" 2>&1 | Out-String | Set-Content -LiteralPath $log -Encoding utf8
    if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $log; throw "$name compilation failed" }
    Write-Output "PASS: $name compilation"
}
[xml]$runtimeProject = Get-Content -LiteralPath (Join-Path $projectRoot 'Assembly-CSharp.csproj') -Raw
$defines = $runtimeProject.SelectSingleNode("//*[local-name()='DefineConstants']").InnerText
$common = @('/nologo', '/target:library', '/langversion:9', '/nostdlib+')
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse -File | ForEach-Object { '"' + $_.FullName + '"' })
$runtimeDll = Join-Path $outputRoot 'Assembly-CSharp.dll'
$references = @(Get-References 'Assembly-CSharp.csproj')
# This checkout's generated csproj predates the installed tween assembly.
$references += '/reference:"' + (Join-Path $projectRoot 'Library/ScriptAssemblies/PrimeTween.Runtime.dll') + '"'
Compile 'runtime' ($common + @('/define:' + $defines, '/out:"' + $runtimeDll + '"') + $references + $sources)
$playerDefines = ($defines.Split(';') | Where-Object { $_ -notmatch '^UNITY_EDITOR' -and $_ -ne 'DEVELOPMENT_BUILD' }) -join ';'
Compile 'player' ($common + @('/define:' + $playerDefines, '/out:"' + (Join-Path $outputRoot 'PlayerCheck.dll') + '"') + $references + $sources)
$nunit = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Filter nunit.framework.dll -Recurse -File | Select-Object -First 1
if ($null -eq $nunit) { throw 'Unity NUnit dependency is not imported.' }
$editorArgs = $common + @('/define:' + $defines, '/out:"' + (Join-Path $outputRoot 'PlacementHints.EditorCheck.dll') + '"')
$editorArgs += @(Get-References 'Assembly-CSharp-Editor.csproj')
$editorArgs += @('/reference:"' + $runtimeDll + '"', '/reference:"' + $nunit.FullName + '"')
$editorArgs += @('"' + (Join-Path $projectRoot 'Assets/Editor/PlacementHintSetup.cs') + '"', '"' + (Join-Path $projectRoot 'Assets/Tests/Editor/PlacementHintTests.cs') + '"')
$editorArgs += '"' + (Join-Path $projectRoot 'Assets/Editor/PlacementHintGroupWindow.cs') + '"'
$editorArgs += '"' + (Join-Path $projectRoot 'Assets/Editor/LevelDefinitionEditor.cs') + '"'
$editorArgs += '"' + (Join-Path $projectRoot 'Assets/Editor/PlacementHintTestRunner.cs') + '"'
Compile 'editor-tests' $editorArgs
# Compile this reflection-based test independently, without the runtime or TMP assembly.
# The editor utility legitimately needs those references; the test asmdef does not have them.
$testArgs = $common + @('/define:' + $defines, '/out:"' + (Join-Path $outputRoot 'PlacementHints.TestsCheck.dll') + '"')
$testArgs += @(Get-References 'Assembly-CSharp-Editor.csproj' | Where-Object { $_ -notmatch 'Unity\.TextMeshPro\.dll' })
$testArgs += '/reference:"' + $nunit.FullName + '"'
$testArgs += '"' + (Join-Path $projectRoot 'Assets/Tests/Editor/PlacementHintTests.cs') + '"'
Compile 'tests-isolated' $testArgs
Write-Output 'Compile checks only. Run PlacementHintTests and PlacementHistoryTests in Unity Test Runner for behavioural validation.'
