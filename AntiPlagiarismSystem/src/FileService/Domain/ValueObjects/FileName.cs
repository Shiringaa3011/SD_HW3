using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Domain.ValueObjects
{
    public record FileName
    {
        public string Value { get; }

        public FileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("File name cannot be empty", nameof(value));
            }
            if (value.Length > 255)
            {
                throw new ArgumentException($"File name too long: {value.Length} characters. Maximum is 255", nameof(value));
            }
            Value = value;
        }
    }
}