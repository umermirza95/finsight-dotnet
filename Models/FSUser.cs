using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Finsight.Models
{
    public class FSUser : IdentityUser
    {
        private string? _defaultCurrency;
        public string? DefaultCurrency
        {
            get => _defaultCurrency ?? "USD";
            set => _defaultCurrency = value;
        }
    }
}