using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Finsight.Models
{
    public class FSUser: IdentityUser
    {
        public string? DefaultCurrency { get; set; }
    }
}