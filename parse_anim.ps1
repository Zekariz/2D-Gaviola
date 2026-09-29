$content = Get-Content "Assets/Animations/Player/Player.controller" -Raw
$states = [regex]::Matches($content, '--- !u!1102 &(\d+)[\s\S]*?m_Name: (.*?)[\s\S]*?m_Transitions:\s*([\s\S]*?)\s*m_StateMachineBehaviours:')
$transitions = [regex]::Matches($content, '--- !u!1101 &(\d+)[\s\S]*?m_Conditions:\s*([\s\S]*?)\s*m_DstStateMachine:[\s\S]*?m_DstState: \{fileID: (-?\d+)\}[\s\S]*?m_ExitTime: ([\d\.]+)[\s\S]*?m_HasExitTime: (\d)[\s\S]*?m_HasFixedDuration: (\d)')

$transDict = @{}
foreach ($t in $transitions) {
    $id = $t.Groups[1].Value
    $dst = $t.Groups[3].Value
    $hasExit = $t.Groups[5].Value
    $exitTime = $t.Groups[4].Value
    $conds = $t.Groups[2].Value
    $condList = [regex]::Matches($conds, 'm_ConditionEvent: (.*?)\s+m_EventTreshold: ([\d\.]+)') | ForEach-Object { "$($_.Groups[1].Value)=$($_.Groups[2].Value)" }
    $transDict[$id] = @{ Dst=$dst; Conds=($condList -join ", "); HasExit=$hasExit; ExitTime=$exitTime }
}

$stateNames = @{}
foreach ($s in $states) {
    $stateNames[$s.Groups[1].Value] = $s.Groups[2].Value
    $stateNames["-" + $s.Groups[1].Value] = $s.Groups[2].Value # Handle negative IDs just in case, though Unity usually matches exactly
}

foreach ($s in $states) {
    $sId = $s.Groups[1].Value
    $sName = $s.Groups[2].Value
    Write-Output "STATE: $sName"
    $tLines = [regex]::Matches($s.Groups[3].Value, '\{fileID: (-?\d+)\}')
    foreach ($tl in $tLines) {
        $tId = $tl.Groups[1].Value
        $tObj = $transDict[$tId]
        if ($tObj) {
            $dstName = $stateNames[$tObj.Dst]
            if (-not $dstName) { $dstName = $tObj.Dst }
            Write-Output "  -> $dstName | Conds: $($tObj.Conds) | HasExit: $($tObj.HasExit) | ExitTime: $($tObj.ExitTime)"
        }
    }
}
