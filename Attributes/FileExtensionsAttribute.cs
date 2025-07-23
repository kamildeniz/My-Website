using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace PortfolioApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FileExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        
        public FileExtensionsAttribute(string extensions)
        {
            _extensions = extensions.Split(',').Select(e => e.Trim().ToLowerInvariant()).ToArray();
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
                
                if (string.IsNullOrEmpty(extension) || !_extensions.Contains(extension))
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }
            else if (value is IFormFileCollection files)
            {
                foreach (var formFile in files)
                {
                    var extension = Path.GetExtension(formFile.FileName).ToLowerInvariant().TrimStart('.');
                    
                    if (string.IsNullOrEmpty(extension) || !_extensions.Contains(extension))
                    {
                        return new ValidationResult(GetErrorMessage());
                    }
                }
            }

            return ValidationResult.Success;
        }

        public string GetErrorMessage()
        {
            return $"Sadece {string.Join(", ", _extensions)} uzantılı dosyalar yüklenebilir.";
        }
    }
}
