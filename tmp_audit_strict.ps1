$ErrorActionPreference='Stop'
$root='G:\sts2modding\ClassicMode'
$lang='zhs'
$cards=Get-Content "$root\assets\ClassicMode\localization\$lang\cards.json" -Raw | ConvertFrom-Json
$codeFiles=Get-ChildItem "$root\Cards" -Recurse -Filter *.cs

function To-Key([string]$className){$base=$className -replace '_C$','';$s=[regex]::Replace($base,'([a-z0-9])([A-Z])','$1_$2');return ($s.ToUpperInvariant()+'_C')}
function Get-ClassBlocks([string]$text){$ms=[regex]::Matches($text,'public\s+sealed\s+class\s+([A-Za-z0-9_]+_C)\b');$r=@{};for($i=0;$i -lt $ms.Count;$i++){$n=$ms[$i].Groups[1].Value;$st=$ms[$i].Index;$en=if($i -lt $ms.Count-1){$ms[$i+1].Index}else{$text.Length};$r[$n]=$text.Substring($st,$en-$st)};return $r}

$varsByKey=@{}
foreach($f in $codeFiles){
  $txt=Get-Content $f.FullName -Raw
  foreach($kv in (Get-ClassBlocks $txt).GetEnumerator()){
    $block=$kv.Value
    $set=New-Object 'System.Collections.Generic.HashSet[string]'
    $consts=@{}
    foreach($m in [regex]::Matches($block,'const\s+string\s+([A-Za-z0-9_]+)\s*=\s*"([A-Za-z0-9_]+)"')){$consts[$m.Groups[1].Value]=$m.Groups[2].Value}

    foreach($m in [regex]::Matches($block,'new\s+DynamicVar\("([A-Za-z0-9_]+)"')){[void]$set.Add($m.Groups[1].Value)}
    foreach($m in [regex]::Matches($block,'new\s+DynamicVar\(([A-Za-z0-9_]+)\s*,')){ $k=$m.Groups[1].Value; if($consts.ContainsKey($k)){ [void]$set.Add($consts[$k]) } }
    foreach($m in [regex]::Matches($block,'new\s+PowerVar<\s*([A-Za-z0-9_]+)\s*>')){[void]$set.Add($m.Groups[1].Value)}
    foreach($m in [regex]::Matches($block,'new\s+([A-Za-z0-9_]+Var)\s*\(')){
      $raw=$m.Groups[1].Value
      if($raw -in @('DynamicVar','PowerVar')){continue}
      [void]$set.Add($raw.Substring(0,$raw.Length-3))
    }

    $varsByKey[(To-Key $kv.Key)] = @($set)
  }
}

$skip=@('energyPrefix')
$rx='\{([A-Za-z0-9_]+):[^}]+\}'
$problems=@()

foreach($p in $cards.PSObject.Properties){
  if(-not $p.Name.EndsWith('.description')){continue}
  $key=$p.Name.Substring(0,$p.Name.Length-12)
  $vars= if($varsByKey.ContainsKey($key)){$varsByKey[$key]}else{@()}
  $ph=[regex]::Matches([string]$p.Value,$rx) | ForEach-Object {$_.Groups[1].Value} | Select-Object -Unique
  foreach($name in $ph){
    if($name -in $skip){continue}
    if($name -cmatch '^[a-z]'){continue}
    if($name -notin $vars){
      $problems += [pscustomobject]@{Card=$key;Placeholder=$name;Vars=($vars -join ',')}
    }
  }
}

"Mismatches: $($problems.Count)"
$problems | Sort-Object Card,Placeholder | Format-Table -AutoSize | Out-String -Width 260
