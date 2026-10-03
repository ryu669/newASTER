param([Parameter(Mandatory=$true)][string]$Gallery)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
# Compare the actual rendered art stage, excluding controls whose dynamic font atlas may differ between starts.
# These are the two authored face regions in the 1600x900 gallery layout.
# A two-screen-pixel allowance covers rasterization at the patch boundary, not the full sprite or head.
$regions=@(@(306.927,244.896,40.469,35.730),@(800.657,364.785,155.823,119.630))
foreach($height in @(720,1080)){
    $baseline=[Drawing.Bitmap]::new((Join-Path $Gallery ('idle-'+$height+'.png')))
    try{
        $boxes=@($regions | ForEach-Object { ,@(([Math]::Floor($_[0]*$baseline.Width/1600)-2),([Math]::Floor($_[1]*$baseline.Height/900)-2),([Math]::Ceiling(($_[0]+$_[2])*$baseline.Width/1600)+2),([Math]::Ceiling(($_[1]+$_[3])*$baseline.Height/900)+2)) })
        foreach($expression in @('joy','puzzled','determined')){
            $candidate=[Drawing.Bitmap]::new((Join-Path $Gallery ($expression+'-'+$height+'.png')))
            try{
                if($candidate.Width -ne $baseline.Width -or $candidate.Height -ne $baseline.Height){throw 'Capture size mismatch'}
                $inside=0;$outside=0;$outsideExamples=@()
                for($y=[int][Math]::Ceiling(160*$baseline.Height/900);$y -lt [Math]::Floor(760*$baseline.Height/900);$y++){for($x=[int][Math]::Ceiling(40*$baseline.Width/1600);$x -lt [Math]::Floor(1170*$baseline.Width/1600);$x++){
                    if($baseline.GetPixel($x,$y).ToArgb() -eq $candidate.GetPixel($x,$y).ToArgb()){continue}
                    $allowed=$false;foreach($box in $boxes){if($x -ge $box[0] -and $y -ge $box[1] -and $x -le $box[2] -and $y -le $box[3]){$allowed=$true;break}}
                    if($allowed){$inside++}else{$outside++;if($outsideExamples.Count -lt 12){$outsideExamples+=($x.ToString()+','+$y)}}
                }}
                if($outside -ne 0 -or $inside -eq 0){throw "Expression render mismatch: $expression-$height inside=$inside outside=$outside examples=$($outsideExamples -join ';')"}
                Write-Output "PLAN7_EXPRESSION_LAYER_PASS $expression-$height changedInside=$inside changedOutside=$outside"
            }finally{$candidate.Dispose()}
        }
    }finally{$baseline.Dispose()}
}
