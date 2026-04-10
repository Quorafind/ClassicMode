$ErrorActionPreference = 'Stop'
$root = 'G:\sts2modding\ClassicMode'

function To-ModelKey([string]$className) {
  if ($className.EndsWith('_C')) { return $className.ToUpperInvariant() }
  $name = if ($className.EndsWith('C')) { $className.Substring(0, $className.Length - 1) + '_C' } else { $className + '_C' }
  $out = New-Object System.Text.StringBuilder
  for ($i = 0; $i -lt $name.Length; $i++) {
    $ch = $name[$i]
    if ($i -gt 0 -and [char]::IsUpper($ch) -and [char]::IsLetterOrDigit($name[$i - 1]) -and $name[$i - 1] -ne '_') { [void]$out.Append('_') }
    [void]$out.Append($ch)
  }
  return $out.ToString().ToUpperInvariant()
}

function Get-ClassBlocks([string]$text) {
  $ms = [regex]::Matches($text, 'public\s+sealed\s+class\s+([A-Za-z0-9_]+)\s*:\s*Classic\w*Card')
  $r = @{}
  for ($i = 0; $i -lt $ms.Count; $i++) {
    $n = $ms[$i].Groups[1].Value
    $st = $ms[$i].Index
    $en = if ($i -lt $ms.Count - 1) { $ms[$i + 1].Index }else { $text.Length }
    $r[$n] = $text.Substring($st, $en - $st)
  }
  return $r
}

$files = Get-ChildItem "$root\Cards" -Recurse -Filter *.cs
$varsByKey = @{}
foreach ($f in $files) {
  $txt = Get-Content $f.FullName -Raw
  foreach ($kv in (Get-ClassBlocks $txt).GetEnumerator()) {
    $class = $kv.Key; $block = $kv.Value
    $set = New-Object 'System.Collections.Generic.HashSet[string]'

    $consts = @{}
    foreach ($m in [regex]::Matches($block, 'const\s+string\s+([A-Za-z0-9_]+)\s*=\s*"([A-Za-z0-9_]+)"')) {
      $consts[$m.Groups[1].Value] = $m.Groups[2].Value
    }

    foreach ($m in [regex]::Matches($block, 'new\s+PowerVar<\s*([A-Za-z0-9_]+)\s*>')) { [void]$set.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($block, 'new\s+[A-Za-z0-9_]+Var\s*\(\s*"([A-Za-z0-9_]+)"')) { [void]$set.Add($m.Groups[1].Value) }
    foreach ($m in [regex]::Matches($block, 'new\s+[A-Za-z0-9_]+Var\s*\(\s*([A-Za-z0-9_]+)\s*,')) {
      $k = $m.Groups[1].Value
      if ($consts.ContainsKey($k)) { [void]$set.Add($consts[$k]) }
    }

    $defaultMap = @{
      'DamageVar' = 'Damage'; 'BlockVar' = 'Block'; 'CardsVar' = 'Cards'; 'RepeatVar' = 'Repeat';
      'EnergyVar' = 'Energy'; 'HpLossVar' = 'HpLoss'; 'MagicVar' = 'MagicNumber';
      'CalculatedDamageVar' = 'CalculatedDamage'; 'CalculatedBlockVar' = 'CalculatedBlock';
      'CalculationBaseVar' = 'CalculationBase'; 'CalculationExtraVar' = 'CalculationExtra';
      'ExtraDamageVar' = 'ExtraDamage'
    }
    foreach ($entry in $defaultMap.GetEnumerator()) {
      if ($block.Contains("new $($entry.Key)(")) { [void]$set.Add($entry.Value) }
    }

    $varsByKey[(To-ModelKey $class)] = @($set)
  }
}

$skip = @('energyPrefix', 'IfUpgraded')
$rx = '\{([A-Za-z0-9_]+):[^}]+\}'
foreach ($lang in @('zhs', 'eng')) {
  $cards = Get-Content "$root\assets\ClassicMode\localization\$lang\cards.json" -Raw | ConvertFrom-Json
  $problems = @()
  foreach ($p in $cards.PSObject.Properties) {
    if (-not $p.Name.EndsWith('.description')) { continue }
    $key = $p.Name.Substring(0, $p.Name.Length - 12)
    if (-not $varsByKey.ContainsKey($key)) { continue }
    $vars = $varsByKey[$key]
    $ph = [regex]::Matches([string]$p.Value, $rx) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    foreach ($name in $ph) {
      if ($name -in $skip) { continue }
      if ($name -cmatch '^[a-z]') { continue }
      if ($name -notin $vars) { $problems += [pscustomobject]@{Lang = $lang; Card = $key; Placeholder = $name; Vars = ($vars -join ',') } }
    }
  }
  "${lang} mismatches: $($problems.Count)"
  if ($problems.Count -gt 0) { $problems | Sort-Object Card, Placeholder | Format-Table -AutoSize | Out-String -Width 240 }
}
