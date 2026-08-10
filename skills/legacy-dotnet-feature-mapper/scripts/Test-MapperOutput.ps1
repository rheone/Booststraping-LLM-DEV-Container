<#
.SYNOPSIS
    Validates legacy-dotnet-feature-mapper output against references/doc-manifest.md.

.DESCRIPTION
    Mechanical, non-semantic validation of generated documentation. This script
    NEVER runs, builds, or executes the target application - it only reads files.
    (The "static analysis only" rule governs the application under study, not
    this skill's own tooling.)

    Modes:
      Doc    - validate a single features/*.md or shared-components/*.md file
      Index  - validate index.md internal consistency + on-disk backing of every
               row claiming to be documented
      Batch  - Index mode plus Doc mode over every doc in the output tree

    Emits a JSON report to stdout. Exit codes:
      0 = pass, 1 = validation failures found, 2 = script could not run properly

    IMPORTANT for callers: exit code 2 (or no JSON at all) means VALIDATION DID
    NOT HAPPEN. It must never be treated as a pass. Fall back to the manual
    checklist in references/doc-manifest.md and say so in the run log.

.PARAMETER Mode
    Doc | Index | Batch

.PARAMETER Path
    Doc mode: path to the doc. Index/Batch mode: path to the output docs root.

.PARAMETER SourceRoot
    One or more roots used to resolve citation file paths. If omitted, citation
    existence/line-range checks are reported as 'skipped' (which is a failure of
    coverage, not a pass).

.EXAMPLE
    ./Test-MapperOutput.ps1 -Mode Doc -Path docs/features/order-approval.md -SourceRoot src,sql

.EXAMPLE
    ./Test-MapperOutput.ps1 -Mode Batch -Path docs -SourceRoot src,sql
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Doc', 'Index', 'Batch')]
    [string]$Mode,

    [Parameter(Mandatory)]
    [string]$Path,

    [string[]]$SourceRoot,

    [int]$MinCitations = 3,

    [switch]$Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Manifest data. MUST be kept in sync with references/doc-manifest.md.
# ---------------------------------------------------------------------------

$script:Manifest = @{
    Feature = @{
        RequiredFrontmatter = @(
            'id', 'slug', 'title', 'status', 'trigger_type', 'domain',
            'entry_points', 'last_updated', 'source_snapshot',
            'confidence_summary', 'has_diagram', 'open_questions_count'
        )
        RequiredHeadings    = @(
            'Purpose',
            'Predecessors / Successors',
            'Roles / Permissions',
            'Happy Path',
            'Business Rules',
            'Failure States',
            'Database Interactions',
            'Diagram',
            'Open Questions / Unverified Items',
            'Related Shared Components'
        )
        ConditionalHeadings = @{
            'Authentication' = 'trigger_type=webhook'
        }
    }
    Component = @{
        RequiredFrontmatter = @(
            'id', 'slug', 'name', 'component_type', 'location', 'domain',
            'last_updated', 'source_snapshot', 'confidence_summary',
            'used_by', 'open_questions_count'
        )
        RequiredHeadings    = @(
            'What it does',
            'Inputs / Outputs',
            'Business rules embedded here',
            'Side effects',
            'Confidence / Open Questions'
        )
        ConditionalHeadings = @{}
    }
}

$script:ValidStatuses = @(
    'not started',
    'in progress',
    'documented',
    'documented (open questions)',
    'verification failed',
    'candidate orphan/scheduled proc, unconfirmed'
)

# Statuses that assert a doc exists on disk.
$script:StatusesRequiringDoc = @(
    'documented',
    'documented (open questions)',
    'verification failed'
)

# Literal template placeholders that must never survive into a finished doc.
$script:PlaceholderPatterns = @(
    '<feature-slug>', '<component-slug>', '<Feature Name>', '<Component Name>',
    '<count>', '<date>', '<slug>', '<item>', '<Role/check>',
    'feat-XXXX', 'comp-XXXX', 'qst-XXXX',
    'Rule description', 'Plain-language summary of the business capability',
    'Short plain-language summary of the overall'
)

$script:CitationRegex =
    '(?<path>[^\s`|<>()\[\]]+\.(?:cs|vb|aspx|ascx|ashx|asmx|svc|sql|config|xml|master|cshtml|resx|asax))\s*:\s*(?<start>\d+)(?:\s*-\s*(?<end>\d+))?'

$script:ConfidenceTags = @{
    verified   = 'verified in code'
    inferred   = 'inferred from naming'
    unverified = 'unverified assumption'
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function New-Failure {
    param(
        [Parameter(Mandatory)][string]$Item,
        [Parameter(Mandatory)][string]$Expected,
        [Parameter(Mandatory)][string]$Actual,
        [ValidateSet('error', 'skipped')][string]$Severity = 'error',
        [string]$Target = ''
    )
    [pscustomobject]@{
        item     = $Item
        expected = $Expected
        actual   = $Actual
        severity = $Severity
        target   = $Target
    }
}

function Split-Frontmatter {
    <#
      Returns @{ Frontmatter = <string[]>; Body = <string[]>; Found = <bool> }
      Frontmatter is the raw lines between the opening and closing '---'.
    #>
    param([string[]]$Lines)

    if ($Lines.Count -eq 0 -or $Lines[0].Trim() -ne '---') {
        return @{ Frontmatter = @(); Body = $Lines; Found = $false }
    }

    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i].Trim() -eq '---') {
            return @{
                Frontmatter = if ($i -le 1) { @() } else { $Lines[1..($i - 1)] }
                Body        = if ($i + 1 -lt $Lines.Count) { $Lines[($i + 1)..($Lines.Count - 1)] } else { @() }
                Found       = $true
            }
        }
    }

    return @{ Frontmatter = @(); Body = $Lines; Found = $false }
}

function ConvertFrom-SimpleYaml {
    <#
      Deliberately minimal YAML subset parser: top-level scalars, top-level
      lists ('  - item'), and one level of nested scalar maps. Enough for this
      skill's frontmatter, and dependency-free.
      Returns an ordered hashtable. List values become string[]; nested maps
      become hashtables.
    #>
    param([string[]]$Lines)

    $result = [ordered]@{}
    $currentKey = $null
    $currentKind = $null   # 'list' | 'map'

    foreach ($raw in $Lines) {
        if ([string]::IsNullOrWhiteSpace($raw)) { continue }
        if ($raw.TrimStart().StartsWith('#')) { continue }

        $indent = $raw.Length - $raw.TrimStart().Length
        $line = $raw.Trim()

        if ($indent -eq 0) {
            if ($line -match '^(?<k>[A-Za-z0-9_]+)\s*:\s*(?<v>.*)$') {
                $k = $Matches['k']
                $v = $Matches['v']
                # strip trailing inline comment
                $v = ($v -replace '\s+#.*$', '').Trim()
                if ([string]::IsNullOrWhiteSpace($v)) {
                    $result[$k] = $null
                    $currentKey = $k
                    $currentKind = $null
                }
                else {
                    $result[$k] = $v.Trim('"', "'")
                    $currentKey = $null
                    $currentKind = $null
                }
            }
            continue
        }

        if ($null -eq $currentKey) { continue }

        if ($line.StartsWith('- ')) {
            if ($currentKind -ne 'list') {
                $result[$currentKey] = @()
                $currentKind = 'list'
            }
            $result[$currentKey] = @($result[$currentKey]) + @($line.Substring(2).Trim().Trim('"', "'"))
        }
        elseif ($currentKind -eq 'list') {
            # Continuation line of a list-of-maps entry; presence of the key is
            # all this validator needs, so the detail is intentionally ignored.
            continue
        }
        elseif ($line -match '^(?<k>[A-Za-z0-9_ ()/-]+?)\s*:\s*(?<v>.*)$') {
            if ($currentKind -ne 'map') {
                $result[$currentKey] = @{}
                $currentKind = 'map'
            }
            $v = ($Matches['v'] -replace '\s+#.*$', '').Trim().Trim('"', "'")
            $result[$currentKey][$Matches['k'].Trim()] = $v
        }
    }

    return $result
}

function Get-Headings {
    param([string[]]$Body)
    $headings = @()
    $inFence = $false
    foreach ($line in $Body) {
        if ($line -match '^\s*```') { $inFence = -not $inFence; continue }
        if ($inFence) { continue }
        if ($line -match '^##\s+(?<h>.+?)\s*$') { $headings += $Matches['h'].Trim() }
    }
    return $headings
}

function Get-SectionLines {
    <# Lines under a given H2 heading, up to the next H2. #>
    param([string[]]$Body, [string]$Heading)
    $out = @()
    $capturing = $false
    foreach ($line in $Body) {
        if ($line -match '^##\s+(?<h>.+?)\s*$') {
            $capturing = ($Matches['h'].Trim() -eq $Heading)
            continue
        }
        if ($capturing) { $out += $line }
    }
    return $out
}

function Resolve-CitationPath {
    <#
      Resolve a cited path against the source roots.
      Strategy: exact join first; then suffix match against a cached file index.
      Returns a FileInfo-ish path string, or $null.
    #>
    param([string]$CitedPath, [string[]]$Roots)

    if (-not $Roots -or $Roots.Count -eq 0) { return $null }

    $normalized = $CitedPath -replace '\\', '/'

    foreach ($root in $Roots) {
        $candidate = Join-Path $root ($normalized -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $leaf = Split-Path $normalized -Leaf
    if (-not $script:FileIndex.ContainsKey($leaf)) { return $null }

    $candidates = @($script:FileIndex[$leaf])
    if ($candidates.Count -eq 1) { return $candidates[0] }

    foreach ($m in $candidates) {
        $mNorm = ($m -replace '\\', '/')
        if ($mNorm.EndsWith($normalized, [StringComparison]::OrdinalIgnoreCase)) { return $m }
    }

    # Ambiguous leaf, no suffix match: treat as unresolved.
    return $null
}

function Build-FileIndex {
    param([string[]]$Roots)
    $script:FileIndex = @{}
    if (-not $Roots) { return }
    foreach ($root in $Roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -match '^\.(cs|vb|aspx|ascx|ashx|asmx|svc|sql|config|xml|master|cshtml|resx|asax)$' } |
            ForEach-Object {
                $name = $_.Name
                if (-not $script:FileIndex.ContainsKey($name)) { $script:FileIndex[$name] = @() }
                $script:FileIndex[$name] += $_.FullName
            }
    }
}

$script:LineCountCache = @{}
function Get-FileLineCount {
    param([string]$FullPath)
    if ($script:LineCountCache.ContainsKey($FullPath)) { return $script:LineCountCache[$FullPath] }
    $count = (Get-Content -LiteralPath $FullPath -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    # Measure-Object -Line under-counts a file with no trailing newline edge cases; use raw split as backstop.
    $raw = Get-Content -LiteralPath $FullPath -Raw -ErrorAction SilentlyContinue
    if ($null -ne $raw) {
        $split = ($raw -split "`r?`n").Count
        if ($split -gt $count) { $count = $split }
    }
    $script:LineCountCache[$FullPath] = $count
    return $count
}

# ---------------------------------------------------------------------------
# Doc validation
# ---------------------------------------------------------------------------

function Test-MapperDoc {
    param(
        [Parameter(Mandatory)][string]$DocPath,
        [string[]]$Roots
    )

    $failures = @()
    $checks = 0

    if (-not (Test-Path -LiteralPath $DocPath -PathType Leaf)) {
        return [pscustomobject]@{
            target = $DocPath; docType = 'unknown'; pass = $false; checks = 1
            failures = @(New-Failure -Item 'file.exists' -Expected 'doc file present on disk' -Actual 'file not found' -Target $DocPath)
        }
    }

    $lines = @(Get-Content -LiteralPath $DocPath)
    $checks++
    if ($lines.Count -lt 20) {
        $failures += New-Failure -Item 'file.substantive' -Expected 'at least 20 lines' -Actual "$($lines.Count) lines" -Target $DocPath
    }

    $split = Split-Frontmatter -Lines $lines
    $checks++
    if (-not $split.Found) {
        $failures += New-Failure -Item 'frontmatter.present' -Expected 'YAML frontmatter delimited by ---' -Actual 'not found' -Target $DocPath
        return [pscustomobject]@{ target = $DocPath; docType = 'unknown'; pass = $false; checks = $checks; failures = $failures }
    }

    $fm = ConvertFrom-SimpleYaml -Lines $split.Frontmatter
    $body = $split.Body

    $docType = if ($fm.Contains('component_type')) { 'Component' } else { 'Feature' }
    $spec = $script:Manifest[$docType]

    # --- frontmatter keys ---
    foreach ($key in $spec.RequiredFrontmatter) {
        $checks++
        if (-not $fm.Contains($key) -or $null -eq $fm[$key] -or
            (($fm[$key] -is [string]) -and [string]::IsNullOrWhiteSpace($fm[$key]))) {
            $failures += New-Failure -Item "frontmatter.$key" -Expected 'present and non-empty' -Actual 'missing or empty' -Target $DocPath
        }
    }

    # --- id format ---
    $checks++
    if ($fm.Contains('id') -and $fm['id']) {
        $expectedPrefix = if ($docType -eq 'Component') { 'comp-' } else { 'feat-' }
        if ($fm['id'] -notmatch "^$expectedPrefix\d{4}$") {
            $failures += New-Failure -Item 'frontmatter.id.format' -Expected "$expectedPrefix followed by 4 digits" -Actual "$($fm['id'])" -Target $DocPath
        }
    }

    # --- slug matches filename ---
    $checks++
    $expectedSlug = [IO.Path]::GetFileNameWithoutExtension($DocPath)
    if ($fm.Contains('slug') -and $fm['slug'] -and $fm['slug'] -ne $expectedSlug) {
        $failures += New-Failure -Item 'frontmatter.slug.matchesFilename' -Expected $expectedSlug -Actual "$($fm['slug'])" -Target $DocPath
    }

    # --- headings ---
    $headings = Get-Headings -Body $body
    foreach ($h in $spec.RequiredHeadings) {
        $checks++
        if ($headings -notcontains $h) {
            $failures += New-Failure -Item "heading.$h" -Expected "H2 '## $h' present verbatim" -Actual 'missing' -Target $DocPath
        }
    }

    # --- conditional headings ---
    if ($docType -eq 'Feature') {
        $checks++
        $isWebhook = $fm.Contains('trigger_type') -and "$($fm['trigger_type'])" -eq 'webhook'
        $hasAuth = $headings -contains 'Authentication'
        if ($isWebhook -and -not $hasAuth) {
            $failures += New-Failure -Item 'heading.Authentication' -Expected "present because trigger_type=webhook" -Actual 'missing' -Target $DocPath
        }
        elseif (-not $isWebhook -and $hasAuth) {
            $failures += New-Failure -Item 'heading.Authentication' -Expected "omitted because trigger_type != webhook" -Actual 'present' -Target $DocPath
        }
    }

    # --- status value ---
    $checks++
    if ($docType -eq 'Feature' -and $fm.Contains('status') -and $fm['status']) {
        if ($script:ValidStatuses -notcontains "$($fm['status'])") {
            $failures += New-Failure -Item 'frontmatter.status.valid' -Expected ($script:ValidStatuses -join ' | ') -Actual "$($fm['status'])" -Target $DocPath
        }
    }

    # --- placeholders ---
    $bodyText = ($body -join "`n")
    foreach ($ph in $script:PlaceholderPatterns) {
        $checks++
        if ($bodyText.Contains($ph)) {
            $failures += New-Failure -Item 'body.placeholder' -Expected "template placeholder '$ph' replaced with real content" -Actual "placeholder still present" -Target $DocPath
        }
    }

    # --- confidence tag counts vs frontmatter ---
    $actualTagCounts = @{}
    foreach ($kv in $script:ConfidenceTags.GetEnumerator()) {
        $actualTagCounts[$kv.Key] = ([regex]::Matches($bodyText, [regex]::Escape($kv.Value))).Count
    }
    if ($fm.Contains('confidence_summary') -and $fm['confidence_summary'] -is [hashtable]) {
        foreach ($kv in $script:ConfidenceTags.GetEnumerator()) {
            $checks++
            $declared = $fm['confidence_summary'][$kv.Key]
            $actual = $actualTagCounts[$kv.Key]
            if ($null -eq $declared -or "$declared" -notmatch '^\d+$') {
                $failures += New-Failure -Item "confidence_summary.$($kv.Key)" -Expected 'integer count' -Actual "$declared" -Target $DocPath
            }
            elseif ([int]$declared -ne $actual) {
                $failures += New-Failure -Item "confidence_summary.$($kv.Key)" -Expected "$actual (counted in body)" -Actual "$declared (declared in frontmatter)" -Target $DocPath
            }
        }
    }

    # --- at least some claims are tagged at all ---
    $checks++
    $totalTags = ($actualTagCounts.Values | Measure-Object -Sum).Sum
    if ($totalTags -eq 0) {
        $failures += New-Failure -Item 'body.confidenceTags' -Expected 'at least one confidence tag in body' -Actual '0 tags found' -Target $DocPath
    }

    # --- open_questions_count vs actual bullets ---
    $oqHeading = if ($docType -eq 'Component') { 'Confidence / Open Questions' } else { 'Open Questions / Unverified Items' }
    $oqLines = Get-SectionLines -Body $body -Heading $oqHeading
    $oqBullets = @($oqLines | Where-Object { $_ -match '^\s*[-*]\s+\S' })
    $oqNoneStated = ($oqLines -join ' ') -match '(?i)\bnone\b'
    if ($fm.Contains('open_questions_count') -and "$($fm['open_questions_count'])" -match '^\d+$') {
        $checks++
        $declared = [int]$fm['open_questions_count']
        if ($declared -ne $oqBullets.Count -and -not ($declared -eq 0 -and $oqNoneStated)) {
            $failures += New-Failure -Item 'open_questions_count' -Expected "$($oqBullets.Count) (bullets under '$oqHeading')" -Actual "$declared" -Target $DocPath
        }
    }

    # --- diagram flag vs mermaid fence ---
    if ($docType -eq 'Feature') {
        $checks++
        $hasMermaid = $bodyText -match '(?m)^\s*```mermaid'
        $declaredDiagram = $fm.Contains('has_diagram') -and "$($fm['has_diagram'])" -match '(?i)^true$'
        if ($declaredDiagram -and -not $hasMermaid) {
            $failures += New-Failure -Item 'diagram.consistency' -Expected 'a ```mermaid block because has_diagram: true' -Actual 'no mermaid block found' -Target $DocPath
        }
        elseif (-not $declaredDiagram -and $hasMermaid) {
            $failures += New-Failure -Item 'diagram.consistency' -Expected 'has_diagram: true because a mermaid block is present' -Actual 'has_diagram is not true' -Target $DocPath
        }
        if (-not $hasMermaid) {
            $checks++
            $diagramSection = (Get-SectionLines -Body $body -Heading 'Diagram') -join ' '
            if ($diagramSection -notmatch '(?i)no diagram') {
                $failures += New-Failure -Item 'diagram.justification' -Expected "an explicit 'No diagram - <reason>' statement when no diagram is included" -Actual 'neither a diagram nor a stated reason' -Target $DocPath
            }
        }
    }

    # --- citations ---
    $citationMatches = [regex]::Matches($bodyText, $script:CitationRegex)
    $checks++
    if ($citationMatches.Count -lt $MinCitations) {
        $failures += New-Failure -Item 'citations.minimum' -Expected "at least $MinCitations file:line citations" -Actual "$($citationMatches.Count) found" -Target $DocPath
    }

    # every Business Rules bullet must carry a citation or an explicit unverified tag
    $brLines = Get-SectionLines -Body $body -Heading 'Business rules embedded here'
    if ($docType -eq 'Feature') { $brLines = Get-SectionLines -Body $body -Heading 'Business Rules' }
    foreach ($bullet in @($brLines | Where-Object { $_ -match '^\s*[-*]\s+\S' })) {
        $checks++
        $hasCite = [regex]::IsMatch($bullet, $script:CitationRegex)
        $hasTag = $false
        foreach ($t in $script:ConfidenceTags.Values) { if ($bullet.Contains($t)) { $hasTag = $true } }
        if (-not ($hasCite -and $hasTag)) {
            $short = $bullet.Trim()
            if ($short.Length -gt 80) { $short = $short.Substring(0, 80) + '...' }
            $failures += New-Failure -Item 'citations.businessRule' -Expected 'each business rule bullet carries a file:line citation AND a confidence tag' -Actual "uncited or untagged: $short" -Target $DocPath
        }
    }

    # citation targets exist and line numbers are in range
    if (-not $Roots -or $Roots.Count -eq 0) {
        $failures += New-Failure -Item 'citations.resolution' -Expected 'SourceRoot supplied so cited paths can be checked' -Actual 'no SourceRoot given - citation existence NOT verified' -Severity 'skipped' -Target $DocPath
    }
    else {
        $seen = @{}
        foreach ($m in $citationMatches) {
            $citedPath = $m.Groups['path'].Value
            $startLine = [int]$m.Groups['start'].Value
            $endLine = if ($m.Groups['end'].Success) { [int]$m.Groups['end'].Value } else { $startLine }
            $key = "$citedPath::$startLine-$endLine"
            if ($seen.ContainsKey($key)) { continue }
            $seen[$key] = $true

            $checks++
            $resolved = Resolve-CitationPath -CitedPath $citedPath -Roots $Roots
            if (-not $resolved) {
                $failures += New-Failure -Item 'citations.pathExists' -Expected "cited file '$citedPath' exists under a source root" -Actual 'no matching file found' -Target $DocPath
                continue
            }

            $checks++
            $lineCount = Get-FileLineCount -FullPath $resolved
            if ($endLine -gt $lineCount) {
                $failures += New-Failure -Item 'citations.lineInRange' -Expected "line <= $lineCount in '$citedPath'" -Actual "cited line $endLine" -Target $DocPath
            }
            if ($startLine -lt 1) {
                $failures += New-Failure -Item 'citations.lineInRange' -Expected 'line >= 1' -Actual "cited line $startLine" -Target $DocPath
            }
        }
    }

    $hardFailures = @($failures | Where-Object { $_.severity -eq 'error' })
    return [pscustomobject]@{
        target   = $DocPath
        docType  = $docType
        pass     = ($hardFailures.Count -eq 0)
        checks   = $checks
        failures = $failures
    }
}

# ---------------------------------------------------------------------------
# Index validation
# ---------------------------------------------------------------------------

function Test-MapperIndex {
    param([Parameter(Mandatory)][string]$OutputRoot)

    $failures = @()
    $checks = 0
    $indexPath = Join-Path $OutputRoot 'index.md'

    if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
        return [pscustomobject]@{
            target = $indexPath; pass = $false; checks = 1
            failures = @(New-Failure -Item 'index.exists' -Expected 'index.md present at output root' -Actual 'not found' -Target $indexPath)
        }
    }

    $lines = @(Get-Content -LiteralPath $indexPath)
    $split = Split-Frontmatter -Lines $lines
    $checks++
    if (-not $split.Found) {
        $failures += New-Failure -Item 'index.frontmatter' -Expected 'YAML frontmatter' -Actual 'not found' -Target $indexPath
    }
    $fm = ConvertFrom-SimpleYaml -Lines $split.Frontmatter

    foreach ($key in @('last_updated', 'discovery_complete', 'feature_counts', 'scope_ledger', 'scan_ledger')) {
        $checks++
        if (-not $fm.Contains($key)) {
            $failures += New-Failure -Item "index.frontmatter.$key" -Expected 'present' -Actual 'missing' -Target $indexPath
        }
    }

    # --- parse rows ---
    $rows = @()
    foreach ($line in $split.Body) {
        if ($line -notmatch '^\s*\|') { continue }
        if ($line -match '^\s*\|[\s:|-]+\|\s*$') { continue }
        $cells = @(($line -split '(?<!\\)\|')[1..(($line -split '(?<!\\)\|').Count - 2)] | ForEach-Object { $_.Trim() })
        if ($cells.Count -lt 4) { continue }
        if ($cells[0] -eq 'ID' -or $cells[0] -eq 'Feature') { continue }
        $rows += , $cells
    }

    $checks++
    if ($rows.Count -eq 0) {
        $failures += New-Failure -Item 'index.rows' -Expected 'at least one feature row' -Actual 'no data rows parsed' -Target $indexPath
    }

    # Column order per index-schema.md: ID | Feature | Entry Point(s) | Size | Status | Doc | Last Updated | Verified | Notes
    $idSeen = @{}
    $idToDoc = @{}     # feat id -> doc link from the index row ('' if none)
    $statusCounts = @{}
    foreach ($status in $script:ValidStatuses) { $statusCounts[$status] = 0 }

    foreach ($cells in $rows) {
        $id = $cells[0]
        $status = if ($cells.Count -ge 5) { $cells[4] } else { '' }
        $docCell = if ($cells.Count -ge 6) { $cells[5] } else { '' }

        $checks++
        if ($id -notmatch '^feat-\d{4}$') {
            $failures += New-Failure -Item 'index.row.id' -Expected 'feat-NNNN in the first column' -Actual "'$id'" -Target $indexPath
            continue
        }

        $checks++
        if ($idSeen.ContainsKey($id)) {
            $failures += New-Failure -Item 'index.row.idUnique' -Expected "each feat id appears once" -Actual "$id appears more than once" -Target $indexPath
        }
        $idSeen[$id] = $true
        $idToDoc[$id] = if ($docCell -match '\]\((?<p>[^)]+)\)') { $Matches['p'] } else { '' }

        $checks++
        if ($script:ValidStatuses -notcontains $status) {
            $failures += New-Failure -Item 'index.row.status' -Expected ($script:ValidStatuses -join ' | ') -Actual "'$status' (row $id)" -Target $indexPath
        }
        else {
            $statusCounts[$status]++
        }

        # THE GHOST-COMPLETION CHECK: a row claiming completion must have a real file.
        if ($script:StatusesRequiringDoc -contains $status) {
            $checks++
            $linkPath = $null
            if ($docCell -match '\]\((?<p>[^)]+)\)') { $linkPath = $Matches['p'] }
            if (-not $linkPath) {
                $failures += New-Failure -Item 'index.row.docLink' -Expected "row $id with status '$status' links to a doc" -Actual "Doc cell is '$docCell'" -Target $indexPath
            }
            else {
                $full = Join-Path $OutputRoot ($linkPath -replace '/', [IO.Path]::DirectorySeparatorChar)
                $checks++
                if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
                    $failures += New-Failure -Item 'index.row.docExists' -Expected "file '$linkPath' exists on disk (row $id claims status '$status')" -Actual 'file not found - status asserts work that produced no artifact' -Target $indexPath
                }
            }
        }
    }

    # --- feature_counts match reality ---
    if ($fm.Contains('feature_counts') -and $fm['feature_counts'] -is [hashtable]) {
        $keyMap = @{
            'not_started'                  = 'not started'
            'in_progress'                  = 'in progress'
            'documented'                   = 'documented'
            'documented_open_questions'    = 'documented (open questions)'
            'verification_failed'          = 'verification failed'
            'candidate_orphan_unconfirmed' = 'candidate orphan/scheduled proc, unconfirmed'
        }
        foreach ($kv in $keyMap.GetEnumerator()) {
            if (-not $fm['feature_counts'].ContainsKey($kv.Key)) { continue }
            $checks++
            $declared = $fm['feature_counts'][$kv.Key]
            $actual = $statusCounts[$kv.Value]
            if ("$declared" -notmatch '^\d+$' -or [int]$declared -ne $actual) {
                $failures += New-Failure -Item "index.featureCounts.$($kv.Key)" -Expected "$actual (counted from rows)" -Actual "$declared" -Target $indexPath
            }
        }
    }

    # --- orphan docs: a file in features/ with no index row ---
    $featuresDir = Join-Path $OutputRoot 'features'
    if (Test-Path -LiteralPath $featuresDir) {
        foreach ($f in Get-ChildItem -LiteralPath $featuresDir -Filter '*.md' -File) {
            $checks++
            $docLines = @(Get-Content -LiteralPath $f.FullName -TotalCount 40)
            $docFm = ConvertFrom-SimpleYaml -Lines ((Split-Frontmatter -Lines $docLines).Frontmatter)
            $docId = if ($docFm.Contains('id')) { "$($docFm['id'])" } else { '' }
            if (-not $docId -or -not $idSeen.ContainsKey($docId)) {
                $failures += New-Failure -Item 'index.orphanDoc' -Expected "every doc in features/ has a row in index.md" -Actual "$($f.Name) (id '$docId') has no index row" -Target $indexPath
                continue
            }
            # The row exists, but does it actually point at THIS file? A doc
            # whose id belongs to a row linking somewhere else is an untracked
            # artifact - typically a copy/rename that never reached the index.
            $linked = $idToDoc[$docId]
            if ($linked) {
                $linkedFull = Join-Path $OutputRoot ($linked -replace '/', [IO.Path]::DirectorySeparatorChar)
                if ((Test-Path -LiteralPath $linkedFull) -and
                    ((Resolve-Path -LiteralPath $linkedFull).Path -ne $f.FullName)) {
                    $failures += New-Failure -Item 'index.orphanDoc' -Expected "every doc in features/ is the doc its index row links to" -Actual "$($f.Name) carries id '$docId', but that row links to '$linked'" -Target $indexPath
                }
            }
        }
    }

    $hardFailures = @($failures | Where-Object { $_.severity -eq 'error' })
    return [pscustomobject]@{
        target   = $indexPath
        pass     = ($hardFailures.Count -eq 0)
        checks   = $checks
        rows     = $rows.Count
        failures = $failures
    }
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

try {
    Build-FileIndex -Roots $SourceRoot

    $report = [ordered]@{
        schema      = 'legacy-dotnet-feature-mapper/validation-report/1'
        mode        = $Mode
        target      = $Path
        timestamp   = (Get-Date).ToString('o')
        sourceRoots = @($SourceRoot)
        pass        = $true
        checks      = 0
        results     = @()
        failures    = @()
    }

    switch ($Mode) {
        'Doc' {
            $r = Test-MapperDoc -DocPath $Path -Roots $SourceRoot
            $report.results = @($r)
        }
        'Index' {
            $r = Test-MapperIndex -OutputRoot $Path
            $report.results = @($r)
        }
        'Batch' {
            $results = @()
            $results += Test-MapperIndex -OutputRoot $Path
            foreach ($sub in @('features', 'shared-components')) {
                $dir = Join-Path $Path $sub
                if (-not (Test-Path -LiteralPath $dir)) { continue }
                foreach ($f in Get-ChildItem -LiteralPath $dir -Filter '*.md' -File) {
                    $results += Test-MapperDoc -DocPath $f.FullName -Roots $SourceRoot
                }
            }
            $report.results = $results
        }
    }

    foreach ($r in $report.results) {
        $report.checks += $r.checks
        $report.failures += @($r.failures)
        if (-not $r.pass) { $report.pass = $false }
    }

    $json = [pscustomobject]$report | ConvertTo-Json -Depth 8
    Write-Output $json

    if (-not $Quiet) {
        $errCount = @($report.failures | Where-Object { $_.severity -eq 'error' }).Count
        $skipCount = @($report.failures | Where-Object { $_.severity -eq 'skipped' }).Count
        Write-Verbose "pass=$($report.pass) checks=$($report.checks) errors=$errCount skipped=$skipCount"
    }

    exit ($(if ($report.pass) { 0 } else { 1 }))
}
catch {
    $err = [pscustomobject]@{
        schema  = 'legacy-dotnet-feature-mapper/validation-report/1'
        mode    = $Mode
        target  = $Path
        pass    = $false
        ran     = $false
        message = "VALIDATION DID NOT RUN: $($_.Exception.Message)"
        detail  = "$($_.ScriptStackTrace)"
    }
    $err | ConvertTo-Json -Depth 5 | Write-Output
    exit 2
}
