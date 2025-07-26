# Create placeholder images using .NET

# Create eticaret-api.jpg
Add-Type -AssemblyName System.Drawing
$width = 800
$height = 600

# Function to create a placeholder image
function Create-PlaceholderImage($path, $text, $bgColor, $textColor) {
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Draw background
    $brush = New-Object System.Drawing.SolidBrush($bgColor)
    $graphics.FillRectangle($brush, 0, 0, $width, $height)
    
    # Draw text
    $font = New-Object System.Drawing.Font("Arial", 32, [System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush($textColor)
    $stringFormat = New-Object System.Drawing.StringFormat
    $stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
    $stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
    
    $rect = New-Object System.Drawing.RectangleF(0, 0, $width, $height)
    $graphics.DrawString($text, $font, $brush, $rect, $stringFormat)
    
    # Save the image
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    
    # Clean up
    $graphics.Dispose()
    $bitmap.Dispose()
}

# Create images directory if it doesn't exist
$imageDir = "$PSScriptRoot"
if (-not (Test-Path $imageDir)) {
    New-Item -ItemType Directory -Path $imageDir | Out-Null
}

# Create placeholder images
Create-PlaceholderImage -Path "$imageDir\eticaret-api.jpg" -Text "E-Ticaret API" -bgColor "#6e8efb" -textColor "White"
Create-PlaceholderImage -Path "$imageDir\library-management.jpg" -Text "Kütüphane Yönetim Sistemi" -bgColor "#2ecc71" -textColor "White"
Create-PlaceholderImage -Path "$imageDir\nlayered-demo.jpg" -Text "Katmanlı Mimari Demo" -bgColor "#e74c3c" -textColor "White"
Create-PlaceholderImage -Path "$imageDir\nlayer-udemy.jpg" -Text "N-Katmanlı Uygulama" -bgColor "#f39c12" -textColor "White"
Create-PlaceholderImage -Path "$imageDir\personal-website.jpg" -Text "Kişisel Web Sitesi" -bgColor "#9b59b6" -textColor "White"

Write-Host "Placeholder images have been created in $imageDir" -ForegroundColor Green
