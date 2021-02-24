using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SER.Models.Patch
{
    public class BaseValidationModel
    {
        [Required]
        public string Model { get; set; }
        [Required]
        public string Field { get; set; }
        [Required]
        public string Value { get; set; }
        public string Id { get; set; }
    }

    public class UserValidationModel : BaseValidationModel
    {
        public new string Model { get; set; } = "User";
    }
}
