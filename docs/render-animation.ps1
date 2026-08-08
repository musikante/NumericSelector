# Regenerates docs/images/numeric-selector-motion.gif, the animation in the README.
#
# Run it from anywhere with:  pwsh -File docs/render-animation.ps1
# Requirements: the solution built in Release, and ffmpeg on PATH.
#
# It does NOT record the screen. It hosts the real control —the very DLL that ships in the
# package— in an off-screen window and renders one frame per property change with
# RenderTargetBitmap. Nothing is simulated: what the GIF shows is the control's own template
# reacting, so the animation cannot drift away from the behaviour it advertises. It also means
# there is no mouse pointer in it, and no input injection to be flaky about.
#
# Two traps, both already paid for:
#   * the window has to be Show()n, or the lookless control never applies its template and
#     everything measures 0;
#   * RenderTargetBitmap honours the element's offset inside its parent, so what gets rendered
#     is the fixed-size ROOT (offset 0,0). Rendering the centred control yields a blank image.
#
# The host must be a .NET 10 STA process. PowerShell 7 on Windows is both, so `pwsh` is enough.

$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$dll = Join-Path $repo 'NumericSelector\bin\Release\net10.0-windows\NumericSelector.dll'
$gif = Join-Path $PSScriptRoot 'images\numeric-selector-motion.gif'
# NOTE: not named $frames. PowerShell scoping is dynamic, so a variable by that name would be
# shadowed inside Save-Frame by the $frames parameter of the Hold/Sweep callers, and the frames
# would be written to a folder named after a frame count.
$frameDir = Join-Path ([System.IO.Path]::GetTempPath()) 'numeric-selector-frames'

if (-not (Test-Path $dll)) {
	throw "The control is not built in Release. Run: dotnet build $repo\NumericSelector.slnx -c Release"
}
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
	throw "ffmpeg is not on PATH; it is what turns the frames into the GIF."
}

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
Add-Type -Path $dll

Remove-Item $frameDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $frameDir | Out-Null

$W = 460; $H = 112

# --- the subject ---
$sel = New-Object NumericSelector.BoundedNumericSelector
$sel.Minimum = 0; $sel.Maximum = 100; $sel.Value = 0
$sel.ResetValue = 50
$sel.BaseWidth = 320
$sel.MainText = "MainText"
$sel.DetailText = "DetailText"
$sel.VerticalAlignment = 'Center'
$sel.HorizontalAlignment = 'Center'

# --- the caption, OUTSIDE the control: what is not a property name does not go inside it ---
$caption = New-Object System.Windows.Controls.TextBlock
$caption.HorizontalAlignment = 'Center'
$caption.VerticalAlignment = 'Center'
$caption.FontFamily = New-Object System.Windows.Media.FontFamily("Consolas")
$caption.FontSize = 12
$caption.Foreground = New-Object System.Windows.Media.SolidColorBrush(
	[System.Windows.Media.Color]::FromRgb(0x5A, 0x6B, 0x7A))

$grid = New-Object System.Windows.Controls.Grid
$grid.Width = $W; $grid.Height = $H
$grid.Background = [System.Windows.Media.Brushes]::White
$r1 = New-Object System.Windows.Controls.RowDefinition; $r1.Height = '*'
$r2 = New-Object System.Windows.Controls.RowDefinition; $r2.Height = '24'
$grid.RowDefinitions.Add($r1); $grid.RowDefinitions.Add($r2)
[System.Windows.Controls.Grid]::SetRow($sel, 0)
[System.Windows.Controls.Grid]::SetRow($caption, 1)
$grid.Children.Add($sel) | Out-Null
$grid.Children.Add($caption) | Out-Null

$win = New-Object System.Windows.Window
$win.WindowStyle = 'None'; $win.ShowInTaskbar = $false; $win.ResizeMode = 'NoResize'
$win.SizeToContent = 'WidthAndHeight'
$win.Left = -3000; $win.Top = -3000
$win.Content = $grid
$win.Show()
$win.Dispatcher.Invoke([action]{}, [System.Windows.Threading.DispatcherPriority]::Loaded)

$script:n = 0
function Save-Frame {
	$win.UpdateLayout()
	$win.Dispatcher.Invoke([action]{}, [System.Windows.Threading.DispatcherPriority]::Render)
	$rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
		$W, $H, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
	$rtb.Render($grid)
	$enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
	$enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
	$script:n++
	$fs = [System.IO.File]::Create((Join-Path $frameDir ("f_{0:d4}.png" -f $script:n)))
	$enc.Save($fs); $fs.Close()
}

function Hold([int]$frames) { for ($i = 0; $i -lt $frames; $i++) { Save-Frame } }

# Eased sweep, so the bar moves like a hand dragging it and not like a metronome.
function Sweep([int]$from, [int]$to, [int]$frames) {
	for ($i = 1; $i -le $frames; $i++) {
		$t = $i / $frames
		$e = $t * $t * (3 - 2 * $t)   # smoothstep
		$sel.Value = [int][Math]::Round($from + ($to - $from) * $e)
		Save-Frame
	}
}

# ---------------- storyboard ----------------

# 1. The bar filling: the signature gesture of the control.
$caption.Text = "Value"
Hold 6
Sweep 0 100 26
Sweep 100 65 12
Hold 8

# 2. The optional detail row.
$caption.Text = "ShowDetail = true"
$sel.ShowDetail = $true
Hold 26

# 3. The value box changing sides.
$caption.Text = "ValueBoxDock = Left"
$sel.ValueBoxDock = [NumericSelector.ValueBoxDock]::Left
Hold 24
$caption.Text = "ValueBoxDock = Right"
$sel.ValueBoxDock = [NumericSelector.ValueBoxDock]::Right
Hold 16

# 4. Any Brush, not just a flat colour — and the value moving so the gradient is seen filling.
$caption.Text = "BarFill = LinearGradientBrush"
$g = New-Object System.Windows.Media.LinearGradientBrush
$g.StartPoint = New-Object System.Windows.Point(0, 0)
$g.EndPoint = New-Object System.Windows.Point(1, 0)
$g.GradientStops.Add((New-Object System.Windows.Media.GradientStop(
	[System.Windows.Media.Color]::FromRgb(0xFF, 0xC1, 0x07), 0)))
$g.GradientStops.Add((New-Object System.Windows.Media.GradientStop(
	[System.Windows.Media.Color]::FromRgb(0xFF, 0x57, 0x22), 1)))
$sel.BarFill = $g
Hold 10
Sweep 65 90 14
Sweep 90 40 16
Hold 10

# 5. Back to the starting state, so the loop closes without a jump.
$caption.Text = "Value"
$sel.BarFill = [System.Windows.Media.Brushes]::Orange
$sel.ShowDetail = $false
Sweep 40 0 12
Hold 4

$win.Close()

# 20 fps, and a palette computed from the frames themselves: the drawing is mostly flat colour
# plus one gradient, so 128 colours are plenty and the file stays under 100 KB.
ffmpeg -y -loglevel error -framerate 20 -i (Join-Path $frameDir 'f_%04d.png') `
	-filter_complex "[0:v]split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle" `
	$gif

Remove-Item $frameDir -Recurse -Force -ErrorAction SilentlyContinue
"{0} frames -> {1} ({2:N0} KB)" -f $script:n, $gif, ((Get-Item $gif).Length / 1KB)
