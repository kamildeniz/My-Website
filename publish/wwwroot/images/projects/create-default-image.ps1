# Create a simple default project image
Add-Type -AssemblyName System.Drawing

$width = 800
$height = 600

# Create a new bitmap
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Fill with a gradient background
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($width, $height)),
    [System.Drawing.Color]::FromArgb(41, 128, 185),
    [System.Drawing.Color]::FromArgb(44, 62, 80)
)
$graphics.FillRectangle($gradientBrush, 0, 0, $width, $height)

# Add some text
$font = New-Object System.Drawing.Font("Arial", 48, [System.Drawing.FontStyle]::Bold)
$text = "Project Image"
$textSize = $graphics.MeasureString($text, $font)
$textX = ($width - $textSize.Width) / 2
$textY = ($height - $textSize.Height) / 2

# Add a semi-transparent background for the text
$bgPadding = 20
$bgRect = New-Object System.Drawing.RectangleF(
    $textX - $bgPadding,
    $textY - $bgPadding,
    $textSize.Width + ($bgPadding * 2),
    $textSize.Height + ($bgPadding * 2)
)
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 0, 0, 0))
$graphics.FillRoundedRectangle($bgBrush, $bgRect, 10)

# Add the text
$textBrush = [System.Drawing.Brushes]::White
$stringFormat = New-Object System.Drawing.StringFormat
$stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
$stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

# Add rounded rectangle function
Add-Type -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(
        this Graphics graphics,
        Brush brush,
        RectangleF bounds,
        float cornerRadius)
    {
        using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
        {
            graphics.FillPath(brush, path);
        }
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        RectangleF arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));
        GraphicsPath path = new GraphicsPath();

        // Top left arc
        path.AddArc(arc, 180, 90);

        // Top right arc
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom right arc
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom left arc
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
"@

$graphics.DrawString($text, $font, $textBrush, $textX + ($textSize.Width / 2), $textY + ($textSize.Height / 2), $stringFormat)

# Save the image
$bitmap.Save("$PSScriptRoot\default-project.jpg", [System.Drawing.Imaging.ImageFormat]::Jpeg)

# Clean up
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "Default project image created successfully at: $PSScriptRoot\default-project.jpg"
