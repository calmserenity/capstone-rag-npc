Add-Type -AssemblyName System.Drawing

$outputDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

function New-Canvas {
    param([int]$Width = 1400, [int]$Height = 820)
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $graphics.Clear([System.Drawing.Color]::White)
    return @($bitmap, $graphics)
}

function Save-Canvas {
    param($Bitmap, $Graphics, [string]$Path)
    $Graphics.Dispose()
    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Bitmap.Dispose()
}

$navy = [System.Drawing.Color]::FromArgb(40, 62, 90)
$blue = [System.Drawing.Color]::FromArgb(78, 121, 167)
$orange = [System.Drawing.Color]::FromArgb(242, 142, 43)
$green = [System.Drawing.Color]::FromArgb(89, 161, 79)
$grid = [System.Drawing.Color]::FromArgb(218, 223, 230)
$dark = [System.Drawing.Color]::FromArgb(45, 45, 45)
$muted = [System.Drawing.Color]::FromArgb(105, 105, 105)

$titleFont = New-Object System.Drawing.Font("Arial", 28, [System.Drawing.FontStyle]::Bold)
$axisFont = New-Object System.Drawing.Font("Arial", 18, [System.Drawing.FontStyle]::Regular)
$labelFont = New-Object System.Drawing.Font("Arial", 18, [System.Drawing.FontStyle]::Bold)
$smallFont = New-Object System.Drawing.Font("Arial", 15, [System.Drawing.FontStyle]::Regular)

# Automated test results
$canvas = New-Canvas
$bitmap = $canvas[0]
$graphics = $canvas[1]
$graphics.DrawString("Automated Software Verification Results", $titleFont, (New-Object System.Drawing.SolidBrush($navy)), 365, 45)

$plotLeft = 260
$plotTop = 165
$plotWidth = 980
$rowHeight = 180
$maxTests = 20

for ($tick = 0; $tick -le $maxTests; $tick += 5) {
    $x = $plotLeft + ($tick / $maxTests) * $plotWidth
    $graphics.DrawLine((New-Object System.Drawing.Pen($grid, 2)), $x, $plotTop - 25, $x, $plotTop + 2 * $rowHeight + 70)
    $graphics.DrawString($tick.ToString(), $smallFont, (New-Object System.Drawing.SolidBrush($muted)), $x - 8, $plotTop + 2 * $rowHeight + 80)
}

$testRows = @(
    @{ Name = "Backend pytest suite"; Passed = 20; Y = $plotTop },
    @{ Name = "Unity EditMode suite"; Passed = 6; Y = $plotTop + $rowHeight }
)

foreach ($row in $testRows) {
    $graphics.DrawString($row.Name, $axisFont, (New-Object System.Drawing.SolidBrush($dark)), 35, $row.Y + 30)
    $barWidth = ($row.Passed / $maxTests) * $plotWidth
    $graphics.FillRectangle((New-Object System.Drawing.SolidBrush($green)), $plotLeft, $row.Y, $barWidth, 90)
    $graphics.DrawString("$($row.Passed) passed", $labelFont, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)), $plotLeft + 18, $row.Y + 28)
    $graphics.DrawString("0 failed", $smallFont, (New-Object System.Drawing.SolidBrush($muted)), $plotLeft + $barWidth + 18, $row.Y + 32)
}

$graphics.DrawString("Number of automated tests", $axisFont, (New-Object System.Drawing.SolidBrush($dark)), 590, 705)
Save-Canvas -Bitmap $bitmap -Graphics $graphics -Path (Join-Path $outputDirectory "chapter4-automated-test-results.png")

# RAGAS comparison
$canvas = New-Canvas
$bitmap = $canvas[0]
$graphics = $canvas[1]
$graphics.DrawString("RAGAS Evaluation: Baseline and Tuned System", $titleFont, (New-Object System.Drawing.SolidBrush($navy)), 320, 45)

$plotLeft = 130
$plotTop = 150
$plotHeight = 500
$plotBottom = $plotTop + $plotHeight
$groupWidth = 380
$barWidth = 88

for ($tick = 0; $tick -le 10; $tick += 2) {
    $value = $tick / 10
    $y = $plotBottom - $value * $plotHeight
    $graphics.DrawLine((New-Object System.Drawing.Pen($grid, 2)), $plotLeft, $y, 1280, $y)
    $graphics.DrawString(("{0:N1}" -f $value), $smallFont, (New-Object System.Drawing.SolidBrush($muted)), 105, $y - 10)
}

$metrics = @(
    @{ Name = "Context precision"; Baseline = 0.558333; Tuned = 0.508333 },
    @{ Name = "Faithfulness"; Baseline = 0.100000; Tuned = 0.475000 },
    @{ Name = "Answer relevancy"; Baseline = 0.122629; Tuned = 0.405503 }
)

for ($index = 0; $index -lt $metrics.Count; $index++) {
    $metric = $metrics[$index]
    $groupX = $plotLeft + 70 + $index * $groupWidth
    $baseHeight = $metric.Baseline * $plotHeight
    $tunedHeight = $metric.Tuned * $plotHeight
    $baseY = $plotBottom - $baseHeight
    $tunedY = $plotBottom - $tunedHeight

    $graphics.FillRectangle((New-Object System.Drawing.SolidBrush($blue)), $groupX, $baseY, $barWidth, $baseHeight)
    $graphics.FillRectangle((New-Object System.Drawing.SolidBrush($orange)), $groupX + 115, $tunedY, $barWidth, $tunedHeight)

    $graphics.DrawString(("{0:N3}" -f $metric.Baseline), $smallFont, (New-Object System.Drawing.SolidBrush($dark)), $groupX - 2, $baseY - 32)
    $graphics.DrawString(("{0:N3}" -f $metric.Tuned), $smallFont, (New-Object System.Drawing.SolidBrush($dark)), $groupX + 112, $tunedY - 32)
    $labelFormat = New-Object System.Drawing.StringFormat
    $labelFormat.Alignment = [System.Drawing.StringAlignment]::Center
    $labelRect = [System.Drawing.RectangleF]::new(
        [float]($groupX - 65),
        [float]($plotBottom + 24),
        320,
        44
    )
    $graphics.DrawString(
        $metric.Name,
        $smallFont,
        (New-Object System.Drawing.SolidBrush($dark)),
        $labelRect,
        $labelFormat
    )
    $labelFormat.Dispose()
}

$graphics.FillRectangle((New-Object System.Drawing.SolidBrush($blue)), 500, 735, 32, 22)
$graphics.DrawString("Baseline", $smallFont, (New-Object System.Drawing.SolidBrush($dark)), 544, 733)
$graphics.FillRectangle((New-Object System.Drawing.SolidBrush($orange)), 720, 735, 32, 22)
$graphics.DrawString("Tuned", $smallFont, (New-Object System.Drawing.SolidBrush($dark)), 764, 733)

Save-Canvas -Bitmap $bitmap -Graphics $graphics -Path (Join-Path $outputDirectory "chapter4-ragas-comparison.png")

$titleFont.Dispose()
$axisFont.Dispose()
$labelFont.Dispose()
$smallFont.Dispose()
