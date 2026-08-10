<#
.SYNOPSIS
    Tests Test-MapperOutput.ps1 against known-good and known-bad fixtures.

.DESCRIPTION
    A validator that is silently wrong is worse than no validator - it
    manufactures false confidence, which is the exact failure mode this whole
    skill exists to prevent. This script proves the validator grades correctly.

    Each case copies the known-good fixture tree to a temp directory, applies
    one deliberate defect, and asserts the validator reports the specific
    failure item expected (and the expected exit code).

    Run from anywhere:  ./Test-Validator.ps1
    Exit 0 = all cases passed, 1 = one or more cases failed.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$validator = Join-Path $scriptDir 'Test-MapperOutput.ps1'
$fixtures = Join-Path $scriptDir 'fixtures'
$goodDocs = Join-Path $fixtures 'docs-good'
$srcRoots = @((Join-Path $fixtures 'src'), (Join-Path $fixtures 'sql'))

if (-not (Test-Path $validator)) { throw "Validator not found at $validator" }
if (-not (Test-Path $goodDocs)) { throw "Fixtures not found at $goodDocs" }

$script:Passed = 0
$script:Failed = 0
$script:Failures = @()

function New-Sandbox {
    $dir = Join-Path ([IO.Path]::GetTempPath()) ("mapper-test-" + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    Copy-Item -LiteralPath $goodDocs -Destination $dir -Recurse
    return $dir
}

function Edit-Sandbox {
    param([string]$File, [string]$Find, [string]$Replace)
    $content = Get-Content -LiteralPath $File -Raw
    # .Contains, not -like: backtick is the wildcard escape char and these
    # fixtures are full of backticked citations.
    if (-not $content.Contains($Find)) { throw "Mutation target not found in ${File}: $Find" }
    ($content -replace [regex]::Escape($Find), $Replace) | Set-Content -LiteralPath $File -NoNewline
}

function Invoke-Validator {
    param([string]$Mode, [string]$Path, [string[]]$Roots = $srcRoots)
    if ($Roots -and $Roots.Count -gt 0) {
        $raw = & $validator -Mode $Mode -Path $Path -SourceRoot $Roots 2>&1
    }
    else {
        $raw = & $validator -Mode $Mode -Path $Path 2>&1
    }
    $code = $LASTEXITCODE
    $text = ($raw | Out-String)
    $json = $null
    try { $json = $text | ConvertFrom-Json } catch { }
    return [pscustomobject]@{ ExitCode = $code; Json = $json; Text = $text }
}

function Assert-Case {
    <#
      Name          - case label
      Result        - Invoke-Validator output
      ExpectPass    - expected value of report.pass
      ExpectExit    - expected exit code
      ExpectItem    - a failure 'item' that must be present (optional)
      ForbidItem    - a failure 'item' that must NOT be present (optional)
      ExpectSeverity- required severity of ExpectItem (optional)
    #>
    param(
        [string]$Name,
        $Result,
        [bool]$ExpectPass,
        [int]$ExpectExit,
        [string]$ExpectItem,
        [string]$ForbidItem,
        [string]$ExpectSeverity
    )

    $problems = @()

    if ($null -eq $Result.Json) {
        $problems += "no parseable JSON report (exit $($Result.ExitCode)): $($Result.Text.Trim())"
    }
    else {
        if ($Result.Json.pass -ne $ExpectPass) {
            $problems += "expected pass=$ExpectPass, got pass=$($Result.Json.pass)"
        }
        $items = @()
        if ($Result.Json.PSObject.Properties.Name -contains 'failures' -and $Result.Json.failures) {
            $items = @($Result.Json.failures)
        }
        if ($ExpectItem) {
            $hit = @($items | Where-Object { $_.item -eq $ExpectItem })
            if ($hit.Count -eq 0) {
                $names = ($items | ForEach-Object { $_.item }) -join ', '
                $problems += "expected failure item '$ExpectItem'; got: [$names]"
            }
            elseif ($ExpectSeverity -and $hit[0].severity -ne $ExpectSeverity) {
                $problems += "expected severity '$ExpectSeverity' on '$ExpectItem', got '$($hit[0].severity)'"
            }
        }
        if ($ForbidItem) {
            $bad = @($items | Where-Object { $_.item -eq $ForbidItem })
            if ($bad.Count -gt 0) { $problems += "unexpected failure item '$ForbidItem'" }
        }
    }

    if ($Result.ExitCode -ne $ExpectExit) {
        $problems += "expected exit $ExpectExit, got $($Result.ExitCode)"
    }

    if ($problems.Count -eq 0) {
        $script:Passed++
        Write-Host "  PASS  $Name" -ForegroundColor Green
    }
    else {
        $script:Failed++
        $script:Failures += "$Name :: $($problems -join '; ')"
        Write-Host "  FAIL  $Name" -ForegroundColor Red
        foreach ($p in $problems) { Write-Host "          $p" -ForegroundColor DarkRed }
    }
}

Write-Host "`nTest-MapperOutput.ps1 validator test suite" -ForegroundColor Cyan
Write-Host "-----------------------------------------"

$sandboxes = @()

try {
    # -- Case 1: the known-good tree must pass cleanly -----------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Assert-Case -Name 'known-good tree passes' `
        -Result (Invoke-Validator -Mode Batch -Path $sb) -ExpectPass $true -ExpectExit 0

    # -- Case 2: missing required heading ------------------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') -Find '## Failure States' -Replace '### Failure States'
    Assert-Case -Name 'missing required H2 heading is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'heading.Failure States'

    # -- Case 3: fabricated line number (real file, impossible line) ---------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') `
        -Find 'Billing/OrderApproval.aspx.cs:23' -Replace 'Billing/OrderApproval.aspx.cs:9999'
    Assert-Case -Name 'fabricated line number is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'citations.lineInRange'

    # -- Case 4: citation to a file that does not exist ----------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') `
        -Find 'Billing/OrderApproval.aspx.cs:31' -Replace 'Billing/NoSuchFile.aspx.cs:31'
    Assert-Case -Name 'citation to nonexistent file is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'citations.pathExists'

    # -- Case 5: frontmatter confidence counts disagree with the body --------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') -Find 'verified: 12' -Replace 'verified: 99'
    Assert-Case -Name 'confidence_summary mismatch is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'confidence_summary.verified'

    # -- Case 6: template placeholder left in the body -----------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') `
        -Find '## Failure States' -Replace "## Failure States`n`n- Rule description - to be filled in`n"
    Assert-Case -Name 'unreplaced template placeholder is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'body.placeholder'

    # -- Case 7: business rule bullet with no confidence tag -----------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') `
        -Find '- Approval only succeeds when the order is still Pending - `usp_ApproveOrder.sql:14` `(verified in code)`' `
        -Replace '- Approval only succeeds when the order is still Pending - `usp_ApproveOrder.sql:14`'
    Assert-Case -Name 'untagged business rule is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'citations.businessRule'

    # -- Case 8: stub doc (the classic ghost-completion artifact) ------------
    $sb = New-Sandbox; $sandboxes += $sb
    "# Order Approval`n`nTODO" | Set-Content -LiteralPath (Join-Path $sb 'features/order-approval.md')
    Assert-Case -Name 'stub doc with no frontmatter is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'frontmatter.present'

    # -- Case 9: THE GHOST-COMPLETION CASE -----------------------------------
    #    index says 'documented' but no file was ever written.
    $sb = New-Sandbox; $sandboxes += $sb
    Remove-Item -LiteralPath (Join-Path $sb 'features/order-approval.md') -Force
    Assert-Case -Name 'index row claiming documented with no file on disk is caught' `
        -Result (Invoke-Validator -Mode Index -Path $sb) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'index.row.docExists'

    # -- Case 10: invalid status value ---------------------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'index.md') -Find '| M | documented |' -Replace '| M | done |'
    Assert-Case -Name 'invalid index status value is caught' `
        -Result (Invoke-Validator -Mode Index -Path $sb) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'index.row.status'

    # -- Case 11: feature_counts disagree with the rows ----------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'index.md') -Find '  documented: 1' -Replace '  documented: 5'
    Assert-Case -Name 'feature_counts mismatch is caught' `
        -Result (Invoke-Validator -Mode Index -Path $sb) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'index.featureCounts.documented'

    # -- Case 12: doc on disk with no index row ------------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Copy-Item (Join-Path $sb 'features/order-approval.md') (Join-Path $sb 'features/order-rejection.md')
    Assert-Case -Name 'orphan doc with no index row is caught' `
        -Result (Invoke-Validator -Mode Index -Path $sb) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'index.orphanDoc'

    # -- Case 13: no SourceRoot => citation checks reported as SKIPPED -------
    #    Must be visible as unperformed coverage, never silently treated as OK.
    $sb = New-Sandbox; $sandboxes += $sb
    Assert-Case -Name 'missing SourceRoot reports citation checks as skipped' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md') -Roots @()) `
        -ExpectPass $true -ExpectExit 0 -ExpectItem 'citations.resolution' -ExpectSeverity 'skipped'

    # -- Case 14: missing doc file in Doc mode -------------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Assert-Case -Name 'validating a nonexistent doc fails rather than passing' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/does-not-exist.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'file.exists'

    # -- Case 15: missing index in Index mode --------------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Remove-Item -LiteralPath (Join-Path $sb 'index.md') -Force
    Assert-Case -Name 'missing index.md fails rather than passing' `
        -Result (Invoke-Validator -Mode Index -Path $sb) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'index.exists'

    # -- Case 16: diagram flag disagrees with body ---------------------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') -Find 'has_diagram: true' -Replace 'has_diagram: false'
    Assert-Case -Name 'has_diagram inconsistent with body is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'diagram.consistency'

    # -- Case 17: open_questions_count disagrees with bullets ----------------
    $sb = New-Sandbox; $sandboxes += $sb
    Edit-Sandbox -File (Join-Path $sb 'features/order-approval.md') -Find 'open_questions_count: 1' -Replace 'open_questions_count: 0'
    Assert-Case -Name 'open_questions_count mismatch is caught' `
        -Result (Invoke-Validator -Mode Doc -Path (Join-Path $sb 'features/order-approval.md')) `
        -ExpectPass $false -ExpectExit 1 -ExpectItem 'open_questions_count'
}
finally {
    foreach ($sb in $sandboxes) {
        try { Remove-Item -LiteralPath $sb -Recurse -Force -ErrorAction Stop } catch { }
    }
}

Write-Host "-----------------------------------------"
Write-Host "Passed: $script:Passed   Failed: $script:Failed" -ForegroundColor $(if ($script:Failed -eq 0) { 'Green' } else { 'Red' })
if ($script:Failed -gt 0) {
    Write-Host "`nFailures:" -ForegroundColor Red
    foreach ($f in $script:Failures) { Write-Host " - $f" -ForegroundColor Red }
    exit 1
}
exit 0
