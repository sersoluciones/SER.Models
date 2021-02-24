using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace SER.Models
{
    public enum Options
    {
        add, replace, remove
    }

    public class SerPatchModel
    {
        [JsonPropertyName("op")]
        public Options Op { get; set; }

        [Required]
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }
}
