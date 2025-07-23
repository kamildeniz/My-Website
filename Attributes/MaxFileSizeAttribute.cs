using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PortfolioApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;
        
        public MaxFileSizeAttribute(int maxFileSizeInBytes)
        {
            _maxFileSize = maxFileSizeInBytes;
        }

        protected override ValidationResult IsValid(
            object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }
            else if (value is IFormFileCollection files)
            {
                foreach (var formFile in files)
                {
                    if (formFile.Length > _maxFileSize)
                    {
                        return new ValidationResult(GetErrorMessage());
                    }
                }
            }

            return ValidationResult.Success;
        }

        public string GetErrorMessage()
        {
            return $"Maksimum dosya boyutu {_maxFileSize / (1024 * 1024)} MB olmalıdır.";
        }
    }
}
