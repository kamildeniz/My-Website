Add-Type -AssemblyName System.Drawing

$width = 800
$height = 600

# Create a new bitmap
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Fill with a solid color
$graphics.Clear([System.Drawing.Color]::FromArgb(41, 128, 185))

# Add some text
$font = New-Object System.Drawing.Font("Arial", 48, [System.Drawing.FontStyle]::Bold)
$text = "Project Image"
$textSize = $graphics.MeasureString($text, $font)
$textX = ($width - $textSize.Width) / 2
$textY = ($height - $textSize.Height) / 2

# Add a semi-transparent background for the text
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 0, 0, 0))
$graphics.FillRectangle($bgBrush, $textX - 20, $textY - 20, $textSize.Width + 40, $textSize.Height + 40)

# Add the text
$textBrush = [System.Drawing.Brushes]::White
$graphics.DrawString($text, $font, $textBrush, $textX, $textY)

# Save the image
$bitmap.Save("$PSScriptRoot\default-project.jpg", [System.Drawing.Imaging.ImageFormat]::Jpeg)

# Clean up
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "Default project image created successfully at: $PSScriptRoot\default-project.jpg"
